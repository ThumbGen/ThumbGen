using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.KINOPOISK)]
    internal class KinopoiskCollector: BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.KINOPOISK; }
        }

        public override Country Country
        {
            get { return Country.Russia; }
        }

        public override string Host
        {
            get { return "http://www.kinopoisk.ru"; }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        public override bool SupportsBackdrops
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
                return "http://www.kinopoisk.ru/index.php?kp_query={0}";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                //return @"href=[^\d]+\d+/film/(?<ID>[\d]+)[^>]+>(?<Title>[^>]+)</a>,&nbsp;<a[^>]+>(?<Year>[^<]+)</a>(?:[^>]+>){7}\.{0,3}\s{0,1}";
                //return "href=[^\\d]+\\d+/film/(?<ID>[\\d]+)[^>]+>(?<Title>[^>]+)</a>, <span class=\"year\"><a[^>]+>(?<Year>[^<]+)</a>(?:[^>]+>){7}\\.{0,3}\\s{0,1}";
                return "href=[^\\d]+\\d+/film/(?<ID>[\\d]+)[^>]+>(?<Title>[^>]+)</a>[^<]+<span class=\"year\">(<a[^>]+>)?(?<Year>[^<]+)(</a>)?(?:[^>]+>){7}\\.{0,3}\\s{0,1}";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "<title>(?<Title>[^<]+?)</title>";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "<span style=\"color: #666; font-size: 13px\">(?<OriginalTitle>[^<]+?)</span>";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "m_act%5Byear%5D/(?<Year>\\d{4})/";
            }
        }

        protected override string RuntimeRegex
        {
            get
            {
                return "<td class=\"time\" id=\"runtime\">(?<Runtime>[0-9]+?)\\s";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                //return "<span class=._reachbanner_.><div class=\"brand_words\">(?<Plot>.+?)</div></span>";
                return "itemprop=\"description\">(?<Plot>.+?)</div></span>";
            }
        }

        protected override string RatingRegex
        {
            get
            {
                return @"IMDB:\s(?<Rating>[^\s]+)\s";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "m_act%5Bgenre%5D/\\d+/\"\\s*>(?<Genre>[^>]*?)</a>";
            }
        }

        protected override string DirectorRegex
        {
            get
            {
                return "itemprop=\"director\"><a href=\"[^\"]+\"\\s*>(?<Director>[^<]*?)</a>";
            }
        }

        protected override string ActorsRegex
        {
            get
            {
                return "itemprop=\"actors\"\\s*><a href=\"/level/\\d+/people/\\d+/\">(?<Actor>[^<]+?)</a>";
            }
        }

        protected override string CountryRegex
        {
            get
            {
                return "m_act%5Bcountry[^\"]+\"\\s*>(?<Country>[^<]*?)</a";
            }
        }

        protected override string PostersRegex
        {
            get
            {
                return "<img\\s+src=\"(?<Cover>(.*?)/images/poster/[^\"]+?)\"";
            }
        }

        protected override string BackdropsRegex
        {
            get
            {
                //return "src=\"(?<Backdrop>/images/kadr/[^\"]+?)\"";
                return "src=\"(?<Backdrop>/images/kadr/[^\"]+?)\"(.*?)/></a>(.*?)<b><i>(?<Width>[0-9]*?)&times;(?<Height>[0-9]*?)<";
            }
        }


        private bool ProcessVisualSection(MovieInfo movieInfo, string id)
        {
            bool _result = false;

            // first posters
            string _page = Helpers.GetPage(string.Format("http://www.kinopoisk.ru/level/17/film/{0}", id), null, Encoding.GetEncoding("Windows-1251"), "", true, true);
            if (!string.IsNullOrEmpty(_page))
            {
                if (_page.Contains("<title>Архив постеров на КиноПоиск.ru</title>"))
                {
                    // movie has no posters
                    return _result;
                }
                Regex _reg = new Regex(PostersRegex, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (_reg.IsMatch(_page))
                {
                    for (int _i = 0; _i <= (_reg.Matches(_page).Count - 1); _i++)
                    {
                        string _imageUrl = string.Format("{0}", _reg.Matches(_page)[_i].Groups["Cover"].Value.Replace("sm_", ""));
                        
                        ResultMovieItem _movieItem = new ResultMovieItem(id, movieInfo.Name, _imageUrl, this.CollectorName);
                        _movieItem.CollectorMovieUrl = string.Format("http://www.kinopoisk.ru/level/1/film/{0}/sr/1/", id);
                        _movieItem.MovieInfo = movieInfo;

                        ResultsList.Add(_movieItem);
                        _result = true;
                    }
                }
            }

            //then backdrops
            _page = Helpers.GetPage(string.Format("http://www.kinopoisk.ru/level/13/film/{0}", id), null, Encoding.GetEncoding("Windows-1251"), "", true, true);

            if (!string.IsNullOrEmpty(_page))
            {
                Regex _reg = new Regex(BackdropsRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (_reg.IsMatch(_page))
                {
                    foreach (Match _m2 in _reg.Matches(_page))
                    {
                        string _thumbUrl = string.Format("http://www.kinopoisk.ru/{0}", _m2.Groups["Backdrop"].Value);
                        string _originalUrl = _thumbUrl.Replace("sm_", "");

                        if (!string.IsNullOrEmpty(movieInfo.Name) && !string.IsNullOrEmpty(_thumbUrl) && !string.IsNullOrEmpty(_originalUrl))
                        {
                            string _width = _m2.Groups["Width"].Value;
                            string _height = _m2.Groups["Height"].Value;

                            BackdropItem _bi = new BackdropItem(id, movieInfo.IMDBID, this.CollectorName, _thumbUrl, _originalUrl);
                            _bi.SetSize(_width, _height);
                            BackdropsList.Add(_bi);
                        }
                    }
                }
            }

            return _result;
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);

            _result.Overview = _result.Overview != null ? _result.Overview.Replace("<i>", "").Replace("</i>", "").Replace("<br>", "").Replace("&nbsp;", "") : null;

            _result.Cast.Clear();
            List<string> _t = GetItems(input, ActorsRegex, "Actor");
            _t.ForEach(a => 
            {
                if (_result.Cast.Count < 5)
                {
                    _result.Cast.Add(a);
                }
            });

            return _result;
        }

        private bool ProcessPage(string input, string id, bool skipImages, string link, string title, string originalTitle)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(input))
            {
                MovieInfo _movieInfo = GetMovieInfo(input);

                if (skipImages)
                {
                    if (!string.IsNullOrEmpty(_movieInfo.Name))
                    {
                        ResultMovieItem _movieItem = new ResultMovieItem(id, _movieInfo.Name, null, this.CollectorName);
                        _movieItem.MovieInfo = _movieInfo;
                        _movieItem.CollectorMovieUrl = link;
                        ResultsList.Add(_movieItem);
                        _result = true;
                    }
                }
                else
                {
                    // process the VisualSection 
                    bool _res = ProcessVisualSection(_movieInfo, id);
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
                Helpers.GetPage(this.Host, null, Encoding.GetEncoding("windows-1251"), "", true, true);
                m_GotCookie = true;
            }

            string _resultsPage = Helpers.GetPage(string.Format(SearchMask, keywords.Replace(" ", "+")), null, Encoding.GetEncoding("Windows-1251"), "", true, true);
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                Regex _reg = new Regex(SearchListRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (_reg.IsMatch(_resultsPage))
                {
                    // we got the results page
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        string _relLink = string.Format("http://www.kinopoisk.ru/level/1/film/{0}/sr/1/", _match.Groups["ID"].Value);
                        string _title = _match.Groups["Title"].Value;
                        //string _originalTitle = _match.Groups["OriginalTitle"].Value;
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

                        string _page = Helpers.GetPage(_relLink, null, Encoding.GetEncoding("Windows-1251"), "", true, true);
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
