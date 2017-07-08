using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "nationality")]
    public class Nationality
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}

