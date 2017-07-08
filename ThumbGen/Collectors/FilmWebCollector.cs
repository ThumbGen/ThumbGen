using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.FILMWEB)]
    internal class FilmWebCollector: BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.FILMWEB; }
        }

        public override Country Country
        {
            get { return Country.Poland; }
        }

        public override string Host
        {
            get { return "http://www.filmweb.pl"; }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<a class=\"?hdr hdr-medium\"? href=\"(?<Link>/[^\"]*?)\"";
            }
        }

        protected override string SearchMask
        {
            get
            {
                //return "http://www.filmweb.pl/search?q={0}&alias=film";
                return "http://www.filmweb.pl/search/film?q={0}";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "<title>(?<Title>[^\\(]+)\\(";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "href=\"(?<Cover>[^\"]+?)\" class=\"?film_mini\"?>";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "<h2 class=\"text-large caption\">(?<OriginalTitle>[^<]+)</h2";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "\\((?<Year>\\d{4})\\) -";
            }
        }

        //protected override string RuntimeRegex
        //{
        //    get
        //    {
        //        return "class=\"?time\"?>(?<Runtime>[0-9]*?)<";
        //    }
        //}

        protected override string PlotRegex
        {
            get
            {
                return "</div><p class=text>(?<Plot>[^<]+)<";
            }
        }

        protected override string RatingRegex
        {
            get
            {
                return "> (?<Rating>\\d+,\\d+)</strong>/10";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "<a href=\"/search/film\\?genre[^\"]*\">(?<Genre>[^<]*?)</a>";
            }
        }

        protected override string CountryRegex
        {
            get
            {
                return "<a href=\"/search/film\\?countryIds=\\d+\">(?<Country>[^<]+?)</a>";
            }
        }

        //protected override string DirectorRegex
        //{
        //    get
        //    {
        //        return "<meta name=\"?keywords\"? content=\"(?<Director>[^\\s]+\\s[^\\s]+?)\\s";
        //    }
        //}

        //protected override string ActorsRegex
        //{
        //    get
        //    {
        //        return "<img width=\"?\\d+\"? height=\"?\\d+\"? src=\"[^\"]+\" alt=\"(?<Actor>[^\"]+?)\"";
        //    }
        //}

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = new MovieInfo();

            _result.Name = GetItem(input, TitleRegex, "Title").Trim();
            _result.OriginalTitle = GetItem(input, OriginalTitleRegex, "OriginalTitle").Trim().Trim('\r').Trim('\n').Trim('\t').Trim();
            _result.OriginalTitle = string.IsNullOrEmpty(_result.OriginalTitle) ? _result.Name : _result.OriginalTitle;
            _result.Year = GetItem(input, YearRegex, "Year");
            _result.Runtime = GetItem(input, RuntimeRegex, "Runtime");
            _result.Overview = GetItem(input, PlotRegex, "Plot").Trim().Replace("<span>", "").Replace("<p>", "").Replace("<strong>", "").Replace("</strong>", "").Replace("<br />", "").
                            Replace("</p>", "").Replace("<span class=\"source\">", "").Replace("</span>","").Replace("&nbsp;", "").Trim('\n').Trim('\t');
            _result.Rating = GetItem(input, RatingRegex, "Rating").Replace(",", ".");
            _result.Genre.AddRange(GetItems(input, GenresRegex, "Genre"));
            //_result.ReleaseDate = GetItem(input, "data premiery:.*?<strong>(?<1>.*?)</strong>", 1);
            _result.Director.AddRange(GetItems(input, DirectorRegex, "Director"));
            _result.Cast.AddRange(GetItems(input, ActorsRegex, "Actor"));
            _result.Countries.AddRange(GetItems(input, CountryRegex, "Country"));

            return _result;
        }


        protected override bool ProcessVisualSection(string relLink, MovieInfo movieInfo, string id)
        {
            return false;
        }

        private bool ProcessPage(string input, string id, bool skipImages, string link)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(input))
            {
                MovieInfo _movieInfo = GetMovieInfo(input);

                if (!string.IsNullOrEmpty(_movieInfo.Year) && !string.IsNullOrEmpty(this.Year) && (_movieInfo.Year != this.Year))
                {
                    return false;
                }

                string _title = _movieInfo.Name;
                string _imageUrl = GetItem(input, CoverRegex, "Cover");

                if (!string.IsNullOrEmpty(_title))
                {
                    ResultMovieItem _movieItem = new ResultMovieItem(id, _title, _imageUrl, this.CollectorName);
                    _movieItem.MovieInfo = _movieInfo;
                    _movieItem.CollectorMovieUrl = link;
                    ResultsList.Add(_movieItem);
                    _result = true;
                }

                if (!skipImages && !string.IsNullOrEmpty(VisualSectionRegex))
                {
                    // process the VisualSection if any
                    bool _res = ProcessVisualSection(GetItem(input, VisualSectionRegex, 1), _movieInfo, id);
                    if (_res)
                    {
                        _result = true;
                    }
                }
            }

            return _result;
        }

        private static bool m_GotCookie = false;

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            // must get the cookies once and remember them
            if (!m_GotCookie)
            {
                Helpers.GetPage("http://www.filmweb.pl", null, Encoding.UTF8, "", true, true);
            }

            string _s = string.Format("{0}", SearchMask);
            string _resultsPage = Helpers.GetPage(string.Format(_s, keywords.Replace(" ", "+")), null, Encoding.UTF8, "", true, true);
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                Regex _reg = new Regex(SearchListRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (_reg.IsMatch(_resultsPage))
                {
                    // we got the results page
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        string _relLink = _match.Groups["Link"].Value;
                        string _link = string.Format("http://www.filmweb.pl{0}", _relLink); 
                        //string _title = _match.Groups["Title"].Value.Replace("<b>", "").Replace("</b>", "").Trim();
                        string _id = _relLink; // use _relLink as the movies have no ID on this website

                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        string _page = Helpers.GetPage(_link, null, Encoding.UTF8, "", true, true);
                        if (!string.IsNullOrEmpty(_page))
                        {
                            bool _r = ProcessPage(_page, _id, skipImages, _link);
                            if (_r)
                            {
                                _result = true;
                            }
                        }
                    }
                }
                //else
                //{
                //    // direct page
                //    bool _r = ProcessPage(_resultsPage, null, skipImages);
                //    if (_r)
                //    {
                //        _result = true;
                //    }
                //}

            }

            return _result;
        }
    }
}
