using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.CINEMAPTGATE)]
    internal class CinemaPTGateCollector : BaseCollector
    {

        public override string CollectorName
        {
            get { return BaseCollector.CINEMAPTGATE; }
        }

        public override Country Country
        {
            get { return Country.Portugal; }
        }

        public override string Host
        {
            get { return "http://cinema.ptgate.pt"; }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "<h2 class=\"title\">(?<OriginalTitle>[^<]+)</h2>";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "<title>(?<Title>[\\W\\w]+) \\((?<Year>\\d+)\\)</title>";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "<title>(?<Title>[\\W\\w]+) \\((?<Year>\\d+)\\)</title>";
            }
        }

        protected override string RatingRegex
        {
            get
            {
                return "alt=\"PTGate\" />\\s+</div>\\s+<span>(?<Rating>[0-9.]+)</span>";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "<h2>Sinopse</h2>\\s+<p>\\s+(?<Plot>[^<]+)+\\s+<";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);

            if (!string.IsNullOrEmpty(input))
            {
                Regex _reg;

                // rating

                // must multiply rating by 2 to reach x/10 (they have only 5 stars)
                try
                {
                    _result.Rating = string.Format("{0:0.#}", 2 * _result.dRating);
                }
                catch { }

                // country
                _result.Countries.Clear();
                _reg = new Regex("<b>País:</b> ([^<]*)<br />", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(input))
                {
                    _result.Countries.AddRange(_reg.Matches(input)[0].Groups[1].Value.Split(new char[] { ',' }).ToTrimmedList());
                }

                // genres
                _result.Genre.Clear();
                _reg = new Regex("<b>Género:</b>([^<]*)", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(input))
                {
                    _result.Genre.AddRange(_reg.Matches(input)[0].Groups[1].Value.Split(new char[] { ',' }).ToTrimmedList());
                }

                // runtime
                _reg = new Regex("<b>Duração:</b>([0-9 ]*)", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(input))
                {
                    _result.Runtime = _reg.Matches(input)[0].Groups[1].Value.Trim();
                }

                // director
                string _directorArea = Helpers.GetSubstringBetweenStrings(input, "<b>Realização:</b>", "<b>Intérpretes:");
                if (!string.IsNullOrEmpty(_directorArea))
                {
                    _reg = new Regex("href=\"/pessoas/[0-9]*\">([^<]*)", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_directorArea))
                    {
                        _result.Director.Add(_reg.Matches(input)[0].Groups[1].Value.Trim());
                    }
                    else
                    {
                        _reg = new Regex("<b>Realização:</b><br />([^<]*)", RegexOptions.IgnoreCase);
                        if (_reg.IsMatch(_directorArea))
                        {
                            _result.Director.Add(_reg.Matches(input)[0].Groups[1].Value.Trim());
                        }
                    }
                }

                // cast
                string _castArea = Helpers.GetSubstringBetweenStrings(input, "<b>Intérpretes:</b><br", "<br");
                if (!string.IsNullOrEmpty(_castArea))
                {
                    _reg = new Regex("href=\"/pessoas/[0-9]*\">([^<]*)</a>", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_castArea))
                    {
                        foreach (Match _match in _reg.Matches(_castArea))
                        {
                            _result.Cast.Add(_match.Groups[1].Value.Trim());
                        }
                    }
                }

                // plot
                _result.Overview = _result.Overview.Trim();

                // classification
                _reg = new Regex("<b>Classificação:</b>([^<]*)", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(input))
                {
                    _result.Certification = _reg.Matches(input)[0].Groups[1].Value.Trim();
                }
            }

            return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            List<string> _IDs = new List<string>();

            string _resultsPage = Helpers.GetPage(string.Format("{0}/pesquisa/?q={1}", Host, HttpUtility.UrlEncode(keywords, Encoding.GetEncoding(1252)).Replace(" ", "+")), Encoding.GetEncoding(1252));
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                Regex _reg = new Regex("<a href=\"(/filmes[^\"]*/([0-9]*))\"", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(_resultsPage))
                {
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }
                        string _relLink = _match.Groups[1].Value;
                        string _id = _match.Groups[2].Value;
                        if (_IDs.Contains(_id))
                        {
                            continue; //avoid duplicates...
                        }
                        _IDs.Add(_id);
                        if (!string.IsNullOrEmpty(_relLink))
                        {
                            string _moviePageLink = string.Format("{0}{1}", Host, _relLink);
                            string _moviePage = Helpers.GetPage(_moviePageLink, Encoding.GetEncoding(1252));
                            if (!string.IsNullOrEmpty(_moviePage))
                            {
                                string _imageUrl = null;
                                string _title = null;
                                Regex _infoReg = new Regex("<title>(?<Title>[\\W\\w]+) \\((?<Year>\\d+)\\)</title>", RegexOptions.IgnoreCase);
                                if (_infoReg.IsMatch(_moviePage))
                                {
                                    _title = _infoReg.Matches(_moviePage)[0].Groups["Title"].Value.Trim();
                                }
                                _infoReg = new Regex("<img src=\"(?<Url>/Movies/\\d+.jpg)\"", RegexOptions.IgnoreCase);
                                if (_infoReg.IsMatch(_moviePage))
                                {
                                    string _relUrl = _infoReg.Matches(_moviePage)[0].Groups["Url"].Value;
                                    if (!string.IsNullOrEmpty(_relUrl))
                                    {
                                        _imageUrl = string.Format("{0}{1}", Host, _relUrl);
                                    }
                                }

                                if (!string.IsNullOrEmpty(_title))
                                {
                                    string _imdbId = nfoHelper.ExtractIMDBId(_moviePage);
                                    if (string.IsNullOrEmpty(_imdbId) && (!string.IsNullOrEmpty(imdbID)))
                                    {
                                        _imdbId = imdbID;
                                    }
                                    ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, _imageUrl, this.CollectorName);
                                    _movieItem.CollectorMovieUrl = _moviePageLink;
                                    _movieItem.MovieInfo = GetMovieInfo(_moviePage);
                                    _movieItem.MovieInfo.Name = _title;
                                    if(string.IsNullOrEmpty(_movieItem.MovieInfo.IMDBID))
                                    {
                                        _movieItem.MovieInfo.IMDBID = _imdbId;
                                    }

                                    if (!IsValidYear(_movieItem.MovieInfo.Year))
                                    {
                                        continue;
                                    }

                                    ResultsList.Add(_movieItem);
                                    _result = true;
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
