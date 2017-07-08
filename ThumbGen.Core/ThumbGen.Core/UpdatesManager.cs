using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Xml;
using System.Threading;
using System.Reflection;
using Ionic.Zip;
using System.Windows;

namespace ThumbGen.Core
{
    internal class RemoteDomainItem
    {
        public string Host { get; private set; }
        public string Repository { get; private set; }
        public bool AlreadyTried { get; set; }

        public RemoteDomainItem(string host, string rep)
        {
            Host = host;
            Repository = rep;
            AlreadyTried = false;
        }
    }

    public class UpdatesManager
    {
        public UpdatesManager(bool isAutoUpdateCall, string host, string repository)
        {
            IsAutoUpdateCall = isAutoUpdateCall;
            AddRemoteDomain(host, repository);
        }

        public void AddRemoteDomain(string host, string repository)
        {
            m_Domains.Add(new RemoteDomainItem(host, repository));
        }

        public bool IsAutoUpdateCall { get; private set; }

        XmlDocument m_docVersionInfo = new XmlDocument();
        private string m_VersionInfoFile;

        public event EventHandler Processing;
        public event EventHandler Processed;

        private void SetProcessing()
        {
            if (Processing != null)
            {
                Processing(this, new EventArgs());
            }
        }

        private void SetProcessed()
        {
            if (Processed != null)
            {
                Processed(this, new EventArgs());
            }
        }

        private List<RemoteDomainItem> m_Domains = new List<RemoteDomainItem>(); 

        private RemoteDomainItem m_RemoteDomain = null;
        private RemoteDomainItem RemoteDomain
        {
            get
            {
                if (m_RemoteDomain == null)
                {
                    List<RemoteDomainItem> _tmp = new List<RemoteDomainItem>();
                    foreach (RemoteDomainItem _item in m_Domains)
                    {
                        if (!_item.AlreadyTried)
                        {
                            _tmp.Add(_item);
                        }
                    }
                    if (_tmp.Count != 0)
                    {
                        Random _rand = new Random();
                        m_RemoteDomain = _tmp[_rand.Next(0, _tmp.Count)];
                    }
                }
                return m_RemoteDomain;
            }
        }

        public void CheckUpdates()
        {
            CheckUpdates(-1);
        }

        public void CheckUpdates(int domainIndex)
        {
            if (domainIndex >= 0 && domainIndex < m_Domains.Count)
            {
                m_RemoteDomain = m_Domains[domainIndex];
            }
            else
            {
                m_RemoteDomain = null;
            }

            if (RemoteDomain == null)
            {
                // couldn't have success on any mirror
                if (!IsAutoUpdateCall)
                {
                    MessageBox.Show(String.Format("Unable to connect to the updates server.\n\nPlease try again later."), "Error during version check", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                SetProcessed();
                return;
            }

            RemoteDomain.AlreadyTried = true;

            //CheckInternetConnectivity.CheckResponse _response = CheckInternetConnectivity.CheckSiteStatus(RemoteDomain.Host.Replace("http://", String.Empty));
            //if(!_response.Online)
            //{
            //    m_RemoteDomain = null;
            //    CheckUpdates();
            //    return;
            //}

            try
            {
                IPHostEntry _inetServer = Dns.GetHostEntry(RemoteDomain.Host.Replace("http://", String.Empty));
            }
            catch
            {
                //MessageBox.Show("Unable to connect to the updates server.\nPlease check your internet connection and try again.", "Error during version check", MessageBoxButton.OK, MessageBoxImage.Error);
                //SetProcessed();
                CheckUpdates();
                return;
            }

            m_VersionInfoFile = Path.GetTempFileName();
            try
            {
                //string _versionFileUrl = string.Format("{0}/updates/versioninfo.xml", RemoteDomain);
                string _versionFileUrl = string.Format("{0}/versioninfo.xml", RemoteDomain.Repository);
                System.Net.WebClient _client = new System.Net.WebClient();
                _client.Proxy = null;
                _client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(client_DownloadVersionInfoFileCompleted);
                _client.DownloadFileAsync(new Uri(_versionFileUrl, UriKind.RelativeOrAbsolute), m_VersionInfoFile);
            }
            catch (Exception ex)
            {
                CleanupFilesAndSetProcessed();

                string errorDetails = String.Empty;
                MessageBoxImage iconsToShow = MessageBoxImage.Information;
                if (ex.Message.Contains("could not be resolved"))
                {
                    errorDetails = String.Format("Error looking up {0}.\nPlease check your internet connection and try again.", RemoteDomain);
                    iconsToShow = MessageBoxImage.Error;
                }
                else if (ex.Message.Contains("404"))
                {
                    errorDetails = "Upgrades are currently unavailable.\nPlease try again later.";
                    iconsToShow = MessageBoxImage.Information;
                }
                MessageBox.Show(String.Format("{0}", errorDetails), "Error downloading file", MessageBoxButton.OK, iconsToShow);
                return;

            }
        }

        private static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch { }
            }
        }

        private void CleanupFilesAndSetProcessed()
        {
            DeleteFile(m_VersionInfoFile);

            foreach (FileItem _file in m_Files)
            {
                DeleteFile(_file.FilePath);
            }

            SetProcessed();
        }

        private void client_DownloadVersionInfoFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {

            SetProcessed();

            if (e.Error == null && !e.Cancelled && File.Exists(m_VersionInfoFile))
            {
                try
                {
                    m_docVersionInfo.Load(m_VersionInfoFile);
                }
                catch
                {
                    //MessageBox.Show("Cannot check updates. Please retry later", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CheckUpdates();
                    return;
                }
                finally
                {
                    CleanupFilesAndSetProcessed();
                }

                Version _lastVersion = new Version(m_docVersionInfo.SelectSingleNode("//CurrentVersion/VersionNumber").FirstChild.InnerText);
                Version _currentVersion = new Version(VersionNumber.LongVersion);

                string _releaseNotes = GenericHelpers.GetValueFromXmlNode(m_docVersionInfo.SelectSingleNode("//CurrentVersion/ReleaseNotes"));
                _releaseNotes = string.IsNullOrEmpty(_releaseNotes) ? string.Empty : string.Format("Comment: {0}\n\n", _releaseNotes);

                if (_lastVersion.CompareTo(_currentVersion) > 0)
                {
                    MessageBoxResult _res = MessageBox.Show(string.Format("There is a new version available for download!\n\nNew version: {0} (you have {1})\n\n{2}Do you want to upgrade now?", _lastVersion, _currentVersion, _releaseNotes),
                                                "Update available", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    switch (_res)
                    {
                        case MessageBoxResult.Yes:
                            SetProcessing();
                            if (PrepareUpdate())
                            {
                                ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), m_Files);

                                if (m_Events.Count > 0)
                                {
                                    bool _b = false;
                                    while (!_b)
                                    {
                                        _b = true;
                                        foreach (WaitHandle _handle in m_Events)
                                        {
                                            bool _cb = _handle.WaitOne(50);
                                            if (!_cb)
                                            {
                                                _b = false;
                                            }
                                        }
                                        //_b = WaitHandle.WaitAll(m_Events.ToArray(), 50);
                                        GenericHelpers.DoEvents();
                                        Thread.Sleep(0);
                                    }

                                    string _currentBasePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                                    bool _doRestart = true;
                                    foreach (FileItem _file in m_Files)
                                    {
                                        if (File.Exists(_file.FilePath))
                                        {
                                            string _fn = Path.GetFileName(_file.FilePath);
                                            string _fnbak = Path.ChangeExtension(_fn, ".bak");
                                            string _fncurrent = Path.Combine(_currentBasePath, _fn);
                                            try
                                            {
                                                DeleteFile(_fnbak);
                                                if (File.Exists(_fncurrent))
                                                {
                                                    File.Move(_fncurrent, Path.Combine(_currentBasePath, _fnbak));
                                                }
                                                if (_file.IsZipped)
                                                {
                                                    using (ZipFile _zip = new ZipFile(_file.FilePath))
                                                    {
                                                        _zip.ExtractAll(Path.GetDirectoryName(_fncurrent), ExtractExistingFileAction.OverwriteSilently);
                                                    }
                                                }
                                                else
                                                {
                                                    File.Copy(_file.FilePath, _fncurrent);
                                                }
                                            }
                                            catch
                                            {
                                                _doRestart = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            _doRestart = false;
                                            break;
                                        }
                                    }

                                    if (!_doRestart)
                                    {
                                        CleanupFilesAndSetProcessed();
                                        MessageBox.Show("Error during updating current version. Please retry later.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        return;
                                    }

                                    CleanupFilesAndSetProcessed();

                                    MessageBox.Show("Upgrade successful! Please restart the application.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                    //System.Windows.Forms.Application.Restart();
                                    Application.Current.Shutdown();
                                }
                                else
                                {
                                    CleanupFilesAndSetProcessed();
                                }
                            }
                            else
                            {
                                CleanupFilesAndSetProcessed();
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if (!IsAutoUpdateCall)
                    {
                        MessageBox.Show("You have the latest version!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                CheckUpdates();
            }
            }
            catch (Exception ex)
            {
                Loggy.Logger.Error(ex.Message);
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DoWork(object param)
        {
            StartDownloads(param as List<FileItem>);
        }

        private List<ManualResetEvent> m_Events = new List<ManualResetEvent>();
        private List<FileItem> m_Files = new List<FileItem>();

        private struct FileItem
        {
            public ManualResetEvent Event;
            public string FilePath;
            public string RemoteFileName;
            public Uri Source;
            public bool IsZipped;
        }

        private void StartDownloads(List<FileItem> files)
        {
            foreach (FileItem _item in files)
            {
                System.Net.WebClient _client = new System.Net.WebClient();
                _client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(client_DownloadFileCompleted);
                _client.DownloadFileAsync(_item.Source, _item.FilePath, _item);
            }
        }

        public bool DownloadRemoteFile(string dest, string remoteFilename)
        {
            bool _result = false;
            System.Net.WebClient _client = new System.Net.WebClient();
            _client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(client_DownloadFileCompleted);
            _client.DownloadFile(new Uri(string.Format("{0}/{1}", RemoteDomain.Repository, remoteFilename), UriKind.RelativeOrAbsolute), dest);
            _result = File.Exists(dest);

            return _result;
        }

        private bool PrepareUpdate()
        {
            bool _result = true;
            m_Files.Clear();
            m_Events.Clear();

            try
            {
                XmlNodeList _filesToDownload = m_docVersionInfo.SelectNodes("//CurrentVersion/Files/File");
                foreach (XmlNode _node in _filesToDownload)
                {
                    string _filename = GenericHelpers.GetValueFromXmlNode(_node.SelectSingleNode("Name"));
                    if (string.IsNullOrEmpty(_filename))
                    {
                        continue;
                    }
                    string _remoteFilename = GenericHelpers.GetValueFromXmlNode(_node.SelectSingleNode("RemoteName"));
                    _remoteFilename = string.IsNullOrEmpty(_remoteFilename) ? _filename : _remoteFilename;
                    string _fileUrl = string.Format("{0}/{1}", RemoteDomain.Repository, _remoteFilename);
                    string _localTempFile = Path.Combine(Path.GetTempPath(), _filename);
                    string _isZipped = GenericHelpers.GetAttributeFromXmlNode(_node, "Zipped");

                    ManualResetEvent _event = new ManualResetEvent(false);

                    FileItem _fileItem = new FileItem();
                    _fileItem.FilePath = _localTempFile;
                    _fileItem.Source = new Uri(_fileUrl, UriKind.RelativeOrAbsolute);
                    _fileItem.RemoteFileName = _remoteFilename;
                    _fileItem.Event = _event;
                    _fileItem.IsZipped = string.IsNullOrEmpty(_isZipped) ? false : Boolean.Parse(_isZipped);

                    m_Events.Add(_event);
                    m_Files.Add(_fileItem);
                }
            }
            catch
            {
                _result = false;
            }
            return _result;
        }

        public static void ClearBakFiles()
        {
            string _currentBasePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string[] _baks = Directory.GetFiles(_currentBasePath, "*.bak", SearchOption.TopDirectoryOnly);
            foreach (string _file in _baks)
            {
                DeleteFile(_file);
            }
        }

        private void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            FileItem _file = (FileItem)e.UserState;
            if (e.Cancelled || e.Error != null)
            {
                DeleteFile(_file.FilePath); // remove it from temp in case of failure
            }
            _file.Event.Set();
        }
    }
}
