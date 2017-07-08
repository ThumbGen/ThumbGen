using System.Runtime.Serialization;

namespace AlloCine
{
    [DataContract]
    public class Review
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "creationDate")]
        public string CreationDate { get; set; }

        [DataMember(Name = "type")]
        public Type Type { get; set; }

        [DataMember(Name = "subject")]
        public Subject Subject { get; set; }

        [DataMember(Name = "newsSource")]
        public NewsSource NewsSource { get; set; }

        [DataMember(Name = "author")]
        public string Author { get; set; }

        [DataMember(Name = "reviewUrl")]
        public ReviewUrl ReviewUrl { get; set; }

        [DataMember(Name = "body")]
        public string Body { get; set; }

        [DataMember(Name = "rating")]
        public string Rating { get; set; }

    }
}
