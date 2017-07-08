using System;
using System.Collections;
using System.Net;
using CookComputing.XmlRpc;

// Add a reference to the CookComputing.XmlRpc.dll to the project
// Can be downloaded from: http://www.xml-rpc.net/ 
// Latest version at the moment 2.4.0: http://xmlrpcnet.googlecode.com/files/xml-rpc.net.2.4.0.zip

namespace MovieMeterHelper
{
    /// <summary>
    /// The XmlRpcMethod representation of the available methods
    /// </summary>

    [XmlRpcUrl("http://www.moviemeter.nl/ws")]
    public interface IMMApi : IXmlRpcProxy
    {
        /* The following methods are currently (19-06-2009) available:
        system.listMethods() 
        system.methodHelp(string method_name) 
        system.methodSignature(string method_name)

        api.startSession(string apikey), returns array with sessionkey and unix timestamp for session's expiration date 
        api.closeSession(string sessionkey), returns boolean 
        film.search(string sessionkey, string search), returns array with films 
        film.retrieveScore(string sessionkey, int filmId), returns array with information about the current score (average, total, amount of votes) 
        film.retrieveImdb(string sessionkey, int filmId), returns array with imdb code, url, score and votes for this film 
        film.retrieveByImdb(string sessionkey, string imdb code), returns filmId corresponding to the imdb code supplied 
        film.retrieveDetails(string sessionkey, int filmId), returns array with information about the film 
        director.search(string sessionkey, string search), returns array with directors 
        director.retrieveDetails(string sessionkey, int directorId), returns array with director's information 
        director.retrieveFilms(string sessionkey, int directorId), returns array with director's films 
         */

        [XmlRpcMethod("api.startSession")]
        ApiStartSession StartSession(string apikey);

        [XmlRpcMethod("api.closeSession")]
        bool CloseSession(string sessionkey);

        [XmlRpcMethod("film.search")]
        Film[] Search(string sessionkey, string search);

        [XmlRpcMethod("film.retrieveScore")]
        FilmScore RetrieveScore(string sessionkey, int filmId);

        [XmlRpcMethod("film.retrieveImdb")]
        FilmImdb RetrieveImdb(string sessionkey, int filmId);

        [XmlRpcMethod("film.retrieveByImdb")]
        string RetrieveByImdb(string sessionkey, string imdb_code);

        [XmlRpcMethod("film.retrieveDetails")]
        FilmDetail RetrieveDetails(string sessionkey, int filmId);

        [XmlRpcMethod("director.search")]
        Director[] DirectorSearch(string sessionkey, string search);

        [XmlRpcMethod("director.retrieveDetails")]
        DirectorDetail DirectorRetrieveDetails(string sessionkey, int directorId);

        [XmlRpcMethod("director.retrieveFilms")]
        DirectorFilm[] DirectorRetrieveFilms(string sessionkey, int directorId);



    }


    /// <summary>
    /// Definitions for the method-results
    /// In these classes more functionality can be added (like conversion to int or date)
    /// </summary>

    public class ApiStartSession
    {
        public string session_key;
        public int valid_till;
        public string disclaimer;

    }

    public class ApiError
    {
        public string faultCode;
        public string faultString;
    }

    public class Film
    {
        public string filmId;
        public string url;
        public string title;
        public string alternative_title;
        public string year;
        public string average;
        public string votes_count;
        public string similarity;

    }

    public class FilmScore
    {
        public string votes;
        public string total;
        public string average;
    }

    public class FilmImdb
    {
        public string code;
        public string url;
        public string score;
        public int votes;
    }

    public class FilmDetail
    {
        public string url;
        public string thumbnail;
        public string title;
        public Title[] alternative_titles;
        public string year;
        public string imdb;
        public string plot;
        public string duration;
        public Duration[] durations;
        public Actor[] actors;
        public string actors_text;
        public Director[] directors;
        public string directors_text;
        public Country[] countries;
        public string countries_text;
        public string[] genres;
        public string genres_text;
        public Date[] dates_cinema;
        public Date[] dates_video;
        public string average;
        public string votes_count;
        public int filmId;

        public class Duration
        {
            public string duration;
            public string description;
        }

        public class Actor
        {
            public string name;
            public string voice;
        }
        public class Director
        {
            public string id;
            public string name;
        }
        public class Country
        {
            public string iso_3166_1;
            public string name;
        }
        public class Date
        {
            public string date;
        }
        public class Title
        {
            public string title;
        }
    }

    public class Director
    {
        public string directorId;
        public string url;
        public string name;
        public string similarity;
    }

    public class DirectorDetail
    {
        public string url;
        public string thumbnail;
        public string name;
        public string born;
        public string deceased;
    }

    public class DirectorFilm
    {
        public string filmId;
        public string url;
        public string title;
        public string alternative_title;
        public string year;
    }
}