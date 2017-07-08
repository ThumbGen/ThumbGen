using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "originalBroadcast")]
    public class OriginalBroadcast
    {
        [DataMember(Name = "dateStart")]
        public string DateStart { get; set; }
    }
}
