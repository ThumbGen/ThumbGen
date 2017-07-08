using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using ThumbGen.MovieSheets;
using System.Windows;
using System.Web;

namespace ThumbGen
{

    public enum UpdateItemType
    {
        Moviesheet,
        Extrasheet,
        ParentFoldersheet,
        Thumbnail,
        ExtraThumbnail,
        Nfo,
        ImagesExport
    }

    [Serializable]
    [XmlRootAttribute(ElementName = "info", IsNullable = false)]
    public class MoviesheetInfo
    {
        [XmlElement(ElementName = "templatename")]
        public string TemplateName { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string FileVersion { get; set; }

        [XmlElement(ElementName = "coverextension")]
        public string CoverExtension { get; set; }

        public bool HasCover
        {
            get
            {
                return !string.IsNullOrEmpty(CoverExtension);
            }
        }

        [XmlElement(ElementName = "backgroundextension")]
        public string BackgroundExtension { get; set; }

        public bool HasBackground
        {
            get
            {
                return !string.IsNullOrEmpty(BackgroundExtension);
            }
        }

        [XmlElement(ElementName = "fanart1extension")]
        public string Fanart1Extension { get; set; }

        public bool HasFanart1
        {
            get
            {
                return !string.IsNullOrEmpty(Fanart1Extension);
            }
        }

        [XmlElement(ElementName = "fanart2extension")]
        public string Fanart2Extension { get; set; }

        public bool HasFanart2
        {
            get
            {
                return !string.IsNullOrEmpty(Fanart2Extension);
            }
        }

        [XmlElement(ElementName = "fanart3extension")]
        public string Fanart3Extension { get; set; }

        public bool HasFanart3
        {
            get
            {
                return !string.IsNullOrEmpty(Fanart3Extension);
            }
        }

        [XmlElement(ElementName = "previewextension")]
        public string PreviewExtension { get; set; }

        public bool HasPreview
        {
            get
            {
                return !string.IsNullOrEmpty(PreviewExtension);
            }
        }

        [XmlElement(ElementName = "moviename")]
        public string MovieName { get; set; }

        [XmlElement(ElementName = "moviehash")]
        public string MovieHash { get; set; }

        public MoviesheetInfo()
        {
        }

        public void SaveInfo(string destPath)
        {
            XmlSerializer _xs = new XmlSerializer(typeof(MoviesheetInfo));
            using (FileStream _fs = new FileStream(destPath, FileMode.Create, FileAccess.ReadWrite))
            {
                try
                {
                    _xs.Serialize(_fs, this);
                }
                catch { }
            }
        }

        public void SaveInfo(Stream target)
        {
            if (target != null)
            {
                try
                {
                    XmlSerializer _xs = new XmlSerializer(typeof(MoviesheetInfo));
                    _xs.Serialize(target, this);
                }
                catch { }
            }
        }

        public static MoviesheetInfo LoadInfo(Stream data)
        {
            if (data != null && data.CanRead)
            {
                data.Position = 0;
                XmlSerializer _xs = new XmlSerializer(typeof(MoviesheetInfo));
                try
                {
                    return _xs.Deserialize(data) as MoviesheetInfo;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }

    public class MoviesheetsUpdateManagerParams
    {
        public string BackgroundPath { get; set; }
        public string Fanart1Path { get; set; }
        public string Fanart2Path { get; set; }
        public string Fanart3Path { get; set; }
        public MovieInfo nfo { get; set; }
        public string CoverPath { get; set; }
        public string PreviewPath { get; set; }

        public MoviesheetsUpdateManagerParams(string bg, string f1, string f2, string f3, MovieInfo movieinfo, string cover, string preview)
        {
            BackgroundPath = bg;
            Fanart1Path = f1;
            Fanart2Path = f2;
            Fanart3Path = f3;
            nfo = movieinfo;
            CoverPath = cover;
            PreviewPath = preview;
        }
    }

    public class MovieDoneEventArgs : EventArgs
    {
        public string MovieFilename { get; set; }

        public MovieDoneEventArgs(string moviefilename)
        {
            MovieFilename = moviefilename;
        }

    }

    public class MoviesheetsUpdateManager
    {
        public static string EXTENSION = ".tgmd";
        public static string BACKGROUND_STREAM_NAME = "BACKGROUND";
        public static string COVER_STREAM_NAME = "COVER";
        public static string FANART1_STREAM_NAME = "FANART1";
        public static string FANART2_STREAM_NAME = "FANART2";
        public static string FANART3_STREAM_NAME = "FANART3";
        public static string INFO_STREAM_NAME = "INFO";
        public static string NFO_STREAM_NAME = "NFO";
        public static string PREVIEW_STREAM_NAME = "PREVIEW";

        public static List<TemplateItem> SelectedTemplates = new List<TemplateItem>();

        private string m_movieFilename;

        private string m_targetFilename = null;
        public string TargetFilename
        {
            get
            {
                //if (string.IsNullOrEmpty(m_targetFilename))
                //{
                //    m_targetFilename = GetUpdateFilename(m_movieFilename, m_IsParentFolder);
                //}
                return m_targetFilename;
            }
        }

        private bool? m_HasCover = null;
        public bool HasCover
        {
            get
            {
                if (m_HasCover == null)
                {
                    m_HasCover = !string.IsNullOrEmpty(TargetFilename) ? ZipHelper.HasStream(TargetFilename, COVER_STREAM_NAME) : false;
                }
                return (bool)m_HasCover;
            }
        }

        private bool? m_HasBackground = null;
        public bool HasBackground
        {
            get
            {
                if (m_HasBackground == null)
                {
                    m_HasBackground = !string.IsNullOrEmpty(TargetFilename) ? ZipHelper.HasStream(TargetFilename, BACKGROUND_STREAM_NAME) : false;
                }
                return (bool)m_HasBackground;
            }
        }

        private bool? m_HasFanart1 = null;
        public bool HasFanart1
        {
            get
            {
                if (m_HasFanart1 == null)
                {
                    m_HasFanart1 = !string.IsNullOrEmpty(TargetFilename) ? ZipHelper.HasStream(TargetFilename, FANART1_STREAM_NAME) : false;
                }
                return (bool)m_HasFanart1;
            }
        }

        private bool? m_HasFanart2 = null;
        public bool HasFanart2
        {
            get
            {
                if (m_HasFanart2 == null)
                {
                    m_HasFanart2 = !string.IsNullOrEmpty(TargetFilename) ? ZipHelper.HasStream(TargetFilename, FANART2_STREAM_NAME) : false;
                }
                return (bool)m_HasFanart2;
            }
        }

        private bool? m_HasFanart3 = null;
        public bool HasFanart3
        {
            get
            {
                if (m_HasFanart3 == null)
                {
                    m_HasFanart3 = !string.IsNullOrEmpty(TargetFilename) ? ZipHelper.HasStream(TargetFilename, FANART3_STREAM_NAME) : false;
                }
                return (bool)m_HasFanart3;
            }
        }

        private bool? m_HasNfo = null;
        public bool HasNfo
        {
            get
            {
                if (m_HasNfo == null)
                {
                    m_HasNfo = !string.IsNullOrEmpty(TargetFilename) ? ZipHelper.HasStream(TargetFilename, NFO_STREAM_NAME) : false;
                }
                return (bool)m_HasNfo;
            }
        }

        private bool? m_HasPreview = null;
        public bool HasPreview
        {
            get
            {
                if (m_HasPreview == null)
                {
                    m_HasPreview = !string.IsNullOrEmpty(TargetFilename) ? ZipHelper.HasStream(TargetFilename, PREVIEW_STREAM_NAME) : false;
                }
                return (bool)m_HasPreview;
            }
        }

        //public static string GetUpdateFilename(string movieFilename, bool isParentFolder)
        //{
        //    if (!string.IsNullOrEmpty(movieFilename))
        //    {
        //        return isParentFolder ? FileManager.Configuration.GetParentFolderMetadataPath(movieFilename, false) : FileManager.Configuration.GetMoviesheetMetadataPath(movieFilename, false); 
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        public static MoviesheetsUpdateManager CreateManagerForMovie(string moviePath)
        {
            return new MoviesheetsUpdateManager(FileManager.Configuration.GetMoviesheetMetadataPath(moviePath, false), moviePath);
        }

        public static MoviesheetsUpdateManager CreateManagerForParentFolder(string moviePath)
        {
            return new MoviesheetsUpdateManager(FileManager.Configuration.GetParentFolderMetadataPath(moviePath, false), moviePath);
        }

        public static MoviesheetsUpdateManager CreateManagerFromMetadata(string metadataFile, string moviePath)
        {
            return new MoviesheetsUpdateManager(metadataFile, moviePath);
        }

        private MoviesheetsUpdateManager(string metadataFilePath, string moviePath)
        {
            m_targetFilename = metadataFilePath;
            m_movieFilename = moviePath;
        }

        private void AddPart(string partName, string sourceFilename)
        {
            if (!string.IsNullOrEmpty(sourceFilename) && File.Exists(sourceFilename))
            {
                using (FileStream _fs = new FileStream(sourceFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ZipHelper.AddStreamPartToZip(TargetFilename, _fs, partName);
                }
            }
        }

        private void AddPart(string partName, Stream data)
        {
            ZipHelper.AddStreamPartToZip(TargetFilename, data, partName);
        }

        public void GenerateUpdateFile(MoviesheetsUpdateManagerParams parameters, string templateName)
        {
            /* update file 
             * contains:
             *  - info
             *  - background
             *  - .nfo file
             *  - fanart1
             *  - fanart2
             *  - fanart3
             *  - cover
             */

            string _backup = null;
            try
            {
                if (!string.IsNullOrEmpty(TargetFilename) && File.Exists(TargetFilename))
                {
                    // backup current .tgmd file by renaming it to .tgmdbackup
                    _backup = Path.ChangeExtension(TargetFilename, ".tgmdbackup");
                    File.Move(TargetFilename, _backup);
                }

                // make new one
                MoviesheetInfo _info = new MoviesheetInfo();
                _info.FileVersion = "1";
                _info.TemplateName = templateName;
                _info.MovieName = string.IsNullOrEmpty(m_movieFilename) ? string.Empty : HttpUtility.HtmlEncode(Path.GetFileName(m_movieFilename));
                try
                {
                    _info.MovieHash = Subtitles.SubtitlesManager.GetMovieHash(m_movieFilename);
                }
                catch { }
                try
                {
                    AddPart(BACKGROUND_STREAM_NAME, parameters.BackgroundPath);
                    _info.BackgroundExtension = Path.GetExtension(parameters.BackgroundPath);
                    AddPart(COVER_STREAM_NAME, parameters.CoverPath);
                    _info.CoverExtension = Path.GetExtension(parameters.CoverPath);
                    AddPart(FANART1_STREAM_NAME, parameters.Fanart1Path);
                    _info.Fanart1Extension = Path.GetExtension(parameters.Fanart1Path);
                    AddPart(FANART2_STREAM_NAME, parameters.Fanart2Path);
                    _info.Fanart2Extension = Path.GetExtension(parameters.Fanart2Path);
                    AddPart(FANART3_STREAM_NAME, parameters.Fanart3Path);
                    _info.Fanart3Extension = Path.GetExtension(parameters.Fanart3Path);
                    AddPart(PREVIEW_STREAM_NAME, parameters.PreviewPath);
                    _info.PreviewExtension = Path.GetExtension(parameters.PreviewPath);

                    using (MemoryStream _ms = new MemoryStream())
                    {
                        parameters.nfo.Save(_ms, this.m_movieFilename, true);
                        AddPart(NFO_STREAM_NAME, _ms);
                    }

                }
                finally
                {
                    using (MemoryStream _ms = new MemoryStream())
                    {
                        _info.SaveInfo(_ms);
                        AddPart(INFO_STREAM_NAME, _ms);
                    }
                }
            }
            finally
            {
                if (_backup != null)
                {
                    // remove the old file
                    Helpers.RemoveFile(_backup);
                }
            }
        }

        public bool GetImage(string partName, string destFilename)
        {
            bool _result = false;

            // assume the files are jpg
            Stream _st = ZipHelper.ExtractStreamFromZip(TargetFilename, partName);
            if (_st != null && _st.Length != 0)
            {
                _st.CopyTo(destFilename);
                _st.Dispose();
                _st = null;
                _result = true;
            }

            return _result;
        }

        public Stream GetPreview()
        {
            Stream _result = null;
            if (!string.IsNullOrEmpty(TargetFilename) && File.Exists(TargetFilename))
            {
                _result = ZipHelper.ExtractStreamFromZip(TargetFilename, PREVIEW_STREAM_NAME);
                if (_result != null && _result.CanSeek)
                {
                    _result.Position = 0;
                }
            }
            return _result;
        }

        public MovieInfo GetMovieInfo()
        {
            MovieInfo _result = new MovieInfo();

            if (!string.IsNullOrEmpty(TargetFilename) && File.Exists(TargetFilename))
            {
                Stream _st = ZipHelper.ExtractStreamFromZip(TargetFilename, NFO_STREAM_NAME);
                if (_st != null && _st.CanSeek)
                {
                    _st.Position = 0;
                    try
                    {
                        _result = _result.Load(_st);
                    }
                    catch { }
                }
            }

            return _result;
        }

        public MoviesheetInfo GetMetadataInfo()
        {
            MoviesheetInfo _result = null;
            if (!string.IsNullOrEmpty(TargetFilename) && File.Exists(TargetFilename))
            {
                Stream _st = ZipHelper.ExtractStreamFromZip(TargetFilename, INFO_STREAM_NAME);
                if (_st != null && _st.CanSeek)
                {
                    _st.Position = 0;
                    try
                    {
                        _result = MoviesheetInfo.LoadInfo(_st);
                    }
                    catch { }
                }
            }
            return _result;
        }

        private static ManualResetEvent CancelProcessing = new ManualResetEvent(false);

        private static object ProcessedCount = 0;
        //private static int TotalCount = 0;

        //private static event EventHandler<MovieDoneEventArgs> MovieDone;

        public class UpdateItemComparer : IEqualityComparer<UpdateItem>
        {
            public bool Equals(UpdateItem x, UpdateItem y)
            {
                return (x != null && y != null && x.GetHashCode() == y.GetHashCode());
            }

            public int GetHashCode(UpdateItem obj)
            {
                return obj.GetHashCode();
            }
        }

        public class UpdatesDispatcher : IEnumerable<MetadataUpdateItem>
        {
            private List<MetadataUpdateItem> m_Cache = new List<MetadataUpdateItem>();

            public int Count
            {
                get
                {
                    return m_Cache.Count;
                }
            }

            public UpdatesDispatcher()
            {
                AllItems = new List<UpdateItem>();
            }

            public void Add(MetadataUpdateItem item)
            {
                m_Cache.Add(item);
            }

            public void RemoveEmptyItems()
            {
                for (int _i = m_Cache.Count - 1; _i > 0; _i--)
                {
                    if (m_Cache[_i].Items.Count == 0)
                    {
                        m_Cache.RemoveAt(_i);
                    }
                }
            }

            public MetadataUpdateItem this[int index]
            {
                get { return m_Cache[index]; }
            }

            public IEnumerator<MetadataUpdateItem> GetEnumerator()
            {
                return m_Cache.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return m_Cache.GetEnumerator();
            }

            public static List<UpdateItem> AllItems = null;

            public void Clear()
            {
                while (AllItems.Count > 0)
                {
                    AllItems[0] = null;
                    AllItems.RemoveAt(0);
                }
                AllItems.Clear();

                while (m_Cache.Count > 0)
                {
                    m_Cache[0].Clear();
                    m_Cache[0] = null;
                    m_Cache.RemoveAt(0);
                }
                m_Cache.Clear();
            }

            public bool HasWorkForMovie(string moviepath)
            {
                bool _result = false;

                if (!string.IsNullOrEmpty(moviepath))
                {
                    foreach (MetadataUpdateItem _mui in m_Cache)
                    {
                        if (!string.IsNullOrEmpty(_mui.MoviePath) && string.Compare(moviepath, _mui.MoviePath, true) == 0)
                        {
                            _result = true;
                            break;
                        }
                    }
                }

                return _result;
            }
        }

        public class MetadataUpdateItem
        {
            public ManualResetEvent DoneEvent { get; set; }
            public string MoviePath { get; set; }
            public string MetadataFile { get; set; }
            public List<UpdateItem> Items { get; private set; }

            public MetadataUpdateItem(string moviepath, string metadatafile)
            {
                MoviePath = moviepath;
                MetadataFile = metadatafile;
                Items = new List<UpdateItem>();
            }

            public void Clear()
            {
                while (Items.Count > 0)
                {
                    Items[0] = null;
                    Items.RemoveAt(0);
                }
                Items.Clear();
            }

            public void AddItem(UpdateItem item)
            {
                if (!UpdatesDispatcher.AllItems.Contains(item, new UpdateItemComparer()))
                {
                    Items.Add(item);
                    UpdatesDispatcher.AllItems.Add(item);
                }
            }

            public void ThreadPoolCallback()
            {
                try
                {
                    try
                    {
                        Loggy.Logger.Debug(string.Format("Entering Thread {0}", Thread.CurrentThread.ManagedThreadId));
                        Loggy.Logger.Factory.Flush();

                        try
                        {
                            MovieItem _movieItem = FileManager.GetMovieByFilePath(this.MoviePath);
                            FileManager.SetMovieItemStatus(_movieItem, MovieItemStatus.Querying);
                        }
                        catch { }

                        MoviesheetsUpdateManager _man = new MoviesheetsUpdateManager(this.MetadataFile, this.MoviePath);

                        MoviesheetInfo _metadataInfo = _man.GetMetadataInfo();

                        string _ext = _metadataInfo != null && !string.IsNullOrEmpty(_metadataInfo.CoverExtension) ? _metadataInfo.CoverExtension : ".jpg";
                        string _tmpCoverPath = Helpers.GetUniqueFilename(_ext);
                        _ext = _metadataInfo != null && !string.IsNullOrEmpty(_metadataInfo.BackgroundExtension) ? _metadataInfo.BackgroundExtension : ".jpg";
                        string _tmpBackgroundPath = Helpers.GetUniqueFilename(_ext);
                        _ext = _metadataInfo != null && !string.IsNullOrEmpty(_metadataInfo.Fanart1Extension) ? _metadataInfo.Fanart1Extension : ".jpg";
                        string _tmpFanart1Path = Helpers.GetUniqueFilename(_ext);
                        _ext = _metadataInfo != null && !string.IsNullOrEmpty(_metadataInfo.Fanart2Extension) ? _metadataInfo.Fanart2Extension : ".jpg";
                        string _tmpFanart2Path = Helpers.GetUniqueFilename(_ext);
                        _ext = _metadataInfo != null && !string.IsNullOrEmpty(_metadataInfo.Fanart3Extension) ? _metadataInfo.Fanart3Extension : ".jpg";
                        string _tmpFanart3Path = Helpers.GetUniqueFilename(_ext);

                        MovieInfo _movieinfo = _man.GetMovieInfo();
                        MediaInfoData _mediainfo = _movieinfo != null ? _movieinfo.MediaInfo : null;

                        Action ExtractImagesIfNeeded = new Action(delegate
                            {
                                if (!File.Exists(_tmpCoverPath))
                                {
                                    _man.GetImage(MoviesheetsUpdateManager.COVER_STREAM_NAME, _tmpCoverPath);
                                }
                                if (!File.Exists(_tmpBackgroundPath))
                                {
                                    _man.GetImage(MoviesheetsUpdateManager.BACKGROUND_STREAM_NAME, _tmpBackgroundPath);
                                }
                                if (!File.Exists(_tmpFanart1Path))
                                {
                                    _man.GetImage(MoviesheetsUpdateManager.FANART1_STREAM_NAME, _tmpFanart1Path);
                                }
                                if (!File.Exists(_tmpFanart2Path))
                                {
                                    _man.GetImage(MoviesheetsUpdateManager.FANART2_STREAM_NAME, _tmpFanart2Path);
                                }
                                if (!File.Exists(_tmpFanart3Path))
                                {
                                    _man.GetImage(MoviesheetsUpdateManager.FANART3_STREAM_NAME, _tmpFanart3Path);
                                }
                            });

                        try
                        {
                            foreach (UpdateItem _item in Items)
                            {
                                // if cancellation was approved, jump out
                                if (CancelProcessing.WaitOne(20))
                                {
                                    return;
                                }

                                switch (_item.ItemType)
                                {
                                    case UpdateItemType.Moviesheet:
                                    case UpdateItemType.Extrasheet:
                                    case UpdateItemType.ParentFoldersheet:
                                        if (_item.Template != null)
                                        {
                                            SheetType _sheetType = _item.ItemType == UpdateItemType.Extrasheet ? SheetType.Extra : _item.ItemType == UpdateItemType.ParentFoldersheet ? SheetType.Spare : SheetType.Main;

                                            MovieSheetsGenerator _Generator = new MovieSheetsGenerator(_sheetType, this.MoviePath);

                                            _Generator.SelectedTemplate = _item.Template;

                                            // call the Action responsible to extract images if missing
                                            ExtractImagesIfNeeded.Invoke();

                                            // try to get latest IMDB rating for the movie
                                            if (_movieinfo != null && FileManager.Configuration.Options.UpdateIMDbRating)
                                            {
                                                try
                                                {
                                                    string _newRating = new IMDBMovieInfo().GetIMDbRating(_movieinfo.IMDBID);
                                                    if (!string.IsNullOrEmpty(_newRating))
                                                    {
                                                        _movieinfo.Rating = _newRating;
                                                        try
                                                        {
                                                            // update back the metadata (as the rating is needed for playlists)
                                                            using (MemoryStream _ms = new MemoryStream())
                                                            {
                                                                _movieinfo.Save(_ms, this.MoviePath, true);
                                                                _man.AddPart(NFO_STREAM_NAME, _ms);
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Loggy.Logger.DebugException("Updating Rating into .tgmd.", ex);
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }

                                            // set items

                                            _Generator.MovieInfo = _movieinfo;
                                            _Generator.MediaInfo = _mediainfo;
                                            _Generator.UpdateCover(_tmpCoverPath);
                                            _Generator.UpdateBackdrop(MoviesheetImageType.Background, _tmpBackgroundPath);
                                            _Generator.UpdateBackdrop(MoviesheetImageType.Fanart1, _tmpFanart1Path);
                                            _Generator.UpdateBackdrop(MoviesheetImageType.Fanart2, _tmpFanart2Path);
                                            _Generator.UpdateBackdrop(MoviesheetImageType.Fanart3, _tmpFanart3Path);

                                            _Generator.RenderAndReplicateMoviesheet(_item.TargetPath, true);
                                            _Generator.Dispose();
                                            _Generator.MovieInfo = null;
                                            _Generator.MediaInfo = null;
                                            _Generator.SelectedTemplate = null;
                                            _Generator = null;
                                        }
                                        break;
                                    case UpdateItemType.Thumbnail:
                                        if (!File.Exists(_tmpCoverPath))
                                        {
                                            _man.GetImage(MoviesheetsUpdateManager.COVER_STREAM_NAME, _tmpCoverPath);
                                        }
                                        Helpers.CreateThumbnailImage(_tmpCoverPath, _item.TargetPath, FileManager.Configuration.Options.KeepAspectRatio);
                                        break;
                                    case UpdateItemType.ExtraThumbnail:
                                        if (!File.Exists(_tmpCoverPath))
                                        {
                                            _man.GetImage(MoviesheetsUpdateManager.COVER_STREAM_NAME, _tmpCoverPath);
                                        }
                                        Helpers.CreateExtraThumbnailImage(_tmpCoverPath, _item.TargetPath);
                                        break;
                                    case UpdateItemType.Nfo:
                                        if (_movieinfo != null)
                                        {
                                            nfoHelper.GenerateNfoFile(_item.MoviePath, _movieinfo, _movieinfo.MediaInfo != null ? _movieinfo.MediaInfo : null);
                                        }
                                        break;
                                    case UpdateItemType.ImagesExport:
                                        Executor _executor = new Executor(_item.MoviePath);
                                        // make sure the images are extracted to their temp locations (as maybe no sheet needs to be generated, only export is wanted
                                        ExtractImagesIfNeeded.Invoke();
                                        // export images (that are required)
                                        _executor.ExportCover(_tmpCoverPath);
                                        _executor.ExportBackdrop(_tmpBackgroundPath, MoviesheetImageType.Background);
                                        _executor.ExportBackdrop(_tmpFanart1Path, MoviesheetImageType.Fanart1);
                                        _executor.ExportBackdrop(_tmpFanart2Path, MoviesheetImageType.Fanart2);
                                        _executor.ExportBackdrop(_tmpFanart3Path, MoviesheetImageType.Fanart3);
                                        break;
                                }

                            } // foreach

                            try
                            {
                                MovieItem _movieItem = FileManager.GetMovieByFilePath(this.MoviePath);
                                _movieItem.MovieItemStatus = MovieItemStatus.Done;
                            }
                            catch (Exception ex)
                            {
                                Loggy.Logger.DebugException("Set movieitem status:", ex);
                            }
                            _man = null;
                        }
                        finally
                        {
                            Helpers.RemoveFile(_tmpCoverPath);
                            Helpers.RemoveFile(_tmpBackgroundPath);
                            Helpers.RemoveFile(_tmpFanart1Path);
                            Helpers.RemoveFile(_tmpFanart2Path);
                            Helpers.RemoveFile(_tmpFanart3Path);
                        }
                    }

                    catch (Exception ex)
                    {
                        try
                        {
                            MovieItem _movieItem = FileManager.GetMovieByFilePath(this.MoviePath);
                            FileManager.SetMovieItemStatus(_movieItem, MovieItemStatus.Exception);
                            Loggy.Logger.DebugException(string.Format("Processing file {0}", this.MoviePath), ex);
                        }
                        catch { }
                    }
                }
                finally
                {

                    if (this.DoneEvent != null)
                    {
                        this.DoneEvent.Set();
                    }
                }
            }
        }

        public class UpdateItem
        {
            public string MoviePath { get; set; }
            public UpdateItemType ItemType { get; set; }
            public TemplateItem Template { get; set; }

            private string m_TargetPath = null;
            public string TargetPath
            {
                get
                {
                    return GetTargetPath();
                }
            }

            private string GetTargetPath()
            {
                if (m_TargetPath == null)
                {
                    try
                    {
                        // based on ItemType detect the target path
                        switch (ItemType)
                        {
                            case UpdateItemType.Moviesheet:
                                m_TargetPath = FileManager.Configuration.GetMoviesheetPath(MoviePath, true);
                                break;
                            case UpdateItemType.Extrasheet:
                                m_TargetPath = FileManager.Configuration.GetMoviesheetForFolderPath(MoviePath, true);
                                break;
                            case UpdateItemType.ParentFoldersheet:
                                m_TargetPath = FileManager.Configuration.GetMoviesheetForParentFolderPath(MoviePath, true);
                                break;
                            case UpdateItemType.Thumbnail:
                                m_TargetPath = FileManager.Configuration.GetThumbnailPath(MoviePath, true);
                                break;
                            case UpdateItemType.ExtraThumbnail:
                                m_TargetPath = FileManager.Configuration.GetFolderjpgPath(MoviePath, true);
                                break;
                            case UpdateItemType.Nfo:
                                m_TargetPath = FileManager.Configuration.GetMovieInfoPath(MoviePath, true, MovieinfoType.Export);
                                break;
                            case UpdateItemType.ImagesExport:
                                m_TargetPath = Guid.NewGuid().ToString();
                                break;
                            default:
                                m_TargetPath = null;
                                break;
                        }
                    }
                    catch { }
                }

                // IMPORTANT! Resolve the path to be able to hash it properly (d:\test\movies\..\sheet.jpg  it THE SAME as d:\test\news\..\sheet.jpg)
                if (!string.IsNullOrEmpty(m_TargetPath))
                {
                    try
                    {
                        m_TargetPath = new Uri(m_TargetPath).LocalPath;
                    }
                    catch
                    {
                        // let m_targetpath be whatever string (used to GetHash)
                    }
                }

                return m_TargetPath;
            }

            public override int GetHashCode()
            {
                if (!string.IsNullOrEmpty(TargetPath))
                {
                    return TargetPath.ToLowerInvariant().GetHashCode();
                }
                else
                {
                    return 0;
                }
            }

            public UpdateItem(string moviepath, UpdateItemType itemtype)
            {
                MoviePath = moviepath;
                ItemType = itemtype;

                GetTargetPath();
            }
        }

        public static void ApplyNewTemplate(IList<FileInfo> movieFiles, List<TemplateItem> templates)
        {
            bool _genExtraThumb = FileManager.Configuration.Options.AutogenerateFolderJpg;
            bool _genThumb = FileManager.Configuration.Options.AutogenerateThumbnail;
            bool _updateRating = FileManager.Configuration.Options.UpdateIMDbRating;
            bool _genMainsheet = FileManager.Configuration.Options.AutogenerateMovieSheet;
            bool _genExtrasheet = FileManager.Configuration.Options.AutogenerateMoviesheetForFolder;
            bool _genParentFoldersheet = FileManager.Configuration.Options.AutogenerateMoviesheetForParentFolder;
            bool _genNfo = FileManager.Configuration.Options.AutogenerateMovieInfo;
            bool _doExports = (FileManager.Configuration.Options.ExportImagesOptions.AutoExportFanart1jpgAsBackground ||
                              FileManager.Configuration.Options.ExportImagesOptions.AutoExportFanart2jpgAsBackground ||
                              FileManager.Configuration.Options.ExportImagesOptions.AutoExportFanart3jpgAsBackground ||
                              FileManager.Configuration.Options.ExportImagesOptions.AutoExportFanartjpgAsBackground ||
                              FileManager.Configuration.Options.ExportImagesOptions.AutoExportFolderjpgAsCover) && 
                                FileManager.Configuration.Options.EnableExportFromMetadata;

            FileManager.CancellationPending = false;
            CancelProcessing.Reset();

            // back up the setting and restore it at the end
            bool _autogenerateMetadata = FileManager.Configuration.Options.AutogenerateMoviesheetMetadata;

            UpdatesDispatcher _dispatcher = new UpdatesDispatcher();

            try
            {
                FileManager.Configuration.Options.AutogenerateMoviesheetMetadata = false;
                /* 
                 *  Main thread is dispatching work and ONLY rendering final result is passed to a thread
                 *  
                 *  Build first a list with "what needs to be done": what moviesheets need render, what foldersheets need render and what parentsheets need render
                 *  Then distribute rendering to threads
                 */

                int _total = movieFiles.Count;

                FileManager.ShowAdorner("Analyzing files... Please wait...", false);

                foreach (FileInfo _file in movieFiles)
                {
                    MetadataUpdateItem _mui = null;

                    try
                    {
                        // if we need to generate some item that requires main metadatafile
                        if (_genMainsheet || _genExtrasheet || _genThumb || _genExtraThumb || _genNfo || _doExports)
                        {
                            // check if there is a metadata file available
                            string _metadataFilename = FileManager.Configuration.GetMoviesheetMetadataPath(_file.FullName, false);
                            if (!string.IsNullOrEmpty(_metadataFilename) && File.Exists(_metadataFilename))
                            {
                                _mui = new MetadataUpdateItem(_file.FullName, _metadataFilename);
                                _dispatcher.Add(_mui);

                                // we have a candidate for thumbnail/extrathumbnail/moviesheet/extrasheet
                                if (_genMainsheet)
                                {
                                    UpdateItem _ui = new UpdateItem(_file.FullName, UpdateItemType.Moviesheet);
                                    _ui.Template = templates[0];
                                    _mui.AddItem(_ui);
                                }

                                if (_genExtrasheet)
                                {
                                    UpdateItem _ui = new UpdateItem(_file.FullName, UpdateItemType.Extrasheet);
                                    _ui.Template = templates[1];
                                    _mui.AddItem(_ui);
                                }

                                if (_genThumb)
                                {
                                    UpdateItem _ui = new UpdateItem(_file.FullName, UpdateItemType.Thumbnail);
                                    _mui.AddItem(_ui);
                                }

                                if (_genExtraThumb)
                                {
                                    UpdateItem _ui = new UpdateItem(_file.FullName, UpdateItemType.ExtraThumbnail);
                                    _mui.AddItem(_ui);
                                }

                                if (_genNfo)
                                {
                                    UpdateItem _ui = new UpdateItem(_file.FullName, UpdateItemType.Nfo);
                                    _mui.AddItem(_ui);
                                }

                                if (_doExports)
                                {
                                    UpdateItem _ui = new UpdateItem(_file.FullName, UpdateItemType.ImagesExport);
                                    _mui.AddItem(_ui);
                                }

                                // generate the dummy file (if required)
                                FileManager.GenerateDummyFile(_file.FullName);
                            }
                        }
                        // if it is required to generate also sheet for parent folder 
                        if (FileManager.Configuration.Options.AutogenerateMoviesheetForParentFolder)
                        {
                            string _pfoldermetadataFilename = FileManager.Configuration.GetParentFolderMetadataPath(_file.FullName, false);
                            if (!string.IsNullOrEmpty(_pfoldermetadataFilename) && File.Exists(_pfoldermetadataFilename))
                            {
                                _mui = new MetadataUpdateItem(_file.FullName, _pfoldermetadataFilename);
                                _dispatcher.Add(_mui);

                                UpdateItem _ui = new UpdateItem(_file.FullName, UpdateItemType.ParentFoldersheet);
                                _ui.Template = templates[2];
                                _mui.AddItem(_ui);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            MovieItem _movieItem = FileManager.GetMovieByFilePath(_file.FullName);
                            FileManager.SetMovieItemStatus(_movieItem, MovieItemStatus.Exception);
                            Loggy.Logger.DebugException(string.Format("Processing file {0}", _file.FullName), ex);
                        }
                        catch { }
                    }
                }

                // remove all Items from dispatcher that have no UpdateItems inside (Items.Count = 0)
                _dispatcher.RemoveEmptyItems();

                // update status for movies that will not be processed (no metadata or nothing to do)
                foreach (FileInfo _file in movieFiles)
                {
                    if (!_dispatcher.HasWorkForMovie(_file.FullName))
                    {
                        try
                        {
                            MovieItem _movieItem = FileManager.GetMovieByFilePath(_file.FullName);
                            FileManager.SetMovieItemStatus(_movieItem, MovieItemStatus.MetadataMissing);
                        }
                        catch { }
                    }
                }

                // at this point we know what we need to do (_dispatcher has all items that needs to be generated)
                // decide how many threads can start generating results (sheets or thumbs)

                int _maxThreads = GetMaxUsableThreads();
                Loggy.Logger.Debug(string.Format("UpdateManager will use {0} thread(s)", _maxThreads));

                ManualResetEvent[] _doneEvents = new ManualResetEvent[_dispatcher.Count];

                FileManager.ShowAdorner("Processing... Please wait...", true);
                try
                {
                    using (Pool _pool = new Pool(_maxThreads, ThreadPriority.BelowNormal, true))
                    {
                        int _i = 0;
                        foreach (MetadataUpdateItem _item in _dispatcher)
                        {
                            // if cancellation was approved, jump out
                            if (CancelProcessing.WaitOne(20))
                            {
                                return;
                            }

                            _doneEvents[_i] = new ManualResetEvent(false);
                            _item.DoneEvent = _doneEvents[_i];
                            _pool.QueueTask(new Action(_item.ThreadPoolCallback));
                            _i++;
                            Helpers.DoEvents();
                            Thread.Sleep(20);
                        }

                        // go
                        _pool.IsPaused = false;

                        // Wait for all threads in pool to finish
                        bool _b = false;
                        while (!_b)
                        {
                            if (CancelProcessing.WaitOne(20))
                            {
                                break;
                            }
                            if (FileManager.CancellationPending)
                            {
                                FileManager.CancellationPending = false;
                                if (MessageBox.Show("Are you sure you want to stop the process?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                                {
                                    _pool.ClearTasks();
                                    CancelProcessing.Set(); // signal to the other threads that job was cancelled
                                    return;
                                }
                                else
                                {
                                    FileManager.ShowAdorner("Processing... Please wait...", true);
                                }
                            }
                            _b = true;
                            foreach (WaitHandle _handle in _doneEvents)
                            {
                                bool _cb = _handle.WaitOne(50);
                                if (!_cb)
                                {
                                    _b = false;
                                }
                            }

                            Helpers.DoEvents();
                            Thread.Sleep(50);
                        }
                    }
                }
                finally
                {
                    FileManager.HideAdorners();
                }
            }
            finally
            {
                FileManager.Configuration.Options.AutogenerateMoviesheetMetadata = _autogenerateMetadata;
                _dispatcher.Clear();
            }
        }

        private static int GetMaxUsableThreads()
        {
            int _result = 1;
            int _CPUCount = Environment.ProcessorCount;
            if (_CPUCount > 4)
            {
                _CPUCount = 4;
            }

            // if no multicore enabled and more CPUs OR just one CPU available then use main thread with a synchronous approach
            if ((!FileManager.Configuration.Options.EnableMultiCoreSupport && _CPUCount > 1) || _CPUCount == 1)
            {
                _result = 1;
            }
            else
            {
                _result = _CPUCount;
            }

            return _result;
        }
    }
}
