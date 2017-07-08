using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "season")]
    public class Season : IAlloError
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "parentSeries")]
        public ParentSeries ParentSeries { get; set; }

        [DataMember(Name = "seasonNumber")]
        public string SeasonNumber { get; set; }

        [DataMember(Name = "episodeCount")]
        public string EpisodeCount { get; set; }

        [DataMember(Name = "yearStart")]
        public string YearStart { get; set; }

        [DataMember(Name = "yearEnd")]
        public string YearEnd { get; set; }

        [DataMember(Name = "castMember")]
        public List<CastMember> CastMemberList { get; set; }

        [DataMember(Name = "episode")]
        public List<Episode> EpisodeList { get; set; }

        [DataMember(Name = "link")]
        public List<Link> LinkList { get; set; }

        [DataMember(Name = "media")]
        public List<Media> MediaList { get; set; }

        [DataMember(Name = "statistics")]
        public Statistics Statistics { get; set; }

        public Error Error { get; set; }
    }
}
