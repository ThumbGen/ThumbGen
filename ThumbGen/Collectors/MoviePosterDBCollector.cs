using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.MOVIEPOSTERDB)]
    internal class MoviePosterDBCollector: BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.MOVIEPOSTERDB; }
        }

        public override bool SupportsIMDbSearch
        {
            get
            {
                return true;
            }
        }

        public override Country Country
        {
            get { return Country.International; }
        }

        public override string Host
        {
            get { return "http://movieposterdb.com"; }
        }

        protected override string  SearchMask
        {
            get 
            { 
                return "http://www.movieposterdb.com/search/?type=movies&query={0}";
            }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "href=\"(?<Link>/movie/(?<ID>[0-9]*)/[^\\\"]*)\">(?<Title>[^\"]*)</a.*?<b>(?<Year>[0-9]+)</b>";
            }
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(imdbID))
            {
                keywords = imdbID.Replace("tt", "");

                string _title = null;
                _result = DoProcessing(string.Format("http://www.movieposterdb.com/movie/{0}", keywords), keywords, _title, null);
            }
            else
            {
                keywords = keywords.Replace(" ", "+");


                string page = Helpers.GetPage(string.Format(SearchMask, keywords), Encoding.GetEncoding(1252));
                Regex regex = new Regex(SearchListRegex, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (!string.IsNullOrEmpty(page))
                {
                    foreach (Match match in regex.Matches(page))
                    {
                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }
                        try
                        {
                            string _link = this.Host + match.Groups["Link"].Value;
                            string _id = match.Groups["ID"].Value; // same as imdb (but without tt)
                            string _title = HttpUtility.HtmlDecode(match.Groups["Title"].Value);
                            string _year = match.Groups["Year"].Value;

                            if (!IsValidYear(_year))
                            {
                                continue;
                            }

                            _result = DoProcessing(_link, _id, _title, _year);
                        }
                        catch { }
                    }
                }
            }
            return _result;

        }

        private bool DoProcessing(string _link, string _id, string _title, string _year)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(_link))
            {
                string _gallery = Helpers.GetPage(_link);
                // process the groups
                Regex _groupReg = new Regex("<a href=\"(/poster/[^\"]*)\">", RegexOptions.IgnoreCase);
                if (!string.IsNullOrEmpty(_gallery))
                {
                    if (string.IsNullOrEmpty(_title)) // when methid is called having the imdbid, so it goes directly to the Gallery page
                    {
                        _title = Regex.Match(_gallery, "<span itemprop=\"name\">(?<Title>[^<]+)</span>").Groups["Title"].Value;
                        _year = Regex.Match(_gallery, "<span itemprop=\"copyrightYear\">(?<Year>[^<]+)</span>").Groups["Year"].Value;
                    }

                    if (string.IsNullOrEmpty(_title))
                    {
                        return _result;
                    }

                    foreach (Match gmatch in _groupReg.Matches(_gallery))
                    {
                        string _glink = this.Host + gmatch.Groups[1].Value;

                        string _groupGallery = Helpers.GetPage(_glink);
                        if (!string.IsNullOrEmpty(_groupGallery))
                        {
                            var posterLink = Regex.Match(_groupGallery, "Poster\" src=\"(?<Cover>[^\"]+)\"").Groups["Cover"].Value;
                            var lang = Regex.Match(_groupGallery, "class=\"icon\" src=\"(?<Lang>/images/flags/[^\\.]+\\.png)").Groups["Lang"].Value;
                            if(!string.IsNullOrEmpty(posterLink))
                            {
                                var _movieItem = new ResultMovieItem(_id, _title, posterLink, CollectorName);
                                _movieItem.CollectorMovieUrl = _link;
                                _movieItem.MovieInfo = new MovieInfo();
                                _movieItem.MovieInfo.IMDBID = "tt" + _id;
                                _movieItem.MovieInfo.Name = _title;
                                _movieItem.MovieInfo.Year = _year;
                                _movieItem.LanguageImageUrl = !string.IsNullOrEmpty(lang) ? Host + lang : string.Empty ;
                                ResultsList.Add(_movieItem);
                                _result = true;
                            }
                        }
                    }

                    Regex _galReg = new Regex("<a href=\"/poster/[^\\\"]*\">[^\"]*<img src=\"(http://www.movieposterdb.com/posters[^\"]*)\"(?:[\\w\\W])*?<img src=\"([^\\\"]*)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    // now extract all posters that have no group, so we can build directly the link to the target poster
                    foreach (Match match2 in _galReg.Matches(_gallery))
                    {
                        // group 1 = small poster link
                        // group 2 = link to language flag
                        string _imageUrl = match2.Groups[1].Value.Replace("/t_", "/l_").Replace("/s_", "/l_");
                        string langUrl = match2.Groups[2].Value;

                        // if the lang url is not .png then it's not language, wrong capture, skip it
                        if(!langUrl.EndsWith(".png"))
                        {
                            continue;
                        }

                        ResultMovieItem _movieItem = new ResultMovieItem(_id, _title, _imageUrl, CollectorName);
                        _movieItem.MovieInfo = new MovieInfo();
                        _movieItem.MovieInfo.IMDBID = "tt" + _id;
                        _movieItem.MovieInfo.Name = _title;
                        _movieItem.MovieInfo.Year = _year;
                        _movieItem.CollectorMovieUrl = _link;
                        _movieItem.LanguageImageUrl = langUrl;
                        ResultsList.Add(_movieItem);
                        _result = true;
                    }
                }
            }
            return _result;
        }
    }
}
