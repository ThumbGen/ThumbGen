using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThumbGen.Amazon.ECS;
using System.ServiceModel;
using Simple;
using System.Net;
using System.IO;
using System.Windows;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Design;
using System.Drawing.Imaging;

namespace ThumbGen
{
    internal abstract class AmazonCollectorBase : BaseCollector
    {
        // Amazon IDs
        private const string accessKeyId = "AKIAJ4ETWEHI44HSTRSQ";
        private const string secretKey = "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";
        private const string associatedTag = "thum07-20";

        public AmazonCollectorBase()
        {

        }

        protected virtual string SearchIndex
        {
            get
            {
                return "DVD";
            }
        }

        protected virtual string TargetUrl
        {
            get
            {
                return "https://webservices.amazon.com/onca/soap?Service=AWSECommerceService";
            }
        }

        public override string CollectorName
        {
            get { return null; }
        }

        private MovieInfo GetMovieInfo(Item item)
        {
            MovieInfo _result = new MovieInfo();
            if (item != null)
            {
                _result.Name = item.ItemAttributes.Title;
                try
                {
                    _result.Cast = item.ItemAttributes.Actor.ToTrimmedList();
                }
                catch { }
                //_result.Countries.Add(item.ItemAttributes.Country);
                try
                {
                    _result.Director = item.ItemAttributes.Director.ToTrimmedList();
                }
                catch { }
                _result.Genre.Add(item.ItemAttributes.Genre);
                _result.Studios.Add(item.ItemAttributes.Studio);
            }
            return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            // create a WCF Amazon ECS client
            BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
            binding.MaxReceivedMessageSize = int.MaxValue;
            AWSECommerceServicePortTypeClient client = new AWSECommerceServicePortTypeClient(
                binding,
                new EndpointAddress(TargetUrl));

            // add authentication to the ECS client
            client.ChannelFactory.Endpoint.Behaviors.Add(new AmazonSigningEndpointBehavior(accessKeyId, secretKey));

            // prepare an ItemSearch request
            ItemSearchRequest request = new ItemSearchRequest();
            //request.Count = "1";
            request.Condition = ThumbGen.Amazon.ECS.Condition.All;
            request.SearchIndex = this.SearchIndex;
            //request.Title = title;
            request.Keywords = keywords;//title.Replace(" ", "%20");
            request.ResponseGroup = new string[] { "Small", "Images" };

            ItemSearch itemSearch = new ItemSearch();
            itemSearch.Request = new ItemSearchRequest[] { request };
            itemSearch.AWSAccessKeyId = accessKeyId;
            itemSearch.AssociateTag = associatedTag;

            // issue the ItemSearch request
            ItemSearchResponse response = null;
            try
            {
                response = client.ItemSearch(itemSearch);
            }
            catch
            {
                return _result;
            }
            if (response == null)
            {
                response = client.ItemSearch(itemSearch);
            }
            if (response != null)
            {
                // prepare the ResultsList
                if (response.Items[0] != null && response.Items[0].Item != null)
                {
                    foreach (Item item in response.Items[0].Item)
                    {
                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }
                        string _imageUrl = item.LargeImage == null ? string.Empty : item.LargeImage.URL;
                        if (!string.IsNullOrEmpty(_imageUrl))
                        {
                            ResultMovieItem _movieItem = new ResultMovieItem(null, item.ItemAttributes.Title, _imageUrl, CollectorName);
                            _movieItem.MovieInfo = GetMovieInfo(item);
                            ResultsList.Add(_movieItem);
                        }
                    }
                    _result = true;
                }
            }

            return _result;
        }

    }

    [MovieCollector(BaseCollector.AMAZONDE)]
    internal class AmazonCollectorDE : AmazonCollectorBase
    {
        public AmazonCollectorDE()
        {
            
        }

        public override Country Country
        {
            get { return Country.Germany; }
        }

        public override string Host
        {
            get { return "http://webservices.amazon.de"; }
        }

        protected override string TargetUrl
        {
            get
            {
                return "https://webservices.amazon.de/onca/soap?Service=AWSECommerceService";
            }
        }

        public override string CollectorName
        {
            get
            {
                return AMAZONDE;
            }
        }
    }

    [MovieCollector(BaseCollector.AMAZONCOUK)]
    internal class AmazonCollectorCoUK : AmazonCollectorBase
    {
        public AmazonCollectorCoUK()
        {

        }

        public override Country Country
        {
            get { return Country.UK; }
        }

        public override string Host
        {
            get { return "http://webservices.amazon.co.uk"; }
        }

        protected override string TargetUrl
        {
            get
            {
                return "https://webservices.amazon.co.uk/onca/soap?Service=AWSECommerceService";
            }
        }

        public override string CollectorName
        {
            get
            {
                return AMAZONCOUK;
            }
        }
    }

    [MovieCollector(BaseCollector.AMAZONCA)]
    internal class AmazonCollectorCA : AmazonCollectorBase
    {
        public AmazonCollectorCA()
        {

        }

        public override Country Country
        {
            get { return Country.Canada; }
        }

        public override string Host
        {
            get { return "http://webservices.amazon.ca"; }
        }

        protected override string TargetUrl
        {
            get
            {
                return "https://webservices.amazon.ca/onca/soap?Service=AWSECommerceService";
            }
        }

        public override string CollectorName
        {
            get
            {
                return AMAZONCA;
            }
        }
    }

    [MovieCollector(BaseCollector.AMAZONFR)]
    internal class AmazonCollectorFR : AmazonCollectorBase
    {
        public AmazonCollectorFR()
        {

        }

        public override Country Country
        {
            get { return Country.France; }
        }

        public override string Host
        {
            get { return "http://webservices.amazon.fr"; }
        }

        protected override string TargetUrl
        {
            get
            {
                return "https://webservices.amazon.fr/onca/soap?Service=AWSECommerceService";
            }
        }

        public override string CollectorName
        {
            get
            {
                return AMAZONFR;
            }
        }
    }

    [MovieCollector(BaseCollector.AMAZON)]
    internal class AmazonCollector: AmazonCollectorBase
    {
        public AmazonCollector()
        {
        }

        public override string Host
        {
            get { return "http://webservices.amazon.com"; }
        }

        public override Country Country
        {
            get { return Country.USA; }
        }

        public override string CollectorName
        {
            get { return BaseCollector.AMAZON; }
        }

        protected override string TargetUrl
        {
            get
            {
                return "https://webservices.amazon.com/onca/soap?Service=AWSECommerceService";
            }
        }
    }

    internal class AmazonCollectorMusic : AmazonCollectorBase
    {
        public AmazonCollectorMusic()
        {

        }

        public override Country Country
        {
            get { return Country.USA; }
        }

        public override string Host
        {
            get { return "http://webservices.amazon.com"; }
        }

        public override string CollectorName
        {
            get { return AMAZON_MUSIC; }
        }

        protected override string SearchIndex
        {
            get
            {
                return "Music";
            }
        }

        protected override string TargetUrl
        {
            get
            {
                return "https://webservices.amazon.com/onca/soap?Service=AWSECommerceService";
            }
        }
    }

}
