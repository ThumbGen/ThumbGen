using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "period")]
    public class Period
    {
        [DataMember(Name = "dateStart")]
        public string DateStart { get; set; }

        [DataMember(Name = "dateEnd")]
        public string DateEnd { get; set; }
    }
}

