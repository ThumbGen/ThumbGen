using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract]
    public class BoxOffice
    {
        [DataMember(Name = "type")]
        public Type Type { get; set; }

        [DataMember(Name = "country")]
        public Country Country { get; set; }

        [DataMember(Name = "period")]
        public Period Period { get; set; }

        [DataMember(Name = "week")]
        public string Week { get; set; }

        [DataMember(Name = "admissionCount")]
        public string AdmissionCount { get; set; }

        [DataMember(Name = "admissionCountTotal")]
        public string AdmissionCountTotal { get; set; }

        [DataMember(Name = "copyCount")]
        public string CopyCount { get; set; }

    }
}
