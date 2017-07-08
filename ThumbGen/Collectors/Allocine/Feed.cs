using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "feed")]
    public class Feed : IAlloError
    {
        [DataMember(Name = "page")]
        public string Page { get; set; }

        [DataMember(Name = "count")]
        public string Count { get; set; }

        [DataMember(Name = "results")]
        public List<Results> ResultsList { get; set; }

        [DataMember(Name = "totalResults")]
        public string TotalResults { get; set; }

        [DataMember(Name = "movie")]
        public List<Movie> MovieList { get; set; }

        [DataMember(Name = "theater")]
        public List<Theater> TheaterList { get; set; }

        [DataMember(Name = "location")]
        public List<Location> LocationList { get; set; }

        [DataMember(Name = "person")]
        public List<PersonLight> PersonList { get; set; }

        [DataMember(Name = "tvseries")]
        public List<TvSeries> TvSeriesList { get; set; }

        [DataMember(Name = "news")]
        public List<News> NewsList { get; set; }

        [DataMember(Name = "media")]
        public List<Media> MediaList { get; set; }

        [DataMember(Name = "updated")]
        public string Updated { get; set; }

        [DataMember(Name = "review")]
        public List<Review> ReviewList { get; set; }

        public Error Error { get; set; }
    }
}