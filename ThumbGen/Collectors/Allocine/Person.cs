using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "person")]
    public class Person: IAlloError
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "name")]
        public NameGivenFamily NameGivenFamily { get; set; }

        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        [DataMember(Name = "nationality")]
        public List<Nationality> NationalityList { get; set; }

        [DataMember(Name = "activity")]
        public List<Activity> ActivityList { get; set; }

        [DataMember(Name = "activityShort")]
        public string ActivityShort { get; set; }

        [DataMember(Name = "biographyShort")]
        public string BiographyShort { get; set; }

        [DataMember(Name = "biography")]
        public string Biography { get; set; }

        [DataMember(Name = "birthDate")]
        public string BirthDate { get; set; }

        [DataMember(Name = "birthPlace")]
        public string BirthPlace { get; set; }

        [DataMember(Name = "picture")]
        public Picture Picture { get; set; }

        [DataMember(Name = "link")]
        public List<Link> LinkList { get; set; }

        [DataMember(Name = "participation")]
        public List<Participation> ParticipationList { get; set; }

        [DataMember(Name = "media")]
        public List<Media> MediaList { get; set; }

        [DataMember(Name = "news")]
        public List<News> NewsList { get; set; }

        public Error Error { get; set; }
    }
}

