﻿using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "distributor")]
    public class Distributor
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}

