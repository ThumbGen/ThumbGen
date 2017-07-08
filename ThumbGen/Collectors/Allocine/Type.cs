using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "type")]
    public class Type
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}

