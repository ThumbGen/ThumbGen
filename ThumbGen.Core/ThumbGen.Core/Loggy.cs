using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Threading;
using Microsoft.Win32;
using System.Reflection;
using System.IO;
using System.Windows;

namespace ThumbGen.Core
{
  
    public class Loggy
    {
        LoggingConfiguration LogConfiguration;

        public static Logger Logger;

        static string GetSystemInfo(string appName)
        {
            return string.Format("{0} v{1} (Core: v{2}) OS: {3} .NET: {4} Culture: {5} UI: {6} Admin: {7} CPUCount: {8} CPUType: {9}" +
                                 "\r\n=======================", appName, ThumbGen.Core.VersionNumber.LongVersion, ThumbGen.Core.VersionNumber.CoreLongVersion,
                                 Environment.OSVersion.ToString(), DotNetVersion.InstalledDotNetVersionsAsString(),
                                 Thread.CurrentThread.CurrentCulture.DisplayName, Thread.CurrentThread.CurrentUICulture.DisplayName,
                                 IsUserAdministrator().ToString(), Environment.ProcessorCount.ToString(), GetCPUType());
        }

        public void InitLogging(string appName)
        {
            LogConfiguration = new LoggingConfiguration();
            Logger = LogManager.GetLogger("ThumbGenLogger");

            // Step 1. Create configuration object 
            //LoggingConfiguration config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            FileTarget fileTarget = new FileTarget();
            LogConfiguration.AddTarget("file", fileTarget);

            // Step 3. Set target properties 
            fileTarget.FileName = "${basedir}/" + Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".log";
            fileTarget.Layout = "${threadid}: ${longdate} [${callsite:className=false}] ${message} ${exception:format=tostring}";
            fileTarget.AutoFlush = true;
            fileTarget.DeleteOldFileOnStartup = true;
            fileTarget.Header = GetSystemInfo(appName);
            fileTarget.Footer = "=======================\r\nEnd log";

            // Step 4. Define rules 
            LoggingRule rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            LogConfiguration.LoggingRules.Add(rule2);

            // Step 5. Activate the configuration 
            LogManager.Configuration = LogConfiguration;
        }


        public static bool IsUserAdministrator()
        {
            //bool value to hold our return value
            bool isAdmin;
            try
            {
                //get the currently logged in user
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            return isAdmin;
        }

        public static string GetCPUType()
        {
            try
            {
                RegistryKey RegKey = Registry.LocalMachine;
                RegKey = RegKey.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
                Object cpuSpeed = RegKey.GetValue("~MHz");
                Object cpuType = RegKey.GetValue("VendorIdentifier");
                return string.Format("{0}@{1} MHz", cpuType, cpuSpeed);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
