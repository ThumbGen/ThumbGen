using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.MOVIEPLAYERIT)]
    internal class MoviePlayerITCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.MOVIEPLAYERIT; }
        }

        public override Country Country
        {
            get { return Country.Italy; }
        }

        public override string Host
        {
            get { return "http://www.movieplayer.it"; }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "<meta name=\"title\" content=\"(?<Title>[^\"]+)";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "<meta name=\"titolo_originale\" content=\"(?<OriginalTitle>[^\"]+)";
            }

        }

        protected override string CountryRegex
        {
            get
            {
                return "<meta name=\"nazione\" content=\"(?<Country>[^\"]+)";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "<meta name=\"genere\" content=\"(?<Genre>[^\"]+)";
            }
        }

        protected override string ActorsRegex
        {
            get
            {
                return "<meta name=\"cast\" content=\"(?<Actor>[^\"]+)";
            }
        }

        protected override string DirectorRegex
        {
            get
            {
                return "<meta name=\"regia\" content=\"(?<Director>[^\"]+)";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "<meta property=\"og:image\" content=\"(?<Cover>[^\"]+)\"/>";
            }
        }


        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);

            if (!string.IsNullOrEmpty(input))
            {
                // director
                string _director = GetItem(input, this.DirectorRegex, "Director", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                if (!string.IsNullOrEmpty(_director))
                {
                    _result.Director = _director.Split(',').ToTrimmedList();
                }

                // cast
                string _cast = GetItem(input, this.ActorsRegex, "Actor", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                if (!string.IsNullOrEmpty(_cast))
                {
                    _result.Cast = _cast.Split(',').ToTrimmedList();
                }

                // countries
                string _countries = GetItem(input, this.CountryRegex, "Country", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                if (!string.IsNullOrEmpty(_countries))
                {
                    _result.Countries = _countries.Split(',').ToTrimmedList();
                }

                // genres
                string _genres = GetItem(input, this.GenresRegex, "Genre", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                if (!string.IsNullOrEmpty(_genres))
                {
                    _result.Genre = _genres.Split(',').ToTrimmedList();
                }

            }
            return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            HttpWebRequest _req = (HttpWebRequest)WebRequest.Create(string.Format("{0}/ricerca/", Host));
            // Set values for the request back
            //_req.Method = "GET";
            _req.ContentType = "application/x-www-form-urlencoded";
            _req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; .NET CLR 2.0.50727; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; .NET4.0C; .NET4.0E)";

            // Do the request to get the response
            string _strResponse = null;

            _strResponse = Helpers.GetPage(string.Format("{0}/ricerca/film/{1}", Host, string.Format("?q={0}&submit.x=21&submit.y=11", keywords)));

            if (!string.IsNullOrEmpty(_strResponse))
            {
                // get the request id (eg. http://www.movieplayer.it/ricerca/dGhlIHlvdW5nIHZpY3Rvcmlh/1/ )
                string _reqId = _req.Address.OriginalString;
                if (!string.IsNullOrEmpty(_reqId))
                {
                    _reqId = _reqId.Replace("http://www.movieplayer.it/ricerca/", "").Replace("/1/", "");
                    //if (!string.IsNullOrEmpty(_reqId))
                    {
                        // requery just for the movies page
                        _strResponse = string.IsNullOrEmpty(_reqId) ? _strResponse : Helpers.GetPage(string.Format("{0}/ricerca/film/{1}/", Host, _reqId));
                        if (!string.IsNullOrEmpty(_strResponse))
                        {
                            Regex _reg = new Regex("<a href=\"(?<RelLink>/film/(?<Id>[^/]*)/[^\"]*)\">(?<Title>[^\"]+)</a>[^\"]*<em>trama:</em>(?<Plot>.*?)(<|\\[)", RegexOptions.IgnoreCase);
                            if (_reg.IsMatch(_strResponse))
                            {
                                List<string> _IDs = new List<string>();

                                foreach (Match _match in _reg.Matches(_strResponse))
                                {
                                    if (FileManager.CancellationPending)
                                    {
                                        return ResultsList.Count != 0;
                                    }
                                    try
                                    {
                                        string _id = _match.Groups["Id"].Value;

                                        if (_IDs.Contains(_id))
                                        {
                                            continue; // avoid duplicates
                                        }

                                        _IDs.Add(_id);

                                        string _relLink = _match.Groups["RelLink"].Value;
                                        string _title = HttpUtility.HtmlDecode(_match.Groups["Title"].Value).Replace("\n\t", "").Replace("</strong>", "").Replace("<strong>", "").Trim();
                                        string _year = string.Empty;
                                        Regex _yearEx = new Regex("\\(([0-9]*)\\)", RegexOptions.IgnoreCase);
                                        if (_yearEx.IsMatch(_title))
                                        {
                                            _year = _yearEx.Matches(_title)[0].Groups[1].Value;
                                        }

                                        if (!IsValidYear(_year))
                                        {
                                            continue;
                                        }

                                        _title = _yearEx.Replace(_title, "").Trim(new char[] { '\r', '\n', ' ' });
                                        _title = _title.Replace("<strong>", "").Replace("</strong>", "");
                                        string _plot = HttpUtility.HtmlDecode(_match.Groups["Plot"].Value).Trim().Replace("<strong>", "").Replace("</strong>", "");
                                        string _moviePageLink = string.Format("{0}{1}", Host, _relLink);
                                        // load the gallery and check if u can find some posters
                                        string _moviePage = Helpers.GetPage(_moviePageLink);
                                        if (!string.IsNullOrEmpty(_moviePage))
                                        {
                                            MovieInfo _movieInfo = GetMovieInfo(_moviePage);

                                            _movieInfo.Name = _title;

                                            if (string.IsNullOrEmpty(_movieInfo.Year))
                                            {
                                                _movieInfo.Year = _year;
                                            }

                                            _movieInfo.Overview = _plot;

                                            if (skipImages)
                                            {
                                                if (!string.IsNullOrEmpty(_title))
                                                {
                                                    ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, null, this.CollectorName);
                                                    _movieItem.CollectorMovieUrl = _moviePageLink;
                                                    _movieItem.MovieInfo = _movieInfo;
                                                    ResultsList.Add(_movieItem);
                                                    _result = true;
                                                }

                                            }
                                            else
                                            {
                                                string _imageUrl = GetCoverLink(_moviePage);
                                                if (!string.IsNullOrEmpty(_imageUrl))
                                                {
                                                    _imageUrl = _imageUrl.Replace("_medium", "");
                                                }

                                                if (!string.IsNullOrEmpty(_title))
                                                {
                                                    ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, _imageUrl, this.CollectorName);
                                                    _movieItem.CollectorMovieUrl = _moviePageLink;
                                                    _movieItem.MovieInfo = _movieInfo;
                                                    ResultsList.Add(_movieItem);
                                                    _result = true;
                                                }
                                            }
                                        }

                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }

            return _result;
        }
    }
}
