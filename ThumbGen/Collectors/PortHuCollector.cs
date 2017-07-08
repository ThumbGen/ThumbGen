using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.PORTHU)]
    internal class PortHuCollector:BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.PORTHU; }
        }

        public override Country Country
        {
            get { return Country.Hungary; }
        }

        public override string Host
        {
            get { return "http://port.hu"; }
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
                return "http://www.port.hu/pls/ci/cinema.film_creator?i_text={0}&i_film_creator=1";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                //return "i_film_id=(?<ID>[\\d]*)&amp;i_city_id[^>]*.(?<Title>[^<]*)[^>]*.(?<OriginalTitle>[^<]*)(?<TT>.*?)(?<Year>\\d{4})?\\) <";  // this is TOO COMPLEX and hangs
                return "i_film_id=(?<ID>[\\d]*)(?<TT>[^\"]+)\" target=\"_top\">(?<Title>[^<]*)[^>](?<TT>.*?)(?<Year>\\d{4})?\\) <";
            }
        }

        protected override string IDRegex
        {
            get
            {
                return "i_film_id=(?<ID>[0-9]*)&";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "class=\"blackbigtitle\">(?<Title>[^<]*)";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "class=\"blackbigtitle\">(?<Title>[^<]*)</h1>((.*?)class=\"txt\">\\((?<OriginalTitle>[^<]*)\\))?";
            }
            
        }

        protected override string YearRegex
        {
            get
            {
                return ", (?<Year>19\\d\\d|20\\d\\d)</span";
            }
        }

        protected override string IMDBIdRegex
        {
            get
            {
                return "i_param=(?<IMDBID>\\d{7})\" target=\"top\">IMDb";
            }
        }

        protected override string RuntimeRegex
        {
            get
            {
                return ", (?<Runtime>[0-9]+) perc";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "span class=\"txt\">(?<Plot>\\b[\\S].*?)</span><span class=\"txt\"></span>";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return ", (?<Genre>[^,]+), [0-9]+ perc";
            }
        }

        protected override string DirectorRegex
        {
            get
            {
                return "rendez.:&nbsp;</span><span class=\"txt\"><a(.*?)target=\"_top\">(?<Director>.*?)</a></span>";
            }
        }

        protected override string ActorsRegex
        {
            get
            {
                //return "<a href=\"/pls/pe/person\\.person\\?i_pers_id=[^\"]+\" target=\"_top\">(<b>)?(?<Actor>[^<]+)(</b>)?</a> \\(";
                return "";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);

            _result.Name = _result.Name != null ? _result.Name.TrimStart('(').TrimEnd(')') : null;
            _result.OriginalTitle = GetItem(input, OriginalTitleRegex, "OriginalTitle", RegexOptions.IgnoreCase | RegexOptions.Singleline).Trim();
            if (string.IsNullOrEmpty(_result.OriginalTitle))
            {
                _result.OriginalTitle = _result.Name;
            }
            _result.OriginalTitle = _result.OriginalTitle != null ? _result.OriginalTitle.TrimStart('(').TrimEnd(')') : null;
            if (!string.IsNullOrEmpty(_result.IMDBID))
            {
                _result.IMDBID = "tt" + _result.IMDBID;
            }

            return _result;
        }

        private bool ProcessPage(string input, string id, bool skipImages, string link, string title, string originalTitle)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(input))
            {
                MovieInfo _movieInfo = GetMovieInfo(input);

                if (!string.IsNullOrEmpty(this.IMDBID) && !string.IsNullOrEmpty(_movieInfo.IMDBID) && this.IMDBID != _movieInfo.IMDBID)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(id))
                {
                    Match _match = Regex.Match(input, IDRegex, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (_match.Success)
                    {
                        id = _match.Groups["ID"].Value;
                    }
                }

                // port.hu has NO POSTER
                //string _imageUrl = string.Format("http://www.sratim.co.il/movies/images/{0}", GetItem(input, CoverRegex, 1));

                _movieInfo.Name = string.IsNullOrEmpty(_movieInfo.Name) ? title : _movieInfo.Name;
                _movieInfo.OriginalTitle = string.IsNullOrEmpty(_movieInfo.OriginalTitle) ? _movieInfo.Name : _movieInfo.OriginalTitle;

                if (!string.IsNullOrEmpty(_movieInfo.Name))
                {
                    ResultMovieItem _movieItem = new ResultMovieItem(id, _movieInfo.Name, null, this.CollectorName);
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


        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            string _s = string.Format("{0}", SearchMask);

            string _resultsPage = Helpers.GetPage(string.Format(SearchMask, keywords.Replace(" ", "+")), Encoding.GetEncoding("iso-8859-2"));
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                // detect if is a searchlist or directly a page
                Regex _reg = new Regex(SearchListRegex, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_resultsPage))
                {
                    // found matches, it is a list
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        string _year = _match.Groups["Year"].Value;
                        string _title = _match.Groups["Title"].Value;
                        //string _originalTitle = _match.Groups["OriginalTitle"].Value.Trim().TrimStart('(').TrimEnd(')');
                        string _id = _match.Groups["ID"].Value;
                        string _relLink = string.Format("http://www.port.hu/spread/pls/fi/films.film_page?i_where=2&i_film_id={0}", _id);

                        if(!IsValidYear(_year))
                        {
                            continue;
                        }

                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        string _page = Helpers.GetPage(_relLink, Encoding.GetEncoding("iso-8859-2"));
                        if (!string.IsNullOrEmpty(_page))
                        {
                            bool _r = ProcessPage(_page, _id, skipImages, _relLink, _title, null);
                            if (_r)
                            {
                                _result = true;
                            }
                        }
                    }
                }
                else
                {
                    // no match or a movie page
                    // direct page
                    bool _r = ProcessPage(_resultsPage, null, skipImages, null, null, null);
                    if (_r)
                    {
                        _result = true;
                    }
                }
            }

            return _result;
        }
    }
}
