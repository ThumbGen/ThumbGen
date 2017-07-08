using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract]
    public class SoundFormat
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}
