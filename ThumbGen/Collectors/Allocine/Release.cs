using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "release")]
    public class Release
    {
        [DataMember(Name = "releaseDate")]
        public string ReleaseDate { get; set; }

        [DataMember(Name = "reissueDate")]
        public string ReissueDate { get; set; }

        [DataMember(Name = "country")]
        public Country Country { get; set; }

        [DataMember(Name = "releaseState")]
        public ReleaseState ReleaseState { get; set; }

        [DataMember(Name = "distributor")]
        public Distributor Distributor { get; set; }

    }
}
