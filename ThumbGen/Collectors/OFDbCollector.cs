using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.OFDB)]
    internal class OFDbCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.OFDB; }
        }

        public override Country Country
        {
            get { return Country.Germany; }
        }

        public override string Host
        {
            get { return "http://www.ofdb.de"; }
        }

        public override bool SupportsIMDbSearch
        {
            get
            {
                return true;
            }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        private static string SearchPageRegex = "<a href=\"(film/([0-9]+)[^\\\"]+)\" onmouseover=\"Tip\\('<img src=&quot;([^\\&]+)";

        public override MovieInfo QueryMovieInfo(string imdbId)
        {
            MovieInfo _result = null;

            if (!string.IsNullOrEmpty(imdbId))
            {
                // search by imdbid
                string _search = Search("", imdbId);
                if (!string.IsNullOrEmpty(_search))
                {
                    Regex _reg = new Regex(SearchPageRegex, RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_search))
                    {
                        string _moviePageLink = string.Format("{0}/{1}", Host, _reg.Matches(_search)[0].Groups[1].Value);
                        if (!string.IsNullOrEmpty(_moviePageLink))
                        {
                            _result = GetMovieInfo(Helpers.GetPage(_moviePageLink));
                        }
                    }
                }
            }
            return _result;
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = new MovieInfo();

            if (!string.IsNullOrEmpty(input))
            {
                // imdbid
                _result.IMDBID = nfoHelper.ExtractIMDBId(input);

                Regex _reg;
                //original title
                string _otitle = Helpers.GetSubstringBetweenStrings(input, ">Originaltitel:", "class=\"Normal\">");
                if (!string.IsNullOrEmpty(_otitle))
                {
                    _reg = new Regex("<b>([^<]*)</b>", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_otitle))
                    {
                        _result.OriginalTitle = HttpUtility.HtmlDecode(_reg.Matches(_otitle)[0].Groups[1].Value.Trim());
                    }
                }
                // country
                string _country = Helpers.GetSubstringBetweenStrings(input, ">Herstellungsland:", "class=\"Normal\">");
                if (!string.IsNullOrEmpty(_country))
                {
                    _reg = new Regex("Land&Text=([^\"]*)", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_country))
                    {
                        foreach (Match _m in _reg.Matches(_country))
                        {
                            _result.Countries.Add(HttpUtility.UrlDecode(HttpUtility.HtmlDecode(_m.Groups[1].Value.Trim())));
                        }
                    }
                }
                // director
                string _director = Helpers.GetSubstringBetweenStrings(input, ">Regie:", "class=\"Normal\">");
                if (!string.IsNullOrEmpty(_director))
                {
                    _reg = new Regex(">([^<]+)</span", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_director))
                    {
                        _result.Director.Add(HttpUtility.HtmlDecode(_reg.Matches(_director)[0].Groups[1].Value.Trim()));
                    }
                }
                // cast
                string _cast = Helpers.GetSubstringBetweenStrings(input, ">Darsteller:", "class=\"Normal\">");
                if (!string.IsNullOrEmpty(_cast))
                {
                    _reg = new Regex(">([^<]+)</span", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_director))
                    {
                        foreach (Match _m in _reg.Matches(_cast))
                        {
                            string _actor = HttpUtility.HtmlDecode(_m.Groups[1].Value.Trim());
                            if (!_actor.Contains("[mehr]"))
                            {
                                _result.Cast.Add(_actor);
                            }
                        }
                    }
                }
                // rating
                _reg = new Regex("<br>Note: ([0-9.]*) &nbsp;", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(input))
                {
                    _result.Rating = _reg.Matches(input)[0].Groups[1].Value;
                }
                // genres
                string _genres = Helpers.GetSubstringBetweenStrings(input, ">Genre(s):", "class=\"Normal\">");
                if (!string.IsNullOrEmpty(_genres))
                {
                    _reg = new Regex(">([^<]+)</span", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_genres))
                    {
                        foreach (Match _m in _reg.Matches(_genres))
                        {
                            string _genre = HttpUtility.HtmlDecode(_m.Groups[1].Value.Trim());
                            if (!_genre.Contains("[mehr]"))
                            {
                                _result.Genre.Add(_genre);
                            }
                        }
                    }
                }
                // plot
                string _plotArea = Helpers.GetSubstringBetweenStrings(input, "<b>Inhalt:</", "<b>[mehr]</b");
                if (!string.IsNullOrEmpty(_plotArea))
                {
                    _reg = new Regex("<a href=\"([^\"]*)", RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_plotArea))
                    {
                        string _relLink = HttpUtility.HtmlDecode(_reg.Matches(_plotArea)[0].Groups[1].Value.Trim());
                        if (!string.IsNullOrEmpty(_relLink))
                        {
                            string _linkToPlot = string.Format("{0}/{1}", Host, _relLink);
                            string _plotPage = Helpers.GetPage(_linkToPlot);
                            if (!string.IsNullOrEmpty(_plotPage))
                            {
                                string _area = Helpers.GetSubstringBetweenStrings(_plotPage, "gelesen</b></", "</font></p>");
                                if (!string.IsNullOrEmpty(_area))
                                {
                                    _reg = new Regex("gelesen</b></b>([\\W\\w]+)", RegexOptions.IgnoreCase);
                                    if (_reg.IsMatch(_area))
                                    {
                                        _result.Overview = HttpUtility.HtmlDecode(_reg.Matches(_area)[0].Groups[1].Value.Replace("<br>", "").Replace("<br />", "").Replace("<br/>", "").Replace("</font>", "").Trim());
                                    }
                                }
                            }

                        }
                    }
                }

                // certification
                _reg = new Regex("(FSK [0-9]+)", RegexOptions.IgnoreCase);
                if (_reg.IsMatch(input))
                {
                    _result.Certification = _reg.Matches(input)[0].Groups[1].Value.Trim();
                }
            }
            return _result;
        }

        private string Search(string keywords, string imdbID)
        {
            string _postData1 = string.IsNullOrEmpty(imdbID) ? string.Format("&Kat=Titel&SText={0}", HttpUtility.UrlEncode(keywords, Encoding.UTF8)) : string.Format("&Kat=IMDb&SText={0}", imdbID);

            return Helpers.GetPage(string.Format("{0}/view.php?page=suchergebnis{1}", Host, _postData1), null, Encoding.UTF8, "", false, false);


            //HttpWebRequest _req = (HttpWebRequest)WebRequest.Create(string.Format("{0}/view.php?page=suchergebnis", Host));
            //// Set values for the request back
            //_req.Method = "POST";
            //_req.ContentType = "application/x-www-form-urlencoded";

            //string _postData = string.IsNullOrEmpty(imdbID) ? string.Format("&Kat=Titel&SText={0}", HttpUtility.UrlEncode(keywords, Encoding.UTF8)) : string.Format("&Kat=IMDb&SText={0}", imdbID);

            //_req.ContentLength = _postData.Length;
            //// Write the request
            //try
            //{
            //    StreamWriter _stOut = new StreamWriter(_req.GetRequestStream(), System.Text.Encoding.Default);
            //    _stOut.Write(_postData);
            //    _stOut.Close();
            //}
            //catch
            //{
            //    return _result;
            //}
            //// Do the request to get the response
            //try
            //{
            //    using (StreamReader stIn = new StreamReader(_req.GetResponse().GetResponseStream()))
            //    {
            //        _result = stIn.ReadToEnd();
            //        stIn.Close();
            //    }
            //}
            //catch
            //{
            //    return _result;
            //}

            //return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            string _strResponse = Search(keywords, imdbID);
            if (!string.IsNullOrEmpty(_strResponse))
            {
                _strResponse = Helpers.GetSubstringBetweenStrings(_strResponse, string.IsNullOrEmpty(imdbID) ? "Titel:" : "Filme:", "google_ad_client");
                if (!string.IsNullOrEmpty(_strResponse))
                {
                    // Group 1 = relative link to moviepage, Group 2 = ID, Group 3 = relative link to full thumbnail
                    Regex _reg = new Regex(SearchPageRegex, RegexOptions.IgnoreCase);
                    if (_reg.IsMatch(_strResponse))
                    {
                        foreach (Match _match in _reg.Matches(_strResponse))
                        {
                            if (FileManager.CancellationPending)
                            {
                                return ResultsList.Count != 0;
                            }

                            string _relLink = _match.Groups[1].Value;
                            string _id = _match.Groups[2].Value;
                            string _moviePageLink = string.Format("{0}/{1}", Host, _relLink);
                            string _imageUrl = string.Format("{0}/{1}", Host, _match.Groups[3].Value);

                            string _title = null;
                            string _year = null;
                            string _imdbId = null;
                            // go to moviepage
                            string _moviePage = Helpers.GetPage(_moviePageLink);
                            if (!string.IsNullOrEmpty(_moviePage))
                            {
                                // get IMDb Id
                                _imdbId = nfoHelper.ExtractIMDBId(_moviePage);
                                // get title and year always and MovieInfo in a separate method
                                // Group 1 = Title, Group 2 = year
                                Regex _regTitle = new Regex(@"<title>OFDb - (.*) \(([0-9]{4})\)</title>", RegexOptions.IgnoreCase);
                                if (_regTitle.IsMatch(_moviePage))
                                {
                                    _title = HttpUtility.HtmlDecode(_regTitle.Matches(_moviePage)[0].Groups[1].Value.Trim());
                                    _year = _regTitle.Matches(_moviePage)[0].Groups[2].Value.Trim();
                                }

                                if (!IsValidYear(_year))
                                {
                                    continue;
                                }

                                if (!string.IsNullOrEmpty(_imageUrl) && !string.IsNullOrEmpty(_title))
                                {
                                    ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, _imageUrl, this.CollectorName);
                                    _movieItem.CollectorMovieUrl = _moviePageLink;
                                    _movieItem.MovieInfo = GetMovieInfo(_moviePage);
                                    _movieItem.MovieInfo.Name = _title;
                                    _movieItem.MovieInfo.Year = _year;
                                    
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
