using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Drawing.Imaging;
using System.Collections;
using System.Globalization;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Web;
using System.Windows.Input;
using System.Drawing;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using System.Text;
using System.Xml.Xsl;
using System.Xml.XPath;
using ThumbGen.Renderer;

namespace ThumbGen.MovieSheets
{
    public enum MoviesheetImageType
    {
        Background,
        Fanart1,
        Fanart2,
        Fanart3
    }

    public class MovieSheetsGenerator : FrameworkElement, IDisposable
    {
        public TemplateItem SelectedTemplate
        {
            get { return (TemplateItem)GetValue(SelectedTemplateProperty); }
            set { SetValue(SelectedTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedTemplateProperty =
            DependencyProperty.Register("SelectedTemplate", typeof(TemplateItem), typeof(MovieSheetsGenerator), new UIPropertyMetadata(null));

        public MovieInfo MovieInfo
        {
            get { return (MovieInfo)GetValue(MovieInfoProperty); }
            set { SetValue(MovieInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MovieInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MovieInfoProperty =
            DependencyProperty.Register("MovieInfo", typeof(MovieInfo), typeof(MovieSheetsGenerator),
                new UIPropertyMetadata(null, OnMovieInfoProperty));

        private static void OnMovieInfoProperty(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MovieSheetsGenerator _gen = obj as MovieSheetsGenerator;
            if (_gen != null)
            {
                _gen.Invalidate();
            }
        }

        public MediaInfoData MediaInfo
        {
            get { return (MediaInfoData)GetValue(MediaInfoProperty); }
            set { SetValue(MediaInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MediaInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaInfoProperty =
            DependencyProperty.Register("MediaInfo", typeof(MediaInfoData), typeof(MovieSheetsGenerator),
                new UIPropertyMetadata(null, OnMediaInfoProperty));

        private static void OnMediaInfoProperty(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MovieSheetsGenerator _gen = obj as MovieSheetsGenerator;
            if (_gen != null)
            {
                _gen.Invalidate();
            }
        }

        public string MovieSheetTempPath { get; set; }
        public string MovieSheetPreviewTempPath { get; set; }

        public string CoverTempPath
        {
            get { return (string)GetValue(CoverTempPathProperty); }
            set { SetValue(CoverTempPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CoverTempPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoverTempPathProperty =
            DependencyProperty.Register("CoverTempPath", typeof(string), typeof(MovieSheetsGenerator), new UIPropertyMetadata(null));

        public string BackdropTempPath
        {
            get { return (string)GetValue(BackdropTempPathProperty); }
            set { SetValue(BackdropTempPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackdropTempPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackdropTempPathProperty =
            DependencyProperty.Register("BackdropTempPath", typeof(string), typeof(MovieSheetsGenerator), new UIPropertyMetadata(null));

        public string Fanart1TempPath
        {
            get { return (string)GetValue(Fanart1TempPathProperty); }
            set { SetValue(Fanart1TempPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Fanart1TempPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Fanart1TempPathProperty =
            DependencyProperty.Register("Fanart1TempPath", typeof(string), typeof(MovieSheetsGenerator), new UIPropertyMetadata(null));

        public string Fanart2TempPath
        {
            get { return (string)GetValue(Fanart2TempPathProperty); }
            set { SetValue(Fanart2TempPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Fanart1TempPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Fanart2TempPathProperty =
            DependencyProperty.Register("Fanart2TempPath", typeof(string), typeof(MovieSheetsGenerator), new UIPropertyMetadata(null));

        public string Fanart3TempPath
        {
            get { return (string)GetValue(Fanart3TempPathProperty); }
            set { SetValue(Fanart3TempPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Fanart1TempPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Fanart3TempPathProperty =
            DependencyProperty.Register("Fanart3TempPath", typeof(string), typeof(MovieSheetsGenerator), new UIPropertyMetadata(null));

        public bool NeedsRender
        {
            get { return (bool)GetValue(NeedsRenderProperty); }
            set { SetValue(NeedsRenderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NeedsRender.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NeedsRenderProperty =
            DependencyProperty.Register("NeedsRender", typeof(bool), typeof(MovieSheetsGenerator), new UIPropertyMetadata(false));



        private List<string> m_GarbageFiles = new List<string>();

        public string SoundTempPath { get; set; }
        public string FormatTempPath { get; set; }
        public string ResolutionTempPath { get; set; }
        public string VideoTempPath { get; set; }

        public string CurrentMoviePath { get; private set; }

        public string LastError { get; set; }

        public string RenderedXML { get; set; }

        public SheetType SheetType { get; private set; }

        static MovieSheetsGenerator()
        {

        }

        public MovieSheetsGenerator(SheetType sheetType, string moviePath)
        {
            SheetType = sheetType;
            CurrentMoviePath = moviePath;
            GenerateNewTempFilesNames();
        }

        private static string GetFormattedEnumeration(XmlNode node, List<string> data)
        {
            string _result = string.Empty;
            string _sep = "/";

            if (data != null && data.Count != 0)
            {
                int _cnt = data.Count;

                if (node != null)
                {
                    if (node.Attributes["Separator"] != null)
                    {
                        _sep = node.Attributes["Separator"].Value;
                    }

                    if (node.Attributes["MaximumValues"] != null)
                    {
                        _cnt = Convert.ToInt16(node.Attributes["MaximumValues"].Value);
                    }
                    if (data.Count < _cnt)
                    {
                        _cnt = data.Count;
                    }
                }

                for (int i = 0; i < _cnt; i++)
                {
                    _result = string.Format("{0}{1}{2}", _result, HttpUtility.HtmlEncode(data[i]), _sep);
                }

            }

            return string.IsNullOrEmpty(_result) ? string.Empty : _result.TrimEnd(_sep.ToCharArray());
        }

        private static string GetPATH(TemplateItem template)
        {
            if (template != null)
            {
                return Path.GetDirectoryName(template.TemplatePath).TrimEnd('\\');
            }
            else
            {
                return string.Empty;
            }
        }

        private static void SplitStringList(Hashtable hashtable, IEnumerable<string> input, string prefix)
        {
            string _s = string.Empty;

            for (int _i = 0; _i < 5; _i++)
            {
                if (input != null && input.Count() > _i && !string.IsNullOrEmpty(input.ElementAt(_i)))
                {
                    _s = input.ElementAt(_i);
                }
                else
                {
                    _s = string.Empty;
                }
                hashtable[string.Format("%{0}{1}%", prefix, _i + 1)] = _s;
            }
        }

        private static void RemoveTmpFile(string file)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        private static System.Drawing.Size GetPreviewThumbSize(System.Drawing.Size inputSize)
        {
            float nPercentW = ((float)320 / (float)inputSize.Width);
            float nPercentH = ((float)180 / (float)inputSize.Height);
            float nPercent = 0;

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(inputSize.Width * nPercent);
            int destHeight = (int)(inputSize.Height * nPercent);

            return new System.Drawing.Size(destWidth, destHeight);
        }

        public void ClearGarbage()
        {
            foreach (string _file in m_GarbageFiles)
            {
                RemoveTmpFile(_file);
                FileManager.GarbageFiles.Remove(_file);
            }

            RemoveTmpFile(MovieSheetPreviewTempPath);
            RemoveTmpFile(MovieSheetTempPath);
            RemoveTmpFile(CoverTempPath);
            RemoveTmpFile(BackdropTempPath);
            RemoveTmpFile(Fanart1TempPath);
            RemoveTmpFile(Fanart2TempPath);
            RemoveTmpFile(Fanart3TempPath);
        }

        public void Invalidate()
        {
            NeedsRender = true;
        }

        private void AddToGarbageFiles(string file)
        {
            m_GarbageFiles.Add(file);
            // add it to the global list too just in case the current object is not cleaning it
            FileManager.AddToGarbageFiles(file);
        }

        private void GenerateNewTempFilesNames()
        {
            MovieSheetTempPath = Helpers.GetUniqueFilename(".jpg");
            MovieSheetPreviewTempPath = Helpers.GetUniqueFilename(".jpg");
            CoverTempPath = Helpers.GetUniqueFilename(".jpg");
            BackdropTempPath = Helpers.GetUniqueFilename(".jpg");
            Fanart1TempPath = Helpers.GetUniqueFilename(".jpg");
            Fanart2TempPath = Helpers.GetUniqueFilename(".jpg");
            Fanart3TempPath = Helpers.GetUniqueFilename(".jpg");

            AddToGarbageFiles(MovieSheetTempPath);
            AddToGarbageFiles(MovieSheetPreviewTempPath);
            AddToGarbageFiles(CoverTempPath);
            AddToGarbageFiles(BackdropTempPath);
            AddToGarbageFiles(Fanart1TempPath);
            AddToGarbageFiles(Fanart2TempPath);
            AddToGarbageFiles(Fanart3TempPath);
        }

        private void ProcessMediaItem(XmlDocument doc, string itemTempPath, MediaInfoFlags flag, string firstToken, string secondToken,
                                     string firstFileQuery, TemplateItem template)
        {
            if (doc != null)
            {
                XmlNode _Node = doc.SelectSingleNode(string.Format("//ImageElement[@SourceData='{0}']", firstToken));
                if (_Node != null)
                {
                    try
                    {
                        itemTempPath = Helpers.GetUniqueFilename(".jpg");
                        AddToGarbageFiles(itemTempPath);
                        string _fileFromTemplate = string.Empty;
                        try
                        {
                            if (flag != MediaInfoFlags.Unknown)
                            {
                                _fileFromTemplate = Helpers.SelectSingleNodeCaseInsensitive(doc, firstFileQuery, "Name", MediaModel.MediaInfoText[flag]).Attributes["Image"].Value.Replace("%PATH%", GetPATH(template));
                            }
                        }
                        catch { }
                        if (flag != MediaInfoFlags.Unknown)
                        {
                            if (File.Exists(_fileFromTemplate))
                            {
                                File.Copy(_fileFromTemplate, itemTempPath, true);
                            }
                            else
                            {
                                MediaModel.SaveMediaFlagImageToDisk(flag, itemTempPath);
                            }
                            _Node.Attributes["Source"].Value = "File";
                            _Node.Attributes["SourceData"].Value = itemTempPath;
                        }
                    }
                    catch { }
                }
                _Node = Helpers.SelectSingleNodeCaseInsensitive(doc, "//ImageElement", "Name", secondToken);
                if (_Node != null)
                {
                    try
                    {
                        itemTempPath = Helpers.GetUniqueFilename(".jpg");
                        AddToGarbageFiles(itemTempPath);
                        MediaModel.SaveMediaFlagImageToDisk(flag, itemTempPath);
                        _Node.Attributes["Source"].Value = "File";
                        _Node.Attributes["SourceData"].Value = itemTempPath;
                    }
                    catch { }
                }
            }
        }

        private string ProcessRating(string input, XmlDocument doc, TemplateItem template)
        {
            if (MovieInfo != null && !string.IsNullOrEmpty(MovieInfo.Rating))
            {
                double num = double.Parse(MovieInfo.Rating, CultureInfo.InvariantCulture);
                double num10 = num * 10.0;

                input = input.Replace("%RATINGPERCENT%", string.Format("{0:#.#}", num10).Replace("%RATING%", string.Format("{0:#.#}", num)));
                int index = input.IndexOf("%RATINGSTARS%");
                if (index > 0)
                {
                    try
                    {
                        int num3 = input.IndexOf("/>", index) + 2;
                        int startIndex = -1;
                        for (int i = index; i > 0; i--)
                        {
                            startIndex = input.IndexOf("<ImageElement", i, (int)(index - i));
                            if (startIndex >= 0)
                            {
                                break;
                            }
                        }
                        XmlDocument document = new XmlDocument();
                        string xml = input.Substring(startIndex, num3 - startIndex);
                        document.LoadXml(xml);
                        XmlNode node = document.SelectSingleNode("ImageElement");
                        if (node == null)
                        {
                            return input;
                        }
                        Image image = Image.FromFile(doc.SelectSingleNode("/Template/Settings/Rating").Attributes["FileName"].Value.Replace("%PATH%", GetPATH(template)));
                        int num6 = 0;
                        int num7 = 0;
                        Bitmap bitmap = new Bitmap(int.Parse(node.Attributes["Width"].Value), int.Parse(node.Attributes["Height"].Value), PixelFormat.Format32bppArgb);
                        Graphics graphics = Graphics.FromImage(bitmap);
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        num /= 2.0;
                        for (int j = 0; j < 5; j++)
                        {
                            int num9 = image.Width;
                            if (num < 1.0)
                            {
                                num9 = (int)(image.Width * num);
                            }
                            graphics.DrawImage(image, new Rectangle(num6, num7, num9, image.Height), new Rectangle(0, 0, num9, image.Height), GraphicsUnit.Pixel);
                            if (num <= 1.0)
                            {
                                num6 += num9;
                                break;
                            }
                            num6 += image.Width;
                            num--;
                        }
                        num6 += 10;
                        graphics.Dispose();
                        image.Dispose();
                        MemoryStream stream = new MemoryStream();
                        bitmap.Save(stream, ImageFormat.Png);
                        string newValue = Convert.ToBase64String(stream.ToArray());
                        stream.Close();
                        stream = null;
                        input = input.Replace("%RATINGSTARS%", newValue);
                    }
                    catch
                    {
                    }
                }
                return input;
            }
            return input.Replace("%RATING%", "").Replace("%RATINGPERCENT%", "");
        }

        private void UpdateMediaInfo(XmlDocument doc, TemplateItem template)
        {
            if (doc != null)
            {
                ProcessMediaItem(doc, SoundTempPath, MediaInfo == null ? MediaInfoFlags.Unknown : MediaInfo.Audio.Flag,
                    "%SOUNDFORMAT%", "AudioCodecINFO", "/Template/SoundFormats/SoundFormat", template);
                ProcessMediaItem(doc, FormatTempPath, MediaInfo == null ? MediaInfoFlags.Unknown : MediaInfo.Format.Flag,
                    "%MEDIAFORMAT%", "AudioCodecINFO2", "/Template/MediaFormats/MediaFormat", template);
                ProcessMediaItem(doc, ResolutionTempPath, MediaInfo == null ? MediaInfoFlags.Unknown : MediaInfo.Resolution.Flag,
                    "%RESOLUTION%", "ResolutionINFO", "/Template/Resolutions/Resolution", template);
                ProcessMediaItem(doc, VideoTempPath, MediaInfo == null ? MediaInfoFlags.Unknown : MediaInfo.Video.Flag,
                    "%VIDEOFORMAT%", "VideoINFO", "/Template/VideoFormats/VideoFormat", template);
            }
        }

        private IEnumerable<string> DetectImages(IEnumerable<string> inputNames, XmlDocument doc, string imagesFolderXPath, string defaultImageXPath, string defaultFolder)
        {
            List<string> _result = new List<string>();

            // get the folder path for the images, collect all the files there and try to match each file from the inputNames to some image
            // if no match can be found for one inputName it receives the defaultImageXPath (if specified)
            if (inputNames != null && inputNames.Count() != 0 && doc != null && !string.IsNullOrEmpty(imagesFolderXPath) && !string.IsNullOrEmpty(defaultImageXPath))
            {
                string _templatePath = GetPATH(this.SelectedTemplate);
                // get the paths
                string _folderPath = @"%PATH%\..\Common\" + defaultFolder;
                XmlNode _node = doc.SelectSingleNode(imagesFolderXPath);
                _folderPath = _node != null && !string.IsNullOrEmpty(_node.Value) ? _node.Value : _folderPath;
                _folderPath = _folderPath.Replace("%PATH%", _templatePath);

                string _defaultImagePath = string.Empty;
                _node = doc.SelectSingleNode(defaultImageXPath);
                _defaultImagePath = _node != null && !string.IsNullOrEmpty(_node.Value) ? _node.Value : _defaultImagePath;
                _defaultImagePath = _defaultImagePath.Replace("%PATH%", _templatePath);

                // collect images 
                if (Directory.Exists(_folderPath))
                {
                    string[] _files = Directory.GetFiles(_folderPath, "*.*", SearchOption.AllDirectories);
                    if (_files.Count() != 0)
                    {
                        // match them
                        foreach (string _name in inputNames)
                        {
                            if (!string.IsNullOrEmpty(_name))
                            {
                                string _path = _files.FirstOrDefault((_item) =>
                                                                         {
                                                                             return Path.GetFileNameWithoutExtension(_item).ToLowerInvariant().Contains(_name.Trim().ToLowerInvariant());
                                                                         });
                                _path = string.IsNullOrEmpty(_path) ? _defaultImagePath : _path;
                                if (!string.IsNullOrEmpty(_path))
                                {
                                    _result.Add(_path);
                                }
                            }
                        }
                    }
                }
            }

            return _result;
        }

        private Dictionary<string, string> m_Tokens = new Dictionary<string, string>();

        private string AnalyzeTemplate(TemplateItem template)
        {
            string buf = string.Empty;

            if (template != null)
            {
                XmlDocument _doc = new XmlDocument();
                _doc.Load(template.TemplatePath);

                // if Background contains some reference to the TITLEPATH, replace it with our background
                XmlNodeList _backNodes = _doc.SelectNodes("//ImageElement[starts-with(@Name, 'Background')]");
                if (_backNodes != null && _backNodes.Count != 0)
                {
                    foreach (XmlNode _backNode in _backNodes)
                    {
                        if (_backNode != null)
                        {
                            string _srcData = _backNode.Attributes["SourceData"].Value;
                            if (string.IsNullOrEmpty(_srcData) || (!string.IsNullOrEmpty(_srcData) && _srcData.Contains("%TITLEPATH%")))
                            {
                                _backNode.Attributes["SourceData"].Value = "%BACKGROUND%";
                            }
                        }
                    }
                }
                // if Cover contains some reference to the TITLEPATH, replace it with our cover
                XmlNodeList _coverNodes = _doc.SelectNodes("//ImageElement[starts-with(@Name, 'Cover')]");
                if (_coverNodes != null && _coverNodes.Count != 0)
                {
                    foreach (XmlNode _coverNode in _coverNodes)
                    {
                        if (_coverNode != null)
                        {
                            string _srcData = _coverNode.Attributes["SourceData"].Value;
                            if (string.IsNullOrEmpty(_srcData) || (!string.IsNullOrEmpty(_srcData) && _srcData.Contains("%TITLEPATH%")))
                            {
                                _coverNode.Attributes["SourceData"].Value = "%COVER%";
                            }
                        }
                    }
                }

                XmlNodeList _fanartNodes = _doc.SelectNodes("//ImageElement[starts-with(@Name, 'Fanart')]");
                if (_fanartNodes != null && _fanartNodes.Count != 0)
                {
                    foreach (XmlNode _fanartNode in _fanartNodes)
                    {
                        if (_fanartNode != null)
                        {
                            string _name = string.Empty;
                            try
                            {
                                _name = _fanartNode.Attributes["Name"].Value.ToLowerInvariant();
                            }
                            catch { }
                            switch (_name)
                            {
                                case "fanart1":
                                    _fanartNode.Attributes["SourceData"].Value = "%FANART1%";
                                    break;
                                case "fanart2":
                                    _fanartNode.Attributes["SourceData"].Value = "%FANART2%";
                                    break;
                                case "fanart3":
                                    _fanartNode.Attributes["SourceData"].Value = "%FANART3%";
                                    break;
                                default:
                                    string _srcData = _fanartNode.Attributes["SourceData"].Value;
                                    if (string.IsNullOrEmpty(_srcData) || (!string.IsNullOrEmpty(_srcData) && (_srcData.Contains("%COVER%") || _srcData.Contains("%TITLEPATH%"))))
                                    {
                                        _fanartNode.Attributes["SourceData"].Value = "%BACKGROUND%";
                                    }
                                    break;
                            }
                        }
                    }
                }

                UpdateMediaInfo(_doc, template);

                buf = _doc.SelectSingleNode("/Template/ImageDrawTemplate").OuterXml;
                Hashtable hashtable = new Hashtable();
                hashtable["%PATH%"] = HttpUtility.HtmlEncode(GetPATH(template));
                hashtable["%BACKGROUND%"] = HttpUtility.HtmlEncode(BackdropTempPath);
                hashtable["%FANART1%"] = HttpUtility.HtmlEncode(Fanart1TempPath);
                hashtable["%FANART2%"] = HttpUtility.HtmlEncode(Fanart2TempPath);
                hashtable["%FANART3%"] = HttpUtility.HtmlEncode(Fanart3TempPath);

                hashtable["%TITLEPATH%"] = string.IsNullOrEmpty(CurrentMoviePath) ? string.Empty : HttpUtility.HtmlEncode(Path.GetDirectoryName(CurrentMoviePath).TrimEnd('\\'));
                try
                {
                    hashtable["%MOVIEFILENAME%"] = string.IsNullOrEmpty(CurrentMoviePath) ? string.Empty : HttpUtility.HtmlEncode(Path.GetFileName(CurrentMoviePath));
                    hashtable["%MOVIEFILENAMEWITHOUTEXT%"] = string.IsNullOrEmpty(CurrentMoviePath) ? string.Empty : HttpUtility.HtmlEncode(Path.GetFileNameWithoutExtension(CurrentMoviePath));
                    hashtable["%MOVIEFOLDER%"] = string.IsNullOrEmpty(CurrentMoviePath) ? string.Empty : HttpUtility.HtmlEncode(Helpers.GetMovieFolderName(CurrentMoviePath, ""));
                    hashtable["%MOVIEPARENTFOLDER%"] = string.IsNullOrEmpty(CurrentMoviePath) ? string.Empty : HttpUtility.HtmlEncode(Helpers.GetMovieParentFolderName(CurrentMoviePath, ""));
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("Movie filename analysis", ex);
                }
                hashtable["%COVER%"] = HttpUtility.HtmlEncode(CoverTempPath);
                hashtable["%TITLE%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Name) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Name);
                hashtable["%ORIGINALTITLE%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.OriginalTitle) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.OriginalTitle); ;
                hashtable["%PLOT%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Overview) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Overview);
                hashtable["%TAGLINE%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Tagline) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Tagline);
                hashtable["%METASCORE%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Metascore) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Metascore);
                hashtable["%TRAILER%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Trailer) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Trailer);
                hashtable["%COMMENTS%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Comments) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Comments);
                hashtable["%YEAR%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Year) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Year);
                hashtable["%RUNTIME%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Runtime) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Runtime);
                hashtable["%RELEASEDATE%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.ReleaseDate) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.ReleaseDate);
                hashtable["%MPAA%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.MPAA) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.MPAA);
                hashtable["%ACTORS%"] = MovieInfo == null || MovieInfo.Cast == null || MovieInfo.Cast.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Actors"), MovieInfo.Cast);
                hashtable["%GENRES%"] = MovieInfo == null || MovieInfo.Genre == null || MovieInfo.Genre.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Genres"), MovieInfo.Genre);
                hashtable["%DIRECTORS%"] = MovieInfo == null || MovieInfo.Director == null || MovieInfo.Director.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Directors"), MovieInfo.Director);

                hashtable["%CERTIFICATION%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Certification) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Certification);
                hashtable["%CERTIFICATIONTEXT%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Certification) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.Certification);
                hashtable["%COUNTRIES%"] = MovieInfo == null || MovieInfo.Countries == null || MovieInfo.Countries.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Countries"), MovieInfo.Countries);
                hashtable["%STUDIOS%"] = MovieInfo == null || MovieInfo.Studios == null || MovieInfo.Studios.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Studios"), MovieInfo.Studios);
                hashtable["%CERTIFICATIONCOUNTRYCODE%"] = !string.IsNullOrEmpty(FileManager.Configuration.Options.IMDBOptions.CertificationCountry) ? FileManager.Configuration.Options.IMDBOptions.CertificationCountry : string.Empty;

                hashtable["%IMDBID%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.IMDBID) ? string.Empty : MovieInfo.IMDBID.Replace("tt", "").Replace("TT", "");

                if (MediaInfo != null)
                {
                    hashtable["%SUBTITLESTEXT%"] = MediaInfo.EmbeddedSubtitles.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Subtitles"), MediaInfo.GetSubtitlesList(true, MediaInfo.EmbeddedSubtitles));
                    hashtable["%SUBTITLES%"] = MediaInfo.EmbeddedSubtitles.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Subtitles"), MediaInfo.GetSubtitlesList(false, MediaInfo.EmbeddedSubtitles));
                    SplitStringList(hashtable, MediaInfo.GetSubtitlesList(false, MediaInfo.EmbeddedSubtitles), "SUBTITLES");

                    hashtable["%EXTERNALSUBTITLESTEXT%"] = MediaInfo.ExternalSubtitlesList.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Subtitles"), MediaInfo.GetSubtitlesList(true, MediaInfo.ExternalSubtitlesList));
                    hashtable["%EXTERNALSUBTITLES%"] = MediaInfo.ExternalSubtitlesList.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Subtitles"), MediaInfo.GetSubtitlesList(false, MediaInfo.ExternalSubtitlesList));
                    SplitStringList(hashtable, MediaInfo.GetSubtitlesList(false, MediaInfo.ExternalSubtitlesList), "EXTERNALSUBTITLES");

                    hashtable["%ALLSUBTITLESTEXT%"] = GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Subtitles"),
                                                MediaInfoManager.GetAllDistinctSubtitles(MediaInfo.GetSubtitlesList(true, MediaInfo.EmbeddedSubtitles), MediaInfo.GetSubtitlesList(true, MediaInfo.ExternalSubtitlesList)));
                    hashtable["%ALLSUBTITLES%"] = GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Subtitles"),
                                                MediaInfoManager.GetAllDistinctSubtitles(MediaInfo.GetSubtitlesList(false, MediaInfo.EmbeddedSubtitles), MediaInfo.GetSubtitlesList(false, MediaInfo.ExternalSubtitlesList)));

                    hashtable["%LANGUAGECODE%"] = string.IsNullOrEmpty(MediaInfo.LanguageCode) ? string.Empty : MediaInfo.LanguageCode;
                    hashtable["%LANGUAGE%"] = string.IsNullOrEmpty(MediaInfo.Language) ? string.Empty : MediaInfo.Language;

                    hashtable["%LANGUAGECODES%"] = MediaInfo.LanguageCodes.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/LanguageCodes"), MediaInfo.LanguageCodes);
                    hashtable["%LANGUAGES%"] = MediaInfo.Languages.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Languages"), MediaInfo.Languages);
                    SplitStringList(hashtable, MediaInfo.Languages, "LANGUAGES");

                    hashtable["%MEDIAFORMATTEXT%"] = Helpers.GetAttributeFromXmlNode(Helpers.SelectSingleNodeCaseInsensitive(_doc, "/Template/MediaFormats/MediaFormat", "Name", MediaModel.MediaInfoText[MediaInfo.Format.Flag]), "Text");
                    hashtable["%SOUNDFORMATTEXT%"] = Helpers.GetAttributeFromXmlNode(Helpers.SelectSingleNodeCaseInsensitive(_doc, "/Template/SoundFormats/SoundFormat", "Name", MediaModel.MediaInfoText[MediaInfo.Audio.Flag]), "Text");
                    hashtable["%RESOLUTIONTEXT%"] = Helpers.GetAttributeFromXmlNode(Helpers.SelectSingleNodeCaseInsensitive(_doc, "/Template/Resolutions/Resolution", "Name", MediaModel.MediaInfoText[MediaInfo.Resolution.Flag]), "Text");
                    hashtable["%VIDEOFORMATTEXT%"] = Helpers.GetAttributeFromXmlNode(Helpers.SelectSingleNodeCaseInsensitive(_doc, "/Template/VideoFormats/VideoFormat", "Name", MediaModel.MediaInfoText[MediaInfo.Video.Flag]), "Text");

                    hashtable["%FRAMERATETEXT%"] = string.IsNullOrEmpty(MediaInfo.FrameRate) ? string.Empty : MediaInfo.FrameRate;
                    hashtable["%FRAMERATE%"] = string.IsNullOrEmpty(MediaInfo.FrameRate) ? string.Empty : MediaInfo.FrameRate.Replace(".", "_");
                    hashtable["%ASPECTRATIOTEXT%"] = string.IsNullOrEmpty(MediaInfo.AspectRatio) ? string.Empty : MediaInfo.AspectRatio;
                    hashtable["%ASPECTRATIO%"] = string.IsNullOrEmpty(MediaInfo.AspectRatio) ? string.Empty : MediaInfo.AspectRatio.Replace(".", "_").Replace(":", "-");
                    hashtable["%VIDEORESOLUTION%"] = string.IsNullOrEmpty(MediaInfo.VideoResolution) ? string.Empty : MediaInfo.VideoResolution;
                    hashtable["%VIDEORESOLUTIONTEXT%"] = string.IsNullOrEmpty(MediaInfo.VideoResolution) ? string.Empty : MediaInfo.VideoResolution;
                    hashtable["%VIDEOCODECTEXT%"] = string.IsNullOrEmpty(MediaInfo.VideoCodec) ? string.Empty : MediaInfo.VideoCodec;
                    hashtable["%VIDEOBITRATETEXT%"] = string.IsNullOrEmpty(MediaInfo.VideoBitrate) ? string.Empty : MediaInfo.VideoBitrate;
                    hashtable["%OVERALLBITRATETEXT%"] = string.IsNullOrEmpty(MediaInfo.OverallBitrate) ? string.Empty : MediaInfo.OverallBitrate;
                    hashtable["%AUDIOCODECTEXT%"] = string.IsNullOrEmpty(MediaInfo.AudioCodec) ? string.Empty : MediaInfo.AudioCodec;
                    hashtable["%AUDIOCHANNELSTEXT%"] = string.IsNullOrEmpty(MediaInfo.AudioChannels) ? string.Empty : MediaInfo.AudioChannels;
                    hashtable["%AUDIOBITRATETEXT%"] = string.IsNullOrEmpty(MediaInfo.AudioBitrate) ? string.Empty : MediaInfo.AudioBitrate;

                    hashtable["%DURATION%"] = string.IsNullOrEmpty(MediaInfo.Duration) ? string.Empty : MediaInfo.Duration;
                    hashtable["%DURATIONSEC%"] = string.IsNullOrEmpty(MediaInfo.DurationSeconds) ? string.Empty : MediaInfo.DurationSeconds;
                    hashtable["%DURATIONTEXT%"] = string.IsNullOrEmpty(MediaInfo.FormattedDuration) ? string.Empty : MediaInfo.FormattedDuration;

                    hashtable["%FILESIZETEXT%"] = string.IsNullOrEmpty(MediaInfo.FileSize) ? string.Empty : MediaInfo.FileSize;
                    hashtable["%CONTAINERTEXT%"] = string.IsNullOrEmpty(MediaInfo.ContainerFormat) ? string.Empty : MediaInfo.ContainerFormat;
                }
                else
                {
                    hashtable["%DURATION%"] = string.Empty;
                    Loggy.Logger.Debug("Media info is null");
                }

                EpisodeData _epData = new EpisodeData(CurrentMoviePath);
                string _season = string.IsNullOrEmpty(CurrentMoviePath) ? string.Empty : _epData.Season;
                hashtable["%SEASON%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Season) ? (string.IsNullOrEmpty(_season) ? string.Empty : _season) : HttpUtility.HtmlEncode(MovieInfo.Season);
                string _episode = string.IsNullOrEmpty(CurrentMoviePath) ? string.Empty : _epData.Episode;
                hashtable["%EPISODE%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Episode) ? (string.IsNullOrEmpty(_episode) ? string.Empty : _episode) : HttpUtility.HtmlEncode(MovieInfo.Episode);

                hashtable["%EPISODETITLE%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.EpisodeName) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.EpisodeName);
                hashtable["%EPISODEPLOT%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.EpisodePlot) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.EpisodePlot);
                hashtable["%EPISODERELEASEDATE%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.EpisodeReleaseDate) ? string.Empty : HttpUtility.HtmlEncode(MovieInfo.EpisodeReleaseDate);

                hashtable["%EPISODELIST%"] = MovieInfo == null || MovieInfo.Episodes == null || MovieInfo.Episodes.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Episodes"), MovieInfo.Episodes);
                hashtable["%EPISODENAMESLIST%"] = MovieInfo == null || MovieInfo.EpisodesNames == null || MovieInfo.EpisodesNames.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/EpisodesNames"), MovieInfo.EpisodesNames);

                hashtable["%EPISODEWRITERS%"] = MovieInfo == null || MovieInfo.Writers == null || MovieInfo.Writers.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/Writers"), MovieInfo.Writers);
                hashtable["%EPISODEGUESTSTARS%"] = MovieInfo == null || MovieInfo.GuestStars == null || MovieInfo.GuestStars.Count == 0 ? string.Empty : GetFormattedEnumeration(_doc.SelectSingleNode("//Template/Settings/GuestStars"), MovieInfo.GuestStars);

                hashtable["%RATING%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Rating) ? string.Empty : string.Format("{0}/10", MovieInfo.Rating);
                try
                {
                    hashtable["%RATINGPERCENT%"] = MovieInfo == null || string.IsNullOrEmpty(MovieInfo.Rating) ? "0" : string.Format("{0:#.#}", MovieInfo.dRating * 10);
                }
                catch{}

                if (MovieInfo != null)
                {
                    // detect the studios images
                    IEnumerable<string> _studioImages = DetectImages(MovieInfo.Studios, _doc,
                                                                     "//Template/Settings/Studios/@ImagesFolder",
                                                                     "//Template/Settings/Studios/@DefaultImage",
                                                                     "Studios");
                    SplitStringList(hashtable, _studioImages, "STUDIO_IMAGE");

                    // detect the countries images
                    IEnumerable<string> _countriesImages = DetectImages(MovieInfo.Countries, _doc,
                                                                        "//Template/Settings/Countries/@ImagesFolder",
                                                                        "//Template/Settings/Countries/@DefaultImage",
                                                                        "Countries");
                    SplitStringList(hashtable, _countriesImages, "COUNTRY_IMAGE");

                    // detect the certification image
                    IEnumerable<string> _certification = DetectImages(new List<string>() {MovieInfo.Certification}, _doc,
                                                                      "//Template/Settings/Certification/@ImagesFolder",
                                                                      "//Template/Settings/Certification/@DefaultImage",
                                                                      "Certifications");
                    hashtable["%CERTIFICATION_IMAGE%"] = (_certification != null && _certification.Count() != 0)
                                                             ? _certification.ElementAt(0)
                                                             : string.Empty;

                }

                foreach (DictionaryEntry entry in hashtable)
                {
                    if (entry.Key != null && entry.Value != null)
                    {
                        string _tmp = HttpUtility.HtmlDecode(entry.Value.ToString());
                        _tmp = _tmp.ToUpper();
                        buf = buf.Replace(entry.Key.ToString() + "{UPPER}", HttpUtility.HtmlEncode(_tmp));

                        buf = buf.Replace(entry.Key.ToString() + "{LOWER}", entry.Value.ToString().ToLower());

                        _tmp = HttpUtility.HtmlDecode(entry.Value.ToString());
                        _tmp = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_tmp.ToString());
                        buf = buf.Replace(entry.Key.ToString() + "{TITLECASE}", HttpUtility.HtmlEncode(_tmp));
                        buf = buf.Replace(entry.Key.ToString(), entry.Value.ToString());
                    }
                }

                buf = ProcessRating(buf, _doc, template);

                XmlElement _tokensNode = ProcessTokens(hashtable);
                if (_tokensNode != null)
                {
                    XmlDocument _tmpDoc = new XmlDocument();
                    try
                    {
                        _tmpDoc.LoadXml(buf);
                        if (_tmpDoc.DocumentElement != null)
                        {
                            XmlNode _nodeDest = _tmpDoc.ImportNode(_tokensNode, true);
                            _tmpDoc.DocumentElement.AppendChild(_nodeDest);

                            // try to get mediainfo
                            try
                            {
                                XmlDocument _mediaDoc = new XmlDocument();
                                string _mediaDataXml = string.Empty;
                                MediaInfoManager.GetMediaInfoData(this.CurrentMoviePath, true, true, false, out _mediaDataXml);
                                if (!string.IsNullOrEmpty(_mediaDataXml))
                                {
                                    _mediaDoc.LoadXml(_mediaDataXml);
                                    XmlNode _nodeDest2 = _tmpDoc.ImportNode(_mediaDoc.DocumentElement, true);
                                    _tmpDoc.DocumentElement.AppendChild(_nodeDest2);
                                }
                            }
                            catch (Exception ex)
                            {
                                Loggy.Logger.DebugException("Cannot get mediainfo:", ex);
                            }

                            buf = _tmpDoc.OuterXml;
                        }
                    }
                    catch (Exception ex)
                    {
                        Loggy.Logger.DebugException("Cannot add tokens:", ex);
                    }
                }
                hashtable.Clear();
                hashtable = null;
            }

            return buf;
        }

        private XmlElement ProcessTokens(Hashtable hashtable)
        {
            XmlElement _result = null;
            m_Tokens.Clear();
            try
            {
                if (hashtable != null)
                {
                    SortedDictionary<string, string> _dict = new SortedDictionary<string, string>();
                    foreach (DictionaryEntry _entry in hashtable)
                    {
                        _dict.Add(_entry.Key as string, _entry.Value as string);
                    }

                    _result = new XmlDocument().CreateElement("tokens");
                    foreach(KeyValuePair<string, string> _pair in _dict)
                    {
                        if (!string.IsNullOrEmpty(_pair.Key))
                        {
                            XmlElement _elem = _result.OwnerDocument.CreateElement("token");
                            _elem.SetAttribute("name", _pair.Key);
                            _elem.InnerXml = _pair.Value;
                            _result.AppendChild(_elem);

                            try
                            {
                                m_Tokens.Add(string.Format("Token_{0}", _pair.Key.Replace("%", "")), _pair.Value);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Cannot prepare tokens", ex);
            }
            return _result;
        }

        private bool GenerateFinalMoviesheet(string targetPath)
        {
            bool _result = false;

            if (FileManager.EnableMovieSheets && RenderMoviesheet(false))
            {
                try
                {
                    File.Copy(MovieSheetTempPath, targetPath, true);

                    _result = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return _result;
        }

        public void Init()
        {
            GenerateNewTempFilesNames();
            MovieInfo = null;
        }

        private void CloneImage(string source, string dest)
        {
            if (File.Exists(source))
            {
                File.Copy(source, dest, true);
            }
        }

        private void CreateBinding(MovieSheetsGenerator source, MovieSheetsGenerator dest, string sourceProp, DependencyProperty destProp)
        {
            Binding _b = new Binding();
            _b.Source = source;
            _b.Path = new PropertyPath(sourceProp);
            dest.SetBinding(destProp, _b);
        }

        public MovieSheetsGenerator Clone(bool duplicateImages, SheetType sheettype)
        {
            MovieSheetsGenerator _result = new MovieSheetsGenerator(sheettype, this.CurrentMoviePath);

            if (duplicateImages)
            {
                CloneImage(this.BackdropTempPath, _result.BackdropTempPath);
            }
            else
            {
                CreateBinding(this, _result, "BackdropTempPath", MovieSheetsGenerator.BackdropTempPathProperty);
            }
            if (duplicateImages)
            {
                CloneImage(this.CoverTempPath, _result.CoverTempPath);
            }
            else
            {
                CreateBinding(this, _result, "CoverTempPath", MovieSheetsGenerator.CoverTempPathProperty);
            }
            if (duplicateImages)
            {
                CloneImage(this.Fanart1TempPath, _result.Fanart1TempPath);
            }
            else
            {
                CreateBinding(this, _result, "Fanart1TempPath", MovieSheetsGenerator.Fanart1TempPathProperty);
            }
            if (duplicateImages)
            {
                CloneImage(this.Fanart2TempPath, _result.Fanart2TempPath);
            }
            else
            {
                CreateBinding(this, _result, "Fanart2TempPath", MovieSheetsGenerator.Fanart2TempPathProperty);
            }
            if (duplicateImages)
            {
                CloneImage(this.Fanart3TempPath, _result.Fanart3TempPath);
            }
            else
            {
                CreateBinding(this, _result, "Fanart3TempPath", MovieSheetsGenerator.Fanart3TempPathProperty);
            }

            CreateBinding(this, _result, "MovieInfo", MovieSheetsGenerator.MovieInfoProperty);
            CreateBinding(this, _result, "MediaInfo", MovieSheetsGenerator.MediaInfoProperty);

            return _result;
        }

        public void UpdateMediaInfo()
        {
            if (this.MovieInfo != null)
            {
                this.MovieInfo.MediaInfo = this.MediaInfo;
            }
            Invalidate();
        }

        public void UpdateCover(string source)
        {
            CoverTempPath = Helpers.GetUniqueFilename(".jpg");
            AddToGarbageFiles(CoverTempPath);
            if (!string.IsNullOrEmpty(source))
            {
                Helpers.SaveImageToDisk(source, CoverTempPath);
            }
            Invalidate();
        }

        public void UpdateBackdrop(MoviesheetImageType imgType, string source)
        {
            string _tempPath = Helpers.GetUniqueFilename(".jpg"); ;
            AddToGarbageFiles(_tempPath);

            switch (imgType)
            {
                case MoviesheetImageType.Background:
                    BackdropTempPath = _tempPath;
                    break;
                case MoviesheetImageType.Fanart1:
                    Fanart1TempPath = _tempPath;
                    break;
                case MoviesheetImageType.Fanart2:
                    Fanart2TempPath = _tempPath;
                    break;
                case MoviesheetImageType.Fanart3:
                    Fanart3TempPath = _tempPath;
                    break;
            }
            if (!string.IsNullOrEmpty(source))
            {
                Helpers.SaveImageToDisk(source, _tempPath);
            }
            else
            {
                // source is null, remove the image

            }
            Invalidate();
        }

        public bool RenderAndReplicateMoviesheet(string targetPath, bool silent)
        {
            bool _result = false;

            string _tempMoviesheetPath = null;
            try
            {
                
                _tempMoviesheetPath = Helpers.GetUniqueFilename(Helpers.GetSheetExtensionBasedOnType(SheetType));
                if (this.GenerateFinalMoviesheet(_tempMoviesheetPath))
                {
                    // copy it to final moviesheet location
                    File.Copy(_tempMoviesheetPath, targetPath, true);
                    _result = true;
                    File.Delete(_tempMoviesheetPath);
                }
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Loggy.Logger.DebugException("RenderAndReplicateMoviesheet: Cannot render", ex);
            }

            return _result;
        }

        public static bool RenderAndReplicateFinalMoviesheet(MovieSheetsGenerator main, MovieSheetsGenerator extra, MovieSheetsGenerator parentfolder, bool silent)
        {
            bool _result = false;

            try
            {
                try
                {
                    if (FileManager.Configuration.Options.AutogenerateMovieSheet && main != null)
                    {
                        _result = main.RenderAndReplicateMoviesheet(FileManager.Configuration.GetMoviesheetPath(main.CurrentMoviePath, true), silent);
                    }

                    if (FileManager.Configuration.Options.AutogenerateMoviesheetForFolder && extra != null)
                    {
                        _result = extra.RenderAndReplicateMoviesheet(FileManager.Configuration.GetMoviesheetForFolderPath(extra.CurrentMoviePath, true), silent);
                    }

                    if (FileManager.Configuration.Options.AutogenerateMoviesheetForParentFolder && parentfolder != null)
                    {
                        _result = parentfolder.RenderAndReplicateMoviesheet(FileManager.Configuration.GetMoviesheetForParentFolderPath(parentfolder.CurrentMoviePath, true), silent);
                    }

                    if (FileManager.Configuration.Options.AutogenerateMoviesheetMetadata && main != null)
                    {
                        if (main.SelectedTemplate != null && !string.IsNullOrEmpty(main.SelectedTemplate.TemplateName))
                        {
                            // important..update the mediainfo
                            main.UpdateMediaInfo();

                            MoviesheetsUpdateManager _man = MoviesheetsUpdateManager.CreateManagerForMovie(main.CurrentMoviePath);
                            MoviesheetsUpdateManagerParams _params = new MoviesheetsUpdateManagerParams(main.BackdropTempPath,
                                                                                                        main.Fanart1TempPath,
                                                                                                        main.Fanart2TempPath,
                                                                                                        main.Fanart3TempPath,
                                                                                                        main.MovieInfo,
                                                                                                        main.CoverTempPath,
                                                                                                        main.MovieSheetPreviewTempPath);
                            _man.GenerateUpdateFile(_params, main.SelectedTemplate.TemplateName);
                            _man = null;
                            _result = true;
                        }
                        else
                        {
                            main.LastError = "You must have a selected main template in order to generate metadata";
                        }
                    }

                    if (FileManager.Configuration.Options.GenerateParentFolderMetadata && parentfolder != null)
                    {
                        if (parentfolder.SelectedTemplate != null && !string.IsNullOrEmpty(parentfolder.SelectedTemplate.TemplateName))
                        {
                            // important..update the mediainfo
                            parentfolder.UpdateMediaInfo();

                            MoviesheetsUpdateManager _man = MoviesheetsUpdateManager.CreateManagerForParentFolder(parentfolder.CurrentMoviePath);
                            MoviesheetsUpdateManagerParams _params = new MoviesheetsUpdateManagerParams(parentfolder.BackdropTempPath,
                                                                                                        parentfolder.Fanart1TempPath,
                                                                                                        parentfolder.Fanart2TempPath,
                                                                                                        parentfolder.Fanart3TempPath,
                                                                                                        parentfolder.MovieInfo,
                                                                                                        parentfolder.CoverTempPath,
                                                                                                        parentfolder.MovieSheetPreviewTempPath);
                            _man.GenerateUpdateFile(_params, parentfolder.SelectedTemplate.TemplateName);
                            _man = null;
                            _result = true;
                        }
                        else
                        {
                            parentfolder.LastError = "You must have a selected main template in order to generate metadata";
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!silent)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Loggy.Logger.DebugException("RenderAndReplicateFinalMoviesheet: Cannot render", ex);
                }
            }
            finally
            {

            }

            return _result;
        }

        private MemoryStream XslTransformStream(string xslPath, Stream stream, Dictionary<string, string> parameters)
        {
            if (File.Exists(xslPath))
            {
                MemoryStream _transformedStream = new MemoryStream();
                using (FileStream _fs = new FileStream(xslPath, FileMode.Open, FileAccess.Read))
                {

                    //XmlTextReader _xformReader = new XmlTextReader(_fs);

                    XslCompiledTransform _xform = new XslCompiledTransform(true);
                    _xform.Load(xslPath, XsltSettings.TrustedXslt, new XmlUrlResolver());

                    XPathDocument _document = new XPathDocument(stream);

                    XsltArgumentList _argList = new XsltArgumentList();
                    if (parameters != null)
                    {
                        foreach (KeyValuePair<string, string> _pair in parameters)
                        {
                            _argList.AddParam(_pair.Key, "", _pair.Value);
                        }
                    }

                    _argList.AddExtensionObject("tg:ThumbGenUtils", new XSLExtensions());

                    _xform.Transform(_document, _argList, _transformedStream);

                    if (_transformedStream != null)
                    {
                        _transformedStream.Position = 0;
                    }
                }
                return _transformedStream;
            }
            else
            {
                return null;
            }
        }

        public bool RenderMoviesheet(bool getThumbnail)
        {
            LastError = null;

            Loggy.Logger.Debug("Entering rendermoviesheet");

            bool _result = false;

            string _moviesheetpath = MovieSheetTempPath;
            string _previewmoviesheetpath = MovieSheetPreviewTempPath;

            SheetOutputType _oType = Helpers.GetSheetOutputType(SheetType);

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                try
                {
                    if (getThumbnail || !File.Exists(_moviesheetpath) || NeedsRender) // render only if thumbMode asked or when full rendering is asked but there is no image rendered yet
                    {
                        Loggy.Logger.Debug("Needs render");

                        _moviesheetpath = Helpers.GetUniqueFilename(".jpg");
                        AddToGarbageFiles(_moviesheetpath);

                        _previewmoviesheetpath = Helpers.GetUniqueFilename(".jpg");
                        AddToGarbageFiles(_previewmoviesheetpath);

                        Loggy.Logger.Debug("Start analyze template");
                        string templateXml = AnalyzeTemplate(SelectedTemplate);
                        Loggy.Logger.Debug("End analyze template");

                        if (!string.IsNullOrEmpty(templateXml))
                        {
                            templateXml = ProcessXSLScript(templateXml);

                            RenderedXML = templateXml;
                            ImageFormat _imgFormat = ImageFormat.Jpeg;
                            switch (_oType)
                            {
                                case SheetOutputType.JPG:
                                    _imgFormat = ImageFormat.Jpeg;
                                    break;
                                case SheetOutputType.PNG:
                                    _imgFormat = ImageFormat.Png;
                                    break;
                            }

                            TemplateRenderer renderer = new TemplateRenderer();
                            try
                            {
                                var renderedResult = renderer.Render(templateXml, _imgFormat);

                                System.Drawing.Size _previewSize = GetPreviewThumbSize(new System.Drawing.Size(renderedResult.Width, renderedResult.Height));

                                File.WriteAllBytes(_moviesheetpath, renderedResult.Data);

                                Loggy.Logger.Debug(string.Format("Rendering using {0}; thumbmode={1}; done in {2}ms; file= {3}",
                                                SelectedTemplate == null ? "" : SelectedTemplate.TemplateName,
                                                getThumbnail.ToString(), 
                                                renderedResult.RenderDurationMilliseconds, 
                                                Path.GetFileName(this.CurrentMoviePath)));

                                // save the small preview too...
                                Loggy.Logger.Debug("Saving small preview");
                                Helpers.CreateThumbnailImage(_moviesheetpath, _previewmoviesheetpath, true, false, _previewSize, true, double.PositiveInfinity);
                                Loggy.Logger.Debug("Small preview saved");

                                // correct overscan if required
                                if (FileManager.Configuration.Options.MovieSheetsOptions.IsOverscanCorrectionNeeded(this.SheetType))
                                {
                                    Helpers.CorrectOverscan(_moviesheetpath, FileManager.Configuration.Options.MovieSheetsOptions.OverscanLeft,
                                                                             FileManager.Configuration.Options.MovieSheetsOptions.OverscanTop,
                                                                             FileManager.Configuration.Options.MovieSheetsOptions.OverscanRight,
                                                                             FileManager.Configuration.Options.MovieSheetsOptions.OverscanBottom, 
                                                                             new System.Drawing.Size(renderedResult.Width, renderedResult.Height),
                                                                             SheetType);
                                    Loggy.Logger.Debug("Overscan correction applied");
                                }

                                // reset it
                                NeedsRender = false;
                            }
                            catch (Exception ex)
                            {
                                Loggy.Logger.Debug("Exception loading template:" + ex.Message + "\r\n" + templateXml);
                                throw ex;
                            }
                        }
                        _result = true;
                    }
                    else
                    {
                        Loggy.Logger.Debug("No render needed");
                    }

                    // resize it (only if not thumb moviesheet and only if needed from Settings)
                    if (!getThumbnail && !FileManager.Configuration.Options.MovieSheetsOptions.IsMaxQuality && (_oType == SheetOutputType.JPG))
                    {
                        Loggy.Logger.Debug("Needs resize");
                        string _tmpPath = Helpers.GetUniqueFilename(".jpg");
                        try
                        {
                            double _maxFilesize = FileManager.Configuration.Options.MovieSheetsOptions.IsMaxQuality ? double.PositiveInfinity : FileManager.Configuration.Options.MovieSheetsOptions.MaxFilesize;
                            // get target image dimensions
                            System.Drawing.Size _imgSize = Helpers.GetImageSize(_moviesheetpath);
                            // generate the smaller image to _tmpPath
                            if (File.Exists(_moviesheetpath))
                            {
                                Loggy.Logger.Debug("Preparing to reduce filesize");
                                if (Helpers.CreateThumbnailImage(_moviesheetpath, _tmpPath, true, false, _imgSize, true, _maxFilesize))
                                {
                                    Loggy.Logger.Debug("Filesize reduced");
                                    // overwrite the initial image with the smaller one
                                    File.Copy(_tmpPath, _moviesheetpath, true);
                                }
                                else
                                {
                                    Loggy.Logger.Debug("Cannot reduce filesize");
                                }
                            }
                            else
                            {
                                _result = false;
                                throw new Exception("Moviesheet was not rendered...");
                            }
                        }
                        finally
                        {
                            try
                            {
                                File.Delete(_tmpPath);
                            }
                            catch { }
                        }
                    }
                    _result = true;
                    //LastRenderError = "Invalid or no template.";

                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message + "\r\n" + (ex.InnerException != null ? ex.InnerException.Message : string.Empty);
                Loggy.Logger.DebugException("render", ex);
            }

            MovieSheetTempPath = _moviesheetpath;
            MovieSheetPreviewTempPath = _previewmoviesheetpath;

            return _result;
        }

        private string ProcessXSLScript(string templateXml)
        {
            string _xslPath = Path.Combine(GetPATH(this.SelectedTemplate), "template.xslt");
            if (File.Exists(_xslPath))
            {
                Loggy.Logger.Debug("XSL Found");
                Stream _transformedXml = this.XslTransformStream(_xslPath, new MemoryStream(Encoding.UTF8.GetBytes(templateXml)), m_Tokens);
                if (_transformedXml != null && _transformedXml.CanSeek)
                {
                    _transformedXml.Position = 0;
                    using (StreamReader reader = new StreamReader(_transformedXml))
                    {
                        string _s = reader.ReadToEnd();
                        if (!string.IsNullOrEmpty(_s))
                        {
                            templateXml = _s;
                        }
                    }
                    _transformedXml.Dispose();
                    _transformedXml = null;
                }
                Loggy.Logger.Debug("XSL Processed");
            }
            return templateXml;
        }

        private void SetMediaInfoFromMovieInfoOrFile(string movieFile)
        {
            if (this.MovieInfo != null)
            {
                try
                {
                    this.MediaInfo = this.MovieInfo.MediaInfo;
                    if (this.MediaInfo == null && !string.IsNullOrEmpty(movieFile))
                    {
                        this.MediaInfo = MediaInfoManager.GetMediaInfoData(movieFile);
                    }
                }
                catch { }
            }
        }

        public bool CreateMoviesheetFromMetadata(string metadataFile, string movieFile, string targetFile, bool silent)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(metadataFile) && !string.IsNullOrEmpty(targetFile))
            {
                MoviesheetsUpdateManager _metaMan = MoviesheetsUpdateManager.CreateManagerFromMetadata(metadataFile, movieFile);

                _metaMan.GetImage(MoviesheetsUpdateManager.COVER_STREAM_NAME, this.CoverTempPath);
                _metaMan.GetImage(MoviesheetsUpdateManager.BACKGROUND_STREAM_NAME, this.BackdropTempPath);
                _metaMan.GetImage(MoviesheetsUpdateManager.FANART1_STREAM_NAME, this.Fanart1TempPath);
                _metaMan.GetImage(MoviesheetsUpdateManager.FANART2_STREAM_NAME, this.Fanart2TempPath);
                _metaMan.GetImage(MoviesheetsUpdateManager.FANART3_STREAM_NAME, this.Fanart3TempPath);

                this.MovieInfo = _metaMan.GetMovieInfo();

                SetMediaInfoFromMovieInfoOrFile(movieFile);

                return this.RenderAndReplicateMoviesheet(targetFile, silent);
            }
            return _result;
        }

        public bool CreateMoviesheetFromCustomData(string background, string cover, string fanart1, string fanart2, string fanart3, string nfoFile, string movieFile, string targetFile, bool silent)
        {
            this.UpdateBackdrop(MoviesheetImageType.Background, background);
            this.UpdateBackdrop(MoviesheetImageType.Fanart1, fanart1);
            this.UpdateBackdrop(MoviesheetImageType.Fanart2, fanart2);
            this.UpdateBackdrop(MoviesheetImageType.Fanart3, fanart3);
            this.UpdateCover(cover);
            nfoFileType nfotype = nfoFileType.Unknown;
            this.MovieInfo = nfoHelper.LoadNfoFile(movieFile, nfoFile, out nfotype);

            SetMediaInfoFromMovieInfoOrFile(movieFile);

            return this.RenderAndReplicateMoviesheet(targetFile, silent);
        }


        #region IDisposable Members

        public void Dispose()
        {
            //ClearGarbage();
        }

        #endregion


    }

}
