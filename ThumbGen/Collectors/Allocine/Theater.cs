using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract]
    public class Theater
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "address")]
        public string Address { get; set; }

        [DataMember(Name = "postalCode")]
        public string PostalCode { get; set; }

        [DataMember(Name = "city")]
        public string City { get; set; }

        [DataMember(Name = "link")]
        public List<Link> LinkList { get; set; }







    }
}
