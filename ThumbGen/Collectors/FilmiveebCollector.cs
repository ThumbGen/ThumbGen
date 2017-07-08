using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.FILMIVEEB)]
    internal class FilmiveebCollector: BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.FILMIVEEB; }
        }

        public override Country Country
        {
            get { return Country.Estonia; }
        }

        public override string Host
        {
            get { return "http://www.filmiveeb.ee"; }
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
                return "http://www.filmiveeb.ee/?leht=otsing&marksona=";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<a href=\"(?<RelLink>filmid/(?<ID>[0-9]+)/[^[/]+/)\">(?<Title>[^\\(]+)\\((?<Year>\\d{4})\\)</a><br /><span style=\"font-size: 10px\">(?<OriginalTitle>[^<]+)</span>";
            }
        }

        //protected override string TitleRegex
        //{
        //    get
        //    {
        //        return "<h1>(?<Title>[^<]+)</h1>";
        //    }
        //}

        //protected override string OriginalTitleRegex
        //{
        //    get
        //    {
        //        return "<h2>(?<OriginalTitle>[^<]+)</h2>";                
        //    }
        //}

        protected override string GenresRegex
        {
            get
            {
                return "anr:</strong></td>[^<]+<td>(?<Genre>[^<]+)</td>";
            }
        }

        protected override string StudiosRegex
        {
            get
            {
                return "Stuudio:</strong></td>[^<]+<td>(?<Studio>[^<]+)</td>";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "name=\"title\" content=\"[^\\(]+\\((?<Year>\\d{4})\\) - Filmiveeb";
            }
        }

        protected override string CountryRegex
        {
            get
            {
                return "Riik:</strong></td>[^<]+<td>(?<Country>[^<]+)</td>";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "name=\"description\" content=\"(?<Plot>[^\"]+)\"";
            }
        }

        protected override string RatingRegex
        {
            get
            {
                return "<h3>(?<Rating>[\\d\\.]+) / 10</h3>";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);

            string _genre = GetItem(input, this.GenresRegex, "Genre");
            if (!string.IsNullOrEmpty(_genre))
            {
                _result.Genre = _genre.Split(',').ToTrimmedList();
            }
            string _country = GetItem(input, this.CountryRegex, "Country");
            if (!string.IsNullOrEmpty(_country))
            {
                _result.Countries = _country.Split('/').ToTrimmedList();
            }
            _result.Studios = _result.Studios.ToTrimmedList();

            string _plot = GetItem(input, PlotRegex, "Plot", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(_plot))
            {
                _plot = _plot.Trim('\r', '\n', '\t').Replace("<br />", "");
                _plot = Regex.Replace(_plot, "<a href=[^>]+>", "").Replace("</a>", ""); 

                _result.Overview = _plot;
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

                string _imageUrl = string.Format("{0}/filmipildid/{1}.gif", Host, id);

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

            //string _resultsPage = Helpers.GetPagePost(SearchMask, Encoding.GetEncoding("iso-8859-1"), string.Format("&otsitav={0}&otsing.x=0&otsing.y=0", keywords));
            string _resultsPage = Helpers.GetPage(SearchMask + keywords, Encoding.UTF8);
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
                        if (!string.IsNullOrEmpty(_year) && !string.IsNullOrEmpty(this.Year) && (_year != this.Year))
                        {
                            continue;
                        }
                        string _title = _match.Groups["Title"].Value.Trim();
                        string _originalTitle = _match.Groups["OriginalTitle"].Value.Trim();
                        string _id = _match.Groups["ID"].Value;
                        string _relLink = string.Format("{0}/{1}", Host, _match.Groups["RelLink"].Value);

                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        string _page = Helpers.GetPage(_relLink, Encoding.UTF8);
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
