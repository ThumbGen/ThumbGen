using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Xml;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;

namespace ThumbGen
{
    public enum nfoFileType
    {
        Unknown,
        ThumbGen,
        xbmc,
        ember,
        mymovies,
        tvixie,
        Metadata,
        PrefCollector,
        WDTVHub
    }

    internal abstract class NfoHelperBase
    {
        public abstract nfoFileType FileType { get; }
        protected abstract string XslFileName { get; }
        public abstract Stream ParseFile(string filePath);
    }

    internal class XBMCNfoHelper : NfoHelperBase
    {
        protected override string XslFileName
        {
            get { return "XBMCImport.xslt"; }
        }

        public override nfoFileType FileType
        {
            get { return nfoFileType.xbmc; }
        }

        public override Stream ParseFile(string filePath)
        {
            Stream _result = null;
            XmlDocument _doc = new XmlDocument();
            try
            {
                bool _isXBMC = false;
                _doc.Load(filePath);
                // loaded; check if it is a XBMC format
                if (_doc.DocumentElement != null)
                {
                    // if it has the ThumbGen attribute is not a XBMC file
                    if (_doc.DocumentElement.HasAttribute("ThumbGen"))
                    {
                        return null;
                    }
                    if (!_isXBMC)
                    {
                        // if it has fileinfo element is is XBMC
                        _isXBMC = _doc.DocumentElement.SelectSingleNode("//fileinfo") != null;
                    }
                    if (!_isXBMC)
                    {
                        _isXBMC = _doc.DocumentElement.SelectSingleNode("//sorttitle") != null;
                    }
                    if (!_isXBMC)
                    {
                        _isXBMC = _doc.DocumentElement.SelectSingleNode("//top250") != null;
                    }
                }
                _doc = null;
                if (_isXBMC)
                {
                    // transform it
                    _result = Helpers.XslTransformEmbededStream(XslFileName, new FileStream(filePath, FileMode.Open, FileAccess.Read), null);
                }
            }
            catch
            { }

            return _result;
        }
    }

    internal class EmberNfoHelper : NfoHelperBase
    {
        protected override string XslFileName
        {
            get { return "import_ember.xslt"; }
        }

        public override nfoFileType FileType
        {
            get { return nfoFileType.ember; }
        }

        public override Stream ParseFile(string filePath)
        {
            Stream _result = null;
            XmlDocument _doc = new XmlDocument();
            try
            {
                bool _isEmber = false;
                _doc.Load(filePath);
                // loaded; check if it is a ember format
                if (_doc.DocumentElement != null)
                {
                    // if it has the ThumbGen attribute is not a ember file
                    if (_doc.DocumentElement.HasAttribute("ThumbGen"))
                    {
                        return null;
                    }
                    if (_doc.DocumentElement.SelectSingleNode("//episodedetails") != null)
                    {
                        return null;
                    }
                    if (!_isEmber)
                    {
                        _isEmber = _doc.DocumentElement.SelectSingleNode("//actor/thumb") != null;
                    }
                    if (!_isEmber)
                    {
                        // if it has thumbs element is is ember
                        _isEmber = _doc.DocumentElement.SelectSingleNode("//thumb") != null;
                    }
                }
                _doc = null;
                if (_isEmber)
                {
                    // transform it
                    _result = Helpers.XslTransformEmbededStream(XslFileName, new FileStream(filePath, FileMode.Open, FileAccess.Read), null);
                }
            }
            catch
            { }

            return _result;
        }
    }

    internal class MyMoviesNfoHelper : NfoHelperBase
    {
        public override nfoFileType FileType
        {
            get { return nfoFileType.mymovies; }
        }

        protected override string XslFileName
        {
            get { return "MyMoviesImport.xslt"; }
        }

        public override Stream ParseFile(string filePath)
        {
            Stream _result = null;

            string _tmp = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
            if (!string.IsNullOrEmpty(_tmp) && _tmp.Contains("http://www.mymovies.dk"))
            {
                XmlDocument _doc = new XmlDocument();
                try
                {
                    _doc.Load(filePath);

                    // transform it
                    _result = Helpers.XslTransformEmbededStream(XslFileName, new FileStream(filePath, FileMode.Open, FileAccess.Read), null);
                }
                catch
                { }
            }
            return _result;
        }
    }

    internal class TvixieNfoHelper : NfoHelperBase
    {
        public override nfoFileType FileType
        {
            get { return nfoFileType.tvixie; }
        }

        protected override string XslFileName
        {
            get { return "import_tvixie.xslt"; }
        }

        public override Stream ParseFile(string filePath)
        {
            Stream _result = null;

            string _tmp = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
            if (!string.IsNullOrEmpty(_tmp) && _tmp.Contains("<MovieInfo") && _tmp.Contains("<GUID") && _tmp.Contains("FolderNameClean"))
            {
                XmlDocument _doc = new XmlDocument();
                try
                {
                    _doc.Load(filePath);

                    // transform it
                    _result = Helpers.XslTransformEmbededStream(XslFileName, new FileStream(filePath, FileMode.Open, FileAccess.Read), null);
                }
                catch
                { }
            }
            return _result;
        }
    }

    internal class nfoHelper
    {
        public static string TVIXIE_EXPORT_XSL = "export_tvixie.xslt";
        public static string XBMC_EXPORT_XSL = "export_xbmc.xslt";
        public static string WDTVHUB_EXPORT_XSL = "export_wdtvhub.xslt";
        public static string WDTVHUB_EXPORT_XSL_V2 = "export_wdtvhub_v2.xslt";

        /// <summary>
        /// To convert a Byte Array of Unicode values (UTF-8 encoded) to a complete String.
        /// </summary>
        /// <param name="characters">Unicode Byte Array to be converted to String</param>
        /// <returns>String converted from Unicode Byte Array</returns>
        private static String UTF8ByteArrayToString(Byte[] characters)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            String constructedString = encoding.GetString(characters);
            return (constructedString);
        }

        /// <summary>
        /// Converts the String to UTF8 Byte array and is used in De serialization
        /// </summary>
        /// <param name="pXmlString"></param>
        /// <returns></returns>
        private Byte[] StringToUTF8ByteArray(String pXmlString)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(pXmlString);
            return byteArray;
        }

        public static string GetIMDBId(string movieFilename)
        {
            if (string.IsNullOrEmpty(movieFilename))
            {
                return null;
            }

            string _result = null;
            string _folder = Path.GetDirectoryName(FileManager.Configuration.GetMovieInfoPath(movieFilename, false, MovieinfoType.Import));

            if (Directory.Exists(_folder))
            {
                // first search in the import nfo folder
                _result = fileScanner(new DirectoryInfo(_folder), Path.GetFileName(movieFilename), true);
            }

            if (string.IsNullOrEmpty(_result))
            {
                _folder = Path.GetDirectoryName(FileManager.Configuration.GetMovieInfoPath(movieFilename, false, MovieinfoType.Export));
                if (Directory.Exists(_folder))
                {
                    // search in the export nfo folder 
                    _result = fileScanner(new DirectoryInfo(_folder), Path.GetFileName(movieFilename), true);
                }
            }

            if (string.IsNullOrEmpty(_result))
            {
                // not found, search in the movie's folder
                _folder = Path.GetDirectoryName(movieFilename);
                if (Directory.Exists(_folder))
                {
                    _result = fileScanner(new DirectoryInfo(_folder), Path.GetFileName(movieFilename), true);
                }
            }


            return _result;
        }

        private static string fileScanner(DirectoryInfo dir)
        {
            return fileScanner(dir, "*", true);
        }

        private static string fileScanner(DirectoryInfo dir, string filename, bool searchAll)
        {
            string nfoExt = "nfo,txt";
            Char[] splitters = new Char[] { ',', ';' };
            string[] extensions = nfoExt.Split(splitters);
            string[] mask = new string[extensions.Length];

            // combine the filename/mask
            // with the extension list to create
            // a list of files to look for
            for (int i = 0; i < extensions.Length; i++)
            {
                string ext = extensions[i].Trim();
                if (ext.Length > 1)
                    mask[i] = (searchAll ? "*" : filename) + "." + ext;
            }

            FilesCollector _fc = new FilesCollector();
            // iterate through each pattern and get the corresponding files
            foreach (string pattern in mask)
            {
                // if pattern is null or empty continue to next pattern
                if (string.IsNullOrEmpty(pattern))
                    continue;

                // Get all the files specfied by the current pattern from the directory
                FileInfo[] nfoList = dir.GetFiles(pattern.Trim());
                // get all movies in the current folder
                List<FileInfo> _movies = _fc.CollectFiles(dir.FullName, false) as List<FileInfo>;
                // If none continue to the next pattern
                if (nfoList.Length == 0)
                    continue;

                // iterate through the list of files and scan them
                foreach (FileInfo file in nfoList)
                {
                    // if there are more movies inside the folder then name of nfo must match the name of the movie; if only 1 movie then parse any .nfo/.txt inside the folder
                    if (_movies != null)
                    {
                        if (_movies.Count == 1)
                        {
                            // just one movie
                            if (nfoList.Length >= 1 && filename != "*")
                            {
                                // scan file and retrieve result;  if a match is found return the imdb id
                                return parseFile(file.FullName);
                            }
                        }
                        else
                        {
                            // more movies
                            string _importNfo = FileManager.Configuration.GetMovieInfoPath(filename, false, MovieinfoType.Import);
                            //if ((nfoList.Length >= 1 && filename != "*" && string.Compare(Path.GetFileNameWithoutExtension(file.Name), Path.GetFileNameWithoutExtension(filename)) == 0))
                            if ((nfoList.Length >= 1 && filename != "*" && string.Compare(Path.GetFileNameWithoutExtension(file.Name), Path.GetFileNameWithoutExtension(_importNfo)) == 0))
                            {
                                // scan file and retrieve result;  if a match is found return the imdb id
                                return parseFile(file.FullName);
                            }
                        }
                    }
                }
            }
            // we found nothing so return empty
            return null;
        }

        public static string ExtractIMDBId(string input)
        {
            string _result = null;

            Match match = Regex.Match(input, @"tt\d{7}", RegexOptions.IgnoreCase);

            // If success return the id, on failure return empty. 
            if (match.Success)
            {
                _result = match.Value;
            }
            else
            {
                match = Regex.Match(input, @"title\?\d{7}", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    _result = "tt" + match.Value.Substring(6, 7);
                }
                else
                {
                    _result = null;
                }
            }
            return _result;
        }

        private static string parseFile(string filePath)
        {
            // Read the nfo file content into a string
            string s = File.ReadAllText(filePath);
            // Check for the existance of a imdb id 
            s = ExtractIMDBId(s);
            // return the string
            return s;
        }

        private static string GetNfoFilename(string movieFilename, bool getNearMovie)
        {
            if (getNearMovie)
            {
                return Path.ChangeExtension(movieFilename, FileManager.Configuration.Options.NamingOptions.MovieinfoExtension);
            }
            else
            {
                string _importNfo = FileManager.Configuration.GetMovieInfoPath(movieFilename, false, MovieinfoType.Import);
                if (string.IsNullOrEmpty(_importNfo) || !File.Exists(_importNfo))
                {
                    _importNfo = FileManager.Configuration.GetMovieInfoPath(movieFilename, false, MovieinfoType.Export);
                }
                return _importNfo;
            }
        }

        private static string PatchAmpersandAnd(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Replace("&amp;", "&");
        }

        private static void SaveNfo(string movieFilename, MovieInfo movie, MediaInfoData mediainfo, string targetFile)
        {
            if (movie != null)
            {
                if (mediainfo != null)
                {
                    movie.MediaInfo = mediainfo;
                }

                movie.Filename = Path.GetFileName(movieFilename);

                // autotranslating items to English
                if (FileManager.Configuration.Options.MovieSheetsOptions.AutotranslateGenre && movie.Genre.Count != 0)
                {
                    Translator.TranslatorManager _tm = new Translator.TranslatorManager();
                    List<string> _glist = new List<string>();

                    TextInfo _ti = new CultureInfo("en").TextInfo;

                    foreach (string _g in movie.Genre)
                    {
                        string _ss = _tm.Translate(_g);
                        if (!string.IsNullOrEmpty(_ss))
                        {
                            _ss = _ti.ToTitleCase(_ss);
                            _glist.Add(_ss);
                        }
                    }
                    movie.Genre = _glist;
                }

                string _folder = Path.GetDirectoryName(targetFile);
                Directory.CreateDirectory(_folder);
                try
                {
                    if (FileManager.Configuration.Options.ExportNfoAsTvixie) // Tvixie format
                    {
                        XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
                        using (MemoryStream _ms = new MemoryStream())
                        {
                            XmlTextWriter _tw = new XmlTextWriter(_ms, Encoding.UTF8);
                            _tw.Formatting = Formatting.Indented;
                            _xs.Serialize(_tw, movie);
                            _ms.Position = 0;
                            MemoryStream _res = Helpers.XslTransformEmbededStream(TVIXIE_EXPORT_XSL, _ms, null);
                            using (FileStream _fs = new FileStream(targetFile, FileMode.Create, FileAccess.ReadWrite))
                            {
                                _res.CopyTo(_fs);
                            }
                            if (_res != null)
                            {
                                _res.Dispose();
                            }
                        }
                    }
                    else if (FileManager.Configuration.Options.ExportNfoAsWDTVHUB) // WDTV Live Hub format
                    {
                        XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
                        using (MemoryStream _ms = new MemoryStream())
                        {
                            XmlTextWriter _tw = new XmlTextWriter(_ms, Encoding.UTF8);
                            _tw.Formatting = Formatting.Indented;
                            _xs.Serialize(_tw, movie);
                            _ms.Position = 0;
                            Dictionary<string, string> _params = new Dictionary<string, string>();
                            _params.Add("IsEpisode", EpisodeData.IsEpisodeFile(movieFilename) ? "1" : "0");
                            _params.Add("ExportBackdropsType", ((int)FileManager.Configuration.Options.NamingOptions.ExportBackdropType).ToString());
                            MemoryStream _res = Helpers.XslTransformEmbededStream(WDTVHUB_EXPORT_XSL, _ms, _params);
                            // format the xml
                            _res.Position = 0;
                            XmlReader _xr = XmlReader.Create(_res);
                            XDocument _doc = XDocument.Load(_xr);
                            try
                            {
                                _res.Dispose();
                                _res = new MemoryStream();
                                XmlWriterSettings _set = new XmlWriterSettings();
                                _set.Indent = true;
                                XmlWriter _xw = XmlWriter.Create(_res, _set);
                                _doc.Save(_xw);
                                _xw.Close();
                            }
                            catch { }

                            // stupid patch for &
                            _res.Position = 0;
                            var str = PatchAmpersandAnd(Encoding.UTF8.GetString(_res.ToArray()));
                            using (var sw = new StreamWriter(targetFile))
                            {
                                sw.Write(str);
                            }
                            // Old way, before patching the &
                            //using (FileStream _fs = new FileStream(targetFile, FileMode.Create, FileAccess.ReadWrite))
                            //{
                            //    _res.Position = 0;
                            //    _res.CopyTo(_fs);
                            //}
                            _res.Dispose();
                        }
                    }
                    else if (FileManager.Configuration.Options.ExportNfoAsWDTVHUB_V2) // WDTV Live Hub format (new firmware)
                    {
                        XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
                        using (MemoryStream _ms = new MemoryStream())
                        {
                            XmlTextWriter _tw = new XmlTextWriter(_ms, Encoding.UTF8);
                            _tw.Formatting = Formatting.Indented;
                            _xs.Serialize(_tw, movie);
                            _ms.Position = 0;
                            Dictionary<string, string> _params = new Dictionary<string, string>();
                            _params.Add("IsEpisode", EpisodeData.IsEpisodeFile(movieFilename) ? "1" : "0");
                            _params.Add("ExportBackdropsType", ((int)FileManager.Configuration.Options.NamingOptions.ExportBackdropType).ToString(CultureInfo.InvariantCulture));
                            MemoryStream _res = Helpers.XslTransformEmbededStream(WDTVHUB_EXPORT_XSL_V2, _ms, _params);
                            // format the xml
                            _res.Position = 0;
                            XmlReader _xr = XmlReader.Create(_res);
                            XDocument _doc = XDocument.Load(_xr);
                            try
                            {
                                _res.Dispose();
                                _res = new MemoryStream();
                                XmlWriterSettings _set = new XmlWriterSettings();
                                _set.Indent = true;
                                XmlWriter _xw = XmlWriter.Create(_res, _set);
                                _doc.Save(_xw);
                                _xw.Close();
                            }
                            catch { }

                            // stupid patch for &
                            _res.Position = 0;
                            var str = PatchAmpersandAnd(Encoding.UTF8.GetString(_res.ToArray()));
                            using (var sw = new StreamWriter(targetFile))
                            {
                                sw.Write(str);
                            }
                            // Old way, before patching the &
                            //using (FileStream _fs = new FileStream(targetFile, FileMode.Create, FileAccess.ReadWrite))
                            //{
                            //    _res.Position = 0;
                            //    _res.CopyTo(_fs);
                            //}
                            _res.Dispose();
                        }
                    }
                    else if (FileManager.Configuration.Options.ExportNfoAsXBMC) // XBMC format
                    {
                        XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
                        using (MemoryStream _ms = new MemoryStream())
                        {
                            XmlTextWriter _tw = new XmlTextWriter(_ms, Encoding.UTF8);
                            _tw.Formatting = Formatting.Indented;
                            _xs.Serialize(_tw, movie);
                            _ms.Position = 0;
                            MemoryStream _res = Helpers.XslTransformEmbededStream(XBMC_EXPORT_XSL, _ms, null);

                            _res.Position = 0;
                            XmlReader _xr = XmlReader.Create(_res);
                            XDocument _doc = XDocument.Load(_xr);
                            try
                            {
                                // patch trailer
                                if (!string.IsNullOrEmpty(movie.Trailer))
                                {
                                    Match m = Regex.Match(movie.Trailer, "watch\\?v=(?<ID>.*)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                                    string id = m.Success ? m.Groups["ID"].Value.Trim() : null;
                                    if (!string.IsNullOrEmpty(id))
                                    {
                                        XElement trailerNode = _doc.Descendants("trailer").FirstOrDefault();
                                        if (trailerNode == null)
                                        {
                                            trailerNode = new XElement("trailer");
                                        }
                                        trailerNode.Value = string.Format("plugin://plugin.video.youtube/?action=play_video&videoid={0}", id);
                                        _doc.Element("movie").Add(trailerNode);
                                    }
                                }

                                // patch mediainfo
                                if (mediainfo != null)
                                {
                                    Match _m = Regex.Match(mediainfo.VideoResolution, "(?<Width>\\d+)x(?<Height>\\d+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                                    string _width = _m.Success ? _m.Groups["Width"].Value : "0";
                                    string _height = _m.Success ? _m.Groups["Height"].Value : "0";

                                    XElement _fileInfo = new XElement("fileinfo");
                                    _doc.Element("movie").Add(_fileInfo);
                                    XElement _streamDetails =
                                                    new XElement("streamdetails",
                                                        new XElement("audio",
                                                            new XElement("channels", mediainfo.AudioChannels),
                                                            new XElement("codec", mediainfo.AudioCodec != null ? mediainfo.AudioCodec.Replace("-", "") : mediainfo.AudioCodec)),
                                                        new XElement("video",
                                                            new XElement("aspect", mediainfo.AspectRatio),
                                                            new XElement("duration", mediainfo.Duration),
                                                            new XElement("codec", mediainfo.VideoCodec),
                                                            new XElement("height", _height),
                                                            new XElement("width", _width))
                                                    );
                                    _fileInfo.Add(_streamDetails);

                                    List<string> _subsList = MediaInfoManager.GetAllDistinctSubtitles(mediainfo.GetSubtitlesList(true, mediainfo.EmbeddedSubtitles), mediainfo.GetSubtitlesList(true, mediainfo.ExternalSubtitlesList));
                                    foreach (string _sub in _subsList)
                                    {
                                        XElement _s = new XElement("subtitle",
                                                        new XElement("language"),
                                                        new XElement("longlanguage", _sub)
                                                    );
                                        _streamDetails.Add(_s);
                                    }
                                }
                                // end patch mediainfo
                                _res.Dispose();
                                _res = new MemoryStream();

                                XmlWriterSettings _set = new XmlWriterSettings();
                                _set.Indent = true;

                                XmlWriter _xw = XmlWriter.Create(_res, _set);

                                _doc.Save(_xw);
                                _xw.Close();
                            }
                            catch { }



                            using (FileStream _fs = new FileStream(targetFile, FileMode.Create, FileAccess.ReadWrite))
                            {
                                _res.Position = 0;
                                _res.CopyTo(_fs);
                            }
                            if (_res != null)
                            {
                                _res.Dispose();
                            }
                        }
                    }
                    else // ThumbGen's format
                    {
                        using (FileStream _fs = new FileStream(targetFile, FileMode.Create, FileAccess.ReadWrite))
                        {
                            XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
                            XmlTextWriter _tw = new XmlTextWriter(_fs, Encoding.UTF8);
                            _tw.Formatting = Formatting.Indented;
                            _xs.Serialize(_tw, movie);
                        }

                        if (FileManager.Configuration.Options.PutFullMediaInfoToExportedNfo)
                        {
                            try
                            {
                                MediaInfoManager.AppendFullMediaInfoToNfoFile(targetFile, movieFilename);
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("Gen nfo", ex);
                }
            }
        }

        private static MovieInfo TryParse(NfoHelperBase helper, string nfoFilename, out bool loaded, out nfoFileType nfofiletype)
        {
            MovieInfo _result = null;

            loaded = false;
            nfofiletype = nfoFileType.Unknown;
            if (helper != null)
            {
                MemoryStream _ms = helper.ParseFile(nfoFilename) as MemoryStream;
                if (_ms != null)
                {
                    try
                    {
                        _ms.Position = 0;
                        XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
                        _result = _xs.Deserialize(_ms) as MovieInfo;
                        loaded = true;
                        nfofiletype = helper.FileType;
                    }
                    catch (Exception ex)
                    {
                        Loggy.Logger.DebugException(string.Format("Cannot deserialize nfo {0} using {1}", nfoFilename, helper.FileType.ToString()), ex);
                    }
                }
            }
            return _result;
        }

        public static MovieInfo LoadNfoFile(string movieFilename, out nfoFileType nfofiletype)
        {
            return LoadNfoFile(movieFilename, null, out nfofiletype);
        }

        public static MovieInfo LoadNfoFile(string movieFilename, string nfoFilePath, out nfoFileType nfofiletype)
        {
            MovieInfo _result = new MovieInfo();
            nfofiletype = nfoFileType.Unknown;

            string _nfoFilename = nfoFilePath;

            // if there's no nfo specified
            if (string.IsNullOrEmpty(_nfoFilename))
            {
                // first try to load from target movieinfo path
                _nfoFilename = GetNfoFilename(movieFilename, false);
                if (string.IsNullOrEmpty(_nfoFilename) || !File.Exists(_nfoFilename))
                {
                    // try also near the movie
                    _nfoFilename = GetNfoFilename(movieFilename, true);
                }
            }

            if (File.Exists(_nfoFilename))
            {
                using (FileStream _fs = new FileStream(_nfoFilename, FileMode.Open, FileAccess.Read))
                {
                    // check first if it is a valid XML
                    XmlDocument _doc = new XmlDocument();
                    try
                    {
                        _doc.Load(_fs);
                    }
                    catch
                    {
                        // no valid xml, safely jump out
                        return _result;
                    }


                    bool _loaded = false;
                    // check if it is an ember one
                    _result = TryParse(new EmberNfoHelper(), _nfoFilename, out _loaded, out nfofiletype);

                    // check if maybe it is a xbmc nfo file
                    if (!_loaded)
                    {
                        _result = TryParse(new XBMCNfoHelper(), _nfoFilename, out _loaded, out nfofiletype);
                    }

                    // check if maybe it is a mymovies.dk nfo file
                    if (!_loaded)
                    {
                        _result = TryParse(new MyMoviesNfoHelper(), _nfoFilename, out _loaded, out nfofiletype);
                    }

                    // check if maybe it is a TViXiE nfo file
                    if (!_loaded)
                    {
                        _result = TryParse(new TvixieNfoHelper(), _nfoFilename, out _loaded, out nfofiletype);
                    }

                    if (!_loaded)
                    {
                        try
                        {
                            _fs.Position = 0;
                            XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
                            _result = _xs.Deserialize(_fs) as MovieInfo;
                            nfofiletype = nfoFileType.ThumbGen;
                        }
                        catch (Exception ex)
                        {
                            Loggy.Logger.DebugException("Cannot deserialize nfo:" + _nfoFilename, ex);
                        }
                    }
                }
            }

            return _result;
        }

        public static void GenerateNfoFile(string movieFilename, MovieInfo movie, MediaInfoData mediainfo)
        {
            GenerateNfoFile(movieFilename, movie, mediainfo, FileManager.Configuration.GetMovieInfoPath(movieFilename, true, MovieinfoType.Export)); // always generate respecting the naming convention for Export!
        }

        public static void GenerateNfoFile(string movieFilename, MovieInfo movie, MediaInfoData mediainfo, string targetPath)
        {
            SaveNfo(movieFilename, movie, mediainfo, targetPath);
        }

        public static bool HasMovieInfoFile(string movieFilename)
        {
            return (File.Exists(GetNfoFilename(movieFilename, true)) ||
                    File.Exists(GetNfoFilename(movieFilename, false)));
        }

    }
}

