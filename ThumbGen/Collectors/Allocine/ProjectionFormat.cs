using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract]
    public class ProjectionFormat
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}
