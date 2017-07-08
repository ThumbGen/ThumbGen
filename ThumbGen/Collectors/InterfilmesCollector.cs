using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.INTERFILMES)]
    internal class InterfilmesCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.INTERFILMES; }
        }

        public override Country Country
        {
            get { return ThumbGen.Country.Brasil; }
        }

        public override string Host
        {
            get { return "http://www.interfilmes.com"; }
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
                return string.Format("{0}/busca.html", Host);
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<a href=\"(?<RelLink>filme_(?<ID>[0-9]+)_[^\"]+)\">.*?<font color=#FFFFFF face=Verdana size=2>(?<Title>[^\\(]+).*?Ano de Lançamento: (?<Year>[0-9]+)";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "(?<Cover>FILMES/[0-9]+/[0-9]+/fotocapa\\.jpg)";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "Título no Brasil:</u>.*?(?<Title>[^<]+)<br>";
            }
        }
        
        protected override string OriginalTitleRegex
        {
            get
            {
                return "Título Original:</u>.*?(?<OriginalTitle>[^<]+)<br>";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "Ano de Lançamento:</u>.*?(?<Year>[0-9]+)<br>";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "itemprop=\"description\">(?<Plot>.*?)</";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "Gênero:</u>.*?(?<Genre>[^<]+)<br>";
            }
        }

        protected override string CountryRegex
        {
            get
            {
                return "País de Origem:</u>.*?(?<Country>[^<]+)<br>";
            }
        }

        protected override string RuntimeRegex
        {
            get
            {
                return "(?<Runtime>[0-9]+) minutos";
            }
        }

        protected override string ReleaseDateRegex
        {
            get
            {
                return "Estréia no Brasil:</u>&nbsp;(?<ReleaseDate>[0-9/]+)<br>";
            }
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = base.GetMovieInfo(input);

            string _country = GetItem(input, this.CountryRegex, "Country").Trim();
            if (!string.IsNullOrEmpty(_country))
            {
                _result.Countries = _country.Split('/').ToTrimmedList();
            }

            string _genre = GetItem(input, this.GenresRegex, "Genre").Trim();
            if (!string.IsNullOrEmpty(_genre))
            {
                _result.Genre = _genre.Split('/').ToTrimmedList();
            }

            _result.Overview = GetItem(input, PlotRegex, "Plot", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
            _result.Year = GetItem(input, YearRegex, "Year", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

            return _result;
        }

        protected override string GetCoverLink(string input)
        {
            string _imageUrl = GetItem(input, CoverRegex, "Cover");
            if (!string.IsNullOrEmpty(_imageUrl))
            {
                _imageUrl = string.Format("{0}/{1}", Host, _imageUrl);
            }
            return _imageUrl;
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

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            string _resultsPage = Helpers.GetPagePost(SearchMask, Encoding.GetEncoding("ISO-8859-1"), string.Format("&search={0}", keywords));
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
                        string _relLink = string.Format("{0}/{1}", Host, _match.Groups["RelLink"].Value);

                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        string _page = Helpers.GetPage(_relLink, Encoding.GetEncoding("ISO-8859-1"));
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
