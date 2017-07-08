using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Collections;
using System.Windows.Threading;
using FileExplorer.View;
using FileExplorer.ViewModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using System.Threading;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using NLog;
using System.Windows.Controls;
using Fluent;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Win32;
using ThumbGen.Core;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ThumbGenMainWindow : Window
    {
        public ThumbGenMainWindow()
        {
            InitializeComponent();
        }

        public override void EndInit()
        {
            base.EndInit();

            try
            {
                // preload the ssh scripts
                LoadSSHScripts();
                this.Loaded += new RoutedEventHandler(ThumbGenMainWindow_Loaded);
                this.Closing += new System.ComponentModel.CancelEventHandler(ThumbGenMainWindow_Closing);

                //setup the docking colors
                AvalonDock.ThemeFactory.ChangeTheme("aero.normalcolor");
                AvalonDock.ThemeFactory.ChangeColors(System.Windows.Media.Colors.Gray);
                AvalonDock.ThemeFactory.ChangeBrush(AvalonDock.AvalonDockBrushes.DockablePaneTitleForeground, System.Windows.Media.Brushes.Black);
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("MainWindow EndInit", ex);
            }
        }

        private void LoadSSHScripts()
        {
            // extract the scripts.xml if it NOT exists
            string _s = Path.Combine(FileManager.GetScriptsFolder(), "scripts.xml");
            if (!File.Exists(_s))
            {
                FileManager.ExtractPresetFile("scripts.xml", _s);
            }
            // dynamically create the Tools buttons
            try
            {
                XDocument _doc = XDocument.Load(_s);
                foreach (XElement _xe in _doc.XPathSelectElements("//script"))
                {
                    string _name = _xe.Attribute("name") != null ? _xe.Attribute("name").Value : "??";
                    string _desc = _xe.Attribute("description") != null ? _xe.Attribute("description").Value : "No description";
                    string _expect = "#";
                    try
                    {
                        _expect = _xe.Element("command").Attribute("prompt").Value;
                    }
                    catch (Exception ex)
                    {
                        Loggy.Logger.DebugException("element missing", ex);
                    }
                    string _method = "ssh";
                    try
                    {
                        _method = _xe.Element("command").Attribute("method").Value;
                    }
                    catch (Exception ex)
                    {
                        Loggy.Logger.DebugException("element missing", ex);
                    }
                    // create the button
                    Fluent.Button _btn = new Fluent.Button();
                    _btn.Text = _name;
                    _btn.ToolTip = new Fluent.ScreenTip() { Title = _name, Text = _desc };
                    // put the Dictionary<string> with the commands in the Tag
                    Dictionary<string, string> _commands = new Dictionary<string, string>();
                    foreach (XElement _line in _xe.XPathSelectElements("command/line"))
                    {
                        if (!string.IsNullOrEmpty(_line.Value))
                        {
                            _commands.Add(_line.Value, _expect);
                        }
                    }
                    _btn.Tag = _commands;
                    _btn.LargeIcon = new BitmapImage(new Uri("/images/batch32.png", UriKind.RelativeOrAbsolute));
                    if (_method == "ssh")
                    {
                        _btn.Click += new RoutedEventHandler(CustomScriptButtonSSH_Click);
                    }
                    else
                    {
                        _btn.Click += new RoutedEventHandler(CustomScriptButtonTelnet_Click);
                    }
                    // add the button to the ribbon
                    grpSSHTools.Items.Add(_btn);
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Cannot load ssh scripts", ex);
            }
        }

        void CustomScriptButtonSSH_Click(object sender, RoutedEventArgs e)
        {
            new SSHHelper().ExecuteCommands((sender as FrameworkElement).Tag as Dictionary<string, string>);
        }

        void CustomScriptButtonTelnet_Click(object sender, RoutedEventArgs e)
        {
            new TelnetHelper().ExecuteCommands((sender as FrameworkElement).Tag as Dictionary<string, string>);
        }

        void ThumbGenMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                this.DockManager.SaveLayout(FileManager.GetMainLayoutDefaultFilePath());
            }
            catch (Exception ex)
            {
                Loggy.Logger.Error("Exception while saving layout:" + ex.Message);
            }

            UpdateConfigurationData();
            // save current profile
            if (FileManager.ProfilesMan.SelectedProfile != null)
            {
                FileManager.Configuration.SaveConfiguration(FileManager.ProfilesMan.SelectedProfile.ProfilePath);
            }
            // remember the current selected profile
            FileManager.Configuration.StoreLastUsedProfile();
            // clean garbage
            FileManager.CleanupGarbageFiles();
            MP4Tagger.MP4Manager.ClearGarbage();
            //MovieSheets.MovieSheetsManager.ClearGarbage();
        }

        private void UpdateConfigurationData()
        {
            if (this.IsLoaded)
            {
                FileManager.Configuration.Options.Collectors = string.Empty;
                foreach (CollectorNode _collector in collectorsBox.Items)
                {
                    if (_collector.IsSelected)
                    {
                        FileManager.Configuration.Options.Collectors = FileManager.Configuration.Options.Collectors + ',' + _collector.Name;
                    }
                    if (_collector.IsPreferedInfoCollector)
                    {
                        FileManager.Configuration.Options.PreferedInfoCollector = _collector.Name;
                    }
                    if (_collector.IsPreferedCoverCollector)
                    {
                        FileManager.Configuration.Options.PreferedCoverCollector = _collector.Name;
                    }
                }

            }
        }

        void ThumbGenMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow.Title = string.Format("{0} {1}", "ThumbGen", VersionNumber.LongVersion);
                Collectors = new ObservableCollection<CollectorNode>();
                foreach (BaseCollector _bc in BaseCollector.MovieCollectors.Values)
                {
                    Collectors.Add(new CollectorNode(null, false, _bc));
                }

                collectorsBox.DataContext = Collectors;

                this.Dispatcher.BeginInvoke((System.Action)delegate
                {
                    UpdateCollectorsSelection(collectorsBox.Items);

                }, DispatcherPriority.Background);


                OverlayAdornerHelper.RemoveAllAdorners(this.DockManager);
            }

            finally
            {
                try
                {
                    App.SplashW.Close();
                    App.SplashW = null;
                }
                catch { }
                //try
                //{
                //    App.Splash.Close(TimeSpan.FromTicks(0));

                //}
                //catch
                //{
                //    // just in case...
                //}

            }

            if (FileManager.Configuration.Options.AutoCheckUpdates)
            {
                CheckForUpdates(true);
            }
        }

        private void UpdateCollectorsSelection(ItemCollection items)
        {
            // if the Collectors string is empty then select all
            if (!string.IsNullOrEmpty(FileManager.Configuration.Options.Collectors))
            {

                foreach (CollectorNode _collector in items)
                {
                    _collector.IsSelected = false;
                    if (string.Compare(_collector.Name, FileManager.Configuration.Options.PreferedInfoCollector, true) == 0)
                    {
                        _collector.IsPreferedInfoCollector = true;
                    }
                    else
                    {
                        _collector.IsPreferedInfoCollector = false;
                    }
                    if (string.Compare(_collector.Name, FileManager.Configuration.Options.PreferedCoverCollector, true) == 0)
                    {
                        _collector.IsPreferedCoverCollector = true;
                    }
                    else
                    {
                        _collector.IsPreferedCoverCollector = false;
                    }
                }

                string[] _checkedCollectors = FileManager.Configuration.Options.Collectors.Split(',');
                foreach (string _s in _checkedCollectors)
                {
                    if (!string.IsNullOrEmpty(_s))
                    {
                        foreach (CollectorNode _collector in collectorsBox.Items)
                        {
                            if (string.Compare(_collector.Name, _s, true) == 0)
                            {
                                _collector.IsSelected = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        internal ObservableCollection<CollectorNode> Collectors { get; set; }

        private void startbutton_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<CollectorNode> _selectedCollectors = FileManager.GetSelectedCollectors(Collectors);
            if (!_selectedCollectors.Any() && !FileManager.Configuration.Options.DisableSearch)
            {
                MessageBox.Show("Please select at least one provider for images.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GC.Collect();
            //GC.WaitForPendingFinalizers();

            ExplorerWindow _fileExplorer = new ExplorerWindow(this);
            ExplorerWindowViewModel _fileExplorerVM = new ExplorerWindowViewModel();
            _fileExplorer.DataContext = _fileExplorerVM;
            _fileExplorerVM.SelectPath(FileManager.Configuration.Options.LastSelectedFolder);

            bool? _res = _fileExplorer.ShowDialog();

            StartActionType _actionType = _fileExplorer.StartActionType;

            if (_res.HasValue && _res.Value)
            {
                switch (_actionType)
                {
                    case StartActionType.Process:
                    case StartActionType.ProcessAutomatic:
                    case StartActionType.ProcessSemiautomatic:
                    case StartActionType.ProcessFeelingLucky:
                    case StartActionType.GenerateRandomThumbs:

                        switch (_actionType)
                        {
                            case StartActionType.Process:
                                FileManager.Mode = ProcessingMode.Manual;
                                break;
                            case StartActionType.ProcessSemiautomatic:
                                FileManager.Mode = ProcessingMode.SemiAutomatic;
                                break;
                            case StartActionType.ProcessAutomatic:
                                FileManager.Mode = ProcessingMode.Automatic;
                                break;
                            case StartActionType.ProcessFeelingLucky:
                                FileManager.Mode = ProcessingMode.FeelingLucky;
                                break;
                        }

                        dcMovies.IsEnabled = false;
                        TheRibbon.IsEnabled = false;
                        try
                        {
                            IList<string> list = _fileExplorerVM.CollectSelectedFolders();

                            UpdateConfigurationData();

                            _fileExplorerVM.Dispose();
                            _fileExplorer.Close();
                            _fileExplorer = null;
                            GC.Collect();

                            if (list.Count != 0)
                            {
                                FileManager.ProcessMovies(_selectedCollectors as ObservableCollection<CollectorNode>, this, list, _actionType);
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("Nothing selected, nothing to do...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                        }
                        finally
                        {
                            dcMovies.IsEnabled = true;
                            TheRibbon.IsEnabled = true;
                        }
                        break;

                    case StartActionType.FixNetworkShares:
                    case StartActionType.UnfixNetworkShares:
                    case StartActionType.UpdateMoviesheetsTemplate:
                    case StartActionType.CreatePlaylist:
                    case StartActionType.GenerateDummyFile:

                        _fileExplorer.Close();
                        _fileExplorer = null;
                        GC.Collect();

                        IList<string> list2 = _fileExplorerVM.CollectSelectedFolders();

                        if (list2.Count != 0)
                        {
                            FileManager.ProcessMovies(_selectedCollectors as ObservableCollection<CollectorNode>, this, list2, _actionType);
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Nothing selected, nothing to do...", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        break;
                }
            }
        }

        private const string UPDATES_HOST = "http://thumbgen.org";

        private const string UPDATES_REPOSITORY = "http://thumbgen.org/updates";
        private void CheckForUpdates(bool isAutoUpdate)
        {
            UpdatesManager _updater = new UpdatesManager(isAutoUpdate, UPDATES_HOST, UPDATES_REPOSITORY);
            _updater.Processing += _update_Processing;
            _updater.Processed += _update_Processed;

            OverlayAdornerHelper _adorner = new OverlayAdornerHelper(this.DockManager, new LoadingScreen("Checking for updates...", false));
            Helpers.DoEvents();
            //btnUpdateButton.IsEnabled = false;
            _updater.CheckUpdates();
        }

        private void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            TheRibbon.IsBackstageOpen = false;

            CheckForUpdates(false);
        }

        void _update_Processed(object sender, EventArgs e)
        {
            dcMovies.IsEnabled = true;
            OverlayAdornerHelper.RemoveAllAdorners(this.DockManager);
            //UpdateButton.IsEnabled = true;
        }

        void _update_Processing(object sender, EventArgs e)
        {
            dcMovies.IsEnabled = false;
            OverlayAdornerHelper.RemoveAllAdorners(this.DockManager);
            OverlayAdornerHelper _adorner = new OverlayAdornerHelper(this.DockManager, new LoadingScreen("Performing update...", false));
            Helpers.DoEvents();
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string _testMovie = @"d:\Test\Inglorious Basterds [2009]\CD1 Inglorious Basterds.avi";
            string _ffmpeg = @"d:\Work\mtn-200808a-win32\mtn.exe";
            //VideoScreenShot.MakeThumbnail(_testMovie);

            double _duration = 0d; // MediaInfoManager.GetDurationMilliseconds(movieFilename);
            System.Windows.Size _size = new System.Windows.Size(); // MediaInfoManager.GetVideoResolution(movieFilename);
            MediaInfoManager.GetDurationAndVideoResolution(_testMovie, out _duration, out _size);

            int _cnt = 5;
            System.Windows.Size _thumbSize = FileManager.Configuration.Options.ThumbnailSize;
            double _rap = 1;
            if (_size.Width >= _size.Height)
            {
                _rap = _size.Width / _thumbSize.Width;
                _cnt = (int)Math.Round(_thumbSize.Height / (_size.Height / _rap));
            }
            else
            {
                _rap = _size.Height / _thumbSize.Height;
                _cnt = (int)Math.Round(_thumbSize.Width / (_size.Width / _rap));
            }

            //string _command = string.Format("{0} -i \"{1}\" -ss 0:0:5.0  -vframes 1  -vcodec png  -y -f      image2 frame.png",
            string _command = string.Format(" -o .tg.jpg -w {0} -t -c 1 -h 10 -r {1} -i -b 0.50 -D 12 -P \"{2}\"",
                    FileManager.Configuration.Options.ThumbnailSize.Width, _cnt, _testMovie);
            string _imageUrl = Path.ChangeExtension(Helpers.GetCorrectThumbnailPath(_testMovie, true), ".tg.jpg");

            try
            {
                ProcessStartInfo _pi = new ProcessStartInfo(_ffmpeg, _command);
                _pi.CreateNoWindow = true;
                _pi.UseShellExecute = false;
                Process.Start(_pi).WaitForExit(20000);


                if (File.Exists(_imageUrl))
                {
                    Helpers.CreateThumbnailImage(_imageUrl, Helpers.GetCorrectThumbnailPath(_testMovie, true), true, true, Helpers.ThumbnailSize, false, Helpers.MaxThumbnailFilesize);
                }
            }
            finally
            {
                if (File.Exists(_imageUrl))
                {
                    try
                    {
                        File.Delete(_imageUrl);
                    }
                    catch { }
                }
            }

            //MoviePlayer.Show(this, @"d:\Test\Inglorious Basterds [2009]\CD1 Inglorious Basterds.avi", null, new Size(0,0));
        }

        private void debugTemplateBtn_Click(object sender, RoutedEventArgs e)
        {
            DebugTemplateBox.Show(this);
        }

        private void clearSeriesCacheBtn_Click(object sender, RoutedEventArgs e)
        {
            CurrentSeriesHelper.Reset();
            GC.Collect();
        }

        public void ProfileSelector_OnProfileChanged(object sender, ProfileChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FileManager.Configuration.Options.Collectors))
            {
                UpdateCollectorsSelection(collectorsBox.Items);

                try
                {
                    Loggy.Logger.Debug("Profile changed to:");
                    Loggy.Logger.Debug(FileManager.Configuration.Options.Save());
                }
                catch { }
            }
        }

        public void ProfileSelector_OnProfileSaving(object sender, ProfileSavingEventArgs e)
        {
            UpdateConfigurationData();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            SplashWindow.ShowAbout(this);
        }

        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TheRibbon.IsBackstageOpen = false;
                this.Dispatcher.BeginInvoke((System.Action) delegate
                    {
                        Options.Show(this, FileManager.Configuration.Options);
                    });
            }
            catch(Exception ex)
            {
                Loggy.Logger.Error("Cannot show options:" + ex.Message);
            }
        }

        private void Twitter_Click(object sender, RoutedEventArgs e)
        {
            TheRibbon.IsBackstageOpen = false;
            Helpers.OpenUrlInBrowser("http://twitter.com/ThumbGen");
        }


        private void GotoWebsite_Click(object sender, RoutedEventArgs e)
        {
            TheRibbon.IsBackstageOpen = false;
            Helpers.OpenUrlInBrowser("http://thumbgen.org");

        }

        private void DockManager_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(FileManager.GetMainLayoutDefaultFilePath()))
            {
                try
                {
                    this.DockManager.RestoreLayout(FileManager.GetMainLayoutDefaultFilePath());
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("Cannot restore main layout", ex);
                    try
                    {
                        File.Delete(FileManager.GetMainLayoutDefaultFilePath());
                    }
                    catch { }
                }
            }
        }

        private void ExecuteTelnetSSHCommand(string prompt)
        {

            string _cmd = cmbSSHHistory.Text;
            if (!string.IsNullOrEmpty(_cmd))
            {
                if ((bool)cbSendMethod.IsChecked)
                {
                    new SSHHelper() { Prompt = prompt }.SendShellCommand(_cmd);
                }
                else
                {
                    new TelnetHelper().SendCommand(_cmd);
                }
                // add it to history
                if (!FileManager.Configuration.Options.SSHOptions.SSHHistory.Contains(_cmd))
                {
                    FileManager.Configuration.Options.SSHOptions.SSHHistory.Insert(0, _cmd);
                }
                // keep max 20 entries
                if (FileManager.Configuration.Options.SSHOptions.SSHHistory.Count > 15)
                {
                    FileManager.Configuration.Options.SSHOptions.SSHHistory.RemoveAt(15);
                }
                cmbSSHHistory.ItemsSource = FileManager.Configuration.Options.SSHOptions.SSHHistory;
            }
        }


        private void btnSSHSend_Click(object sender, RoutedEventArgs e)
        {
            ExecuteTelnetSSHCommand(tbPrompt.Text);
        }

        private void btnSSHReboot_Click(object sender, RoutedEventArgs e)
        {
            //new SSHHelper().SendReboot();
            new TelnetHelper().SendReboot();
        }

        private void cmbSSHHistory_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ExecuteTelnetSSHCommand(tbPrompt.Text);
            }
        }

        private void btnTelnetChangePass_Click(object sender, RoutedEventArgs e)
        {
            TelnetHelper.ChangePassword(this);
        }

        private void Facebook_Click(object sender, RoutedEventArgs e)
        {
            TheRibbon.IsBackstageOpen = false;
            Helpers.OpenUrlInBrowser("http://www.facebook.com/profile.php?id=100001472183409");
        }

        private void btnDesigner_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string _designerLocal = Path.Combine(FileManager.GetThumbGenFolder(), "ThumbGen.Designer.exe");
                if (File.Exists(_designerLocal))
                {
                    try
                    {
                        Process.Start(_designerLocal);
                    }
                    catch
                    {
                        Loggy.Logger.Log(LogLevel.Info, "Can't start Designer from " + _designerLocal);
                    }
                }
                else
                {
                    Helpers.OpenUrlInBrowser("http://thumbgen.org");
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.Error("Cannot start designer:" + ex.Message);
            }
        }

        private void btnBundlesManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new Bundles.BundlesManagerView { Owner = this }.ShowDialog();
            }
            catch (Exception ex)
            {
                Loggy.Logger.Error("Cannot start designer:" + ex.Message);
            }
        }

    }
}
