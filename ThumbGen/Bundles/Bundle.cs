using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ThumbGen.Bundles
{
    public class Bundle
    {
        public XDocument Document { get; private set; }

        private readonly XElement profilesElement;
        private readonly XElement templatesElement;
        private readonly XElement propertiesElement;
        private readonly XElement fontsElement;

        public string Id
        {
            get
            {
                return GetProperty("Id");
            }
        }

        public string Name
        {
            get
            {
                return GetProperty("Name");
            }
        }

        public string Author
        {
            get
            {
                return GetProperty("Author");
            }
        }

        public string Version
        {
            get
            {
                return GetProperty("Version");
            }
        }

        public string DefaultProfile
        {
            get
            {
                return GetProperty("DefaultProfile");
            }
        }

        public string InstalledOn
        {
            get
            {
                return GetProperty("InstalledOn");
            }
        }

        public IEnumerable<string> Profiles
        {
            get
            {
                return profilesElement.SafeDescendantValues("Profile");
            }
        }

        public IEnumerable<string> Templates
        {
            get
            {
                return templatesElement.SafeDescendantValues("Template");
            }
        }

        public IEnumerable<string> Fonts
        {
            get
            {
                return fontsElement.SafeDescendantValues("Font");
            }
        }

        public Bundle(XElement bundleRoot = null)
        {
            Document = new XDocument();
            if (bundleRoot == null)
            {
                var root = new XElement("Bundle");
                Document.Add(root);

                propertiesElement = new XElement("Properties");
                root.Add(propertiesElement);

                profilesElement = new XElement("Profiles");
                root.Add(profilesElement);

                templatesElement = new XElement("Templates");
                root.Add(templatesElement);

                fontsElement = new XElement("Fonts");
                root.Add(fontsElement);
            }
            else
            {
                Document = new XDocument(bundleRoot);
                propertiesElement = Document.Descendants("Properties").FirstOrDefault();
                profilesElement = Document.Descendants("Profiles").FirstOrDefault();
                templatesElement = Document.Descendants("Templates").FirstOrDefault();
                fontsElement = Document.Descendants("Fonts").FirstOrDefault();
            }
        }

        public void AddProfile(string profile)
        {
            profilesElement.Add(new XElement("Profile", profile));
        }

        public void AddTemplate(string template)
        {
            templatesElement.Add(new XElement("Template", template));
        }

        public void AddFont(string font)
        {
            fontsElement.Add(new XElement("Font", font));
        }

        public void ExtractProperties(XDocument source)
        {
            if (source == null) return;
            var props = source.Descendants("Properties").FirstOrDefault();
            if (props != null)
            {
                SetProperties(props);
            }
        }

        public void SetProperties(XElement properties)
        {
            propertiesElement.ReplaceAll(properties.Descendants());
        }

        public void AddProperty(string name, string value)
        {
            propertiesElement.Add(new XElement(name, value));
        }

        public string GetProperty(string name)
        {
            return propertiesElement.SafeDescendantValue(name);
        }

    }

}

