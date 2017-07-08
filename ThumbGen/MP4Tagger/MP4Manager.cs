using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace ThumbGen.MP4Tagger
{
    internal class MP4Manager
    {
        public static string DEST_FILE_NAME = "zzz.mp4";
        public static string TEMPLATE_NAME = "template.mp4";
        public static string FIXED_JPG_EXTENSION = ".JPG_TG";

        public static string TemplateTempPath;

        private static bool GenerateThumbnail(string movieFilePath, byte[] thumbnailData)
        {
            bool _result = false;

            if (File.Exists(movieFilePath) && thumbnailData.Length != 0 &&
                !string.IsNullOrEmpty(TemplateTempPath) && File.Exists(TemplateTempPath))
            {
                string _templateFilename = Path.Combine(Path.GetDirectoryName(movieFilePath), DEST_FILE_NAME);

                try
                {
                    File.Copy(TemplateTempPath, _templateFilename, true);
                    _result = true;
                }
                catch { }

                if (_result)
                {
                    _result = false;
                    try
                    {
                        // open file
                        IntPtr ptr = MP4V2Wrapper.MP4Modify(ref _templateFilename, 0, 0);
                        if (ptr != IntPtr.Zero)
                        {
                            VBMP4Tags vb = new VBMP4Tags();

                            IntPtr tags = MP4V2Wrapper.VBMP4TagsAlloc();

                            MP4V2Wrapper.VBMP4TagsFetch(ref vb, ptr);

                            MP4V2Wrapper.MP4TagsRemoveArtwork(tags, 0);

                            MP4V2Wrapper.VBMP4SetCoverArt(thumbnailData, thumbnailData.Length);

                            MP4V2Wrapper.MP4TagsStore(tags, ptr);

                            MP4V2Wrapper.VBMP4TagsFree(ref vb);

                            MP4V2Wrapper.MP4Close(ptr);

                            _result = true;
                        }

                    }
                    catch { }
                }
            }

            return _result;
        }

        private static bool GenerateThumbnail(string movieFilePath, string thumbnailFilePath)
        {
            bool _result = false;

            if (File.Exists(movieFilePath) && File.Exists(thumbnailFilePath))
            {
                byte[] _buff;
                int _bytesRead = 0;

                using (FileStream _fs = new FileStream(thumbnailFilePath, FileMode.Open, FileAccess.Read))
                {
                    _buff = new byte[_fs.Length];
                    _bytesRead = _fs.Read(_buff, 0, (int)_fs.Length);
                }
                if (_bytesRead != 0)
                {
                    _result = GenerateThumbnail(movieFilePath, _buff);
                }
            }

            return _result;
        }

        private static Stream GetEmbeddedFile(string assemblyName, string fileName)
        {
            try
            {
                System.Reflection.Assembly a = System.Reflection.Assembly.Load(assemblyName);
                Stream str = a.GetManifestResourceStream(string.Format("{0}.MP4Tagger.{1}", assemblyName, fileName));

                if (str == null)
                    throw new Exception("Could not locate embedded resource '" + fileName + "' in assembly '" + assemblyName + "'");
                return str;
            }
            catch (Exception e)
            {
                throw new Exception(assemblyName + ": " + e.Message);
            }
        }

        private static Stream GetEmbeddedTemplate()
        {
            return GetEmbeddedFile("ThumbGen", TEMPLATE_NAME);
        }

        public static void Prepare()
        {
            GetTemplateToTempFolder();
        }

        public static void ClearGarbage()
        {
            if (!string.IsNullOrEmpty(TemplateTempPath) && File.Exists(TemplateTempPath))
            {
                try
                {
                    File.Delete(TemplateTempPath);
                }
                catch { }
            }
        }

        private static void GetTemplateToTempFolder()
        {
            string _path = Path.GetTempFileName();

            Stream _template = GetEmbeddedTemplate();
            if (_template != null && _template.CanSeek && _template.CanRead)
            {
                using (FileStream _fs = new FileStream(_path, FileMode.Create, FileAccess.ReadWrite))
                {
                    _template.Position = 0;
                    _template.CopyTo(_fs);
                }
            }
            if (new FileInfo(_path).Length != 0)
            {
                TemplateTempPath = _path;
            }
            else
            {
                try
                {
                    File.Delete(_path);
                }
                catch { }
                TemplateTempPath = null;
            }
        }

        private static bool CopyTemplateToDestination(string targetMovieFilePath)
        {
            bool _result = false;
            if (!string.IsNullOrEmpty(TemplateTempPath) && File.Exists(TemplateTempPath) && File.Exists(targetMovieFilePath))
            {
                try
                {
                    File.Copy(TemplateTempPath, Path.Combine(Path.GetDirectoryName(targetMovieFilePath), DEST_FILE_NAME), true);
                    _result = true;
                }
                catch { }
            }
            return _result;
        }

        public static void ApplyBatchFix(IList<FileInfo> movieFiles)
        {
            if (!FileManager.DisableKhedasFix)
            {
                foreach (FileInfo _file in movieFiles)
                {
                    string _jpgFile = null;
                    string _folderJpg = Path.Combine(_file.DirectoryName, FileManager.Configuration.Options.NamingOptions.FolderjpgName(_file.FullName));
                    if (File.Exists(_folderJpg))
                    {
                        _jpgFile = _folderJpg;
                    }
                    else
                    {
                        _jpgFile = Path.ChangeExtension(_file.FullName,FileManager.Configuration.Options.NamingOptions.ThumbnailExtension);
                    }
                    if (!string.IsNullOrEmpty(_jpgFile) && File.Exists(_jpgFile))
                    {
                        GenerateThumbnail(_file.FullName, _jpgFile);
                    }
                    // rename all *.jpg to *.jpg_tg - SHOULD NOT BE ALL, rename only the file having same name as the movie
                    //string[] _jpegs = Directory.GetFiles(_file.DirectoryName, "*" + BaseCollector.TARGET_IMAGE_EXTENSION, SearchOption.TopDirectoryOnly);
                    //if (_jpegs != null && _jpegs.Length != 0)
                    //{
                    //    foreach (string _jpeg in _jpegs)
                    //    {
                    //        try
                    //        {
                    //            File.Move(_jpeg, Path.ChangeExtension(_jpeg, FIXED_JPG_EXTENSION));
                    //        }
                    //        catch { }
                    //    }
                    //}
                    try
                    {
                        File.Move(Path.ChangeExtension(_file.FullName, FileManager.Configuration.Options.NamingOptions.ThumbnailExtension),
                                  Path.ChangeExtension(_file.FullName, FIXED_JPG_EXTENSION));
                    }
                    catch { }
                    try
                    {
                        File.Move(Path.Combine(_file.DirectoryName, FileManager.Configuration.Options.NamingOptions.FolderjpgName(_file.FullName)),
                                  Path.ChangeExtension(Path.Combine(_file.DirectoryName, FileManager.Configuration.Options.NamingOptions.FolderjpgName(_file.FullName)), FIXED_JPG_EXTENSION));
                    }
                    catch { }
                }
            }
        }

        public static void ApplyBatchUnFix(IList<FileInfo> movieFiles)
        {
            if (!FileManager.DisableKhedasFix)
            {
                foreach (FileInfo _fileInfo in movieFiles)
                {
                    string _dir = Path.GetDirectoryName(_fileInfo.FullName);
                    string[] _jpegs = Directory.GetFiles(_dir, "*" + FIXED_JPG_EXTENSION, SearchOption.TopDirectoryOnly);
                    if (_jpegs != null && _jpegs.Length != 0)
                    {
                        foreach (string _file in _jpegs)
                        {
                            try
                            {
                                File.Move(_file, Path.ChangeExtension(_file, FileManager.Configuration.Options.NamingOptions.ThumbnailExtension));
                            }
                            catch { }
                        }
                    }

                    // remove zzz.mp4
                    if (File.Exists(Path.Combine(_dir, DEST_FILE_NAME)))
                    {
                        try
                        {
                            File.Delete(Path.Combine(_dir, DEST_FILE_NAME));
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
