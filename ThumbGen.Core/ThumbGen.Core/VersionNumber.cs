using System;
using System.Deployment;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Microsoft.Win32;

namespace ThumbGen.Core
{
    public class VersionNumber
    {
        /// <summary>
        /// Initializes an instance of the <c>AssemblyVersionTextBlock</c> class.
        /// </summary>

        public enum VersionTypes
        {
            Long,
            Short,
            Medium
        }

        public static string ShortVersion
        {
            get
            {
                return GetVersion(VersionTypes.Short, false);
            }
        }

        public static string CoreShortVersion
        {
            get
            {
                return GetVersion(VersionTypes.Short, true);
            }
        }

        public static string LongVersion
        {
            get
            {
                return GetVersion(VersionTypes.Long, false);
            }
        }

        public static string CoreLongVersion
        {
            get
            {
                return GetVersion(VersionTypes.Long, true);
            }
        }

        public static string MediumVersion
        {
            get
            {
                return GetVersion(VersionTypes.Medium, false);
            }
        }

        public static string CoreMediumVersion
        {
            get
            {
                return GetVersion(VersionTypes.Medium, true);
            }
        }

        public VersionNumber()
        {


        }

        private static string GetVersion(VersionTypes versionType, bool isCore)
        {
            try
            {
                Assembly _EntryAssembly = Assembly.GetEntryAssembly();
                AssemblyName[] _AllAssembly = _EntryAssembly.GetReferencedAssemblies();
                Assembly _ExecutingAssembly = Assembly.GetExecutingAssembly();
                Assembly _ass = isCore ? _ExecutingAssembly : _EntryAssembly;
                switch (versionType)
                {
                    case VersionTypes.Long:
                        return _ass.GetName().Version.ToString();
                    //this.ToolTip = _ToolTip;
                    case VersionTypes.Short:
                        return _ass.GetName().Version.Major.ToString() + '.' + _ass.GetName().Version.Minor.ToString();
                    case VersionTypes.Medium:
                        return _ass.GetName().Version.Major.ToString() + '.' + _ass.GetName().Version.Minor.ToString() + '.' + _ass.GetName().Version.Build.ToString();
                    default:
                        return string.Empty;
                }

            }
            catch
            {
                return "0.12.22.22";
            }
        }


    }

    public static class DotNetVersion
    {

        public static string InstalledDotNetVersionsAsString()
        {
            string _result = string.Empty;
            try
            {
                foreach (Version _ver in InstalledDotNetVersions())
                {
                    _result = _result + ',' + _ver.ToString();
                }
            }
            catch { }
            return _result.Trim(',');
        }


        public static Collection<Version> InstalledDotNetVersions()
        {
            Collection<Version> versions = new Collection<Version>();
            RegistryKey NDPKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP", false);
            if (NDPKey != null)
            {
                string[] subkeys = NDPKey.GetSubKeyNames();
                foreach (string subkey in subkeys)
                {
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey, false), subkey, versions);
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey, false).OpenSubKey("Client", false), subkey, versions);
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey, false).OpenSubKey("Full", false), subkey, versions);
                }
            }
            return versions;
        }

        private static void GetDotNetVersion(RegistryKey parentKey, string subVersionName, Collection<Version> versions)
        {
            if (parentKey != null)
            {
                string installed = Convert.ToString(parentKey.GetValue("Install"));
                if (installed == "1")
                {
                    string version = Convert.ToString(parentKey.GetValue("Version"));
                    if (string.IsNullOrEmpty(version))
                    {
                        if (subVersionName.StartsWith("v"))
                            version = subVersionName.Substring(1);
                        else
                            version = subVersionName;
                    }

                    Version ver = new Version(version);

                    if (!versions.Contains(ver))
                        versions.Add(ver);
                }
            }
        }
    }
}
