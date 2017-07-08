using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.CSFD)]
    internal class CSFDCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.CSFD; }
        }

        public override Country Country
        {
            get { return Country.CzechRep; }
        }

        public override string Host
        {
            get { return "http://www.csfd.cz"; }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }


        private bool ProcessPage(string input, string inputIMDBID, bool skipImages)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(input))
            {
                string _imdbid = nfoHelper.ExtractIMDBId(input);
                if (!string.IsNullOrEmpty(inputIMDBID) && !string.IsNullOrEmpty(_imdbid) && string.Compare(_imdbid, inputIMDBID) != 0)
                {
                    return _result;
                }
                
                if (!string.IsNullOrEmpty(inputIMDBID) && string.IsNullOrEmpty(_imdbid))
                {
                    return _result;
                }

                if (!string.IsNullOrEmpty(inputIMDBID) && !string.IsNullOrEmpty(_imdbid) && string.Compare(_imdbid, inputIMDBID) == 0)
                {
                    m_MatchedByIMDBId = true;
                }

                MovieInfo _info = new MovieInfo();

                Match _match = null;

                string _regex = @"<title>(?<Title>[^/^\(]*)/? (?<OriginalTitle>[^\(]*)";
                _info.Name = GetItem(input, _regex, "Title");

                if (!string.IsNullOrEmpty(_info.Name))
                {
                    _info.OriginalTitle = GetItem(input, _regex, "OriginalTitle");
                    if (string.IsNullOrEmpty(_info.OriginalTitle))
                    {
                        _match = Regex.Match(input, @"/flag_[^>]+>.*?<h3>(?<1>[^<]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        if(_match.Success)
                        {
                            _info.OriginalTitle = _match.Groups[1].Value;
                        }
                    }
                    _info.Year = GetItem(input, ", (?<Year>\\d{4}),", "Year");
                    if (string.IsNullOrEmpty(_info.Year))
                    {
                        _match = Regex.Match(input, @",\s(?<1>\d{4}),", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        if (_match.Success)
                        {
                            _info.Year = _match.Groups[1].Value;
                        }
                    }

                    _match = Regex.Match(input, "<h2 class=\"average\">(?<Rating>[0-9]+)%</h2>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    if(_match.Success)
                    {
                        string _r = _match.Groups["Rating"].Value.Replace(",", ".").Trim();
                        double _dr = 0d;
                        if(!string.IsNullOrEmpty(_r) && Double.TryParse(_r, out _dr))
                        {
                            _info.Rating = (_dr / 10).ToString("F2");
                        }
                    }

                    _match = Regex.Match(input, @",\s\d{4},\s(?<1>\d*?)\smin", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    if (_match.Success)
                    {
                        _info.Runtime = _match.Groups[1].Value;
                    }
                    _match = Regex.Match(input, "alt=\"Odrážka\"\\s+class=\"[^\"]+\"/>(?<1>.*?)<", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (_match.Success)
                    {
                        _info.Overview = _match.Groups[1].Value.Replace("<p>", "").Replace("<strong>", "").Replace("</strong>", "").Replace("<br />", "").
                            Replace("</p>", "").Replace("<span class=\"source\">", "").Replace("</span>","").Replace("&nbsp;", "").Trim('\n').Trim('\t');
                    }
                    string[] _genres = null;
                    _match = Regex.Match(input, "<p class=\"genre\">(?<Genre>[^<]+)</p", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (_match.Success)
                    {
                        _genres = _match.Groups[1].Value.Split('/');
                        if (_genres != null && _genres.Count() != 0)
                        {
                            _info.Genre = _genres.ToTrimmedList();
                        }
                    }
                    string _imageUrl = null;
                    _match = Regex.Match(input, "(?<Cover>http://img\\.csfd\\.cz/posters/\\d+/\\d+[^\\.]+.jpg)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (_match.Success)
                    {
                        _imageUrl = _match.Groups[1].Value;
                    }
                    _info.IMDBID = _imdbid;
                    
                    string _id = null;
                    _match = Regex.Match(input, "href=\"/film/(?<1>[0-9]*)-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (_match.Success)
                    {
                        _id = _match.Groups[0].Value;
                    }
                    if (string.IsNullOrEmpty(_id))
                    {
                        _id = Helpers.GetUniqueFilename("");
                    }

                    if (!IsValidYear(_info.Year))
                    {
                        return false;
                    }

                    if (!string.IsNullOrEmpty(_info.Name))
                    {
                        ResultMovieItem _movieItem = new ResultMovieItem(_id, _info.Name, _imageUrl, this.CollectorName);
                        _movieItem.MovieInfo = _info;
                        _movieItem.MovieInfo.Name = _info.Name;
                        if(string.IsNullOrEmpty(_movieItem.MovieInfo.IMDBID))
                        {
                            _movieItem.MovieInfo.IMDBID = inputIMDBID;
                        }

                        ResultsList.Add(_movieItem);
                        _result = true;
                    }
                }
            }
            return _result;
        }

        private bool m_MatchedByIMDBId = false;

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            //string _resultsPage = Helpers.GetPage(string.Format("{0}/hledani-filmu-hercu-reziseru-ve-filmove-databazi/?search={1}", Host, keywords));
            string _resultsPage = Helpers.GetPage(string.Format("{0}/hledat/?q={1}", Host, keywords.Replace(" ","+")));
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                // if the page has IMDBID it is directly a moviepage
                string _imdbId = nfoHelper.ExtractIMDBId(_resultsPage);
                if (!string.IsNullOrEmpty(_imdbId))
                {
                    // direct page
                    bool _r = ProcessPage(_resultsPage, imdbID, skipImages);
                    if (_r)
                    {
                        _result = true;
                    }
                }
                else
                {
                    // multiple results page

                    // Group 1 = Link, Group 2 = Title
                    Regex _reg = new Regex("\"subject\"><a\\shref=.(?<Link>/film/(?<ID>.*?)-.*?).\\s.*?>(?<Title>.*?)</a>", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_resultsPage))
                    {
                        int _cnt = 0;
                        List<string> _IDs = new List<string>();
                        foreach (Match _match in _reg.Matches(_resultsPage))
                        {
                            if (FileManager.CancellationPending)
                            {
                                return ResultsList.Count != 0;
                            }

                            string _id = _match.Groups["ID"].Value.ToLowerInvariant();
                            if (_IDs.Contains(_id))
                            {
                                continue; // avoid duplicates
                            }
                            _IDs.Add(_id);

                            string _page = Helpers.GetPage(string.Format("{0}{1}", Host, _match.Groups["Link"].Value), Encoding.UTF8, true);
                            if (!string.IsNullOrEmpty(_page))
                            {
                                bool _r = ProcessPage(_page, imdbID, skipImages);
                                if (_r)
                                {
                                    _result = true;
                                    if (m_MatchedByIMDBId)
                                    {
                                        break;
                                    }
                                }
                            }
                            _cnt++;
                            
                            if (_cnt == 15) // limit to 15 results as the search engine is crazy returning huge amount of matches
                            {
                                break;
                            }
                        }
                    }
                }
            }


            return _result;
        }
    }
}
