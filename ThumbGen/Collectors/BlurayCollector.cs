using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.BLURAYCOM)]
    internal class BlurayCollector: BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.BLURAYCOM; }
        }

        public override Country Country
        {
            get { return Country.USA; }
        }

        public override string Host
        {
            get { return "http://blu-ray.com"; }
        }

        public override string Tooltip
        {
            get
            {
                return "";
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

        protected override string SearchMask
        {
            get
            {
                return "http://www.blu-ray.com/search/?quicksearch=1&quicksearch_country=US&quicksearch_keyword={0}&section=bluraymovies";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<a href=\"(http://www.blu-ray.com/movies/([^\\\"]*)/([0-9]*))/\">";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "href=\"[^\\?]+\\?year=\\d+\" rel=\"nofollow\">(?<Year>\\d+)</a>";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "genre=(?<Genre>[^\"]+)\" rel=\"nofollow\" title";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "</center><br>(?<Plot>.*?)<br><br>";
            }
        }

        protected override string StudiosRegex
        {
            get
            {
                return "studioid=\\d+\" rel=\"nofollow\">(?<Studio>[^<]+)</a> \\|";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "<title>(?<Title>[^<]+)</title>";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "<meta name=\"description\" content=\"(?<OriginalTitle>[^\\)]*) \\(\\d{4}\\)";
            }
        }

        protected override string RuntimeRegex
        {
            get
            {
                return "\\| (?<Runtime>\\d+) min \\|";
            }
        }

        protected override string RatingRegex
        {
            get
            {
                return "color: #111111\">(?<Rating>[^<]+)</font>";
            }
        }

        protected string CertificationRegex
        {
            get
            {
                return "\\| Rated (?<Certif>[^\\|]+)\\|";
            }
        }

        protected override string VisualSectionRegex
        {
            get
            {
                //return "<img src=\"(http://images\\d*\\.static-bluray\\.com/reviews/\\d+_\\d+\\.[^\"]+)\"";
                return "id=\"screenshot\\d*\" src=\"(?<Link>http://images\\d*\\.static-bluray\\.com/reviews/[^\\.]+\\.jpg)\" width=\"(?<Width>\\d*)\" height=\"(?<Height>\\d*)\"";
            }
        }

        private string RemoveTitleNoise(string title)
        {
            return title.Replace("-Blu-ray", "").Replace("Blu-ray", "").Trim('-');
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = new MovieInfo();

            _result.Name = RemoveTitleNoise(GetItem(input, TitleRegex, "Title")).Trim();
            _result.OriginalTitle = RemoveTitleNoise(GetItem(input, OriginalTitleRegex, "OriginalTitle")).Trim();
            if (string.IsNullOrEmpty(_result.OriginalTitle))
            {
                _result.OriginalTitle = _result.Name;
            }
            _result.Year = GetItem(input, YearRegex, "Year");
            //_result.IMDBID = GetItem(input, IMDBIdRegex, 1);
            _result.Runtime = GetItem(input, RuntimeRegex, "Runtime");
            _result.Overview = GetItem(input, PlotRegex, "Plot", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
            _result.Overview = _result.Overview.Replace("\r\n", "");
            _result.Rating = GetItem(input, RatingRegex, "Rating").Replace(",", ".");
            if (!string.IsNullOrEmpty(_result.Rating))
            {
                _result.Rating = (_result.dRating * 2).ToString();
            }
            _result.Certification = GetItem(input, CertificationRegex, "Certif").Trim();
            _result.Genre.AddRange(GetItems(input, GenresRegex, "Genre"));
            _result.Studios.AddRange(GetItems(input, StudiosRegex, "Studio"));

            return _result;
        }

        protected override bool ProcessVisualSection(string relLink, MovieInfo movieInfo, string id)
        {
            bool _result = false;

            List<string> _cache = new List<string>();

            // process backdrops here
            //string _backdropsLink = relLink.Insert(relLink.LastIndexOf('/'), "-Screenshots");
            string _backdropsLink = relLink + "#Screenshots";
            string _backdropPage = Helpers.GetPage(_backdropsLink, Encoding.UTF8, true);
            if (!string.IsNullOrEmpty(_backdropPage))
            {
                Regex _reg = new Regex(VisualSectionRegex, RegexOptions.IgnoreCase);
                foreach (Match _m2 in _reg.Matches(_backdropPage))
                {
                    string _originalUrl = null;
                    string _thumbUrl = _m2.Groups["Link"].Value;
                    if (!string.IsNullOrEmpty(_thumbUrl))
                    {
                        _originalUrl = _thumbUrl.Insert(_thumbUrl.LastIndexOf('.'), "_large");
                    }

                    // avoid duplicates returned by regex
                    if (!_cache.Contains(_thumbUrl))
                    {
                        if (!string.IsNullOrEmpty(movieInfo.Name) && !string.IsNullOrEmpty(_thumbUrl) && !string.IsNullOrEmpty(_originalUrl))
                        {
                            BackdropItem _bi = new BackdropItem(id, null, this.CollectorName, _thumbUrl, _originalUrl);
                            _bi.SetSize("1280", "720");
                            BackdropsList.Add(_bi);
                            _result = true;
                            _cache.Add(_thumbUrl);
                        }
                    }
                }
            }

            return _result;
        }

        private bool ProcessPage(string input, string id, bool skipImages, string link, string title)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(input))
            {
                MovieInfo _movieInfo = GetMovieInfo(input);

                if (!IsValidYear(_movieInfo.Year))
                {
                    return false;
                }

                string _imageUrl = string.Format("http://images.blu-ray.com/movies/covers/{0}_front.jpg", id);

                if (!string.IsNullOrEmpty(_movieInfo.Name))
                {
                    ResultMovieItem _movieItem = new ResultMovieItem(id, _movieInfo.Name, _imageUrl, this.CollectorName);
                    _movieItem.MovieInfo = _movieInfo;
                    _movieItem.CollectorMovieUrl = link;
                    ResultsList.Add(_movieItem);
                    _result = true;
                }

                if (!skipImages && !string.IsNullOrEmpty(VisualSectionRegex))
                {
                    // process the VisualSection if any
                    bool _res = ProcessVisualSection(link, _movieInfo, id);
                    if (_res)
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

            string page = Helpers.GetPage(string.Format(SearchMask, HttpUtility.UrlEncode(Encoding.GetEncoding("ISO-8859-1").GetBytes(keywords))), Encoding.UTF8, true);
            Regex regex = new Regex(SearchListRegex, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(page))
            {
                foreach (Match match in regex.Matches(page))
                {
                    if (FileManager.CancellationPending)
                    {
                        return ResultsList.Count != 0;
                    }
                    try
                    {
                        string _link = match.Groups[1].Value;
                        string _title = RemoveTitleNoise(match.Groups[2].Value);
                        if (!string.IsNullOrEmpty(_title))
                        {
                            _title = _title.Replace("-", " ");
                        }
                        string _id = match.Groups[3].Value;
                        //string _imageUrl = string.Format("http://images.blu-ray.com/movies/covers/{0}_front.jpg", _id);

                        string _page = Helpers.GetPage(_link, Encoding.GetEncoding("ISO-8859-1"), true);
                        if (!string.IsNullOrEmpty(_page))
                        {
                            bool _r = ProcessPage(_page, _id, skipImages, _link, _title);
                            if (_r)
                            {
                                _result = true;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Loggy.Logger.DebugException("bluray collector", ex);
                    }
                }
            }

            return _result;
        }
    }
}
