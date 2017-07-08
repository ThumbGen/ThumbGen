using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Ionic.Zip;

namespace ThumbGen.Bundles
{
    public class BundleManager
    {
        private BundlesDatabase database;

        private readonly string bundleFilename;
        private readonly string basePath;
        private bool installing;
        private ZipFile zipFile;

        public event EventHandler<BundleEventArgs> BundleInstalled;

        public event EventHandler<BundleEventArgs> BundleRemoved;

        public static bool InstallBundle(string basePath, string bundleFilename)
        {
            return new BundleManager(basePath, bundleFilename).InstallBundle();
        }

        public BundleManager(string basePath, string bundleFilename)
        {
            this.bundleFilename = bundleFilename;
            this.basePath = basePath;
            database = new BundlesDatabase(basePath);
        }

        public bool InstallBundle()
        {
            if (string.IsNullOrEmpty(bundleFilename) || !File.Exists(bundleFilename))
            {
                return false;
            }
            if (installing) return false;

            installing = true;
            try
            {
                using (zipFile = new ZipFile(bundleFilename))
                {
                    // check if the file is logically valid
                    if (!CheckIntegrity())
                    {
                        return false;
                    }
                    // get the Bundle object
                    var bundle = GetBundle();
                    // start extracting profiles
                    ProcessProfiles(bundle);
                    // start extracting templates
                    ProcessTemplates(bundle);
                    // set default profile
                    //SetDefaultProfile(bundle);
                    // store the installed bundle in the database
                    database.AddBundle(bundle);
                    // raise installed event
                    var handler = BundleInstalled;
                    if (handler != null)
                    {
                        handler(this, new BundleEventArgs(bundle));
                    }
                }

            }
            finally
            {
                zipFile = null;
                installing = false;
            }
            return true;
        }

        public Bundle GetBundle()
        {
            using (zipFile = new ZipFile(bundleFilename))
            {
                var bundle = new Bundle();
                bundle.ExtractProperties(LoadBundleConfig());
                return bundle;
            }
        }

        public bool RemoveBundle(string id, string version)
        {
            // remove the selected bundle
            var bundle = database.GetBundle(id, version);

            // uninstall fonts
            bundle.Fonts.ForEach(x =>
                                     {
                                         try
                                         {
                                             FontHelper.RemoveFont(Path.Combine(basePath, x));
                                         }
                                         catch
                                         {
                                         }
                                     });

            // remove profiles (files)
            bundle.Profiles.ForEach(x =>
                                        {
                                            try
                                            {
                                                File.Delete(Path.Combine(FileManager.GetProfilesFolder(), x));
                                            }
                                            catch
                                            {
                                            }
                                        });
            // remove templates (folders)
            bundle.Templates.ForEach(x =>
                                         {
                                             try
                                             {
                                                 Directory.Delete(Path.Combine(FileManager.GetTemplatesFolder(), x), true);
                                             }
                                             catch
                                             {
                                             }
                                         });


            var result = database.RemoveBundle(bundle.Id, bundle.Version);

            var handler = BundleRemoved;
            if (handler != null)
            {
                handler(this, new BundleEventArgs(bundle));
            }

            return result;
        }


        private void ProcessProfiles(Bundle bundle)
        {
            zipFile.Where(x => x.FileName.StartsWith("Profiles/", false, CultureInfo.InvariantCulture))
                   .ForEach(x =>
                   {
                       x.Extract(basePath, ExtractExistingFileAction.OverwriteSilently);
                       bundle.AddProfile(Path.GetFileName(x.FileName));
                   });
        }

        private void ProcessTemplates(Bundle bundle)
        {
            zipFile.Where(x => x.FileName.StartsWith("Templates/", false, CultureInfo.InvariantCulture))
                   .ForEach(x =>
                   {
                       x.Extract(basePath, ExtractExistingFileAction.OverwriteSilently);
                       if (!x.IsDirectory)
                       {
                           // install it if it's a font
                           var ext = Path.GetExtension(x.FileName);
                           if (String.Compare(ext, ".ttf", StringComparison.OrdinalIgnoreCase) == 0
                               || String.Compare(ext, ".otf", StringComparison.OrdinalIgnoreCase) == 0)
                           {
                               var fontPath = Path.Combine(basePath, x.FileName);
                               // if font was not present, and successfully installed then remember it for uninstallation
                               if (FontHelper.InstallFont(fontPath) == FontActionResult.InstalledSuccessfully)
                               {
                                   bundle.AddFont(x.FileName);
                               }
                           }
                       }
                       else
                       {
                           var name = Regex.Replace(x.FileName, "\\ATemplates/", string.Empty).TrimEnd('/');
                           bundle.AddTemplate(name);
                       }
                   });
            //var root = zipFile.EntriesSorted.FirstOrDefault(x => x.IsDirectory);
            //if (root != null)
            //{
            //    var name = Regex.Replace(root.FileName, "\\ATemplates/", string.Empty).TrimEnd('/');
            //    bundle.AddTemplate(name);
            //}
        }

        //private void SetDefaultProfile(Bundle bundle)
        //{
        //    var defaultProfile = bundle.DefaultProfile;
        //    if (!string.IsNullOrEmpty(defaultProfile))
        //    {
        //        var config = Path.Combine(basePath, "ThumbGen.tgp");
        //        if (!File.Exists(config)) return;

        //        var doc = XDocument.Load(config);
        //        var node = doc.Root != null ? doc.Root.Descendants("LastProfileUsed").FirstOrDefault() : null;
        //        if (node != null)
        //        {
        //            node.Value = defaultProfile;
        //            doc.Save(config);
        //        }
        //    }
        //}

        private XDocument LoadBundleConfig()
        {
            var entry = GetBundleConfig();
            if (entry != null)
            {
                XDocument result;
                var tmp = Path.GetTempFileName();
                try
                {
                    using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.ReadWrite))
                    {
                        entry.Extract(stream);
                        stream.Position = 0;
                        using (XmlReader reader = XmlReader.Create(stream))
                        {
                            result = XDocument.Load(reader);
                        }
                    }
                }
                finally
                {
                    File.Delete(tmp);
                }
                return result;
            }
            return null;
        }

        private ZipEntry GetBundleConfig()
        {
            return zipFile.FirstOrDefault(x => !x.IsDirectory && String.Compare(x.FileName, "Bundle.cfg", StringComparison.OrdinalIgnoreCase) == 0);
        }

        private bool CheckIntegrity()
        {
            // check if bundle.cfg is present
            var hasBundleCfg = GetBundleConfig() != null;


            return hasBundleCfg;
        }

    }

    public class BundleEventArgs : EventArgs
    {
        public Bundle Bundle { get; private set; }

        public BundleEventArgs(Bundle bundle)
        {
            Bundle = bundle;
        }
    }
}
