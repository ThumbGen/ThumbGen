using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.IO;
using System.Web;
using System.Windows;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ThumbGen
{
    internal class TheTVDBSerieItem
    {
        public int ID { get; private set; }
        public string Title { get; private set; }
        public string IMDBId { get; private set; }
        public MovieInfo MovieInfo { get; set; }

        public TheTVDBSerieItem(int id, string title, string imdbid)
        {
            ID = id;
            Title = title;
            IMDBId = imdbid;
        }
    }

    [MovieCollector(BaseCollector.THETVDB)]
    internal class TheTVDBCollector : BaseCollector
    {
        private const string API_KEY = "1E181C7EA0EA1574";

        public TheTVDBCollector()
        {
        }

        public override Country Country
        {
            get { return Country.International; }
        }

        public override string Host
        {
            get { return "http://thetvdb.com"; }
        }

        public override string CollectorName
        {
            get { return THETVDB; }
        }

        public override bool SupportsBackdrops
        {
            get
            {
                return true;
            }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        public override bool SupportsIMDbSearch
        {
            get
            {
                return true;
            }
        }

        /*
         * http://www.thetvdb.com/api/api-key-here/series/series-id-here/banners.xml
         * http://www.thetvdb.com/banners/posters/75340-1.jpg
         * 
         * 
         * http://www.thetvdb.com/api/1E181C7EA0EA1574/series/75340/banners
         * 
         * check banner type
         * 
         * 
         * */

        private bool IsSameSeason(string season1, string season2)
        {
            bool _result = true;
            if (!string.IsNullOrEmpty(season1) && !string.IsNullOrEmpty(season2))
            {
                if (string.Compare(season1, season2, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    _result = false;
                }
            }
            return _result;
        }

        private MemoryStream SendRequest(string url)
        {
            MemoryStream _result = null;
            HttpWebResponse wresp = null;

            wresp = null;
            try
            {
                HttpWebRequest wreq = (HttpWebRequest)WebRequest.Create(url);
                wreq.AllowWriteStreamBuffering = true;
                wreq.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                wreq.Timeout = 15000;

                try
                {
                    wresp = (HttpWebResponse)wreq.GetResponse();
                    _result = new MemoryStream();
                    StreamExtensions.CopyTo(wresp.GetResponseStream(), _result);
                    if (_result != null && _result.CanSeek)
                    {
                        _result.Position = 0;
                    }
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("TVDB.Com SendRequest " + url, ex);
                }
            }
            finally
            {
                if (wresp != null)
                {
                    wresp.Close();
                }
            }

            return _result;
        }

        private string m_MirrorPath;
        private Dictionary<int, TheTVDBSerieItem> m_Series = new Dictionary<int, TheTVDBSerieItem>();

        private string BaseUrl
        {
            get
            {
                return string.Format("{0}/api/{1}/series/", m_MirrorPath, API_KEY);
            }
        }


        private bool GetMirrors()
        {
            bool _result = true;
            //TO BE IMPLEMENTED!

            //string _mirrorsUrl = string.Format("http://www.thetvdb.com/api/{0}/mirrors.xml", API_KEY);
            //MemoryStream _stream = SendRequest(_mirrorsUrl);

            //if (_stream != null && _stream.Length > 0)
            //{
            //    _stream.Position = 0;
            //    XmlDocument _doc = new XmlDocument();
            //    _doc.Load(_stream);

            //}
            //_result = _stream != null;

            m_MirrorPath = Host;

            return _result;
        }

        private string GetSeriesYear(XmlNode movie)
        { 
            string _year = Helpers.GetValueFromXmlNode(movie, "FirstAired");
            try
            {
                _year = Convert.ToDateTime(_year).Year.ToString();
            }
            catch
            {
                _year = null;
            }
            return _year;
        }

        private MovieInfo GetMovieInfo(XmlNode movie, string id)
        {
            MovieInfo _result = new MovieInfo();
            _result.HasRightToLeftDirection = GetFlowDirection() == FlowDirection.RightToLeft;
            _result.TVDBID = id;
            _result.Name = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(movie, "SeriesName"));
            _result.Year = GetSeriesYear(movie);
            _result.IMDBID = Helpers.GetValueFromXmlNode(movie, "IMDB_ID");
            _result.SetReleaseDate(this.GetFormattedDate(Helpers.GetValueFromXmlNode(movie, "FirstAired")));
            _result.Overview = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(movie, "Overview"));

            // check if no rating already
            if (string.IsNullOrEmpty(_result.Rating))
            {
                _result.Rating = Helpers.GetValueFromXmlNode(movie, "Rating");
            }

            string _actorsList = Helpers.GetValueFromXmlNode(movie, "Actors");
            if (!string.IsNullOrEmpty(_actorsList))
            {
                _result.Cast = _actorsList.Split('|').ToTrimmedList();

            }

            string _directorList = Helpers.GetValueFromXmlNode(movie, "Director");
            if (!string.IsNullOrEmpty(_directorList))
            {
                _result.Director = _directorList.Split('|').ToTrimmedList();

            }

            string _genresList = Helpers.GetValueFromXmlNode(movie, "Genre");
            if (!string.IsNullOrEmpty(_genresList))
            {
                _result.Genre = _genresList.Split('|').ToTrimmedList();

            }
            string _studios = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(movie, "Network"));
            if (!string.IsNullOrEmpty(_studios))
            {
                _result.Studios.Add(_studios);
            }
            _result.Runtime = Helpers.GetValueFromXmlNode(movie, "Runtime");

            return _result;
        }

        public string GetCurrentSeriesRootFolder()
        {
            return TVShowsHelper.GetCurrentSeriesRootFolder(this.CurrentMovie.FilePath);
        }

        private bool IsSameSeriesBeingProcessed()
        {
            return TVShowsHelper.IsSameSeriesBeingProcessed(this.CurrentMovie.FilePath, m_EpisodeData);
        }

        private bool GetSeries(string keywords)
        {
            m_Series.Clear();

            MemoryStream _stream = null;

            XmlDocument _doc = new XmlDocument();

            // if we are processing same series then use cache
            if (IsSameSeriesBeingProcessed())
            {
                try
                {
                    _doc.LoadXml(CurrentSeriesHelper.GetSeriesDataXML);
                }
                catch { }
            }
            else
            {
                // if u have imdbid then USE IT!!
                if (!string.IsNullOrEmpty(m_IMDbId))
                {
                    // sample:  http://thetvdb.com/api/GetSeriesByRemoteID.php?imdbid=tt0411008
                    string _seriesUrl = string.Format("{0}/api/GetSeriesByRemoteID.php?imdbid={1}&language={2}", m_MirrorPath, m_IMDbId, FileManager.Configuration.Options.MovieSheetsOptions.TVShowsLanguage);
                    _stream = SendRequest(_seriesUrl);
                    if (_stream == null || _stream.Length == 0)
                    {
                        _seriesUrl = string.Format("{0}/api/GetSeriesByRemoteID.php?imdbid={1}", m_MirrorPath, m_IMDbId);
                        _stream = SendRequest(_seriesUrl);
                    }
                }
                else // no imdbid, use keywords
                {
                    string _seriesUrl = string.Format("{0}/api/GetSeries.php?seriesname={1}&language={2}", m_MirrorPath, keywords, FileManager.Configuration.Options.MovieSheetsOptions.TVShowsLanguage);
                    _stream = SendRequest(_seriesUrl);
                    if (_stream == null || _stream.Length == 0)
                    {
                        _seriesUrl = string.Format("{0}/api/GetSeries.php?seriesname={1}", m_MirrorPath, keywords);
                        _stream = SendRequest(_seriesUrl);
                    }
                }

                if (_stream != null && _stream.Length > 0)
                {
                    _stream.Position = 0;
                    try
                    {
                        _doc.Load(_stream);
                        _stream.Dispose();
                        _stream = null;
                    }
                    catch { }
                }
            }


            XmlNodeList _series = _doc.SelectNodes("//Series");
            if (_series.Count != 0)
            {
                foreach (XmlNode _item in _series)
                {
                    int _id = Convert.ToInt32(Helpers.GetValueFromXmlNode(_item, "seriesid"));
                    string _title = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(_item, "SeriesName"));
                    string _imdb = Helpers.GetValueFromXmlNode(_item, "IMDB_ID");
                    string _year = GetSeriesYear(_item);

                    if (string.IsNullOrEmpty(_imdb) && !IsValidYear(_year))
                    {
                        continue;
                    }

                    // if I have an imdbid and it is not matching the found one, skip this series
                    if (!string.IsNullOrEmpty(m_IMDbId) /*&& !string.IsNullOrEmpty(_imdb)*/ &&
                         m_IMDbId.ToLowerInvariant() != _imdb.ToLowerInvariant())
                    {
                        // this series does not match the provided imdbid
                        continue;
                    }

                    bool _addIt = true;

                    if (IsSameSeriesBeingProcessed())
                    {
                        // if we have series data, we use it for filtering
                        if (!string.IsNullOrEmpty(CurrentSeriesHelper.SeriesID) && (string.Compare(CurrentSeriesHelper.SeriesID, _id.ToString(), true) != 0))
                        {
                            // different ID do not add it
                            _addIt = false;
                        }
                    }
                    else
                    {
                        // we are not anymore in the same series RootFolder, reset CurrentSeriesHelper data
                        CurrentSeriesHelper.Reset();
                        // remember the new series root
                        CurrentSeriesHelper.SeriesRootFolder = GetCurrentSeriesRootFolder();
                        // cache result of the current GetSeriesData
                        CurrentSeriesHelper.GetSeriesDataXML = _doc.OuterXml.ToString();
                    }

                    if (_addIt)
                    {
                        MovieInfo _movieInfo = GetMovieInfo(_item, _id.ToString());

                        if (!m_Series.ContainsKey(_id))
                        {
                            TheTVDBSerieItem _new = new TheTVDBSerieItem(_id, _title, _imdb);
                            _new.MovieInfo = _movieInfo;
                            m_Series.Add(_id, _new);
                        }
                    }
                }
            }

            return m_Series.Count != 0;
        }

        private void GetPosters(TheTVDBSerieItem serieItem)
        {
            XmlDocument _doc = new XmlDocument();
            MemoryStream _stream = null;

            SetCurrentEpisodeRelatedInfo(serieItem.ID, serieItem.MovieInfo);

            // check if cache has the current series XML
            if (CurrentSeriesHelper.GetPostersData.ContainsKey(serieItem.ID))
            {
                _doc.LoadXml(CurrentSeriesHelper.GetPostersData[serieItem.ID]);
            }
            else
            {
                string _postersUrl = string.Format("{0}{1}/banners.xml", BaseUrl, serieItem.ID);
                _stream = SendRequest(_postersUrl);
                if (_stream != null && _stream.Length > 0)
                {
                    //SetCurrentEpisodeRelatedInfo(serieItem.ID, serieItem.MovieInfo);

                    _stream.Position = 0;

                    try
                    {
                        _doc.Load(_stream);
                        _stream.Dispose();
                        _stream = null;

                        // update cache
                        if (!CurrentSeriesHelper.GetPostersData.ContainsKey(serieItem.ID))
                        {
                            CurrentSeriesHelper.GetPostersData.Add(serieItem.ID, _doc.OuterXml.ToString());
                        }
                        else
                        {
                            CurrentSeriesHelper.GetPostersData[serieItem.ID] = _doc.OuterXml.ToString();
                        }
                    }
                    catch { }
                }
            }
            // take just poster and season that is not seasonwide 
            //XmlNodeList _images = _doc.SelectNodes("//Banner[BannerType='poster' or (BannerType='season' and BannerType2='season')]");
            //XmlNodeList _images = _doc.SelectNodes("//Banner[BannerType='poster' or (BannerType='season' ) or BannerType='fanart' or BannerType='series']");
            XmlNodeList _images = null;
            if (FileManager.Configuration.Options.RetrieveBannersAsBackdrops)
            {
                _images = _doc.SelectNodes("//Banner");
            }
            else
            {
                _images = _doc.SelectNodes("//Banner[BannerType='poster' or (BannerType='season' and BannerType2='season') or BannerType='fanart']");
            }
            if (_images.Count != 0)
            {
                foreach (XmlNode _item in _images)
                {
                    // process posters
                    string _type = Helpers.GetValueFromXmlNode(_item, "BannerType");
                    string _type2 = Helpers.GetValueFromXmlNode(_item, "BannerType2");
                    if ((string.Compare(_type, "poster") == 0) || ((string.Compare(_type, "season") == 0) && (string.Compare(_type2, "seasonwide") != 0)))
                    {
                        string _relPath = Helpers.GetValueFromXmlNode(_item.SelectSingleNode("BannerPath"));
                        string _imageUrl = string.Format("{0}/banners/{1}", m_MirrorPath, _relPath);
                        string _seasonNumber = Helpers.GetValueFromXmlNode(_item.SelectSingleNode("Season"));
                        if (!IsSameSeason(_seasonNumber, m_EpisodeData.Season))
                        {
                            continue;
                        }

                        string _id = serieItem.ID.ToString();
                        string _title = serieItem.Title;
                        string _extraText = !string.IsNullOrEmpty(_seasonNumber) ? string.Format(" [Season {0}]", _seasonNumber) : string.Empty;
                        int _seasonNr = 0;
                        try
                        {
                            _seasonNr = !string.IsNullOrEmpty(_seasonNumber) ? Int32.Parse(_seasonNumber) : 0;
                        }
                        catch { }

                        if (!string.IsNullOrEmpty(_title) && !string.IsNullOrEmpty(_relPath))
                        {
                            ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, _imageUrl, CollectorName);
                            _movieItem.ExtraText = _extraText;
                            _movieItem.IsSeasonCover = !string.IsNullOrEmpty(_movieItem.ExtraText);
                            _movieItem.Season = _seasonNr;
                            _movieItem.MovieInfo = serieItem.MovieInfo;
                            _movieItem.CollectorMovieUrl = _id != null ? string.Format("http://thetvdb.com/index.php?tab=series&id={0}", _id) : null;
                            ResultsList.Insert(Math.Max(0, ResultsList.Count - 1), _movieItem);
                        }
                    }
                    else // must be a fanart
                    {
                        string _relPath = Helpers.GetValueFromXmlNode(_item.SelectSingleNode("ThumbnailPath"));
                        string _thumbUrl = string.Format("{0}/banners/{1}", m_MirrorPath, _relPath);
                        string _relPath2 = Helpers.GetValueFromXmlNode(_item.SelectSingleNode("BannerPath"));
                        string _originalUrl = string.Format("{0}/banners/{1}", m_MirrorPath, _relPath2);
                        bool _isWideBanner = Helpers.GetValueFromXmlNode(_item, "BannerType2") == "graphical" || Helpers.GetValueFromXmlNode(_item, "BannerType2") == "blank";
                        if (string.IsNullOrEmpty(_relPath))
                        {
                            _relPath = _relPath2;
                            _thumbUrl = _originalUrl;
                        }

                        string _width = null;
                        string _height = null;
                        string _s = Helpers.GetValueFromXmlNode(_item, "BannerType2");
                        if (!string.IsNullOrEmpty(_s))
                        {
                            Match _m = Regex.Match(_s, "(?<Width>[0-9]+?)x(?<Height>([0-9]*)?)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                            _width = _m.Success ? _m.Groups["Width"].Value : null;
                            _height = _m.Success ? _m.Groups["Height"].Value : null;
                        }

                        if (!string.IsNullOrEmpty(_relPath) && !string.IsNullOrEmpty(_relPath2))
                        {
                            BackdropItem _bi = new BackdropItem(serieItem.ID.ToString(), (serieItem != null ? serieItem.IMDBId : string.Empty), this.CollectorName, _thumbUrl, _originalUrl);
                            _bi.Season = m_EpisodeData.Season;
                            _bi.Episode = m_EpisodeData.Episode;
                            _bi.IsBanner = _isWideBanner;
                            _bi.SetSize(_width, _height);
                            this.BackdropsList.Add(_bi);
                        }
                    }
                }
            }
        }

        private string GetFormattedDate(string date)
        {
            DateTimeFormatInfo _dtfi = new DateTimeFormatInfo();
            _dtfi.DateSeparator = "-";
            _dtfi.ShortDatePattern = "yyyy-MM-dd";
            return Helpers.GetFormattedDate(date, _dtfi);
            //string _result = date;
            //DateTime _out = DateTime.MinValue;
            //DateTimeFormatInfo _dtfi = new DateTimeFormatInfo();
            //_dtfi.DateSeparator = "-";
            //_dtfi.ShortDatePattern = "yyyy-MM-dd";
            //if (DateTime.TryParse(date, _dtfi, DateTimeStyles.None, out _out) && _out != DateTime.MinValue)
            //{
            //    _result = _out.ToString(FileManager.Configuration.Options.CustomDateFormat);
            //}
            //return _result;
        }

        private void SetCurrentEpisodeRelatedInfo(int _id, MovieInfo info)
        {
            if (info != null)
            {
                string _episode = m_EpisodeData.Episode;
                if (!string.IsNullOrEmpty(_episode))
                {
                    XmlDocument _doc = new XmlDocument();
                    if (CurrentSeriesHelper.GetEpisodesData.ContainsKey(_id))
                    {
                        try
                        {
                            _doc.LoadXml(CurrentSeriesHelper.GetEpisodesData[_id]);
                        }
                        catch { }
                    }

                    // get EpisodesList
                    //string _order = null;
                    string _clause = null;
                    string _episodeField = null;
                    string _seasonField = null;
                    string _query = null;
                    switch (m_EpisodeData.Type)
                    {
                        default:
                        case EpisodeType.AiredOrder:
                            //_order = "default";
                            _clause = string.Format("[SeasonNumber='{0}']", m_EpisodeData.Season);
                            _episodeField = "EpisodeNumber";
                            _seasonField = "SeasonNumber";
                            _query = string.Format("//Episode[SeasonNumber='{0}' and EpisodeNumber='{1}']", m_EpisodeData.Season, m_EpisodeData.Episode);
                            break;
                        case EpisodeType.DVDOrder:
                            //_order = "dvd";
                            _clause = string.Format("[DVD_season='{0}']", m_EpisodeData.Season);
                            _episodeField = "DVD_episodenumber";
                            _seasonField = "DVD_season";
                            _query = string.Format("//Episode[(DVD_season='{0}' or DVD_season='{0}.0') and (DVD_episodenumber='{1}' or DVD_episodenumber='{1}.0')]", m_EpisodeData.Season, m_EpisodeData.Episode);
                            break;
                        case EpisodeType.Absolute:
                            //_order = "absolute";
                            _clause = string.Empty;
                            _episodeField = "absolute_number";
                            _seasonField = string.Empty;
                            _query = string.Format("//Episode[absolute_number='{0}']", m_EpisodeData.Episode);
                            break;
                    }

                    XmlNodeList _episodes = _doc.SelectNodes(string.Format("//Episode{0}", _clause));
                    if (_episodes != null && _episodes.Count != 0)
                    {
                        SortedDictionary<int, string> _dict = new SortedDictionary<int, string>();
                        foreach (XmlNode _ep in _episodes)
                        {
                            string _number = TrimValue(Helpers.GetValueFromXmlNode(_ep, _episodeField));
                            string _name = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(_ep, "EpisodeName"));
                            _name = string.IsNullOrEmpty(_name) ? "" : _name;

                            int _nr = 0;
                            if (Int32.TryParse(_number, out _nr) && !_dict.ContainsKey(_nr))
                            {
                                _dict.Add(_nr, _name);
                            }
                            else // no episode number, add it at the beginning
                            {
                                info.Episodes.Add(string.IsNullOrEmpty(_number) ? "" : _number);
                                info.EpisodesNames.Add(_name);
                            }
                        }

                        foreach (KeyValuePair<int, string> _pair in _dict)
                        {
                            info.Episodes.Add(_pair.Key.ToString());
                            info.EpisodesNames.Add(_pair.Value);
                        }
                    }

                    //    string _episodeUrl = string.Format("{0}{1}/{2}/{3}/{4}/{5}.xml", BaseUrl, _id, _order, m_EpisodeData.Season, _episode, FileManager.Configuration.Options.MovieSheetsOptions.TVShowsLanguage);
                    //    MemoryStream _stream = SendRequest(_episodeUrl);
                    //    if (_stream == null || _stream.Length == 0)
                    //    {
                    //        _episodeUrl = string.Format("{0}{1}/{2}/{3}/{4}/", BaseUrl, _id, _order, m_EpisodeData.Season, _episode);
                    //        _stream = SendRequest(_episodeUrl);
                    //    }
                    //    if (_stream != null && _stream.Length > 0)
                    //    {
                    //        _stream.Position = 0;
                    //        try
                    //        {
                    //            _doc.Load(_stream);
                    //        }
                    //        catch { }
                    //    }

                    XmlElement _episodeNode = _doc.SelectSingleNode(_query) as XmlElement;
                    if (_episodeNode != null)
                    {
                        info.Season = string.IsNullOrEmpty(_seasonField) ? string.Empty : Helpers.GetValueFromXmlNode(_episodeNode, _seasonField);
                        info.Episode = TrimValue(Helpers.GetValueFromXmlNode(_episodeNode, _episodeField));
                        info.EpisodeName = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(_episodeNode, "EpisodeName"));
                        info.EpisodePlot = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(_episodeNode, "Overview"));
                        info.SetEpisodeReleaseDate(this.GetFormattedDate(Helpers.GetValueFromXmlNode(_episodeNode, "FirstAired")));
                        info.Rating = Helpers.GetValueFromXmlNode(_episodeNode, "Rating");
                        string _director = Helpers.GetValueFromXmlNode(_episodeNode, "Director");
                        if (!string.IsNullOrEmpty(_director))
                        {
                            info.Director = _director.Split('|', ',').ToTrimmedList().ToListWithoutEmptyItems();
                        }
                        string _writers = Helpers.GetValueFromXmlNode(_episodeNode, "Writer");
                        if (!string.IsNullOrEmpty(_writers))
                        {
                            info.Writers = _writers.Split('|', ',').ToTrimmedList().ToListWithoutEmptyItems();
                        }
                        string _guests = Helpers.GetValueFromXmlNode(_episodeNode, "GuestStars");
                        if (!string.IsNullOrEmpty(_guests))
                        {
                            info.GuestStars = _guests.Split('|', ',').ToTrimmedList().ToListWithoutEmptyItems();
                        }
                    }
                }
            }
        }

        private string TrimValue(string value)
        {
            return string.IsNullOrEmpty(value) ? value : Regex.Replace(value, "(\\.[0-9]+)", "");
        }

        private void GetEpisodes(TheTVDBSerieItem serieItem)
        {
            XmlDocument _doc = new XmlDocument();
            MemoryStream _stream = null;

            // check if cache has the current series XML
            if (CurrentSeriesHelper.GetEpisodesData.ContainsKey(serieItem.ID))
            {
                _doc.LoadXml(CurrentSeriesHelper.GetEpisodesData[serieItem.ID]);
            }
            else
            {
                string _episodesUrl = string.Format("{0}{1}/all/{2}.xml", BaseUrl, serieItem.ID, FileManager.Configuration.Options.MovieSheetsOptions.TVShowsLanguage);
                _stream = SendRequest(_episodesUrl);
                if (_stream == null || _stream.Length == 0)
                {
                    _episodesUrl = string.Format("{0}{1}/all/", BaseUrl, serieItem.ID);
                    _stream = SendRequest(_episodesUrl);
                }
                if (_stream != null && _stream.Length > 0)
                {
                    try
                    {
                        _doc.Load(_stream);
                        _stream.Dispose();
                        _stream = null;

                        // update cache
                        if (!CurrentSeriesHelper.GetEpisodesData.ContainsKey(serieItem.ID))
                        {
                            CurrentSeriesHelper.GetEpisodesData.Add(serieItem.ID, _doc.OuterXml.ToString());
                        }
                        else
                        {
                            CurrentSeriesHelper.GetEpisodesData[serieItem.ID] = _doc.OuterXml.ToString();
                        }
                    }
                    catch { }
                }
            }

            // take just poster and season that is not seasonwide
            XmlNodeList _episodes = _doc.SelectNodes("//Episode");
            if (_episodes.Count != 0)
            {
                string _episodeField = null;
                string _seasonField = null;
                switch (m_EpisodeData.Type)
                {
                    default:
                    case EpisodeType.AiredOrder:
                        _episodeField = "EpisodeNumber";
                        _seasonField = "SeasonNumber";
                        break;
                    case EpisodeType.DVDOrder:
                        _episodeField = "DVD_episodenumber";
                        _seasonField = "DVD_season";
                        break;
                    case EpisodeType.Absolute:
                        _episodeField = "absolute_number";
                        _seasonField = string.Empty;
                        break;
                }

                foreach (XmlNode _item in _episodes)
                {
                    string _relPath = Helpers.GetValueFromXmlNode(_item.SelectSingleNode("filename"));
                    string _imageUrl = string.Format("{0}/banners/{1}", m_MirrorPath, _relPath);

                    string _seasonNumber = string.IsNullOrEmpty(_seasonField) ? string.Empty : Helpers.GetValueFromXmlNode(_item.SelectSingleNode(_seasonField));
                    string _episodeNumber = Helpers.GetValueFromXmlNode(_item.SelectSingleNode(_episodeField));
                    // remove trailing numbers like 2.0 -> 2
                    _seasonNumber = TrimValue(_seasonNumber);
                    _episodeNumber = TrimValue(_episodeNumber);

                    // if same episode and (if defined) same season
                    if ((_episodeNumber == m_EpisodeData.Episode) &&
                         ((!string.IsNullOrEmpty(_seasonNumber) && (_seasonNumber == m_EpisodeData.Season)) || string.IsNullOrEmpty(_seasonNumber))
                       )
                    {
                        string _episodeName = Helpers.GetValueFromXmlNode(_item.SelectSingleNode("EpisodeName"));
                        _episodeName = string.IsNullOrEmpty(_episodeName) ? string.Empty : string.Format(" - {0}", _episodeName);
                        string _id = serieItem.ID.ToString();
                        string _title = serieItem.Title;
                        string _extraText = _seasonNumber != null ? string.Format(" [Season {0}]", _seasonNumber) : string.Empty;
                        //_extraText = _episodeNumber != null && _episodeNumber != "0" ? string.Format("{0} [Episode {1}{2}] (screenshot)", _extraText, _episodeNumber, _episodeName) : _extraText;
                        _extraText = _episodeNumber != null && _episodeNumber != "0" ? string.Format("{0} [Episode {1}]", _extraText, _episodeNumber) : _extraText;

                        if (!string.IsNullOrEmpty(_title) /*&& !string.IsNullOrEmpty(_relPath)*/)
                        {
                            ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, _imageUrl, CollectorName);
                            _movieItem.ExtraText = _extraText;
                            _movieItem.MovieInfo = serieItem.MovieInfo;
                            _movieItem.CollectorMovieUrl = _id != null ? string.Format("http://thetvdb.com/index.php?tab=series&id={0}", _id) : null;
                            ResultsList.Add(_movieItem);

                            if (!string.IsNullOrEmpty(_relPath))
                            {
                                BackdropItem _bi = new BackdropItem(serieItem.ID.ToString(), (serieItem != null ? serieItem.IMDBId : string.Empty), this.CollectorName, _imageUrl, _imageUrl);
                                _bi.Episode = m_EpisodeData.Episode;
                                _bi.Season = m_EpisodeData.Season;
                                _bi.IsScreenshot = true;
                                this.BackdropsList.Insert(0, _bi);
                            }
                        }

                    }
                }
            }
        }

        private FlowDirection GetFlowDirection()
        {
            return FileManager.Configuration.Options.MovieSheetsOptions.TVShowsLanguage == "he" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        private string m_IMDbId = null;
        private EpisodeData m_EpisodeData = null;

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            m_IMDbId = imdbID;

            try
            {
                m_EpisodeData = EpisodeData.GetEpisodeData(this.CurrentMovie.Filename);
            }
            catch { }

            try
            {
                // don't start querying if there's no episode number detected
                if (!string.IsNullOrEmpty(m_EpisodeData.Episode) && GetMirrors())
                {
                    if (GetSeries(keywords))
                    {
                        foreach (KeyValuePair<int, TheTVDBSerieItem> _item in m_Series)
                        {
                            if (FileManager.CancellationPending)
                            {
                                return ResultsList.Count != 0;
                            }

                            if (skipImages)
                            {
                                string _id = _item.Value.ID.ToString();
                                ResultMovieItem _movieItem = new ResultMovieItem(_id, _item.Value.Title, null, CollectorName);
                                _movieItem.MovieInfo = _item.Value.MovieInfo;
                                SetCurrentEpisodeRelatedInfo(Convert.ToInt32(_id), _movieItem.MovieInfo);

                                _movieItem.CollectorMovieUrl = !string.IsNullOrEmpty(_id) ? string.Format("http://thetvdb.com/index.php?tab=series&id={0}", _id) : null;
                                ResultsList.Add(_movieItem);
                            }
                            else
                            {
                                GetEpisodes(_item.Value);
                                GetPosters(_item.Value);
                            }

                            _result = true;
                        }
                    }
                }
            }
            catch { }

            return _result;
        }
    }
}
