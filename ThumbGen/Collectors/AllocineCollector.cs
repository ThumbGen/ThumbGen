using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using AlloCine;

namespace ThumbGen.Collectors
{

    [MovieCollector(BaseCollector.ALLOCINE)]
    internal class AllocineCollector : BaseCollector
    {
        public override string CollectorName
        {
            get { return BaseCollector.ALLOCINE; }
        }

        public override Country Country
        {
            get { return ThumbGen.Country.France; }
        }

        public override string Host
        {
            get { return "http://www.allocine.fr"; }
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

        private string GetSearchUrl(string keywords)
        {
            return string.Format("http://api.allocine.fr/rest/v3/search?partner=YW5kcm9pZC12M3M&filter=movie,tvseries&q={0}&count=10&format=xml", keywords.Replace(" ", "+"));
        }

        private string GetQueryMovieDetailsUrl(string id)
        {
            return string.Format("http://api.allocine.fr/rest/v3/movie?partner=YW5kcm9pZC12M3M&code={0}&format=xml&profile=large&filter=movie,tvseries", id);
        }

        private string GetMovieUrl(string id)
        {
            return string.Format("http://www.allocine.fr/film/fichefilm_gen_cfilm={0}.html", id);
        }

        private static XName GetName(string name)
        {
            return XName.Get(name, "http://www.allocine.net/v6/ns/");
        }

        protected override MovieInfo GetMovieInfo(string input)
        {
            // input is the id of the movie
            var _result = new MovieInfo();

            if (string.IsNullOrEmpty(input)) return _result;
            var id = -1;
            if (!int.TryParse(input, out id)) return _result;

            var api = new AlloCineApi();
            var movie = api.MovieGetInfo(id);
            if (movie.Error != null) return _result;
            try
            {

                _result.OriginalTitle = movie.OriginalTitle;

                string _title = movie.Title;
                _result.Name = string.IsNullOrEmpty(_title) ? _result.OriginalTitle : _title;

                _result.Year = movie.ProductionYear;

                if (movie.Release != null)
                {
                    _result.ReleaseDate = movie.Release.ReleaseDate;
                }

                if (movie.Trailer != null)
                {
                    _result.Trailer = movie.Trailer.Href;
                }

                _result.Countries = movie.NationalityList.Select(x => x.Value).ToList();

                _result.Genre = movie.GenreList.Select(x => x.Value).ToList();

                //_result.Cast = movie.CastMemberList.Take(Math.Max(movie.CastMemberList.Count, 5)).Select(x => x.Person.Name).ToList();
                _result.Cast = movie.CastingShort.Actors.Split(',').ToTrimmedList();

                _result.Director = movie.CastingShort.Directors.Split(',').ToTrimmedList();

                int _minutes = 0;
                Int32.TryParse(movie.Runtime, out _minutes);
                _result.Runtime = Math.Abs(_minutes / 60).ToString();

                if (movie.Statistics != null)
                {
                    string _r = movie.Statistics.UserRating;
                    if (!string.IsNullOrEmpty(_r))
                    {
                        _result.Rating = _r;
                        _result.Rating = (_result.dRating*2).ToString();
                    }
                }
                _result.Overview = movie.SynopsisShort;
                if (string.IsNullOrEmpty(_result.Overview))
                {
                    _result.Overview = movie.Synopsis;
                }
                else
                {
                    _result.Comments = movie.Synopsis;
                }

                // backdrops
                movie.MediaList
                    .Where(x => x.Type != null && x.Type.Code == "31006" && x.Thumbnail != null)
                    .Select(x => x.Thumbnail.Href).ToList()
                    .ForEach( x=>
                        {
                            var _bi = new BackdropItem(input, null, this.CollectorName, x, x);
                            BackdropsList.Add(_bi);    
                        });
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Allocine: ", ex);
            }
            return _result;
        }

        private XDocument GetXDocument(string page)
        {
            XDocument doc = null;
            using (var oStream = new MemoryStream(new System.Text.UTF8Encoding().GetBytes(page)))
            {
                doc = XDocument.Load(XmlReader.Create(oStream));
            }
            return doc;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool result = false;
            try
            {
                var api = new AlloCineApi();
                var feed = api.Search(keywords);
                if (feed.Error == null)
                {
                    foreach (var movie in feed.MovieList)
                    {
                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }
                        try
                        {
                            string id = movie.Code;
                            string originalTitle = movie.OriginalTitle;
                            string title = movie.Title;
                            title = string.IsNullOrEmpty(title) ? originalTitle : title;
                            MovieInfo movieInfo = GetMovieInfo(id);
                            string posterPath = movie.Poster.Href;
                            string imageUrl = string.IsNullOrEmpty(posterPath) ? null : posterPath;

                            var movieItem = new ResultMovieItem(id, title, imageUrl, CollectorName);
                            movieItem.CollectorMovieUrl = id != null ? GetMovieUrl(id) : null;
                            movieItem.MovieInfo = movieInfo;
                            ResultsList.Add(movieItem);
                            result = true;
                        }
                        catch (Exception ex)
                        {
                            Loggy.Logger.DebugException("Allocine iteration: ", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Allocine results: ", ex);
            }

            return result;
        }
    }
}
