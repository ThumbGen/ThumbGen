using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.CINEMARXRO)]
    internal class CinemarxCollector : BaseCollector
    {

        public override string CollectorName
        {
            get { return BaseCollector.CINEMARXRO; }
        }

        public override Country Country
        {
            get { return ThumbGen.Country.Romania; }
        }

        public override string Host
        {
            get { return "http://www.cinemarx.ro"; }
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
                return "http://www.cinemarx.ro/cauta/filme/{0}";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<h3><a href=\"(?<RelLink>/filme/[^\"]+)\" title=\"(?<Title>[^\"]+)\"[^\\(]+\\((?<Year>\\d{4})";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "<img src=\"(?<Cover>http://static\\.cinemarx\\.ro/poze/cache/[^\"]+)\" alt=\"Poster";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "<h2 id=\"movie_subtitle\">(?<Title>[^<]+)</h2>";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "title=\"(?<OriginalTitle>[^\"]+)\" id=\"movie_title\"";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "class=\"year\">(?<Year>\\d{4})</a>";
            }
        }

        protected override string ActorsRegex
        {
            get
            {
                return "title=\"Actor:.*?>(?<Actor>[^\"]+)</a";
            }
        }

        protected override string DirectorRegex
        {
            get
            {
                return "title=\"Regizor:.*?>(?<Director>[^\"]+)</a";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "title=\"Gen:.*?>(?<Genre>[^\"]+)</a";
            }
        }

        protected override string RuntimeRegex
        {
            get
            {
                return "<h5>Durata:</h5>(?<Runtime>[0-9]+) minute</td>";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "<div class=\"article_content\">(?<Plot>.*?)</div>";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);

            string _plot = GetItem(input, PlotRegex, "Plot", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(_plot))
            {
                _plot = _plot.Trim('\r', '\n', '\t').Replace("<br />", "").Replace("&nbsp;", "").Replace("<p align=\"justify\">", "").Replace("</p>", "");
                _plot = Regex.Replace(_plot, "<a href=[^>]+>", "").Replace("</a>", "");

                _result.Overview = _plot.Trim();
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

                string _imageUrl = GetItem(input, CoverRegex, "Cover");
                _imageUrl = Regex.Replace(_imageUrl, "cache/[^/]+/", "");

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

        private static bool m_GotCookie = false;

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            // must get the cookies once and remember them
            if (!m_GotCookie)
            {
                Helpers.GetPage("http://www.cinemarx.ro", null, Encoding.UTF8, "", true, true);
            }

            string _resultsPage = Helpers.GetPage(string.Format(SearchMask, keywords.Replace(" ","+")), null, Encoding.UTF8, "", true, true);
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
                        string _relLink = string.Format("{0}{1}", Host, _match.Groups["RelLink"].Value);

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
