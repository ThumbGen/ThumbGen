using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "statistics")]
    public class Statistics
    {
        [DataMember(Name = "pressRating")]
        public string PressRating { get; set; }

        [DataMember(Name = "pressReviewCount")]
        public string PressReviewCount { get; set; }

        [DataMember(Name = "userRating")]
        public string UserRating { get; set; }

        [DataMember(Name = "userReviewCount")]
        public string UserReviewCount { get; set; }

        [DataMember(Name = "userRatingCount")]
        public string UserRatingCount { get; set; }

        [DataMember(Name = "commentCount")]
        public string CommentCount { get; set; }

        [DataMember(Name = "photoCount")]
        public string PhotoCount { get; set; }

        [DataMember(Name = "videoCount")]
        public string VideoCount { get; set; }

        [DataMember(Name = "rating")]
        public List<Rating> RatingList { get; set; }

        [DataMember(Name = "ratingStats")]
        public List<RatingStats> RatingStats { get; set; }

        [DataMember(Name = "fanCount")]
        public string FanCount { get; set; }

        [DataMember(Name = "releaseWeekPosition")]
        public string ReleaseWeekPosition { get; set; }

        [DataMember(Name = "theaterCount")]
        public string TheaterCount { get; set; }

        [DataMember(Name = "theaterCountOnRelease")]
        public string TheaterCountOnRelease { get; set; }

    }
}