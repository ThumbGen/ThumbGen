using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using ThumbGen;


namespace AlloCine
{
    public class AlloCineApi
    {
        private readonly WebClient client;
        //private const string PartnerKey = "YW5kcm9pZC12M3M";
        //private const string PartnerKey = "aXBhZC12MQ";
        private const string AlloCinePartnerKey = "100043982026";
        private const string AlloCineSecretKey = "29d185d98c984a359e6e6f26a0474269";
        private const string AlloCineBaseAddress = "http://api.allocine.fr/rest/v3/";
        //private const string SearchUrl = "search?partner=" + AlloCinePartnerKey + "&{0}";
        //private const string MovieGetInfoUrl = "movie?partner=" + AlloCinePartnerKey + "&{0}";
        private const string SearchUrl = "search?{0}";
        private const string MovieGetInfoUrl = "movie?{0}";
        //private const string MobileBrowserUserAgent = "Dalvik/1.6.0 (Linux; U; Android 4.2.2; Nexus 4 Build/JDQ39E)";


        public AlloCineApi()
        {
            client = new WebClient { BaseAddress = AlloCineBaseAddress, Encoding = Encoding.UTF8 };
        }

        private readonly Random rand = new Random();

        public Feed Search(string query, TypeFilters[] types, int resultsPerPage, int pageNumber)
        {
            var nvc = new NameValueCollection();
            nvc["partner"] = AlloCinePartnerKey;
            nvc["format"] = ResponseFormat.Json.ToString().ToLower();
            if (!string.IsNullOrEmpty(query))
                nvc["q"] = HttpUtility.UrlEncodeUnicode(query);

            if (types != null)
                nvc["filter"] = string.Join(",", types.ToList().Select(x => x.ToString()).ToArray()).ToLower();

            if (resultsPerPage > 0)
                nvc["count"] = resultsPerPage.ToString(CultureInfo.InvariantCulture);

            if (pageNumber > 0)
                nvc["page"] = pageNumber.ToString(CultureInfo.InvariantCulture);

            var searchQuery = BuildSearchQueryWithSignature(ref nvc);
            var alObjectModel = DownloadData(string.Format(SearchUrl, searchQuery), typeof(AllocineObjectModel)) as AllocineObjectModel;

            if (alObjectModel != null)
            {   //If AlloCine returned an Error, we assigned the Error object to the Feed Error Object for easy check 
                //from the class client side
                if (alObjectModel.Error != null && alObjectModel.Error.Value != null)
                {
                    alObjectModel.Feed = new Feed { Error = alObjectModel.Error };
                }
                return alObjectModel.Feed;
            }
            return null;
        }

        public Feed Search(string query)
        {
            return Search(query, new[] { TypeFilters.Movie }, 10, 1);
        }

        public Feed Search(string query, int resultsPerPage, int pageNumber)
        {
            return Search(query, new[] { TypeFilters.Movie }, resultsPerPage, pageNumber);
        }

        public Movie MovieGetInfo(int movieCode, ResponseProfiles profile, TypeFilters[] types, IEnumerable<string> stripTags)
        {
            var nvc = new NameValueCollection();
            nvc["partner"] = AlloCinePartnerKey;
            nvc["format"] = ResponseFormat.Json.ToString().ToLower();

            nvc["code"] = movieCode.ToString().ToLower();

            nvc["profile"] = profile.ToString().ToLower();

            if (types != null)
                nvc["filter"] = string.Join(",", types.ToList().Select(x => x.ToString()).ToArray()).ToLower();

            if (stripTags != null)
                nvc["striptags"] = string.Join(",", stripTags.ToArray()).ToLower();

            var searchQuery = BuildSearchQueryWithSignature(ref nvc);
            var alObjectModel = DownloadData(string.Format(MovieGetInfoUrl, searchQuery), typeof(AllocineObjectModel)) as AllocineObjectModel;

            if (alObjectModel != null)
            {   //If AlloCine returned an Error, we assigned the Error object to the Movie Error Object for easy check 
                //from the class client side
                if (alObjectModel.Error != null && alObjectModel.Error.Value != null)
                {
                    alObjectModel.Movie = new Movie { Error = alObjectModel.Error };
                }
                return alObjectModel.Movie;
            }
            return null;

        }

        public Movie MovieGetInfo(int movieCode)
        {
            return MovieGetInfo(movieCode, ResponseProfiles.Large, new[] { TypeFilters.Movie }, new[] { "synopsis", "synopsisshort" });
        }

        public Movie MovieGetInfo(int movieCode, ResponseProfiles profile)
        {
            return MovieGetInfo(movieCode, profile, new[] { TypeFilters.Movie }, new[] { "synopsis", "synopsisshort" });
        }

        public Movie MovieGetInfo(int movieCode, ResponseProfiles profile, TypeFilters[] types)
        {
            return MovieGetInfo(movieCode, profile, types, new[] { "synopsis", "synopsisshort" });
        }

        private object DownloadData(string url, System.Type type)
        {
            //Simulate the call as it was made from a Mobile device by setting the User Agent to an android browser
            //The header must be redefined after each request
            client.Headers.Add("user-agent", GetRandomUserAgent());

            try
            {
                var response = client.DownloadString(url);
                return JsonConvert.DeserializeObject(response, typeof(AllocineObjectModel));
            }
            catch (Exception ex)
            {
                Loggy.Logger.Error("Allocine exception:" + ex.Message);
                return new AllocineObjectModel() { Error = new Error() { Code = "Exception", Value = ex.Message } };
            }
        }
        
        private string MakeRequest(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.ContentType = "application/x-www-form-urlencoded";
            req.Accept = "application/json";
            req.UserAgent = GetRandomUserAgent(); // MobileBrowserUserAgent;
            var ip = GetRandomIp();
            //req.Headers.Add("REMOTE_ADDR", ip);
            //req.Headers.Add("HTTP_X_FORWARDED_FOR", ip);
            ServicePointManager.ServerCertificateValidationCallback = delegate
            {
                return true; //always trust the presented cerificate
            };
            WebResponse response = req.GetResponse();
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null) return string.Empty;
                using (var sr = new StreamReader(responseStream))
                {
                    //Need to return this response 
                    var strContent = sr.ReadToEnd();
                    return strContent;
                }
            }
        }

        private string GetRandomIp()
        {
            return string.Format("{0}.{1}.{2}.{3}", rand.Next(0, 256), rand.Next(0, 256), rand.Next(0, 256), rand.Next(0, 256));
        }

        private string UrlEncodeUpperCase(string stringToEncode)
        {
            var reg = new Regex(@"%[a-f0-9]{2}");
            stringToEncode = HttpUtility.UrlEncode(stringToEncode);
            return reg.Replace(stringToEncode, m => m.Value.ToUpperInvariant());
        }

        private string BuildSearchQueryWithSignature(ref NameValueCollection nvc)
        {
            NameValueCollection collection = nvc;
            nvc["sed"] = DateTime.Now.ToString("yyyyMMdd");

            var searchQuery = string.Join("&", collection.AllKeys.Select(k => string.Format("{0}={1}", k, collection[k])).ToArray());

            string toEncrypt = AlloCineSecretKey + searchQuery;
            string sig;
            using (SHA1 sha = new SHA1CryptoServiceProvider())
            {
                //We do not forget to use our custom URLEncode function to have the escaped characters using Upper case as AlloCine is expecting
                sig = UrlEncodeUpperCase(Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(toEncrypt))));
            }
            searchQuery += "&sig=" + sig;

            return searchQuery;
        }

        private string GetRandomUserAgent()
        {
            return "Dalvik/1.6.0 (Linux; U; Android 4.0.3; SGH-T989 Build/IML" + rand.Next(10, 100).ToString(CultureInfo.InvariantCulture) + "K)";


            var v = rand.Next(1, 5) + "." + rand.Next(0, 10);
            var a = rand.Next(0, 10).ToString(CultureInfo.InvariantCulture);
            var b = rand.Next(0, 100).ToString(CultureInfo.InvariantCulture);
            var c = rand.Next(0, 1000).ToString(CultureInfo.InvariantCulture);

            var userAgents = new List<string>
            {
                "Mozilla/5.0 (Linux; U; Android $v; fr-fr; Nexus One Build/FRF91) AppleWebKit/5$b.$c (KHTML, like Gecko) Version/$a.$a Mobile Safari/5$b.$c",
                "Mozilla/5.0 (Linux; U; Android $v; fr-fr; Dell Streak Build/Donut AppleWebKit/5$b.$c+ (KHTML, like Gecko) Version/3.$a.2 Mobile Safari/ 5$b.$c.1",
                "Mozilla/5.0 (Linux; U; Android 4.$v; fr-fr; LG-L160L Build/IML74K) AppleWebkit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30",
                "Mozilla/5.0 (Linux; U; Android 4.$v; fr-fr; HTC Sensation Build/IML74K) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30",
                "Mozilla/5.0 (Linux; U; Android $v; en-gb) AppleWebKit/999+ (KHTML, like Gecko) Safari/9$b.$a",
                "Mozilla/5.0 (Linux; U; Android $v.5; fr-fr; HTC_IncredibleS_S710e Build/GRJ$b) AppleWebKit/5$b.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/5$b.1",
                "Mozilla/5.0 (Linux; U; Android 2.$v; fr-fr; HTC Vision Build/GRI$b) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1",
                "Mozilla/5.0 (Linux; U; Android $v.4; fr-fr; HTC Desire Build/GRJ$b) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1",
                "Mozilla/5.0 (Linux; U; Android 2.$v; fr-fr; T-Mobile myTouch 3G Slide Build/GRI40) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1",
                "Mozilla/5.0 (Linux; U; Android $v.3; fr-fr; HTC_Pyramid Build/GRI40) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1",
                "Mozilla/5.0 (Linux; U; Android 2.$v; fr-fr; HTC_Pyramid Build/GRI40) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari",
                "Mozilla/5.0 (Linux; U; Android 2.$v; fr-fr; HTC Pyramid Build/GRI40) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/5$b.1",
                "Mozilla/5.0 (Linux; U; Android 2.$v; fr-fr; LG-LU3000 Build/GRI40) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/5$b.1",
                "Mozilla/5.0 (Linux; U; Android 2.$v; fr-fr; HTC_DesireS_S510e Build/GRI$a) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/$c.1",
                "Mozilla/5.0 (Linux; U; Android 2.$v; fr-fr; HTC_DesireS_S510e Build/GRI40) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile",
                "Mozilla/5.0 (Linux; U; Android $v.3; fr-fr; HTC Desire Build/GRI$a) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1",
                "Mozilla/5.0 (Linux; U; Android 2.$v; fr-fr; HTC Desire Build/FRF$a) AppleWebKit/533.1 (KHTML, like Gecko) Version/$a.0 Mobile Safari/533.1",
                "Mozilla/5.0 (Linux; U; Android $v; fr-lu; HTC Legend Build/FRF91) AppleWebKit/533.1 (KHTML, like Gecko) Version/$a.$a Mobile Safari/$c.$a",
                "Mozilla/5.0 (Linux; U; Android $v; fr-fr; HTC_DesireHD_A9191 Build/FRF91) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1",
                "Mozilla/5.0 (Linux; U; Android $v.1; fr-fr; HTC_DesireZ_A7$c Build/FRG83D) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/$c.$a",
                "Mozilla/5.0 (Linux; U; Android $v.1; en-gb; HTC_DesireZ_A7272 Build/FRG83D) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/$c.1",
                "Mozilla/5.0 (Linux; U; Android $v; fr-fr; LG-P5$b Build/FRG83) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1"
            };

            var result = userAgents[rand.Next(0, userAgents.Count) - 1];
            result = result.Replace("$v", v).Replace("$a", a).Replace("$b", b).Replace("$c", c);
            return result;
        }

    }
}
