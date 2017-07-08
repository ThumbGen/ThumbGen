using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ThumbGen.Bundles
{
    public class BundlesDatabase
    {
        private readonly string bundlesDB;

        private XDocument document;

        public IEnumerable<Bundle> Bundles { get; private set; }

        public void Refresh()
        {
            document = XDocument.Load(bundlesDB);

            if (document.Root == null) CreateDatabase();

            Bundles = document.Descendants("Bundle").Select(x => new Bundle(x));
        }

        private void CreateDatabase()
        {
            document = new XDocument(new XElement("Bundles"));
            document.Save(bundlesDB);
        }

        public BundlesDatabase(string basePath)
        {
            Bundles = new List<Bundle>();
            bundlesDB = Path.Combine(basePath, Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".bundles");
            if (!File.Exists(bundlesDB))
            {
                CreateDatabase();
            }
        }

        public void AddBundle(Bundle bundle)
        {
            Refresh();
            if (document.Root == null) return;

            if (HasBundle(bundle.Id, bundle.Version))
            {
                return;
            }

            bundle.AddProperty("InstalledOn", DateTime.Now.Date.ToString("dd.MM.yyyy"));
            document.Root.Add(bundle.Document.Root);

            document.Save(bundlesDB);
            UpdateBundlesList();
        }

        public bool RemoveBundle(string id, string version)
        {
            Refresh();
            if (document.Root == null) return false;

            var bundle = document.Root.XPathSelectElement(string.Format("//Bundles/Bundle/Properties[Id='{0}' and Version='{1}']", id, version));
            if (bundle != null)
            {
                bundle.Parent.Remove();
                document.Save(bundlesDB);
            }
            UpdateBundlesList();
            return true;
        }

        public bool HasBundle(string id, string version)
        {
            return GetBundle(id, version) != null;
        }

        public bool HasBundle(string id)
        {
            return GetBundle(id) != null;
        }

        public Bundle GetBundle(string id)
        {
            UpdateBundlesList();
            return Bundles.FirstOrDefault(x => x.Id == id);
        }

        public Bundle GetBundle(string id, string version)
        {
            UpdateBundlesList();
            return Bundles.FirstOrDefault(x => x.Id == id && x.Version == version);
        }

        private void UpdateBundlesList()
        {
            Refresh();
        }
    }
}
