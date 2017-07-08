using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.SRATIM)]
    internal class SratimCollector:BaseCollector 
    {
        public override string CollectorName
        {
            get { return BaseCollector.SRATIM; }
        }

        public override Country Country
        {
            get { return Country.Israel; }
        }

        public override string Host
        {
            get { return "http://www.sratim.co.il"; }
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
                return "http://sratim.co.il/browse.php?q={0}";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<a href=\"(?<RelLink>view\\.php\\?id=(?<ID>[0-9]+)&amp;q=[^\"]+)\" title=\"(?<Title>[^|]+) \\| (?<OriginalTitle>[^|]+) \\| (?<Year>\\d{4})\">";
            }
        }

        protected override string IMDBIdRegex
        {
            get
            {
                return "<a href=\"http://www.imdb.com/title/(?<IMDBID>tt\\d{1,})";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "href=\"browse\\.php\\?uy=(?<Year>\\d{4})";
            }
        }

        protected override string DirectorRegex
        {
            get
            {
                return "בימוי:</td><td>[^>]+>(?<Director>[^<]+)";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "itemprop=\"genre\">(?<Genre>[^<]+)</span>";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "font-size:14px;text-align:justify;\" itemprop=\"description\">(?<Plot>(.*?))</div";
            }
        }

        protected override string StudiosRegex
        {
            get
            {
                return "הפקה:</td><td>(?<Studio>[^,^<]+)";
            }
        }

        protected override string CountryRegex
        {
            get
            {
                return "מדינה:</td><td>(?<Country>[^<]+)<";
            }
        }

        protected override string ActorsRegex
        {
            get
            {
                return "itemprop=\"actors\">(?<Actor>[^<]+)</";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "img src=\"(?<Cover>[^\"]+)\" class=\"lrg_cover";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);

            _result.Overview = GetItem(input, PlotRegex, 1).Trim();
            _result.Genre.Clear();
            _result.Genre.AddRange(GetItems(input, GenresRegex, 1).ToTrimmedList().Distinct());
            _result.Cast.Clear();
            _result.Cast.AddRange(GetItems(input, ActorsRegex, "Actor"));
            foreach (string _s in _result.Director)
            {
                if (_result.Cast.Contains(_s))
                {
                    _result.Cast.Remove(_s);
                }
            }

            string _country = GetItem(input, this.CountryRegex, "Country", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
            if (!string.IsNullOrEmpty(_country))
            {
                _result.Countries = _country.Split(',').ToTrimmedList();
            }

            _result.Overview = GetItem(input, PlotRegex, "Plot", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim().Replace("<br />", "");

            _result.HasRightToLeftDirection = true;

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

                string _imageUrl = string.Format("http://sratim.co.il/{0}", GetCoverLink(input));

                if (string.IsNullOrEmpty(_movieInfo.Name))
                {
                    _movieInfo.Name = title;
                }
                if (string.IsNullOrEmpty(_movieInfo.OriginalTitle))
                {
                    _movieInfo.OriginalTitle = originalTitle;
                }

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

            string _resultsPage = Helpers.GetPage(string.Format(SearchMask, keywords.Replace(" ", "+")));
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                Regex _reg = new Regex(SearchListRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (_reg.IsMatch(_resultsPage))
                {
                    // we got the results page
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        string _relLink = string.Format("http://sratim.co.il/view.php?id={0}", _match.Groups["ID"].Value);
                        string _title = _match.Groups["Title"].Value;
                        string _originalTitle = _match.Groups["OriginalTitle"].Value;
                        string _year = _match.Groups["Year"].Value;
                        string _id = _match.Groups["ID"].Value;

                        if (!string.IsNullOrEmpty(_year) && !string.IsNullOrEmpty(this.Year) && (_year != this.Year))
                        {
                            continue;
                        }

                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        string _page = Helpers.GetPage(_relLink);
                        if (!string.IsNullOrEmpty(_page))
                        {
                            bool _r = ProcessPage(_page, _id, skipImages, _relLink, _title, _originalTitle);
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
