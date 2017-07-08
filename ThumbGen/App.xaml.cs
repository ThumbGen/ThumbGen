using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Resources;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using NLog.Config;
using NLog.Targets;
using NLog;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static SplashWindow SplashW { get; set; }

        public App()
        {

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // catch unhandled exception
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);

            ThumbGen.Core.AssemblyResolverHelper.Init();

            ThumbGen.Renderer.Engine.Initialize();

            base.OnStartup(e);

            DoStartup(e);
        }

        private void DoStartup(StartupEventArgs e)
        {
            FileManager.EnableMovieSheets = true;

            new ThumbGen.Core.Loggy().InitLogging("ThumbGen");

            FileManager.Configuration = new Configuration();

            FileManager.PrepareSatelliteFolders();

            FileManager.ExtractProfiles();

            FileManager.PrepareExternalFiles();

            FileManager.PopulateMyGalleryResults();

            FileManager.LoadLastUsedProfile();

            ThumbGen.Core.UpdatesManager.ClearBakFiles();

            CommandLineManager _cmdManager = new CommandLineManager(e.Args);
            if (_cmdManager.HasValidArgs())
            {
                /* there are some params -> do stuff without a GUI */
                _cmdManager.Process();

            }
            else
            {
                // do stuff with GUI

                SplashW = new SplashWindow();
                SplashW.Show();

                // important to reset MainWindow here as it is set to SplashWindow!!
                Application.Current.MainWindow = null;

                // IMPORTANT- must add the theming Fluent dictionary here, to the app.xaml (to be available in the whole app) 
                // can't be done directly in app.xaml coz the dll is not available at runtime
                // <ResourceDictionary Source="pack://application:,,,/Fluent;Component/Themes/Office2010/Silver.xaml"/>
                ResourceDictionary _resDict = new ResourceDictionary();
                _resDict.Source = new Uri("pack://application:,,,/Fluent;Component/Themes/Office2010/Silver.xaml", UriKind.RelativeOrAbsolute);
                this.Resources.MergedDictionaries.Add(_resDict);

                // trick needed to force loading the Fluent.dll when the mainwindow is not a RibbonWindow (weird...)
                Fluent.RibbonWindow _t = new Fluent.RibbonWindow();
                _t.Close();
                _t = null;

                // show the mainwindow
                new ThumbGenMainWindow().ShowDialog();

            }

            _cmdManager.Dispose();

            this.Shutdown();
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is ArgumentException)
            {
                // do nothing, invalid image most probably
                try
                {
                    Loggy.Logger.DebugException("unexpected ex: ", e.Exception);
                }
                catch { }
                MessageBox.Show("Image cannot be retrieved. Please report the movie (and the collector) to the ThumbGen author.", "Invalid format", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }
            else
            {
                string _details = e.Exception.ToString() + "\n" + (e.Exception.InnerException != null ? e.Exception.InnerException.Message : string.Empty);
                string _title = Application.Current != null && Application.Current.MainWindow != null ? Application.Current.MainWindow.Title : "Current Application";

                try
                {
                    Loggy.Logger.ErrorException("Got exception.", e.Exception);
                }
                catch { }

                MessageBox.Show("Please send ThumbGen.log to ThumbGen@gmail.com.\r\n\r\n" + _details, "Unexpected exception occured", MessageBoxButton.OK, MessageBoxImage.Error);

                if (Application.Current != null)
                {
                    Application.Current.Shutdown();
                }

            }
        }
    }

}
