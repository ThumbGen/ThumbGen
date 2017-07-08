using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "tvseries")]
    public class TvSeries : IAlloError
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "seriesType")]
        public SeriesType SeriesType { get; set; }

        [DataMember(Name = "originalTitle")]
        public string OriginalTitle { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "keywords")]
        public string Keywords { get; set; }

        [DataMember(Name = "originalBroadcast")]
        public OriginalBroadcast OriginalBroadcast { get; set; }

        [DataMember(Name = "originalChannel")]
        public OriginalChannel OriginalChannel { get; set; }

        [DataMember(Name = "formatTime")]
        public string FormatTime { get; set; }

        [DataMember(Name = "productionStatus")]
        public ProductionStatus ProductionStatus { get; set; }

        [DataMember(Name = "season")]
        public List<Season> SeasonList { get; set; }

        [DataMember(Name = "seasonCount")]
        public string SeasonCount { get; set; }

        [DataMember(Name = "episodeCount")]
        public string EpisodeCount { get; set; }

        [DataMember(Name = "yearStart")]
        public string YearStart { get; set; }

        [DataMember(Name = "yearEnd")]
        public string YearEnd { get; set; }

        [DataMember(Name = "nationality")]
        public List<Nationality> NationalityList { get; set; }

        [DataMember(Name = "genre")]
        public List<Genre> GenreList { get; set; }

        [DataMember(Name = "synopsis")]
        public string Synopsis { get; set; }

        [DataMember(Name = "synopsisShort")]
        public string SynopsisShort { get; set; }

        [DataMember(Name = "castingShort")]
        public CastingShort CastingShort { get; set; }

        [DataMember(Name = "castMember")]
        public List<CastMember> CastMemberList { get; set; }

        [DataMember(Name = "poster")]
        public Poster Poster { get; set; }

        [DataMember(Name = "trailer")]
        public Trailer Trailer { get; set; }

        [DataMember(Name = "topBanner")]
        public TopBanner TopBanner { get; set; }

        [DataMember(Name = "link")]
        public List<Link> LinkList { get; set; }

        [DataMember(Name = "media")]
        public List<Media> MediaList { get; set; }

        [DataMember(Name = "statistics")]
        public Statistics Statistics { get; set; }

        [DataMember(Name = "news")]
        public List<News> NewsList { get; set; }

        [DataMember(Name = "feature")]
        public List<Feature> FeatureList { get; set; }

        [DataMember(Name = "trivia")]
        public List<Trivia> TriviaList { get; set; }

        [DataMember(Name = "tag")]
        public List<Tag> TagList { get; set; }

        [DataMember(Name = "festivalAward")]
        public List<FestivalAward> FestivalAwardList { get; set; }

        public Error Error { get; set; }
    }
}
