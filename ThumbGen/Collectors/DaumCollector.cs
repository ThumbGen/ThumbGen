using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.DAUMNET)]
    internal class DaumCollector : BaseCollector
    {

        public override string CollectorName
        {
            get { return BaseCollector.DAUMNET; }
        }

        public override Country Country
        {
            get { return ThumbGen.Country.Korea; }
        }

        public override string Host
        {
            get { return "http://www.daum.net"; }
        }

        protected override string SearchMask
        {
            get
            {
                return "http://movie.daum.net/search.do?type=all&nil_profile=vsearch&nil_src=movie&q={0}";
            }
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
                return "<a href=\"(?<RelLink>http://movie\\.daum\\.net/moviedetail/moviedetailMain\\.do\\?movieId=(?<ID>[0-9]+))\" class=\"fs13.*?;'>(?<Title>[^<]+)<[^\\(]+\\((?<Year>[0-9]+)\\)</span>";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "thumbnail\" title=\"[^\"]+\"><img src=\"(?<Cover>[^\"]+)";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "<em class=\"title_AKA\"> <span class=\"[^\"]+\">(?<OriginalTitle>[^<]+)</span>";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "<a href='/search\\.do\\?type=movie&q=[^>]+>(?<Year>[0-9]+)</a>";
            }
        }

        protected override string RuntimeRegex
        {
            get
            {
                return "(?<Runtime>[0-9]+) 분";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "main=genre'>(?<Genre>[^<]+)</a>";
            }
        }

        protected override string CountryRegex
        {
            get
            {
                return "main=country'[^>]*>(?<Country>[^<]+)</a>";
            }
        }

        protected override string ReleaseDateRegex
        {
            get
            {
                return "개봉 (?<ReleaseDate>[0-9-]+)";
            }
        }

        protected override string DirectorRegex
        {
            get
            {
                return "main=director' title='(?<Director>[^']+)'";
            }
        }

        protected override string ActorsRegex
        {
            get
            {
                return "main=actors\" [^>]+>(?<Actor>[^<]+)</a>";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "\"title_kor\" >(?<Title>[^<]+)</strong>";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "<div class=\"txt\"><table><tr><td>.*?(?<Plot>[^<]+)</td>";
            }
        }

        protected override string RatingRegex
        {
            get
            {
                return "네티즌별점</span></span><em>(?<Rating>[^<]+)</em></span></span>";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _info = base.GetMovieInfo(input);
            _info.Overview = GetItem(input, PlotRegex, "Plot", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim().Trim().Replace("/a>", "").Trim();

            return _info;
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
                _imageUrl = Regex.Replace(_imageUrl, "C198x288", "image");

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

            string _resultsPage = Helpers.GetPage(string.Format(SearchMask, keywords.Replace(" ", "%20")), Encoding.UTF8);
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

                        if (!IsValidYear(_year)) { continue; }

                        string _title = _match.Groups["Title"].Value.Trim();
                        string _id = _match.Groups["ID"].Value;
                        string _relLink = _match.Groups["RelLink"].Value;

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
