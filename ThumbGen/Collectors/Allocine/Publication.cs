using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "publication")]
    public class Publication
    {
        [DataMember(Name = "dateStart")]
        public string DateStart { get; set; }

    }
}