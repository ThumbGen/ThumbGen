using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "color")]
    public class Color
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}

