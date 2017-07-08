using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract]
    internal class AllocineObjectModel
    {
        [DataMember(Name = "error")]
        public Error Error { get; set; }

        [DataMember(Name = "movie")]
        public Movie Movie { get; set; }

        [DataMember(Name = "feed")]
        public Feed Feed { get; set; }

        [DataMember(Name = "person")]
        public Person Person { get; set; }

        [DataMember(Name = "media")]
        public Media Media { get; set; }

        [DataMember(Name = "tvseries")]
        public TvSeries TvSeries { get; set; }

        [DataMember(Name = "season")]
        public Season Season { get; set; }

        [DataMember(Name = "episode")]
        public Episode Episode { get; set; }

        /*
        [DataMember(Name = "error")]
        public Error Error = new Error();

        [DataMember(Name = "movie")]
        public Movie Movie = new Movie();

        [DataMember(Name = "feed")]
        public Feed Feed = new Feed();

        [DataMember(Name = "person")]
        public Person Person = new Person();

        [DataMember(Name = "media")]
        public Media Media = new Media();

        [DataMember(Name = "tvseries")]
        public TvSeries TvSeries = new TvSeries();

        [DataMember(Name = "season")]
        public Season Season = new Season();

        [DataMember(Name = "episode")]
        public Episode Episode = new Episode();
         * */

        public AllocineObjectModel()
        {
            Error = new Error();
            Movie = new Movie();
            Feed = new Feed();
            Person = new Person();
            Media = new Media();
            TvSeries = new TvSeries();
            Season = new Season();
            Episode = new Episode();
        }
    }
}
