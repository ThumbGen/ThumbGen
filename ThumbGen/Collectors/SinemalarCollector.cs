using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Threading;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.SINEMALAR)]
    internal class SinemalarCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.SINEMALAR; }
        }

        public override Country Country
        {
            get { return ThumbGen.Country.Turkey; }
        }

        public override string Host
        {
            get { return "http://www.sinemalar.com"; }
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
                return "http://www.sinemalar.com/filmler/ara/{0}";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "bordo no_link_style p16\" href=\"(?<RelLink>http://www\\.sinemalar\\.com/film/\\d+/[^\"]+)\" title=\"(?<Title>[^\"]+)\">.*?bordo no_link_style p14\" href=\"[^\"]+\" title=\"(?<OriginalTitle>[^\"]+)\">";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "<img class=\"photo_bordered detay_afis\" alt=\"Soysuzlar Çetesi\" title=\"Soysuzlar Çetesi\" src=\"(?<Cover>[^\"]+)\" /></a>";
            }
        }

        protected override string IMDBIdRegex
        {
            get
            {
                return "http://www\\.imdb\\.com/title/(?<IMDBID>tt\\d{1,})\">";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                //return "<title>Sinemalar\\.com ~ (?<Title>[^~]+) ~ (?<OriginalTitle>[^~]+) \\((?<Year>[0-9]+)\\)</title>";
                return string.Empty;
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                //return this.TitleRegex;
                return string.Empty;
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "<meta property=\"og:title\" content=\"[^\\(]+\\((?<Year>\\d{4})\\)\"/>";
            }
        }
        
        protected override string RatingRegex
        {
            get
            {
                return "<h4 class=\"p30 turuncu bold\">(?<Rating>[\\d\\.]+)<span";
            }
        }

        protected override string CountryRegex
        {
            get
            {
                return "href=\"\" title=\"[^\"]+\">(?<Country>[^<]+)</a>&nbsp;";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "href=\"http://www\\.sinemalar\\.com/filmler/tur_\\d+\" title=\"[^\"]+\">(?<Genre>[^<]+)</a>";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "Filmin Özeti.*?<p class=\"c333\">(?<Plot>.*?)</p>";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);
            _result.Overview = GetItem(input, PlotRegex, "Plot", RegexOptions.IgnoreCase | RegexOptions.Singleline).Trim().Trim();

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

        private static bool m_GotCookie = false;

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            // must get the cookies once and remember them
            if (!m_GotCookie)
            {
                Helpers.GetPage(string.Format(SearchMask, HttpUtility.UrlPathEncode(keywords)), null, Encoding.UTF8, "", true, true);
                //Thread.Sleep(10000);
            }

            //string _resultsPage = Helpers.GetPage(string.Format(SearchMask, HttpUtility.UrlPathEncode(keywords)), Encoding.UTF8);
            string _resultsPage = Helpers.GetPage(string.Format(SearchMask, HttpUtility.UrlPathEncode(keywords)), null, Encoding.UTF8, "", true, true);
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
                        //string _year = _match.Groups["Year"].Value;

                        //if (!IsValidYear(_year))
                        //{
                        //    continue;
                        //}

                        string _title = _match.Groups["Title"].Value.Trim();
                        string _originalTitle = _match.Groups["OriginalTitle"].Value.Trim();
                        string _id = _match.Groups["RelLink"].Value;
                        string _relLink = _id;//string.Format("{0}/{1}", Host, _match.Groups["RelLink"].Value);

                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        //string _page = Helpers.GetPage(_relLink, Encoding.UTF8);
                        string _page = Helpers.GetPage(_relLink, null, Encoding.UTF8, "", true, true);
                        if (!string.IsNullOrEmpty(_page))
                        {
                            bool _r = ProcessPage(_page, _id, skipImages, _relLink, _title, _originalTitle);
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
