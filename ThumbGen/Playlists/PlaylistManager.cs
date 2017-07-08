using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using ThumbGen.Translator;
using System.Threading;
using System.Globalization;

namespace ThumbGen.Playlists
{
    public class PlaylistItem
    {
        private long m_Duration = 0;
        public long Duration
        {
            get
            {
                if (m_Duration == 0)
                {
                    m_Duration = Info != null && Info.MediaInfo == null ? 0 : Helpers.GetDurationSeconds(Info.MediaInfo.Duration);
                }
                return m_Duration;
            }
        }

        private string m_DisplayTitle = null;
        public string DisplayTitle
        {
            get
            {
                if (m_DisplayTitle == null)
                {
                    m_DisplayTitle = Info == null ? string.Empty : string.Format("{0} {1} {2}", Info.Name, Info.Year, Info.Rating);
                }
                return m_DisplayTitle;
            }
        }

        public string Title
        {
            get
            {
                return Info != null ? Info.Name : string.Empty;
            }
        }

        public string OriginalTitle
        {
            get
            {
                return Info != null ? Info.OriginalTitle : string.Empty;
            }
        }

        private int m_Year = 0;
        public int Year
        {
            get
            {
                if (m_Year == 0)
                {
                    m_Year = Info != null && !string.IsNullOrEmpty(Info.Year) ? Int16.Parse(Info.Year) : 0;
                }
                return m_Year;
            }
        }

        private DateTime m_ReleaseDate = DateTime.MinValue;
        public DateTime ReleaseDate
        {
            get
            {
                if (m_ReleaseDate == DateTime.MinValue)
                {
                    string _format;
                    CultureInfo _provider = CultureInfo.InvariantCulture;
                    DateTime _dt = DateTime.MinValue;
                    if(Info != null && !string.IsNullOrEmpty(Info.ReleaseDate))
                    {
                        _format = "dd.MM.yyyy";
                        if (!DateTime.TryParseExact(Info.ReleaseDate, _format, _provider, DateTimeStyles.None, out _dt) || _dt == DateTime.MinValue)
                        {
                            _format = "yyyy-MM-dd";
                            if (!DateTime.TryParseExact(Info.ReleaseDate, _format, _provider, DateTimeStyles.None, out _dt) || _dt == DateTime.MinValue)
                            {
                                if (!DateTime.TryParse(Info.ReleaseDate, out _dt) || _dt == DateTime.MinValue)
                                {

                                }
                            }
                        }
                    }
                    m_ReleaseDate = _dt;
                }
                return m_ReleaseDate;
            }
        }

        public MovieInfo Info { get; set; }

        public string Filepath { get; set; }

        public PlaylistItem(string filePath, MovieInfo info)
        {
            Filepath = filePath;
            Info = info;
        }
    }

    public class PlaylistManager : IDisposable
    {
        public static string NOSPLIT_CRITERIA = "NOSPLIT";
        public static string UNASSIGNED_PLAYLIST_FILENAME = "_unassigned";

        private TranslatorManager m_translatorManager = new TranslatorManager();

        private Dictionary<int, MovieInfo> m_InfoCache = new Dictionary<int, MovieInfo>();

        private MovieInfo ExtractMovieInfo(string movieFilePath)
        {
            int _key = movieFilePath.GetHashCode();
            if (m_InfoCache.ContainsKey(_key))
            {
                return m_InfoCache[_key];
            }
            else
            {
                // check if there is a nfo/metadata file available
                string _metadataFilename = FileManager.Configuration.GetMoviesheetMetadataPath(movieFilePath, false);
                string _nfoFilename = FileManager.Configuration.GetMovieInfoPath(movieFilePath, false, MovieinfoType.Export);

                MovieInfo _info = null;

                // check metadata
                if (!string.IsNullOrEmpty(_metadataFilename) && File.Exists(_metadataFilename))
                {
                    MoviesheetsUpdateManager _metadataManager = MoviesheetsUpdateManager.CreateManagerForMovie(movieFilePath);
                    _info = _metadataManager.GetMovieInfo();
                    if (string.IsNullOrEmpty(_info.Name))
                    {
                        _info = null;
                    }
                }

                // check nfo
                if (_info == null && !string.IsNullOrEmpty(_nfoFilename) && File.Exists(_nfoFilename))
                {
                    nfoFileType _nfotype = nfoFileType.Unknown;
                    _info = nfoHelper.LoadNfoFile(movieFilePath, out _nfotype);
                    if (_info != null && string.IsNullOrEmpty(_info.Name))
                    {
                        _info = null;
                    }
                }


                m_InfoCache.Add(movieFilePath.GetHashCode(), _info);
                return _info;
            }
        }

        public void CreatePlaylists(IList<FileInfo> movieFiles, IList<UserOptions.Playlists> configSets)
        {
            if (configSets != null && configSets.Count != 0)
            {
                foreach (UserOptions.Playlists _config in configSets)
                {
                    if (_config.IsActive)
                    {
                        CreatePlaylists(movieFiles, _config);
                    }
                }
            }

        }

        private void CreatePlaylists(IList<FileInfo> movieFiles, UserOptions.Playlists config)
        {
            /*
             * - take each file and try to open .nfo/.tgmd
             * - from .nfo extract list of genres and put the to the dictionary
             * - process each item in dictionary
             * 
             */

            Dictionary<string, List<PlaylistItem>> _ItemsDictionary = new Dictionary<string, List<PlaylistItem>>();

            if (config != null && movieFiles != null && movieFiles.Count != 0)
            {
                bool _useSingleFile = config.Criteria == PlaylistManager.NOSPLIT_CRITERIA;
                bool _useUnassigned = config.UseUnassignedPlaylist && !_useSingleFile;

                string criteria = config.Criteria;
                if (!string.IsNullOrEmpty(criteria))
                {

                    if (_useUnassigned)
                    {
                        // add the _unassigned as key for the items missing the criteria
                        _ItemsDictionary.Add(UNASSIGNED_PLAYLIST_FILENAME, new List<PlaylistItem>());
                    }

                    if (_useSingleFile)
                    {
                        // add the PlaylistManager.NOSPLIT_PLAYLIST_FILENAME as key for all items
                        _ItemsDictionary.Add(config.SingleFilename, new List<PlaylistItem>());
                    }

                    try
                    {
                        bool _addToUnassigned = false;
                        foreach (FileInfo _file in movieFiles)
                        {
                            _addToUnassigned = false;
                            try
                            {
                                MovieItem _movieItem = FileManager.GetMovieByFilePath(_file.FullName);
                                //FileManager.SetMovieItemStatus(_movieItem, MovieItemStatus.Querying);

                                // extract movieinfo from metadata/nfo files
                                MovieInfo _info = ExtractMovieInfo(_file.FullName);

                                if (_info != null)
                                {
                                    List<string> _criteriaItems = null;

                                    // if more playlists will be generated then extract criteria items (else all items will be added to the PlaylistManager.NOSPLIT_PLAYLIST_FILENAME file
                                    if (!_useSingleFile)
                                    {
                                        _criteriaItems = ExtractCriteriaItems(criteria, _info);
                                    }

                                    if (_criteriaItems != null)
                                    {
                                        // will not enter here if single file mode
                                        // for each criteria item, if not in dictionary add it, if already there add movie to the list
                                        foreach (string _citem in _criteriaItems)
                                        {
                                            string _name = _citem;
                                            if (config.ForceEnglishResults)
                                            {
                                                _name = m_translatorManager.Translate(_name);
                                            }

                                            string _key = string.IsNullOrEmpty(_name) ? null : _name.ToLowerInvariant().Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
                                            if (!string.IsNullOrEmpty(_key))
                                            {
                                                if (!_ItemsDictionary.ContainsKey(_key))
                                                {
                                                    _ItemsDictionary.Add(_key, new List<PlaylistItem>());
                                                }
                                                List<PlaylistItem> _list = _ItemsDictionary[_key];

                                                _list.Add(new PlaylistItem(_file.FullName, _info));
                                            }
                                            else
                                            {
                                                _addToUnassigned = true;
                                            }
                                        }

                                        //FileManager.SetMovieItemStatus(_movieItem, MovieItemStatus.Done);
                                    }
                                    else
                                    {
                                        if (_useSingleFile)
                                        {
                                            List<PlaylistItem> _list = _ItemsDictionary[config.SingleFilename];
                                            _list.Add(new PlaylistItem(_file.FullName, _info));
                                            //FileManager.SetMovieItemStatus(_movieItem, MovieItemStatus.Done);
                                        }
                                        else
                                        {
                                            //FileManager.SetMovieItemStatus(_movieItem, MovieItemStatus.NotFound);
                                            _addToUnassigned = true;
                                        }
                                    }
                                }
                                else
                                {
                                    //FileManager.SetMovieItemStatus(_movieItem, MovieItemStatus.MetadataMissing);
                                    _addToUnassigned = true;
                                }

                                if (_useUnassigned && _addToUnassigned)
                                {
                                    _ItemsDictionary[UNASSIGNED_PLAYLIST_FILENAME].Add(new PlaylistItem(_file.FullName, new MovieInfo()));
                                }

                                //Helpers.DoEvents();
                            }
                            catch (Exception ex)
                            {
                                Loggy.Logger.DebugException("Processing playlist:", ex);
                            }
                        } // for
                    }
                    finally
                    {
                        //_translatorManager.ClearCache();
                        //_translatorManager = null;
                    }
                }

                // ItemsDictionary has all required infos, can create playlist files
                CreatePlaylistFiles(_ItemsDictionary, config);
            }
        }

        private List<string> ExtractCriteriaItems(string criteria, MovieInfo _info)
        {
            List<string> _criteriaItems = null;

            if (_info != null)
            {
                object _reflectedObject = _info;

                // try to get propertyinfo for the criteria
                Type _t = _info.GetType();

                PropertyInfo _p = _t.GetProperty(criteria);
                // property not found on MovieInfo object
                if (_p == null)
                {
                    // maybe it is a MediaInfoData property?
                    _p = _t.GetProperty("MediaInfo");
                    if (_p != null)
                    {
                        // get MediaInfoData object from the MovieInfo
                        MediaInfoData _mdata = _t.GetProperty("MediaInfo").GetValue(_reflectedObject, null) as MediaInfoData;
                        if (_mdata != null)
                        {
                            // the object to get prop value is now MediaInfoData
                            _reflectedObject = _mdata;
                            // try to get Property for the criteria
                            _p = _mdata.GetType().GetProperty(criteria);
                        }
                    }
                }

                if (_p != null)
                {

                    if (_p.PropertyType == typeof(List<string>))
                    {
                        // list of strings, split it and use individual item
                        _criteriaItems = _p.GetValue(_reflectedObject, null) as List<string>;
                        if(_criteriaItems != null)
                        {
                            _criteriaItems = (from c in _criteriaItems
                                              select c.Trim()).ToList<string>();
                        }
                    }
                    else
                    {
                        if (_p.PropertyType == typeof(string))
                        {
                            string _value = _p.GetValue(_reflectedObject, null) as string;
                            _value = _value != null ? _value.Trim() : _value;

                            if (criteria == "Name" || criteria == "OriginalTitle") // Name is always splitted by 1st char
                            {
                                _value = string.IsNullOrEmpty(_value) ? null :  _value.Substring(0, 1).ToUpperInvariant();
                            }
                            if (!string.IsNullOrEmpty(_value))
                            {
                                // string, use it directly (if not Rating)
                                if (criteria == "Rating")
                                {
                                    // in this case try to convert to integer and use only integer part
                                    if (_info.dRating > 0)
                                    {
                                        _value = ((int)_info.dRating).ToString();
                                    }
                                    else
                                    {
                                        _value = null;
                                    }
                                }

                                if (!string.IsNullOrEmpty(_value))
                                {
                                    _criteriaItems = new List<string>() { _value };
                                }
                            }
                        }
                    }
                }
            }

            return _criteriaItems;
        }

        private void CreatePlaylistFiles(Dictionary<string, List<PlaylistItem>> itemsDictionary, UserOptions.Playlists config)
        {
            try
            {
                if (itemsDictionary.Count != 0)
                {
                    string _playlistBaseFolder = config.RelPath;
                    if (!string.IsNullOrEmpty(_playlistBaseFolder))
                    {
                        try
                        {
                            Directory.CreateDirectory(_playlistBaseFolder);
                        }
                        catch(Exception ex)
                        {
                            Loggy.Logger.DebugException("Create playlists folder", ex);
                            return;
                        }

                        if (config.CleanFolder && (config.FileType == UserOptions.PlaylistFileType.M3U))
                        {
                            foreach (string file in Directory.GetFiles(_playlistBaseFolder, "*.m3u"))
                            {
                                try
                                {
                                    FileInfo fi = new FileInfo(file);
                                    File.SetAttributes(file, FileAttributes.Normal);
                                    File.Delete(file);
                                }
                                catch { }
                            }

                            try
                            {
                                Directory.Delete(_playlistBaseFolder);
                            }
                            catch { }
                            
                            Directory.CreateDirectory(_playlistBaseFolder);
                        }
                        

                        //Parallel.ForEach(ItemsDictionary, new Action<KeyValuePair<string,List<PlaylistItem>>>(delegate
                        foreach (KeyValuePair<string, List<PlaylistItem>> _pair in itemsDictionary)
                        {
                            try
                            {
                                // sort the list
                                _pair.Value.Sort(new PlaylistItemsComparer(config));

                                // m3u format
                                if (config.FileType == UserOptions.PlaylistFileType.M3U)
                                {
                                    CreateM3UPlaylist(_pair.Key, _pair.Value, _playlistBaseFolder, config);
                                }

                                //Helpers.DoEvents();
                            }
                            catch (Exception ex)
                            {
                                Loggy.Logger.DebugException("Finalize playlists", ex);
                            }
                        }
                    }
                }
            }
            finally
            {
                itemsDictionary.Clear();
            }
        }

        private void CreateM3UPlaylist(string filename, List<PlaylistItem> items, string baseFolder, UserOptions.Playlists config)
        {
            // if there are items in the criteria
            if (items != null && items.Count > 0)
            {
                FileInfo _t = new FileInfo(Path.Combine(baseFolder, filename) + "." + config.FileType.ToString().ToLowerInvariant());
                using (StreamWriter _text = new StreamWriter(_t.FullName))
                {
                    _text.WriteLine("#EXTM3U");
                    foreach (PlaylistItem _item in items)
                    {
                        _text.WriteLine(string.Format("#EXTINF:{0},{1}", _item.Duration, _item.DisplayTitle));

                        string _pathToMakeRelative = null;
                        if (config.UseFolderInsteadOfMovie)
                        {
                            // check if we are inside DVD/Bluray folder structure
                            if (Helpers.IsDVDPath(_item.Filepath))
                            {
                                _pathToMakeRelative = Helpers.GetDVDRootDirectory(_item.Filepath);
                            }
                            else
                            {
                                if (Helpers.IsBlurayPath(_item.Filepath))
                                {
                                    _pathToMakeRelative = Helpers.GetBlurayRootDirectory(_item.Filepath);
                                }
                                else
                                {
                                    // clasic structure, use Folder path
                                    _pathToMakeRelative = Path.GetDirectoryName(_item.Filepath);
                                }
                            }
                        }
                        else
                        {
                            // use movie path
                            _pathToMakeRelative = _item.Filepath;
                        }

                        // make the filepath relative to the path to playlist
                        Uri _plist = new Uri(_t.FullName, UriKind.Absolute);
                        Uri _cfile = new Uri(_pathToMakeRelative, UriKind.Absolute);
                        Uri _final = _plist.MakeRelativeUri(_cfile);

                        //_text.WriteLine(Uri.UnescapeDataString(_final.OriginalString));
                        string _s = Uri.UnescapeDataString(_final.OriginalString);
                        if (_s[_s.Length - 1] != '\\' && _s[_s.Length - 1] != '/' && config.UseFolderInsteadOfMovie)
                        {
                            _s = _s + @"/";
                        }
                        _text.WriteLine(_s);
                        _text.WriteLine();
                    }
                }
            }
        }

        // this is the main comparer between two PlaylistItem objects
        private class PlaylistItemsComparer : Comparer<PlaylistItem>
        {
            private UserOptions.Playlists Config = new UserOptions.Playlists();

            private AlphaTitleFullComparer _alpha = new AlphaTitleFullComparer();
            private AlphaTitleFirstLetterComparer _alphaFirst = new AlphaTitleFirstLetterComparer();
            private AlphaTitleOrgFullComparer _alphaOrg = new AlphaTitleOrgFullComparer();
            private AlphaTitleOrgFirstLetterComparer _alphaOrgFirst = new AlphaTitleOrgFirstLetterComparer();
            private YearComparer _year = new YearComparer();
            private RatingComparer _rating = new RatingComparer();
            private ReleaseDateComparer _releaseDate = new ReleaseDateComparer();

            public Comparer<PlaylistItem> GetComparerBySortCriteria(string criteria, bool isDescending)
            {
                switch (criteria)
                {
                    default:
                    case "Alpha":
                        _alpha.IsDescending = isDescending;
                        return _alpha;
                    case "AlphaFirst":
                        _alphaFirst.IsDescending = isDescending;
                        return _alphaFirst;
                    case "AlphaOrg":
                        _alpha.IsDescending = isDescending;
                        return _alphaOrg;
                    case "AlphaOrgFirst":
                        _alphaFirst.IsDescending = isDescending;
                        return _alphaOrgFirst;
                    case "Year":
                        _year.IsDescending = isDescending;
                        return _year;
                    case "Rating":
                        _rating.IsDescending = isDescending;
                        return _rating;
                    case "ReleaseDate":
                        _releaseDate.IsDescending = isDescending;
                        return _releaseDate;
                }
            }

            public PlaylistItemsComparer(UserOptions.Playlists config)
            {
                Config = config;
            }

            public override int Compare(PlaylistItem x, PlaylistItem y)
            {
                // compare using first sorting criteria
                int _result = GetComparerBySortCriteria(Config.SortCriteria, Config.IsSortingDescending).Compare(x, y);
                //if the items are the same
                if (_result == 0)
                {
                    // compare using second sorting criteria
                    _result = GetComparerBySortCriteria(Config.SortCriteria2, Config.IsSortingDescending2).Compare(x, y);
                }
                if (_result == 0)
                {
                    // if they are still equal then sort them Alphabetically
                    _result = GetComparerBySortCriteria("Alpha", false).Compare(x, y);
                }
                return _result;
            }

        }

        private abstract class BaseComparer : Comparer<PlaylistItem>
        {
            public bool IsDescending { get; set; }
        }

        // comparer for Alphabetically by full Title
        private class AlphaTitleFullComparer : BaseComparer
        {
            public override int Compare(PlaylistItem x, PlaylistItem y)
            {
                int _result = string.Compare(x.Title, y.Title);
                return IsDescending ? -_result : _result;
            }
        }

        // comparer for Alphabetically by first letter
        private class AlphaTitleFirstLetterComparer : BaseComparer
        {
            public override int Compare(PlaylistItem x, PlaylistItem y)
            {
                int _result = string.Compare(string.IsNullOrEmpty(x.Title) ? "" : x.Title[0].ToString(), string.IsNullOrEmpty(y.Title) ? "" : y.Title[0].ToString());
                return IsDescending ? -_result : _result;
            }
        }

        // comparer for Alphabetically by full Original Title
        private class AlphaTitleOrgFullComparer : BaseComparer
        {
            public override int Compare(PlaylistItem x, PlaylistItem y)
            {
                int _result = string.Compare(x.OriginalTitle, y.OriginalTitle);
                return IsDescending ? -_result : _result;
            }
        }

        // comparer for Alphabetically by first letter for OriginalTitle
        private class AlphaTitleOrgFirstLetterComparer : BaseComparer
        {
            public override int Compare(PlaylistItem x, PlaylistItem y)
            {
                int _result = string.Compare(string.IsNullOrEmpty(x.OriginalTitle) ? "" : x.OriginalTitle[0].ToString(), string.IsNullOrEmpty(y.OriginalTitle) ? "" : y.OriginalTitle[0].ToString());
                return IsDescending ? -_result : _result;
            }
        }

        // comparer for Year
        private class YearComparer : BaseComparer
        {
            public override int Compare(PlaylistItem x, PlaylistItem y)
            {
                int _result = 0;
                if (x.Year > y.Year)
                    _result = 1;
                if (x.Year < y.Year)
                    _result = -1;
                return IsDescending ? -_result : _result;
            }
        }

        // comparer for Rating
        private class RatingComparer : BaseComparer
        {
            public override int Compare(PlaylistItem x, PlaylistItem y)
            {
                int _result = 0;
                if (x.Info != null && y.Info != null)
                {
                    if (x.Info.dRating > y.Info.dRating)
                        _result = 1;
                    if (x.Info.dRating < y.Info.dRating)
                        _result = -1;
                }
                return IsDescending ? -_result : _result;
            }
        }

        // comparer for ReleaseDate
        private class ReleaseDateComparer : BaseComparer
        {
            public override int Compare(PlaylistItem x, PlaylistItem y)
            {
                int _result = x.ReleaseDate.CompareTo(y.ReleaseDate);;
                return IsDescending ? -_result : _result;
            }
        }


        public void Dispose()
        {
            m_translatorManager = null;
            m_InfoCache.Clear();
        }
    }



    public class Parallel
    {
        public static int NumberOfParallelTasks;

        static Parallel()
        {
            NumberOfParallelTasks = Environment.ProcessorCount;
        }

        public static void ForEach<T>(IEnumerable<T> enumerable, Action<T> action)
        {
            var syncRoot = new object();

            if (enumerable == null) return;

            var enumerator = enumerable.GetEnumerator();

            InvokeAsync<T> del = InvokeAction;

            var seedItemArray = new T[NumberOfParallelTasks];
            var resultList = new List<IAsyncResult>(NumberOfParallelTasks);

            for (int i = 0; i < NumberOfParallelTasks; i++)
            {
                bool moveNext;

                lock (syncRoot)
                {
                    moveNext = enumerator.MoveNext();
                    seedItemArray[i] = enumerator.Current;
                }

                if (moveNext)
                {
                    var iAsyncResult = del.BeginInvoke
             (enumerator, action, seedItemArray[i], syncRoot, i, null, null);
                    resultList.Add(iAsyncResult);
                }
            }

            foreach (var iAsyncResult in resultList)
            {
                del.EndInvoke(iAsyncResult);
                iAsyncResult.AsyncWaitHandle.Close();
            }
        }

        delegate void InvokeAsync<T>(IEnumerator<T> enumerator,
        Action<T> achtion, T item, object syncRoot, int i);

        static void InvokeAction<T>(IEnumerator<T> enumerator, Action<T> action,
                T item, object syncRoot, int i)
        {
            if (String.IsNullOrEmpty(Thread.CurrentThread.Name))
                Thread.CurrentThread.Name =
            String.Format("Parallel.ForEach Worker Thread No:{0}", i);

            bool moveNext = true;

            while (moveNext)
            {
                action.Invoke(item);

                lock (syncRoot)
                {
                    moveNext = enumerator.MoveNext();
                    item = enumerator.Current;
                }
            }
        }
    }
}
