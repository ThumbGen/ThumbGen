using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;
using System.Web;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.CINEPASSION)]
    internal class CinePassionCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.CINEPASSION; }
        }

        public override Country Country
        {
            get { return Country.France; }
        }

        public override string Host
        {
            get { return "http://passion-xbmc.org"; }
        }

        public override bool SupportsBackdrops
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

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        private string m_Language = "fr";

        private string APIKEY = "287f0c5e8876c8be6efb26e1401a928a";
        //http://passion-xbmc.org/scraper/API/1/Movie.Search/Query/Lang/Format/APIKEY/XXX
        private string m_BaseUrl = "http://passion-xbmc.org/scraper/API/1/";

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

                wresp = (HttpWebResponse)wreq.GetResponse();

                _result = new MemoryStream();
                StreamExtensions.CopyTo(wresp.GetResponseStream(), _result);
                if (_result != null && _result.CanSeek)
                {
                    _result.Position = 0;
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

        private string QUERY_IMDB = "IMDB";
        private string QUERY_Title = "Title";

        private string GetSearchUrl(string criteria, string query)
        {
            return string.Format("{0}/Movie.Search/{1}/{2}/{3}/{4}/XML/{5}/{6}", m_BaseUrl,
                        FileManager.Configuration.Options.CinePassionOptions.Username,
                        FileManager.Configuration.Options.CinePassionOptions.Pass, query, m_Language, APIKEY, criteria);
        }

        private string GetInfoUrl(string movieId)
        {
            return string.Format("{0}/Movie.GetInfo/{1}/{2}/ID/{3}/XML/{4}/{5}", m_BaseUrl, 
                      FileManager.Configuration.Options.CinePassionOptions.Username,
                      FileManager.Configuration.Options.CinePassionOptions.Pass, m_Language, APIKEY, movieId);
        }

        public override MovieInfo QueryMovieInfo(string imdbId)
        {
            MovieInfo _result = null;

            if (!string.IsNullOrEmpty(imdbId))
            {
                XmlDocument _doc = new XmlDocument();
                try
                {
                    _doc.Load(GetSearchUrl(imdbId.Replace("tt", ""), QUERY_IMDB));
                }
                catch { }
                XmlNodeList _movies = _doc.SelectNodes("//movie");
                if (_movies.Count != 0)
                {
                    string _id = Helpers.GetValueFromXmlNode(_movies[0], "id");

                    GetMovieDetails(_id, true, out _result);
                }
            }
            return _result;
        }

        private MovieInfo GetMovieInfo(XmlNode movieNode)
        {
            MovieInfo _result = new MovieInfo();
            if (movieNode != null)
            {
                XmlNodeList _castingList = movieNode.SelectNodes("casting/person");
                if (_castingList.Count != 0)
                {
                    foreach (XmlNode _actor in _castingList)
                    {
                        string _d = HttpUtility.HtmlDecode(Helpers.GetAttributeFromXmlNode(_actor, "name"));
                        if (!string.IsNullOrEmpty(_d))
                        {
                            _result.Cast.Add(_d);
                        }
                    }
                }
                //_result.Certification

                XmlNodeList _countriesList = movieNode.SelectNodes("countries/country");
                if (_countriesList.Count != 0)
                {
                    foreach (XmlNode _country in _countriesList)
                    {
                        string _d = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(_country));
                        if (!string.IsNullOrEmpty(_d))
                        {
                            _result.Countries.Add(_d);
                        }
                    }
                }

                XmlNodeList _directorsList = movieNode.SelectNodes("directors/director");
                if (_directorsList.Count != 0)
                {
                    foreach (XmlNode _director in _directorsList)
                    {
                        string _d = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(_director));
                        if (!string.IsNullOrEmpty(_d))
                        {
                            _result.Director.Add(_d);
                        }
                    }
                }

                XmlNodeList _genresList = movieNode.SelectNodes("genres/genre");
                if (_genresList.Count != 0)
                {
                    foreach (XmlNode _genre in _genresList)
                    {
                        string _d = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(_genre));
                        if (!string.IsNullOrEmpty(_d))
                        {
                            _result.Genre.Add(_d);
                        }
                    }
                }

                _result.IMDBID = Helpers.GetValueFromXmlNode(movieNode, "id_imdb").PadLeft(7, '0');
                if (!string.IsNullOrEmpty(_result.IMDBID) && !_result.IMDBID.StartsWith("t"))
                {
                    _result.IMDBID = "tt" + _result.IMDBID;
                }
                _result.Name = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(movieNode, "title"));
                _result.OriginalTitle = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(movieNode, "originaltitle"));
                _result.Overview = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(movieNode, "plot"));
                _result.Rating = Helpers.GetValueFromXmlNode(movieNode.SelectSingleNode("//ratings/rating[@type='imdb']"));
                if (string.IsNullOrEmpty(_result.Rating))
                {
                    _result.Rating = Helpers.GetValueFromXmlNode(movieNode.SelectSingleNode("//ratings/rating[@type='allocine']"));
                }
                //_result.ReleaseDate
                _result.Runtime = Helpers.GetValueFromXmlNode(movieNode, "runtime");

                XmlNodeList _studiosList = movieNode.SelectNodes("studios/studio");
                if (_studiosList.Count != 0)
                {
                    foreach (XmlNode _studio in _studiosList)
                    {
                        string _d = HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(_studio));
                        if (!string.IsNullOrEmpty(_d))
                        {
                            _result.Studios.Add(_d);
                        }
                    }
                }

                _result.Year = Helpers.GetValueFromXmlNode(movieNode, "year");

            }
            return _result;
        }

        private bool GetMovieDetails(string movieId, bool onlyInfo, out MovieInfo movieInfo)
        {
            bool _result = false;
            movieInfo = null;

            if (!string.IsNullOrEmpty(movieId))
            {
                XmlDocument _docDetails = new XmlDocument();
                try
                {
                    _docDetails.Load(this.GetInfoUrl(movieId));
                }
                catch { }
                XmlNode _movie = _docDetails.SelectSingleNode("//movie");

                if (_movie != null)
                {
                    MovieInfo _movieInfo = GetMovieInfo(_movie);
                    movieInfo = _movieInfo;

                    if (!onlyInfo)
                    {
                        // get posters
                        if (_movie != null)
                        {
                            XmlNodeList _images = _movie.SelectNodes("//image[@type='Poster' and @size='original']");
                            if (_images.Count != 0)
                            {
                                foreach (XmlNode _image in _images)
                                {
                                    string _imageUrl = Helpers.GetAttributeFromXmlNode(_image, "url");
                                    ResultMovieItem _movieItem = new ResultMovieItem(movieId, _movieInfo.Name, _imageUrl, this.CollectorName);
                                    _movieItem.CollectorMovieUrl = Helpers.GetValueFromXmlNode(_movie, "url");
                                    _movieItem.MovieInfo = _movieInfo;
                                    ResultsList.Add(_movieItem);
                                    _result = true;
                                }
                            }
                            else
                            {
                                ResultMovieItem _movieItem = new ResultMovieItem(movieId, _movieInfo.Name, null, this.CollectorName);
                                _movieItem.CollectorMovieUrl = Helpers.GetValueFromXmlNode(_movie, "url");
                                _movieItem.MovieInfo = _movieInfo;
                                ResultsList.Add(_movieItem);
                                _result = true;
                            }
                            // get fanart/backdrops
                            XmlNodeList _fanarts = _movie.SelectNodes("//image[@type='Fanart' and @size='preview']");
                            if (_fanarts.Count != 0)
                            {
                                foreach (XmlNode _fanart in _fanarts)
                                {
                                    string _fanartId = Helpers.GetAttributeFromXmlNode(_fanart, "id");
                                    if (!string.IsNullOrEmpty(_fanartId))
                                    {
                                        string _thumbUrl = Helpers.GetAttributeFromXmlNode(_movie.SelectSingleNode(string.Format("//image[@id='{0}' and @size='preview']", _fanartId)), "url");
                                        XmlNode _originalNode = _movie.SelectSingleNode(string.Format("//image[@id='{0}' and @size='original']", _fanartId));
                                        string _originalUrl = Helpers.GetAttributeFromXmlNode(_originalNode, "url");
                                        string _width = _originalNode != null ? _originalNode.Attributes["width"].Value : null;
                                        string _height = _originalNode != null ? _originalNode.Attributes["height"].Value : null;


                                        if (!string.IsNullOrEmpty(_thumbUrl) && !string.IsNullOrEmpty(_originalUrl))
                                        {
                                            BackdropItem _bi = new BackdropItem(movieId, _movieInfo.IMDBID, this.CollectorName, _thumbUrl, _originalUrl);
                                            _bi.SetSize(_width, _height);
                                            this.BackdropsList.Add(_bi);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return _result;

        }

        private bool ProcessRequest(string url)
        {
            bool _result = false;

            XmlDocument _doc = new XmlDocument();
            try
            {
                _doc.Load(url);
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Exception cinepassion:", ex);
            }
            XmlNodeList _movies = _doc.SelectNodes("//movie");
            if (_movies.Count != 0)
            {
                foreach (XmlNode _item in _movies)
                {
                    string _id = Helpers.GetValueFromXmlNode(_item, "id");
                    MovieInfo _tmp;
                    if (GetMovieDetails(_id, false, out _tmp))
                    {
                        _result = true;
                    }
                }
            }

            return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(imdbID))
            {
                string _Url = this.GetSearchUrl(imdbID.Replace("tt", ""), QUERY_IMDB);
                if (ProcessRequest(_Url))
                {
                    _result = true;
                }
            }
            else
            {
                string _Url = this.GetSearchUrl(keywords, QUERY_Title);
                if (ProcessRequest(_Url))
                {
                    _result = true;
                }
            }

            return _result;
        }
    }
}
