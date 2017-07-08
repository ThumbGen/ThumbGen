using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.OUTNOW)]
    internal class OutnowCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.OUTNOW; }
        }

        public override Country Country
        {
            get { return ThumbGen.Country.Germany; }
        }

        public override string Host
        {
            get { return "http://www.outnow.ch"; }
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
                return "http://outnow.ch/Suche/?text={0}";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<td><a href=\"(?<RelLink>[^\"]+)\" [^>]+>(?<OriginalTitle>[^<]+)</a> \\((?<Year>[0-9]{4})";
            }
        }

        protected override string IMDBIdRegex
        {
            get
            {
                return "href=\"http://www.imdb.com/title/(?<IMDBID>tt[0-9]{7})/";
            }
        }

        protected override string TitleRegex
        {
            get
            {
                return "\"local\">(?<Title>[^<]+)";
            }
        }

        protected override string OriginalTitleRegex
        {
            get
            {
                return "\"cname\">(?<OriginalTitle>[^<]+)";
            }
        }

        protected override string YearRegex
        {
            get
            {
                return "id=\"cyear\">(?<Year>[^<]+)";
            }
        }
        
        protected override string RuntimeRegex
        {
            get
            {
                return "\"label\">Laufzeit:</span> (?<Runtime>[^\\s]+)";
            }
        }

        protected override string PlotRegex
        {
            get
            {
                return "Infos\\szu(.*?)\n<p>(?<Plot>(.*?))</p><p";
            }
        }

        protected override string RatingRegex
        {
            get
            {
                return "title=\"Bewertung: (?<Rating>[0-9\\.,]+)\\s";
            }
        }

        protected override string GenresRegex
        {
            get
            {
                return "<a href=\"[^\"]+\"\\stitle=\"Genre[^>]+>(?<Genre>[^<]+)";
            }
        }

        protected override string ReleaseDateRegex
        {
            get
            {
                return "Kinostart:</span>\\s(?<Release>[0-9\\.,]+)";
            }
        }

        protected override string DirectorRegex
        {
            get
            {
                return "Regie(.*?)\">(?<Director>[^<]+)</a>";
            }
        }

        protected override string ActorsRegex
        {
            get
            {
                return "<td><p><a href=\"/Person[^>]+>(?<Actor>[^<]+)";
            }
        }

        protected override string CountryRegex
        {
            get
            {
                return "<a href=\"/Movies/Suche/Land/(.*?)>(?<Country>[^<]+)</a>";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "<div class=\"mainposter\">(.*?)<img\\ssrc=\"(?<Cover>/Media/Movies/[^_]+_[^\\.]+.jpg)\"\\s";
            }
        }

        protected override string BackdropsRegex
        {
            get
            {
                return "Film-Szenenbild[^\"]+ansehen\"><img\\ssrc=\"(?<Backdrop>/Media/Movies/[^_]+_[^\\.]+\\.[^\"]+)";
            }
        }

        private string WallpapersRegex
        {
            get
            {
                return "Wallpaper[^\"]+ansehen\"><img\\ssrc=\"(?<Backdrop>/Media/Movies/Wallpapers[^\"]+)";
            }
        }

        protected override bool ProcessVisualSection(string Link, MovieInfo movieInfo, string id)
        {
            bool _result = false;

            string _page = Helpers.GetPage(Link);

            if (!string.IsNullOrEmpty(_page))
            {
                Regex _reg = new Regex(BackdropsRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (_reg.IsMatch(_page))
                {
                    foreach (Match _m2 in _reg.Matches(_page))
                    {
                        string _thumbUrl = string.Format("http://www.outnow.ch{0}", _m2.Groups["Backdrop"].Value);
                        string _originalUrl = _thumbUrl.Replace("_small.", ".");

                        if (!string.IsNullOrEmpty(movieInfo.Name) && !string.IsNullOrEmpty(_thumbUrl) && !string.IsNullOrEmpty(_originalUrl))
                        {
                            BackdropsList.Add(new BackdropItem(id, movieInfo.IMDBID, this.CollectorName, _thumbUrl, _originalUrl));
                        }
                    }
                }
            }

            return _result;
        }

        private bool ProcessWallpaper(string Link, MovieInfo movieInfo, string id)
        {
            bool _result = false;

            string _page = Helpers.GetPage(Link);

            if (!string.IsNullOrEmpty(_page))
            {
                Regex _reg = new Regex(WallpapersRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (_reg.IsMatch(_page))
                {
                    foreach (Match _m2 in _reg.Matches(_page))
                    {
                        string _thumbUrl = string.Format("http://www.outnow.ch{0}", _m2.Groups["Backdrop"].Value);
                        string _originalUrl = null;

                        if (!string.IsNullOrEmpty(movieInfo.Name) && !string.IsNullOrEmpty(_thumbUrl))
                        {
                            BackdropItem _bi = new BackdropItem(id, movieInfo.IMDBID, this.CollectorName, _thumbUrl, _originalUrl);
                            _bi.GetOriginalUrl = new BackdropBase.GetOriginalUrlHandler(GetWallpaperLink);
                            BackdropsList.Add(_bi);
                        }
                    }
                }
            }

            return _result;
        }

        public static string GetWallpaperLink(string thumbUrl)
        {
            // http://outnow.ch/Media/Movies/Wallpapers/2006/Cars/003633
            // http://outnow.ch/Movies/2006/Cars/Wallpapers/003633/

            string _result = string.Empty; // do not return null to avoid resending the request

            if(!string.IsNullOrEmpty(thumbUrl))
            {
                string _link = Regex.Replace(thumbUrl, "outnow\\.ch/Media/Movies/Wallpapers/([0-9]{4})/([^/]+)/([0-9]+)", "outnow.ch/Movies/$1/$2/Wallpapers/$3/");
                if (!string.IsNullOrEmpty(_link))
                {
                    string _page = Helpers.GetPage(_link);
                    if (!string.IsNullOrEmpty(_page))
                    {
                        //Match _m = Regex.Match(_page, "{\\sid:\\s(?<ID>[0-9]+),\\swallpaper:\\s'(?<Wallpaper>[0-9]+)'", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        //if (_m.Success)
                        //{
                        //    HttpWebRequest _req = (HttpWebRequest)WebRequest.Create("http://outnow.ch/_ajax/");
                        //    // Set values for the request back
                        //    _req.Method = "POST";
                        //    _req.ContentType = "application/json";

                        //    string _postData = string.Format("{{\"class\": \"Movies\", \"method\": \"showWallpaper\", \"args\": {{\"id\": {0}, \"wallpaper\": \"{1}\"}}}}", _m.Groups["ID"].Value, _m.Groups["Wallpaper"].Value);

                        //    _req.ContentLength = _postData.Length;
                        //    // Write the request
                        //    try
                        //    {
                        //        StreamWriter _stOut = new StreamWriter(_req.GetRequestStream(), System.Text.Encoding.Default);
                        //        _stOut.Write(_postData);
                        //        _stOut.Close();
                        //    }
                        //    catch
                        //    {
                        //        return _result;
                        //    }
                        //    // Do the request to get the response
                        //    string _strResponse = null;
                        //    try
                        //    {
                        //        using (StreamReader stIn = new StreamReader(_req.GetResponse().GetResponseStream()))
                        //        {
                        //            _strResponse = stIn.ReadToEnd();
                        //            stIn.Close();
                        //        }
                        //    }
                        //    catch
                        //    {
                        //        return _result;
                        //    }
                        //    if (!string.IsNullOrEmpty(_strResponse))
                        //    {
                        //        return _result;
                        //    }
                        //}

                        Match _m = Regex.Match(_page, "><img src=\"(?<Link>/Media/Movies/Wallpapers[^\"]+/view)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        if (_m.Success)
                        {
                            // http://outnow.ch/Media/Movies/Wallpapers/2006/Cars/003631/615769f781383b3a203232fa3516e7d8/view - good
                            // http://www.outnow.ch/Media/Movies/Wallpapers/2006/Cars/003629/d58392a9830a4391d246bed08067bde5/view
                            // http://outnow.ch/Media/Movies/Wallpapers/2006/Cars/003633/3d7874e263a5c8438f4d244d89c7bc7a/download

                            //req.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/532.5 (KHTML, like Gecko) Chrome/4.0.249.78 Safari/532.5";
                            //req.Headers[HttpRequestHeader.Accept] = "text/html, image/gif, image/jpeg, image/pjpeg, image/pjpeg, application/x-shockwave-flash, application/x-ms-application, application/x-ms-xbap, application/vnd.ms-xpsdocument, application/xaml+xml, application/msword, */*";

                            _result = string.Format("http://outnow.ch{0}", _m.Groups["Link"].Value.Replace("/view", "/download"));

                            HttpWebRequest webClient = (HttpWebRequest)WebRequest.Create(_result);
                            webClient.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; .NET CLR 2.0.50727; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; .NET4.0C; .NET4.0E)";
                            webClient.Accept = "image/gif, image/jpeg, image/pjpeg, image/pjpeg, application/x-shockwave-flash, application/x-ms-application, application/x-ms-xbap, application/vnd.ms-xpsdocument, application/xaml+xml, application/msword, */*";
                            webClient.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                            webClient.KeepAlive = true;
                            //webClient.CookieContainer.Add(new Cookie("Cookie", "__utma=5962157.986135282.1273392436.1273392436.1273407971.2; __utmb=5962157.8.9.1273408014609; __utmz=5962157.1273392436.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none); __sess=qcfsvf9ekqieclt9q8nl3jg674; __utmc=5962157"));

                            WebResponse _resp = webClient.GetResponse();
                            //string firstRes = Encoding.UTF8.GetString(firstResponse);  

                            
                        }
                    }
                }
            }
            return _result; 
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            MovieInfo _result = new MovieInfo();

            _result.Name = GetItem(input, TitleRegex, "Title");
            _result.OriginalTitle = GetItem(input, OriginalTitleRegex, "OriginalTitle");
            if (string.IsNullOrEmpty(_result.Name))
            {
                _result.Name = _result.OriginalTitle;
            }
            _result.Year = GetItem(input, YearRegex, "Year");
            _result.IMDBID = GetItem(input, IMDBIdRegex, "IMDBID");
            _result.Runtime = GetItem(input, RuntimeRegex, "Runtime");
            _result.Overview = GetItem(input, PlotRegex, "Plot", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim().Replace("<i>", "").Replace("</i>", "");
            _result.Rating = GetItem(input, RatingRegex, "Rating").Replace(",", ".");
            if (!string.IsNullOrEmpty(_result.Rating))
            {
                _result.Rating = (_result.dRating * 10 / 6).ToString();
            }
            _result.Genre.AddRange(GetItems(input, GenresRegex, "Genre"));
            _result.ReleaseDate = GetItem(input, ReleaseDateRegex, "Release");
            _result.Director.AddRange(GetItems(input, DirectorRegex, "Director", RegexOptions.Singleline | RegexOptions.IgnoreCase));
            _result.Cast.AddRange(GetItems(input, ActorsRegex, "Actor"));
            _result.Countries.AddRange(GetItems(input, CountryRegex, "Country", RegexOptions.Singleline | RegexOptions.IgnoreCase));
            //_result.Studios.AddRange(GetItems(input, StudiosRegex, 1));

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

                string _imageUrl = string.Format("http://www.outnow.ch{0}", GetItem(input, CoverRegex, "Cover", RegexOptions.Singleline | RegexOptions.IgnoreCase));
                _imageUrl = Regex.Replace(_imageUrl, "_[^\\.]+.", ".", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                if (!string.IsNullOrEmpty(_movieInfo.Name))
                {
                    ResultMovieItem _movieItem = new ResultMovieItem(id, _movieInfo.Name, _imageUrl, this.CollectorName);
                    _movieItem.MovieInfo = _movieInfo;
                    _movieItem.CollectorMovieUrl = link;
                    ResultsList.Add(_movieItem);
                    _result = true;
                }

                if (!skipImages)
                {
                    // process the VisualSection if any
                    bool _res = ProcessVisualSection(link + "Bilder/", _movieInfo, id);
                    if (_res)
                    {
                        _result = true;
                    }

                    //_res = ProcessWallpaper(link + "Wallpapers/", _movieInfo, id);
                    //if (_res || _result)
                    //{
                    //    _result = true;
                    //}
                }
            }

            return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            string _resultsPage = Helpers.GetPage(string.Format(SearchMask, keywords.Replace(" ", "+")));
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                Regex _reg = new Regex(SearchListRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (_reg.IsMatch(_resultsPage))
                {
                    // we got the results page
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        string _relLink = string.Format("http://www.outnow.ch{0}", _match.Groups["RelLink"].Value);
                        //string _title = _match.Groups["Title"].Value;
                        string _originalTitle = _match.Groups["OriginalTitle"].Value;
                        string _year = _match.Groups["Year"].Value;
                        string _id = _match.Groups["RelLink"].Value; // use rellink as id

                        if (!IsValidYear(_year))
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
                            bool _r = ProcessPage(_page, _id, skipImages, _relLink, null, _originalTitle);
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
