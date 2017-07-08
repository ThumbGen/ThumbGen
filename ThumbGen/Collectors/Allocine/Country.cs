using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "country")]
    public class Country
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}

