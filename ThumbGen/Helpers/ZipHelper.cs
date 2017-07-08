using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Packaging;

namespace ThumbGen
{
    public static class ZipHelper
    {
        private const long BUFFER_SIZE = 4096;

        public static void AddFileToZip(string zipFilename, string fileToAdd, string sDirectory)
        {
            using (Package zip = Package.Open(zipFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                string destFilename = ".\\" + sDirectory + "\\" + Path.GetFileName(fileToAdd);
                Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                if (zip.PartExists(uri))
                {
                    zip.DeletePart(uri);
                }
                PackagePart part = zip.CreatePart(uri, "", CompressionOption.Normal);
                using (FileStream fileStream = new FileStream(fileToAdd, FileMode.Open, FileAccess.Read))
                {
                    using (Stream dest = part.GetStream())
                    {
                        CopyStream(fileStream, dest);
                    }
                }
            }
        }

        //public static void AddFileToZip(string zipFilename, string fileToAdd)
        //{
        //    using (Package zip = Package.Open(zipFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        //    {
        //        string destFilename = ".\\" + Path.GetFileName(fileToAdd);
        //        Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
        //        if (zip.PartExists(uri))
        //        {
        //            zip.DeletePart(uri);
        //        }
        //        PackagePart part = zip.CreatePart(uri, "", CompressionOption.Maximum);
        //        using (FileStream fileStream = new FileStream(fileToAdd, FileMode.Open, FileAccess.Read, FileShare.Read))
        //        {
        //            using (Stream dest = part.GetStream())
        //            {
        //                CopyStream(fileStream, dest);
        //            }
        //        }
        //    }
        //}

        public static void AddStreamPartToZip(string zipFilename, Stream streamToAdd, string destPartName)
        {
            using (Package zip = Package.Open(zipFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri(".\\" + destPartName, UriKind.Relative));
                if (zip.PartExists(uri))
                {
                    zip.DeletePart(uri);
                }
                PackagePart part = zip.CreatePart(uri, "");
                using (Stream dest = part.GetStream())
                {
                    CopyStream(streamToAdd, dest);
                }
            }
        }

        public static Stream ExtractStreamFromZip(string zipFilename, string fileToExtract)
        {
            if (IsValidFile(zipFilename))
            {
                try
                {
                    using (ZipPackage _package = (ZipPackage) Package.Open(zipFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        PackagePartCollection _packageParts = _package.GetParts();
                        foreach (PackagePart _part in _packageParts)
                        {
                            if (_part.Uri.OriginalString == '/' + fileToExtract)
                            {
                                MemoryStream _stream = new MemoryStream();
                                CopyStream(_part.GetStream(), _stream as Stream);
                                return _stream;
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Loggy.Logger.ErrorException("Loading stream from metadata.",ex);
                }
            }
            return null;
        }

        public static bool HasStream(string zipFilename, string partName)
        {
            if (IsValidFile(zipFilename))
            {
                using (ZipPackage _package = (ZipPackage)Package.Open(zipFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    PackagePartCollection _packageParts = _package.GetParts();
                    foreach (PackagePart _part in _packageParts)
                    {
                        if (_part.Uri.OriginalString == '/' + partName)
                        {
                            Stream _st = _part.GetStream();
                            return _st != null && _st.Length != 0;
                        }
                    }
                }
            }
            return false;
        }

        public static bool IsValidFile(string zipFilename)
        {
            return File.Exists(zipFilename) && new FileInfo(zipFilename).Length > 0;
        }

        public static void CopyStream(Stream inputStream, Stream outputStream)
        {
            if (outputStream == null)
            {
                return;
            }
            if (inputStream != null)
            {
                inputStream.Position = 0;
            }

            long bufferSize = inputStream.Length < BUFFER_SIZE ? inputStream.Length : BUFFER_SIZE;
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;
            long bytesWritten = 0;
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                outputStream.Write(buffer, 0, bytesRead);
                bytesWritten += bufferSize;
            }

            outputStream.Position = 0;
        }
    }
}
