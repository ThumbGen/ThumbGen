using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Web;
using System.Windows.Media;
using System.Windows;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Net;
using System.Xml;
using FileExplorer.View;
using System.Collections;
using System.Threading;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using ThumbGen.MovieSheets;

namespace ThumbGen
{
    public static class ConfigHelpers
    {
        private static string keyStr = "dfss3432dssf";

        public static string Encrypt(string strToEncrypt)
        {
            try
            {
                TripleDESCryptoServiceProvider objDESCrypto =
                    new TripleDESCryptoServiceProvider();
                MD5CryptoServiceProvider objHashMD5 = new MD5CryptoServiceProvider();
                byte[] byteHash, byteBuff;
                string strTempKey = keyStr;
                byteHash = objHashMD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strTempKey));
                objHashMD5 = null;
                objDESCrypto.Key = byteHash;
                objDESCrypto.Mode = CipherMode.ECB; //CBC, CFB
                byteBuff = ASCIIEncoding.ASCII.GetBytes(strToEncrypt);
                return Convert.ToBase64String(objDESCrypto.CreateEncryptor().
                    TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }

        public static string Decrypt(string strEncrypted)
        {
            try
            {
                TripleDESCryptoServiceProvider objDESCrypto =
                    new TripleDESCryptoServiceProvider();
                MD5CryptoServiceProvider objHashMD5 = new MD5CryptoServiceProvider();
                byte[] byteHash, byteBuff;
                string strTempKey = keyStr;
                byteHash = objHashMD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strTempKey));
                objHashMD5 = null;
                objDESCrypto.Key = byteHash;
                objDESCrypto.Mode = CipherMode.ECB; //CBC, CFB
                byteBuff = Convert.FromBase64String(strEncrypted);
                string strDecrypted = ASCIIEncoding.ASCII.GetString
                (objDESCrypto.CreateDecryptor().TransformFinalBlock
                (byteBuff, 0, byteBuff.Length));
                objDESCrypto = null;
                return strDecrypted;
            }
            catch (Exception ex)
            {
                return "Wrong Input. " + ex.Message;
            }
        }

        public static string ConstructPath(string movieFilename, string mask, string extension, bool forcePath)
        {
            if (!string.IsNullOrEmpty(movieFilename) && !string.IsNullOrEmpty(mask))
            {
                string _M = GetTokenValue("$M", movieFilename);
                string _N = GetTokenValue("$N", movieFilename);
                string _E = GetTokenValue("$E", movieFilename);
                string _F = GetTokenValue("$F", movieFilename);
                string _P = GetTokenValue("$P", movieFilename);
                string _final = mask.Replace("$M", _M).Replace("$N", _N).Replace("$E", _E).Replace("$F", _F).Replace("$P", _P).Replace(@"\\", @"\").Trim() + extension;
                try
                {
                    if (forcePath)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(_final));
                    }
                }
                catch { }
                return _final;
            }
            else
            {
                return null;
            }
        }

        public static string GetTokenValue(string token, string movieFilename)
        {
            switch (token)
            {
                case ("$M"):
                    string _resultM = Path.GetDirectoryName(movieFilename) + @"\";
                    if (FileManager.Configuration.Options.EnableSmartOutputForDVDBRAY)
                    {
                        if (Helpers.IsDVDPath(movieFilename))
                        {
                            _resultM = Helpers.GetDVDRootDirectory(movieFilename);
                        }
                        if (Helpers.IsBlurayPath(movieFilename))
                        {
                            _resultM = Helpers.GetBlurayRootDirectory(movieFilename);
                        }
                    }
                    return _resultM;

                case ("$N"):
                    string _resultN = Path.GetFileNameWithoutExtension(movieFilename);
                    if (FileManager.Configuration.Options.EnableSmartOutputForDVDBRAY)
                    {
                        if (Helpers.IsDVDPath(movieFilename))
                        {
                            _resultN = Helpers.GetMovieFolderName(Helpers.GetDVDRootDirectory(movieFilename), "");
                        }
                        if (Helpers.IsBlurayPath(movieFilename))
                        {
                            _resultN = Helpers.GetMovieFolderName(Helpers.GetBlurayRootDirectory(movieFilename), "");
                        }
                    }
                    return _resultN;

                case ("$E"):
                    return Path.GetExtension(movieFilename);

                case ("$F"):
                    string _resultF = Helpers.GetMovieFolderName(movieFilename, string.Empty);
                    if (FileManager.Configuration.Options.EnableSmartOutputForDVDBRAY)
                    {
                        if (Helpers.IsDVDPath(movieFilename))
                        {
                            _resultF = Helpers.GetMovieFolderName(Helpers.GetDVDRootDirectory(movieFilename), "");
                        }
                        if (Helpers.IsBlurayPath(movieFilename))
                        {
                            _resultF = Helpers.GetMovieFolderName(Helpers.GetBlurayRootDirectory(movieFilename), "");
                        }
                    }
                    return _resultF;
                case ("$P"):
                    string _resultP = Helpers.GetMovieParentFolderName(movieFilename, string.Empty);
                    if (FileManager.Configuration.Options.EnableSmartOutputForDVDBRAY)
                    {
                        if (Helpers.IsDVDPath(movieFilename))
                        {
                            _resultP = Helpers.GetMovieParentFolderName(Helpers.GetDVDRootDirectory(movieFilename), "");
                        }
                        if (Helpers.IsBlurayPath(movieFilename))
                        {
                            _resultP = Helpers.GetMovieParentFolderName(Helpers.GetBlurayRootDirectory(movieFilename), "");
                        }
                    }
                    return _resultP;
                default:
                    return null;
            }
        }

        public static bool CheckIfFileNeedsCreation(string moviePath, string mask, out string coverPath)
        {
            bool _result = false;
            coverPath = null;

            if (!string.IsNullOrEmpty(moviePath) && !string.IsNullOrEmpty(mask))
            {
                coverPath = ConfigHelpers.ConstructPath(moviePath, mask, "", false);
                Uri _uri = new Uri(coverPath, UriKind.RelativeOrAbsolute);
                if (!_uri.IsAbsoluteUri)
                {
                    coverPath = Path.Combine(Path.GetDirectoryName(moviePath), mask);
                }
                _result = true;
            }

            return _result;
        }

        public static bool CheckIfFileExists(string moviePath, string mask, out string coverPath)
        {
            bool _result = false;

            coverPath = ConfigHelpers.ConstructPath(moviePath, mask, "", false);
            Uri _uri = new Uri(coverPath, UriKind.RelativeOrAbsolute);
            if (!_uri.IsAbsoluteUri)
            {
                coverPath = Path.Combine(Path.GetDirectoryName(moviePath), mask);
            }
            _result = File.Exists(coverPath);

            return _result;
        }
    }

    public class BaseNotifyPropertyChanged : INotifyPropertyChanged
    {
        public BaseNotifyPropertyChanged()
        { }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion
    }

    [Serializable]
    public enum IMDBMovieInfoBehaviour
    {
        DoNotUseIMDBMovieInfo,
        FillMissingDataFromIMDB,
        UseOnlyIMDBMovieInfo
    }

    public class PredefinedNames
    {
        public string DisplayName { get; set; }
        public string Mask { get; set; }
        public string Extension { get; set; }
        public string Tooltip { get; set; }

        public PredefinedNames(string dname, string mask, string ext, string tt)
        {
            DisplayName = dname;
            Mask = mask;
            Extension = ext;
            Tooltip = tt;
        }
    }

    [TypeConverter(typeof(LocalizedExportBackConverter))]
    public enum ExportBackdropTypes
    {
        DoNotExportImages = 0,
        UseInternetLinks = 1,
        UseLocalExportedImages = 2,
        UseGeneratedMoviesheets = 3
    }

    public class LocalizedExportBackConverter : ResourceEnumConverter
    {
        public LocalizedExportBackConverter(Type type)
            : base(type, Properties.Resources.ResourceManager)
        {
        }
    }

    /* WDLXTV Live
     * 
wd_tv.jpg - proper name for directory moviesheet
folder_sheet.jpg - proper name for directory moviesheet
movie.mkv_sheet.jpg - generic movie sheet
movie.mkv_sheet.wall.jpg - wall mode specific moviesheet
movie.mkv_sheet.std.jpg - std mode specific moviesheet
movie.mkv_sheet.sheet.jpg - sheet mode specific moviesheet


*_sheet.jpg - generic sheet for every mode
*_sheet.wall.jpg - wall mode specific sheet
*_sheet.std.jpg - std mode specific sheet
*_sheet.sheet.jpg - sheet mode specific sheet
folder_sheet.jpg - generic folder sheet ( no mode options currently)
wd_tv.jpg - generic folder sheet ( no mode options currently)
     */

    [Serializable]
    public class NamingConventions : BaseNotifyPropertyChanged
    {
        public static Dictionary<string, PredefinedNames> PredefinedNamesDict = new Dictionary<string, PredefinedNames>()
        {
            {"0", new PredefinedNames("Custom", "", ".jpg", "You are using a custom path/format")},
            {"5", new PredefinedNames("WDTV1-LaurentG Generic folder sheet (wd_tv.jpg)", @"$M\wd_tv", ".jpg", "Generic folder sheet. Recommended for each movie in own folder")},
            {"7", new PredefinedNames("WDTV1-Titi Generic folder sheet (wd_tv.jpg)", @"$M\wd_tv", ".jpg", "Generic folder sheet. Recommended for each movie in own folder")},
            {"10", new PredefinedNames("WDTV1-Titi Movie sheet (<moviename.ext>_sheet.jpg)", @"$M\$N$E_sheet", ".jpg", "Sheet is dynamically changing when browsing movies")},
            {"15", new PredefinedNames("WDTV1-Titi Folder sheet(<foldername>_sheet.jpg)", @"$M\$F_sheet", ".jpg", "Sheet is displayed for foldername")},
            {"20", new PredefinedNames("WDTV1-Titi Generic folder sheet (folder_sheet.jpg)", @"$M\folder_sheet", ".jpg", "There is a unique sheet for all movies inside a folder")},
            {"25", new PredefinedNames("WDLXTV-Live Generic folder sheet (wd_tv.jpg)", @"$M\wd_tv", ".jpg", "Generic folder sheet (no mode options currently)")},
            {"30", new PredefinedNames("WDLXTV-Live Generic folder sheet (folder.jpg_sheet.jpg)", @"$M\folder.jpg_sheet", ".jpg", "Generic folder sheet (no mode options currently)")},
            {"35", new PredefinedNames("WDLXTV-Live Generic dynamic sheet/all modes (<moviename.ext>_sheet.jpg)", @"$M\$N$E_sheet", ".jpg", "Generic dynamic sheet for all modes")},
            {"40", new PredefinedNames("WDLXTV-Live Wall mode specific sheet (<moviename.ext>_sheet.wall.jpg)", @"$M\$N$E_sheet.wall", ".jpg", "Wall mode specific sheet")},
            {"45", new PredefinedNames("WDLXTV-Live Std mode specific sheet (<moviename.ext>_sheet.std.jpg)", @"$M\$N$E_sheet.std", ".jpg", "Std mode specific sheet")},
            {"50", new PredefinedNames("WDLXTV-Live Sheet mode specific sheet (<moviename.ext>_sheet.sheet.jpg)", @"$M\$N$E_sheet.sheet", ".jpg", "Sheet mode specific sheet")},
            {"55", new PredefinedNames("WDLXTV-Live Generic folder sheet used in all modes (<foldername>_sheet.jpg)", @"$M\$F_sheet", ".jpg", "Generic folder sheet used in all modes")},
            {"80", new PredefinedNames("ACRyan Movie sheet (0001.jpg)", @"$M\0001", ".jpg", "Movie/episode sheet for POHD (each movie file in own folder)")},
            {"85", new PredefinedNames("ACRyan Season sheet (0001.jpg)", @"$M\..\0001", ".jpg", "Season sheet for POHD (each movie file in own folder)")},
            {"90", new PredefinedNames("ACRyan Series sheet (0001.jpg)", @"$M\..\..\0001", ".jpg", "Series sheet for POHD (each movie file in own folder)")}
        };

        public static List<string> PredefinedInfoExtensions = new List<string>()
        {
            ".nfo", ".tvixie", ".txt", ".xml"
        };

        public static string DEFAULT_THUMBNAIL_EXTENSION = ".jpg";
        public static string DEFAULT_FOLDERJPG_EXTENSION = ".jpg";
        public static string DEFAULT_MOVIEINFO_EXTENSION = ".nfo";
        public static string DEFAULT_MOVIESHEET_EXTENSION = ".jpg";

        public static string DEFAULT_THUMBNAIL_MASK = @"$M\$N";
        public static string DEFAULT_FOLDERJPG_MASK = @"$M\folder";
        public static string DEFAULT_MOVIEINFO_MASK = @"$M\$N";
        public static string DEFAULT_MOVIESHEET_MASK = @"$M\wd_tv";
        public static string DEFAULT_MSHEET_MOVIESHEET_MASK = @"$M\$N$E_sheet";
        public static string DEFAULT_MSHEET_MOVIESHEET_FOR_FOLDER_MASK = @"$M\$F_sheet";
        public static string DEFAULT_MSHEET_MOVIESHEET_FOR_PARENTFOLDER_MASK = @"$M\..\$P_sheet";
        public static string DEFAULT_MOVIESHEET_METADATA_MASK = @"$M\$N$E";
        public static string DEFAULT_PARENTFOLDER_METADATA_MASK = @"$M\..\$P";

        public static string DEFAULT_DUMMY_FILE = @"$M\$N$E_sheet$E";

        private string GetFormattedItem(string movieFilename, string mask, string extension)
        {
            string _result = string.Empty;

            if (!string.IsNullOrEmpty(movieFilename) && !string.IsNullOrEmpty(mask))
            {

                int _start = mask.LastIndexOf(@"\");
                string _s = mask.Substring(_start, mask.Length - _start);

                _result = _s;

                Hashtable _hashtable = new Hashtable();
                _hashtable["$M"] = ConfigHelpers.GetTokenValue("$M", movieFilename);
                _hashtable["$N"] = ConfigHelpers.GetTokenValue("$N", movieFilename);
                _hashtable["$E"] = ConfigHelpers.GetTokenValue("$E", movieFilename);
                _hashtable["$F"] = ConfigHelpers.GetTokenValue("$F", movieFilename);
                _hashtable["$P"] = ConfigHelpers.GetTokenValue("$P", movieFilename);
                foreach (DictionaryEntry _entry in _hashtable)
                {
                    if (_entry.Key != null && _entry.Value != null)
                    {
                        _result = _result.Replace(_entry.Key.ToString(), _entry.Value.ToString());
                    }
                }
                _result = _result.Trim(new char[] { '\\' }) + extension;
            }

            return _result;
        }

        public string FolderjpgName(string movieFilename)
        {
            //return Path.ChangeExtension(this.FolderjpgMask.Replace("$M", "").Replace("$N", "").Replace("$E", "").Replace("$F", "").Trim(new char[] { '\\', ' ' }), this.FolderjpgExtension);
            return GetFormattedItem(movieFilename, this.FolderjpgMask, this.FolderjpgExtension);
        }

        public string MoviesheetName(string movieFilename)
        {
            return GetFormattedItem(movieFilename, this.MoviesheetMask, this.MoviesheetExtension);
        }

        private string m_SelectedPredefinedItem;
        public string SelectedPredefinedItem
        {
            get
            {
                return m_SelectedPredefinedItem;
            }
            set
            {
                m_SelectedPredefinedItem = value;
                NotifyPropertyChanged("SelectedPredefinedItem");
            }
        }

        private string m_SelectedExtraPredefinedItem;
        public string SelectedExtraPredefinedItem
        {
            get
            {
                return m_SelectedExtraPredefinedItem;
            }
            set
            {
                m_SelectedExtraPredefinedItem = value;
                NotifyPropertyChanged("SelectedExtraPredefinedItem");
            }
        }

        private string m_ThumbnailMask;
        public string ThumbnailMask
        {
            get
            {
                return m_ThumbnailMask;
            }
            set
            {
                m_ThumbnailMask = value;
                NotifyPropertyChanged("ThumbnailMask");
            }
        }

        private string m_ThumbnailExtension;
        public string ThumbnailExtension
        {
            get
            {
                return m_ThumbnailExtension;
            }
            set
            {
                m_ThumbnailExtension = value;
                NotifyPropertyChanged("ThumbnailExtension");
            }
        }

        private string m_FolderjpgMask;
        public string FolderjpgMask
        {
            get
            {
                return m_FolderjpgMask;
            }
            set
            {
                m_FolderjpgMask = value;
                NotifyPropertyChanged("FolderjpgMask");
            }
        }

        private string m_FolderjpgExtension;
        public string FolderjpgExtension
        {
            get
            {
                return m_FolderjpgExtension;
            }
            set
            {
                m_FolderjpgExtension = value;
                NotifyPropertyChanged("FolderjpgExtension");
            }
        }

        private string m_MovieinfoMask;
        public string MovieinfoMask
        {
            get
            {
                return m_MovieinfoMask;
            }
            set
            {
                m_MovieinfoMask = value;
                NotifyPropertyChanged("MovieinfoMask");
            }
        }

        private string m_MovieinfoExtension;
        public string MovieinfoExtension
        {
            get
            {
                return m_MovieinfoExtension;
            }
            set
            {
                m_MovieinfoExtension = value;
                NotifyPropertyChanged("MovieinfoExtension");
            }
        }

        private string m_MovieinfoExportMask;
        public string MovieinfoExportMask
        {
            get
            {
                return m_MovieinfoExportMask;
            }
            set
            {
                m_MovieinfoExportMask = value;
                NotifyPropertyChanged("MovieinfoExportMask");
            }
        }

        private string m_MovieinfoExportExtension;
        public string MovieinfoExportExtension
        {
            get
            {
                return m_MovieinfoExportExtension;
            }
            set
            {
                m_MovieinfoExportExtension = value;
                NotifyPropertyChanged("MovieinfoExportExtension");
            }
        }


        private string m_MoviesheetMask;
        public string MoviesheetMask
        {
            get
            {
                return m_MoviesheetMask;
            }
            set
            {
                m_MoviesheetMask = value;
                NotifyPropertyChanged("MoviesheetMask");
            }
        }

        private string m_MoviesheetMetadataMask;
        public string MoviesheetMetadataMask
        {
            get
            {
                return m_MoviesheetMetadataMask;
            }
            set
            {
                m_MoviesheetMetadataMask = value;
                NotifyPropertyChanged("MoviesheetMetadataMask");
            }
        }

        private string m_ParentFolderMetadataMask;
        public string ParentFolderMetadataMask
        {
            get
            {
                return m_ParentFolderMetadataMask;
            }
            set
            {
                m_ParentFolderMetadataMask = value;
                NotifyPropertyChanged("ParentFolderMetadataMask");
            }
        }

        private string m_MoviesheetForFolderMask;
        public string MoviesheetForFolderMask
        {
            get
            {
                return m_MoviesheetForFolderMask;
            }
            set
            {
                m_MoviesheetForFolderMask = value;
                NotifyPropertyChanged("MoviesheetForFolderMask");
            }
        }

        private string m_MoviesheetForParentFolderMask;
        public string MoviesheetForParentFolderMask
        {
            get
            {
                return m_MoviesheetForParentFolderMask;
            }
            set
            {
                m_MoviesheetForParentFolderMask = value;
                NotifyPropertyChanged("MoviesheetForParentFolderMask");
            }
        }

        private string m_DummyFileMask;
        public string DummyFileMask
        {
            get
            {
                return m_DummyFileMask;
            }
            set
            {
                m_DummyFileMask = value;
                NotifyPropertyChanged("DummyFileMask");
            }
        }

        private string m_MoviesheetExtension;
        public string MoviesheetExtension
        {
            get
            {
                return m_MoviesheetExtension;
            }
            set
            {
                m_MoviesheetExtension = value;
                NotifyPropertyChanged("MoviesheetExtension");
            }
        }

        private string m_MoviesheetForFolderExtension;
        public string MoviesheetForFolderExtension
        {
            get
            {
                return m_MoviesheetForFolderExtension;
            }
            set
            {
                m_MoviesheetForFolderExtension = value;
                NotifyPropertyChanged("MoviesheetForFolderExtension");
            }
        }

        private string m_MoviesheetForParentFolderExtension;
        public string MoviesheetForParentFolderExtension
        {
            get
            {
                return m_MoviesheetForParentFolderExtension;
            }
            set
            {
                m_MoviesheetForParentFolderExtension = value;
                NotifyPropertyChanged("MoviesheetForParentFolderExtension");
            }
        }

        private ExportBackdropTypes m_ExportBackdropType = ExportBackdropTypes.UseInternetLinks;
        public ExportBackdropTypes ExportBackdropType
        {
            get
            {
                return m_ExportBackdropType;
            }
            set
            {
                m_ExportBackdropType = value;
                NotifyPropertyChanged("ExportBackdropType");
            }
        }

        private string m_MoviesheetMetadataExtension;
        public string MoviesheetMetadataExtension
        {
            get
            {
                return m_MoviesheetMetadataExtension;
            }
            set
            {
                m_MoviesheetMetadataExtension = value;
                NotifyPropertyChanged("MoviesheetMetadataExtension");
            }
        }

        public NamingConventions()
        {
            ThumbnailMask = DEFAULT_THUMBNAIL_MASK;
            ThumbnailExtension = DEFAULT_THUMBNAIL_EXTENSION;

            FolderjpgMask = DEFAULT_FOLDERJPG_MASK;
            FolderjpgExtension = DEFAULT_FOLDERJPG_EXTENSION;

            MovieinfoMask = DEFAULT_MOVIEINFO_MASK;
            MovieinfoExtension = DEFAULT_MOVIEINFO_EXTENSION;

            MovieinfoExportMask = DEFAULT_MOVIEINFO_MASK;
            MovieinfoExportExtension = DEFAULT_MOVIEINFO_EXTENSION;

            MoviesheetMask = DEFAULT_MOVIESHEET_MASK;
            MoviesheetExtension = DEFAULT_THUMBNAIL_EXTENSION;

            MoviesheetForFolderMask = DEFAULT_MSHEET_MOVIESHEET_FOR_FOLDER_MASK;
            MoviesheetForFolderExtension = DEFAULT_THUMBNAIL_EXTENSION;

            MoviesheetForParentFolderMask = DEFAULT_MSHEET_MOVIESHEET_FOR_PARENTFOLDER_MASK;
            MoviesheetForParentFolderExtension = DEFAULT_THUMBNAIL_EXTENSION;

            MoviesheetMetadataExtension = MoviesheetsUpdateManager.EXTENSION;
            MoviesheetMetadataMask = DEFAULT_MOVIESHEET_METADATA_MASK;

            ParentFolderMetadataMask = DEFAULT_PARENTFOLDER_METADATA_MASK;

            DummyFileMask = DEFAULT_DUMMY_FILE;

            ExportBackdropType = ThumbGen.ExportBackdropTypes.UseInternetLinks;

            SelectedPredefinedItem = "0";
            SelectedExtraPredefinedItem = "0";
        }
    }

    [Serializable]
    public class UserOptions : BaseNotifyPropertyChanged
    {
        public static string DEFAULT_DVD_FILTER = "vts_((?!01_0).)*\\.ifo|video_ts.ifo";

        private bool m_AutoCheckUpdates;
        public bool AutoCheckUpdates
        {
            get
            {
                return m_AutoCheckUpdates;
            }
            set
            {
                m_AutoCheckUpdates = value;
                NotifyPropertyChanged("AutoCheckUpdates");
            }
        }

        private bool m_RetrieveBannersAsBackdrops;
        public bool RetrieveBannersAsBackdrops
        {
            get
            {
                return m_RetrieveBannersAsBackdrops;
            }
            set
            {
                m_RetrieveBannersAsBackdrops = value;
                NotifyPropertyChanged("RetrieveBannersAsBackdrops");
            }
        }

        private bool m_RetrieveEpisodeScreenshots;
        public bool RetrieveEpisodeScreenshots
        {
            get
            {
                return m_RetrieveEpisodeScreenshots;
            }
            set
            {
                m_RetrieveEpisodeScreenshots = value;
                NotifyPropertyChanged("RetrieveEpisodeScreenshots");
            }
        }

        private bool m_GetBannerAsFanart2;
        public bool GetBannerAsFanart2
        {
            get
            {
                return m_GetBannerAsFanart2;
            }
            set
            {
                m_GetBannerAsFanart2 = value;
                NotifyPropertyChanged("GetBannerAsFanart2");
            }
        }

        private string m_BatchAutoMask;
        public string BatchAutoMask
        {
            get
            {
                return m_BatchAutoMask;
            }
            set
            {
                m_BatchAutoMask = value;
                NotifyPropertyChanged("BatchAutoMask");
            }
        }

        private string m_LastSelectedFolder;
        public string LastSelectedFolder
        {
            get
            {
                return HttpUtility.HtmlDecode(m_LastSelectedFolder);
            }
            set
            {
                m_LastSelectedFolder = HttpUtility.HtmlEncode(value);
                NotifyPropertyChanged("LastSelectedFolder");
            }
        }

        private bool m_DisableSearch;
        public bool DisableSearch
        {
            get
            {
                return m_DisableSearch;
            }
            set
            {
                m_DisableSearch = value;
                NotifyPropertyChanged("DisableSearch");
            }
        }

        private bool m_DisableMediaInfoProcessing;
        public bool DisableMediaInfoProcessing
        {
            get
            {
                return m_DisableMediaInfoProcessing;
            }
            set
            {
                m_DisableMediaInfoProcessing = value;
                NotifyPropertyChanged("DisableMediaInfoProcessing");
            }
        }

        private bool m_OverwriteExistingThumbs;
        public bool OverwriteExistingThumbs
        {
            get
            {
                return m_OverwriteExistingThumbs;
            }
            set
            {
                m_OverwriteExistingThumbs = value;
                NotifyPropertyChanged("OverwriteExistingThumbs");
            }
        }

        private bool m_RecurseFolders;
        public bool RecurseFolders
        {
            get
            {
                return m_RecurseFolders;
            }
            set
            {
                m_RecurseFolders = value;
                NotifyPropertyChanged("RecurseFolders");
            }
        }

        private bool m_AutogenerateFolderJpg;
        public bool AutogenerateFolderJpg
        {
            get
            {
                return m_AutogenerateFolderJpg;
            }
            set
            {
                m_AutogenerateFolderJpg = value;
                NotifyPropertyChanged("AutogenerateFolderJpg");
            }
        }

        private bool m_AutogenerateThumbnail;
        public bool AutogenerateThumbnail
        {
            get
            {
                return m_AutogenerateThumbnail;
            }
            set
            {
                m_AutogenerateThumbnail = value;
                NotifyPropertyChanged("AutogenerateThumbnail");
            }
        }

        private bool m_AutogenerateMovieSheet;
        public bool AutogenerateMovieSheet
        {
            get
            {
                return m_AutogenerateMovieSheet;
            }
            set
            {
                m_AutogenerateMovieSheet = value;
                NotifyPropertyChanged("AutogenerateMovieSheet");
            }
        }

        private bool m_AutogenerateMoviesheetForFolder;
        public bool AutogenerateMoviesheetForFolder
        {
            get
            {
                return m_AutogenerateMoviesheetForFolder;
            }
            set
            {
                m_AutogenerateMoviesheetForFolder = value;
                NotifyPropertyChanged("AutogenerateMoviesheetForFolder");
            }
        }

        private bool m_AutogenerateMovieInfo;
        public bool AutogenerateMovieInfo
        {
            get
            {
                return m_AutogenerateMovieInfo;
            }
            set
            {
                m_AutogenerateMovieInfo = value;
                NotifyPropertyChanged("AutogenerateMovieInfo");
            }
        }

        private bool m_AutogenerateMoviesheetMetadata;
        public bool AutogenerateMoviesheetMetadata
        {
            get
            {
                return m_AutogenerateMoviesheetMetadata;
            }
            set
            {
                m_AutogenerateMoviesheetMetadata = value;
                NotifyPropertyChanged("AutogenerateMoviesheetMetadata");
            }
        }

        private bool m_GenerateParentFolderMetadata;
        public bool GenerateParentFolderMetadata
        {
            get
            {
                return m_GenerateParentFolderMetadata;
            }
            set
            {
                m_GenerateParentFolderMetadata = value;
                NotifyPropertyChanged("GenerateParentFolderMetadata");
            }
        }

        private bool m_AutogenerateMoviesheetForParentFolder;
        public bool AutogenerateMoviesheetForParentFolder
        {
            get
            {
                return m_AutogenerateMoviesheetForParentFolder;
            }
            set
            {
                m_AutogenerateMoviesheetForParentFolder = value;
                NotifyPropertyChanged("AutogenerateMoviesheetForParentFolder");
            }
        }

        private bool m_UseIMDbIdWherePossible;
        public bool UseIMDbIdWherePossible
        {
            get
            {
                return m_UseIMDbIdWherePossible;
            }
            set
            {
                m_UseIMDbIdWherePossible = value;
                NotifyPropertyChanged("UseIMDbIdWherePossible");
            }
        }

        private bool m_UseMovieHashWherePossible;
        public bool UseMovieHashWherePossible
        {
            get
            {
                return m_UseMovieHashWherePossible;
            }
            set
            {
                m_UseMovieHashWherePossible = value;
                NotifyPropertyChanged("UseMovieHashWherePossible");
            }
        }

        private bool m_PromptBeforeSearch;
        public bool PromptBeforeSearch
        {
            get
            {
                return m_PromptBeforeSearch;
            }
            set
            {
                m_PromptBeforeSearch = value;
                NotifyPropertyChanged("PromptBeforeSearch");
            }
        }

        private bool m_UseFolderNamesForDetection;
        public bool UseFolderNamesForDetection
        {
            get
            {
                return m_UseFolderNamesForDetection;
            }
            set
            {
                m_UseFolderNamesForDetection = value;
                NotifyPropertyChanged("UseFolderNamesForDetection");
            }
        }

        private bool m_UpdateIMDbRating;
        public bool UpdateIMDbRating
        {
            get
            {
                return m_UpdateIMDbRating;
            }
            set
            {
                m_UpdateIMDbRating = value;
                NotifyPropertyChanged("UpdateIMDbRating");
            }
        }

        private bool m_UseBlacklist;
        public bool UseBlacklist
        {
            get
            {
                return m_UseBlacklist;
            }
            set
            {
                m_UseBlacklist = value;
                NotifyPropertyChanged("UseBlacklist");
            }
        }

        private bool m_SwitchOffInternalNoiseRemover;
        public bool SwitchOffInternalNoiseRemover
        {
            get
            {
                return m_SwitchOffInternalNoiseRemover;
            }
            set
            {
                m_SwitchOffInternalNoiseRemover = value;
                NotifyPropertyChanged("SwitchOffInternalNoiseRemover");
            }
        }

        private string m_BlackList;
        public string Blacklist
        {
            get
            {
                return HttpUtility.HtmlDecode(m_BlackList);
            }

            set
            {
                m_BlackList = HttpUtility.HtmlEncode(value);
                NotifyPropertyChanged("Blacklist");
            }
        }

        private string m_Collectors;
        public string Collectors
        {
            get
            {
                return m_Collectors;
            }
            set
            {
                m_Collectors = value;
                NotifyPropertyChanged("Collectors");
            }
        }

        private string m_PreferedInfoCollector;
        public string PreferedInfoCollector
        {
            get
            {
                return m_PreferedInfoCollector;
            }
            set
            {
                m_PreferedInfoCollector = value;
                NotifyPropertyChanged("PreferedInfoCollector");
            }
        }

        private string m_PreferedCoverCollector;
        public string PreferedCoverCollector
        {
            get
            {
                return m_PreferedCoverCollector;
            }
            set
            {
                m_PreferedCoverCollector = value;
                NotifyPropertyChanged("PreferedCoverCollector");
            }
        }

        private PreviewType m_PreviewType;
        public PreviewType PreviewType
        {
            get
            {
                return m_PreviewType;
            }
            set
            {
                m_PreviewType = value;
                NotifyPropertyChanged("PreviewType");
            }
        }

        private string m_CustomMovieExtensions;
        public string CustomMovieExtensions
        {
            get
            {
                return HttpUtility.HtmlDecode(m_CustomMovieExtensions);
            }

            set
            {
                m_CustomMovieExtensions = HttpUtility.HtmlEncode(value);
                NotifyPropertyChanged("CustomMovieExtensions");
            }
        }

        private string m_SkipFoldersHavingStrings;
        public string SkipFoldersHavingStrings
        {
            get
            {
                return HttpUtility.HtmlDecode(m_SkipFoldersHavingStrings);
            }

            set
            {
                m_SkipFoldersHavingStrings = HttpUtility.HtmlEncode(value);
                NotifyPropertyChanged("SkipFoldersHavingStrings");
            }
        }

        private Size m_ThumbnailSize;
        public Size ThumbnailSize
        {
            get
            {
                return m_ThumbnailSize;
            }
            set
            {
                m_ThumbnailSize = value;
                NotifyPropertyChanged("ThumbnailSize");
            }
        }

        private bool m_KeepAspectRatio;
        public bool KeepAspectRatio
        {
            get
            {
                return m_KeepAspectRatio;
            }
            set
            {
                m_KeepAspectRatio = value;
                NotifyPropertyChanged("KeepAspectRatio");
            }
        }

        private bool m_AddWatermark;
        public bool AddWatermark
        {
            get
            {
                return m_AddWatermark;
            }
            set
            {
                m_AddWatermark = value;
                NotifyPropertyChanged("AddWatermark");
            }
        }

        private string m_MTNPath;
        public string MTNPath
        {
            get
            {
                return HttpUtility.HtmlDecode(m_MTNPath);
            }
            set
            {
                m_MTNPath = HttpUtility.HtmlEncode(value);
                NotifyPropertyChanged("MTNPath");
                NotifyPropertyChanged("IsMTNPathSpecified");
            }
        }

        private string m_LastBackdropSelectedFolder;
        public string LastBackdropSelectedFolder
        {
            get
            {
                return m_LastBackdropSelectedFolder;
            }
            set
            {
                m_LastBackdropSelectedFolder = value;
                NotifyPropertyChanged("LastBackdropSelectedFolder");
            }
        }

        private string m_LastCoverSelectedFolder;
        public string LastCoverSelectedFolder
        {
            get
            {
                return m_LastCoverSelectedFolder;
            }
            set
            {
                m_LastCoverSelectedFolder = value;
                NotifyPropertyChanged("LastCoverSelectedFolder");
            }
        }

        private string m_LastProfileUsed;
        public string LastProfileUsed
        {
            get
            {
                return m_LastProfileUsed;
            }
            set
            {
                m_LastProfileUsed = value;
                NotifyPropertyChanged("LastProfileUsed");
            }
        }

        private string m_LastMovieResultsLayoutUsed;
        public string LastMovieResultsLayoutUsed
        {
            get
            {
                return m_LastMovieResultsLayoutUsed;
            }
            set
            {
                m_LastMovieResultsLayoutUsed = value;
                NotifyPropertyChanged("LastMovieResultsLayoutUsed");
            }
        }

        public bool IsMTNPathSpecified
        {
            get
            {
                return !string.IsNullOrEmpty(MTNPath);
            }
        }

        private bool m_EnableMultiCoreSupport;
        public bool EnableMultiCoreSupport
        {
            get
            {
                return m_EnableMultiCoreSupport;
            }
            set
            {
                m_EnableMultiCoreSupport = Environment.ProcessorCount > 1 ? value : false;
                NotifyPropertyChanged("EnableMultiCoreSupport");
            }
        }

        private bool m_SaveOriginalCoverAsExtraThumbnail;
        public bool SaveOriginalCoverAsExtraThumbnail
        {
            get
            {
                return m_SaveOriginalCoverAsExtraThumbnail;
            }
            set
            {
                m_SaveOriginalCoverAsExtraThumbnail = value;
                NotifyPropertyChanged("SaveOriginalCoverAsExtraThumbnail");
            }
        }

        private int m_SemiautomaticTimeout;
        public int SemiautomaticTimeout
        {
            get
            {
                return m_SemiautomaticTimeout;
            }
            set
            {
                m_SemiautomaticTimeout = value;
                NotifyPropertyChanged("SemiautomaticTimeout");
            }
        }

        private bool m_ExportNfoAsTvixie;
        public bool ExportNfoAsTvixie
        {
            get
            {
                return m_ExportNfoAsTvixie;
            }
            set
            {
                m_ExportNfoAsTvixie = value;
                NotifyPropertyChanged("ExportNfoAsTvixie");
            }
        }

        private bool m_ExportNfoAsXBMC;
        public bool ExportNfoAsXBMC
        {
            get
            {
                return m_ExportNfoAsXBMC;
            }
            set
            {
                m_ExportNfoAsXBMC = value;
                NotifyPropertyChanged("ExportNfoAsXBMC");
            }
        }

        private bool m_ExportNfoAsWDTVHUB;
        public bool ExportNfoAsWDTVHUB
        {
            get
            {
                return m_ExportNfoAsWDTVHUB;
            }
            set
            {
                m_ExportNfoAsWDTVHUB = value;
                NotifyPropertyChanged("ExportNfoAsWDTVHUB");
            }
        }

        private bool m_ExportNfoAsWDTVHUB_V2;
        public bool ExportNfoAsWDTVHUB_V2
        {
            get
            {
                return m_ExportNfoAsWDTVHUB_V2;
            }
            set
            {
                m_ExportNfoAsWDTVHUB_V2 = value;
                NotifyPropertyChanged("ExportNfoAsWDTVHUB_V2");
            }
        }

        private bool m_ExportNfoAsThumbGen;
        public bool ExportNfoAsThumbGen
        {
            get
            {
                return m_ExportNfoAsThumbGen;
            }
            set
            {
                m_ExportNfoAsThumbGen = value;
                NotifyPropertyChanged("ExportNfoAsThumbGen");
            }
        }

        private bool m_PutFullMediaInfoToExportedNfo;
        public bool PutFullMediaInfoToExportedNfo
        {
            get
            {
                return m_PutFullMediaInfoToExportedNfo;
            }
            set
            {
                m_PutFullMediaInfoToExportedNfo = value;
                NotifyPropertyChanged("PutFullMediaInfoToExportedNfo");
            }
        }

        private bool m_GenerateDummyFile;
        public bool GenerateDummyFile
        {
            get
            {
                return m_GenerateDummyFile;
            }
            set
            {
                m_GenerateDummyFile = value;
                NotifyPropertyChanged("GenerateDummyFile");
            }
        }

        private string m_UserDefinedFilesFilter;
        public string UserDefinedFilesFilter
        {
            get
            {
                return m_UserDefinedFilesFilter;
            }
            set
            {
                m_UserDefinedFilesFilter = value;
                NotifyPropertyChanged("UserDefinedFilesFilter");
            }
        }

        private string m_CustomDateFormat;
        public string CustomDateFormat
        {
            get
            {
                return m_CustomDateFormat;
            }
            set
            {
                m_CustomDateFormat = value;
                if (string.IsNullOrEmpty(value))
                {
                    m_CustomDateFormat = "dd.MM.yyyy";
                }
                NotifyPropertyChanged("CustomDateFormat");
            }
        }

        private bool m_ShowAllCollectors;
        public bool ShowAllCollectors
        {
            get
            {
                return m_ShowAllCollectors;
            }
            set
            {
                m_ShowAllCollectors = value;
                NotifyPropertyChanged("ShowAllCollectors");
            }
        }

        private bool m_EnableSmartOutputForDVDBRAY;
        public bool EnableSmartOutputForDVDBRAY
        {
            get
            {
                return m_EnableSmartOutputForDVDBRAY;
            }
            set
            {
                m_EnableSmartOutputForDVDBRAY = value;
                NotifyPropertyChanged("EnableSmartOutputForDVDBRAY");
            }
        }

        private bool m_EnableExportFromMetadata;
        public bool EnableExportFromMetadata
        {
            get
            {
                return m_EnableExportFromMetadata;
            }
            set
            {
                m_EnableExportFromMetadata = value;
                NotifyPropertyChanged("EnableExportFromMetadata");
            }
        }

        private bool m_SkipFoldersStartingWithDot;
        public bool SkipFoldersStartingWithDot
        {
            get
            {
                return m_SkipFoldersStartingWithDot;
            }
            set
            {
                m_SkipFoldersStartingWithDot = value;
                NotifyPropertyChanged("SkipFoldersStartingWithDot");
            }
        }

        public string TestMovieFile { get; set; }
        public string TestNfoFile { get; set; }
        public string TestBackground { get; set; }
        public string TestCover { get; set; }
        public string TestFanart1 { get; set; }
        public string TestFanart2 { get; set; }
        public string TestFanart3 { get; set; }
        public string TestMetadata { get; set; }

        private ObservableCollection<Playlists> m_PlaylistsJobs = new ObservableCollection<Playlists>();

        public ObservableCollection<Playlists> PlaylistsJobs
        {
            get
            {
                return m_PlaylistsJobs;
            }
            set
            {
                m_PlaylistsJobs = value;
                NotifyPropertyChanged("PlaylistsJobs");
            }
        }

        public Watermark WatermarkOptions { get; set; }
        public Subtitles SubtitlesOptions { get; set; }
        public Connection ConnectionOptions { get; set; }
        public MovieSheets MovieSheetsOptions { get; set; }
        public FileBrowser FileBrowserOptions { get; set; }
        public WindowSettings WindowsOptions { get; set; }
        public NamingConventions NamingOptions { get; set; }
        public Playlists PlaylistOptions { get; set; }
        public IMDB IMDBOptions { get; set; }
        public Import ImportOptions { get; set; }
        public SSH SSHOptions { get; set; }
        public Telnet TelnetOptions { get; set; }
        public ExportImages ExportImagesOptions { get; set; }
        public TVShowsFilters TVShowsFiltersOptions { get; set; }
        public CinePassion CinePassionOptions { get; set; }

        [Serializable]
        public class Import : BaseNotifyPropertyChanged
        {
            private string m_ANTXML;
            public string ANTXML
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_ANTXML);
                }
                set
                {
                    m_ANTXML = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("ANTXML");
                }
            }

            private bool m_ANTUseCustomSearch;
            public bool ANTUseCustomSearch
            {
                get
                {
                    return m_ANTUseCustomSearch;
                }
                set
                {
                    m_ANTUseCustomSearch = value;
                    NotifyPropertyChanged("ANTUseCustomSearch");
                }
            }

            private string m_ANTCustomSearchRegex;
            public string ANTCustomSearchRegex
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_ANTCustomSearchRegex);
                }
                set
                {
                    m_ANTCustomSearchRegex = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("ANTCustomSearchRegex");
                }
            }

            private string m_CollectorzXML;
            public string CollectorzXML
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_CollectorzXML);
                }
                set
                {
                    m_CollectorzXML = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("CollectorzXML");
                }
            }

            public Import()
            {
                ANTXML = string.Empty;
                CollectorzXML = string.Empty;
                m_ANTUseCustomSearch = false;
                m_ANTCustomSearchRegex = "^(?<ID>\\d*)"; // expects the ID of the movie at the beginning of the filename
            }
        }

        [Serializable]
        public class Playlists : BaseNotifyPropertyChanged
        {
            [NonSerialized]
            public static string NOSPLIT_CRITERIA = "<no split (one file)>";

            private string m_RelPath;
            public string RelPath
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_RelPath);
                }
                set
                {
                    m_RelPath = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("RelPath");
                }
            }

            private string m_SingleFilename;
            public string SingleFilename
            {
                get
                {
                    return m_SingleFilename;
                }
                set
                {
                    m_SingleFilename = string.IsNullOrEmpty(value) ? "movies" : value;
                    NotifyPropertyChanged("SingleFilename");
                }
            }

            private string m_Criteria;
            public string Criteria
            {
                get
                {
                    return m_Criteria;
                }
                set
                {
                    m_Criteria = value;
                    NotifyPropertyChanged("Criteria");
                }
            }

            private string m_SortCriteria;
            public string SortCriteria
            {
                get
                {
                    return m_SortCriteria;
                }
                set
                {
                    m_SortCriteria = value;
                    NotifyPropertyChanged("SortCriteria");
                }
            }

            private string m_SortCriteria2;
            public string SortCriteria2
            {
                get
                {
                    return m_SortCriteria2;
                }
                set
                {
                    m_SortCriteria2 = value;
                    NotifyPropertyChanged("SortCriteria2");
                }
            }

            private PlaylistFileType m_FileType;
            public PlaylistFileType FileType
            {
                get
                {
                    return m_FileType;
                }
                set
                {
                    m_FileType = value;
                    NotifyPropertyChanged("FileType");
                }
            }

            private bool m_ForceEnglishResults;
            public bool ForceEnglishResults
            {
                get
                {
                    return m_ForceEnglishResults;
                }
                set
                {
                    m_ForceEnglishResults = value;
                    NotifyPropertyChanged("ForceEnglishResults");
                }
            }

            private bool m_UseFolderInsteadOfMovie;
            public bool UseFolderInsteadOfMovie
            {
                get
                {
                    return m_UseFolderInsteadOfMovie;
                }
                set
                {
                    m_UseFolderInsteadOfMovie = value;
                    NotifyPropertyChanged("UseFolderInsteadOfMovie");
                }
            }

            private bool m_UseUnassignedPlaylist;
            public bool UseUnassignedPlaylist
            {
                get
                {
                    return m_UseUnassignedPlaylist;
                }
                set
                {
                    m_UseUnassignedPlaylist = value;
                    NotifyPropertyChanged("UseUnassignedPlaylist");
                }
            }

            private bool m_IsSortingDescending;
            public bool IsSortingDescending
            {
                get
                {
                    return m_IsSortingDescending;
                }
                set
                {
                    m_IsSortingDescending = value;
                    NotifyPropertyChanged("IsSortingDescending");
                }
            }

            private bool m_IsSortingDescending2;
            public bool IsSortingDescending2
            {
                get
                {
                    return m_IsSortingDescending2;
                }
                set
                {
                    m_IsSortingDescending2 = value;
                    NotifyPropertyChanged("IsSortingDescending2");
                }
            }

            private bool m_IsActive;
            public bool IsActive
            {
                get
                {
                    return m_IsActive;
                }
                set
                {
                    m_IsActive = value;
                    NotifyPropertyChanged("IsActive");
                }
            }

            private bool m_CleanFolder;
            public bool CleanFolder
            {
                get
                {
                    return m_CleanFolder;
                }
                set
                {
                    m_CleanFolder = value;
                    NotifyPropertyChanged("CleanFolder");
                }
            }

            public Playlists()
            {
                Criteria = "Genre";
                FileType = PlaylistFileType.M3U;
                RelPath = string.Empty;
                ForceEnglishResults = false;
                SortCriteria = "Alpha";
                SortCriteria2 = "Alpha";
                UseFolderInsteadOfMovie = false;
                UseUnassignedPlaylist = true;
                IsSortingDescending = false;
                IsSortingDescending2 = false;
                SingleFilename = "movies";
                IsActive = true;
                CleanFolder = false;
            }
        }

        [Serializable]
        public class CinePassion : BaseNotifyPropertyChanged
        {
            private string m_Username;
            public string Username
            {
                get
                {
                    return m_Username;
                }
                set
                {
                    m_Username = value;
                    NotifyPropertyChanged("Username");
                }
            }

            private string m_Password = string.Empty;

            [XmlIgnore]
            public string Pass
            {
                get { return m_Password; }
                set
                {
                    m_Password = value;
                    NotifyPropertyChanged("Pass");
                }
            }

            public string Password
            {
                get
                {
                    return HttpUtility.HtmlEncode(ConfigHelpers.Encrypt(m_Password));
                }
                set
                {
                    m_Password = HttpUtility.HtmlDecode(ConfigHelpers.Decrypt(value));
                    NotifyPropertyChanged("Password");
                }
            }

            public CinePassion()
            {

            }
        }

        [Serializable]
        public class IMDB : BaseNotifyPropertyChanged
        {
            private IMDBMovieInfoBehaviour m_UsageBehaviour;
            public IMDBMovieInfoBehaviour UsageBehaviour
            {
                get
                {
                    return m_UsageBehaviour;
                }
                set
                {
                    m_UsageBehaviour = value;
                    NotifyPropertyChanged("UsageBehaviour");
                }
            }

            private bool m_AlwaysUseIMDbRating;
            public bool AlwaysUseIMDbRating
            {
                get
                {
                    return m_AlwaysUseIMDbRating;
                }
                set
                {
                    m_AlwaysUseIMDbRating = value;
                    NotifyPropertyChanged("AlwaysUseIMDbRating");
                }
            }

            private bool m_AutofillIMDbIdForFirstMovieIfMissing;
            public bool AutofillIMDbIdForFirstMovieIfMissing
            {
                get
                {
                    return m_AutofillIMDbIdForFirstMovieIfMissing;
                }
                set
                {
                    m_AutofillIMDbIdForFirstMovieIfMissing = value;
                    NotifyPropertyChanged("AutofillIMDbIdForFirstMovieIfMissing");
                }
            }

            private bool m_UseIMDbPreselectDialog;
            public bool UseIMDbPreselectDialog
            {
                get
                {
                    return m_UseIMDbPreselectDialog;
                }
                set
                {
                    m_UseIMDbPreselectDialog = value;
                    NotifyPropertyChanged("UseIMDbPreselectDialog");
                }
            }

            private int m_MaxCountResults;
            public int MaxCountResults
            {
                get
                {
                    return m_MaxCountResults;
                }
                set
                {
                    m_MaxCountResults = Math.Max(1, value);
                    NotifyPropertyChanged("MaxCountResults");
                }
            }

            private string m_Country;
            public string Country
            {
                get
                {
                    return m_Country;
                }
                set
                {
                    m_Country = value;
                    NotifyPropertyChanged("Country");
                }
            }

            private string m_CertificationCountry;
            public string CertificationCountry
            {
                get
                {
                    return m_CertificationCountry;
                }
                set
                {
                    m_CertificationCountry = value;
                    NotifyPropertyChanged("CertificationCountry");
                }
            }

            private bool m_UseFeelingLuckyMode;
            public bool UseFeelingLuckyMode
            {
                get
                {
                    return m_UseFeelingLuckyMode;
                }
                set
                {
                    m_UseFeelingLuckyMode = value;
                    NotifyPropertyChanged("UseFeelingLuckyMode");
                }
            }

            public IMDB()
            {
                UsageBehaviour = IMDBMovieInfoBehaviour.FillMissingDataFromIMDB;
                AlwaysUseIMDbRating = true;
                UseIMDbPreselectDialog = true;
                MaxCountResults = 10;
                AutofillIMDbIdForFirstMovieIfMissing = true;
                Country = "com";
                CertificationCountry = "us";
                UseFeelingLuckyMode = false;
            }
        }

        [Serializable]
        public enum SortOption
        {
            Alphabetically,
            Date
        }

        [Serializable]
        public class FileBrowser : BaseNotifyPropertyChanged
        {
            private bool m_ShowMediaInfo;
            public bool ShowMediaInfo
            {
                get
                {
                    return m_ShowMediaInfo;
                }
                set
                {
                    m_ShowMediaInfo = value;
                    NotifyPropertyChanged("ShowMediaInfo");
                }
            }

            private bool m_ShowMovieSheetAtFolderLevel;
            public bool ShowMovieSheetAtFolderLevel
            {
                get
                {
                    return m_ShowMovieSheetAtFolderLevel;
                }
                set
                {
                    m_ShowMovieSheetAtFolderLevel = value;
                    NotifyPropertyChanged("ShowMovieSheetAtFolderLevel");
                }
            }

            private bool m_ShowMovieSheet;
            public bool ShowMovieSheet
            {
                get
                {
                    return m_ShowMovieSheet;
                }
                set
                {
                    m_ShowMovieSheet = value;
                    NotifyPropertyChanged("ShowMovieSheet");
                }
            }

            private bool m_ShowHasExternalSubtitles;
            public bool ShowHasExternalSubtitles
            {
                get
                {
                    return m_ShowHasExternalSubtitles;
                }
                set
                {
                    m_ShowHasExternalSubtitles = value;
                    NotifyPropertyChanged("ShowHasExternalSubtitles");
                }
            }

            private bool m_ShowHasMovieInfo;
            public bool ShowHasMovieInfo
            {
                get
                {
                    return m_ShowHasMovieInfo;
                }
                set
                {
                    m_ShowHasMovieInfo = value;
                    NotifyPropertyChanged("ShowHasMovieInfo");
                }
            }

            private bool m_ShowHasMoviesheet;
            public bool ShowHasMoviesheet
            {
                get
                {
                    return m_ShowHasMoviesheet;
                }
                set
                {
                    m_ShowHasMoviesheet = value;
                    NotifyPropertyChanged("ShowHasMoviesheet");
                }
            }

            private bool m_ShowHasMoviesheetMetadata;
            public bool ShowHasMoviesheetMetadata
            {
                get
                {
                    return m_ShowHasMoviesheetMetadata;
                }
                set
                {
                    m_ShowHasMoviesheetMetadata = value;
                    NotifyPropertyChanged("ShowHasMoviesheetMetadata");
                }
            }

            private bool m_FilterWithoutMoviesheet;
            public bool FilterWithoutMoviesheet
            {
                get
                {
                    return m_FilterWithoutMoviesheet;
                }
                set
                {
                    m_FilterWithoutMoviesheet = value;
                    NotifyPropertyChanged("FilterWithoutMoviesheet");
                }
            }

            private bool m_FilterWithoutExtraMoviesheet;
            public bool FilterWithoutExtraMoviesheet
            {
                get
                {
                    return m_FilterWithoutExtraMoviesheet;
                }
                set
                {
                    m_FilterWithoutExtraMoviesheet = value;
                    NotifyPropertyChanged("FilterWithoutExtraMoviesheet");
                }
            }

            private bool m_FilterWithoutExtSubtitles;
            public bool FilterWithoutExtSubtitles
            {
                get
                {
                    return m_FilterWithoutExtSubtitles;
                }
                set
                {
                    m_FilterWithoutExtSubtitles = value;
                    NotifyPropertyChanged("FilterWithoutExtSubtitles");
                }
            }

            private bool m_FilterWithoutMovieInfo;
            public bool FilterWithoutMovieInfo
            {
                get
                {
                    return m_FilterWithoutMovieInfo;
                }
                set
                {
                    m_FilterWithoutMovieInfo = value;
                    NotifyPropertyChanged("FilterWithoutMovieInfo");
                }
            }

            private bool m_FilterWithoutMetadata;
            public bool FilterWithoutMetadata
            {
                get
                {
                    return m_FilterWithoutMetadata;
                }
                set
                {
                    m_FilterWithoutMetadata = value;
                    NotifyPropertyChanged("FilterWithoutMetadata");
                }
            }

            private bool m_FilterWithoutThumbnail;
            public bool FilterWithoutThumbnail
            {
                get
                {
                    return m_FilterWithoutThumbnail;
                }
                set
                {
                    m_FilterWithoutThumbnail = value;
                    NotifyPropertyChanged("FilterWithoutThumbnail");
                }
            }

            private bool m_FilterWithoutFolderJpg;
            public bool FilterWithoutFolderJpg
            {
                get
                {
                    return m_FilterWithoutFolderJpg;
                }
                set
                {
                    m_FilterWithoutFolderJpg = value;
                    NotifyPropertyChanged("FilterWithoutFolderJpg");
                }
            }

            private bool m_FilterOnlyFirstMovieInFolder;
            public bool FilterOnlyFirstMovieInFolder
            {
                get
                {
                    return m_FilterOnlyFirstMovieInFolder;
                }
                set
                {
                    m_FilterOnlyFirstMovieInFolder = value;
                    NotifyPropertyChanged("FilterOnlyFirstMovieInFolder");
                }
            }

            private bool m_UseBRayFilter;
            public bool UseBRayFilter
            {
                get
                {
                    return m_UseBRayFilter;
                }
                set
                {
                    m_UseBRayFilter = value;
                    NotifyPropertyChanged("UseBRayFilter");
                }
            }

            private SortOption m_Sorting;
            public SortOption Sorting
            {
                get
                {
                    return m_Sorting;
                }
                set
                {
                    m_Sorting = value;
                    NotifyPropertyChanged("Sorting");
                }
            }

            private bool m_IsSortingAscending;
            public bool IsSortingAscending
            {
                get
                {
                    return m_IsSortingAscending;
                }
                set
                {
                    m_IsSortingAscending = value;
                    NotifyPropertyChanged("IsSortingAscending");
                }
            }

            public FileBrowser()
            {
                ShowMediaInfo = true;
                ShowMovieSheet = true;
                ShowMovieSheetAtFolderLevel = false;
                ShowHasExternalSubtitles = true;
                ShowHasMovieInfo = true;
                ShowHasMoviesheet = true;
                ShowHasMoviesheetMetadata = true;
                FilterWithoutExtSubtitles = false;
                FilterWithoutMovieInfo = false;
                FilterWithoutMoviesheet = false;
                FilterWithoutMetadata = false;
                FilterWithoutExtraMoviesheet = false;
                FilterWithoutThumbnail = false;
                FilterWithoutFolderJpg = false;
                FilterOnlyFirstMovieInFolder = false;
                UseBRayFilter = false;
                Sorting = SortOption.Alphabetically;
                IsSortingAscending = true;
            }

            public bool IsFilterActive()
            {
                return (FilterWithoutExtSubtitles || FilterWithoutMovieInfo || FilterWithoutMoviesheet || FilterWithoutExtraMoviesheet
                          || FilterWithoutThumbnail || FilterWithoutFolderJpg || FilterOnlyFirstMovieInFolder || FilterWithoutMetadata);
            }
        }

        [Serializable]
        public class TVShowsFilters : BaseNotifyPropertyChanged
        {
            private bool m_UseX;
            public bool UseX
            {
                get
                {
                    return m_UseX;
                }
                set
                {
                    m_UseX = value;
                    NotifyPropertyChanged("UseXFilter");
                }
            }

            private bool m_UseAired;
            public bool UseAired
            {
                get
                {
                    return m_UseAired;
                }
                set
                {
                    m_UseAired = value;
                    NotifyPropertyChanged("UseAired");
                }
            }

            private bool m_UseDVD;
            public bool UseDVD
            {
                get
                {
                    return m_UseDVD;
                }
                set
                {
                    m_UseDVD = value;
                    NotifyPropertyChanged("UseDVD");
                }
            }

            private bool m_UseAbsolute;
            public bool UseAbsolute
            {
                get
                {
                    return m_UseAbsolute;
                }
                set
                {
                    m_UseAbsolute = value;
                    NotifyPropertyChanged("UseAbsolute");
                }
            }

            private bool m_UseShort;
            public bool UseShort
            {
                get
                {
                    return m_UseShort;
                }
                set
                {
                    m_UseShort = value;
                    NotifyPropertyChanged("UseShort");
                }
            }

            private bool m_UseEachEpisodeInOwnFolder;
            public bool UseEachEpisodeInOwnFolder
            {
                get
                {
                    return m_UseEachEpisodeInOwnFolder;
                }
                set
                {
                    m_UseEachEpisodeInOwnFolder = value;
                    NotifyPropertyChanged("UseEachEpisodeInOwnFolder");
                }
            }

            //private bool m_UseAllSeasonEpisodesInOneFolder;
            //public bool UseAllSeasonEpisodesInOneFolder
            //{
            //    get
            //    {
            //        return m_UseAllSeasonEpisodesInOneFolder;
            //    }
            //    set
            //    {
            //        m_UseAllSeasonEpisodesInOneFolder = value;
            //        NotifyPropertyChanged("UseAllSeasonEpisodesInOneFolder");
            //    }
            //}

            public TVShowsFilters()
            {
                this.UseAbsolute = false;
                this.UseAired = true;
                this.UseDVD = true;
                this.UseShort = false;
                this.UseX = true;

                this.UseEachEpisodeInOwnFolder = false;
                //this.UseAllSeasonEpisodesInOneFolder = true;
            }
        }

        [Serializable]
        public class MovieSheets : BaseNotifyPropertyChanged
        {
            private int m_Count;
            public int Count
            {
                get
                {
                    return m_Count;
                }
                set
                {
                    m_Count = value;
                    NotifyPropertyChanged("Count");
                }
            }

            private string m_TemplateName;
            public string TemplateName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_TemplateName);
                }
                set
                {
                    m_TemplateName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("TemplateName");
                }
            }

            private string m_ExtraTemplateName;
            public string ExtraTemplateName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_ExtraTemplateName);
                }
                set
                {
                    m_ExtraTemplateName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("ExtraTemplateName");
                }
            }

            private string m_ParentFolderTemplateName;
            public string ParentFolderTemplateName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_ParentFolderTemplateName);
                }
                set
                {
                    m_ParentFolderTemplateName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("ParentFolderTemplateName");
                }
            }

            private double m_MaxFilesize;
            public double MaxFilesize
            {
                get
                {
                    return m_MaxFilesize;
                }
                set
                {
                    m_MaxFilesize = value;
                    NotifyPropertyChanged("MaxFilesize");
                }
            }

            private bool m_IsMaxQuality;
            public bool IsMaxQuality
            {
                get
                {
                    return m_IsMaxQuality;
                }
                set
                {
                    m_IsMaxQuality = value;
                    NotifyPropertyChanged("IsMaxQuality");
                }
            }

            private bool m_AutoSelectFolderjpgAsCover;
            public bool AutoSelectFolderjpgAsCover
            {
                get
                {
                    return m_AutoSelectFolderjpgAsCover;
                }
                set
                {
                    m_AutoSelectFolderjpgAsCover = value;
                    NotifyPropertyChanged("AutoSelectFolderjpgAsCover");
                }
            }

            private string m_AutoSelectFolderjpgAsCoverName;
            public string AutoSelectFolderjpgAsCoverName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoSelectFolderjpgAsCoverName);
                }
                set
                {
                    m_AutoSelectFolderjpgAsCoverName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoSelectFolderjpgAsCoverName");
                }
            }

            private bool m_AutoSelectFanartjpgAsBackground;
            public bool AutoSelectFanartjpgAsBackground
            {
                get
                {
                    return m_AutoSelectFanartjpgAsBackground;
                }
                set
                {
                    m_AutoSelectFanartjpgAsBackground = value;
                    NotifyPropertyChanged("AutoSelectFanartjpgAsBackground");
                }
            }

            private string m_AutoSelectFanartjpgAsBackgroundName;
            public string AutoSelectFanartjpgAsBackgroundName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoSelectFanartjpgAsBackgroundName);
                }
                set
                {
                    m_AutoSelectFanartjpgAsBackgroundName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoSelectFanartjpgAsBackgroundName");
                }
            }

            private bool m_AutoSelectFanart1jpgAsBackground;
            public bool AutoSelectFanart1jpgAsBackground
            {
                get
                {
                    return m_AutoSelectFanart1jpgAsBackground;
                }
                set
                {
                    m_AutoSelectFanart1jpgAsBackground = value;
                    NotifyPropertyChanged("AutoSelectFanart1jpgAsBackground");
                }
            }

            private string m_AutoSelectFanart1jpgAsBackgroundName;
            public string AutoSelectFanart1jpgAsBackgroundName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoSelectFanart1jpgAsBackgroundName);
                }
                set
                {
                    m_AutoSelectFanart1jpgAsBackgroundName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoSelectFanart1jpgAsBackgroundName");
                }
            }

            private bool m_AutoSelectFanart2jpgAsBackground;
            public bool AutoSelectFanart2jpgAsBackground
            {
                get
                {
                    return m_AutoSelectFanart2jpgAsBackground;
                }
                set
                {
                    m_AutoSelectFanart2jpgAsBackground = value;
                    NotifyPropertyChanged("AutoSelectFanart2jpgAsBackground");
                }
            }

            private string m_AutoSelectFanart2jpgAsBackgroundName;
            public string AutoSelectFanart2jpgAsBackgroundName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoSelectFanart2jpgAsBackgroundName);
                }
                set
                {
                    m_AutoSelectFanart2jpgAsBackgroundName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoSelectFanart2jpgAsBackgroundName");
                }
            }

            private bool m_AutoSelectFanart3jpgAsBackground;
            public bool AutoSelectFanart3jpgAsBackground
            {
                get
                {
                    return m_AutoSelectFanart3jpgAsBackground;
                }
                set
                {
                    m_AutoSelectFanart3jpgAsBackground = value;
                    NotifyPropertyChanged("AutoSelectFanart3jpgAsBackground");
                }
            }

            private string m_AutoSelectFanart3jpgAsBackgroundName;
            public string AutoSelectFanart3jpgAsBackgroundName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoSelectFanart3jpgAsBackgroundName);
                }
                set
                {
                    m_AutoSelectFanart3jpgAsBackgroundName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoSelectFanart3jpgAsBackgroundName");
                }
            }

            private bool m_AutopopulateFromMetadata;
            public bool AutopopulateFromMetadata
            {
                get
                {
                    return m_AutopopulateFromMetadata;
                }
                set
                {
                    m_AutopopulateFromMetadata = value;
                    NotifyPropertyChanged("AutopopulateFromMetadata");
                }
            }

            private bool m_InsertInPoolFromMetadata;
            public bool InsertInPoolFromMetadata
            {
                get
                {
                    return m_InsertInPoolFromMetadata;
                }
                set
                {
                    m_InsertInPoolFromMetadata = value;
                    NotifyPropertyChanged("InsertInPoolFromMetadata");
                }
            }

            private bool m_DoNotAutopopulateBackdrop;
            public bool DoNotAutopopulateBackdrop
            {
                get
                {
                    return m_DoNotAutopopulateBackdrop;
                }
                set
                {
                    m_DoNotAutopopulateBackdrop = value;
                    NotifyPropertyChanged("DoNotAutopopulateBackdrop");
                }
            }

            private bool m_DoNotAutopopulateFanart;
            public bool DoNotAutopopulateFanart
            {
                get
                {
                    return m_DoNotAutopopulateFanart;
                }
                set
                {
                    m_DoNotAutopopulateFanart = value;
                    NotifyPropertyChanged("DoNotAutopopulateFanart");
                }
            }

            private string m_DefaultExternalSubtitlesLanguage;
            public string DefaultExternalSubtitlesLanguage
            {
                get
                {
                    return m_DefaultExternalSubtitlesLanguage;
                }
                set
                {
                    m_DefaultExternalSubtitlesLanguage = value;
                    NotifyPropertyChanged("DefaultExternalSubtitlesLanguage");
                }
            }

            private string m_TVShowsLanguage;
            public string TVShowsLanguage
            {
                get
                {
                    return m_TVShowsLanguage;
                }
                set
                {
                    m_TVShowsLanguage = value;
                    NotifyPropertyChanged("TVShowsLanguage");
                }
            }

            private string m_DefaultAudioLanguage;
            public string DefaultAudioLanguage
            {
                get
                {
                    return m_DefaultAudioLanguage;
                }
                set
                {
                    m_DefaultAudioLanguage = value;
                    NotifyPropertyChanged("DefaultAudioLanguage");
                }
            }

            private bool m_IgnoreOtherLanguages;
            public bool IgnoreOtherLanguages
            {
                get
                {
                    return m_IgnoreOtherLanguages;
                }
                set
                {
                    m_IgnoreOtherLanguages = value;
                    NotifyPropertyChanged("IgnoreOtherLanguages");
                }
            }

            private bool m_AutotranslateGenre;
            public bool AutotranslateGenre
            {
                get
                {
                    return m_AutotranslateGenre;
                }
                set
                {
                    m_AutotranslateGenre = value;
                    NotifyPropertyChanged("AutotranslateGenre");
                }
            }

            private bool m_DisablePreferredInfoCollector;
            public bool DisablePreferredInfoCollector
            {
                get
                {
                    return m_DisablePreferredInfoCollector;
                }
                set
                {
                    m_DisablePreferredInfoCollector = value;
                    NotifyPropertyChanged("DisablePreferredInfoCollector");
                }
            }

            private bool m_AutorefreshPreview;
            public bool AutorefreshPreview
            {
                get
                {
                    return m_AutorefreshPreview;
                }
                set
                {
                    m_AutorefreshPreview = value;
                    NotifyPropertyChanged("AutorefreshPreview");
                }
            }

            private int m_OverscanLeft;
            public int OverscanLeft
            {
                get
                {
                    return m_OverscanLeft;
                }
                set
                {
                    m_OverscanLeft = value;
                    NotifyPropertyChanged("OverscanLeft");
                }
            }

            private int m_OverscanRight;
            public int OverscanRight
            {
                get
                {
                    return m_OverscanRight;
                }
                set
                {
                    m_OverscanRight = value;
                    NotifyPropertyChanged("OverscanRight");
                }
            }

            private int m_OverscanTop;
            public int OverscanTop
            {
                get
                {
                    return m_OverscanTop;
                }
                set
                {
                    m_OverscanTop = value;
                    NotifyPropertyChanged("OverscanTop");
                }
            }

            private int m_OverscanBottom;
            public int OverscanBottom
            {
                get
                {
                    return m_OverscanBottom;
                }
                set
                {
                    m_OverscanBottom = value;
                    NotifyPropertyChanged("OverscanBottom");
                }
            }

            public bool IsOverscanCorrectionNeeded(SheetType type)
            {
                bool _apply = false;
                switch (type)
                {
                    default:
                    case SheetType.Main:
                        _apply = ApplyOverscanMain;
                        break;
                    case SheetType.Extra:
                        _apply = ApplyOverscanExtra;
                        break;
                    case SheetType.Spare:
                        _apply = ApplyOverscanSpare;
                        break;
                }
                return (OverscanLeft != 0 || OverscanRight != 0 || OverscanTop != 0 || OverscanBottom != 0) && _apply;
            }

            private bool m_AutoTakeScreenshots;
            public bool AutoTakeScreenshots
            {
                get
                {
                    return m_AutoTakeScreenshots;
                }
                set
                {
                    m_AutoTakeScreenshots = value;
                    NotifyPropertyChanged("AutoTakeScreenshots");
                }
            }

            private bool m_ApplyOverscanMain;
            public bool ApplyOverscanMain
            {
                get
                {
                    return m_ApplyOverscanMain;
                }
                set
                {
                    m_ApplyOverscanMain = value;
                    NotifyPropertyChanged("ApplyOverscanMain");
                }
            }

            private bool m_ApplyOverscanExtra;
            public bool ApplyOverscanExtra
            {
                get
                {
                    return m_ApplyOverscanExtra;
                }
                set
                {
                    m_ApplyOverscanExtra = value;
                    NotifyPropertyChanged("ApplyOverscanExtra");
                }
            }

            private bool m_ApplyOverscanSpare;
            public bool ApplyOverscanSpare
            {
                get
                {
                    return m_ApplyOverscanSpare;
                }
                set
                {
                    m_ApplyOverscanSpare = value;
                    NotifyPropertyChanged("ApplyOverscanSpare");
                }
            }

            private ObservableCollection<MovieInfoProviderItemType> m_MovieInfoPriorities = new ObservableCollection<MovieInfoProviderItemType>();

            public ObservableCollection<MovieInfoProviderItemType> MovieInfoPriorities
            {
                get
                {
                    return m_MovieInfoPriorities;
                }
                set
                {
                    m_MovieInfoPriorities = value;
                    NotifyPropertyChanged("MovieInfoPriorities");
                }
            }

            public MovieSheets()
            {
                IsMaxQuality = true;
                MaxFilesize = 500000;
                DoNotAutopopulateBackdrop = false;
                DoNotAutopopulateFanart = true;

                AutoSelectFanartjpgAsBackgroundName = "fanart.jpg";
                AutoSelectFanartjpgAsBackground = false;
                AutoSelectFolderjpgAsCoverName = "folder.jpg";
                AutoSelectFanart1jpgAsBackground = false;
                AutoSelectFanart1jpgAsBackgroundName = "fanart1.jpg";
                AutoSelectFanart2jpgAsBackground = false;
                AutoSelectFanart2jpgAsBackgroundName = "fanart2.jpg";
                AutoSelectFanart3jpgAsBackground = false;
                AutoSelectFanart3jpgAsBackgroundName = "fanart3.jpg";
                AutoSelectFolderjpgAsCover = false;

                DefaultExternalSubtitlesLanguage = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
                TVShowsLanguage = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
                DefaultAudioLanguage = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
                IgnoreOtherLanguages = false;
                Count = 2;
                AutorefreshPreview = true;
                DisablePreferredInfoCollector = false;
                AutopopulateFromMetadata = false;
                OverscanBottom = 0;
                OverscanLeft = 0;
                OverscanRight = 0;
                OverscanTop = 0;
                AutotranslateGenre = false;
                AutoTakeScreenshots = true;
                InsertInPoolFromMetadata = true;

                ApplyOverscanMain = true;
                ApplyOverscanExtra = true;
                ApplyOverscanSpare = true;
            }
        }

        [Serializable]
        public class WindowProperties : BaseNotifyPropertyChanged
        {
            private Rect m_Position;
            public Rect Position
            {
                get
                {
                    return m_Position;
                }
                set
                {
                    m_Position = value;
                    NotifyPropertyChanged("Position");
                }
            }

            private WindowState m_State;
            public WindowState State
            {
                get
                {
                    return m_State;
                }
                set
                {
                    m_State = value;
                    NotifyPropertyChanged("State");
                }
            }


            public WindowProperties()
            {
                State = WindowState.Maximized;
                Position = new Rect(16, 16, 1024, 768);
            }
        }

        [Serializable]
        public class WindowSettings : BaseNotifyPropertyChanged
        {
            public SerializableDictionary<string, WindowProperties> Windows { get; set; }


            public WindowSettings()
            {
                Windows = new SerializableDictionary<string, WindowProperties>();
                Windows.Add(typeof(ThumbGenMainWindow).FullName, new WindowProperties());
                Windows.Add(typeof(ResultsListBox).FullName, new WindowProperties());
                Windows.Add(typeof(ExplorerWindow).FullName, new WindowProperties());
                Windows.Add(typeof(Options).FullName, new WindowProperties());
            }
        }

        [Serializable]
        public class Subtitles : BaseNotifyPropertyChanged
        {
            private string m_Language;
            public string Language
            {
                get
                {
                    return m_Language;
                }
                set
                {
                    m_Language = value;
                    NotifyPropertyChanged("Language");
                }
            }

            public Subtitles()
            {
            }
        }

        [Serializable]
        public enum PlaylistFileType
        {
            M3U,
            PLS
        }

        [Serializable]
        public enum ConnectionType
        {
            Direct,
            UseIE,
            Proxy
        }

        [Serializable]
        public class Connection : BaseNotifyPropertyChanged
        {
            private ConnectionType m_Type;
            public ConnectionType Type
            {
                get
                {
                    return m_Type;
                }
                set
                {
                    m_Type = value;
                    NotifyPropertyChanged("Type");
                }
            }

            private string m_ProxyHost;
            public string ProxyHost
            {
                get
                {
                    return m_ProxyHost;
                }
                set
                {
                    m_ProxyHost = value;
                    NotifyPropertyChanged("ProxyHost");
                }
            }

            private string m_ProxyPort;
            public string ProxyPort
            {
                get
                {
                    return m_ProxyPort;
                }
                set
                {
                    m_ProxyPort = value;
                    NotifyPropertyChanged("ProxyPort");
                }
            }

            private string m_ProxyUser;
            public string ProxyUser
            {
                get
                {
                    return m_ProxyUser;
                }
                set
                {
                    m_ProxyUser = value;
                    NotifyPropertyChanged("ProxyUser");
                }
            }

            private string m_Password = string.Empty;

            [XmlIgnore]
            public string ProxyPass
            {
                get { return m_Password; }
                set
                {
                    m_Password = value;
                    NotifyPropertyChanged("ProxyPass");
                }
            }

            public string ProxyPassword
            {
                get
                {
                    return HttpUtility.HtmlEncode(ConfigHelpers.Encrypt(m_Password));
                }
                set
                {
                    m_Password = HttpUtility.HtmlDecode(ConfigHelpers.Decrypt(value));
                    NotifyPropertyChanged("ProxyPassword");
                }
            }

            public Connection()
            {
                ProxyHost = string.Empty;
                ProxyPort = string.Empty;
                ProxyUser = string.Empty;
                ProxyPass = string.Empty;
            }
        }

        [Serializable]
        public class SSH : BaseNotifyPropertyChanged
        {
            private string m_SSHHost;
            public string SSHHost
            {
                get
                {
                    return m_SSHHost;
                }
                set
                {
                    m_SSHHost = value;
                    NotifyPropertyChanged("SSHHost");
                }
            }

            private string m_SSHPort;
            public string SSHPort
            {
                get
                {
                    return m_SSHPort;
                }
                set
                {
                    m_SSHPort = value;
                    NotifyPropertyChanged("SSHPort");
                }
            }

            private string m_SSHUser;
            public string SSHUser
            {
                get
                {
                    return m_SSHUser;
                }
                set
                {
                    m_SSHUser = value;
                    NotifyPropertyChanged("SSHUser");
                }
            }

            private string m_Password = string.Empty;

            [XmlIgnore]
            public string SSHPass
            {
                get { return m_Password; }
                set
                {
                    m_Password = value;
                    NotifyPropertyChanged("SSHPass");
                }
            }

            public string SSHPassword
            {
                get
                {
                    return HttpUtility.HtmlEncode(ConfigHelpers.Encrypt(m_Password));
                }
                set
                {
                    m_Password = HttpUtility.HtmlDecode(ConfigHelpers.Decrypt(value));
                    NotifyPropertyChanged("SSHPassword");
                }
            }

            private List<string> m_SSHHistory;
            public List<string> SSHHistory
            {
                get
                {
                    List<string> _list = new List<string>();
                    foreach (string _s in m_SSHHistory)
                    {
                        _list.Add(HttpUtility.HtmlDecode(_s));
                    }
                    m_SSHHistory = _list;
                    return m_SSHHistory;
                }
                set
                {
                    m_SSHHistory = new List<string>();
                    foreach (string _s in value)
                    {
                        m_SSHHistory.Add(HttpUtility.HtmlEncode(_s));
                    }

                    NotifyPropertyChanged("SSHHistory");
                }
            }

            public SSH()
            {
                SSHHost = "wdtvlive";
                SSHUser = "root";
                SSHPass = "";
                SSHPort = "22";
                SSHHistory = new List<string>();
            }
        }

        [Serializable]
        public class Telnet : BaseNotifyPropertyChanged
        {
            private string m_TelnetHost;
            public string TelnetHost
            {
                get
                {
                    return m_TelnetHost;
                }
                set
                {
                    m_TelnetHost = value;
                    NotifyPropertyChanged("TelnetHost");
                }
            }

            private string m_TelnetPort;
            public string TelnetPort
            {
                get
                {
                    return m_TelnetPort;
                }
                set
                {
                    m_TelnetPort = value;
                    NotifyPropertyChanged("TelnetPort");
                }
            }

            private string m_TelnetUser;
            public string TelnetUser
            {
                get
                {
                    return m_TelnetUser;
                }
                set
                {
                    m_TelnetUser = value;
                    NotifyPropertyChanged("TelnetUser");
                }
            }

            private string m_Password = string.Empty;

            [XmlIgnore]
            public string TelnetPass
            {
                get { return m_Password; }
                set
                {
                    m_Password = value;
                    NotifyPropertyChanged("TelnetPass");
                }
            }

            public string TelnetPassword
            {
                get
                {
                    return HttpUtility.HtmlEncode(ConfigHelpers.Encrypt(m_Password));
                }
                set
                {
                    m_Password = HttpUtility.HtmlDecode(ConfigHelpers.Decrypt(value));
                    NotifyPropertyChanged("TelnetPassword");
                }
            }

            public Telnet()
            {
                TelnetHost = "wdtvlive";
                TelnetUser = "root";
                TelnetPass = "";
                TelnetPort = "23";
            }
        }

        [Serializable]
        public class ExportImages : BaseNotifyPropertyChanged
        {
            // cover
            private double m_MaxFilesize;
            public double MaxFilesize
            {
                get
                {
                    return m_MaxFilesize;
                }
                set
                {
                    m_MaxFilesize = value;
                    NotifyPropertyChanged("MaxFilesize");
                }
            }

            private bool m_IsMaxQuality;
            public bool IsMaxQuality
            {
                get
                {
                    return m_IsMaxQuality;
                }
                set
                {
                    m_IsMaxQuality = value;
                    NotifyPropertyChanged("IsMaxQuality");
                }
            }

            private double m_MaxFilesizeBackground;
            public double MaxFilesizeBackground
            {
                get
                {
                    return m_MaxFilesizeBackground;
                }
                set
                {
                    m_MaxFilesizeBackground = value;
                    NotifyPropertyChanged("MaxFilesizeBackground");
                }
            }

            private bool m_IsMaxQualityBackground;
            public bool IsMaxQualityBackground
            {
                get
                {
                    return m_IsMaxQualityBackground;
                }
                set
                {
                    m_IsMaxQualityBackground = value;
                    NotifyPropertyChanged("IsMaxQualityBackground");
                }
            }

            private double m_MaxFilesizeFanart1;
            public double MaxFilesizeFanart1
            {
                get
                {
                    return m_MaxFilesizeFanart1;
                }
                set
                {
                    m_MaxFilesizeFanart1 = value;
                    NotifyPropertyChanged("MaxFilesizeFanart1");
                }
            }

            private bool m_IsMaxQualityFanart1;
            public bool IsMaxQualityFanart1
            {
                get
                {
                    return m_IsMaxQualityFanart1;
                }
                set
                {
                    m_IsMaxQualityFanart1 = value;
                    NotifyPropertyChanged("IsMaxQualityFanart1");
                }
            }

            private double m_MaxFilesizeFanart2;
            public double MaxFilesizeFanart2
            {
                get
                {
                    return m_MaxFilesizeFanart2;
                }
                set
                {
                    m_MaxFilesizeFanart2 = value;
                    NotifyPropertyChanged("MaxFilesizeFanart2");
                }
            }

            private bool m_IsMaxQualityFanart2;
            public bool IsMaxQualityFanart2
            {
                get
                {
                    return m_IsMaxQualityFanart2;
                }
                set
                {
                    m_IsMaxQualityFanart2 = value;
                    NotifyPropertyChanged("IsMaxQualityFanart2");
                }
            }

            private double m_MaxFilesizeFanart3;
            public double MaxFilesizeFanart3
            {
                get
                {
                    return m_MaxFilesizeFanart3;
                }
                set
                {
                    m_MaxFilesizeFanart3 = value;
                    NotifyPropertyChanged("MaxFilesizeFanart3");
                }
            }

            private bool m_IsMaxQualityFanart3;
            public bool IsMaxQualityFanart3
            {
                get
                {
                    return m_IsMaxQualityFanart3;
                }
                set
                {
                    m_IsMaxQualityFanart3 = value;
                    NotifyPropertyChanged("IsMaxQualityFanart3");
                }
            }

            private bool m_IsResizingCover;
            public bool IsResizingCover
            {
                get
                {
                    return m_IsResizingCover;
                }
                set
                {
                    m_IsResizingCover = value;
                    NotifyPropertyChanged("IsResizingCover");
                }
            }

            private bool m_IsResizingBackground;
            public bool IsResizingBackground
            {
                get
                {
                    return m_IsResizingBackground;
                }
                set
                {
                    m_IsResizingBackground = value;
                    NotifyPropertyChanged("IsResizingBackground");
                }
            }

            private bool m_IsResizingFanart1;
            public bool IsResizingFanart1
            {
                get
                {
                    return m_IsResizingFanart1;
                }
                set
                {
                    m_IsResizingFanart1 = value;
                    NotifyPropertyChanged("IsResizingFanart1");
                }
            }

            private bool m_IsResizingFanart2;
            public bool IsResizingFanart2
            {
                get
                {
                    return m_IsResizingFanart2;
                }
                set
                {
                    m_IsResizingFanart2 = value;
                    NotifyPropertyChanged("IsResizingFanart2");
                }
            }

            private bool m_IsResizingFanart3;
            public bool IsResizingFanart3
            {
                get
                {
                    return m_IsResizingFanart3;
                }
                set
                {
                    m_IsResizingFanart3 = value;
                    NotifyPropertyChanged("IsResizingFanart3");
                }
            }

            private int m_CoverSizeWidth;
            public int CoverSizeWidth
            {
                get
                {
                    return m_CoverSizeWidth;
                }
                set
                {
                    m_CoverSizeWidth = value;
                    NotifyPropertyChanged("CoverSizeWidth");
                }
            }

            private int m_CoverSizeHeight;
            public int CoverSizeHeight
            {
                get
                {
                    return m_CoverSizeHeight;
                }
                set
                {
                    m_CoverSizeHeight = value;
                    NotifyPropertyChanged("CoverSizeHeight");
                }
            }

            private int m_BackgroundSizeWidth;
            public int BackgroundSizeWidth
            {
                get
                {
                    return m_BackgroundSizeWidth;
                }
                set
                {
                    m_BackgroundSizeWidth = value;
                    NotifyPropertyChanged("BackgroundSizeWidth");
                }
            }

            private int m_BackgroundSizeHeight;
            public int BackgroundSizeHeight
            {
                get
                {
                    return m_BackgroundSizeHeight;
                }
                set
                {
                    m_BackgroundSizeHeight = value;
                    NotifyPropertyChanged("BackgroundSizeHeight");
                }
            }

            private int m_Fanart1SizeWidth;
            public int Fanart1SizeWidth
            {
                get
                {
                    return m_Fanart1SizeWidth;
                }
                set
                {
                    m_Fanart1SizeWidth = value;
                    NotifyPropertyChanged("Fanart1SizeWidth");
                }
            }

            private int m_Fanart1SizeHeight;
            public int Fanart1SizeHeight
            {
                get
                {
                    return m_Fanart1SizeHeight;
                }
                set
                {
                    m_Fanart1SizeHeight = value;
                    NotifyPropertyChanged("Fanart1SizeHeight");
                }
            }

            private int m_Fanart2SizeWidth;
            public int Fanart2SizeWidth
            {
                get
                {
                    return m_Fanart2SizeWidth;
                }
                set
                {
                    m_Fanart2SizeWidth = value;
                    NotifyPropertyChanged("Fanart2SizeWidth");
                }
            }

            private int m_Fanart2SizeHeight;
            public int Fanart2SizeHeight
            {
                get
                {
                    return m_Fanart2SizeHeight;
                }
                set
                {
                    m_Fanart2SizeHeight = value;
                    NotifyPropertyChanged("Fanart2SizeHeight");
                }
            }

            private int m_Fanart3SizeWidth;
            public int Fanart3SizeWidth
            {
                get
                {
                    return m_Fanart3SizeWidth;
                }
                set
                {
                    m_Fanart3SizeWidth = value;
                    NotifyPropertyChanged("Fanart3SizeWidth");
                }
            }

            private int m_Fanart3SizeHeight;
            public int Fanart3SizeHeight
            {
                get
                {
                    return m_Fanart3SizeHeight;
                }
                set
                {
                    m_Fanart3SizeHeight = value;
                    NotifyPropertyChanged("Fanart3SizeHeight");
                }
            }

            private bool m_AutoExportFolderjpgAsCover;
            public bool AutoExportFolderjpgAsCover
            {
                get
                {
                    return m_AutoExportFolderjpgAsCover;
                }
                set
                {
                    m_AutoExportFolderjpgAsCover = value;
                    NotifyPropertyChanged("AutoExportFolderjpgAsCover");
                }
            }

            private string m_AutoExportFolderjpgAsCoverName;
            public string AutoExportFolderjpgAsCoverName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoExportFolderjpgAsCoverName);
                }
                set
                {
                    m_AutoExportFolderjpgAsCoverName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoExportFolderjpgAsCoverName");
                }
            }

            private bool m_AutoExportFanartjpgAsBackground;
            public bool AutoExportFanartjpgAsBackground
            {
                get
                {
                    return m_AutoExportFanartjpgAsBackground;
                }
                set
                {
                    m_AutoExportFanartjpgAsBackground = value;
                    NotifyPropertyChanged("AutoExportFanartjpgAsBackground");
                }
            }

            private string m_AutoExportFanartjpgAsBackgroundName;
            public string AutoExportFanartjpgAsBackgroundName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoExportFanartjpgAsBackgroundName);
                }
                set
                {
                    m_AutoExportFanartjpgAsBackgroundName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoExportFanartjpgAsBackgroundName");
                }
            }

            private bool m_AutoExportFanart1jpgAsBackground;
            public bool AutoExportFanart1jpgAsBackground
            {
                get
                {
                    return m_AutoExportFanart1jpgAsBackground;
                }
                set
                {
                    m_AutoExportFanart1jpgAsBackground = value;
                    NotifyPropertyChanged("AutoExportFanart1jpgAsBackground");
                }
            }

            private string m_AutoExportFanart1jpgAsBackgroundName;
            public string AutoExportFanart1jpgAsBackgroundName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoExportFanart1jpgAsBackgroundName);
                }
                set
                {
                    m_AutoExportFanart1jpgAsBackgroundName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoExportFanart1jpgAsBackgroundName");
                }
            }

            private bool m_AutoExportFanart2jpgAsBackground;
            public bool AutoExportFanart2jpgAsBackground
            {
                get
                {
                    return m_AutoExportFanart2jpgAsBackground;
                }
                set
                {
                    m_AutoExportFanart2jpgAsBackground = value;
                    NotifyPropertyChanged("AutoExportFanart2jpgAsBackground");
                }
            }

            private string m_AutoExportFanart2jpgAsBackgroundName;
            public string AutoExportFanart2jpgAsBackgroundName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoExportFanart2jpgAsBackgroundName);
                }
                set
                {
                    m_AutoExportFanart2jpgAsBackgroundName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoExportFanart2jpgAsBackgroundName");
                }
            }

            private bool m_AutoExportFanart3jpgAsBackground;
            public bool AutoExportFanart3jpgAsBackground
            {
                get
                {
                    return m_AutoExportFanart3jpgAsBackground;
                }
                set
                {
                    m_AutoExportFanart3jpgAsBackground = value;
                    NotifyPropertyChanged("AutoExportFanart3jpgAsBackground");
                }
            }

            private string m_AutoExportFanart3jpgAsBackgroundName;
            public string AutoExportFanart3jpgAsBackgroundName
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_AutoExportFanart3jpgAsBackgroundName);
                }
                set
                {
                    m_AutoExportFanart3jpgAsBackgroundName = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("AutoExportFanart3jpgAsBackgroundName");
                }
            }

            private bool m_OverwriteExistingCover;
            public bool OverwriteExistingCover
            {
                get
                {
                    return m_OverwriteExistingCover;
                }
                set
                {
                    m_OverwriteExistingCover = value;
                    NotifyPropertyChanged("OverwriteExistingCover");
                }
            }

            private bool m_OverwriteExistingBackground;
            public bool OverwriteExistingBackground
            {
                get
                {
                    return m_OverwriteExistingBackground;
                }
                set
                {
                    m_OverwriteExistingBackground = value;
                    NotifyPropertyChanged("OverwriteExistingBackground");
                }
            }

            private bool m_OverwriteExistingFanart1;
            public bool OverwriteExistingFanart1
            {
                get
                {
                    return m_OverwriteExistingFanart1;
                }
                set
                {
                    m_OverwriteExistingFanart1 = value;
                    NotifyPropertyChanged("OverwriteExistingFanart1");
                }
            }

            private bool m_OverwriteExistingFanart2;
            public bool OverwriteExistingFanart2
            {
                get
                {
                    return m_OverwriteExistingFanart2;
                }
                set
                {
                    m_OverwriteExistingFanart2 = value;
                    NotifyPropertyChanged("OverwriteExistingFanart2");
                }
            }

            private bool m_OverwriteExistingFanart3;
            public bool OverwriteExistingFanart3
            {
                get
                {
                    return m_OverwriteExistingFanart3;
                }
                set
                {
                    m_OverwriteExistingFanart3 = value;
                    NotifyPropertyChanged("OverwriteExistingFanart3");
                }
            }

            private bool m_PreserveAspectRatioCover;
            public bool PreserveAspectRatioCover
            {
                get
                {
                    return m_PreserveAspectRatioCover;
                }
                set
                {
                    m_PreserveAspectRatioCover = value;
                    NotifyPropertyChanged("PreserveAspectRatioCover");
                }
            }

            private bool m_PreserveAspectRatioBackground;
            public bool PreserveAspectRatioBackground
            {
                get
                {
                    return m_PreserveAspectRatioBackground;
                }
                set
                {
                    m_PreserveAspectRatioBackground = value;
                    NotifyPropertyChanged("PreserveAspectRatioBackground");
                }
            }

            private bool m_PreserveAspectRatioFanart1;
            public bool PreserveAspectRatioFanart1
            {
                get
                {
                    return m_PreserveAspectRatioFanart1;
                }
                set
                {
                    m_PreserveAspectRatioFanart1 = value;
                    NotifyPropertyChanged("PreserveAspectRatioFanart1");
                }
            }

            private bool m_PreserveAspectRatioFanart2;
            public bool PreserveAspectRatioFanart2
            {
                get
                {
                    return m_PreserveAspectRatioFanart2;
                }
                set
                {
                    m_PreserveAspectRatioFanart2 = value;
                    NotifyPropertyChanged("PreserveAspectRatioFanart2");
                }
            }

            private bool m_PreserveAspectRatioFanart3;
            public bool PreserveAspectRatioFanart3
            {
                get
                {
                    return m_PreserveAspectRatioFanart3;
                }
                set
                {
                    m_PreserveAspectRatioFanart3 = value;
                    NotifyPropertyChanged("PreserveAspectRatioFanart3");
                }
            }

            private string m_CoverExtension;
            public string CoverExtension
            {
                get
                {
                    return m_CoverExtension;
                }
                set
                {
                    m_CoverExtension = value;
                    NotifyPropertyChanged("CoverExtension");
                }
            }

            private string m_BackgroundExtension;
            public string BackgroundExtension
            {
                get
                {
                    return m_BackgroundExtension;
                }
                set
                {
                    m_BackgroundExtension = value;
                    NotifyPropertyChanged("BackgroundExtension");
                }
            }

            private string m_Fanart1Extension;
            public string Fanart1Extension
            {
                get
                {
                    return m_Fanart1Extension;
                }
                set
                {
                    m_Fanart1Extension = value;
                    NotifyPropertyChanged("Fanart1Extension");
                }
            }

            private string m_Fanart2Extension;
            public string Fanart2Extension
            {
                get
                {
                    return m_Fanart2Extension;
                }
                set
                {
                    m_Fanart2Extension = value;
                    NotifyPropertyChanged("Fanart2Extension");
                }
            }

            private string m_Fanart3Extension;
            public string Fanart3Extension
            {
                get
                {
                    return m_Fanart3Extension;
                }
                set
                {
                    m_Fanart3Extension = value;
                    NotifyPropertyChanged("Fanart3Extension");
                }
            }


            public ExportImages()
            {
                IsMaxQuality = false;
                MaxFilesize = 100000d;
                IsMaxQualityBackground = false;
                MaxFilesizeBackground = 100000d;
                IsMaxQualityFanart1 = false;
                MaxFilesizeFanart1 = 100000d;
                IsMaxQualityFanart2 = false;
                MaxFilesizeFanart2 = 100000d;
                IsMaxQualityFanart3 = false;
                MaxFilesizeFanart3 = 100000d;

                IsResizingCover = false;
                IsResizingBackground = false;
                IsResizingFanart1 = false;
                IsResizingFanart2 = false;
                IsResizingFanart3 = false;
                CoverSizeWidth = 0;
                CoverSizeHeight = 0;
                BackgroundSizeWidth = 0;
                BackgroundSizeHeight = 0;

                AutoExportFanartjpgAsBackgroundName = @"$M\fanart";
                AutoExportFanartjpgAsBackground = false;
                AutoExportFolderjpgAsCoverName = @"$M\cover";
                AutoExportFanart1jpgAsBackground = false;
                AutoExportFanart1jpgAsBackgroundName = @"$M\fanart1";
                AutoExportFanart2jpgAsBackground = false;
                AutoExportFanart2jpgAsBackgroundName = @"$M\fanart2";
                AutoExportFanart3jpgAsBackground = false;
                AutoExportFanart3jpgAsBackgroundName = @"$M\fanart3";
                AutoExportFolderjpgAsCover = false;

                OverwriteExistingCover = true;
                OverwriteExistingBackground = true;
                OverwriteExistingFanart1 = true;
                OverwriteExistingFanart2 = true;
                OverwriteExistingFanart3 = true;

                PreserveAspectRatioCover = true;
                PreserveAspectRatioBackground = true;
                PreserveAspectRatioFanart1 = true;
                PreserveAspectRatioFanart2 = true;
                PreserveAspectRatioFanart3 = true;

                CoverExtension = null;
                BackgroundExtension = null;
                Fanart1Extension = null;
                Fanart2Extension = null;
                Fanart3Extension = null;

            }
        }

        [Serializable]
        public class Watermark : BaseNotifyPropertyChanged
        {
            private string m_FontFamily;
            public string FontFamily
            {
                get
                {
                    return m_FontFamily;
                }
                set
                {
                    m_FontFamily = value;
                    NotifyPropertyChanged("FontFamily");
                }
            }

            private int m_FontSize;
            public int FontSize
            {
                get
                {
                    return m_FontSize;
                }
                set
                {
                    m_FontSize = value;
                    NotifyPropertyChanged("FontSize");
                }
            }

            private int m_FontColorIndex;
            public int FontColorIndex
            {
                get
                {
                    return m_FontColorIndex;
                }
                set
                {
                    m_FontColorIndex = value;
                    NotifyPropertyChanged("FontColorIndex");
                }
            }

            private Size m_Position;
            public Size Position
            {
                get
                {
                    return m_Position;
                }
                set
                {
                    m_Position = value;
                    NotifyPropertyChanged("Position");
                }
            }

            private string m_Text;
            public string Text
            {
                get
                {
                    return HttpUtility.HtmlDecode(m_Text);
                }

                set
                {
                    m_Text = HttpUtility.HtmlEncode(value);
                    NotifyPropertyChanged("Text");
                }
            }

            private bool m_Bold;
            public bool Bold
            {
                get
                {
                    return m_Bold;
                }
                set
                {
                    m_Bold = value;
                    NotifyPropertyChanged("Bold");
                }
            }

            private bool m_Italic;
            public bool Italic
            {
                get
                {
                    return m_Italic;
                }
                set
                {
                    m_Italic = value;
                    NotifyPropertyChanged("Italic");
                }
            }

            public Watermark()
            {
                FontFamily = "Tahoma";
                FontSize = 32;
                FontColorIndex = 0;
                Position = new Size(0, 0);
                Text = "my text here";
                Bold = false;
                Italic = false;
            }
        }

        public UserOptions()
            : this(true, true, false, false, false, string.Empty, string.Empty)
        {

        }

        public UserOptions(bool overwriteExisting, bool recurse, bool prompt, bool useFolderName, bool useBlacklist, string blacklist, string collectors)
        {
            MTNPath = string.Empty;
            AutogenerateFolderJpg = false;
            AutogenerateMovieInfo = false;
            AutogenerateMovieSheet = false;
            AutogenerateMoviesheetForFolder = false;
            AutogenerateMoviesheetForParentFolder = false;
            AutogenerateMoviesheetMetadata = true;
            AutogenerateThumbnail = true;
            GenerateParentFolderMetadata = false;
            RetrieveEpisodeScreenshots = false;
            RetrieveBannersAsBackdrops = false;
            AutoCheckUpdates = false;
            LastSelectedFolder = string.Empty;
            DisableSearch = false;
            DisableMediaInfoProcessing = false;
            UseIMDbIdWherePossible = true;
            UseMovieHashWherePossible = false;
            OverwriteExistingThumbs = overwriteExisting;
            RecurseFolders = recurse;
            PromptBeforeSearch = prompt;
            UserDefinedFilesFilter = DEFAULT_DVD_FILTER;
            UseFolderNamesForDetection = useFolderName;
            UseBlacklist = useBlacklist;
            SwitchOffInternalNoiseRemover = false;
            Blacklist = blacklist;
            CustomMovieExtensions = string.Empty;
            SkipFoldersHavingStrings = string.Empty;
            Collectors = collectors;
            BatchAutoMask = "S$S E$E";
            KeepAspectRatio = true;
            EnableMultiCoreSupport = Environment.ProcessorCount > 1;
            ThumbnailSize = new Size(200, 300);
            AddWatermark = false;
            SemiautomaticTimeout = 10;
            UpdateIMDbRating = false;
            PreferedInfoCollector = BaseCollector.THEMOVIEDB;
            LastBackdropSelectedFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            LastCoverSelectedFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            LastProfileUsed = Path.GetFileNameWithoutExtension(Configuration.ConfigFilePath);
            LastMovieResultsLayoutUsed = string.Empty;
            SaveOriginalCoverAsExtraThumbnail = false;
            ExportNfoAsTvixie = false;
            ExportNfoAsXBMC = false;
            ExportNfoAsWDTVHUB = false;
            ExportNfoAsWDTVHUB_V2 = false;
            ExportNfoAsThumbGen = true;
            PutFullMediaInfoToExportedNfo = false;
            GenerateDummyFile = false;
            PreviewType = ThumbGen.PreviewType.BestFit;
            CustomDateFormat = "dd.MM.yyyy";
            ShowAllCollectors = true;
            EnableSmartOutputForDVDBRAY = true;
            EnableExportFromMetadata = false;
            SkipFoldersStartingWithDot = false;

            WatermarkOptions = new Watermark();
            SubtitlesOptions = new Subtitles();
            ConnectionOptions = new Connection();
            MovieSheetsOptions = new MovieSheets();
            FileBrowserOptions = new FileBrowser();
            WindowsOptions = new WindowSettings();
            NamingOptions = new NamingConventions();
            PlaylistOptions = new Playlists();
            IMDBOptions = new IMDB();
            ImportOptions = new Import();
            SSHOptions = new SSH();
            TelnetOptions = new Telnet();
            ExportImagesOptions = new ExportImages();
            TVShowsFiltersOptions = new TVShowsFilters();
            CinePassionOptions = new CinePassion();
        }

        public string Save()
        {
            string _result = string.Empty;

            XmlSerializer _xs = new XmlSerializer(typeof(UserOptions));
            using (MemoryStream _ms = new MemoryStream())
            {
                try
                {
                    _xs.Serialize(_ms, this);
                    byte[] _b = _ms.ToArray();
                    _result = System.Text.Encoding.UTF8.GetString(_b);
                }
                catch { }
            }

            return _result;
        }
    }

    public class Configuration : BaseNotifyPropertyChanged
    {
        private UserOptions m_Options;
        public UserOptions Options
        {
            get
            {
                return m_Options;
            }
            set
            {
                m_Options = value;
                NotifyPropertyChanged("Options");
            }
        }

        public IWebProxy Proxy = WebRequest.DefaultWebProxy;

        private static string configFilePath = null;
        public static string ConfigFilePath
        {
            get
            {
                if (configFilePath == null)
                {
                    // initially config.xml was stored in the ApplicationData/ThumbGen
                    // act backward compat and move it silently into the new location, near the exe (ThumbGen.tgp)
                    var _old = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"ThumbGen\config.xml");
                    //var _new = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ThumbGen.tgp");
                    var _new = Path.ChangeExtension(Assembly.GetEntryAssembly().Location, ".tgp");
                    if (!File.Exists(_new) && File.Exists(_old))
                    {
                        try
                        {
                            File.Copy(_old, _new, true);
                            File.Delete(_old);
                        }
                        catch (Exception ex)
                        {
                            Loggy.Logger.Error("Could not delete old default profile...", ex.Message);
                        }
                    }
                    configFilePath = _new;
                }
                return configFilePath;
                //return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"ThumbGen\config.xml");
            }
        }

        public Configuration()
            : this(null)
        {

        }

        public Configuration(UserOptions options)
        {
            if (options != null)
            {
                Options = options;
            }
            else
            {
                Options = new UserOptions();
            }
        }

        public void SaveConfiguration()
        {
            SaveConfiguration(ConfigFilePath);
        }

        public void SaveConfiguration(string targetFile)
        {
            if(string.IsNullOrEmpty(targetFile)) return;
            if (!Directory.Exists(Path.GetDirectoryName(targetFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
            }
            XmlSerializer _xs = new XmlSerializer(typeof(UserOptions));
            try
            {
                using (FileStream _fs = new FileStream(targetFile, FileMode.Create, FileAccess.ReadWrite))
                {
                    try
                    {
                        _xs.Serialize(_fs, this.Options);
                    }
                    catch
                    {
                    }
                }
            }
            catch(UnauthorizedAccessException uae)
            {
                MessageBox.Show(uae.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadConfiguration()
        {
            LoadConfiguration(ConfigFilePath);
        }

        public void LoadConfiguration(string sourceFile)
        {
            if (string.IsNullOrEmpty(sourceFile)) return;
            XmlSerializer _xs = new XmlSerializer(typeof(UserOptions));
            if (File.Exists(sourceFile))
            {
                using (FileStream _fs = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        this.Options = _xs.Deserialize(_fs) as UserOptions;
                        // reset some values
                        //this.Options.FileBrowserOptions.FilterWithoutExtSubtitles = false;
                        //this.Options.FileBrowserOptions.FilterWithoutMovieInfo = false;
                        //this.Options.FileBrowserOptions.FilterWithoutMoviesheet = false;
                        //this.Options.FileBrowserOptions.FilterWithoutExtraMoviesheet = false;
                        //this.Options.FileBrowserOptions.FilterWithoutFolderJpg = false;
                        //this.Options.FileBrowserOptions.FilterWithoutThumbnail = false;
                        //this.Options.FileBrowserOptions.FilterOnlyFirstMovieInFolder = false;
                        //
                        if (this.Options.MovieSheetsOptions.MovieInfoPriorities.Count == 0)
                        {
                            this.Options.MovieSheetsOptions.MovieInfoPriorities.Add(MovieInfoProviderItemType.PrefCollector);
                            this.Options.MovieSheetsOptions.MovieInfoPriorities.Add(MovieInfoProviderItemType.MyOwn);
                            this.Options.MovieSheetsOptions.MovieInfoPriorities.Add(MovieInfoProviderItemType.Metadata);
                            this.Options.MovieSheetsOptions.MovieInfoPriorities.Add(MovieInfoProviderItemType.IMDB);
                        }
                        this.Options.MovieSheetsOptions.Count = 2;

                        if (this.Options.PlaylistsJobs.Count == 0)
                        {
                            this.Options.PlaylistsJobs.Add(this.Options.PlaylistOptions);
                        }

                        if (string.IsNullOrEmpty(this.Options.NamingOptions.MovieinfoExportExtension))
                        {
                            this.Options.NamingOptions.MovieinfoExportExtension = this.Options.NamingOptions.MovieinfoExtension;
                        }
                        if (string.IsNullOrEmpty(this.Options.NamingOptions.MovieinfoExportMask))
                        {
                            this.Options.NamingOptions.MovieinfoExportMask = this.Options.NamingOptions.MovieinfoMask;
                        }

                        PatchAutoexportNames();

                    }
                    catch { }
                }
            }

            try
            {
                RefreshProxy();
            }
            catch
            {
            }

            NotifyPropertyChanged("Options");
        }

        private void PatchAutoexportNames()
        {
            UserOptions.ExportImages _exp = this.Options.ExportImagesOptions;

            Func<string, string, string> _GetExt = (oldName, defaultExt) =>
            {
                string _e = Path.GetExtension(oldName);
                return !string.IsNullOrEmpty(_e) ? _e : defaultExt;
            };

            Func<string, string> _GetNewName = (oldName) =>
            {
                try
                {
                    return Path.Combine(Path.GetDirectoryName(oldName), Path.GetFileNameWithoutExtension(oldName));
                }
                catch { return oldName; }
            };

            if (_exp.CoverExtension == null)
            {
                _exp.CoverExtension = _GetExt(_exp.AutoExportFolderjpgAsCoverName, ".jpg");
                _exp.AutoExportFolderjpgAsCoverName = _GetNewName(_exp.AutoExportFolderjpgAsCoverName);
            }
            if (_exp.BackgroundExtension == null)
            {
                _exp.BackgroundExtension = _GetExt(_exp.AutoExportFanartjpgAsBackgroundName, ".jpg");
                _exp.AutoExportFanartjpgAsBackgroundName = _GetNewName(_exp.AutoExportFanartjpgAsBackgroundName);
            }
            if (_exp.Fanart1Extension == null)
            {
                _exp.Fanart1Extension = _GetExt(_exp.AutoExportFanart1jpgAsBackgroundName, ".jpg");
                _exp.AutoExportFanart1jpgAsBackgroundName = _GetNewName(_exp.AutoExportFanart1jpgAsBackgroundName);
            }
            if (_exp.Fanart2Extension == null)
            {
                _exp.Fanart2Extension = _GetExt(_exp.AutoExportFanart2jpgAsBackgroundName, ".jpg");
                _exp.AutoExportFanart2jpgAsBackgroundName = _GetNewName(_exp.AutoExportFanart2jpgAsBackgroundName);
            }
            if (_exp.Fanart3Extension == null)
            {
                _exp.Fanart3Extension = _GetExt(_exp.AutoExportFanart3jpgAsBackgroundName, ".jpg");
                _exp.AutoExportFanart3jpgAsBackgroundName = _GetNewName(_exp.AutoExportFanart3jpgAsBackgroundName);
            }
        }

        public void StoreLastUsedProfile()
        {
            try
            {
                // store it always in config.xml
                XmlSerializer _xs = new XmlSerializer(typeof (UserOptions));
                UserOptions _options = null;
                using (FileStream _fs = new FileStream(ConfigFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    try
                    {
                        _options = _xs.Deserialize(_fs) as UserOptions;
                        _options.LastProfileUsed = HttpUtility.HtmlEncode(FileManager.ProfilesMan.SelectedProfile.ProfilePath);
                    }
                    catch
                    {
                    }
                }
                if (_options != null)
                {
                    using (FileStream _fs = new FileStream(ConfigFilePath, FileMode.Create, FileAccess.Write))
                    {
                        try
                        {
                            _xs.Serialize(_fs, _options);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Loggy.Logger.Error(ex.Message);
            }
        }

        public ProfileItem GetLastUsedProfile()
        {
            ProfileItem _result = new ProfilesManager().CreateProfileItem(ConfigFilePath);

            if (File.Exists(ConfigFilePath))
            {
                XmlSerializer _xs = new XmlSerializer(typeof(UserOptions));
                UserOptions _options = null;
                using (FileStream _fs = new FileStream(ConfigFilePath, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        _options = _xs.Deserialize(_fs) as UserOptions;
                    }
                    catch { }
                }
                if (_options != null)
                {
                    if (!string.IsNullOrEmpty(_options.LastProfileUsed))
                    {
                        var profilePath = _options.LastProfileUsed;
                        if (!Path.IsPathRooted(profilePath))
                        {
                            profilePath = Path.Combine(FileManager.GetProfilesFolder(), _options.LastProfileUsed);
                        }
                        if (File.Exists(profilePath))
                        {
                            return new ProfilesManager().CreateProfileItem(HttpUtility.HtmlDecode(profilePath));
                        }
                    }
                }
            }
            return _result;
        }

        public void RefreshProxy()
        {
            switch (this.Options.ConnectionOptions.Type)
            {
                case UserOptions.ConnectionType.UseIE:
                    this.Proxy = WebRequest.GetSystemWebProxy();
                    break;
                case UserOptions.ConnectionType.Direct:
                    this.Proxy = null;
                    break;
                case UserOptions.ConnectionType.Proxy:
                    if (!string.IsNullOrEmpty(this.Options.ConnectionOptions.ProxyHost))
                    {
                        if (!string.IsNullOrEmpty(this.Options.ConnectionOptions.ProxyPort))
                        {
                            this.Proxy = new WebProxy(this.Options.ConnectionOptions.ProxyHost, Int32.Parse(this.Options.ConnectionOptions.ProxyPort));
                        }
                        else
                        {
                            this.Proxy = new WebProxy(this.Options.ConnectionOptions.ProxyHost);
                        }
                        if (!string.IsNullOrEmpty(this.Options.ConnectionOptions.ProxyUser) &&
                            !string.IsNullOrEmpty(this.Options.ConnectionOptions.ProxyPass))
                        {
                            this.Proxy.Credentials = new NetworkCredential(this.Options.ConnectionOptions.ProxyUser, this.Options.ConnectionOptions.ProxyPass);
                        }
                    }
                    else
                    {
                        this.Proxy = null;
                    }
                    break;
                default:
                    break;
            }

            WebRequest.DefaultWebProxy = this.Proxy;
        }

        public void Reset()
        {
            this.Options = new UserOptions();
            SaveConfiguration(FileManager.ProfilesMan.SelectedProfile.ProfilePath);
        }


        private List<CultureInfo> m_Languages = null;
        public List<CultureInfo> GetLanguages()
        {
            if (m_Languages == null)
            {
                m_Languages = new List<CultureInfo>();
                CultureInfo[] _cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                foreach (CultureInfo _ci in _cultures)
                {
                    if (!string.IsNullOrEmpty(_ci.TwoLetterISOLanguageName) && !_ci.Name.Contains("-") && !string.IsNullOrEmpty(_ci.TwoLetterISOLanguageName) && _ci.TwoLetterISOLanguageName != "iv")
                    {
                        m_Languages.Add(_ci);
                    }
                }
            }
            return m_Languages;
        }

        public bool HasFolderStringToBeSkipped(string path)
        {
            bool _result = false;
            // skip the ones that have the restricted string inside
            if (!string.IsNullOrEmpty(FileManager.Configuration.Options.SkipFoldersHavingStrings))
            {
                string[] _skips = FileManager.Configuration.Options.SkipFoldersHavingStrings.ToLowerInvariant().Split(',');
                if (_skips != null && _skips.Count() != 0)
                {
                    foreach (string _s in _skips)
                    {
                        if (path.ToLowerInvariant().Contains(_s))
                        {
                            _result = true;
                            break;
                        }
                    }
                }
            }

            if (!_result)
            {
                try
                {
                    // check also the RegEx (if provided)
                    if (!string.IsNullOrEmpty(FileManager.Configuration.Options.UserDefinedFilesFilter))
                    {
                        if (Regex.IsMatch(path, FileManager.Configuration.Options.UserDefinedFilesFilter, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                        {
                            _result = true;
                        }
                    }
                }
                catch { }
            }

            return _result;
        }

        public static bool NeedsToExcludeStartingWithDot(string folderName)
        {
            return string.IsNullOrEmpty(folderName) ? false : folderName.StartsWith(".") && FileManager.Configuration.Options.SkipFoldersStartingWithDot;
        }

        public bool HasMoviesheet(string movieFilename)
        {
            try
            {
                return File.Exists(GetMoviesheetPath(movieFilename, false));
            }
            catch
            {
                return false;
            }
        }

        public bool HasExtraMoviesheet(string movieFilename)
        {
            try
            {
                return File.Exists(GetMoviesheetForFolderPath(movieFilename, false));
            }
            catch
            {
                return false;
            }
        }

        public bool HasThumbnail(string movieFilename)
        {
            try
            {
                return File.Exists(GetThumbnailPath(movieFilename, false));
            }
            catch
            {
                return false;
            }
        }

        public bool HasFolderJpg(string movieFilename)
        {
            try
            {
                return File.Exists(GetFolderjpgPath(movieFilename, false));
            }
            catch
            {
                return false;
            }
        }

        public bool HasMoviesheetMetadata(string movieFilename)
        {
            try
            {
                return File.Exists(GetMoviesheetMetadataPath(movieFilename, false));
            }
            catch
            {
                return false;
            }
        }

        public string GetThumbnailPath(string movieFilename, bool forcePath)
        {
            return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.ThumbnailMask, this.Options.NamingOptions.ThumbnailExtension, forcePath);
        }

        public string GetFolderjpgPath(string movieFilename, bool forcePath)
        {
            return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.FolderjpgMask, this.Options.NamingOptions.FolderjpgExtension, forcePath);
        }

        public string GetMovieInfoPath(string movieFilename, bool forcePath, MovieinfoType infotype)
        {
            switch (infotype)
            {
                default:
                case MovieinfoType.Import:
                    return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.MovieinfoMask, this.Options.NamingOptions.MovieinfoExtension, forcePath);

                case MovieinfoType.Export:
                    return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.MovieinfoExportMask, this.Options.NamingOptions.MovieinfoExportExtension, forcePath);
            }

        }

        public string GetMoviesheetPath(string movieFilename, bool forcePath)
        {
            return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.MoviesheetMask, this.Options.NamingOptions.MoviesheetExtension, forcePath);
        }

        public string GetMoviesheetForFolderPath(string movieFilename, bool forcePath)
        {
            return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.MoviesheetForFolderMask, this.Options.NamingOptions.MoviesheetForFolderExtension, forcePath);
        }

        public string GetMoviesheetForParentFolderPath(string movieFilename, bool forcePath)
        {
            return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.MoviesheetForParentFolderMask, this.Options.NamingOptions.MoviesheetForParentFolderExtension, forcePath);
        }

        public string GetMoviesheetMetadataPath(string movieFilename, bool forcePath)
        {
            return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.MoviesheetMetadataMask, this.Options.NamingOptions.MoviesheetMetadataExtension, forcePath);
        }

        public string GetParentFolderMetadataPath(string movieFilename, bool forcePath)
        {
            return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.ParentFolderMetadataMask, this.Options.NamingOptions.MoviesheetMetadataExtension, forcePath);
        }

        public string GetDummyFilePath(string movieFilename)
        {
            return ConfigHelpers.ConstructPath(movieFilename, this.Options.NamingOptions.DummyFileMask, "", true);
        }
    }


    public enum MovieinfoType
    {
        Import,
        Export
    }
}
