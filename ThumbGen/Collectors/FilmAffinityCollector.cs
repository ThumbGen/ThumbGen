using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    internal abstract class FilmAffinityCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return null; }
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

        protected abstract string TargetUrl { get; }

        protected override string IDRegex
        {
            get
            {
                return "-([\\d+]*)-full";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "text-decoration:none\">(?<Genre>[^<]*)</a>";
            }
        }

        protected override string DirectorRegex
        {
            get
            {
                return "<a href=\"search\\.php\\?stype=director&stext=[\\w|\"|+|%]*> *(?<Director>.*?)</a>";
            }
        }

        protected override string ActorsRegex
        {
            get
            {
                return "<a href=\"search\\.php\\?stype=cast&stext=[\\w|\"|+|%]*> *(?<Actor>.*?)</a>";
            }
        }

        protected override string RatingRegex
        {
            get
            {
                return "font-weight: bold;\">(?<Rating>[^<]+?)</td>[\\w|\\s|<|>|/|\"|=]*<img src=\\\"http://www\\.filmaffinity\\.com/imgs/ratings";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "(?:(?<1>http://pics.filmaffinity.com/[^\"]+-large.jpg)\")";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                //return "<b><a href=\"[^\"]*(?<Link>/film[^\"]*?)\">(?<Title>.*?)</";
                return "<b><a href=\"[^\"]*(?<Link>/film(?<ID>[^\"]*?).html)\">(?<Title>.*?)</";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "<img src=\"http://www.filmaffinity.com/images/movie.gif\" border=\"0\"> (?<Title>.*)</span>";
            }
        }

        protected override string SearchMask
        {
            get
            {
                return "search.php?stext={0}&stype=title";
            }
        }

        //protected override string PostersRegex
        //{
        //    get
        //    {
        //        return "http://pics.filmaffinity.com/([^']*-large.jpg)[^,]*(.?)*type_id: 'Posters'";
        //    }
        //}

        private const string baseImageRegex = "pics\\.filmaffinity\\.com\\\\/([^\"]+)\",\"description\":[^,]+,\"type_id\":";

        protected override string PostersRegex
        {
            get
            {
                return baseImageRegex + "\"Posters";
            }
        }

        protected override string BackdropsRegex
        {
            get
            {
                return baseImageRegex + "\"Fotogramas";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = new MovieInfo();

            _result.Name = GetItem(input, TitleRegex, "Title");
            _result.OriginalTitle = GetItem(input, OriginalTitleRegex, "OriginalTitle", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            _result.Year = GetItem(input, YearRegex, "Year", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            _result.Runtime = GetItem(input, RuntimeRegex, "Runtime");
            _result.Overview = GetItem(input, PlotRegex, "Plot", RegexOptions.Singleline | RegexOptions.IgnoreCase).Replace("</td>", "").Replace("(FILMAFFINITY)", "").Replace("<br />", "").Replace("<br/>", "");
            _result.Rating = GetItem(input, RatingRegex, "Rating", RegexOptions.Singleline | RegexOptions.IgnoreCase).Replace(",", ".");
            _result.Genre.AddRange(GetItems(input, GenresRegex, "Genre", RegexOptions.Singleline | RegexOptions.IgnoreCase).ToListWithoutEmptyItems().ToTrimmedList());
            _result.Director.AddRange(GetItems(input, DirectorRegex, "Director", RegexOptions.Singleline | RegexOptions.IgnoreCase).ToListWithoutEmptyItems().ToTrimmedList());
            _result.Cast.AddRange(GetItems(input, ActorsRegex, "Actor", RegexOptions.Singleline | RegexOptions.IgnoreCase).ToListWithoutEmptyItems().ToTrimmedList());
            _result.Countries.AddRange(GetItems(input, CountryRegex, "Country", RegexOptions.Singleline | RegexOptions.IgnoreCase).ToListWithoutEmptyItems().ToTrimmedList());
            string _s = GetItem(input, StudiosRegex, "Studio", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(_s))
            {
                _result.Studios.AddRange(_s.Split(new char[] { ';', '/' }).ToTrimmedList());
            }

            return _result;
        }

        protected override bool ProcessVisualSection(string relLink, MovieInfo movieInfo, string id)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(relLink))
            {
                string _page = Helpers.GetPage(string.Format("http://www.filmaffinity.com{0}", relLink), Encoding.GetEncoding("ISO-8859-1"));
                if (!string.IsNullOrEmpty(_page))
                {
                    Regex _reg = new Regex(PostersRegex, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (_reg.IsMatch(_page))
                    {
                        for (int _i = 0; _i <= (_reg.Matches(_page).Count - 1); _i++)
                        {
                            string _imageUrl = string.Format("http://pics.filmaffinity.com/{0}", _reg.Matches(_page)[_i].Groups[1].Value);
                            ResultMovieItem _movieItem = new ResultMovieItem(id, movieInfo.Name, _imageUrl, this.CollectorName);
                            _movieItem.MovieInfo = movieInfo;

                            ResultsList.Add(_movieItem);
                            _result = true;
                        }
                    }

                    _reg = new Regex(BackdropsRegex, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (_reg.IsMatch(_page))
                    {
                        foreach (Match _m2 in _reg.Matches(_page))
                        {
                            string _originalUrl = string.Format("http://pics.filmaffinity.com/{0}", _m2.Groups[1].Value);
                            string _thumbUrl = _originalUrl.Replace("-large.", "-small.");
                            
                            if (!string.IsNullOrEmpty(movieInfo.Name) && !string.IsNullOrEmpty(_thumbUrl) && !string.IsNullOrEmpty(_originalUrl))
                            {
                                BackdropsList.Add(new BackdropItem(id, movieInfo.IMDBID, this.CollectorName, _thumbUrl, _originalUrl));
                            }
                        }
                    }
                }
            }

            return _result;
        }

        protected virtual bool ProcessPage(string input, string id, bool skipImages)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(input))
            {
                MovieInfo _movieInfo = GetMovieInfo(input);

                if (!IsValidYear(_movieInfo.Year))
                {
                    return false;
                }

                string _title = string.IsNullOrEmpty(_movieInfo.Name) ? GetItem(input, TitleRegex, 1) : _movieInfo.Name;
                string _imageUrl = GetItem(input, CoverRegex, 1);
                string _id = string.IsNullOrEmpty(id) ? GetItem(input, IDRegex, 1) : id;

                if (!string.IsNullOrEmpty(_title))
                {
                    ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, _imageUrl, this.CollectorName);
                    _movieItem.MovieInfo = _movieInfo;

                    ResultsList.Add(_movieItem);
                    _result = true;
                }

                if (!skipImages)
                {
                    // process the VisualSection if any
                    bool _res = ProcessVisualSection(GetItem(input, VisualSectionRegex, 1), _movieInfo, _id);
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

            string _s = string.Format("{0}{1}", TargetUrl, SearchMask);
            string _resultsPage = Helpers.GetPage(string.Format(_s, HttpUtility.UrlEncode(keywords, Encoding.Default)), Encoding.GetEncoding("ISO-8859-1"));
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                Regex _reg = new Regex(SearchListRegex, RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_resultsPage))
                {
                    // we got the results page
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        string _relLink = _match.Groups["Link"].Value;
                        string _title = _match.Groups["Title"].Value;
                        string _id = _match.Groups["ID"].Value;

                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        string _page = Helpers.GetPage(string.Format("{0}{1}", TargetUrl, _relLink), Encoding.GetEncoding("ISO-8859-1"));
                        if (!string.IsNullOrEmpty(_page))
                        {
                            bool _r = ProcessPage(_page, _id, skipImages);
                            if (_r)
                            {
                                _result = true;
                            }
                        }
                    }
                }
                else
                {
                    // direct page
                    bool _r = ProcessPage(_resultsPage, null, skipImages);
                    if (_r)
                    {
                        _result = true;
                    }
                }

            }

            return _result;
        }
    }

    [MovieCollector(BaseCollector.FILMAFFINITYEN)]
    internal class FilmAffinityENCollector : FilmAffinityCollector
    {
        public override Country Country
        {
            get { return Country.UK; }
        }

        public override string Host
        {
            get { return "http://www.filmaffinity.com"; }
        }

        public override string CollectorName
        {
            get
            {
                return FILMAFFINITYEN;
            }
        }

        protected override string TargetUrl
        {
            get { return "http://www.filmaffinity.com/en/"; }
        }

        protected override string OriginalTitleRegex
        {
            get { return @"<b>ORIGINAL TITLE</b>[\w|\s|<|>|/]*<b>(?<OriginalTitle>.*?)</b>"; }
        }

        protected override string YearRegex
        {
            get { return "<b>YEAR</b>[\\w|\\s|<|>|/|=|\"|%]*<td >(?<Year>[0-9]*?)<"; }
        }

        protected override string RuntimeRegex
        {
            get { return "<b>RUNNING TIME</b>[\\w|\\s|<|>|/|=|\"|%]*<td>(?<Runtime>\\d+)"; }
        }

        protected override string PlotRegex
        {
            get { return "<b>SYNOPSIS/PLOT</b>[\\w|\\s|<|>|/|=|\"|%]*<td>(?<Plot>.*?)<"; }
        }

        protected override string CountryRegex
        {
            get { return "<b>COUNTRY</b>[\\w|\\s|<|>|/|=|\"|%|\\.]*title=\"(?<Country>.+?)\""; }
        }

        protected override string StudiosRegex
        {
            get { return "<b>STUDIO/PRODUCER</b>[^\"]*<td  >(?<Studio>[^\"]*)</td>"; }
        }

        protected override string VisualSectionRegex
        {
            get { return "<a href=\"([^\"]*)\">Visual Section</a>"; }
        }

        //protected override string BackdropsRegex
        //{
        //    get { return "http://pics.filmaffinity.com/([^']*-large.jpg)[^,]*(.?)*type_id: 'Stills'"; }
        //}
    }

    [MovieCollector(BaseCollector.FILMAFFINITYES)]
    internal class FilmAffinityESCollector : FilmAffinityCollector
    {
        public override Country Country
        {
            get { return Country.Spain; }
        }

        public override string Host
        {
            get { return "http://www.filmaffinity.com"; }
        }

        public override string CollectorName
        {
            get
            {
                return FILMAFFINITYES;
            }
        }

        protected override string TargetUrl
        {
            get { return "http://www.filmaffinity.com/es/"; }
        }

        protected override string IDRegex
        {
            get
            {
                return "data-movie-id=\"(\\d+)\"";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "<title>(?<OriginalTitle>[^\\(]+)\\s\\(";
            }
        }

        protected override string YearRegex
        {
            get { return @"<dt>A&ntilde;o</dt>.*?<dd>(?<Year>\d{4})</dd>"; }
        }

        protected override string RuntimeRegex
        {
            get { return @"<dt>Duraci&oacute;n</dt>.*?<dd>(?<Runtime>[^&]+)</dd>"; }
        }

        protected override string RatingRegex
        {
            get
            {
                return "<div id=\"movie-rat-avg\">.*?(?<Rating>[\\d\\.,]+).*?<";
            }
        }

        protected override string PlotRegex
        {
            get { return "<dt>Sinopsis</dt>.*?<dd>(?<Plot>[^<]+)</dd>"; }
        }

        protected override string CountryRegex
        {
            get { return "<img src=\"/imgs/countries/[^\\.]+\\.jpg\" title=\"(?<Country>[^\"]+)\""; }
        }

        protected override string ActorsRegex
        {
            get
            {
                return "stype=cast&stext=[^\"]+\">(?<Actor>[^<]+)</a>";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "moviegenre\\.php\\?genre=[^&]+&attr=rat_count\">(?<Genre>[^<]+)</";
            }
        }


        protected override string DirectorRegex
        {
            get
            {
                return "stype=director&stext=[^\"]+\">(?<Director>[^<]+)</a>";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "(?:(?<1>http://pics.filmaffinity.com/[^\"]+-large.jpg)\")";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<div class=\"mc-title\"><a href=\"[^\"]*(?<Link>/film(?<ID>[^\"]*?).html)\">(?<Title>.*?)</";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "original</dt>[^<]*<dd>(?<Title>[^<]+)</dd>";
            }
        }

        protected override string VisualSectionRegex
        {
            get { return "<a href=\"(/es/filmimages\\.php\\?movie_id=\\d+)\">Im&aacute;genes&nbsp;<em>"; }
        }

        //private const string baseImageRegex = "pics\\.filmaffinity\\.com\\\\/([^\"]+)\",\"description\":[^,]+,\"type_id\":";

        //protected override string PostersRegex
        //{
        //    get
        //    {
        //        return baseImageRegex + "\"Posters";
        //    }
        //}

        //protected override string BackdropsRegex
        //{
        //    get
        //    {
        //        return baseImageRegex + "\"Fotogramas";
        //    }
        //}
    }
}
