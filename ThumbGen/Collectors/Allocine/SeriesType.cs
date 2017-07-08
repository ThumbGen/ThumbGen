using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "seriesType")]
    public class SeriesType
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "$")]
        public string Value { get; set; }
    }
}
