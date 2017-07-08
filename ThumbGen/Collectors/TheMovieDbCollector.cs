using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using WatTmdb.V3;

namespace ThumbGen
{
    [MovieCollector(THEMOVIEDB)]
    internal class TheMovieDbCollector : BaseCollector
    {
        //private const string ACCESS_KEY = "c27cb71cff5bd76e1a7a009380562c62";
        private const string AccessKey = "d92ba7d657e46f94f26bab7539cc6112";

        private Tmdb api;
        private TmdbConfiguration configuration;

        public TheMovieDbCollector()
        {
        }

        public override Country Country
        {
            get { return Country.International; }
        }

        public override string Host
        {
            get { return "http://themoviedb.org"; }
        }

        static public string Escape(string s)
        {
            return Regex.Replace(s, "[" + Regex.Escape(new String(Path.GetInvalidFileNameChars())) + "]", "-");
        }

        public override string CollectorName
        {
            get { return THEMOVIEDB; }
        }

        public override bool SupportsIMDbSearch
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

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        public override MovieInfo QueryMovieInfo(string imdbId)
        {
            if (!string.IsNullOrEmpty(imdbId))
            {
                if(api == null)
                {
                    api = new Tmdb(AccessKey, FileManager.Configuration.Options.MovieSheetsOptions.TVShowsLanguage);
                }
                var movie = api.GetMovieByIMDB(imdbId, FileManager.Configuration.Options.MovieSheetsOptions.TVShowsLanguage);
                if (movie != null)
                {
                    return GetMovieInfo(movie);
                }
            }
            return null;
        }

        private string GetFormattedDate(string date)
        {
            var dtfi = new DateTimeFormatInfo { DateSeparator = "-", ShortDatePattern = "yyyy-MM-dd" };
            return Helpers.GetFormattedDate(date, dtfi);
        }

        private MovieInfo GetMovieInfo(TmdbMovie source)
        {
            var result = new MovieInfo();
            if (source == null) return result;
            result.Name = source.title;
            result.OriginalTitle = source.original_title;
            if (string.IsNullOrEmpty(result.OriginalTitle))
            {
                result.OriginalTitle = result.Name;
            }
            result.Year = string.IsNullOrEmpty(source.release_date) ? string.Empty : source.release_date.Substring(0, 4);
            result.Homepage = source.homepage;
            result.IMDBID = source.imdb_id;
            result.Rating = source.vote_average.ToString("N0");
            if (!string.IsNullOrEmpty(source.release_date))
            {
                try
                {
                    result.SetReleaseDate(GetFormattedDate(source.release_date));
                }
                catch
                {
                }
            }
            result.Overview = source.overview;
            result.Tagline = source.tagline;
            var trailers = api.GetMovieTrailers(source.id);
            if (trailers.youtube != null && trailers.youtube.Any())
            {
                result.Trailer = string.Format("http://www.youtube.com/watch?v={0}", trailers.youtube.First().source);
            }
            var cast = api.GetMovieCast(source.id);
            if (cast != null && cast.cast != null && cast.cast.Any())
            {
                result.Cast.AddRange(cast.cast.Select(x => x.name));
            }
            if (cast != null && cast.crew != null && cast.crew.Any(x => x.job == "Director"))
            {
                result.Director.Add(cast.crew.Where(x => x.job == "Director").Select(x => x.name).First());
            }

            result.Genre.AddRange(source.genres.Select(x => x.name));
            result.Runtime = source.runtime.ToString(CultureInfo.InvariantCulture);
            result.Studios.AddRange(source.production_companies.Select(x => x.name));
            result.Countries.AddRange(source.production_countries.Select(x => x.name));

            return result;
        }

        private void AddResultItem(string id, MovieInfo movie, string imageUrl)
        {
            var movieItem = new ResultMovieItem(id, movie.Name, imageUrl, CollectorName);
            movieItem.CollectorMovieUrl = string.Format("http://www.themoviedb.org/movie/{0}", id);
            movieItem.MovieInfo = movie;
            ResultsList.Add(movieItem);
        }

        private bool GetResults(IEnumerable<TmdbMovie> searchResults)
        {
            foreach (var item in searchResults)
            {
                if (item == null) continue;
                if (FileManager.CancellationPending)
                {
                    return ResultsList.Count != 0;
                }
                // get movie info
                MovieInfo movieInfo = GetMovieInfo(item);
                if (string.IsNullOrEmpty(movieInfo.IMDBID) && !IsValidYear(movieInfo.Year))
                {
                    continue;
                }
                // having the tmdbid, call getimages
                var imagesData = api.GetMovieImages(item.id, "ALL");
                // posters
                if (imagesData.posters.Any())
                {
                    foreach (var poster in imagesData.posters.OrderByDescending(x => x.vote_average))
                    {
                        var imageUrl = string.Format("{0}{1}{2}", configuration.images.base_url, "original", poster.file_path);
                        AddResultItem(item.id.ToString(CultureInfo.InvariantCulture), movieInfo, imageUrl);
                    }
                }
                else
                {
                    // no poster found, add anyway the movie without image
                    if (!string.IsNullOrEmpty(movieInfo.Name))
                    {
                        AddResultItem(item.id.ToString(CultureInfo.InvariantCulture), movieInfo, null);
                    }
                }
                // backdrops
                if (imagesData.backdrops.Any())
                {
                    foreach (var backdrop in imagesData.backdrops.OrderByDescending(x => x.vote_average))
                    {
                        var thumbUrl = string.Format("{0}{1}{2}", configuration.images.base_url, configuration.images.backdrop_sizes.First(), backdrop.file_path);
                        var originalUrl = string.Format("{0}{1}{2}", configuration.images.base_url, "original", backdrop.file_path);
                        var bi = new BackdropItem(item.id.ToString(CultureInfo.InvariantCulture), item.imdb_id, CollectorName, thumbUrl, originalUrl);
                        bi.SetSize(backdrop.width.ToString(CultureInfo.InvariantCulture), backdrop.height.ToString(CultureInfo.InvariantCulture));
                        BackdropsList.Add(bi);
                    }
                }
            }
            return ResultsList.Count != 0;
        }



        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool result = false;

            api = new Tmdb(AccessKey, FileManager.Configuration.Options.MovieSheetsOptions.TVShowsLanguage);
            configuration = api.GetConfiguration();

            TmdbMovie movie = null;
            if (!string.IsNullOrEmpty(imdbID))
            {
                movie = api.GetMovieByIMDB(imdbID, FileManager.Configuration.Options.MovieSheetsOptions.TVShowsLanguage);
                if (movie != null)
                {
                    result = GetResults(new[] { movie });
                }
            }
            if (movie == null)// no imdb identification,use list
            {
                var searchResults = api.SearchMovie(Escape(keywords), 1);
                if (searchResults.results.Any())
                {
                    var list = searchResults.results.Select(item => api.GetMovieInfo(item.id)).ToList();
                    result = GetResults(list);
                }
            }

            return result;
        }
    }
}
