using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using ThumbGen.Subtitles;
using System.Text.RegularExpressions;
using System.Web;
using System.Threading;
using ThumbGen.MovieSheets;
using System.Reflection;

namespace ThumbGen
{


    public abstract class BaseCollector : DependencyObject
    {
        public const string AMAZON_MUSIC = "Amazon.com(music)";
        public const string AMAZON = "Amazon.com";
        public const string AMAZONDE = "Amazon.de";
        public const string AMAZONFR = "Amazon.fr";
        public const string AMAZONCOUK = "Amazon.co.uk";
        public const string AMAZONCA = "Amazon.ca";
        public const string THEMOVIEDB = "TheMovieDB.org";
        public const string THETVDB = "TheTVDB.com";
        public const string FILMPOSTERARCHIV = "Filmposter-Archiv.de";
        public const string ALPACINE = "Alpacine.com";
        public const string CINEMAGIA = "Cinemagia.ro";
        public const string BLURAYCOM = "Blu-ray.com";
        public const string FILMAFFINITYEN = "FilmAffinity.com";
        public const string FILMAFFINITYES = "FilmAffinity.com/es";
        public const string MOVIEPLAYERIT = "movieplayer.it";
        public const string CINEMAPTGATE = "Cinema.PTGate.pt";
        public const string CSFD = "CSFD.cz";
        public const string MOVIEPOSTERDB = "MoviePosterDB.com";
        public const string OFDB = "OFDb.de";
        public const string MOVIEMETER = "MovieMeter.nl";
        public const string CINEPASSION = "Cine-Passion";
        public const string FILMWEB = "FilmWeb.pl";
        public const string SRATIM = "sratim.co.il";
        public const string OUTNOW = "OutNow.ch";
        public const string PORTHU = "PORT.hu";
        public const string KINOPOISK = "KinoPoisk.ru";
        public const string VIDEOSNAP = "My Own Thumbnail";
        public const string OWNFROMDISK = "My Own Thumbnail From Disk";
        public const string GALLERY = "My Gallery";
        public const string ANTMOVIECATALOG = "ANT Movie Catalog";
        public const string COLLECTORZMOVIE = "Collectorz.com Movie";
        public const string FILMIVEEB = "Filmiveeb.ee";
        public const string CINEMARXRO = "CinemaRx.ro";
        public const string DAUMNET = "Daum.net";
        public const string INTERFILMES = "Interfilmes.com";
        public const string SINEMALAR = "Sinemalar.com";
        public const string FILMZDK = "Filmz.dk";
        public const string TORECNET = "Torec.net";
        public const string ALLOCINE = "Allocine.fr";

        private const string DEFAULT_PROMPT = "type some keywords here";

        public abstract string CollectorName { get; }

        public virtual bool SupportsIMDbSearch
        {
            get
            {
                return false;
            }
        }

        public virtual bool SupportsBackdrops
        {
            get
            {
                return false;
            }
        }

        public virtual bool SupportsMovieInfo
        {
            get
            {
                return false;
            }
        }

        public abstract Country Country { get; }

        public virtual string Tooltip
        {
            get
            {
                return string.Empty;
            }
        }

        protected virtual string IDRegex { get; set; }

        protected virtual string GenresRegex { get; set; }

        protected virtual string CountryRegex { get; set; }

        protected virtual string StudiosRegex { get; set; }

        protected virtual string DirectorRegex { get; set; }

        protected virtual string ActorsRegex { get; set; }

        protected virtual string PlotRegex { get; set; }

        protected virtual string IMDBIdRegex { get; set; }

        protected virtual string RuntimeRegex { get; set; }

        protected virtual string YearRegex { get; set; }

        protected virtual string ReleaseDateRegex { get; set; }

        protected virtual string OriginalTitleRegex { get; set; }

        protected virtual string RatingRegex { get; set; }

        protected virtual string CoverRegex { get; set; }

        protected virtual string SearchListRegex { get; set; }

        protected virtual string TitleRegex { get; set; }

        protected virtual string SearchMask { get; set; }

        protected virtual string VisualSectionRegex { get; set; }

        protected virtual string PostersRegex { get; set; }

        protected virtual string BackdropsRegex { get; set; }

        internal static string GetItem(string input, string regex, object group)
        {
            return GetItem(input, regex, group, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }

        internal static string GetItem(string input, string regex, object group, RegexOptions flags)
        {
            string _result = string.Empty;

            if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(regex))
            {
                Regex _reg = new Regex(regex, flags);
                if (_reg.IsMatch(input))
                {
                    if (group is string)
                    {
                        string _gs = group as string;
                        _result = HttpUtility.HtmlDecode(_reg.Match(input).Groups[_gs].Value.Trim());
                    }
                    if (group is int)
                    {
                        int _i = (int)group;
                        _result = HttpUtility.HtmlDecode(_reg.Match(input).Groups[_i].Value.Trim());
                    }
                }
            }

            return _result;
        }

        internal static List<string> GetItems(string input, string regex, object group)
        {
            return GetItems(input, regex, group, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }

        internal static List<string> GetItems(string input, string regex, object group, RegexOptions flags)
        {
            List<string> _result = new List<string>();

            if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(regex))
            {
                Regex _reg = new Regex(regex, flags);
                if (_reg.IsMatch(input))
                {
                    if (group is string)
                    {
                        string _gs = group as string;
                        for (int _i = 0; _i <= (_reg.Matches(input).Count - 1); _i++)
                        {
                            _result.Add(HttpUtility.HtmlDecode(_reg.Matches(input)[_i].Groups[_gs].Value).Replace("  ", " "));
                        }
                    }
                    if (group is int)
                    {
                        int _ig = (int)group;
                        for (int _i = 0; _i <= (_reg.Matches(input).Count - 1); _i++)
                        {
                            _result.Add(HttpUtility.HtmlDecode(_reg.Matches(input)[_i].Groups[_ig].Value).Replace("  ", " "));
                        }
                    }

                }
            }

            return _result;
        }



        //private string m_DestinationPath;
        private MovieItem m_CurrentMovie;
        public MovieItem CurrentMovie
        {
            get
            {
                return m_CurrentMovie;
            }

            set
            {
                m_CurrentMovie = value;
            }
        }

        protected string Keywords { get; set; }
        public string SearchTime { get; set; }
        public string IMDBID { get; set; }
        public string Year { get; set; }

        protected TemplatesManager m_TemplatesManager = new TemplatesManager();

        public BaseCollector()
        {
            ResultsList = new ResultMovieItemCollection<ResultMovieItem>();
            BackdropsList = new Collection<BackdropBase>();
            FolderToSkip = null;
            FolderCompleteSkipped = false;
        }

        public event EventHandler Processing;
        public event EventHandler Processed;
        public event EventHandler ThumbnailCreated;

        private string GetAutomaticKeywords(out string year)
        {
            // if current movie has a season/episode inside its name assume it is part of a series and use ParentFoldername instead of Foldername
            bool _isPartOfSeries = EpisodeData.IsEpisodeFile(CurrentMovie.Filename);

            string _fallbackValue = CurrentMovie.FilenameWithoutExtension.ToLowerInvariant();
            //string _folder = _isPartOfSeries ? Helpers.GetMovieParentFolderName(CurrentMovie.FilePath, _fallbackValue).ToLowerInvariant() : Helpers.GetMovieFolderName(CurrentMovie.FilePath, _fallbackValue).ToLowerInvariant();
            string _folder = _isPartOfSeries ? TVShowsHelper.GetCurrentSeriesRootFolder(CurrentMovie.FilePath).ToLowerInvariant() : Helpers.GetMovieFolderName(CurrentMovie.FilePath, _fallbackValue).ToLowerInvariant();
            string _input = FileManager.Configuration.Options.UseFolderNamesForDetection || _isPartOfSeries ? _folder : _fallbackValue;

            // if DVD, try to get the folder name that holds the DVD
            if (Helpers.IsDVDPath(CurrentMovie.FilePath))
            {
                _input = Helpers.GetDVDMovieFolderName(CurrentMovie.FilePath, _input);
            }

            // if bluray go up
            if (Helpers.IsBlurayPath(CurrentMovie.FilePath))
            {
                _input = Helpers.GetBlurayMovieFolderName(CurrentMovie.FilePath, _input);
            }

            return KeywordGenerator.GetKeywords(_input, out year);
        }

        public virtual void ClearOnlyResults()
        {
            this.ResultsList.Clear();
        }

        public virtual void ClearResults()
        {
            this.ResultsList.Clear();
            this.BackdropsList.Clear();
        }

        public abstract string Host { get; }

        protected bool IsValidYear(string year)
        {
            return string.IsNullOrEmpty(year) || string.IsNullOrEmpty(this.Year) || (year == this.Year);

        }

        public ResultMovieItemCollection<ResultMovieItem> ResultsList { get; private set; }

        public Collection<BackdropBase> BackdropsList { get; private set; }

        public abstract bool GetResults(string keywords, string imdbID, bool skipImages);

        public QueryResult ProcessMovie(MovieItem movie)
        {
            Keywords = null;

            return ProcessMovie(movie, Keywords, false, null, null);
        }

        public virtual MovieInfo QueryMovieInfo(string imdbId)
        {
            return new MovieInfo();
        }

        protected virtual bool ProcessVisualSection(string relLink, MovieInfo movieInfo, string id)
        {
            return false;
        }

        protected virtual string GetCoverLink(string input)
        {
            return GetItem(input, CoverRegex, "Cover");
        }

        protected virtual MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = new MovieInfo();

            _result.Name = GetItem(input, TitleRegex, "Title").Trim();
            _result.OriginalTitle = GetItem(input, OriginalTitleRegex, "OriginalTitle").Trim();
            if (string.IsNullOrEmpty(_result.OriginalTitle))
            {
                _result.OriginalTitle = _result.Name;
            }
            _result.Year = GetItem(input, YearRegex, "Year").Trim();
            _result.IMDBID = GetItem(input, IMDBIdRegex, "IMDBID").Trim();
            _result.Runtime = GetItem(input, RuntimeRegex, "Runtime").Trim();
            _result.Overview = GetItem(input, PlotRegex, "Plot").Trim().Trim();
            _result.Rating = GetItem(input, RatingRegex, "Rating").Replace(",", ".").Trim();
            _result.Genre.AddRange(GetItems(input, GenresRegex, "Genre"));
            _result.ReleaseDate = GetItem(input, ReleaseDateRegex, "ReleaseDate");
            _result.Director.AddRange(GetItems(input, DirectorRegex, "Director"));
            _result.Cast.AddRange(GetItems(input, ActorsRegex, "Actor"));
            _result.Countries.AddRange(GetItems(input, CountryRegex, "Country"));
            _result.Studios.AddRange(GetItems(input, StudiosRegex, "Studio"));

            return _result;
        }

        private static SortedDictionary<string, BaseCollector> m_MovieCollectors = new SortedDictionary<string, BaseCollector>();
        public static SortedDictionary<string, BaseCollector> MovieCollectors
        {
            get
            {
                if (m_MovieCollectors.Count == 0)
                {
                    CreateMovieCollectors();
                }
                return m_MovieCollectors;
            }
        }

        static IEnumerable<Type> CollectMovieCollectors(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(MovieCollectorAttribute), true).Length > 0)
                {
                    yield return type;
                }
            }
        }

        public static Type GetCollectorType(string collectorName)
        {
            BaseCollector _bc = GetMovieCollector(collectorName);
            if (_bc != null)
            {
                return _bc.GetType();
            }
            else
            {
                return null;
            }
        }

        private static void CreateMovieCollectors()
        {
            m_MovieCollectors.Clear();
            IEnumerable<Type> _types = CollectMovieCollectors(Assembly.GetExecutingAssembly());
            foreach (Type _t in _types)
            {
                BaseCollector _bc = (BaseCollector)Activator.CreateInstance(_t);
                m_MovieCollectors.Add(_bc.CollectorName, _bc);
            }
        }

        public static BaseCollector GetMovieCollector(string name)
        {
            try
            {
                return MovieCollectors[name];
            }
            catch
            {
                return null;
            }
        }

        [Obsolete]
        public static BaseCollector GetMovieCollectorObject(string collectorName)
        {
            switch (collectorName)
            {
                case BaseCollector.ALPACINE:
                    return new AlpacineCollector();
                case BaseCollector.AMAZON:
                    return new AmazonCollector();
                case BaseCollector.AMAZONDE:
                    return new AmazonCollectorDE();
                case BaseCollector.AMAZONFR:
                    return new AmazonCollectorFR();
                case BaseCollector.AMAZONCOUK:
                    return new AmazonCollectorCoUK();
                case BaseCollector.AMAZONCA:
                    return new AmazonCollectorCA();
                case BaseCollector.ANTMOVIECATALOG:
                    return new ANTMovieCatalogCollector();
                case BaseCollector.BLURAYCOM:
                    return new BlurayCollector();
                case BaseCollector.CINEMAGIA:
                    return new CinemagiaCollector();
                case BaseCollector.CINEMAPTGATE:
                    return new CinemaPTGateCollector();
                case BaseCollector.CINEPASSION:
                    return new CinePassionCollector();
                case BaseCollector.COLLECTORZMOVIE:
                    return new CollectorzComCollector();
                case BaseCollector.CSFD:
                    return new CSFDCollector();
                case BaseCollector.FILMAFFINITYEN:
                    return new FilmAffinityENCollector();
                case BaseCollector.FILMAFFINITYES:
                    return new FilmAffinityESCollector();
                case BaseCollector.FILMIVEEB:
                    return new FilmiveebCollector();
                case BaseCollector.FILMPOSTERARCHIV:
                    return new FilmposterArchivCollector();
                case BaseCollector.FILMWEB:
                    return new FilmWebCollector();
                case BaseCollector.MOVIEMETER:
                    return new MovieMeterCollector();
                case BaseCollector.MOVIEPLAYERIT:
                    return new MoviePlayerITCollector();
                case BaseCollector.MOVIEPOSTERDB:
                    return new MoviePosterDBCollector();
                case BaseCollector.OFDB:
                    return new OFDbCollector();
                case BaseCollector.OUTNOW:
                    return new OutnowCollector();
                case BaseCollector.SRATIM:
                    return new SratimCollector();
                case BaseCollector.THEMOVIEDB:
                    return new TheMovieDbCollector();
                case BaseCollector.THETVDB:
                    return new TheTVDBCollector();
            }
            return null;
        }

        public Window MainWindow;

        private static string FolderToSkip { get; set; }
        private static bool FolderCompleteSkipped { get; set; }

        private QueryResult ProcessIMDBIDAndKeywords(bool skipPrompting, string year)
        {
            QueryResult _result = QueryResult.Done;

            // do not spend time getting imdbid as user wanted to disable search
            if (!FileManager.Configuration.Options.DisableSearch)
            {
                // check also if we are in a series processing session
                if (CurrentSeriesHelper.IsProcessingSeriesItem(CurrentMovie.FilePath) && !string.IsNullOrEmpty(CurrentSeriesHelper.SeriesIMDBID))
                {
                    IMDBID = CurrentSeriesHelper.SeriesIMDBID;
                    Keywords = CurrentSeriesHelper.SeriesName;
                }
                else
                {
                    if (/*do this always FileManager.Configuration.Options.UseIMDbIdWherePossible &&*/ !skipPrompting && string.IsNullOrEmpty(IMDBID))
                    {
                        // check if there is some imdbid specified somewhere
                        IMDBID = nfoHelper.GetIMDBId(CurrentMovie.FilePath);
                        Loggy.Logger.Debug("IMDb Id from nfo: {0}", IMDBID);
                    }

                    // check if the .tgmd file is available
                    if (string.IsNullOrEmpty(IMDBID))
                    {
                        MovieInfo _info = MoviesheetsUpdateManager.CreateManagerForMovie(CurrentMovie.FilePath).GetMovieInfo();
                        if (_info != null)
                        {
                            IMDBID = _info.IMDBID;
                            Year = _info.Year;
                            Loggy.Logger.Debug("From metadata: IMDb Id: {0}; Year: {1]", IMDBID, Year);
                        }
                    }

                    // use IMDB dialog if permitted
                    if (!FileManager.Configuration.Options.PromptBeforeSearch && string.IsNullOrEmpty(IMDBID) && FileManager.Configuration.Options.IMDBOptions.UseIMDbPreselectDialog)
                    {
                        ChooseMovieDialogResult _dresult = ChooseMovieFromIMDb.GetCorrectMovie(this.MainWindow, Keywords, year, true);
                        if (_dresult != null && _dresult.WasSkipMoviePressed)
                        {
                            // must skip current movie!
                            return QueryResult.Skip;
                        }

                        if (_dresult != null && _dresult.MovieInfo != null && !string.IsNullOrEmpty(_dresult.MovieInfo.Name) && !string.IsNullOrEmpty(_dresult.MovieInfo.IMDBID))
                        {
                            IMDBID = _dresult.MovieInfo.IMDBID;
                            Keywords = _dresult.MovieInfo.Name;
                            Year = _dresult.MovieInfo.Year;
                            Loggy.Logger.Debug("IMDb Id from IMDb preselect: {0}", IMDBID);
                        }
                    }

                    if (FileManager.Configuration.Options.UseMovieHashWherePossible && !skipPrompting && string.IsNullOrEmpty(IMDBID))
                    {
                        // try to detect imdb from SubtitlesManager
                        IMDBID = SubtitlesManager.GetImdbId(CurrentMovie.FilePath);
                        Loggy.Logger.Debug("IMDb Id from hash2: {0}", IMDBID);
                    }
                }
            }

            return _result;
        }

        private QueryResult ProcessMovie(MovieItem movie, string keywords, bool skipPrompting, ObservableCollection<ResultItemBase> ownSnapshots, string imdbId)
        {
            if (movie != null)
            {
                Loggy.Logger.Debug(string.Format("Processing {0} keywords={1} imdbid={2}", movie.Filename, keywords, imdbId));
            }

            QueryResult _result = QueryResult.Unknown;
            IMDBID = imdbId;

            if (ownSnapshots == null)
            {
                // if there is a folder2skip set and the new file does not belong to this folder then reset folder2skip, else do nothing
                if (FolderToSkip != null)
                {
                    if (string.Compare(FolderToSkip, Path.GetDirectoryName(movie.FilePath), true) != 0)
                    {
                        FolderToSkip = null;
                        FolderCompleteSkipped = false;
                    }
                    else
                    {
                        return !FolderCompleteSkipped ? QueryResult.Done : QueryResult.Skip;
                    }
                }

                CurrentMovie = movie;

                string _year = null;

                if (string.IsNullOrEmpty(keywords))
                {
                    Keywords = GetAutomaticKeywords(out _year);
                    this.Year = _year;
                }
                else
                {
                    Keywords = keywords;
                }

                // process IMDBId and eventually adjust the keywords
                QueryResult _re = ProcessIMDBIDAndKeywords(skipPrompting, _year);
                if (_re != QueryResult.Done)
                {
                    // if something went wrong in the function, use its return
                    return _re;
                }

                this.ClearResults();
            }

            if (CurrentMovie != null)
            {
                bool _res = false;

                if (ownSnapshots == null)
                {
                    if (FileManager.Configuration.Options.PromptBeforeSearch && !skipPrompting && !FileManager.Configuration.Options.DisableSearch &&
                          (FileManager.Mode == ProcessingMode.Manual || FileManager.Mode == ProcessingMode.SemiAutomatic))
                    {
                        InputBoxDialogResult _ibres = QueryNewKeywords(Keywords, DEFAULT_PROMPT, "Type keywords or IMDB Id to search for:", CurrentMovie.FilePath, false);
                        string _userKeywords = _ibres.Keywords;
                        if (_ibres.Abort)
                        {
                            // abort
                            return QueryResult.Abort;
                        }
                        if (string.IsNullOrEmpty(_userKeywords))
                        {
                            if (_ibres.SkipFolder)
                            {
                                FolderToSkip = CurrentMovie.DirectoryName;
                                FolderCompleteSkipped = true; // user pressed "skip complete folder"
                                return QueryResult.SkipFolder;
                            }
                            else
                            {
                                // skip movie
                                return QueryResult.Skip;
                            }
                        }
                        Keywords = _userKeywords;
                    }

                    // extract IMDBId from Keywords if any
                    string _tmp = nfoHelper.ExtractIMDBId(Keywords);
                    if (!string.IsNullOrEmpty(_tmp))
                    {
                        IMDBID = _tmp;
                        Keywords.Replace(_tmp, "");
                    }

                    // try IMDb here too
                    if (FileManager.Configuration.Options.PromptBeforeSearch && string.IsNullOrEmpty(IMDBID) && FileManager.Configuration.Options.IMDBOptions.UseIMDbPreselectDialog)
                    {
                        ChooseMovieDialogResult _dresult = ChooseMovieFromIMDb.GetCorrectMovie(this.MainWindow, Keywords, null, true);
                        if (_dresult != null && _dresult.WasSkipMoviePressed)
                        {
                            // must skip current movie!
                            return QueryResult.Skip;
                        }

                        if (_dresult != null && _dresult.MovieInfo != null && !string.IsNullOrEmpty(_dresult.MovieInfo.Name) && !string.IsNullOrEmpty(_dresult.MovieInfo.IMDBID))
                        {
                            IMDBID = _dresult.MovieInfo.IMDBID;
                            Keywords = _dresult.MovieInfo.Name;
                            Year = _dresult.MovieInfo.Year;
                        }
                    }

                    // signal processing event
                    if (Processing != null)
                    {
                        Processing(this, new ProcessingEventArgs(Keywords));
                    }

                    // execute the detailed collectors search
                    if (!FileManager.Configuration.Options.DisableSearch)
                    {
                        Loggy.Logger.Debug("Execute search: {0} [{1}]", Keywords, IMDBID);

                        GetResultsHandler _del = new GetResultsHandler(GetResults);
                        IAsyncResult _ar = _del.BeginInvoke(Keywords, IMDBID, false, null/*new AsyncCallback(CallBack)*/, null);

                        while (!_ar.IsCompleted)
                        {
                            Helpers.DoEvents();
                            Thread.Sleep(50);
                        }

                        _res = _del.EndInvoke(_ar);

                        Loggy.Logger.Debug("Search done");
                    }

                    //signal processed event
                    if (Processed != null)
                    {
                        Processed(this, new EventArgs());
                    }
                }

                if (FileManager.Mode == ProcessingMode.Automatic || FileManager.Mode == ProcessingMode.FeelingLucky)
                {
                    // A U T O M A T I C   and   F E E L I N G   L U C K Y

                    DateTime _start = DateTime.UtcNow;
                    Loggy.Logger.Debug("Start automatic processing " + this.CurrentMovie.Filename);

                    _result = QueryResult.Done;

                    if (FileManager.CancellationPending)
                    {
                        FileManager.CancellationPending = false;
                        return QueryResult.Abort;
                    }

                    Executor _executor = new Executor(CurrentMovie.FilePath);
                    ImagesProcessor _imgProcessor = new ImagesProcessor(this.CurrentMovie.FilePath);

                    if (_res && ResultsList.Count != 0 || FileManager.Configuration.Options.DisableSearch || this.IMDBID != null)
                    {
                        UserOptions _options = FileManager.Configuration.Options;
                        BaseCollector _prefCoverCollector = BaseCollector.GetMovieCollector(_options.PreferedCoverCollector);
                        BaseCollector _prefInfoCollector = BaseCollector.GetMovieCollector(_options.PreferedInfoCollector);

                        MovieInfo _movieInfo = null;
                        _movieInfo = GetMovieInfo(_executor); // get movieinfo always, maybe year is there
                        this.Year = string.IsNullOrEmpty(this.Year) && _movieInfo != null ? _movieInfo.Year : this.Year;

                        string _coverUrl = null;
                        _imgProcessor.ImportCover(null, FileManager.Configuration.Options.MovieSheetsOptions.AutopopulateFromMetadata, null);

                        if (string.IsNullOrEmpty(_imgProcessor.CoverPath))
                        {
                            IEnumerable<string> _results = GetRemoteCovers(_prefCoverCollector, _coverUrl, false);
                            _coverUrl = _results != null && _results.Count() != 0 ? _results.First() : string.Empty;
                            _imgProcessor.CoverPath = _coverUrl;
                        }
                        else
                        {
                            _coverUrl = _imgProcessor.CoverPath;
                        }

                        // thumbnail
                        if (_options.AutogenerateThumbnail)
                        {
                            _executor.CreateThumbnail(_coverUrl);
                        }

                        //extra thumbnail
                        if (_options.AutogenerateFolderJpg)
                        {
                            _executor.CreateExtraThumbnail(_coverUrl);
                        }

                        MediaInfoData _mediaData = MediaInfoManager.GetMediaInfoData(CurrentMovie.FilePath);

                        // export cover
                        _executor.ExportCover(_coverUrl);

                        // moviesheets + metadata
                        if (FileManager.EnableMovieSheets && m_TemplatesManager != null)
                        {
                            MovieSheetsGenerator _MainGenerator = new MovieSheetsGenerator(SheetType.Main, CurrentMovie.FilePath);
                            MovieSheetsGenerator _ExtraGenerator = _MainGenerator.Clone(false, SheetType.Extra);
                            MovieSheetsGenerator _SpareGenerator = new MovieSheetsGenerator(SheetType.Spare, CurrentMovie.FilePath);
                            _MainGenerator.MediaInfo = _mediaData;
                            _SpareGenerator.MediaInfo = _mediaData;
                            _MainGenerator.MovieInfo = _movieInfo == null || _movieInfo.IsEmpty ? GetMovieInfo(_executor) : _movieInfo;
                            _SpareGenerator.MovieInfo = _MainGenerator.MovieInfo;

                            _MainGenerator.SelectedTemplate = m_TemplatesManager.GetTemplateItem(FileManager.Configuration.Options.MovieSheetsOptions.TemplateName);
                            _ExtraGenerator.SelectedTemplate = m_TemplatesManager.GetTemplateItem(FileManager.Configuration.Options.MovieSheetsOptions.ExtraTemplateName);
                            _SpareGenerator.SelectedTemplate = m_TemplatesManager.GetTemplateItem(FileManager.Configuration.Options.MovieSheetsOptions.ParentFolderTemplateName);

                            _imgProcessor.MainGenerator = _MainGenerator;
                            _imgProcessor.ExtraGenerator = _ExtraGenerator;
                            _imgProcessor.SpareGenerator = _SpareGenerator;
                            _imgProcessor.Backdrops = GetBackdropCandidates(null, _MainGenerator.MovieInfo.Year);
                            _imgProcessor.ImportImages();

                            if ((_options.AutogenerateMovieSheet || _options.AutogenerateMoviesheetForFolder || _options.AutogenerateMoviesheetMetadata || _options.GenerateParentFolderMetadata || _options.AutogenerateMoviesheetForParentFolder))
                            {
                                // generate and replicate the final moviesheet(s) + metadata
                                if (!MovieSheetsGenerator.RenderAndReplicateFinalMoviesheet(_MainGenerator, _ExtraGenerator, _SpareGenerator, true))
                                {
                                    _result = QueryResult.Skip;
                                }
                            }

                            // export backdrops
                            _executor.ExportBackdrop(_MainGenerator.BackdropTempPath, MoviesheetImageType.Background);

                            _executor.ExportBackdrop(_MainGenerator.Fanart1TempPath, MoviesheetImageType.Fanart1);

                            _executor.ExportBackdrop(_MainGenerator.Fanart2TempPath, MoviesheetImageType.Fanart2);

                            _executor.ExportBackdrop(_MainGenerator.Fanart3TempPath, MoviesheetImageType.Fanart3);

                            _MainGenerator.ClearGarbage();
                            _ExtraGenerator.ClearGarbage();
                            _SpareGenerator.ClearGarbage();
                            _MainGenerator.Dispose();
                            _ExtraGenerator.Dispose();
                            _SpareGenerator.Dispose();
                            _MainGenerator = null;
                            _ExtraGenerator = null;
                            _SpareGenerator = null;
                        }

                        // movie info
                        if (_options.AutogenerateMovieInfo)
                        {
                            _executor.CreateMovieInfoFile(_mediaData, _movieInfo);
                        }

                        DateTime _end = DateTime.UtcNow;
                        Loggy.Logger.Debug("End automatic processing " + this.CurrentMovie.Filename + " in " + (_end - _start).TotalSeconds + " s");
                    }
                    else
                    {
                        // movie was not found in the results list
                        _result = QueryResult.NotFound;
                    }
                }
                else // M A N U A L   OR   S E M I A U T O M A T I C  M O D E  -> Show ResultsPage
                {
                    if (_res && ResultsList.Count != 0 || ownSnapshots != null || FileManager.Configuration.Options.DisableSearch || IMDBID != null)
                    {
                        DialogResult _dialogResult = ResultsListBox.Show(MainWindow, ResultsList, ownSnapshots, CurrentMovie.FilePath, IMDBID, Keywords);
                        if (_dialogResult == null) // skip was pressed
                        {
                            return QueryResult.Skip;
                        }
                        else
                        {
                            switch (_dialogResult.Action)
                            {
                                case ResultsDialogAction.Done:
                                    // some item was selected
                                    if (_dialogResult.Item != null)
                                    {
                                        // everything is created inside the ResultsListBox
                                        if (this.ThumbnailCreated != null)
                                        {
                                            this.ThumbnailCreated(this, new ThumbnailCreatedEventArgs(_result, CurrentMovie));
                                        }

                                        _result = QueryResult.Done;
                                    }
                                    else
                                    {
                                        return QueryResult.Skip;
                                    }
                                    break;
                                case ResultsDialogAction.Skip:
                                    return QueryResult.Skip;
                                    break;
                                case ResultsDialogAction.Aborted:
                                    return QueryResult.Abort;
                                case ResultsDialogAction.ChangeQuery:
                                    _result = ChangeQuery(Keywords, DEFAULT_PROMPT, "Type the new keywords or IMDB Id to search for:", CurrentMovie.FilePath);
                                    if (_result == QueryResult.SkipFolder)
                                    {
                                        FolderToSkip = CurrentMovie.DirectoryName;
                                        FolderCompleteSkipped = true; // user pressed "skip complete folder"
                                        return QueryResult.Skip;
                                    }
                                    break;
                                case ResultsDialogAction.BatchApply:
                                    // processing was already done for this folder, skip all future files until current folder is changed
                                    FolderToSkip = CurrentMovie.DirectoryName;
                                    FolderCompleteSkipped = false;
                                    return QueryResult.Done;
                                case ResultsDialogAction.SkippedCompleteFolder:
                                    FolderToSkip = CurrentMovie.DirectoryName;
                                    FolderCompleteSkipped = true; // user pressed "skip complete folder"
                                    return QueryResult.Skip;
                            }
                        }
                    }
                    else // no items could be found
                    {
                        _result = ChangeQuery(Keywords, DEFAULT_PROMPT, "The movie was not found. Type keywords or IMDB Id to search for:", CurrentMovie.FilePath);
                    }
                } // manual/semiautomatic
            }

            return _result;
        }

        private IEnumerable<string> GetRemoteCovers(bool getAll)
        {
            return GetRemoteCovers(null, string.Empty, getAll);
        }

        private IEnumerable<string> GetRemoteCovers(BaseCollector _prefCoverCollector, string coverUrl, bool getAll)
        {
            List<string> _result = new List<string>();
            if (!string.IsNullOrEmpty(coverUrl))
            {
                _result.Add(coverUrl);
            }
            var movieItems = GetMovieItemsForRemoteCovers(_prefCoverCollector, getAll);

            _result = movieItems.Select(x => x.ImageUrl).ToList();

            return _result;
        }

        private IEnumerable<ResultMovieItem> GetMovieItemsForRemoteCovers(BaseCollector _prefCoverCollector, bool getAll, bool useAllWhenNothingFound = false)
        {
            List<ResultMovieItem> _result = new List<ResultMovieItem>();

            UserOptions _options = FileManager.Configuration.Options;

            IEnumerable<ResultMovieItem> _items = null;
            // choose a poster 

            // if u have imdbid and preferred collector also support imdb then filter out items having it; otherwise use all results set; high restrictive filter first
            if (!string.IsNullOrEmpty(IMDBID) && _prefCoverCollector != null && _prefCoverCollector.SupportsIMDbSearch)
            {
                _items = from c in ResultsList
                         where c.MovieInfo.IMDBID == IMDBID &&
                               c.CollectorName == _options.PreferedCoverCollector &&
                               !string.IsNullOrEmpty(c.ImageUrl)
                         select c;
            }

            // try to filter by preferred collector only
            if ((_items == null || _items.Count() == 0) && _prefCoverCollector != null)
            {
                // now try to take cover only based on pref collector and rank
                _items = from c in ResultsList
                         where c.CollectorName == _options.PreferedCoverCollector &&
                               !string.IsNullOrEmpty(c.ImageUrl)
                         select c;
                // filter by best match here, as u don't have imdbid so u are not sure about movie - only if u have items
                if (_items != null && _items.Count() != 0)
                {
                    _items = GetCoversByBestMatch(_items as ResultMovieItemCollection<ResultMovieItem>, this.Keywords, this.Year);
                }
            }

            // try to filter only by IMDBID
            if (!string.IsNullOrEmpty(IMDBID))
            {
                _items = from c in ResultsList
                         where c.MovieInfo.IMDBID == IMDBID &&
                               !string.IsNullOrEmpty(c.ImageUrl)
                         select c;
            }

            if ((_items == null || _items.Count() == 0))
            {
                // still no cover, then try to rank the results and get a cover from the best match movie
                // passing null items will trigger using ResultsList
                _items = GetCoversByBestMatch(_items as ResultMovieItemCollection<ResultMovieItem>, this.Keywords, this.Year);
            }

            // still nothing...then use the ResultsList
            if (useAllWhenNothingFound && (_items == null || _items.Count() == 0))
            {
                _items = ResultsList.Select(x => x);
            }

            if (_items != null && _items.Count() != 0)
            {
                // check if it is an episode
                string _seasonNumber = EpisodeData.GetEpisodeData(CurrentMovie.Filename).Season;

                if (!string.IsNullOrEmpty(_seasonNumber))
                {
                    // check if there are posters for this season
                    IEnumerable<ResultMovieItem> _seasons = from c in _items
                                                            //where c.Season.ToString() == _seasonNumber
                                                            where c.IsSeasonCover
                                                            select c;
                    if (_seasons != null && _seasons.Count() != 0)
                    {
                        // take always FIRST item to make sure same poster will be used for all episodes of the season
                        _items = new List<ResultMovieItem>() { _seasons.First() };
                    }
                }

                // take a random poster from the list
                if (_items != null && _items.Count() != 0)
                {
                    if (getAll)
                    {
                        //_result.AddRange(_items.Select(item => { return item.ImageUrl; }).ToList());
                        _result.AddRange(_items);
                    }
                    else
                    {
                        ResultMovieItem _chosen = _items.ElementAt(0);
                        if (false) // use maybe a parameter here... for now take always first
                        {
                            Random _rand = new Random();
                            int _index = _rand.Next(0, _items.Count() - 1);
                            _chosen = _items.ElementAt(_index);
                        }
                        if (!_result.Contains(_chosen, new ResultMovieItemComparer()))
                        {
                            _result.Add(_chosen);
                        }
                    }
                }
            }
            return _result;
        }

        public class ResultMovieItemComparer : IEqualityComparer<ResultMovieItem>
        {

            public bool Equals(ResultMovieItem x, ResultMovieItem y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                return x != null && y != null && string.Compare(x.MovieId, y.MovieId, true) == 0 && x.CollectorName == y.CollectorName && x.Title == y.Title;
            }

            public int GetHashCode(ResultMovieItem obj)
            {
                return obj == null ? 0 : obj.GetHashCode();
            }
        }

        private List<BackdropBase> GetBackdropCandidates(string movieid, string year)
        {
            List<BackdropBase> _result = new List<BackdropBase>();

            if (!string.IsNullOrEmpty(this.IMDBID))
            {
                // collect backdrops from all Collectors based on IMDBId
                foreach (BaseCollector _col in (this as AllProvidersCollector).Collectors)
                {
                    _result.AddRange(_col.GetBackdropsByIMDbId(this.IMDBID));
                }
            }

            if (_result.Count == 0)
            {
                // return own backdrops based on movieid 
                if (!string.IsNullOrEmpty(movieid))
                {
                    foreach (BaseCollector _col in (this as AllProvidersCollector).Collectors)
                    {
                        _result.AddRange(_col.GetBackdropsByMovieId(movieid));
                    }
                }
                if (_result.Count == 0)
                {
                    // last chance... search by keywords in every collector and collect backdrops from most relevant movies
                    foreach (BaseCollector _col in (this as AllProvidersCollector).Collectors)
                    {
                        _result.AddRange(_col.GetBackdropsByBestMatch(this.Keywords, year));
                    }
                }
            }


            return _result;
        }

        private MovieInfo GetMovieInfo(Executor executor)
        {
            MovieInfoControl _miControl = new MovieInfoControl();
            _miControl.CurrentMoviePath = executor.MoviePath;
            //_miControl.CurrentMovieItem = new ResultItemBase(

            // preferred info collector
            if (!string.IsNullOrEmpty(FileManager.Configuration.Options.PreferedInfoCollector) && !FileManager.Configuration.Options.MovieSheetsOptions.DisablePreferredInfoCollector)
            {
                _miControl.PrefCollectorInfo = executor.QueryPreferredCollector(this.IMDBID, this.Keywords);
            }
            if (_miControl.PrefCollectorInfo != null)
            {
                this.IMDBID = string.IsNullOrEmpty(this.IMDBID) ? _miControl.PrefCollectorInfo.IMDBID : this.IMDBID;
                //this.Year = string.IsNullOrEmpty(this.Year) ? _miControl.PrefCollectorInfo.Year : this.Year;
            }

            // IMDb Info 
            _miControl.IMDBInfo = executor.QueryIMDB(this.IMDBID, this.Keywords);

            // nfo info + metadata
            _miControl.LoadMyData();

            // select source by priority
            _miControl.SelectInfoSourceByPriority();

            // apply IMDBInfo to the selected source
            if (_miControl.IMDBInfo != null && _miControl.FirstAvailableMovieInfo != null)
            {
                _miControl.FirstAvailableMovieInfo = MovieInfoControl.ApplyIMDbMovieInfoBehaviour(_miControl.FirstAvailableMovieInfo, _miControl.IMDBInfo);
            }

            MovieInfo _result = _miControl.FirstAvailableMovieInfo; // as bindings are not working at this level, we take the info from the FirstAvailableMovieInfo field

            // select something from the resultlist
            if (_result == null || _result.IsEmpty)
            {
                BaseCollector prefCoverCollector = BaseCollector.GetMovieCollector(FileManager.Configuration.Options.PreferedCoverCollector);
                IEnumerable<ResultMovieItem> _results = GetMovieItemsForRemoteCovers(prefCoverCollector, false, true);
                _result = _results != null && _results.Count() != 0 ? _results.First().MovieInfo : null;
            }

            AddCoversAndBackdropsToMovieInfo(_result, _miControl.CurrentMoviePath);

            return _result;
        }

        private string GetItemPath(string path, string moviePath)
        {
            // if the image file has same folder as the movie, remove the directory part
            return Path.GetDirectoryName(path).ToLowerInvariant() == Path.GetDirectoryName(moviePath).ToLowerInvariant() ? Path.GetFileName(path) : path;
        }

        private void AddRemoteCoversAndBackdrops(MovieInfo info)
        {
            info.Covers = this.GetRemoteCovers(true).ToList();
            List<BackdropBase> _tmp = this.GetBackdropCandidates(null, info.Year);
            if (_tmp != null && _tmp.Count != 0)
            {
                info.Backdrops.AddRange(_tmp.Select(x => { return x.OriginalUrl; }));
            }
        }

        private void AddLocalCoversAndBackdrops(MovieInfo info, string moviePath)
        {
            UserOptions _options = FileManager.Configuration.Options;
            string _coverPath = string.Empty;
            bool _c = ConfigHelpers.CheckIfFileExists(moviePath, _options.ExportImagesOptions.AutoExportFolderjpgAsCoverName + _options.ExportImagesOptions.CoverExtension, out _coverPath);
            if (_c || _options.ExportImagesOptions.AutoExportFolderjpgAsCover)
            {
                info.Covers.Add(GetItemPath(_coverPath, moviePath));
            }

            string _backPath = string.Empty;
            bool _b = ConfigHelpers.CheckIfFileExists(moviePath, _options.ExportImagesOptions.AutoExportFanartjpgAsBackgroundName + _options.ExportImagesOptions.BackgroundExtension, out _backPath);
            if (_b || _options.ExportImagesOptions.AutoExportFanartjpgAsBackground)
            {
                info.Backdrops.Add(GetItemPath(_backPath, moviePath));
            }

            string _f1Path = string.Empty;
            bool _f1 = ConfigHelpers.CheckIfFileExists(moviePath, _options.ExportImagesOptions.AutoExportFanart1jpgAsBackgroundName + _options.ExportImagesOptions.Fanart1Extension, out _f1Path);
            if (_f1 || _options.ExportImagesOptions.AutoExportFanart1jpgAsBackground)
            {
                info.Backdrops.Add(GetItemPath(_f1Path, moviePath));
            }

            string _f2Path = string.Empty;
            bool _f2 = ConfigHelpers.CheckIfFileExists(moviePath, _options.ExportImagesOptions.AutoExportFanart2jpgAsBackgroundName + _options.ExportImagesOptions.Fanart2Extension, out _f2Path);
            if (_f2 || _options.ExportImagesOptions.AutoExportFanart2jpgAsBackground)
            {
                info.Backdrops.Add(GetItemPath(_f2Path, moviePath));
            }

            string _f3Path = string.Empty;
            bool _f3 = ConfigHelpers.CheckIfFileExists(moviePath, _options.ExportImagesOptions.AutoExportFanart3jpgAsBackgroundName + _options.ExportImagesOptions.Fanart3Extension, out _f3Path);
            if (_f3 || _options.ExportImagesOptions.AutoExportFanart3jpgAsBackground)
            {
                info.Backdrops.Add(GetItemPath(_f3Path, moviePath));
            }

        }

        private void AddGeneratedMoviesheets(MovieInfo info, string moviePath)
        {
            UserOptions _options = FileManager.Configuration.Options;
            string _mainPath = FileManager.Configuration.GetMoviesheetPath(moviePath, false);
            if (File.Exists(_mainPath) || _options.AutogenerateMovieSheet)
            {
                info.Backdrops.Add(GetItemPath(_mainPath, moviePath));
            }
            string _extraPath = FileManager.Configuration.GetMoviesheetForFolderPath(moviePath, false);
            if (File.Exists(_extraPath) || _options.AutogenerateMoviesheetForFolder)
            {
                info.Backdrops.Add(GetItemPath(_extraPath, moviePath));
            }
            string _sparePath = FileManager.Configuration.GetMoviesheetForParentFolderPath(moviePath, false);
            if (File.Exists(_sparePath) || _options.AutogenerateMoviesheetForParentFolder)
            {
                info.Backdrops.Add(GetItemPath(_sparePath, moviePath));
            }
        }

        public void AddCoversAndBackdropsToMovieInfo(MovieInfo info, string moviePath)
        {
            UserOptions _options = FileManager.Configuration.Options;
            if (info != null && !string.IsNullOrEmpty(moviePath))
            {
                info.Covers.Clear();
                info.Backdrops.Clear();
                switch (_options.NamingOptions.ExportBackdropType)
                {
                    default:
                    case ExportBackdropTypes.DoNotExportImages:
                        break;
                    case ExportBackdropTypes.UseInternetLinks:
                        AddRemoteCoversAndBackdrops(info);
                        break;
                    case ExportBackdropTypes.UseLocalExportedImages:
                        AddLocalCoversAndBackdrops(info, moviePath);
                        break;
                    case ExportBackdropTypes.UseGeneratedMoviesheets:
                        AddGeneratedMoviesheets(info, moviePath);
                        break;
                }
            }

        }

        private delegate bool GetResultsHandler(string keywords, string imdbID, bool skipImages);

        public InputBoxDialogResult QueryNewKeywords(string words, string message, string description, string currentMovieFile, bool showGotoResultsButton)
        {
            return InputBox.Show(this.MainWindow, words, message, description, true, true, currentMovieFile, showGotoResultsButton);
        }

        private QueryResult ChangeQuery(string words, string message, string description, string currentMovieFile)
        {
            InputBoxDialogResult _ibres = QueryNewKeywords(words, message, description, currentMovieFile, true);
            string _newKeywords = _ibres.Keywords;
            string _imdbID = nfoHelper.ExtractIMDBId(_newKeywords);
            if (_ibres.Abort)
            {
                // abort was pressed
                return QueryResult.Abort;
            }
            if (_ibres.GotoResults)
            {
                // jump to the results page
                _imdbID = this.IMDBID;
                return this.ProcessMovie(CurrentMovie, _newKeywords, true, _ibres.Results, _imdbID);
            }
            if (_ibres.Results != null && _ibres.Results.Count != 0)
            {
                // trigger results display using snapshots
                _imdbID = this.IMDBID;
                return this.ProcessMovie(CurrentMovie, _newKeywords, true, _ibres.Results, _imdbID);
            }
            if (!string.IsNullOrEmpty(_newKeywords))
            {
                // resend request for the new title
                return this.ProcessMovie(CurrentMovie, _newKeywords, true, null, _imdbID);
            }
            if (_ibres.SkipFolder)
            {
                return QueryResult.SkipFolder;
            }

            return QueryResult.Skip;
        }

        public List<BackdropBase> GetBackdropsByMovieId(string movieId)
        {
            if (BackdropsList == null)
            {
                return new List<BackdropBase>();
            }
            var _result = from c in BackdropsList
                          where string.Compare(c.MovieId, movieId) == 0
                          select c;
            if (_result != null && _result.Count() != 0)
            {
                return _result.ToList<BackdropBase>();
            }
            else
            {
                return new List<BackdropBase>();
            }
        }

        public IEnumerable<ResultMovieItem> GetCoversByMovieId(string movieId)
        {
            if (ResultsList == null)
            {
                return new List<ResultMovieItem>();
            }
            var _result = from c in ResultsList
                          where !string.IsNullOrEmpty(c.MovieId) &&
                                string.Compare(c.MovieId, movieId) == 0 &&
                                !string.IsNullOrEmpty(c.ImageUrl)
                          select c;
            if (_result != null && _result.Count() != 0)
            {
                return _result.ToList<ResultMovieItem>();
            }
            else
            {
                return new List<ResultMovieItem>();
            }

        }

        public List<BackdropBase> GetBackdropsByIMDbId(string imdbId)
        {
            imdbId = imdbId == null ? string.Empty : imdbId;
            if (BackdropsList == null)
            {
                return new List<BackdropBase>();
            }
            var _result = from c in BackdropsList
                          where string.Compare(c.IMDbId == null ? string.Empty : c.IMDbId, imdbId, true) == 0
                          select c;
            if (_result != null && _result.Count() != 0)
            {
                return _result.ToList<BackdropBase>();
            }
            else
            {
                return new List<BackdropBase>();
            }
        }

        public ResultItemBase GetBestMatchMovie(ResultMovieItemCollection<ResultMovieItem> items, string keywords, string year)
        {
            ResultItemBase _result = null;
            try
            {
                if (string.IsNullOrEmpty(year) && this.CurrentMovie != null)
                {
                    int _year = 0;
                    KeywordGenerator.ExtractYearFromTitle(this.CurrentMovie.FilenameWithoutExtension, false, out _year);
                    if (_year != 0)
                    {
                        year = _year.ToString();
                    }
                }

                if (items == null)
                {
                    items = ResultsList;
                }

                if (items != null && items.Count != 0)
                {
                    //get the best match
                    double _bestRank = 0;
                    foreach (ResultItemBase _re in items)
                    {
                        if (_re != null && _re.MovieInfo != null)
                        {
                            double _currentRank = 0;
                            string _title = string.IsNullOrEmpty(_re.MovieInfo.OriginalTitle) ? _re.MovieInfo.Name : _re.MovieInfo.OriginalTitle;

                            if (!string.IsNullOrEmpty(year) && !string.IsNullOrEmpty(_re.MovieInfo.Year))
                            {

                                _currentRank = LetterPairSimilarity.CompareStrings(year + " " + keywords, _re.MovieInfo.Year + " " + _title);
                            }
                            else
                            {
                                _currentRank = LetterPairSimilarity.CompareStrings(keywords, _title);
                            }

                            if (_currentRank == 1)
                            {
                                // Perfect match
                                _result = _re;
                                break;
                            }
                            if (_currentRank > _bestRank)
                            {
                                _bestRank = _currentRank;
                                _result = _re;
                            }
                        }
                    }
                }
            }
            catch { }
            return _result;
        }

        public List<BackdropBase> GetBackdropsByBestMatch(string keywords, string year)
        {
            ResultItemBase _match = GetBestMatchMovie(null, keywords, year);
            string _bestMatchId = _match == null ? null : _match.MovieId;

            if (!string.IsNullOrEmpty(_bestMatchId))
            {
                return GetBackdropsByMovieId(_bestMatchId);
            }
            else
            {
                return new List<BackdropBase>();
            }
        }

        public IEnumerable<ResultMovieItem> GetCoversByBestMatch(ResultMovieItemCollection<ResultMovieItem> items, string keywords, string year)
        {
            ResultItemBase _match = GetBestMatchMovie(items, keywords, year);
            string _bestMatchId = _match == null ? null : _match.MovieId;
            if (!string.IsNullOrEmpty(_bestMatchId))
            {
                return GetCoversByMovieId(_bestMatchId);
            }
            else
            {
                return new List<ResultMovieItem>();
            }
        }

        private int RankComparison(double a, double b)
        {
            if (a == b)
            {
                return 0;
            }
            if (a < b)
            {
                return 1;
            }
            if (a > b)
            {
                return -1;
            }
            return 0;
        }
    }

    public static class CurrentSeriesHelper
    {
        public static bool IsProcessingSeriesItem(string movieFilepath)
        {
            return !string.IsNullOrEmpty(SeriesID) &&
                   !string.IsNullOrEmpty(SeriesRootFolder) &&
                   string.Compare(SeriesRootFolder, TVShowsHelper.GetCurrentSeriesRootFolder(movieFilepath), true) == 0;
            //string.Compare(SeriesRootFolder, Helpers.GetMovieParentFolderName(movieFilepath, ""), true) == 0;
        }
        public static string SeriesRootFolder { get; set; }
        public static string SeriesName { get; set; }
        public static string SeriesIMDBID { get; set; }
        public static string SeriesID { get; set; }
        public static string Season { get; set; }
        public static string GetSeriesDataXML { get; set; }
        public static Dictionary<int, string> GetPostersData = new Dictionary<int, string>();
        public static Dictionary<int, string> GetEpisodesData = new Dictionary<int, string>();

        public static void Reset()
        {
            GetPostersData.Clear();
            GetEpisodesData.Clear();
            GetSeriesDataXML = null;
            SeriesRootFolder = null;
            SeriesName = null;
            SeriesIMDBID = null;
            SeriesID = null;
            Season = null;
        }
    }

    public enum QueryResult
    {
        Unknown,
        Abort,
        UseSnapshots,
        Resend,
        Skip,
        SkipFolder,
        Done,
        NotFound
    }

    public class MovieCollectorAttribute : System.Attribute
    {
        private string m_Name;
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }

        public MovieCollectorAttribute(string name)
        {
            Name = name;
        }
    }

}
