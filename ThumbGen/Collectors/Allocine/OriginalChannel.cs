using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "originalChannel")]
    public class OriginalChannel
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}
