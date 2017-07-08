using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.FILMZDK)]
    internal class FilmzCollector: BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.FILMZDK; }
        }

        public override Country Country
        {
            get { return ThumbGen.Country.Denmark; }
        }

        public override string Host
        {
            get { return "http://www.filmz.dk"; }
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
                return "http://filmz.dk/soeg?q_extra=&q={0}&search_group_newz[]=movies";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<td><a href=\"(?<RelLink>[^\"]+)\" title=[^>]+>(?<Title>[^<]+)</a></td>.*?</tr>";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "(?<Cover>[^\"]+)\" width=\"226\" height=\"310\"";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "<title>(?<Title>[^-]+)-  Filmz</title>";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "<td class=\"title\">(?<OriginalTitle>[^<]+)</td>";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "<td>(?<Year>\\d{4})</td>";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "<div class=\"genrelist\">(?<Genre>.*?)</div>";
            }
        }

        protected override string IMDBIdRegex
        {
            get
            {
                return "www\\.imdb\\.com/title/(?<IMDBID>tt\\d{1,})\">";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "<div class=\"synopsis\">.*?<p>(?<Plot>.*?)</p>";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);

            string _genre = GetItem(input, this.GenresRegex, "Genre", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
            if (!string.IsNullOrEmpty(_genre))
            {
                _result.Genre = _genre.Split(',').ToTrimmedList();
            }

            _result.Overview = GetItem(input, PlotRegex, "Plot", RegexOptions.IgnoreCase | RegexOptions.Singleline).Trim();

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

                string _imageUrl = GetCoverLink(input);

                _movieInfo.Name = string.IsNullOrEmpty(_movieInfo.Name) ? title : _movieInfo.Name;
                _movieInfo.OriginalTitle = string.IsNullOrEmpty(_movieInfo.OriginalTitle) ? originalTitle : _movieInfo.OriginalTitle;

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

            string _resultsPage = Helpers.GetPage(string.Format(SearchMask, keywords.Replace(" ", "+"), Encoding.UTF8));
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                // detect if is a searchlist or directly a page
                Regex _reg = new Regex(SearchListRegex, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_resultsPage))
                {
                    int _cnt = 0;
                    // found matches, it is a list
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        if (_cnt == 5)
                        {
                            break;
                        }
                        string _year = _match.Groups["Year"].Value;

                        if (!IsValidYear(_year))
                        {
                            continue;
                        }

                        string _title = _match.Groups["Title"].Value.Trim();
                        string _id = _match.Groups["RelLink"].Value;
                        string _relLink = string.Format("{0}/{1}", Host, _match.Groups["RelLink"].Value);

                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        string _page = Helpers.GetPage(_relLink, Encoding.UTF8);
                        if (!string.IsNullOrEmpty(_page))
                        {
                            bool _r = ProcessPage(_page, _id, skipImages, _relLink, _title, _title);
                            if (_r)
                            {
                                _result = true;
                                _cnt++;
                            }
                        }
                    }
                }
                //else
                //{
                //    // no match or a movie page
                //    // direct page
                //    bool _r = ProcessPage(_resultsPage, null, skipImages, null, null, null);
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
