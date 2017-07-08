using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "parentSeason")]
    public class ParentSeason
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}
