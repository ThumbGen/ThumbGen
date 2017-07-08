using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "person")]
    public class PersonLight
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "realName")]
        public string RealName { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        [DataMember(Name = "birthDate")]
        public string BirthDate { get; set; }

        [DataMember(Name = "deathDate")]
        public string DeathDate { get; set; }

        [DataMember(Name = "activity")]
        public List<Activity> ActivityList { get; set; }

        [DataMember(Name = "nationality")]
        public List<Nationality> NationalityList { get; set; }

        [DataMember(Name = "picture")]
        public List<Picture> PictureList { get; set; }

        [DataMember(Name = "link")]
        public List<Link> LinkList { get; set; }


    }
}

