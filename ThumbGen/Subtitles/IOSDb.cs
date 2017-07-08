using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

namespace ThumbGen.Subtitles
{
    public class subdata
    {
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public subtitle[] data;

        public double seconds { get; set; }
        public string status { get; set; }
    }

    public class subInfo
    {
        public string imdbid { get; set; }
        public string moviebytesize { get; set; }
        public string moviehash { get; set; }
        public string sublanguageid { get; set; }
        public string query { get; set; }
    }

    public class subRes
    {
        public string IDMovie { get; set; }
        public string IDMovieImdb { get; set; }
        public string IDSubMovieFile { get; set; }
        public string IDSubtitle { get; set; }
        public string IDSubtitleFile { get; set; }
        public string ISO639 { get; set; }
        public string LanguageName { get; set; }
        public string MovieByteSize { get; set; }
        public string MovieHash { get; set; }
        public string MovieImdbRating { get; set; }
        public string MovieName { get; set; }
        public string MovieNameEng { get; set; }
        public string MovieReleaseName { get; set; }
        public string MovieTimeMS { get; set; }
        public string MovieYear { get; set; }
        public string SubActualCD { get; set; }
        public string SubAddDate { get; set; }
        public string SubAuthorComment { get; set; }
        public string SubBad { get; set; }
        public string SubDownloadLink { get; set; }
        public string SubDownloadsCnt { get; set; }
        public string SubFileName { get; set; }
        public string SubFormat { get; set; }
        public string SubHash { get; set; }
        public string SubLanguageID { get; set; }
        public string SubRating { get; set; }
        public string SubSize { get; set; }
        public string SubSumCD { get; set; }
        public string UserID { get; set; }
        public string UserNickName { get; set; }
        public string ZipDownloadLink { get; set; }

    }

    public class subrt
    {
        // Properties
        public subRes[] data { get; set; }
        public double seconds { get; set; }
        
    }

    public class subtitle
    {
        // Properties
        public string data { get; set; }
        public string idsubtitlefile { get; set; }
    }

    public class movieinfo
    {
        public string MovieHash { get; set; }
        public string MovieImdbID { get; set; }
        public string MovieName { get; set; }
        public string MovieYear { get; set; }
    }

    public class moviedata
    {
        public string status { get; set; }
        public movieinfo[] data { get; set; }
        public double seconds { get; set; }
    }

    public class imdbdata
    {
        // Properties
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string[] aka { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public XmlRpcStruct cast { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string[] certification { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string[] country { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string cover { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public XmlRpcStruct directors { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string duration { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string[] genres { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string goofs { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string id { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string[] language { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string plot { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string rating { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string tagline { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string title { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string trivia { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string votes { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public XmlRpcStruct writers { get; set; }
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string year { get; set; }

    }

    public class imdbheader
    {
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public imdbdata data;

        // Properties
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public double seconds { get; set; }

    }

    public interface IOSDb : IXmlRpcProxy
    {
        // Methods
        [XmlRpcMethod("DownloadSubtitles")]
        subdata DownloadSubtitles(string token, string[] subs);
        [XmlRpcMethod("GetIMDBMovieDetails")]
        imdbheader GetIMDBMovieDetails(string token, string imdbid);
        [XmlRpcMethod("LogIn")]
        XmlRpcStruct LogIn(string username, string password, string language, string useragent);
        [XmlRpcMethod("LogOut")]
        XmlRpcStruct LogOut(string token);
        [XmlRpcMethod("SearchSubtitles")]
        subrt SearchSubtitles(string token, subInfo[] subs);
        [XmlRpcMethod("ServerInfo")]
        XmlRpcStruct ServerInfo();
        [XmlRpcMethod("CheckMovieHash")]
        moviedata CheckMovieHash(string token, string[] hashes);
    }

 

 

}
