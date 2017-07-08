using System;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Threading;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Xml;

namespace ThumbGen
{
    public class BackdropBase : INotifyPropertyChanged
    {
        private bool m_IsSelected;
        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }
            set
            {
                m_IsSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        public string MovieId { get; set; }
        public string IMDbId { get; set; }
        public string Episode { get; set; }
        public string Season { get; set; }
        public bool IsScreenshot { get; set; }
        public bool IsBanner { get; set; }

        public string Width { get; set; }
        public string Height { get; set; }

        public string CollectorName { get; private set; }

        public delegate string GetOriginalUrlHandler(string thumbUrl);
        public GetOriginalUrlHandler GetOriginalUrl;

        public string ThumbUrl { get; private set; }
        private string m_OriginalUrl = null;
        public string OriginalUrl
        {
            get
            {
                // check if getting originalurl handler is available
                if (m_OriginalUrl == null && GetOriginalUrl != null)
                {
                    m_OriginalUrl = GetOriginalUrl(ThumbUrl);
                }
                return m_OriginalUrl;
            }
            private set
            {
                m_OriginalUrl = value;
            }
        }

        public void SetSize(string width, string height)
        {
            if (!string.IsNullOrEmpty(width))
            {
                this.Width = width;
            }
            if (!string.IsNullOrEmpty(height))
            {
                this.Height = height;
            }
        }

        public BackdropBase(string movieId, string imdbId, string collectorName, string thumbUrl, string originalUrl)
        {
            MovieId = movieId;
            IMDbId = imdbId;
            CollectorName = collectorName;
            ThumbUrl = thumbUrl;
            OriginalUrl = originalUrl;
            Width = "?";
            Height = "?";
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion
    }

    public class BackdropItem : BackdropBase
    {
        public BackdropItem(string movieId, string imdbId, string collectorName, string thumbUrl, string originalUrl)
            : base(movieId, imdbId, collectorName, thumbUrl, originalUrl)
        {

        }
    }

    public class ResultMovieItemCollection<T> : Collection<T> where T : ResultMovieItem
    {
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
        }
    }

    [Serializable]
    [XmlRootAttribute(ElementName = "movie", IsNullable = false)]
    public class MovieInfo
    {
        private static string MOVIEINFO_VERSION = "1";

        [XmlAttribute(AttributeName = "ThumbGen")]
        public string MovieInfoVersion { get; set; }

        [XmlElement(ElementName = "hasrighttoleftdirection")]
        public bool HasRightToLeftDirection { get; set; }

        private string m_Name;
        [XmlElement(ElementName = "title")]
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = string.IsNullOrEmpty(value) ? null : value.Trim(new char[] { '"', ' ' });
            }
        }

        private string m_OriginalTitle;
        [XmlElement(ElementName = "originaltitle")]
        public string OriginalTitle
        {
            get
            {
                return m_OriginalTitle;
            }
            set
            {
                m_OriginalTitle = string.IsNullOrEmpty(value) ? null : value.Trim(new char[] { '"', ' ' });
            }
        }
        [XmlElement(ElementName = "year")]
        public string Year { get; set; }
        [XmlElement(ElementName = "plot")]
        public string Overview { get; set; }

        [XmlElement(ElementName = "filename")]
        public string Filename { get; set; }

        [XmlElement(ElementName = "tagline")]
        public string Tagline { get; set; }

        [XmlElement(ElementName = "metascore")]
        public string Metascore { get; set; }

        [XmlElement(ElementName = "trailer")]
        public string Trailer { get; set; }

        [XmlElement(ElementName = "comments")]
        public string Comments { get; set; }

        private string m_Rating;
        [XmlElement(ElementName = "rating")]
        public string Rating
        {
            get
            {
                return m_Rating;
            }

            set
            {
                double _r = 0;
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        NumberFormatInfo _provider = new NumberFormatInfo();
                        _provider.NumberDecimalSeparator = ".";
                        _r = Double.Parse(value.Replace(",", "."), _provider);
                    }
                    catch { }
                }
                m_Rating = _r == 0 ? null : string.Format("{0:0.0}", _r).Replace(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, ".");
            }
        }

        public double dRating
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Rating))
                {
                    NumberFormatInfo _provider = new NumberFormatInfo();
                    _provider.NumberDecimalSeparator = ".";
                    return Convert.ToDouble(m_Rating, _provider);
                }
                else
                {
                    return 0d;
                }
            }
        }
        [XmlElement(ElementName = "homepage")]
        public string Homepage { get; set; }

        [XmlElement(ElementName = "seriesstatus")]
        public string SeriesStatus { get; set; }

        [XmlElement(ElementName = "episode")]
        public string Episode { get; set; }
        [XmlElement(ElementName = "episodeplot")]
        public string EpisodePlot { get; set; }
        [XmlElement(ElementName = "episodename")]
        public string EpisodeName { get; set; }
        [XmlElement(ElementName = "season")]
        public string Season { get; set; }
        [XmlElement(ElementName = "tvdbid")]
        public string TVDBID { get; set; }

        private string m_EpisodeReleaseDate;
        [XmlElement(ElementName = "episodereleasedate")]
        public string EpisodeReleaseDate
        {
            get
            {
                return m_EpisodeReleaseDate;
            }
            set
            {
                m_EpisodeReleaseDate = Helpers.GetFormattedDate(value);
            }
        }
        public void SetEpisodeReleaseDate(string value)
        {
            m_EpisodeReleaseDate = value;
        }

        [XmlArray("episodes")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> Episodes { get; set; }
        [XmlArray("episodesnames")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> EpisodesNames { get; set; }

        [XmlArray("writers")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> Writers { get; set; }

        [XmlArray("gueststars")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> GuestStars { get; set; }

        [XmlElement(ElementName = "id")]
        public string IMDBID { get; set; }

        private string m_ReleaseDate;
        [XmlElement(ElementName = "releasedate")]
        public string ReleaseDate
        {
            get
            {
                return m_ReleaseDate;
            }
            set
            {
                m_ReleaseDate = string.Compare(this.m_ReleaseDate, value, true) != 0 ? Helpers.GetFormattedDate(value) : m_ReleaseDate;
            }
        }

        public void SetReleaseDate(string value)
        {
            m_ReleaseDate = value;
        }

        [XmlElement(ElementName = "mpaa")]
        public string MPAA { get; set; }
        [XmlArray("actor")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> Cast { get; set; }
        [XmlArray("genre")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> Genre { get; set; }
        [XmlArray("director")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> Director { get; set; }
        private string m_Runtime;
        [XmlElement(ElementName = "runtime")]
        public string Runtime
        {
            get
            {
                return m_Runtime;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Regex _reg = new Regex("[^0-9]*");
                    m_Runtime = _reg.Replace(value, string.Empty);
                }
                else
                {
                    m_Runtime = string.Empty;
                }
            }
        }
        [XmlElement(ElementName = "certification")]
        public string Certification { get; set; }
        [XmlArray("studio")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> Studios { get; set; }
        [XmlArray("country")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> Countries { get; set; }

        [XmlArray("cover")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> Covers { get; set; }

        [XmlArray("backdrop")]
        [XmlArrayItem("name", typeof(string))]
        public List<string> Backdrops { get; set; }

        [XmlElement(ElementName = "mediainfo", IsNullable = false)]
        public MediaInfoData MediaInfo { get; set; }

        public bool IsEmpty
        {
            get
            {
                return this == null || string.IsNullOrEmpty(this.Name);
            }
        }

        public MovieInfo()
        {
            MovieInfoVersion = MOVIEINFO_VERSION;
            Cast = new List<string>();
            Genre = new List<string>();
            Director = new List<string>();
            Studios = new List<string>();
            Countries = new List<string>();
            Episodes = new List<string>();
            EpisodesNames = new List<string>();
            Writers = new List<string>();
            GuestStars = new List<string>();
            Covers = new List<string>();
            Backdrops = new List<string>();
        }

        public MovieInfo FillUpMissingItems(MovieInfo source, bool alwaysUseRatingFromSource)
        {
            if (source != null)
            {
                // do not fill up if the target MovieInfo has not even a Name for the movie;that means the info is empty and will stay like that
                if (!string.IsNullOrEmpty(this.Name))
                {
                    this.IMDBID = string.IsNullOrEmpty(this.IMDBID) ? source.IMDBID : this.IMDBID;
                    this.Cast = this.Cast == null || this.Cast.Count == 0 ? source.Cast : this.Cast;
                    this.Certification = string.IsNullOrEmpty(this.Certification)
                                             ? source.Certification
                                             : this.Certification;
                    this.Countries = this.Countries == null || this.Countries.Count == 0 ? source.Countries : this.Countries;
                    this.Director = this.Director == null || this.Director.Count == 0 ? source.Director : this.Director;
                    this.Genre = this.Genre == null || this.Genre.Count == 0 ? source.Genre : this.Genre;
                    this.Homepage = string.IsNullOrEmpty(this.Homepage) ? source.Homepage : this.Homepage;
                    this.Name = string.IsNullOrEmpty(this.Name) ? source.Name : this.Name;
                    this.OriginalTitle = string.IsNullOrEmpty(this.OriginalTitle)
                                             ? source.OriginalTitle
                                             : this.OriginalTitle;
                    this.Overview = string.IsNullOrEmpty(this.Overview) ? source.Overview : this.Overview;
                    this.Tagline = string.IsNullOrEmpty(this.Tagline) ? source.Tagline : this.Tagline;
                    this.Metascore = string.IsNullOrEmpty(this.Metascore) ? source.Metascore : this.Metascore;
                    this.Trailer = string.IsNullOrEmpty(this.Trailer) ? source.Trailer : this.Trailer;
                    this.Comments = string.IsNullOrEmpty(this.Comments) ? source.Comments : this.Comments;
                    alwaysUseRatingFromSource = alwaysUseRatingFromSource && !string.IsNullOrEmpty(source.Rating);
                    this.Rating = string.IsNullOrEmpty(this.Rating) || alwaysUseRatingFromSource
                                      ? source.Rating
                                      : this.Rating;
                    this.ReleaseDate = string.IsNullOrEmpty(this.ReleaseDate) ? source.ReleaseDate : this.ReleaseDate;
                    this.MPAA = string.IsNullOrEmpty(this.MPAA) ? source.MPAA : this.MPAA;
                    this.Runtime = string.IsNullOrEmpty(this.Runtime) || this.Runtime == "0" ? source.Runtime : this.Runtime;
                    this.Studios = this.Studios == null || this.Studios.Count == 0 ? source.Studios : this.Studios;
                    this.Year = string.IsNullOrEmpty(this.Year) ? source.Year : this.Year;
                    this.Episode = string.IsNullOrEmpty(this.Episode) ? source.Episode : this.Episode;
                    this.EpisodeName = string.IsNullOrEmpty(this.EpisodeName) ? source.EpisodeName : this.EpisodeName;
                    this.EpisodePlot = string.IsNullOrEmpty(this.EpisodePlot) ? source.EpisodePlot : this.EpisodePlot;
                    this.Season = string.IsNullOrEmpty(this.Season) ? source.Season : this.Season;
                    this.EpisodeReleaseDate = string.IsNullOrEmpty(this.EpisodeReleaseDate)
                                                  ? source.EpisodeReleaseDate
                                                  : this.EpisodeReleaseDate;
                    this.Episodes = this.Episodes == null || this.Episodes.Count == 0 ? source.Episodes : this.Episodes;
                    this.EpisodesNames = this.EpisodesNames == null || this.EpisodesNames.Count == 0
                                             ? source.EpisodesNames
                                             : this.EpisodesNames;
                    this.Writers = this.Writers == null || this.Writers.Count == 0 ? source.Writers : this.Writers;
                    this.GuestStars = this.GuestStars == null || this.GuestStars.Count == 0 ? source.GuestStars : this.GuestStars;
                    this.Covers = this.Covers == null || this.Covers.Count == 0 ? source.Covers : this.Covers;
                    this.Backdrops = this.Backdrops == null || this.Backdrops.Count == 0 ? source.Backdrops : this.Backdrops;
                }
            }
            return this;
        }

        public void Save(string destPath)
        {
            XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
            using (FileStream _fs = new FileStream(destPath, FileMode.Create, FileAccess.ReadWrite))
            {
                try
                {
                    _xs.Serialize(_fs, this);
                }
                catch { }
            }
        }

        public void Save(Stream target, string movieFilename, bool appendMedia)
        {
            if (target != null)
            {
                try
                {
                    XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
                    _xs.Serialize(target, this);

                    if (appendMedia)
                    {
                        try
                        {
                            MediaInfoManager.AppendFullMediaInfoToNfoFile(this, movieFilename, target);
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        public MovieInfo Load(Stream data)
        {
            if (data != null && data.CanRead)
            {
                data.Position = 0;
                XmlSerializer _xs = new XmlSerializer(typeof(MovieInfo));
                try
                {
                    return _xs.Deserialize(data) as MovieInfo;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }

    public class ResultItemBase : INotifyPropertyChanged
    {
        public string ImageId { get; set; }
        public string MovieId { get; set; }

        private bool m_IsSelected;
        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }
            set
            {
                m_IsSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        private bool m_IsExpanded;
        public bool IsExpanded
        {
            get
            {
                return m_IsExpanded;
            }
            set
            {
                m_IsExpanded = value;
                NotifyPropertyChanged("IsExpanded");
            }
        }
        private MovieInfo m_MovieInfo;
        public MovieInfo MovieInfo
        {
            get
            {
                return m_MovieInfo;
            }
            set
            {
                m_MovieInfo = value;
                NotifyPropertyChanged("MovieInfo");
            }
        }
        public string Title { get; set; }
        public string CollectorName { get; private set; }
        public string CollectorMovieUrl { get; set; }

        public ResultItemBase(string movieid, string title, string collectorName)
            : this()
        {
            MovieId = movieid;
            Title = title;
            CollectorName = collectorName;
        }

        public ResultItemBase()
        {
            MovieInfo = new MovieInfo();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion
    }

    public class ResultMovieFolder : ResultItemBase
    {
        public List<ResultMovieItem> Images { get; private set; }

        public ResultMovieFolder(List<ResultMovieItem> images)
        {
            IsExpanded = true;
            Images = images;
        }

        public ResultMovieFolder(string movieid, string title, string collectorName) :
            base(movieid, title, collectorName)
        {
            IsExpanded = true;
            Images = new List<ResultMovieItem>();
        }
    }

    public class ResultMovieItem : ResultItemBase
    {
        public string ImageUrl { get; set; }
        public Size ImageSize { get; set; }
        public string ExtraText { get; set; }
        public string LanguageImageUrl { get; set; }
        public int Season { get; set; }
        public bool IsSeasonCover { get; set; }

        public double Rank { get; set; }

        // async called (if set) before downloading the image
        public EventHandler DataQuerying { get; set; }
        // wait for it (async) after DataQuerying
        public ManualResetEvent DataReadyEvent { get; set; }

        private void Init()
        {
            Season = 0;
        }

        public ResultMovieItem()
        {
            Init();
        }

        public ResultMovieItem(string movieid, string title, string imageUrl, string collectorName) :
            base(movieid, title, collectorName)
        {
            ImageUrl = imageUrl;
            CollectorMovieUrl = null;
            DataReadyEvent = null;
            Init();
        }
    }

    public class ResultMovieItemSnapshot : ResultMovieItem
    {
        public int Index { get; set; }

        public ResultMovieItemSnapshot(int index, string title, string imageUrl, string collectorName) :
            base(null, title, imageUrl, collectorName)
        {
            Index = index;
        }
    }

    public class CollectorNode : INotifyPropertyChanged
    {
        public BaseCollector Collector { get; private set; }

        public string Name { get; set; }
        public ObservableCollection<ResultItemBase> Results { get; private set; }
        public bool IsExpanded { get; set; }
        public string SearchTime { get; set; }

        private bool m_IsSelected;
        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }

            set
            {
                m_IsSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        private bool m_IsEnabled;
        public bool IsEnabled
        {
            get
            {
                return m_IsEnabled;
            }

            set
            {
                m_IsEnabled = value;
                NotifyPropertyChanged("IsEnabled");
            }
        }

        private bool m_IsPreferedInfoCollector;
        public bool IsPreferedInfoCollector
        {
            get
            {
                return m_IsPreferedInfoCollector;
            }

            set
            {
                m_IsPreferedInfoCollector = value;
                NotifyPropertyChanged("IsPreferedInfoCollector");
            }
        }

        private bool m_IsPreferedCoverCollector;
        public bool IsPreferedCoverCollector
        {
            get
            {
                return m_IsPreferedCoverCollector;
            }

            set
            {
                m_IsPreferedCoverCollector = value;
                NotifyPropertyChanged("IsPreferedCoverCollector");
            }
        }

        public void SetCollector(BaseCollector collector)
        {
            Collector = collector;
            Name = collector != null ? collector.CollectorName : Name;
        }

        public CollectorNode(List<ResultItemBase> results, bool isSelected, BaseCollector collector)
        {
            Results = new ObservableCollection<ResultItemBase>();
            if (results != null && results.Count > 0)
            {
                Results.AddRange(results);
            }
            IsExpanded = true;
            IsSelected = isSelected;
            IsEnabled = true;
            SearchTime = string.Empty;
            Collector = collector;
            Name = collector != null ? collector.CollectorName : Name;
        }

        public CollectorNode(string name)
            : this(null, false, null)
        {
            Name = name;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion
    }

    public class ProcessingEventArgs : EventArgs
    {
        public string Keywords { get; set; }

        public ProcessingEventArgs(string keywords)
        {
            Keywords = keywords;
        }
    }

    public class ThumbnailCreatedEventArgs : EventArgs
    {
        public QueryResult Result { get; set; }
        public MovieItem Movie { get; set; }

        public ThumbnailCreatedEventArgs(QueryResult result, MovieItem movie)
        {
            Result = result;
            Movie = movie;
        }
    }

    public class MovieInfoComparer : IEqualityComparer<MovieInfo>
    {

        public bool Equals(MovieInfo x, MovieInfo y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            return x != null && y != null && string.Compare(x.Name, y.Name, true) == 0 && x.Year == y.Year && x.IMDBID == y.IMDBID;
        }

        public int GetHashCode(MovieInfo obj)
        {
            return obj == null ? 0 : obj.GetHashCode();
        }
    }
}
