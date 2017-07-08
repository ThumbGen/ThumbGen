using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThumbGen.Collectors
{
    [MovieCollector(BaseCollector.TORECNET)]
    internal class TorecCollector: BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.TORECNET; }
        }

        public override Country Country
        {
            get { return ThumbGen.Country.Israel; }
        }

        public override string Host
        {
            get { return "http://www.torec.net"; }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        public override bool SupportsIMDbSearch
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
                return "<a href=\"(?<RelLink>sub\\.asp\\?sub_id=(?<ID>[0-9]+))\">(?<Title>[^/]+) / (?<OriginalTitle>[^<]+)<.*?(?<Year>\\d{4})</span>";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "value=\"image=(?<Cover>/pics/[^\"]+)\"";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "<div class=\"line sub_title\"><h1>(?<Title>[^<]+)</h1> &nbsp; / &nbsp;<bdo dir=\"ltr\">(?<OriginalTitle>[^<]+)</bdo>";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return this.TitleRegex;
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "</strong> (?<Year>\\d{4})<br>";
            }
        }

        protected override string RuntimeRegex
        {
            get
            {
                return "(?<Runtime>[0-9]+) דקות<br>";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "<div class=\"sub_name_div\".*?>(?<Plot>[^<]+)<";
            }
        }

        protected override string IMDBIdRegex
        {
            get
            {
                return "http://www.imdb.com/title/(?<IMDBID>tt\\d{1,})";
            }
        }

        protected override string DirectorRegex
        {
            get
            {
                return "במאי:</strong>(?<Director>[^<]+)<br>";
            }
        }

        protected override string ActorsRegex
        {
            get
            {
                return "<a href=\"/search_actor\\.asp\\?search=[^\"]+\" style=\"[^\"]+\">(?<Actor>[^<]+)</a>";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "ז'אנר:</strong> (?<Genre>[^<]+)<br>";
            }
        }

        protected override string RatingRegex
        {
            get
            {
                return "class=\"rankTable\">(?<Rating>[0-9]+)</span>";
            }
        }

        public override MovieInfo QueryMovieInfo(string imdbId)
        {
            MovieInfo _result = null;

            string input = Helpers.GetPage(string.Format("{0}/ssearch.asp?search={1}", Host, imdbId), null, Encoding.UTF8, Host, false, false);
            if (!string.IsNullOrEmpty(input))
            {
                Match _m = Regex.Match(input, SearchListRegex);
                if (_m.Success)
                {
                    string _id = _m.Groups["ID"].Value;
                    if (!string.IsNullOrEmpty(_id))
                    {
                        string _page = Helpers.GetPage(string.Format("{0}/sub.asp?sub_id={1}", Host, _id));
                        _result = GetMovieInfo(_page);
                    }
                }
            }

            return _result;
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);
            string _genre = GetItem(input, this.GenresRegex, "Genre", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
            if (!string.IsNullOrEmpty(_genre))
            {
                _result.Genre = _genre.Split('/').ToTrimmedList();
            }

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

                // extra for torec... eliminate duplicates
                var _items = from c in ResultsList
                             where c.MovieInfo.IMDBID == _movieInfo.IMDBID
                             select c;
                if (_items != null && _items.Count() != 0)
                {
                    return false;
                }


                string _imageUrl = string.Format("{0}{1}", Host, GetCoverLink(input));

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

            string _criteria = string.IsNullOrEmpty(imdbID) ? keywords.Replace(" ", "+") : imdbID;

            string _resultsPage = Helpers.GetPage(string.Format("{0}/ssearch.asp?search={1}", Host, _criteria), null, Encoding.UTF8, Host, false, false);
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                Regex _reg = new Regex(SearchListRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (_reg.IsMatch(_resultsPage))
                {
                    // we got the results page
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        string _relLink = string.Format("{0}/{1}", Host, _match.Groups["RelLink"].Value);
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
