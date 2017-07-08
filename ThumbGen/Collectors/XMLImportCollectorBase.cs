using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Xml.XPath;
using System.Windows.Data;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    internal class WorkItem
    {
        public List<string> LocalFiles;
        public string MovieID { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string Year { get; set; }
        public string IMDBId { get; set; }
        public string Cover { get; set; }
        public string Background { get; set; }
        private XElement m_Node;
        public XElement Node
        {
            get
            {
                return m_Node;
            }
            set
            {
                m_Node = value;

                if (m_Node != null)
                {
                    // get movieid
                    if (m_Node.Element("_movieid") != null)
                    {
                        this.MovieID = m_Node.Element("_movieid").Value;
                    }

                    // get local files
                    LocalFiles.Clear();
                    foreach (XElement xe in m_Node.XPathSelectElements("localfiles/name"))
                    {
                        if (!string.IsNullOrEmpty(xe.Value))
                        {
                            LocalFiles.Add(xe.Value);
                        }
                    }
                    // get cover
                    if (m_Node.Element("cover") != null)
                    {
                        string _cover = m_Node.Element("cover").Value;
                        if (!string.IsNullOrEmpty(_cover))
                        {
                            try
                            {
                                Uri _u = new Uri(_cover, UriKind.RelativeOrAbsolute);
                                if (!_u.IsAbsoluteUri && !string.IsNullOrEmpty(m_DatabasePath))
                                {
                                    // assume it is near the .xml file
                                    _cover = Path.Combine(Path.GetDirectoryName(m_DatabasePath), _cover);
                                }
                            }
                            catch (Exception ex)
                            {
                                Loggy.Logger.DebugException("XML Import Picture", ex);
                                _cover = string.Empty;
                            }
                        }
                        this.Cover = _cover;
                    }
                    // get background
                    if (m_Node.Element("background") != null)
                    {
                        this.Background = m_Node.Element("background").Value;
                    }
                    // get originaltitle
                    if (m_Node.Element("originaltitle") != null)
                    {
                        this.OriginalTitle = m_Node.Element("originaltitle").Value;
                    }
                    // get title
                    if (m_Node.Element("title") != null)
                    {
                        this.Title = m_Node.Element("title").Value;
                    }
                    if (string.IsNullOrEmpty(OriginalTitle))
                    {
                        OriginalTitle = this.Title;
                    }
                    // get year
                    if (m_Node.Element("year") != null)
                    {
                        this.Year = m_Node.Element("year").Value;
                    }
                    // get IMDB Id
                    if (m_Node.Element("id") != null)
                    {
                        this.IMDBId = m_Node.Element("id").Value;
                    }
                }
            }
        }
        public double Rank { get; set; }

        private string m_DatabasePath;

        public WorkItem(XElement node, string databasePath)
        {
            m_DatabasePath = databasePath;
            
            LocalFiles = new List<string>();

            Node = node;
        }

        public override int GetHashCode()
        {
            return (OriginalTitle + Year + IMDBId + Title).GetHashCode();
        }
    }


    internal class XMLImportCollectorBase : BaseCollector
    {
        protected virtual string OriginalXMLPath { get; set; }
        protected virtual string XSLPath { get; set; }
        protected virtual bool UseCustomSearch { get; set; }
        protected virtual string CustomSearchRegex { get; set; }

        private XDocument m_XMLDoc = new XDocument();

        public override string CollectorName
        {
            get { return ""; }
        }

        public override Country Country
        {
            get { return Country.International; }
        }

        public override bool SupportsMovieInfo
        {
            get
            {
                return true;
            }
        }

        public override bool SupportsIMDbSearch
        {
            get
            {
                return true;
            }
        }

        public override string Host
        {
            get { return ""; }
        }

        private int RankComparison(WorkItem a, WorkItem b)
        {
            if (a.Rank == b.Rank)
            {
                return 0;
            }
            if (a.Rank < b.Rank)
            {
                return 1;
            }
            if (a.Rank > b.Rank)
            {
                return -1;
            }
            return 0;
        }

        public void Load()
        {
            // load the XML and transform it via XSL
            if (!string.IsNullOrEmpty(XSLPath) && !string.IsNullOrEmpty(OriginalXMLPath) && File.Exists(OriginalXMLPath))
            {
                using (XmlReader _xr = XmlReader.Create(Helpers.XslTransformEmbededStream(XSLPath, new FileStream(OriginalXMLPath, FileMode.Open, FileAccess.Read), null)))
                {
                    m_XMLDoc = XDocument.Load(_xr);
                }
            }
        }

        public override MovieInfo QueryMovieInfo(string imdbId)
        {
            MovieInfo _result = new MovieInfo();

            // collect all movies from the big XML file
            var result = from movie in m_XMLDoc.Descendants("movie")
                         select new WorkItem(movie, this.OriginalXMLPath);

            if (result != null && result.Count() != 0)
            {
                foreach (WorkItem _wi in result)
                {
                    if (_wi.IMDBId == imdbId)
                    {
                        // Found by imdbid
                        _result = GetMovieInfo(_wi.Node);
                        break;
                    }
                }
            }

            return _result;
        }

        private MovieInfo GetMovieInfo(XElement el)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    using (var xmlWriter = XmlWriter.Create(stream))
                    {
                        el.WriteTo(xmlWriter);
                    }
                    return new MovieInfo().Load(stream);
                }
            }
            catch
            {
                return new MovieInfo();
            }
        }

        private void AddToResults(WorkItem workItem)
        {
            if (workItem != null)
            {
                ResultMovieItem _movieItem = new ResultMovieItem(workItem.GetHashCode().ToString(), workItem.Title, workItem.Cover, this.CollectorName);
                _movieItem.MovieInfo = GetMovieInfo(workItem.Node);
                this.ResultsList.Add(_movieItem);
                if (!string.IsNullOrEmpty(workItem.Background))
                {
                    this.BackdropsList.Add(new BackdropItem(workItem.GetHashCode().ToString(), workItem.IMDBId, this.CollectorName, workItem.Background, workItem.Background));
                }
            }
        }

        private double CalculateRank(WorkItem wi, string name, string keywords)
        {
            double _result = 0.0;
            if (!string.IsNullOrEmpty(this.Year))
            {
                _result = LetterPairSimilarity.CompareStrings(wi.Year + " " + name, this.Year + " " + keywords);
            }
            else
            {
                _result = LetterPairSimilarity.CompareStrings(name, keywords);
            }
            return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            // based on filename, imdb, keywords, year try to locate the movie in the big XML
            // collect all movies from the big XML file
            var result = from movie in m_XMLDoc.Descendants("movie")
                         select new WorkItem(movie, this.OriginalXMLPath);

            if (result != null && result.Count() != 0)
            {
                // if some custom search criteria is defined, use it
                if (this.UseCustomSearch && !string.IsNullOrEmpty(this.CurrentMovie.Filename) && !string.IsNullOrEmpty(this.CustomSearchRegex))
                {
                    Match _m = Regex.Match(this.CurrentMovie.Filename, this.CustomSearchRegex, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (_m.Success)
                    {
                        string _mid = _m.Groups["ID"].Value;
                        if (!string.IsNullOrEmpty(_mid))
                        {
                            // search all movies for this number
                            foreach (WorkItem _wi in result)
                            {
                                if (_wi.MovieID == _mid)
                                {
                                    // Found by number
                                    this.AddToResults(_wi);
                                    return true;
                                }
                            }
                        }
                    }
                }

                // check if IMDb Id is present and if yes, search a movie having this id
                if (!string.IsNullOrEmpty(imdbID))
                {
                    foreach (WorkItem _wi in result)
                    {
                        if (_wi.IMDBId == imdbID)
                        {
                            // Found by imdbid
                            this.AddToResults(_wi);
                            return true;
                        }
                    }
                }

                // check if the filename is found in the localfiles list of some movie
                if (!string.IsNullOrEmpty(this.CurrentMovie.Filename))
                {
                    foreach (WorkItem _wi in result)
                    {
                        foreach (string _f in _wi.LocalFiles)
                        {
                            if (string.Compare(Path.GetFileName(_f), Path.GetFileName(this.CurrentMovie.Filename), true) == 0)
                            {
                                // Found by filename
                                this.AddToResults(_wi);
                                return true;
                            }
                        }
                    }
                }

                // search by year + original title
                List<WorkItem> _list = new List<WorkItem>();

                foreach (WorkItem _wi in result)
                {
                    // calculate rank for OriginalTitle and for Title and use the biggest value
                    double _RankOriginalTitle = CalculateRank(_wi, _wi.OriginalTitle, keywords);
                    double _RankTitle = CalculateRank(_wi, _wi.Title, keywords);
                    _wi.Rank = Math.Max(_RankOriginalTitle, _RankTitle);

                    if (_wi.Rank == 1)
                    {
                        // Perfect match by title
                        this.AddToResults(_wi);
                        return true;
                    }

                    // add to the list only if at least 75% matching
                    if (_wi.Rank >= 0.75)
                    {
                        _list.Add(_wi);
                    }
                }

                _list.Sort(new Comparison<WorkItem>(RankComparison));

                foreach (WorkItem _wi in _list)
                {
                    this.AddToResults(_wi);
                }

                _list.Clear();
                _list = null;

            }

            return ResultsList.Count != 0;
        }

    }

    public class XML2BoolConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
