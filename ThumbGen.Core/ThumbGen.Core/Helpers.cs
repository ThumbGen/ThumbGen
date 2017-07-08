using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Xml;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Media.Imaging;

namespace ThumbGen.Core
{
    public static class GenericHelpers
    {
        public static string THUMBGEN_TEMP = "_thumbgen_tmp";

        public static string GetUniqueName()
        {
            return Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
        }

        public static bool IsValidFile(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
        }

        public static string GetTemplateName(string filePath)
        {
            return string.IsNullOrEmpty(filePath) ? GetUniqueName() : Path.GetFileNameWithoutExtension(Path.GetDirectoryName(filePath));
        }

        public static bool GetEmbeddedAssembly(string assemblyName, string targetFileName, string assemblyPath)
        {
            bool _result = false;
            try
            {
                //System.Reflection.Assembly a = System.Reflection.Assembly.Load(assemblyName);
                Stream str = Assembly.GetEntryAssembly().GetManifestResourceStream(string.Format("{0}.{1}", assemblyPath, assemblyName));
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
            catch (Exception)
            {
                //throw new Exception(assemblyName + ": " + e.Message);
            }

            return _result;
        }

        public static Assembly GetEmbeddedAssembly(string name)
        {
            try
            {
                Assembly a1 = Assembly.GetExecutingAssembly();
                Stream s = a1.GetManifestResourceStream(name);
                byte[] block = new byte[s.Length];
                s.Read(block, 0, block.Length);
                Assembly a2 = Assembly.Load(block);
                return a2;
            }
            catch
            {
                return null;
            }
        }

        public static string GetValueFromXmlNode(XmlNode node)
        {
            return node != null && !string.IsNullOrEmpty(node.InnerText) ? node.InnerText : string.Empty;
        }

        public static string GetAttributeFromXmlNode(XmlNode node, string attribute)
        {
            return node != null && node.Attributes[attribute] != null ? node.Attributes[attribute].Value : string.Empty;
        }

        public static void DoEvents()
        {
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Input, new ThreadStart(delegate { }));
            }
        }

        public static string GetUniqueFilename(string extension)
        {
            string _filename = Guid.NewGuid().ToString() + extension;
            return Path.Combine(Path.Combine(Path.GetTempPath(), THUMBGEN_TEMP), _filename);
        }

        public static bool GetEmbeddedPreset(string fullfileName, string targetFileName)
        {
            bool _result = false;
            try
            {
                //System.Reflection.Assembly a = System.Reflection.Assembly.Load(assemblyName);
                Stream str = Assembly.GetEntryAssembly().GetManifestResourceStream(fullfileName);
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
                throw new Exception(fullfileName + ": " + e.Message);
            }

            return _result;
        }

        public static string MakePathRelative(string pathToMakeRelative, string pathToTemplate)
        {
            string _result = pathToMakeRelative;

            if (!string.IsNullOrEmpty(pathToTemplate))
            {
                // make the filepath relative to the path to the template
                Uri _tmpl = new Uri(pathToTemplate, UriKind.Absolute);
                Uri _cfile = new Uri(pathToMakeRelative, UriKind.Absolute);
                Uri _final = _tmpl.MakeRelativeUri(_cfile);

                string _s = Uri.UnescapeDataString(_final.OriginalString);

                _result = System.IO.Path.Combine("%PATH%", _s).Replace("/", "\\");
            }
            return _result;
        }

        public static string ExtractPresetFile(string fileName, string destFile, string embeddedName)
        {
            string _tempPath = string.IsNullOrEmpty(destFile) ? GetUniqueFilename(Path.GetExtension(fileName)) : destFile;
            if (GetEmbeddedPreset(embeddedName, _tempPath))
            {
                return _tempPath;
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
                if (File.Exists(imagePath))
                {
                    _result = new BitmapImage();

                    byte[] buffer = System.IO.File.ReadAllBytes(imagePath);
                    if (buffer == null || buffer.Length == 0)
                    {
                        return null;
                    }
                    MemoryStream ms = new MemoryStream(buffer);
                    ms.Position = 0;
                    _result.BeginInit();
                    if (decodeWidth != -1)
                    {
                        _result.DecodePixelWidth = decodeWidth;
                    }
                    _result.CacheOption = BitmapCacheOption.OnLoad;
                    _result.StreamSource = ms;
                    _result.EndInit();
                    _result.Freeze();

                    ms.Dispose();
                    ms.Close();
                    ms = null;
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


        public static Size GetImageSize(string filename)
        {
            Size _result = new Size(0, 0);
            if (File.Exists(filename))
            {
                BitmapImage _bmp = LoadImage(filename, -1);
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

    }

    public static class ExtensionMethods
    {
        public static ObservableCollection<object> ConvertAll<T>(this ObservableCollection<T> input)
        {
            if (input == null)
                return null;
            ObservableCollection<object> returnValues = new ObservableCollection<object>();
            foreach (T item in input)
                returnValues.Add(item);
            return returnValues;
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
    }
}
