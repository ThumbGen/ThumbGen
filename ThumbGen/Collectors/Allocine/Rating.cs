using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract]
    public class Rating
    {
        [DataMember(Name = "note")]
        public string Note { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}
