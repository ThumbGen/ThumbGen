﻿using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract(Name = "parentSeries")]
    public class ParentSeries
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}
