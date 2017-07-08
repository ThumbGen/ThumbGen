using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.CINEMAGIA)]
    internal class CinemagiaCollector : BaseCollector
    {
        public CinemagiaCollector()
        {
        }

        public override string CollectorName
        {
            get { return CINEMAGIA; }
        }

        public override Country Country
        {
            get { return Country.Romania; }
        }

        public override string Host
        {
            get { return "http://www.cinemagia.ro"; }
        }

        public override bool SupportsIMDbSearch
        {
            get
            {
                return true;
            }
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

        protected override MovieInfo GetMovieInfo(string url)
        {
            MovieInfo _result = new MovieInfo();
            string _page = Helpers.GetPage(url);
            if (!string.IsNullOrEmpty(_page))
            {
                // title
                Regex _reg = new Regex("class=\"title_1 mb15\">([^\\<]*)</h2>", RegexOptions.IgnoreCase); // group 1 has the Romanian Title
                if (_reg.IsMatch(_page))
                {
                    _result.Name = HttpUtility.HtmlDecode(_reg.Matches(_page)[0].Groups[1].Value);
                }
                // original title
                _reg = new Regex(string.Format("<a href=\"{0}\" title=\"Film - ([^\\\"]*)", url), RegexOptions.IgnoreCase); // group 1 has the Original Title
                if (_reg.IsMatch(_page))
                {
                    _result.OriginalTitle = HttpUtility.HtmlDecode(_reg.Matches(_page)[0].Groups[1].Value);
                }
                // year
                _reg = new Regex("\">\\(([0-9]*)\\)</a>", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    _result.Year = _reg.Matches(_page)[0].Groups[1].Value;
                }
                // director
                _reg = new Regex("title=\"Regia - ([^\\\"]*)", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    string _val = HttpUtility.HtmlDecode(_reg.Matches(_page)[0].Groups[1].Value);
                    if (!string.IsNullOrEmpty(_val))
                    {
                        string[] _dirs = _val.Split(',');
                        if (_dirs != null && _dirs.Count() != 0)
                        {
                            foreach (string _s in _dirs)
                            {
                                _result.Director.Add(_s.Trim());
                            }
                        }
                    }
                }
                // actor
                _reg = new Regex("title=\"Cu - ([^\\\"]*)", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    foreach (Match _m in _reg.Matches(_page))
                    {
                        _result.Cast.Add(HttpUtility.HtmlDecode(_m.Groups[1].Value));
                    }
                }
                // rating 
                _reg = new Regex("ratingGlobalInfo\">[^\\\"]*<span class=\"left\">([0-9.]*)<", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    _result.Rating = HttpUtility.HtmlDecode(_reg.Matches(_page)[0].Groups[1].Value);
                }
                // imdbid
                string _imdbid = nfoHelper.ExtractIMDBId(_page);
                if (!string.IsNullOrEmpty(_imdbid))
                {
                    _result.IMDBID = _imdbid;
                }
                // genre = group 1/2 same info
                string _genres = Helpers.GetSubstringBetweenStrings(_page, "<h3>Gen film</h3>", "\">Ajustează gen</a");
                if (!string.IsNullOrEmpty(_genres))
                {
                    _reg = new Regex("title=\"Filme ([^\\\"]*)\">([^\\<]*)</a><br />", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_genres))
                    {
                        foreach (Match _m in _reg.Matches(_genres))
                        {
                            _result.Genre.Add(HttpUtility.HtmlDecode(_m.Groups[1].Value));
                        }
                    }
                }
                // runtime
                _reg = new Regex("<h3>Durata </h3><span>([0-9]*) m", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    _result.Runtime = HttpUtility.HtmlDecode(_reg.Matches(_page)[0].Groups[1].Value);
                }

                // plot 
                //string _start = "src=\"http://static.cinemagia.ro/img/trailer.gif\"/>";
                string _start = "<span property=\"v:summary\">";
                //string _plot = Helpers.GetSubstringBetweenStrings(_page, _start, "<a href=\"#\" class=\"expand_sinopsis");
                string _plot = Helpers.GetSubstringBetweenStrings(_page, _start, "</div>");
                if (string.IsNullOrEmpty(_plot) || (!string.IsNullOrEmpty(_plot) && _plot.Contains("<a href=")))
                {
                    _plot = Helpers.GetSubstringBetweenStrings(_page, _start, "</span");
                }
                if (!string.IsNullOrEmpty(_plot))
                {
                    _result.Overview = _plot.Substring(_start.Length, _plot.Length - _start.Length);
                    if (!string.IsNullOrEmpty(_result.Overview))
                    {
                        _result.Overview = HttpUtility.HtmlDecode(_result.Overview.Replace("</a>", "").Replace("</p>", "").Replace("<br>", "").Replace("<br/>", "").Replace("<p style=\"text-align: justify;\">", "").Trim());
                        _result.Overview = _result.Overview.Replace("<b>", "").Replace("</b>", "").Replace("<div>", "").Trim();
                    }

                    //_reg = new Regex("<p style=\"text-align: justify;\">([^<]*)", RegexOptions.IgnoreCase);
                    //if (_reg.IsMatch(_page))
                    //{
                    //    _result.Overview = _reg.Matches(_page)[0].Groups[1].Value;
                    //}
                }

                // countries
                string _countries = Helpers.GetSubstringBetweenStrings(_page, "<h3>Ţara</h3>", "<div class=\"float_container_1\">");
                if (!string.IsNullOrEmpty(_countries))
                {
                    _reg = new Regex("title=\"Filme ([^\"]*)\">([^\\<]*)</", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_countries))
                    {
                        foreach (Match _m in _reg.Matches(_countries))
                        {
                            _result.Countries.Add(HttpUtility.HtmlDecode(_m.Groups[2].Value));
                        }
                    }
                }

                // studios
                string _studios = Helpers.GetSubstringBetweenStrings(_page, "<h3>Produs de</h3>", "<h3>Tip Ecran</h3>");
                if (!string.IsNullOrEmpty(_studios))
                {
                    _reg = new Regex("<span>([\\W\\w]*)</span>", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_studios))
                    {
                        _result.Studios.Add(HttpUtility.HtmlDecode(_reg.Matches(_studios)[0].Groups[1].Value));
                    }
                }

                // certification
                _reg = new Regex("<img src=\"http://static.cinemagia.ro/img/rating_([^.]*).gif", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    _result.Certification = _reg.Matches(_page)[0].Groups[1].Value.Trim().ToUpperInvariant();
                }

            }

            return _result;
        }

        private string SearchPageRegex = "clearfix\">[\\s]*<a href=\"(?<Link>[^\"]*?/filme/[^\"]*?)\"\\s*?title=\"(?<Title>.*?)\">[\\s]*?<img[^\b]*?/>";

        public override MovieInfo QueryMovieInfo(string imdbId)
        {
            MovieInfo _result = null;

            string input = Helpers.GetPage(string.Format("http://www.cinemagia.ro/cauta/?q={0}&new=1", imdbId));
            if (!string.IsNullOrEmpty(input))
            {
                Match _m = Regex.Match(input, SearchPageRegex);
                if (_m.Success)
                {
                    string sUrl = _m.Groups["Link"].Value;
                    if (!string.IsNullOrEmpty(sUrl))
                    {
                        _result = GetMovieInfo(sUrl);
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
                keywords = imdbID;
            }
            else
            {
                keywords = keywords.Replace(" ", "+");
            }

            string input = Helpers.GetPage(string.Format("http://www.cinemagia.ro/cauta/?q={0}&new=1", keywords));
            if (!string.IsNullOrEmpty(input))
            {
                Regex regex = new Regex(SearchPageRegex);
                if (regex.IsMatch(input))
                {
                    int count = regex.Matches(input).Count;
                    foreach (Match match in regex.Matches(input))
                    {
                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        if (!match.Value.Contains("noimg_main"))
                        {
                            string sUrl = match.Groups["Link"].Value;
                            if (sUrl != "")
                            {
                                string _id = string.Empty;
                                Regex _idRegex = new Regex("-(?<ID>[01023456789]*?)/");
                                if (_idRegex.IsMatch(sUrl))
                                {
                                    _id = _idRegex.Matches(sUrl)[0].Groups["ID"].Value;
                                }
                                string _title = HttpUtility.HtmlDecode(match.Groups["Title"].Value);
                                _title = Helpers.StripHTML(_title);

                                if (!string.IsNullOrEmpty(_title))
                                {
                                    // now it worths to get MovieInfo once
                                    // try to get MovieInfo
                                    MovieInfo _movieInfo = GetMovieInfo(sUrl);

                                    if (!IsValidYear(_movieInfo.Year))
                                    {
                                        continue;
                                    }

                                    // got title and base url, go to postere
                                    string _postersUrl = string.Format("{0}postere/?toate=1", sUrl);
                                    string _galleryPage = Helpers.GetPage(_postersUrl);
                                    if (!string.IsNullOrEmpty(_galleryPage))
                                    {
                                        // extract links to individual posters
                                        Regex _galleryRegex = new Regex("src=\"(?<Link>[^\"]*?/resize/[^\"]*?l-thumbnail_gallery[^\"]*?)\"");
                                        if (_galleryRegex.IsMatch(_galleryPage))
                                        {
                                            

                                            foreach (Match _poster in _galleryRegex.Matches(_galleryPage))
                                            {
                                                string _imageUrl = _poster.Groups["Link"].Value.Replace("resize/", "");
                                                _imageUrl = _imageUrl.Replace("-thumbnail_gallery", "");
                                                if (!string.IsNullOrEmpty(_title) && !string.IsNullOrEmpty(_imageUrl))
                                                {
                                                    ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, _imageUrl, CollectorName);
                                                    _movieItem.CollectorMovieUrl = sUrl;
                                                    _movieItem.MovieInfo = _movieInfo;

                                                    ResultsList.Add(_movieItem);
                                                    _result = true;
                                                }
                                            }
                                        }
                                    } // if _galleryPage

                                    // try to get backdrops
                                    // got title and base url, go to imagini
                                    string _backdropsUrl = string.Format("{0}imagini/?toate=1", sUrl);
                                    string _backdropsPage = Helpers.GetPage(_backdropsUrl);
                                    if (!string.IsNullOrEmpty(_backdropsPage))
                                    {
                                        // extract links to individual thumbnails
                                        Regex _galleryRegex = new Regex("src=\"(?<Link>[^\"]*?/resize/[^\"]*?l-thumbnail_gallery[^\"]*?)\"");
                                        if (_galleryRegex.IsMatch(_backdropsPage))
                                        {
                                            foreach (Match _backdrop in _galleryRegex.Matches(_backdropsPage))
                                            {
                                                string _originalUrl = _backdrop.Groups["Link"].Value.Replace("resize/", "");
                                                _originalUrl = _originalUrl.Replace("-thumbnail_gallery", "");

                                                if (!string.IsNullOrEmpty(_title) && !string.IsNullOrEmpty(_originalUrl))
                                                {
                                                    string _thumbUrl = _originalUrl.Replace("img/db/movie", "img/resize/db/movie");
                                                    if (!string.IsNullOrEmpty(_thumbUrl))
                                                    {
                                                        _thumbUrl = _thumbUrl.Insert(_thumbUrl.LastIndexOf('.'), "-imagine");
                                                    }
                                                    BackdropsList.Add(new BackdropItem(_id, _movieInfo.IMDBID, this.CollectorName, _thumbUrl, _originalUrl));
                                                }
                                            }
                                        }
                                    } // if _backdropsPage
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
