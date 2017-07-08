using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Net;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Drawing.Imaging;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Web;
using System.Windows.Data;
using System.Threading;
using ThumbGen.Subtitles;
using System.Windows.Threading;
using System.Globalization;
using Microsoft.Win32;
using System.Reflection;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Net.Configuration;
using System.Xml.Linq;

namespace ThumbGen
{
    public enum SheetType
    {
        Main,
        Extra,
        Spare
    }

    public enum SheetOutputType
    {
        JPG,
        PNG
    }

    public static class XExtensions
    {
        public static string SafeElementValue(this XElement element, XName name)
        {
            string _result = string.Empty;
            if (element != null && element.HasElements && name != null)
            {
                XElement _el = element.Element(name);
                _result = _el != null ? _el.Value : string.Empty;
            }
            return _result;
        }

        public static string SafeDescendantValue(this XElement element, XName name)
        {
            string _result = string.Empty;
            if (element != null && name != null)
            {
                IEnumerable<XElement> _descs = element.Descendants(name);
                if (_descs != null && _descs.Count() != 0)
                {
                    _result = _descs.First().Value;
                }
            }
            return _result;
        }

        public static IEnumerable<string> SafeDescendantValues(this XElement element, XName name)
        {
            IEnumerable<string> _result = null;
            if (element != null && name != null)
            {
                IEnumerable<XElement> _descs = element.Descendants(name);
                if (_descs != null && _descs.Count() != 0)
                {
                    _result = _descs.Select(e => { return e.Value; });
                }
            }
            return _result;
        }

        public static string SafeAttributeValue(this XElement element, string name)
        {
            string _result = string.Empty;
            if (element != null && element.HasAttributes && !string.IsNullOrEmpty(name))
            {
                XAttribute _el = element.Attribute(name);
                _result = _el != null ? _el.Value : string.Empty;
            }
            return _result;
        }

        public static string SafeXPathValue(this XElement element, string expression)
        {
            string _result = string.Empty;
            if (element != null && !string.IsNullOrEmpty(expression))
            {
                XElement _e = element.XPathSelectElement(expression);
                _result = _e != null ? _e.Value : string.Empty;
            }
            return _result;
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null || action == null) return;
            foreach (T element in source)
                action(element);
        }
    }

    public static class Helpers
    {
        public static string DRAGDROP_COVER_FORMAT = "CoverFormat";
        public static string DRAGDROP_BACKDROP_FORMAT = "BackdropFormat";
        public static string DRAGDROP_MOVIEINFO_FORMAT = "MovieInfoFormat";

        public static void AddRange<T>(this Collection<T> collection, IEnumerable<T> values)
        {
            foreach (var item in values)
            {
                collection.Add(item);
            }
        }

        public static string GetCorrectThumbnailPath(string moviePath, bool forcePath)
        {
            string _result = System.IO.Path.ChangeExtension(moviePath, MP4Tagger.MP4Manager.FIXED_JPG_EXTENSION);
            if (!File.Exists(_result))
            {
                _result = FileManager.Configuration.GetThumbnailPath(moviePath, forcePath);
            }
            return _result;
        }

        public static string GetCorrectThumbnailPath(string moviePath, bool isFolder, bool forcePath)
        {
            string _folderPath = FileManager.Configuration.Options.NamingOptions.FolderjpgName(moviePath);//Path.Combine(moviePath, FileManager.Configuration.FolderjpgName);
            _folderPath = Path.Combine(moviePath, Path.ChangeExtension(_folderPath, MP4Tagger.MP4Manager.FIXED_JPG_EXTENSION));
            if (!File.Exists(_folderPath))
            {
                // if Folder must add dummy file name to the moviepath for the GetFolderjpg to work properly
                moviePath = isFolder ? Path.Combine(moviePath, "dum1.dum") : moviePath;
                _folderPath = FileManager.Configuration.GetFolderjpgPath(moviePath, forcePath);// Path.Combine(moviePath, FileManager.Configuration.Options.NamingOptions.FolderjpgName(moviePath));//Path.Combine(moviePath, FileManager.Configuration.FolderjpgName);
            }
            return isFolder ? _folderPath : GetCorrectThumbnailPath(moviePath, forcePath);
        }

        public static string GetMovieFolderName(string filePath, string defaultValue)
        {
            return GetMovieFolderNameByLevel(filePath, defaultValue, 1);
            //if (!string.IsNullOrEmpty(filePath))
            //{
            //    string _folder = Path.GetDirectoryName(filePath);
            //    if (_folder != null)
            //    {
            //        string[] _folders = _folder.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            //        try
            //        {
            //            _folder = _folders.Length > 0 ? _folders[_folders.Length - 1] : defaultValue;
            //            return _folder;
            //        }
            //        catch
            //        {
            //            Loggy.Logger.Debug("Exception accesing folders for " + filePath);
            //            return string.Empty;
            //        }
            //    }
            //    else
            //    {
            //        Loggy.Logger.Debug("_folder is null for " + filePath);
            //        return string.Empty;
            //    }
            //}
            //else
            //{
            //    return string.Empty;
            //}
        }

        public static string GetMovieParentFolderName(string filePath, string defaultValue)
        {
            return GetMovieFolderNameByLevel(filePath, defaultValue, 2);

            //string _result = defaultValue;

            //if (!string.IsNullOrEmpty(filePath))
            //{
            //    string _folder = System.IO.Path.GetDirectoryName(filePath);
            //    if (!string.IsNullOrEmpty(_folder))
            //    {
            //        string[] _folders = _folder.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            //        _result = _folders != null && _folders.Length > 2 ? _folders[_folders.Length - 2] : defaultValue;
            //    }
            //}

            //return _result;
        }


        // level = 1 for the movie folder, = 2 for the parentfolder and so on
        public static string GetMovieFolderNameByLevel(string filePath, string defaultValue, int level)
        {
            string _result = defaultValue;

            if (!string.IsNullOrEmpty(filePath))
            {
                string _folder = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(_folder))
                {
                    string[] _folders = _folder.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    _result = _folders != null && _folders.Length >= level ? _folders[_folders.Length - level] : defaultValue;
                }
            }

            return _result;
        }

        public static string GetValueFromXmlNode(XmlNode node)
        {
            return node != null && !string.IsNullOrEmpty(node.InnerText) ? node.InnerText : string.Empty;
        }

        public static string GetValueFromXmlNode(XmlNode node, string tag)
        {
            return Helpers.GetValueFromXmlNode(node.SelectSingleNode(tag));
        }

        public static string GetAttributeFromXmlNode(XmlNode node, string attribute)
        {
            return node != null && node.Attributes[attribute] != null ? node.Attributes[attribute].Value : string.Empty;
        }

        public static System.Drawing.Size ThumbnailSize = new System.Drawing.Size(200, 300);

        public static double MaxThumbnailFilesize = 69000d;

        public static string GetFormattedImageSize(object source, double width, double height)
        {
            return GetFormattedImageSize(source, width, height, -1);
        }

        public static string GetFormattedImageSize(object source, double width, double height, double size)
        {
            if (size > 0)
            {
                return source != null ? string.Format("{0:0} x {1:0} - {2:0} KB ({3:0} bytes)", width, height, size / 1024, size) : "<unknown size>";
            }
            else
            {
                return source != null ? string.Format("{0:0} x {1:0}", width, height) : "<unknown size>";
            }
        }

        public static BitmapImage LoadImage(string imagePath)
        {
            return LoadImage(imagePath, -1);
        }

        [Obsolete]
        public static BitmapImage LoadImage5(string imagePath, int decodeWidth)
        {
            if (File.Exists(imagePath))
            {
                try
                {
                    // Load image as thumb  
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    if (decodeWidth != -1)
                    {
                        image.DecodePixelWidth = decodeWidth;
                    }
                    image.UriSource = new Uri(imagePath);
                    image.EndInit();
                    //image.Freeze();

                    // save image to stream  
                    MemoryStream ms = new MemoryStream();
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    // load stream as the real bitmap image  
                    BitmapImage memImage = new BitmapImage();
                    memImage.BeginInit();
                    memImage.CacheOption = BitmapCacheOption.OnLoad;
                    memImage.StreamSource = ms;
                    memImage.EndInit();
                    memImage.Freeze();

                    ms.Close();
                    ms.Dispose();
                    ms = null;

                    image = null;

                    return memImage;
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("Load image exception. Path=" + imagePath, ex);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static BitmapImage LoadImage(Stream source)
        {
            BitmapImage _result = null;
            if (source != null && source.CanRead)
            {
                // load stream as the real bitmap image  
                _result = new BitmapImage();
                _result.BeginInit();
                _result.CacheOption = BitmapCacheOption.OnLoad;
                _result.StreamSource = source;
                _result.EndInit();
                _result.Freeze();
                source.Dispose();
                source = null;
            }
            return _result;
        }

        [Obsolete]
        public static BitmapImage LoadImage3(string imagePath, int decodeWidth, bool noCache)
        {
            if (File.Exists(imagePath))
            {
                // Load image as thumb  
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                if (decodeWidth != -1)
                {
                    image.DecodePixelWidth = decodeWidth;
                }
                image.UriSource = new Uri(imagePath);
                image.EndInit();
                image.Freeze();

                // save image to stream  
                MemoryStream ms = new MemoryStream();
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                // load stream as the real bitmap image  
                BitmapImage memImage = new BitmapImage();
                memImage.BeginInit();
                if (!noCache)
                {
                    memImage.CacheOption = BitmapCacheOption.OnLoad;
                }
                else
                {
                    memImage.CacheOption = BitmapCacheOption.None;
                }
                memImage.StreamSource = ms;
                memImage.EndInit();
                memImage.Freeze();

                ms.Close();
                ms.Dispose();
                ms = null;

                return memImage;
            }
            else
            {
                return null;
            }
        }

        public static BitmapImage LoadImage(string imagePath, int decodeWidth)
        {
            BitmapImage _result = null;

            try
            {

                //if (File.Exists(imagePath))
                {
                    _result = new BitmapImage();

                    byte[] buffer = DownloadData(imagePath);
                    if (buffer == null || buffer.Length == 0)
                    {
                        return null;
                    }

                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        ms.Position = 0;

                        _result.BeginInit();
                        if (decodeWidth != -1)
                        {
                            _result.DecodePixelWidth = decodeWidth;
                        }
                        _result.CacheOption = BitmapCacheOption.OnLoad;
                        _result.StreamSource = ms;
                        _result.EndInit();
                        if (_result.CanFreeze)
                        {
                            _result.Freeze();
                        }
                    }
                    buffer = null;
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("loadimage: " + imagePath, ex);
                _result = null;
            }

            return _result;
        }

        public static byte[] DownloadData(string url)
        {
            return DownloadData(url, false);
        }

        public static byte[] DownloadData(string url, bool silent)
        {
            byte[] downloadedData = new byte[0];
            try
            {
                Uri _testUri = new Uri(url, UriKind.RelativeOrAbsolute);
                if (_testUri.IsFile)
                {
                    if (!File.Exists(_testUri.LocalPath))
                    {
                        return null;
                    }
                }


                WebRequest req = WebRequest.Create(url);
                req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable);
                req.Timeout = 30000;

                WebResponse response = req.GetResponse();
                Stream stream = response.GetResponseStream();

                //Download in chuncks
                //byte[] buffer = new byte[8192];

                //Get Total Size
                int dataLength = (int)response.ContentLength;

                //Download to memory
                //Note: adjust the streams here to download directly to the hard drive
                using (MemoryStream memStream = new MemoryStream())
                {
                    stream.CopyToAsync(memStream);
                    memStream.Position = 0;

                    //Convert the downloaded stream to a byte array
                    downloadedData = memStream.ToArray();

                    //Clean up
                    stream.Dispose();
                    stream.Close();
                    stream = null;
                }
                response.Close();
                response = null;

                return downloadedData;
            }
            catch (Exception ex)
            {
                //May not be connected to the internet
                //Or the URL might not exist
                Loggy.Logger.DebugException("There was an error accessing the URL:" + url, ex);
                return null;
            }
        }

        public static bool ResizeImage(Stream originalImage, Stream destThumbNailFile, long quality, bool keepAspectRatio, bool allowCrop, System.Drawing.Size targetSize, bool forceSkipWatermark)
        {
            bool _result = false;

            try
            {
                using (Bitmap bmp = new Bitmap(originalImage))
                {
                    // if targetSize is zero then keep initial size
                    int destWidth = targetSize.Width == 0 ? bmp.Width : targetSize.Width;
                    int destHeight = targetSize.Height == 0 ? bmp.Height : targetSize.Height;

                    destWidth = Math.Min(destWidth, bmp.Width);
                    destHeight = Math.Min(destHeight, bmp.Height);

                    float nPercent = 0;

                    Bitmap _croppedbmp = null;

                    if (allowCrop)
                    {
                        _croppedbmp = bmp.Clone(new Rectangle(0, 0, destWidth, destHeight), System.Drawing.Imaging.PixelFormat.DontCare);
                    }
                    else
                        if (keepAspectRatio)
                        {
                            int sourceWidth = bmp.Width;
                            int sourceHeight = bmp.Height;


                            float nPercentW = 0;
                            float nPercentH = 0;

                            nPercentW = ((float)destWidth / (float)sourceWidth);
                            nPercentH = ((float)destHeight / (float)sourceHeight);

                            if (nPercentH < nPercentW)
                                nPercent = nPercentH;
                            else
                                nPercent = nPercentW;

                            destWidth = (int)(sourceWidth * nPercent);
                            destHeight = (int)(sourceHeight * nPercent);
                        }

                    using (Bitmap thumb = new Bitmap(destWidth, destHeight))
                    {
                        using (Graphics g = Graphics.FromImage(thumb))
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            //Set Image codec of JPEG type, the index of JPEG codec is "1"            
                            System.Drawing.Imaging.ImageCodecInfo codec = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()[1];
                            //Set the parameters for defining the quality of the thumbnail...         
                            System.Drawing.Imaging.EncoderParameters eParams = new System.Drawing.Imaging.EncoderParameters(1);
                            eParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                            //Now draw the image on the instance of thumbnail Bitmap object            
                            g.DrawImage(allowCrop ? _croppedbmp : bmp, new Rectangle(0, 0, destWidth, destHeight));

                            if (FileManager.Configuration.Options.AddWatermark && (FileManager.Mode == ProcessingMode.Manual || FileManager.Mode == ProcessingMode.SemiAutomatic) && !forceSkipWatermark)
                            {
                                try
                                {
                                    System.Windows.Media.Color _color = GetColorFromIndex(FileManager.Configuration.Options.WatermarkOptions.FontColorIndex);
                                    System.Drawing.Color clr2 = System.Drawing.Color.FromArgb(_color.A, _color.R, _color.G, _color.B);
                                    System.Drawing.SolidBrush _brush = new SolidBrush(clr2);
                                    float _fontSize = (float)FileManager.Configuration.Options.WatermarkOptions.FontSize / (96 / 72f);

                                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                                    g.TextContrast = 0;

                                    System.Drawing.FontStyle _fstyle = new System.Drawing.FontStyle();
                                    if (FileManager.Configuration.Options.WatermarkOptions.Bold)
                                    {
                                        _fstyle = _fstyle | System.Drawing.FontStyle.Bold;
                                    }
                                    if (FileManager.Configuration.Options.WatermarkOptions.Italic)
                                    {
                                        _fstyle = _fstyle | System.Drawing.FontStyle.Italic;
                                    }

                                    System.Drawing.FontFamily _ffamily = new System.Drawing.FontFamily(FileManager.Configuration.Options.WatermarkOptions.FontFamily);

                                    Font _font = new Font(_ffamily, _fontSize, _fstyle);

                                    g.DrawString(FileManager.Configuration.Options.WatermarkOptions.Text, _font, _brush,
                                                 (float)FileManager.Configuration.Options.WatermarkOptions.Position.Width,
                                                 (float)FileManager.Configuration.Options.WatermarkOptions.Position.Height);
                                }
                                catch { }
                            }

                            thumb.Save(destThumbNailFile, codec, eParams);
                            if (destThumbNailFile.CanSeek)
                            {
                                destThumbNailFile.Position = 0;
                            }

                            _result = true;
                        }
                    }
                    if (_croppedbmp != null)
                    {
                        _croppedbmp.Dispose();
                        _croppedbmp = null;
                    }
                }
            }
            catch
            {
                _result = false;
            }
            return _result;
        }

        public static System.Windows.Media.Color GetColorFromIndex(int colorIndex)
        {
            try
            {
                ComboBox _cb = new ComboBox();
                _cb.ItemsSource = typeof(System.Windows.Media.Brushes).GetProperties();
                _cb.SelectedIndex = colorIndex;

                string[] _s = _cb.Text.Split(' ');
                string _brushName = _s[1];
                System.Windows.Media.SolidColorBrush _brush = new BrushConverter().ConvertFromString(_brushName) as System.Windows.Media.SolidColorBrush;

                return _brush.Color;
            }
            catch
            {
                return Colors.Black;
            }
        }

        public static bool CreateExtraThumbnailImage(string imageURL, string destinationImagePath)
        {
            double _maxFileSize = FileManager.Configuration.Options.SaveOriginalCoverAsExtraThumbnail ? double.PositiveInfinity : Helpers.MaxThumbnailFilesize;
            System.Drawing.Size _targetSize = FileManager.Configuration.Options.SaveOriginalCoverAsExtraThumbnail ? new System.Drawing.Size(0, 0) : Helpers.ThumbnailSize;
            return Helpers.CreateThumbnailImage(imageURL, destinationImagePath, FileManager.Configuration.Options.KeepAspectRatio, false, _targetSize, false, _maxFileSize);
        }

        public static bool CreateThumbnailImage(string imageURL, string destinationImagePath, bool keepAspectRatio)
        {
            return CreateThumbnailImage(imageURL, destinationImagePath, keepAspectRatio, false, Helpers.ThumbnailSize, false, Helpers.MaxThumbnailFilesize);
        }

        public static bool CreateThumbnailImage(string imageURL, string destinationImagePath, bool keepAspectRatio, bool allowCrop,
                                                System.Drawing.Size targetSize, bool forceSkipWatermark, double maxFilesize)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(imageURL))
            {

                try
                {
                    // download the image to the dest folder
                    byte[] _downloadedData = Helpers.DownloadData(imageURL);
                    if (_downloadedData != null && _downloadedData.Length != 0)
                    {
                        MemoryStream _ms = new MemoryStream(_downloadedData);
                        _ms.Position = 0;

                        if (File.Exists(destinationImagePath))
                        {
                            new FileInfo(destinationImagePath).Attributes = FileAttributes.Normal;
                            File.Delete(destinationImagePath);
                        }

                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationImagePath));
                        }
                        catch { }

                        //copy anyway the source to destination (in case no resize is needed the image will not be later copied to target)
                        try
                        {
                            (_ms as Stream).CopyTo(destinationImagePath);
                            _ms.Position = 0;
                        }
                        catch (Exception ex)
                        {
                            Loggy.Logger.DebugException(string.Format("Cannot copy {0} to {1}", imageURL, destinationImagePath), ex);
                        }

                        Loggy.Logger.Debug(string.Format("Resizing {0} to {1}", imageURL, destinationImagePath));
                        bool _tooBig = true;
                        long _quality = 100L;
                        DateTime _start = DateTime.UtcNow;
                        while (_tooBig)
                        {
                            if (maxFilesize != double.PositiveInfinity)
                            {
                                using (MemoryStream _destStream = new MemoryStream())
                                {
                                    if (Helpers.ResizeImage(_ms, _destStream, _quality, keepAspectRatio, allowCrop, targetSize, forceSkipWatermark))
                                    {
                                        long _size = _destStream.Length;
                                        _tooBig = _size > maxFilesize;
                                        Loggy.Logger.Debug(string.Format("Q {0}, S {1} - {2}", _quality, _size, _tooBig.ToString()));
                                        _quality = _quality > 95 ? _quality - 1 : _quality - 5;
                                        // if not too big, write the stream to file
                                        if (!_tooBig)
                                        {
                                            _destStream.CopyTo(destinationImagePath);
                                        }
                                    }
                                    else
                                    {
                                        // no need to save img as it is saved already
                                        _tooBig = false;
                                    }
                                }
                            }
                            else
                            {
                                // no need to save img as it is saved already
                                _tooBig = false;
                                // check if it needs resizing and do it, even if there is no max filesize
                                if (targetSize.Width != 0 && targetSize.Height != 0)
                                {
                                    using (MemoryStream _destStream = new MemoryStream())
                                    {
                                        if (Helpers.ResizeImage(_ms, _destStream, _quality, keepAspectRatio, allowCrop, targetSize, forceSkipWatermark))
                                        {
                                            _destStream.CopyTo(destinationImagePath);
                                        }
                                    }
                                }
                            }
                            // do not try more than 1 minute
                            if (DateTime.UtcNow - _start > TimeSpan.FromMinutes(1))
                            {
                                break;
                            }
                        }
                        _ms.Dispose();
                        _ms = null;
                        _downloadedData = null;
                        _result = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return _result;
        }

        public static Stream TempImageFromGDIplus(Stream stream)
        {
            MemoryStream _result = new MemoryStream();
            Bitmap badMetadataImage = new Bitmap(stream);

            // get an ImageCodecInfo object that represents the JPEG codec
            ImageCodecInfo myImageCodecInfo = GetEncoderInfo("image/jpeg");
            // Create an Encoder object based on the GUID for the Quality parameter category
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            // Create an EncoderParameters object
            // An EncoderParameters object has an array of EncoderParameter objects.
            // In this case, there is only one EncoderParameter object in the array.
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            // Save the image as a JPEG file with quality level 75.
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 75L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            badMetadataImage.Save(_result, myImageCodecInfo, myEncoderParameters);
            _result.Position = 0;
            return _result;
        }

        public static void CorrectOverscan(string imagePath, int left, int top, int right, int bottom, System.Drawing.Size totalSize, SheetType sheetType)
        {
            // resize the image to fit the newly created rectangle inside
            System.Drawing.Size _innerSize = new System.Drawing.Size();
            _innerSize.Width = Math.Max(0, totalSize.Width - left - right);
            _innerSize.Height = Math.Max(0, totalSize.Height - top - bottom);

            string _tmpPath = Helpers.GetUniqueFilename(Helpers.GetSheetExtensionBasedOnType(sheetType));
            File.Copy(imagePath, _tmpPath);
            try
            {
                int _destWidth = _innerSize.Width;
                int _destHeight = _innerSize.Height;
                //float nPercent = 0;

                using (Bitmap bmp = new Bitmap(_tmpPath))
                {
                    //if (keepAspectRatio)
                    //{
                    //    int sourceWidth = bmp.Width;
                    //    int sourceHeight = bmp.Height;


                    //    float nPercentW = 0;
                    //    float nPercentH = 0;

                    //    nPercentW = ((float)_innerSize.Width / (float)sourceWidth);
                    //    nPercentH = ((float)_innerSize.Height / (float)sourceHeight);

                    //    if (nPercentH < nPercentW)
                    //        nPercent = nPercentH;
                    //    else
                    //        nPercent = nPercentW;

                    //    _destWidth = (int)(sourceWidth * nPercent);
                    //    _destHeight = (int)(sourceHeight * nPercent);
                    //}

                    using (Bitmap thumb = new Bitmap(totalSize.Width, totalSize.Height))
                    {
                        using (Graphics g = Graphics.FromImage(thumb))
                        {
                            SheetOutputType _oType = Helpers.GetSheetOutputType(sheetType);

                            System.Drawing.Imaging.ImageCodecInfo codec = null;
                            System.Drawing.Imaging.EncoderParameters eParams = null;

                            switch (_oType)
                            {
                                case SheetOutputType.JPG:
                                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                    //Set Image codec of JPEG type, the index of JPEG codec is "1"            
                                    codec = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()[1];
                                    //Set the parameters for defining the quality of the thumbnail...         
                                    eParams = new System.Drawing.Imaging.EncoderParameters(1);
                                    eParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                                    break;
                                case SheetOutputType.PNG:
                                    //Set Image codec of PNG type, the index of PNGG codec is "4"            
                                    codec = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()[4];
                                    break;
                            }

                            //Now draw the image on the instance of thumbnail Bitmap object            
                            g.DrawImage(bmp, new Rectangle(left, top, _destWidth, _destHeight));

                            thumb.Save(imagePath, codec, eParams);

                        }
                    }
                }
            }
            finally
            {
                File.Delete(_tmpPath);
            }
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        public static void OpenUrlInBrowser(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch { }
        }

        public static void ScrollIntoViewCentered(this System.Windows.Controls.ListBox listBox, object item)
        {
            // Get the container for the specified item
            var container = listBox.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
            if (null != container)
            {
                // Get the bounds of the item container
                var rect = new Rect(new System.Windows.Point(), container.RenderSize);

                // Find constraining parent (either the nearest ScrollContentPresenter or the ListBox itself)
                FrameworkElement constrainingParent = container;
                do
                {
                    constrainingParent = VisualTreeHelper.GetParent(constrainingParent) as FrameworkElement;
                } while ((null != constrainingParent) &&
                         (listBox != constrainingParent) &&
                         !(constrainingParent is ScrollContentPresenter));

                if (null != constrainingParent)
                {
                    // Inflate rect to fill the constraining parent
                    rect.Inflate(
                        Math.Max((constrainingParent.ActualWidth - rect.Width) / 2, 0),
                        Math.Max((constrainingParent.ActualHeight - rect.Height) / 2, 0));
                }

                // Bring the (inflated) bounds into view
                container.BringIntoView(rect);
            }
        }

        public static string GetPage(string url, Encoding encoding, bool a)
        {
            string str = string.Empty;
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)");
                //client.Proxy = proxy;
                byte[] bytes = client.DownloadData(url);
                str = encoding.GetString(bytes);
                bytes = null;
            }
            catch (Exception exception)
            {
                Loggy.Logger.DebugException("page get", exception);
            }
            return str;
        }

        public static string RemoveHTMLTags(string text)
        {
            return Regex.Replace(text, "<(.|\n)*?>", "");
        }

        public static string GetPage(string sUrl)
        {
            return GetPage(sUrl, Encoding.UTF8);
        }

        public static string GetPage(string sUrl, Encoding encoding)
        {
            byte[] _data = DownloadData(sUrl, true);
            if (_data != null)
            {
                string _result = encoding.GetString(_data);
                _data = null;
                return _result;
            }
            else
            {
                return null;
            }
        }

        public static string StripHTML(string sHTML)
        {
            string sSource = sHTML;
            string str2 = "";
            sHTML = sHTML.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
            if (sHTML != "")
            {
                do
                {
                    if (str2 != "")
                    {
                        if (((str2.ToLower() == "br") || (str2.ToLower() == "br/")) || (str2.ToLower() == "br /"))
                        {
                            sSource = sSource.Replace("<" + str2 + ">", "\n");
                        }
                        else
                        {
                            sSource = sSource.Replace("<" + str2 + ">", "");
                        }
                    }
                    str2 = Extract(sSource, "<", ">", 0);
                }
                while (str2 != "");
            }
            return sSource;
        }

        public static void TrimList(List<string> list)
        {
            if (list != null && list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = list[i].Trim();
                }
            }
        }

        public static string Extract(string sSource, string sBegin, string sEnd, int iEnd)
        {
            string str = "";
            if (sSource.Contains(sBegin))
            {
                int startIndex = sSource.IndexOf(sBegin) + sBegin.Length;
                if (sSource.Substring(startIndex).Contains(sEnd))
                {
                    str = sSource.Substring(startIndex, sSource.Substring(startIndex).IndexOf(sEnd));
                    iEnd = startIndex + sEnd.Length;
                }
            }
            return str;
        }

        public delegate void SaveImageToDiskHandler(string source, string dest);

        public static void SaveImageToDisk(Window owner, string imageUrl)
        {
            string _remoteFileName = Path.GetFileName(imageUrl);
            if (!string.IsNullOrEmpty(_remoteFileName) && owner != null)
            {
                SaveFileDialog _sfd = new SaveFileDialog();
                _sfd.Filter = string.Format("Images (*{0})|*{0}", Path.GetExtension(_remoteFileName));
                _sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                _sfd.FileName = _remoteFileName;
                if ((bool)_sfd.ShowDialog(owner))
                {
                    owner.Dispatcher.BeginInvoke(new Helpers.SaveImageToDiskHandler(Helpers.SaveImageToDisk),
                        DispatcherPriority.Background, new object[] { imageUrl, _sfd.FileName });
                }
            }
        }

        public static void SaveImageToDisk(string source, string dest)
        {
            try
            {
                if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(dest))
                {
                    using (FileStream _fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                    {
                        byte[] _data = Helpers.DownloadData(source);
                        if (_data != null)
                        {
                            _fs.Write(_data, 0, _data.Length);
                        }
                    }
                }
            }
            catch { }
        }

        public static byte[] ComputeMovieHash(string filename)
        {
            byte[] result;
            using (Stream input = File.OpenRead(filename))
            {
                result = ComputeMovieHash(input);
            }
            return result;
        }

        public static byte[] ComputeMovieHash(Stream input)
        {
            long lhash, streamsize;
            streamsize = input.Length;
            lhash = streamsize;

            long i = 0;
            byte[] buffer = new byte[sizeof(long)];
            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }

            input.Position = Math.Max(0, streamsize - 65536);
            i = 0;
            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }
            input.Close();
            byte[] result = BitConverter.GetBytes(lhash);
            Array.Reverse(result);
            return result;
        }

        public static string ToHexadecimal(byte[] bytes)
        {
            StringBuilder hexBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                hexBuilder.Append(bytes[i].ToString("x2"));
            }
            return hexBuilder.ToString();
        }

        public static System.Drawing.Size GetImageSize(string filename)
        {
            System.Drawing.Size _result = new System.Drawing.Size(0, 0);
            if (File.Exists(filename))
            {
                BitmapImage _bmp = Helpers.LoadImage(filename);
                if (_bmp != null)
                {
                    int _width = _bmp != null ? _bmp.PixelWidth : 0;
                    int _height = _bmp != null ? _bmp.PixelHeight : 0;
                    _result.Width = _width;
                    _result.Height = _height;
                }
            }
            return _result;
        }

        public static void DoEvents()
        {
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Input, new ThreadStart(delegate { }));
            }
        }

        public static bool IsDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool GetEmbeddedAssembly(string assemblyName, string targetFileName)
        {
            bool _result = false;
            try
            {
                //System.Reflection.Assembly a = System.Reflection.Assembly.Load(assemblyName);
                Stream str = Assembly.GetEntryAssembly().GetManifestResourceStream(string.Format("ThumbGen.Assemblies.{0}", assemblyName));
                if (File.Exists(targetFileName))
                {
                    try
                    {
                        File.Delete(targetFileName);
                    }
                    catch { }
                }
                str.CopyTo(targetFileName);
                _result = true;
            }
            catch// (Exception e)
            {
                //throw new Exception(assemblyName + ": " + e.Message);
            }

            return _result;
        }

        public static bool GetEmbeddedPreset(string fileName, string targetFileName)
        {
            bool _result = false;
            try
            {
                //System.Reflection.Assembly a = System.Reflection.Assembly.Load(assemblyName);
                Stream str = Assembly.GetEntryAssembly().GetManifestResourceStream(string.Format("ThumbGen.Presets.{0}", fileName));
                if (File.Exists(targetFileName))
                {
                    try
                    {
                        File.Delete(targetFileName);
                    }
                    catch { }
                }
                str.CopyTo(targetFileName);
                _result = true;
            }
            catch (Exception e)
            {
                throw new Exception(fileName + ": " + e.Message);
            }

            return _result;
        }

        public static string GetUniqueFilename(string extension)
        {
            string _filename = Guid.NewGuid().ToString() + extension;
            string _tmpPath = Path.Combine(Path.GetTempPath(), FileManager.THUMBGEN_TEMP);
            Directory.CreateDirectory(_tmpPath);
            return Path.Combine(_tmpPath, _filename);
        }

        public static XmlNode SelectSingleNodeCaseInsensitive(XmlDocument doc, string anteQuery, string attName, string value)
        {
            if (doc != null && !string.IsNullOrEmpty(anteQuery) && !string.IsNullOrEmpty(attName))
            {
                //books/book[translate(@type, ‘ABCDEFGHIJKLMNOPQRSTUVWXYZ’, ‘abcdefghijklmnopqrstuvwxyz’) =’fiction’”
                try
                {
                    return doc.SelectSingleNode(string.Format("{0}[translate(@{1},'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz') = '{2}']", anteQuery, attName, value.ToLowerInvariant()));
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

        public static string GetSubstringBetweenStrings(string input, string start, string stop)
        {
            string _result = null;

            if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(stop))
            {
                int _idx1 = input.IndexOf(start);
                if (_idx1 > 0)
                {
                    int _idx2 = input.IndexOf(stop, _idx1 + start.Length);
                    if (_idx2 > _idx1)
                    {
                        _result = input.Substring(_idx1, _idx2 - _idx1);
                    }
                }
            }
            return _result;
        }

        public static string EncodeToBase64(string str)
        {
            byte[] encbuff = System.Text.Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(encbuff);
        }
        public static string DecodeFromBase64(string str)
        {
            byte[] decbuff = Convert.FromBase64String(str);
            return System.Text.Encoding.UTF8.GetString(decbuff);
        }

        public static string GetFormattedFileSize(double byteCount)
        {
            string size = "0 Bytes";
            if (byteCount >= 1073741824.0)
                size = String.Format("{0:##.##}", byteCount / 1073741824.0) + " GB";
            else if (byteCount >= 1048576.0)
                size = String.Format("{0:##.##}", byteCount / 1048576.0) + " MB";
            else if (byteCount >= 1024.0)
                size = String.Format("{0:##.##}", byteCount / 1024.0) + " KB";
            else if (byteCount > 0 && byteCount < 1024.0)
                size = byteCount.ToString() + " Bytes";

            return size;
        }

        public static string GetFormattedBitrate(string input)
        {
            Match _m = Regex.Match(input, "\\d+");
            return _m.Success ? _GetFormattedBitrate(_m.Value) : "?";
        }

        private static string _GetFormattedBitrate(string input)
        {
            string _result = "?";
            double _bitrate = 0;
            if (double.TryParse(input, out _bitrate))
            {
                if (_bitrate != 0)
                {
                    if (_bitrate >= 1000000000.0)
                        _result = String.Format("{0:0}", _bitrate / 1000000000.0) + " Gbps";
                    else if (_bitrate >= 1000000.0)
                        _result = String.Format("{0:0}", _bitrate / 1000000.0) + " Mbps";
                    else if (_bitrate >= 1000.0)
                        _result = String.Format("{0:0}", _bitrate / 1000.0) + " Kbps";
                    else if (_bitrate > 0 && _bitrate < 1000.0)
                        _result = _bitrate.ToString() + " bps";
                }
            }
            return _result;
        }

        public static long GetDurationSeconds(string formattedDuration)
        {
            if (string.IsNullOrEmpty(formattedDuration))
            {
                return 0;
            }
            Match _match = Regex.Match(formattedDuration, "(?<Hours>[0-9]+)h\\s(?<Mins>[0-9]+)m", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (_match.Success)
            {
                return (long)(TimeSpan.FromHours(Int16.Parse(_match.Groups["Hours"].Value)).TotalSeconds) + (long)(TimeSpan.FromMinutes(Int16.Parse(_match.Groups["Mins"].Value)).TotalSeconds);
            }
            else
            {
                return 0;
            }
        }

        public static bool IsBlurayPath(string movieFilename)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(movieFilename))
            {
                Match _match = Regex.Match(movieFilename, @"bdmv\\stream", RegexOptions.IgnoreCase);
                return _match.Success;
                //string _folder = GetMovieFolderName(movieFilename, string.Empty);
                //string _parentFolder = GetMovieParentFolderName(movieFilename, string.Empty);
                //if ((string.Compare(_parentFolder, "bdmv", true) == 0) && (string.Compare(_folder, "stream", true) == 0))
                //{
                //    _result = true;
                //}
            }
            return _result;
        }

        public static string GetBlurayMovieFolderName(string movieFilename, string defaultValue)
        {
            if (IsBlurayPath(movieFilename))
            {
                Match _match = Regex.Match(movieFilename, @"\\([^\\]*)\\bdmv\\stream\\", RegexOptions.IgnoreCase);
                if (_match.Success)
                {
                    return _match.Groups[1].Value;
                }
                else
                {
                    return defaultValue;

                }

                //string _folder = Path.GetDirectoryName(movieFilename);
                //string[] _folders = _folder.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                //_folder = _folders.Length > 0 ? _folders[_folders.Length - 3] : Path.GetFileNameWithoutExtension(movieFilename);
                //return !string.IsNullOrEmpty(_folder) ? _folder : defaultValue;
            }
            else
            {
                return GetMovieFolderName(movieFilename, defaultValue);
            }
        }

        public static string GetBlurayRootDirectory(string movieFilename)
        {
            if (IsBlurayPath(movieFilename))
            {
                return Regex.Replace(movieFilename, @"bdmv\\stream\\(.*)?", "", RegexOptions.IgnoreCase).Trim();
            }
            else
            {
                return Path.GetDirectoryName(movieFilename);
            }
        }

        public static bool IsDVDPath(string movieFilename)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(movieFilename))
            {
                Match _match = Regex.Match(movieFilename, @"video_ts\\", RegexOptions.IgnoreCase);
                return _match.Success;

                //string _parentFolder = GetMovieFolderName(movieFilename, Path.GetFileNameWithoutExtension(movieFilename));
                //if (string.Compare(_parentFolder, "video_ts", true) == 0)
                //{
                //    _result = true;
                //}
            }

            return _result;
        }

        public static string GetDVDMovieFolderName(string movieFilename, string defaultValue)
        {
            if (IsDVDPath(movieFilename))
            {
                return GetMovieParentFolderName(movieFilename, defaultValue);
            }
            else
            {
                return GetMovieFolderName(movieFilename, defaultValue);
            }
        }

        public static string GetDVDRootDirectory(string movieFilename)
        {
            if (IsDVDPath(movieFilename))
            {
                return Regex.Replace(movieFilename, "video_ts(.*)?", "", RegexOptions.IgnoreCase).Trim();
            }
            else
            {
                return Path.GetDirectoryName(movieFilename);
            }
        }

        public static MemoryStream XslTransformStream(string xslPath, Stream stream, Dictionary<string, string> parameters)
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

        public static MemoryStream XslTransformFile(string xslPath, string xml, Dictionary<string, string> parameters)
        {
            MemoryStream _result;
            using (FileStream _fs = new FileStream(xml, FileMode.Open, FileAccess.Read))
            {
                _result = XslTransformStream(xslPath, _fs, parameters);
            }
            return _result;
        }

        public static MemoryStream XslTransformEmbededStream(string xform, Stream stream, Dictionary<string, string> parameters)
        {

            string _xformPath = string.Format(CultureInfo.InvariantCulture, "ThumbGen.XSLT.{0}", xform);
            Stream _xformStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(_xformPath);
            XmlTextReader _xformReader = new XmlTextReader(_xformStream);

            XslCompiledTransform _xform = new XslCompiledTransform(true);
            _xform.Load(_xformReader);

            XPathDocument _document = new XPathDocument(stream);

            MemoryStream _transformedStream = new MemoryStream();

            XsltArgumentList _argList = new XsltArgumentList();
            if (parameters != null)
            {
                foreach (KeyValuePair<string, string> _pair in parameters)
                {
                    _argList.AddParam(_pair.Key, "", _pair.Value);
                }
            }

            _xform.Transform(_document, _argList, _transformedStream);

            if (_transformedStream != null)
            {
                _transformedStream.Position = 0;
            }

            stream.Dispose();
            stream = null;

            _xformStream.Dispose();
            _xformStream = null;

            _xform = null;

            return _transformedStream;
        }

        private static bool SetAllowUnsafeHeaderParsing()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(SettingsSection));
            if (assembly != null)
            {
                Type type = assembly.GetType("System.Net.Configuration.SettingsSectionInternal");
                if (type != null)
                {
                    object obj2 = type.InvokeMember("Section", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Static, null, null, new object[0]);
                    if (obj2 != null)
                    {
                        FieldInfo field = type.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(obj2, true);
                            return true;
                        }
                    }
                }
            }
            return false;
        }


        public static Dictionary<string, string> dCookie = new Dictionary<string, string>();

        public static string GetPage(string sUrl, WebProxy oProxy, Encoding oEncoding, string sReferer, bool bMethodGet, bool bWithCookies, string acceptedLanguages = null)
        {
            string str = "";
            SetAllowUnsafeHeaderParsing();
            WebClient client = new WebClient();
            string str2 = "";
            try
            {
                //client.Proxy = oProxy;
                client.Encoding = oEncoding;
                client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/532.5 (KHTML, like Gecko) Chrome/4.0.249.78 Safari/532.5");
                client.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                if (!string.IsNullOrEmpty(acceptedLanguages))
                {
                    client.Headers.Add(HttpRequestHeader.AcceptLanguage, acceptedLanguages);
                }
                if (bWithCookies && (dCookie.Count > 0))
                {
                    string str3 = "";
                    str = "";
                    foreach (string str4 in dCookie.Keys)
                    {
                        dCookie.TryGetValue(str4, out str3);
                        string str8 = str;
                        str = str8 + str4 + "=" + str3 + "; ";
                    }
                    str = str.TrimEnd(new char[] { ' ' }).TrimEnd(new char[] { ';' });
                    client.Headers.Add(HttpRequestHeader.Cookie, str);
                }
                if (sReferer != "")
                {
                    client.Headers.Add(HttpRequestHeader.Referer, sReferer);
                }
                if (bMethodGet)
                {
                    str2 = client.DownloadString(sUrl);
                }
                else
                {
                    string str5 = sUrl.Split(new char[] { '?' })[1];
                    NameValueCollection data = new NameValueCollection();
                    foreach (string str6 in str5.Split("&".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        data.Add(str6.Split(new char[] { '=' })[0], HttpUtility.UrlDecode(str6.Split(new char[] { '=' })[1]));
                    }
                    str2 = Encoding.UTF8.GetString(client.UploadValues(sUrl.Split(new char[] { '?' })[0], "post", data));
                }
                str = client.ResponseHeaders["Set-Cookie"];
                if (bWithCookies && (str != null))
                {
                    foreach (string str7 in str.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!str7.StartsWith(" "))
                        {
                            if (dCookie.ContainsKey(str7.Split(new char[] { '=' })[0]))
                            {
                                dCookie.Remove(str7.Split(new char[] { '=' })[0]);
                            }
                            dCookie.Add(str7.Split(new char[] { '=' })[0], str7.Split(new char[] { '=' })[1].Split(new char[] { ';' })[0]);
                        }
                    }
                }
            }
            catch (WebException exception)
            {
                if (exception.Response != null)
                {
                    Stream responseStream = exception.Response.GetResponseStream();
                    StreamReader reader = new StreamReader(responseStream, oEncoding);
                    str2 = reader.ReadToEnd();
                    reader.Close();
                    reader.Dispose();
                    responseStream.Close();
                    responseStream.Dispose();
                }
            }
            catch (Exception exception2)
            {
                Loggy.Logger.DebugException("getpage", exception2);
            }
            client.Dispose();
            return str2;
        }

        public static string GetPagePost(string url, Encoding encoding, string postData)
        {
            string _result = null;
            try
            {
            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(postData))
            {
                HttpWebRequest _req = (HttpWebRequest)WebRequest.Create(url);
                // Set values for the request back
                _req.Method = "POST";
                _req.ContentType = "application/x-www-form-urlencoded";
                _req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 5.1; Trident/4.0; .NET CLR 2.0.50727; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; .NET4.0C; .NET4.0E)";

                _req.ContentLength = postData.Length;
                // Write the request
                try
                {
                    StreamWriter _stOut = new StreamWriter(_req.GetRequestStream(), encoding);
                    _stOut.Write(postData);
                    _stOut.Close();
                }
                catch
                {
                    return _result;
                }
                // Do the request to get the response
                WebResponse _response = _req.GetResponse();
                Stream receiveStream = _response.GetResponseStream();

                // Pipes the stream to a higher level stream reader with the required encoding format. 
                StreamReader readStream = new StreamReader(receiveStream, encoding);

                _result = readStream.ReadToEnd();
                _response.Close();
                readStream.Close();
            }
            }
            catch (WebException we)
            {
                Loggy.Logger.Error(we.Message);
                MessageBox.Show(we.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
            return _result;
        }

        public static CultureInfo GetCultureInfo(string twoletterCode)
        {
            CultureInfo _result = CultureInfo.InvariantCulture;
            try
            {
                if (String.Compare(twoletterCode, "zh", true) == 0)
                {
                    _result = new CultureInfo("zh-CHS");
                }
                else
                {
                    _result = new CultureInfo(twoletterCode);
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("GetCultureInfo(by tag)", ex);
            }
            return _result;
        }

        public static CultureInfo GetCultureInfoFromEnglishName(string englishName)
        {
            CultureInfo _result = CultureInfo.InvariantCulture;
            foreach (CultureInfo _ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (string.Compare(_ci.EnglishName, englishName, true) == 0)
                {
                    _result = _ci;
                    break;
                }
            }
            return _result;
        }

        public static void RemoveFile(string filepath)
        {
            try
            {
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }
            }
            catch { }
        }

        public static string StreamToString(MemoryStream ms)
        {
            return new StreamReader(ms).ReadToEnd();
        }

        public static string RotateFlip(string fileName, RotateFlipType rotateFlipType)
        {
            string _result = null;

            if (File.Exists(fileName))
            {
                using (FileStream _fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    Bitmap _temp = (Bitmap)System.Drawing.Image.FromStream(_fs);
                    Bitmap _bmp = (Bitmap)_temp.Clone();
                    _bmp.RotateFlip(rotateFlipType);
                    Bitmap _tmp2 = (Bitmap)_bmp.Clone();

                    _result = Helpers.GetUniqueFilename(Path.GetExtension(fileName));
                    _tmp2.Save(_result);
                }
            }
            return _result;
        }

        public static string GetFormattedDate(DateTime date)
        {
            return string.Format(string.Format("{{0:{0}}}", FileManager.Configuration.Options.CustomDateFormat), date);
        }

        public static string GetFormattedDate(string date)
        {
            return GetFormattedDate(date, FileManager.Configuration.Options.CustomDateFormat);
        }

        public static string GetFormattedDate(string date, string format)
        {
            DateTimeFormatInfo _dtfi = new DateTimeFormatInfo();
            if (!string.IsNullOrEmpty(date))
            {
                Match _m = Regex.Match(format, "[^\\d^d^D^m^M^y^Y]");
                if (_m.Success)
                {
                    _dtfi.DateSeparator = _m.Value;
                }
                _dtfi.ShortDatePattern = format;
            }
            return Helpers.GetFormattedDate(date, _dtfi /*CultureInfo.CurrentCulture.DateTimeFormat*/);
        }

        public static string GetFormattedDate(string date, IFormatProvider provider)
        {
            //return string.Format(string.Format("{{0:{0}}}", FileManager.Configuration.Options.CustomDateFormat), DateTime.Now.Date);
            string _result = date;
            DateTime _out = DateTime.MinValue;
            if (DateTime.TryParse(date, provider, DateTimeStyles.None, out _out) && _out != DateTime.MinValue)
            {
                //_result = _out.ToString(FileManager.Configuration.Options.CustomDateFormat);
                _result = Helpers.GetFormattedDate(_out);
            }
            return _result;
        }

        public static SheetOutputType GetSheetOutputType(SheetType sheetType)
        {
            return GetSheetOutputType(GetSheetExtensionBasedOnType(sheetType));
        }

        public static SheetOutputType GetSheetOutputType(string ext)
        {
            SheetOutputType _result = SheetOutputType.JPG;
            switch (ext.ToLowerInvariant())
            {
                case ".jpg":
                    _result = SheetOutputType.JPG;
                    break;
                case ".png":
                    _result = SheetOutputType.PNG;
                    break;
            }
            return _result;
        }

        public static string GetSheetExtensionBasedOnType(SheetType sheetType)
        {
            string _result = string.Empty;
            switch (sheetType)
            {
                case SheetType.Main:
                    _result = FileManager.Configuration.Options.NamingOptions.MoviesheetExtension;
                    break;
                case SheetType.Extra:
                    _result = FileManager.Configuration.Options.NamingOptions.MoviesheetForFolderExtension;
                    break;
                case SheetType.Spare:
                    _result = FileManager.Configuration.Options.NamingOptions.MoviesheetForParentFolderExtension;
                    break;
            }
            return _result;
        }

    }

    public enum Country
    {
        International, USA, Canada, Germany, Spain, France, Romania, UK, Italy, Portugal, Netherlands, CzechRep, Poland, Israel, Hungary, Switzerland,
        Russia, Estonia, Korea, Brasil, Turkey, Denmark
    }

    public class CountryImageConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string _result = "/images/flags/";
            switch ((Country)value)
            {
                case (Country.Canada):
                    return _result + "ca.png";
                case (Country.France):
                    return _result + "fr.png";
                case (Country.Germany):
                    return _result + "de.png";
                case (Country.Romania):
                    return _result + "ro.png";
                case (Country.Spain):
                    return _result + "es.png";
                case (Country.UK):
                    return _result + "uk.png";
                case (Country.USA):
                    return _result + "us.png";
                case (Country.Italy):
                    return _result + "it.png";
                case (Country.Portugal):
                    return _result + "pt.png";
                case (Country.CzechRep):
                    return _result + "cz.png";
                case (Country.Poland):
                    return _result + "pl.png";
                case (Country.Netherlands):
                    return _result + "nl.png";
                case (Country.Hungary):
                    return _result + "hu.png";
                case (Country.Switzerland):
                    return _result + "ch.jpg";
                case (Country.Israel):
                    return _result + "hb.png";
                case (Country.Russia):
                    return _result + "ru.png";
                case (Country.Estonia):
                    return _result + "ee.png";
                case (Country.Korea):
                    return _result + "kr.png";
                case (Country.Brasil):
                    return _result + "br.png";
                case (Country.Turkey):
                    return _result + "tr.png";
                case (Country.Denmark):
                    return _result + "dk.png";
                default:
                    return _result + "global.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }

    public static class CountryCode
    {
        public static Dictionary<Country, string> Codes = new Dictionary<Country, string>() 
        { 
            {Country.International, "us"},
            {Country.Canada, "ca"},
            {Country.France, "fr"},
            {Country.Germany, "de"},
            {Country.Italy, "it"},
            {Country.Netherlands, "nl"},
            {Country.Portugal, "pt"},
            {Country.Romania, "us"},
            {Country.Spain, "es"},
            {Country.UK, "gb"},
            {Country.USA, "us"},
            {Country.CzechRep, "cz"},
            {Country.Poland, "pl"},
            {Country.Israel, "he"},
            {Country.Hungary, "hu"},
            {Country.Switzerland, "ch"},
            {Country.Russia, "ru"},
            {Country.Estonia, "ee"},
            {Country.Korea, "kr"},
            {Country.Brasil, "br"},
            {Country.Turkey, "tr"},
            {Country.Denmark, "dk"}
        };

        public static string GetCode(Country country)
        {
            return Codes[country];
        }

        public static Country GetCountry(string countryCode)
        {
            foreach (KeyValuePair<Country, string> _pair in Codes)
            {
                if (string.Compare(_pair.Value, countryCode, true) == 0)
                {
                    return _pair.Key;
                }
            }
            return Country.International;
        }

        private static SortedDictionary<string, string> m_countryList = null;
        public static SortedDictionary<string, string> CountriesList
        {
            get
            {
                if (m_countryList == null)
                {
                    m_countryList = new SortedDictionary<string, string>();
                    // Iterate the Framework Cultures...
                    foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.FrameworkCultures))
                    {
                        RegionInfo ri = null;
                        try
                        {
                            ri = new RegionInfo(ci.Name);
                        }
                        catch
                        {
                            // If a RegionInfo object could not be created we don't want to use the CultureInfo
                            //    for the country list.
                            continue;
                        }
                        // Create new country dictionary entry.
                        KeyValuePair<string, string> newKeyValuePair = new KeyValuePair<string, string>(ri.TwoLetterISORegionName.ToLowerInvariant(), ri.EnglishName);

                        // If the country is not alreayd in the countryList add it...
                        if (!(m_countryList.ContainsKey(ri.TwoLetterISORegionName.ToLowerInvariant())))
                        {
                            m_countryList.Add(newKeyValuePair.Key, newKeyValuePair.Value);
                        }
                    }
                }
                return m_countryList;
            }
        }
    }

    public class FileWatcher
    {
        public event EventHandler Changed;
        private object m_UserData;

        FileSystemWatcher m_Watcher = new FileSystemWatcher();

        public FileWatcher()
        {
        }

        public void StopMonitor()
        {
            m_Watcher.EnableRaisingEvents = false;
        }

        public void StartMonitor(string folder, object userData)
        {
            m_UserData = userData;

            //this is the path we want to monitor
            m_Watcher.Path = folder;


            //Add a list of Filter we want to specify
            //make sure you use OR for each Filter as we need to
            //all of those 

            m_Watcher.NotifyFilter = System.IO.NotifyFilters.DirectoryName;
            m_Watcher.NotifyFilter = m_Watcher.NotifyFilter | System.IO.NotifyFilters.Attributes;

            // add the handler to each event
            m_Watcher.Changed += new FileSystemEventHandler(logchange);
            m_Watcher.Created += new FileSystemEventHandler(logchange);
            m_Watcher.Deleted += new FileSystemEventHandler(logchange);
            // add the rename handler as the signature is different
            m_Watcher.Renamed += new System.IO.RenamedEventHandler(logrename);

            //set { this property to true to start watching
            m_Watcher.EnableRaisingEvents = true;
        }

        private bool IsValidExtension(string fileName)
        {
            string _ext = "*" + Path.GetExtension(fileName).ToLowerInvariant();
            return SubtitlesManager.SubtitlesSupported.Contains(_ext);
        }

        private void logchange(object sender, System.IO.FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed ||
               e.ChangeType == WatcherChangeTypes.Created ||
               e.ChangeType == WatcherChangeTypes.Deleted)
            {
                if (Changed != null && IsValidExtension(e.Name))
                {
                    Changed(this, new MonitorEventArgs(m_UserData));
                }
            }

            //if (e.ChangeType == System.IO.WatcherChangeTypes.Changed)
            //{
            //    //txt_folderactivity.Text += "File " + e.FullPath + " has been modified" + "\r\n";
            //}
            //if (e.ChangeType == System.IO.WatcherChangeTypes.Created)
            //{
            //    //txt_folderactivity.Text += "File " + e.FullPath + " has been created" + "\r\n";
            //}
            //if (e.ChangeType == System.IO.WatcherChangeTypes.Deleted)
            //{
            //    //txt_folderactivity.Text += "File " + e.FullPath + " has been deleted" + "\r\n";
            //}

        }

        public void logrename(object sender, System.IO.RenamedEventArgs e)
        {
            //txt_folderactivity.Text += "File" + e.OldName + " has been renamed to " + e.Name + "\r\n";
            if (Changed != null && IsValidExtension(e.Name))
            {
                Changed(this, new MonitorEventArgs(m_UserData));
            }
        }

    }

    public class MonitorEventArgs : EventArgs
    {
        public object Data;

        public MonitorEventArgs(object data)
        {
            Data = data;
        }
    }

    public static class StreamExtensions
    {
        public static void CopyTo(this Stream src, Stream dest)
        {
            int size = (src.CanSeek) ? Math.Min((int)(src.Length - src.Position), 0x2000) : 0x2000;
            byte[] buffer = new byte[size];
            int n;
            do
            {
                n = src.Read(buffer, 0, buffer.Length);
                dest.Write(buffer, 0, n);
            } while (n != 0);
        }

        public static void CopyTo(this MemoryStream src, Stream dest)
        {
            dest.Write(src.GetBuffer(), (int)src.Position, (int)(src.Length - src.Position));
        }

        public static void CopyTo(this Stream src, MemoryStream dest)
        {
            if (src.CanSeek)
            {
                int pos = (int)dest.Position;
                int length = (int)(src.Length - src.Position) + pos;
                dest.SetLength(length);
                while (pos < length)
                    pos += src.Read(dest.GetBuffer(), pos, length - pos);
            }
            else
                src.CopyTo((Stream)dest);
        }

        public static void CopyTo(this Stream src, string fileName)
        {
            if (src != null && src.CanRead && src.CanSeek)
            {
                using (FileStream _fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    src.CopyTo(_fs);
                }
            }
        }

        private const int DEFAULT_BUFFER_SIZE = short.MaxValue; // +32767
        public static void CopyToAsync(this Stream input, Stream output)
        {
            input.CopyToAsync(output, DEFAULT_BUFFER_SIZE);
        }

        public static void CopyToAsync(this Stream input, Stream output, int bufferSize)
        {
            if (!input.CanRead) throw new InvalidOperationException("input must be open for reading");
            if (!output.CanWrite) throw new InvalidOperationException("output must be open for writing");

            byte[][] buf = { new byte[bufferSize], new byte[bufferSize] };
            int[] bufl = { 0, 0 };
            int bufno = 0;
            IAsyncResult read = input.BeginRead(buf[bufno], 0, buf[bufno].Length, null, null);
            IAsyncResult write = null;

            while (true)
            {

                // wait for the read operation to complete
                read.AsyncWaitHandle.WaitOne();
                bufl[bufno] = input.EndRead(read);

                // if zero bytes read, the copy is complete
                if (bufl[bufno] == 0)
                {
                    break;
                }

                // wait for the in-flight write operation, if one exists, to complete
                // the only time one won't exist is after the very first read operation completes
                if (write != null)
                {
                    write.AsyncWaitHandle.WaitOne();
                    output.EndWrite(write);
                }

                // start the new write operation
                write = output.BeginWrite(buf[bufno], 0, bufl[bufno], null, null);

                // toggle the current, in-use buffer
                // and start the read operation on the new buffer.
                //
                // Changed to use XOR to toggle between 0 and 1.
                // A little speedier than using a ternary expression.
                bufno ^= 1; // bufno = ( bufno == 0 ? 1 : 0 ) ;
                read = input.BeginRead(buf[bufno], 0, buf[bufno].Length, null, null);

            }

            // wait for the final in-flight write operation, if one exists, to complete
            // the only time one won't exist is if the input stream is empty.
            if (write != null)
            {
                write.AsyncWaitHandle.WaitOne();
                output.EndWrite(write);
            }

            output.Flush();
        }
    }

    public static class ListExtensions
    {
        public static List<string> ToTrimmedList(this string[] values)
        {
            List<string> _result = new List<string>();

            if (values != null && values.Count() != 0)
            {
                _result = values.ToList<string>();
                Helpers.TrimList(_result);
            }

            return _result;
        }

        public static List<string> ToTrimmedList(this List<string> values)
        {
            Helpers.TrimList(values);
            return values;
        }

        public static List<string> ToListWithoutEmptyItems(this string[] values)
        {
            return values != null ? ToListWithoutEmptyItems(values.ToList<string>()) : new List<string>();

            //List<string> _result = new List<string>();

            //var _end = (from c in values
            //           where !string.IsNullOrEmpty(c)
            //           select c).ToList<string>();
            //_result.AddRange(_end);
            //return _result;
        }

        public static List<string> ToListWithoutEmptyItems(this List<string> values)
        {
            List<string> _result = new List<string>();

            var _end = (from c in values
                        where !string.IsNullOrEmpty(c)
                        select c).ToList<string>();
            _result.AddRange(_end);
            return _result;
        }
    }

    public class PosterIndexConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int _index = (int)value;
            if (_index > 0)
            {
                return string.Format(" ( Cover {0})", _index);
            }
            else
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        #endregion
    }

    public class BoolNotConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        #endregion
    }

    public class BackdropConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!FileManager.Configuration.Options.FileBrowserOptions.ShowMovieSheet)
            {
                return null;
            }

            string _result = value as string;

            string _input = _result;
            if (!string.IsNullOrEmpty(_input))
            {
                string _folder = null;
                string _movieName = null;

                if (Helpers.IsDirectory(_input))
                {
                    _folder = _input;

                    if (FileManager.Configuration.Options.FileBrowserOptions.ShowMovieSheetAtFolderLevel)
                    {
                        List<FileInfo> _movies = new FilesCollector().CollectFiles(_folder, false) as List<FileInfo>;
                        if (_movies != null && _movies.Count > 0)
                        {
                            _movieName = _movies[0].FullName;
                        }
                    }

                    string _parentFolderSheet = FileManager.Configuration.GetMoviesheetForParentFolderPath(Path.Combine(_input, "dummy\\dummy.dum"), false);
                    if (File.Exists(_parentFolderSheet))
                    {
                        return new ImageLockFixConverter().Convert(_parentFolderSheet, typeof(ImageSource), parameter, CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    _folder = Path.GetDirectoryName(_input);
                    _movieName = _input;
                }

                if (!string.IsNullOrEmpty(_folder))
                {
                    _result = Path.ChangeExtension(Path.Combine(_folder, FileManager.Configuration.Options.NamingOptions.MoviesheetName(_movieName)), MP4Tagger.MP4Manager.FIXED_JPG_EXTENSION);
                    if (!File.Exists(_result))
                    {
                        _result = Path.Combine(_folder, FileManager.Configuration.Options.NamingOptions.MoviesheetName(_movieName));
                    }
                    return new ImageLockFixConverter().Convert(_result, typeof(ImageSource), parameter, CultureInfo.InvariantCulture);
                }
            }

            if (_result != null && !File.Exists(_result))
            {
                _result = null;
            }

            return _result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        #endregion
    }

    public class NotBoolToVisibilityConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool _val = !(bool)value;
            return new BooleanToVisibilityConverter().Convert(_val, typeof(Visibility), null, CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }

    public class ImageLockFixConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            BitmapImage _img = null;
            try
            {
                string _path = value as string;
                if (!string.IsNullOrEmpty(_path))
                {
                    _img = new BitmapImage();
                    Uri _imageSource = new Uri(_path, UriKind.RelativeOrAbsolute);
                    if (!_imageSource.IsAbsoluteUri || _imageSource.Scheme == "http")
                    {
                        return _path;
                    }

                    if (File.Exists(_path))
                    {
                        try
                        {
                            int _decodeWidth = -1;
                            if (Int32.TryParse(parameter as string, out _decodeWidth))
                            {
                                _img = Helpers.LoadImage(_path, _decodeWidth);
                            }
                            else
                            {
                                _img = Helpers.LoadImage(_path, -1);
                            }
                        }
                        catch
                        {
                            return DependencyProperty.UnsetValue;
                        }
                    }
                    else
                    {
                        return DependencyProperty.UnsetValue;
                    }
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.Error(ex.Message);
            }
            return _img ?? DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        #endregion
    }

    //TO BE DELETED
    //public class ImageDPIFixConverter : IValueConverter
    //{
    //    #region IValueConverter Members

    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        string _path = value as string;
    //        if (!string.IsNullOrEmpty(_path))
    //        {
    //            BitmapImage _img = new BitmapImage();
    //            _img.BeginInit();
    //            _img.UriSource = new Uri(_path, UriKind.RelativeOrAbsolute);
    //            _img.EndInit();
    //            return _img;
    //        }
    //        else
    //        {
    //            return value;
    //        }
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return value;
    //    }

    //    #endregion
    //}

    public class StringListCommaConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<string> _list = value as List<string>;
            if (_list != null && _list.Count > 0)
            {
                return string.Join(",", _list.ToArray());
            }
            else
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string _input = value as string;
            if (!string.IsNullOrEmpty(_input))
            {
                List<string> _data = _input.Split(',', '|', ';').ToTrimmedList();
                return _data;
            }
            else
            {
                return null;
            }
        }

        #endregion
    }

    public class NullToBoolConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // If Value is not null, return true
            if (parameter != null)
            { }

            //if (value is string && string.IsNullOrEmpty((string)value))
            if (value is string && value == null)
                return false;
            else if (value == null)
                return false;
            else
                return true;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public class Bool2StyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return System.Windows.FontStyles.Italic;
            }
            return System.Windows.FontStyles.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class Bool2WeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return System.Windows.FontWeights.Bold;
            }
            return System.Windows.FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class SortOption2BoolConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ThumbGen.UserOptions.SortOption _option = (ThumbGen.UserOptions.SortOption)value;
            ThumbGen.UserOptions.SortOption _current = (ThumbGen.UserOptions.SortOption)Enum.Parse(typeof(ThumbGen.UserOptions.SortOption), parameter as string);
            return _option == _current;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ThumbGen.UserOptions.SortOption _current = (ThumbGen.UserOptions.SortOption)Enum.Parse(typeof(ThumbGen.UserOptions.SortOption), parameter as string);
            return (bool)value ? _current : ThumbGen.UserOptions.SortOption.Alphabetically;
        }
    }

    public class AllCollectorsConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool _all = (bool)values[0];
            bool _cover = (bool)values[1];
            bool _info = (bool)values[2];
            bool _selected = (bool)values[3];

            return (_all || _cover || _info || _selected) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
