using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Controls;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using NLog;
using System.Collections;
using Ionic.Zip;
using System.Configuration;
using System.Windows.Media;


namespace ThumbGen
{

    public enum StartActionType
    {
        Unknown,
        Process,
        ProcessAutomatic,
        ProcessSemiautomatic,
        ProcessFeelingLucky,
        FixNetworkShares,
        UnfixNetworkShares,
        GenerateRandomThumbs,
        UpdateMoviesheetsTemplate,
        CreatePlaylist,
        GenerateDummyFile
    }

    public enum MovieItemStatus
    {
        Unknown,
        Done,
        Skipped,
        SkippedExistingThumbnail,
        Querying,
        NotFound,
        MetadataMissing,
        Exception
    }


    public class MovieItem : DependencyObject, INotifyPropertyChanged
    {
        public string FilePath { get; private set; }
        public bool IsFolder { get; set; }

        private string m_Filename;
        public string Filename
        {
            get
            {
                if (string.IsNullOrEmpty(m_Filename))
                {
                    m_Filename = Path.GetFileName(this.FilePath);
                }
                return m_Filename;
            }
        }

        private string m_FilenameWithoutExtension;
        public string FilenameWithoutExtension
        {
            get
            {
                if (string.IsNullOrEmpty(m_FilenameWithoutExtension))
                {
                    m_FilenameWithoutExtension = Path.GetFileNameWithoutExtension(this.FilePath);
                }
                return m_FilenameWithoutExtension;
            }
        }

        public string DirectoryName
        {
            get
            {
                return Path.GetDirectoryName(this.FilePath);
            }
        }

        private string m_MovieFolderName;
        public string MovieFolderName
        {
            get
            {
                if (string.IsNullOrEmpty(m_MovieFolderName))
                {
                    m_MovieFolderName = Helpers.GetMovieFolderName(FilePath, FilenameWithoutExtension);
                }
                return m_MovieFolderName;
            }
        }

        private MovieItemStatus m_MovieItemStatus;
        public MovieItemStatus MovieItemStatus
        {
            get
            {
                return m_MovieItemStatus;
            }
            set
            {
                m_MovieItemStatus = value;
                NotifyPropertyChanged("MovieItemStatus");
            }
        }


        public override string ToString()
        {
            return this.FilePath;
        }

        public MovieItem(string filePath)
        {
            FilePath = filePath;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion
    }

    public class FileInfoComparer : IEqualityComparer<FileInfo>
    {
        #region IEqualityComparer<FileInfo> Members

        public bool Equals(FileInfo x, FileInfo y)
        {
            if (x != null && y != null)
            {
                return string.Compare(x.FullName, y.FullName, true) == 0;
            }
            else
            {
                return false;
            }
        }

        public int GetHashCode(FileInfo obj)
        {
            return obj != null ? obj.GetHashCode() : 0;
        }

        #endregion
    }

    public class FilesCollector
    {
        public IEnumerable<FileInfo> CollectFiles(string path, bool recurseSubfolders, IEnumerable<string> extensionsToCollect)
        {
            return CollectFiles(path, recurseSubfolders, extensionsToCollect, true);
        }

        public IEnumerable<FileInfo> CollectFiles(string path, bool recurseSubfolders, IEnumerable<string> extensionsToCollect, bool ignoreFilter)
        {
            List<FileInfo> _matchingFiles = new List<FileInfo>();

            try
            {
                //var files = from f in Directory.GetFiles(path, "*", recurseSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                //select new FileInfo(f);
                List<FileInfo> files = new List<FileInfo>();
                FindFile(path, "*.*", files, recurseSubfolders, ignoreFilter, extensionsToCollect);

                foreach (FileInfo _fi in files)
                {
                    // check if it is a dummy file
                    if (FileManager.IsDummyFile(_fi.FullName))
                    {
                        continue;
                    }

                    // filter out if it exists already
                    if (!_matchingFiles.Contains(_fi, new FileInfoComparer()) &&
                        (extensionsToCollect != null && extensionsToCollect.Count() != 0 && extensionsToCollect.Contains("*" + _fi.Extension.ToLowerInvariant())))
                    {
                        // check also filtering!
                        bool _skip = false;
                        UserOptions _options = FileManager.Configuration.Options;
                        if (!ignoreFilter && _options.FileBrowserOptions.IsFilterActive())
                        {
                            _skip = (_options.FileBrowserOptions.FilterWithoutMoviesheet && FileManager.Configuration.HasMoviesheet(_fi.FullName)) ||
                                    (_options.FileBrowserOptions.FilterWithoutExtraMoviesheet && FileManager.Configuration.HasExtraMoviesheet(_fi.FullName)) ||
                                    (_options.FileBrowserOptions.FilterWithoutMovieInfo && nfoHelper.HasMovieInfoFile(_fi.FullName)) ||
                                    (_options.FileBrowserOptions.FilterWithoutExtSubtitles && MediaInfoManager.HasExternalSubtitles(_fi.FullName)) ||
                                    (_options.FileBrowserOptions.FilterWithoutThumbnail && FileManager.Configuration.HasThumbnail(_fi.FullName)) ||
                                    (_options.FileBrowserOptions.FilterWithoutMetadata && FileManager.Configuration.HasMoviesheetMetadata(_fi.FullName)) ||
                                    (_options.FileBrowserOptions.FilterWithoutFolderJpg && FileManager.Configuration.HasFolderJpg(_fi.FullName));
                        }
                        if (!_skip)
                        {
                            _matchingFiles.Add(_fi);
                        }
                    }
                }

                //_matchingFiles = Sort<FileInfo>(_matchingFiles).ToList<FileInfo>();
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException(string.Format("Exception collecting files from {0}:", path), ex);
            }

            return _matchingFiles;
        }

        private void FindFile(string dir, string file, List<FileInfo> items, bool recurseSubfolders, bool ignoreFilter, IEnumerable<string> extensionsToCollect)
        {
            DirectoryInfo di = new DirectoryInfo(dir);
            try
            {
                // do not process the folder if it contains strings to be skipped
                if (FileManager.Configuration.HasFolderStringToBeSkipped(di.Name) || Configuration.NeedsToExcludeStartingWithDot(di.Name))
                {
                    return;
                }
                // check if it is a bluray path and if yes and BRay filter (biggest m2ts only) is activated then apply it
                if (FileManager.Configuration.Options.FileBrowserOptions.UseBRayFilter && Helpers.IsBlurayPath(dir))
                {
                    // detect the biggest m2ts file in this dir and select it (discard the other m2ts files)
                    FileInfo[] _tmpFiles = di.GetFiles("*.m2ts", SearchOption.TopDirectoryOnly);
                    FileInfo _winnerFile = null;
                    double _maxSize = 0;
                    foreach (FileInfo _fi in _tmpFiles)
                    {
                        if (_fi.Length > _maxSize)
                        {
                            _winnerFile = _fi;
                            _maxSize = _fi.Length;
                        }
                    }
                    if (_winnerFile != null)
                    {
                        // add the winner
                        items.Add(_winnerFile);
                        // jump out to avoid adding the other m2ts files
                        return;
                    }
                }


                DirectoryInfo[] subs = di.GetDirectories();
                //Array.Sort(subs, new Comparison<DirectoryInfo>(delegate(DirectoryInfo d1, DirectoryInfo d2)
                //    {
                //        return string.Compare(d1.Name, d2.Name);
                //    }));
                subs = Sort<DirectoryInfo>(subs).ToArray<DirectoryInfo>();

                if (recurseSubfolders)
                {
                    foreach (DirectoryInfo sub in subs)
                        FindFile(sub.FullName, file, items, recurseSubfolders, ignoreFilter, extensionsToCollect);
                }
                FileInfo[] files = di.GetFiles(file);
                //Array.Sort(files, new Comparison<FileInfo>(delegate(FileInfo f1, FileInfo f2)
                //    {
                //        return string.Compare(f1.Name, f2.Name);
                //    }));

                files = Sort<FileInfo>(files).ToArray<FileInfo>();

                foreach (FileInfo info in files)
                {
                    //skip HERE excluded names for files and maybe also folders
                    if (!FileManager.Configuration.HasFolderStringToBeSkipped(info.Name))
                    {
                        if (!ignoreFilter && FileManager.Configuration.Options.FileBrowserOptions.FilterOnlyFirstMovieInFolder)
                        {
                            // there is the OnlyFirstMovieInFolder filter active so DO NOT add any other file than first movie file
                            if (extensionsToCollect.Contains("*" + info.Extension.ToLowerInvariant()))
                            {
                                // add first movie found and JUMP OUT
                                items.Add(info);
                                break;
                            }
                        }
                        else
                        {
                            items.Add(info);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException(string.Format("Exception finding files from {0}:", dir), ex);
            }
        }

        public List<string> MoviesToCollect = new List<string>() 
                        {"*.avi", "*.mkv", "*.mpg", "*.mpeg", "*.wmv9", "*.divx", "*.m2t", "*.m2ts", "*.m2v", "*.mov", "*.rmvb",
                         "*.movie", "*.mp4", "*.mts", "*.ts", "*.tp", "*.wm", "*.wmv", "*.iso", "*.m4v", "*.ifo"};


        public IEnumerable<FileInfo> CollectFiles(string path, bool recurseSubfolders)
        {
            return CollectFiles(path, recurseSubfolders, true);
        }

        public IEnumerable<FileInfo> CollectFiles(string path, bool recurseSubfolders, bool ignoreFilter)
        {
            List<string> _movies = new List<string>(MoviesToCollect);

            try
            {
                // append also the custom extensions if any
                if (!string.IsNullOrEmpty(FileManager.Configuration.Options.CustomMovieExtensions))
                {
                    string[] _exts = FileManager.Configuration.Options.CustomMovieExtensions.ToLowerInvariant().Split(',');
                    if (_exts != null && _exts.Count() != 0)
                    {
                        foreach (string _s in _exts)
                        {
                            if (!string.IsNullOrEmpty(Path.GetExtension(_s)) && !_movies.Contains(_s))
                            {
                                _movies.Add(string.Format("*{0}", _s));
                            }
                        }
                    }
                }
            }
            catch
            {
                Loggy.Logger.Debug(string.Format("Exception collecting files from {1}", path));
            }

            return CollectFiles(path, recurseSubfolders, _movies, ignoreFilter);
        }

        public IEnumerable<DirectoryInfo> CollectFolders(string path, bool recurseSubfolders)
        {
            return CollectFolders(path, recurseSubfolders, true);
        }

        public IEnumerable<DirectoryInfo> CollectFolders(string path, bool recurseSubfolders, bool ignoreFilter)
        {
            if (ThumbGen.Helpers.IsDirectory(path))
            {
                List<DirectoryInfo> _res = new List<DirectoryInfo>();

                string[] _dirs = null;
                try
                {
                    _dirs = Directory.GetDirectories(path);
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("Collect folders:", ex);
                }
                if (_dirs != null && _dirs.Count() != 0)
                {
                    //Array.Sort(_dirs);
                    foreach (string _path in _dirs)
                    {
                        DirectoryInfo di = new DirectoryInfo(_path);
                        if (!FileManager.Configuration.HasFolderStringToBeSkipped(_path) && !Configuration.NeedsToExcludeStartingWithDot(di.Name))
                        {
                            _res.Add(di);
                        }
                    }
                }

                _res = Sort<DirectoryInfo>(_res).ToList<DirectoryInfo>();

                return _res;
            }
            else
            {
                return null;
            }
        }

        private IEnumerable<T> Sort<T>(IEnumerable<T> item) where T : FileSystemInfo
        {
            IEnumerable<T> _res = new List<T>();

            ThumbGen.UserOptions.SortOption _sortOption = FileManager.Configuration.Options.FileBrowserOptions.Sorting;
            bool _isAscending = FileManager.Configuration.Options.FileBrowserOptions.IsSortingAscending;

            switch (_sortOption)
            {
                case UserOptions.SortOption.Alphabetically:
                default:
                    _res = _isAscending ? _res = item.OrderBy(f => f.Name).ToList<T>() :
                                          _res = item.OrderByDescending(f => f.Name).ToList<T>();
                    break;
                case UserOptions.SortOption.Date:
                    _res = _isAscending ? _res = item.OrderBy(f => f.LastWriteTimeUtc).ToList<T>() :
                                          _res = item.OrderByDescending(f => f.LastWriteTimeUtc).ToList<T>();
                    break;
            }

            return _res;
        }

        public FilesCollector()
        {

        }
    }


    public enum ProcessingMode
    {
        Manual,  // fully user controlled
        SemiAutomatic, // same as manual but with timer for the default actions
        Automatic, // the ResultsPage is not shown at all but user is prompted to select movie from IMDb
        FeelingLucky // no GUI at all, full automatic, app is choosing the best matches
    }

    public class FileManager
    {
        public FileManager()
        {

        }

        public static List<string> GarbageFiles = new List<string>();

        private static object garbageLock = new object();

        public static void AddToGarbageFiles(string file)
        {
            lock (garbageLock)
            {
                GarbageFiles.Add(file);
            }
        }

        public static ProfilesManager ProfilesMan = new ProfilesManager();

        public static void CleanupGarbageFiles()
        {
            if (GarbageFiles.Count != 0)
            {
                foreach (string _item in GarbageFiles)
                {
                    try
                    {
                        File.Delete(_item);
                    }
                    catch { }
                }
            }
        }

        public static string THUMBGEN_TEMP = "_thumbgen_tmp";

        public static bool DisableOpenSubtitles = false;
        public static bool DisableMediaInfo = false;
        public static bool DisableKhedasFix = false;
        public static bool EnableMovieSheets = false;

        //public static bool AutomaticMode = false;
        //public static bool SemiautomaticMode = false;

        public static ProcessingMode Mode = ProcessingMode.Manual;

        public static SolidColorBrush BackstageBrush = Brushes.Goldenrod;
        public static bool CancellationPending;

        private static System.Windows.Controls.ProgressBar m_Progress;
        private static TextBlock m_ProgressText;

        private static bool m_OverwriteExisting;

        public static AllProvidersCollector CurrentCollector { get; private set; }
        private static ObservableCollection<MovieItem> Movies = new ObservableCollection<MovieItem>();

        //public static Configuration Configuration = new Configuration();
        public static Configuration Configuration;

        public static void SetMovieItemStatus(MovieItem item, MovieItemStatus status)
        {
            try
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    if (item != null)
                    {
                        item.MovieItemStatus = status;
                        try
                        {
                            (Application.Current.MainWindow as ThumbGenMainWindow).progressListBox.ScrollIntoViewCentered(item);
                        }
                        catch { }
                    }
                });
            }
            catch { }
        }

        public static IEnumerable<CollectorNode> GetSelectedCollectors(IEnumerable<CollectorNode> collectors)
        {
            ObservableCollection<CollectorNode> _result = new ObservableCollection<CollectorNode>();

            if (collectors.Count() != 0)
            {
                foreach (CollectorNode _node in collectors)
                {
                    if (_node.IsSelected)
                    {
                        _result.Add(_node);
                    }
                }
            }

            return _result;
        }

        public static void PrepareSatelliteFolders()
        {
            // prepare satellites directories
            Directory.CreateDirectory(GetTemplatesFolder());
            Directory.CreateDirectory(GetGalleryFolder());
            Directory.CreateDirectory(GetProfilesFolder());
            Directory.CreateDirectory(GetMovieLayoutsFolder());
            Directory.CreateDirectory(GetMainLayoutsFolder());
            Directory.CreateDirectory(GetScriptsFolder());
            CleanTempFolder(); // remove any orphan temp files
        }

        public static void CleanTempFolder()
        {
            string _tgtmp = Path.Combine(Path.GetTempPath(), THUMBGEN_TEMP);
            if (Directory.Exists(_tgtmp))
            {
                try
                {
                    Directory.Delete(_tgtmp, true);
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("CleanTempFolder:", ex);
                }
            }
            Directory.CreateDirectory(_tgtmp);
        }

        public static string GetGalleryFolder()
        {
            return Path.Combine(GetThumbGenFolder(), "Gallery");
        }

        public static string GetTemplatesFolder()
        {
            return Path.Combine(GetThumbGenFolder(), "Templates");
        }

        public static string GetThumbGenFolder()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public static string GetProfilesFolder()
        {
            return Path.Combine(GetThumbGenFolder(), "Profiles");
        }

        public static string GetMovieLayoutsFolder()
        {
            return Path.Combine(GetThumbGenFolder(), "Layouts\\MovieResults");
        }

        public static string GetMainLayoutsFolder()
        {
            return Path.Combine(GetThumbGenFolder(), "Layouts\\MainPage");
        }

        public static string GetMainLayoutDefaultFilePath()
        {
            return Path.Combine(GetMainLayoutsFolder(), "default.tgl");
        }

        public static string GetScriptsFolder()
        {
            return Path.Combine(GetThumbGenFolder(), "Scripts");
        }

        public static void PopulateMyGalleryResults()
        {
            ResultsListBox.MyGalleryResults = new ObservableCollection<ResultItemBase>();
            List<FileInfo> _temp = new FilesCollector().CollectFiles(FileManager.GetGalleryFolder(), false, ResultsListBox.GalleryImagesSupported).ToList<FileInfo>();
            if (_temp != null && _temp.Count != 0)
            {
                foreach (FileInfo _info in _temp)
                {
                    ResultMovieItem _titem = new ResultMovieItem(null, Path.GetFileNameWithoutExtension(_info.Name), _info.FullName, BaseCollector.GALLERY);
                    ResultsListBox.MyGalleryResults.Add(_titem);
                }
            }
        }

        public static string ExtractPresetFile(string fileName, string destFile)
        {
            string _tempPath = string.IsNullOrEmpty(destFile) ? Helpers.GetUniqueFilename(Path.GetExtension(fileName)) : destFile;
            if (Helpers.GetEmbeddedPreset(fileName, _tempPath))
            {
                return _tempPath;
            }
            else
            {
                return null;
            }
        }

        public static bool ExtractDefaultTemplate()
        {
            bool _result = false;
            try
            {
                if (!Directory.Exists(Path.Combine(FileManager.GetTemplatesFolder(), "_Default ThumbGen Template_")))
                {
                    string _path = ExtractPresetFile("_Default ThumbGen Template_.zip", null);
                    try
                    {
                        if (!string.IsNullOrEmpty(_path))
                        {
                            using (ZipFile _zip = new ZipFile(_path))
                            {
                                _zip.ExtractAll(FileManager.GetTemplatesFolder());
                                _result = true;
                            }
                        }
                    }
                    finally
                    {
                        File.Delete(_path);
                    }
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("ExtractTemplate:", ex);
            }

            return _result;
        }

        private static void ExtractProfile(string filename)
        {
            string _s = Path.Combine(FileManager.GetProfilesFolder(), filename);
            if (!File.Exists(_s))
            {
                FileManager.ExtractPresetFile(filename, _s);
            }
        }

        public static void ExtractProfiles()
        {
            // if required extract the predefined profiles
            ExtractProfile("ACRyan POHD.tgp");

            ExtractProfile("WDTVLive Movies Sheet.tgp");
            ExtractProfile("WDTVLive TV Shows Sheet.tgp");

            ExtractProfile("Xtreamer.tgp");
            ExtractProfile("Xtreamer TV Shows.tgp");
            ExtractProfile("Xtreamer TG_Sheets.tgp");
        }

        public static void LoadLastUsedProfile()
        {
            // refresh profiles list
            FileManager.ProfilesMan.RefreshProfiles(FileManager.Configuration.GetLastUsedProfile().ProfileName);
            if (FileManager.ProfilesMan.SelectedProfile != null)
            {
                // load configuration
                FileManager.Configuration.LoadConfiguration(FileManager.ProfilesMan.SelectedProfile.ProfilePath);
            }

            Loggy.Logger.Debug("Config loaded");
            try
            {
                Loggy.Logger.Debug(FileManager.Configuration.Options.Save());
            }
            catch { }

        }

        public static bool ExtractModule(string embeddedFileName, string targetFileName)
        {
            string _dllPath = Path.Combine(GetThumbGenFolder(), targetFileName);
            return Helpers.GetEmbeddedAssembly(embeddedFileName, _dllPath);
        }

        public static void PrepareExternalFiles()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
                        {
                            Helpers.RemoveFile(Path.Combine(GetThumbGenFolder(), "7z.dll"));

                            // check if MediaInfo.dll is present and if not disable completely its usage
                            ExtractModule("MediaInfo.dll", "MediaInfo.dll");

                            // check if libmp4v2.dll and MP4V2Wrapper.dll are present and if not disable completely its usage
                            ExtractModule("libmp4v2.dll", "libmp4v2.dll");

                            if (!FileManager.DisableKhedasFix)
                            {
                                ExtractModule("MP4V2Wrapper.dll", "MP4V2Wrapper.dll");
                            }

                            // if required extract the default (embedded) template (must do it here as it needs unzip)
                            FileManager.ExtractDefaultTemplate();

                        }, DispatcherPriority.Background);
            MP4Tagger.MP4Manager.Prepare();
        }

        public static void HideAdorners()
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    try
                    {
                        if (AdornedElement != null)
                        {

                            OverlayAdornerHelper.RemoveAllAdorners(AdornedElement);
                            Helpers.DoEvents();
                        }
                    }
                    catch { }
                });
            }
        }

        public static void ShowAdorner(string text, bool showCancelButton)
        {
            HideAdorners();
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    try
                    {
                        OverlayAdornerHelper _adornerHelper = new OverlayAdornerHelper(AdornedElement, new LoadingScreen(text, showCancelButton));
                        Helpers.DoEvents();
                    }
                    catch { }
                });
            }
        }

        public static MovieItem GetMovieByFilePath(string movieFilename)
        {
            MovieItem _result = null;
            if (Movies != null && Movies.Count != 0)
            {
                foreach (MovieItem _item in Movies)
                {
                    if (string.Compare(_item.FilePath, movieFilename, true) == 0)
                    {
                        _result = _item;
                        break;
                    }
                }
            }
            return _result;
        }

        public static bool IsDummyFile(string moviepath)
        {
            // check if the first chars inside the file are DUMMY_SIG
            bool _result = false;

            if (FileManager.Configuration.Options.GenerateDummyFile)
            {
                try
                {
                    using (FileStream _stream = new FileStream(moviepath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[1024];
                        int read = _stream.Read(buffer, 0, DUMMY_SIG.Length);
                        Array.Resize(ref buffer, read);
                        _result = read == DUMMY_SIG.Length && (System.Text.Encoding.UTF8.GetString(buffer).ToLowerInvariant() == DUMMY_SIG.ToLowerInvariant());
                    }
                }
                catch { }
            }

            return _result;
        }

        private static string DUMMY_SIG = "ThumbGenDummy";

        public static void ProcessMovies(ObservableCollection<CollectorNode> selectedCollectors,
                                         ThumbGenMainWindow mainWindow, IList<string> rootPaths,
                                         StartActionType actionType)
        {
            CurrentSeriesHelper.Reset();

            FolderToSkip = null;
            FolderCompleteSkipped = false;

            if (actionType == StartActionType.Unknown)
            {
                return;
            }
            m_OverwriteExisting = Configuration.Options.OverwriteExistingThumbs;
            m_Progress = mainWindow.progressBar1;
            m_ProgressText = mainWindow.textBlock1;
            Movies.Clear();
            mainWindow.progressListBox.DataContext = Movies;
            //(mainWindow.FindResource("MoviesCollectionView") as CollectionViewSource).Source = Movies;

            bool _aborted = false;

            m_ProgressText.Text = "Collecting movies...";
            try
            {
                ShowAdorner("Collecting movies...", false);
                Helpers.DoEvents();

                List<FileInfo> _files = null;
                if (rootPaths.Count != 0)
                {
                    mainWindow.Dispatcher.Invoke((Action)delegate
                    {
                        FilesCollector _fc = new FilesCollector();
                        _files = new List<FileInfo>();
                        // new approach, scan the given folders fully
                        foreach (string _item in rootPaths)
                        {
                            if (Directory.Exists(_item))
                            {
                                // collect all movies
                                List<FileInfo> _tmp = _fc.CollectFiles(_item, true, false).ToList<FileInfo>();

                                if (_tmp.Count != 0)
                                {
                                    _files.AddRange(_tmp);
                                }
                            }
                            else
                            {
                                if (File.Exists(_item))
                                {
                                    _files.Add(new FileInfo(_item));
                                }
                            }
                        }
                    }, DispatcherPriority.Background);
                }
                else
                {
                    return;
                }

                switch (actionType)
                {
                    case StartActionType.Process:
                    case StartActionType.ProcessAutomatic:
                    case StartActionType.ProcessSemiautomatic:
                    case StartActionType.ProcessFeelingLucky:
                    case StartActionType.GenerateRandomThumbs:
                        foreach (FileInfo _file in _files)
                        {
                            Movies.Add(new MovieItem(_file.FullName));
                            Thread.Sleep(0);
                        }
                        break;
                    case StartActionType.FixNetworkShares:
                        MP4Tagger.MP4Manager.ApplyBatchFix(_files);
                        break;
                    case StartActionType.UnfixNetworkShares:
                        MP4Tagger.MP4Manager.ApplyBatchUnFix(_files);
                        break;
                    case StartActionType.GenerateDummyFile:
                        FileManager.GenerateDummyFiles(_files);
                        break;
                    case StartActionType.UpdateMoviesheetsTemplate:
                        foreach (FileInfo _file in _files)
                        {
                            Movies.Add(new MovieItem(_file.FullName));
                            Thread.Sleep(0);
                        }
                        MoviesheetsUpdateManager.ApplyNewTemplate(_files, MoviesheetsUpdateManager.SelectedTemplates);
                        break;
                    case StartActionType.CreatePlaylist:
                        foreach (FileInfo _file in _files)
                        {
                            Movies.Add(new MovieItem(_file.FullName));
                            Thread.Sleep(0);
                        }

                        ShowAdorner("Generating playlists... Please wait...", false);
                        using (Playlists.PlaylistManager _manager = new Playlists.PlaylistManager())
                        {
                            _manager.CreatePlaylists(_files, FileManager.Configuration.Options.PlaylistsJobs);
                        }

                        break;
                }
            }
            finally
            {
                HideAdorners();
            }
            if (Movies != null && Movies.Count != 0 &&
                (actionType == StartActionType.Process ||
                 actionType == StartActionType.ProcessAutomatic ||
                 actionType == StartActionType.ProcessSemiautomatic ||
                 actionType == StartActionType.ProcessFeelingLucky ||
                 actionType == StartActionType.GenerateRandomThumbs))
            {
                m_Progress.Minimum = 0;
                m_Progress.Maximum = Movies.Count;
                m_Progress.Value = 0;

                List<BaseCollector> _collectors = new List<BaseCollector>();
                if (actionType != StartActionType.GenerateRandomThumbs)
                {
                    foreach (CollectorNode _node in selectedCollectors)
                    {
                        if (_node.Collector != null)
                        {
                            // if it is a XMLImportCollectorBase derived collector then call Load
                            XMLImportCollectorBase _xmlCol = _node.Collector as XMLImportCollectorBase;
                            if (_xmlCol != null)
                            {
                                _xmlCol.Load();
                            }

                            // check if online
                            try
                            {
                                IPHostEntry _inetServer = Dns.GetHostEntry(_node.Collector.Host.Replace("http://", String.Empty));
                                // add it if online
                                _collectors.Add(_node.Collector);
                            }
                            catch
                            {
                                MessageBox.Show(String.Format("Unable to connect to {0}.\n\nThe {1} collector will be disabled for this session.", _node.Collector.Host, _node.Collector.CollectorName), "Connection error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                _node.IsSelected = false; // disable the collector
                            }

                        }
                    }
                }
                else
                {
                    //_collectors.Add(new RandomSnapshotsCollector());
                }
                CurrentCollector = new AllProvidersCollector(_collectors);
                CurrentCollector.MainWindow = mainWindow;

                if (CurrentCollector != null)
                {
                    CurrentCollector.Processing += new EventHandler(_collector_Processing);
                    CurrentCollector.Processed += new EventHandler(_collector_Processed);
                    CurrentCollector.ThumbnailCreated += new EventHandler(_collector_ThumbnailCreated);
                }

                _aborted = DoProcessMovies(Movies, mainWindow, actionType);
            }

            CurrentSeriesHelper.Reset();

            m_Progress.Value = 0;

            if (!_aborted && (actionType == StartActionType.ProcessAutomatic || actionType == StartActionType.ProcessFeelingLucky))
            {
                // chkeck if there are "Not found" movies and ask user maybe he wants to reprocess them manually
                var _notFoundMovies = from c in Movies
                                      where c.MovieItemStatus == MovieItemStatus.NotFound || c.MovieItemStatus == MovieItemStatus.Exception
                                      select c;
                if (_notFoundMovies != null && _notFoundMovies.Count() != 0)
                {
                    if (MessageBox.Show(string.Format("Not found: {0} movie(s)\r\n\r\nWould you like to manually process them?", _notFoundMovies.Count()),
                                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                    {
                        ObservableCollection<MovieItem> _movies = new ObservableCollection<MovieItem>(_notFoundMovies);
                        FileManager.Mode = ProcessingMode.Manual;
                        _aborted = DoProcessMovies(_movies, mainWindow, StartActionType.Process);
                    }
                }
            }

            if (!_aborted)
            {
                m_ProgressText.Text = "Done.";
            }

            CurrentSeriesHelper.Reset();

            m_Progress.Value = 0;
        }

        private static bool DoProcessMovies(ObservableCollection<MovieItem> movies, ThumbGenMainWindow mainWindow, StartActionType actionType)
        {
            bool _aborted = false;

            foreach (MovieItem _item in movies)
            {
                if (FolderToSkip != null)
                {
                    if (string.Compare(FolderToSkip, Path.GetDirectoryName(_item.FilePath), true) != 0)
                    {
                        FolderToSkip = null;
                        FolderCompleteSkipped = false;
                    }
                    else
                    {
                        if (FolderCompleteSkipped)
                        {
                            continue;
                        }
                    }
                }
                if (!ProcessMovie(_item, actionType))
                {
                    m_ProgressText.Text = "Aborted by the user.";
                    _item.MovieItemStatus = MovieItemStatus.Unknown;
                    _aborted = true;
                    break;
                }

                //generate the dummy file for HUB (if required)
                FileManager.GenerateDummyFile(_item.FilePath);

                mainWindow.progressListBox.ScrollIntoViewCentered(_item);
                Thread.Sleep(0);
            }

            return _aborted;
        }

        public static void GenerateDummyFile(string moviepath)
        {
            //generate the dummy file for HUB
            if (FileManager.Configuration.Options.GenerateDummyFile)
            {
                string _path = FileManager.Configuration.GetDummyFilePath(moviepath);
                if (!string.IsNullOrEmpty(_path))
                {
                    try
                    {
                        if (!File.Exists(_path))
                        {
                            File.WriteAllText(_path, DUMMY_SIG + " (disable it in Options)\r\n" + Path.GetFileName(moviepath));
                        }
                    }
                    catch (Exception ex)
                    {
                        Loggy.Logger.LogException(LogLevel.Error, "GenerateDummyFile", ex);
                    }
                }
            }
        }

        public static void GenerateDummyFiles(IEnumerable<FileInfo> movies)
        {
            if (movies != null)
            {
                foreach (FileInfo _item in movies)
                {
                    GenerateDummyFile(_item.FullName);
                }
            }
        }

        public static string FolderToSkip = null;
        public static bool FolderCompleteSkipped { get; set; }

        private static void _collector_ThumbnailCreated(object sender, EventArgs e)
        {
            ThumbnailCreatedEventArgs _args = e as ThumbnailCreatedEventArgs;
            if (_args != null)
            {
                if (_args.Movie != null)
                {
                    _args.Movie.MovieItemStatus = _args.Result == QueryResult.Done ? MovieItemStatus.Done : MovieItemStatus.Skipped;
                }
            }
        }

        private static bool ProcessMovie(MovieItem movie, StartActionType actionType)
        {
            if (movie != null && File.Exists(movie.FilePath))
            {
                movie.MovieItemStatus = MovieItemStatus.Querying;
                //string _sourceFile = Path.GetFileName(movie.FilePath);
                string _imageFile = Helpers.GetCorrectThumbnailPath(movie.FilePath, false);

                m_ProgressText.Text = "Processing " + movie.FilePath;
                m_Progress.Value++;

                if (File.Exists(_imageFile) && !m_OverwriteExisting && FileManager.Configuration.Options.AutogenerateThumbnail)
                {
                    //m_LogBox.Items.Insert(0, string.Format("* {0} - Skipped (thumbnail exists).", movie.Filename));
                    movie.MovieItemStatus = MovieItemStatus.SkippedExistingThumbnail;
                    return true;
                }

                string _msg = string.Empty;
                QueryResult _done = QueryResult.Abort;

                if (CurrentCollector != null)
                {
                    ShowAdorner(string.Format("Analyzing {0}...", movie.Filename), false);
                    Helpers.DoEvents();

                    try
                    {
                        if (actionType == StartActionType.GenerateRandomThumbs)
                        {
                            _done = VideoScreenShot.MakeThumbnail(movie.FilePath) ? QueryResult.Done : QueryResult.Skip;
                        }
                        else
                        {
                            _done = CurrentCollector.ProcessMovie(movie);
                        }
                    }
                    finally
                    {
                        HideAdorners();
                    }
                }

                if (_done == QueryResult.Abort)
                {
                    //abort
                    return false;
                }
                //_msg = string.Format("* {0}{1}", movie.Filename, (bool)_done ? " - Done." : " - Skipped.");

                switch (_done)
                {
                    case QueryResult.Done:
                        movie.MovieItemStatus = MovieItemStatus.Done;
                        break;
                    case QueryResult.NotFound:
                        movie.MovieItemStatus = MovieItemStatus.NotFound;
                        break;
                    case QueryResult.SkipFolder:
                        // TODO: Handle the folder skip here somehow...currently it is not skipping anything
                        FolderToSkip = movie.DirectoryName;
                        FolderCompleteSkipped = true;
                        movie.MovieItemStatus = MovieItemStatus.Skipped;
                        break;
                    default:
                        movie.MovieItemStatus = MovieItemStatus.Skipped;
                        break;
                }

                Helpers.DoEvents();
            }

            return true;
        }

        static UIElement AdornedElement
        {
            get
            {
                if (Application.Current != null)
                {
                    return (Application.Current.MainWindow as ThumbGenMainWindow).DockManager;
                    //return (CurrentCollector.MainWindow as ThumbGenMainWindow).progressListBox;
                }
                else
                {
                    return null;
                }
            }
        }

        static void _collector_Processed(object sender, EventArgs e)
        {
            HideAdorners();
        }

        static void _collector_Processing(object sender, EventArgs e)
        {
            ProcessingEventArgs _args = e as ProcessingEventArgs;
            string _msg = _args.Keywords != null ? string.Format("Searching \"{0}\"", _args != null ? _args.Keywords : string.Empty) : "Please wait...";
            ShowAdorner(_msg, true);
        }

    }
}
