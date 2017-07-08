using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.ALPACINE)]
    internal class AlpacineCollector : BaseCollector
    {
        public AlpacineCollector()
        {
        }

        public override string CollectorName
        {
            get { return ALPACINE; }
        }

        public override Country Country
        {
            get { return Country.Spain; }
        }

        public override string Host
        {
            get { return "http://alpacine.com"; }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        protected override MovieInfo GetMovieInfo(string id)
        {
            MovieInfo _result = new MovieInfo();
            
            // title -> description"\scontent="(?<1>.*?)\s-\s
            // OriginalTitle -> tulo\soriginal:</span>\s(?<1>.*?)</div>
            // Year -> \s\(([0-9]*)\)\s-\s
            // Runtime -> uraci.n:</span>\s(?<1>.*?)\sm
            // Plot -> inopsis:</div><.*?>([^<]*)<
            // Genres, Country, Director, Cast  -> /">([^\<]*)</a>
            // Rating -> voto">[\s\t\n\r]*?(\d.*)[\s|\t|\n|\r|\v|\f]

            string _page = Helpers.GetPage(string.Format("{0}/pelicula/{1}/", Host, id));
            if (!string.IsNullOrEmpty(_page))
            {
                // title
                Regex _reg = new Regex("description\"\\scontent=\"(.*)\\s-\\s", RegexOptions.IgnoreCase); 
                if (_reg.IsMatch(_page))
                {
                    _result.Name = HttpUtility.HtmlDecode(_reg.Matches(_page)[0].Groups[1].Value);
                }
                // original title
                _reg = new Regex("tulo\\soriginal:</span>\\s(.*)</div>", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    _result.OriginalTitle = HttpUtility.HtmlDecode(_reg.Matches(_page)[0].Groups[1].Value);
                }
                // year
                _reg = new Regex("\\s\\(([0-9]*)\\)\\s-\\s", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    _result.Year = _reg.Matches(_page)[0].Groups[1].Value;
                }
                // runtime
                _reg = new Regex("n:</span>\\s(.*)\\sm", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    _result.Runtime = _reg.Matches(_page)[0].Groups[1].Value;
                }
                // plot
                _reg = new Regex("inopsis:</div><.*?>([^<]*)<", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    _result.Overview = HttpUtility.HtmlDecode(_reg.Matches(_page)[0].Groups[1].Value);
                }
                // rating
                _reg = new Regex("voto\">[\\s\\t\\n\\r]*?(\\d.*)[\\s|\\t|\\n|\\r|\\v|\\f]", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_page))
                {
                    _result.Rating = _reg.Matches(_page)[0].Groups[1].Value;
                }
                // genres
                string _genres = Helpers.GetSubstringBetweenStrings(_page, "titulo\">Género", "titulo\">Pa");
                if (!string.IsNullOrEmpty(_genres))
                {
                    _reg = new Regex("/\">([^\\<]*)</a>", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_genres))
                    {
                        foreach (Match _m in _reg.Matches(_genres))
                        {
                            _result.Genre.Add(HttpUtility.HtmlDecode(_m.Groups[1].Value));
                        }
                    }
                }

                // countries
                string _countries = Helpers.GetSubstringBetweenStrings(_page, ">País:</span>", "titulo\">Durac");
                if (!string.IsNullOrEmpty(_countries))
                {
                    _reg = new Regex("/\">([^\\<]*)</a>", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_countries))
                    {
                        foreach (Match _m in _reg.Matches(_countries))
                        {
                            _result.Countries.Add(HttpUtility.HtmlDecode(_m.Groups[1].Value));
                        }
                    }
                }
                // director
                string _director = Helpers.GetSubstringBetweenStrings(_page, "titulo\">Direcc", "titulo\">Interpretaci");
                if (!string.IsNullOrEmpty(_director))
                {
                    _reg = new Regex("/\">([^\\<]*)</a>", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_director))
                    {
                        foreach (Match _m in _reg.Matches(_director))
                        {
                            _result.Director.Add(HttpUtility.HtmlDecode(_m.Groups[1].Value));
                        }
                    }
                }
                // cast
                string _cast = Helpers.GetSubstringBetweenStrings(_page, "titulo\">Interpretaci", "titulo\">Sinopsis");
                if (!string.IsNullOrEmpty(_cast))
                {
                    _reg = new Regex("/\">([^\\<]*)</a>", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_cast))
                    {
                        foreach (Match _m in _reg.Matches(_cast))
                        {
                            _result.Cast.Add(HttpUtility.HtmlDecode(_m.Groups[1].Value));
                        }
                    }
                }
            }

            return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            keywords = keywords.Replace(" ", "+");

            string input = Helpers.GetPage(string.Format("http://alpacine.com/buscar/?buscar={0}", keywords));
            if (!string.IsNullOrEmpty(input))
            {
                string _reg = "href=\"(?<Link>/pelicula/[^\"]*?)\">(?<Title>.*?)</";
                Regex regex = new Regex(_reg);
                if (regex.IsMatch(input))
                {
                    int count = regex.Matches(input).Count;
                    foreach (Match match in regex.Matches(input))
                    {
                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }
                        string sUrl = match.Groups["Link"].Value;
                        if (sUrl != "")
                        {
                            string _id = sUrl.Substring(10, sUrl.Length - 10 - 1);
                            string _title = match.Groups["Title"].Value;
                            _title = Helpers.StripHTML(_title);
                            if (!string.IsNullOrEmpty(_title))
                            {
                                // get movie info
                                MovieInfo _movieInfo = GetMovieInfo(_id);

                                if (!IsValidYear(_movieInfo.Year))
                                {
                                    continue;
                                }

                                if (skipImages)
                                {
                                    if (!string.IsNullOrEmpty(_title))
                                    {
                                        ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, null, CollectorName);
                                        _movieItem.CollectorMovieUrl = string.Format("{0}{1}", Host, sUrl);
                                        _movieItem.MovieInfo = _movieInfo;
                                        ResultsList.Add(_movieItem);
                                        _result = true;
                                    }
                                }
                                else
                                {
                                    string _gallery = Helpers.GetPage(string.Format("{0}{1}carteles/", Host, sUrl));
                                    if (!string.IsNullOrEmpty(_gallery))
                                    {
                                        string _reg2 = "<a\\shref=\"(?<1>/cartel/.*?)\"\\starget";
                                        Regex _regex2 = new Regex(_reg2);
                                        if (_regex2.IsMatch(_gallery))
                                        {
                                            foreach (Match _galleryMatch in _regex2.Matches(_gallery))
                                            {
                                                string _cartel = _galleryMatch.Groups["1"].Value;
                                                string _coverPage = Helpers.GetPage(string.Format("{0}{1}", Host, _cartel));
                                                if (!string.IsNullOrEmpty(_coverPage))
                                                {
                                                    string _reg3 = "imagen\"\\ssrc=\"(?<1>.*?)\"";
                                                    Regex _regex3 = new Regex(_reg3);
                                                    if (_regex3.IsMatch(_coverPage))
                                                    {
                                                        string _imageUrl = _regex3.Match(_coverPage).Groups["1"].Value;
                                                        if (!string.IsNullOrEmpty(_title) && !string.IsNullOrEmpty(_imageUrl))
                                                        {
                                                            ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, _imageUrl, CollectorName);
                                                            _movieItem.CollectorMovieUrl = string.Format("{0}{1}", Host, sUrl);
                                                            _movieItem.MovieInfo = _movieInfo;
                                                            ResultsList.Add(_movieItem);
                                                            _result = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        }

                    }
                }
            }

            return _result;
        }
    }
}
