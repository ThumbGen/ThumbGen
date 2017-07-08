using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MovieMeterHelper;
using CookComputing.XmlRpc;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.MOVIEMETER)]
    internal class MovieMeterCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.MOVIEMETER; }
        }

        public override Country Country
        {
            get { return Country.Netherlands; }
        }

        public override string Host
        {
            get { return "http://www.moviemeter.nl"; }
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

        private IMMApi m_apiProxy;
        //private string m_BaseUrl = "http://www.moviemeter.nl/ws";
        private string m_apiKey = "35b7j0vc7s8h1rbr5pfry670zkmkeap7";
        private string m_sessionKey = "";
        private int m_sessionValidTill = 20 * 60;

        private string getSessionKey()
        {
            try
            {
                //sessionValidTill: Seconds since the Unix Epoch, can be used for about 30 minutes 
                if ((new DateTime(1970, 1, 1)).AddSeconds(m_sessionValidTill) < DateTime.Now.ToUniversalTime())
                {
                    m_apiProxy = (IMMApi)XmlRpcProxyGen.Create(typeof(IMMApi));
                    ApiStartSession s = m_apiProxy.StartSession(m_apiKey);
                    m_sessionKey = s.session_key;
                    m_sessionValidTill = s.valid_till;
                }
            }
            catch { }
            return m_sessionKey;
        }

        public override MovieInfo QueryMovieInfo(string imdbId)
        {
            MovieInfo _result = null;

            if (!string.IsNullOrEmpty(imdbId))
            {
                m_apiProxy = (IMMApi)XmlRpcProxyGen.Create(typeof(IMMApi));
                try
                {
                    string _mmId = null;
                    try
                    {
                        _mmId = m_apiProxy.RetrieveByImdb(getSessionKey(), imdbId.Replace("tt", ""));
                    }
                    catch { }
                    if (!string.IsNullOrEmpty(_mmId))
                    {
                        _result = GetMovieInfo(_mmId);
                    }
                }
                finally { }
            }
            return _result;
        }

        protected override MovieInfo GetMovieInfo(string mmid)
        {
            MovieInfo _movieInfo = null;
            try
            {
                if (!string.IsNullOrEmpty(mmid))
                {
                    int _filmId = Int32.Parse(mmid);
                    FilmDetail _details = m_apiProxy.RetrieveDetails(getSessionKey(), _filmId);
                    if (_details != null)
                    {
                        _movieInfo = new MovieInfo();
                        foreach (MovieMeterHelper.FilmDetail.Actor _actor in _details.actors)
                        {
                            _movieInfo.Cast.Add(_actor.name);
                        }
                        foreach (MovieMeterHelper.FilmDetail.Country _country in _details.countries)
                        {
                            _movieInfo.Countries.Add(_country.name);
                        }
                        foreach (MovieMeterHelper.FilmDetail.Director _director in _details.directors)
                        {
                            _movieInfo.Director.Add(_director.name);
                        }
                        foreach (string _genre in _details.genres)
                        {
                            _movieInfo.Genre.Add(_genre);
                        }
                        if (!string.IsNullOrEmpty(_details.imdb))
                        {
                            _movieInfo.IMDBID = "tt" + _details.imdb;
                        }
                        _movieInfo.OriginalTitle = _details.title;
                        if (_details.alternative_titles.Count() != 0)
                        {
                            _movieInfo.Name = _details.alternative_titles[_details.alternative_titles.Count() - 1].title;
                        }
                        if (string.IsNullOrEmpty(_movieInfo.Name))
                        {
                            _movieInfo.Name = _movieInfo.OriginalTitle;
                        }
                        _movieInfo.Overview = _details.plot;
                        _movieInfo.Rating = _details.average;
                        try
                        {
                            if (!string.IsNullOrEmpty(_movieInfo.Rating))
                            {
                                _movieInfo.Rating = (2 * _movieInfo.dRating).ToString();
                            }
                        }
                        catch { }
                        if (_details.dates_cinema.Count() != 0)
                        {
                            _movieInfo.ReleaseDate = _details.dates_cinema[0].date;
                        }
                        _movieInfo.Runtime = _details.duration;
                        _movieInfo.Year = _details.year;
                    }
                }

            }
            catch { }
            return _movieInfo;
        }

        private bool GetMovie(string mmid)
        {
            bool _result = false;

            try
            {
                int _filmId = Int32.Parse(mmid);
                if (!string.IsNullOrEmpty(mmid))
                {
                    FilmDetail _details = m_apiProxy.RetrieveDetails(getSessionKey(), _filmId);
                    if (_details != null)
                    {
                        MovieInfo _movieInfo = GetMovieInfo(mmid);

                        if (!IsValidYear(_movieInfo.Year))
                        {
                            return false;
                        }

                        string _imageUrl = string.IsNullOrEmpty(_details.thumbnail) ? null : _details.thumbnail.Replace("/thumbs/", "/");
                        if (_imageUrl != null)
                        {
                            var matches = Regex.Matches(_imageUrl, "(?<A>http://www.moviemeter.nl/images/cover/\\d+/\\d+.)([^\\.]*\\.)(?<B>\\w+)");
                            if (matches.Count != 0)
                            {
                                _imageUrl = matches[0].Groups["A"].Value + matches[0].Groups["B"].Value;
                            }
                        }
                        ResultMovieItem _movieItem = new ResultMovieItem(mmid, _details.title, _imageUrl, this.CollectorName);
                        _movieItem.CollectorMovieUrl = _details.url;
                        _movieItem.MovieInfo = _movieInfo;
                        ResultsList.Add(_movieItem);
                        _result = true;
                    }
                }
            }
            catch { }

            return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            // Start with a connection
            m_apiProxy = (IMMApi)XmlRpcProxyGen.Create(typeof(IMMApi));
            try
            {
                string _mmId = null;

                if (!string.IsNullOrEmpty(imdbID))
                {
                    // get MovieMeter ID from imdb id
                    try
                    {
                        _mmId = m_apiProxy.RetrieveByImdb(getSessionKey(), imdbID.Replace("tt", ""));
                    }
                    catch { }
                    if (!string.IsNullOrEmpty(_mmId))
                    {
                        if (GetMovie(_mmId))
                        {
                            _result = true;
                        }
                    }
                }
                else
                {
                    // search by keywords
                    Film[] _filmlist = null;
                    try
                    {
                        _filmlist = m_apiProxy.Search(getSessionKey(), keywords);
                    }
                    catch { }
                    if (_filmlist != null && _filmlist.Count() != 0)
                    {
                        foreach (Film _f in _filmlist)
                        {
                            //loop trough the filmlist 
                            if (GetMovie(_f.filmId))
                            {
                                _result = true;
                            }
                        }
                    }

                }

            }
            finally
            {
                //m_apiProxy.CloseSession(m_sessionKey);
            }

            return _result;
        }
    }
}
