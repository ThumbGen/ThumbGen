using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "copyrightHolder")]
    public class CopyrightHolder
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

    }
}