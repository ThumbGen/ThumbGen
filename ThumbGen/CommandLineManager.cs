using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using NDesk.Options;
using System.IO;
using System.Windows;

namespace ThumbGen
{
    public class CommandLineManager:IDisposable
    {
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);
        [DllImport("Kernel32.dll")]
        public static extern bool FreeConsole();

        private OptionSet m_OptionSet;

        private bool m_ShowHelp;
        private string m_ProfileName;

        private string[] m_Args;

        private static bool m_Connected;
        public static void InitConsole()
        {
            if (!m_Connected)
            {
                AttachConsole(-1);
                m_Connected = true;
            }
        }

        public void WriteWelcome()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("ThumbGen - @2009-2010 ~ Command line mode ~ NOT WORKING!");
        }

        public void WriteHelp()
        {
            WriteWelcome();
            Console.WriteLine("Usage: thumbgen [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            if (m_OptionSet != null)
            {
                m_OptionSet.WriteOptionDescriptions(Console.Out);
            }
            Console.WriteLine();
            Console.WriteLine("Hit Return to continue");
        }

        private string GetCmdArgs()
        {
            string _res = string.Empty;
            foreach (string _s in m_Args)
            { _res = string.Format("{0} {1}", _res, _s); }
            return _res;
        }

        public CommandLineManager(string[] args)
        {
            m_Args = args;

            Loggy.Logger.Debug("CmdLine: " + GetCmdArgs());

            InitConsole();
        }

        private bool ParseArgs()
        {
            List<string> _extra = new List<string>();

            if (m_Args != null)
            {
                // parse command line
                m_OptionSet = new OptionSet()
                {
                    { "h|?|help",  "Show this message and exit",  v => m_ShowHelp = v != null },
                    { "p=",  "Name of the profile to be used (if not specified the last used one will be selected; use -pdefault for the default one)",  v => m_ProfileName = v }
                };
                
                try
                {
                    _extra = m_OptionSet.Parse(m_Args);
                }
                catch (OptionException e)
                {
                    Console.WriteLine("ThumbGen: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `thumbgen --help' for more information.");
                    Application.Current.Shutdown(-1);
                }
            }
            return _extra.Count == 0 && m_Args.Length != 0;
        }

        public bool HasValidArgs()
        {
            return ParseArgs();
        }

        private void DoHelp()
        {
            if (m_ShowHelp)
            {
                WriteHelp();
            }
        }

        private void DoProfile()
        {
            if (!string.IsNullOrEmpty(m_ProfileName))
            {
                bool _isDefault = m_ProfileName == "default" || m_ProfileName == "<default>";
                string _newProfile = _isDefault ? Configuration.ConfigFilePath : Path.Combine(FileManager.GetProfilesFolder(), m_ProfileName);
                if (File.Exists(_newProfile))
                {
                    FileManager.ProfilesMan.SwitchProfiles(FileManager.ProfilesMan.SelectedProfile.ProfilePath, _newProfile);
                    try
                    {
                        Loggy.Logger.Debug("Profile used:");
                        Loggy.Logger.Debug(FileManager.Configuration.Options.Save());
                    }
                    catch { }
                }
            }
        }

        public void Process()
        {
            // process the command line arguments
            DoHelp();

            DoProfile();

            

        }

        public void Dispose()
        {
            FreeConsole();
        }
    }
}
