using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "activity")]
    public class Activity
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}

