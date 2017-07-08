﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract]
    public class Media : IAlloError
    {
        [DataMember(Name = "class")]
        public string Class { get; set; }

        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "rcode")]
        public string Rcode { get; set; }

        [DataMember(Name = "type")]
        public Type Type { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "thumbnail")]
        public Thumbnail Thumbnail { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "rendition")]
        public List<Rendition> RenditionList { get; set; }

        [DataMember(Name = "publication")]
        public Publication Publication { get; set; }

        //[DataMember(Name = "copyrightHolder")]
        //public CopyrightHolder CopyrightHolder { get; set; }

        public Error Error { get; set; }
    }
}
