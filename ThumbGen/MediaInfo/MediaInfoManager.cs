using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaInfoLib;
using System.IO;
using System.Windows;
using System.Globalization;
using ThumbGen.Subtitles;
using System.Threading;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Reflection;
using System.Xml;
using DiscUtils;

namespace ThumbGen
{
    internal class MediaInfoManager
    {
        public static bool AppendFullMediaInfoToNfoFile(MovieInfo info, string movieFilepath, Stream output)
        {
            bool _result = false;
            // try to add fullmediainfo to a ThumbGen generated .nfo file
            if (info != null)
            {
                using (MemoryStream _ms = new MemoryStream())
                {
                    // save MovieInfo to stream
                    info.Save(_ms, movieFilepath, false);
                    _ms.Position = 0;
                    // load the stream into _nfo 
                    XmlDocument _nfo = new XmlDocument();
                    _nfo.Load(_ms);
                    // load the full media info into _media
                    XmlDocument _media = new XmlDocument();
                    string _tmpMediaDataXml = null;
                    MediaInfoManager.GetMediaInfoData(movieFilepath, true, true, false, out _tmpMediaDataXml);
                    if (!string.IsNullOrEmpty(_tmpMediaDataXml))
                    {
                        _media.LoadXml(_tmpMediaDataXml);
                    }
                    if (_media.DocumentElement != null)
                    {
                        // import the fullmedia in the _nfo document
                        XmlNode _nodeDest2 = _nfo.ImportNode(_media.DocumentElement, true);
                        _nfo.DocumentElement.AppendChild(_nodeDest2);
                    }

                    if (output != null)
                    {
                        output.Position = 0;
                        _nfo.Save(output);
                        output.Position = 0;
                        _result = true;
                    }
                }
            }

            return _result;
        }

        public static void AppendFullMediaInfoToNfoFile(string nfoFilepath, string movieFilepath)
        {
            try
            {
                nfoFileType _out = nfoFileType.Unknown;
                MovieInfo _info = nfoHelper.LoadNfoFile(movieFilepath, nfoFilepath, out _out);
                if (_out == nfoFileType.ThumbGen)
                {
                    using (MemoryStream _ms = new MemoryStream())
                    {
                        if (AppendFullMediaInfoToNfoFile(_info, movieFilepath, _ms))
                        {
                            if (_ms.Length != 0)
                            {
                                _ms.Position = 0;
                                using (FileStream _fs = new FileStream(nfoFilepath, FileMode.Open, FileAccess.ReadWrite))
                                {
                                    _ms.CopyTo(_fs);
                                }
                            }
                        }
                        else
                        {
                            Loggy.Logger.Debug("Appending mediainfo failed");
                        }

                    }
                }
                else
                {
                    Loggy.Logger.Debug("Will not add full mediainfo to a non-ThumbGen nfo file: " + nfoFilepath);
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Cannot add full mediainfo to " + nfoFilepath, ex);
            }
        }

        private static string RedirectISO(string filename)
        {
            string _result = null;

            try
            {
                if (!string.IsNullOrEmpty(filename) && File.Exists(filename))
                {
                    List<string> _candidates = new List<string>();

                    // open the ISO file
                    VolumeManager volMgr = new VolumeManager();
                    volMgr.AddDisk(VirtualDisk.OpenDisk(filename, FileAccess.Read));

                    VolumeInfo volInfo = null;
                    volInfo = volMgr.GetLogicalVolumes()[0];

                    DiscUtils.FileSystemInfo fsInfo = FileSystemManager.DetectDefaultFileSystems(volInfo)[0];

                    using (DiscFileSystem _dfs = fsInfo.Open(volInfo))
                    {
                        foreach (string _fname in _dfs.GetFiles("", "*.*", SearchOption.AllDirectories))
                        {
                            string _fileExt = System.IO.Path.GetExtension(_fname).ToLowerInvariant();
                            if (_fileExt == ".ifo" || _fileExt == ".mpls")
                            {
                                _candidates.Add(_fname);
                            }
                        }

                        double _biggestDuration = 0d;
                        string _tmpBRResult = "";
                        // select from the candidates the one that has the longest duration (if mpls skip files bigger than 10K)
                        foreach (string _cpath in _candidates)
                        {
                            string _cext = Path.GetExtension(_cpath).ToLowerInvariant();
                            string _tmp = Helpers.GetUniqueFilename(_cext);
                            using (FileStream _fs = new FileStream(_tmp, FileMode.Create, FileAccess.Write))
                            {
                                using (Stream source = _dfs.OpenFile(_cpath, FileMode.Open, FileAccess.Read))
                                {
                                    source.CopyTo(_fs);
                                }
                            }

                            // if it is a DVD iso
                            if (_cext == ".ifo")
                            {
                                if (string.Compare(Path.GetFileNameWithoutExtension(_cpath), "video_ts", true) == 0)
                                {
                                    File.Delete(_tmp);
                                    // skip the menu
                                    continue;
                                }

                                // use first ifo that is not the menu
                                FileManager.AddToGarbageFiles(_tmp);
                                _result = _tmp;
                                break;
                            }

                            // if it is a BLURAY iso (choose biggest mpls file)
                            if (_cext == ".mpls")
                            {
                                long _length = new FileInfo(_tmp).Length;
                                if (Path.GetExtension(_cpath).ToLowerInvariant() == ".mpls" && _length > 10 * 1024)
                                {
                                    File.Delete(_tmp);
                                    continue; // take next one, this is too big and mediainfo will hang
                                }

                                if (GetDurationMilliseconds(_tmp) > _biggestDuration)
                                {
                                    // important.. add it to the garbage files
                                    //FileManager.AddToGarbageFiles(_tmp);
                                    //_result = _tmp;
                                    if (!string.IsNullOrEmpty(_tmpBRResult))
                                    {
                                        File.Delete(_tmpBRResult); // remove previous winner and remember the current one
                                    }
                                    _tmpBRResult = _tmp;
                                }
                                else
                                {
                                    File.Delete(_tmp);
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(_result) && !string.IsNullOrEmpty(_tmpBRResult))
                        {
                            FileManager.AddToGarbageFiles(_tmpBRResult);
                            _result = _tmpBRResult;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("ISO Processing", ex);
            }

            return _result;
        }

        public static bool IsISO(string filePath)
        {
            return Path.GetExtension(filePath).ToLowerInvariant().Contains("iso");
        }

        const double _16_9 = 1.778;
        const double _2_35 = 2.35;

        // get the duration as double
        private static double GetDuration(MediaInfo mi)
        {
            if (mi != null)
            {
                return MediaInfoData.GetDurationFromString(mi.Get(StreamKind.General, 0, "Duration"));
            }
            else
            {
                return 0d;
            }
        }

        public static double GetDurationMilliseconds(string filePath)
        {
            double _result = 0;

            try
            {
                MediaInfo _mi = new MediaInfo();
                _mi.Open(filePath);
                _result = GetDuration(_mi);
                _mi.Close();
            }
            catch { }

            return _result;
        }

        public static void GetDurationAndVideoResolution(string filePath, out double duration, out Size resolution)
        {
            duration = 0d;
            resolution = new Size(0, 0);
            try
            {
                MediaInfo _mi = new MediaInfo();
                _mi.Open(filePath);
                duration = GetDuration(_mi);

                string _height = _mi.Get(StreamKind.Video, 0, "Height");
                string _width = _mi.Get(StreamKind.Video, 0, "Width");
                resolution.Width = Int32.Parse(_width);
                resolution.Height = Int32.Parse(_height);

                _mi.Close();

            }
            catch { }

        }

        public static Size GetVideoResolution(string filePath)
        {
            Size _result = new Size(0, 0);

            try
            {
                MediaInfo _mi = new MediaInfo();
                _mi.Open(filePath);
                string _height = _mi.Get(StreamKind.Video, 0, "Height");
                string _width = _mi.Get(StreamKind.Video, 0, "Width");
                _result.Width = Int32.Parse(_width);
                _result.Height = Int32.Parse(_height);
                _mi.Close();

            }
            catch { }

            return _result;
        }

        private static string GetCodecInfoText(MediaInfo mi, StreamKind kind)
        {
            string _result = mi.Get(kind, 0, "CodecID/Hint");
            if (string.IsNullOrEmpty(_result))
            {
                _result = mi.Get(kind, 0, "Format");
            }
            return _result;
        }

        private static void SetFormat(MediaInfoData result, MediaInfo mi, string filePath)
        {
            if (mi != null)
            {
                string _format = mi.Get(StreamKind.General, 0, "Format");
                string _formatCodec = mi.Get(StreamKind.General, 0, "CodecID");
                string _formatProfile = mi.Get(StreamKind.General, 0, "Format_Profile").ToLowerInvariant();

                // store the untouche
                result.ContainerFormat = _format;

                if (!string.IsNullOrEmpty(_format))
                {
                    _format = _format.ToLowerInvariant();
                }
                if (_format.Contains("mpeg") || _format.Contains("avi") || _format.Contains("divx"))
                {
                    result.Format.Flag = MediaInfoFlags.Format_Mpeg;
                }
                if (_format.Contains("matroska"))
                {
                    result.Format.Flag = MediaInfoFlags.Format_MKV;
                }

                if (_format.Contains("dvd"))
                {
                    result.Format.Flag = MediaInfoFlags.Format_DVD;
                }

                if (Path.GetExtension(filePath).ToLowerInvariant().Contains("m2ts") || _format.Contains("blu-ray"))
                {
                    result.Format.Flag = MediaInfoFlags.Format_Bluray;
                }

                if (_formatCodec.Contains("qt") || _formatProfile.Contains("quicktime"))
                {
                    result.Format.Flag = MediaInfoFlags.Format_Mov;
                }

                if (_format.Contains("realmedia") || _formatCodec.Contains("rv40"))
                {
                    result.Format.Flag = MediaInfoFlags.Format_Rmvb;
                }

                //duration
                //string _duration = string.Empty;
                double _dur = GetDuration(mi); // milliseconds
                result.DurationSeconds = ((int)(_dur / 1000)).ToString();
            }
        }

        public static MediaInfoData GetMediaInfoData(string filePath)
        {
            string _data = null;
            return GetMediaInfoData(filePath, false, false, true, out _data);
        }

        public static MediaInfoData GetMediaInfoData(string filePath, bool getTextData, bool getXml, bool getDetails, out string textData)
        {
            MediaInfoData _result = new MediaInfoData();

            textData = string.Empty;

            if (FileManager.Configuration.Options.DisableMediaInfoProcessing)
            {
                return _result;
            }

            try
            {
                string _originalFilePath = filePath;

                if (IsISO(filePath))
                {
                    filePath = RedirectISO(filePath);
                    if (string.IsNullOrEmpty(filePath))
                    {
                        ProcessSubtitles(_result, null, _originalFilePath);
                        return _result; // get out if u couldn't extract the .ifo
                    }
                }

                MediaInfo _mi = new MediaInfo();
                _mi.Open(filePath);

                if (getTextData)
                {
                    if (getXml)
                    {
                        _mi.Option("Inform", "XML");
                        textData = _mi.Inform();
                        // sometimes mediainfo returns invalid chars in node name. try to fix that (for now the &)
                        // Temporary Fix until it gets fixed in mediainfo
                        textData = Regex.Replace(textData, "(</?\\w+)(&)(\\w+)", "$1_$3", RegexOptions.IgnoreCase);
                        textData = Regex.Replace(textData, "(</?\\w+)(#)(\\w+)", "$1_$3", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        _mi.Option("Inform", "TEXT");
                        textData = _mi.Inform();
                    }
                    
                }

                if (!getDetails)
                {
                    _mi.Close();
                    return _result;
                }

                // format
                SetFormat(_result, _mi, filePath);

                // filesize (be aware of a possible redirection .iso->.ifo for example... use in that case the _originalFilePath
                string _filesize = string.Compare(_originalFilePath, filePath, true) == 0 ? _mi.Get(StreamKind.General, 0, "FileSize") : new FileInfo(_originalFilePath).Length.ToString();
                //double _fs = 0;
                //if (double.TryParse(_filesize, out _fs))
                //{
                //    _filesize = Helpers.GetFormattedFileSize(_fs);
                //}
                _result.FileSizeBytes = _filesize;

                // resolution
                // check resolution
                string _height = _mi.Get(StreamKind.Video, 0, "Height");
                string _width = _mi.Get(StreamKind.Video, 0, "Width");
                string _aspectRatio = _mi.Get(StreamKind.Video, 0, "DisplayAspectRatio");

                _result.VideoResolution = string.Format("{0}x{1}", _width, _height);

                // check aspect ratio
                if (!string.IsNullOrEmpty(_aspectRatio))
                {
                    double _aspect = 0d;
                    try
                    {
                        _aspect = Double.Parse(_aspectRatio.Replace(",", "."), CultureInfo.InvariantCulture);
                    }
                    catch { }

                    _aspectRatio = string.Format("{0:0.00}:1", _aspect).Replace(",", ".");
                    if (_aspect > 1.3 && _aspect < 1.4)
                    {
                        _aspectRatio = "4:3";
                    }
                    if (_aspect == 1.500)
                    {
                        _aspectRatio = "3:2";
                    }
                    if (_aspect > 1.7 && _aspect < 1.8)
                    {
                        _aspectRatio = "16:9";
                    }

                    if (_aspect > 2.3 && _aspect < 2.4)
                    {
                        _aspectRatio = "2.35:1";
                    }

                }
                _result.AspectRatio = _aspectRatio;

                // check refresh rate
                _result.FrameRate = _mi.Get(StreamKind.Video, 0, "FrameRate");

                int _h = 5000;
                int _w = 5000;
                double _ar = 0;
                Int32.TryParse(_height, out _h);
                Int32.TryParse(_width, out _w);
                double.TryParse(_aspectRatio, out _ar);

                if (_w != 0 && _h != 0)
                {
                    _result.Resolution.Flag = MediaInfoFlags.Resolution_288p;
                }
                if (_w > 320 && _h > 240)
                {
                    _result.Resolution.Flag = MediaInfoFlags.Resolution_480p;
                }

                if (_h >= 576)
                {
                    _result.Resolution.Flag = MediaInfoFlags.Resolution_576p;
                }

                if ((_ar == _16_9 && _w >= 1280 && _h >= 720) || (_ar != _16_9 && _w > 720 && _h > 544) || _w >= 1280)
                {
                    _result.Resolution.Flag = MediaInfoFlags.Resolution_720p;
                }

                if ((_ar == _16_9 && _w >= 1920 && _h >= 1080) || (_ar != _16_9 && _w > 1280 || _h > 817))
                {
                    _result.Resolution.Flag = MediaInfoFlags.Resolution_1080p;
                }

                // check overallbitrate
                _result.OverallBitrate = Helpers.GetFormattedBitrate(_mi.Get(StreamKind.General, 0, "OverallBitRate"));
                if (string.IsNullOrEmpty(_result.OverallBitrate))
                {
                    _result.OverallBitrate = Helpers.GetFormattedBitrate(_mi.Get(StreamKind.General, 0, "OverallBitRate_Nominal"));
                }

                // check videobitrate
                _result.VideoBitrate = Helpers.GetFormattedBitrate(_mi.Get(StreamKind.Video, 0, "BitRate"));
                if (string.IsNullOrEmpty(_result.VideoBitrate))
                {
                    _result.VideoBitrate = Helpers.GetFormattedBitrate(_mi.Get(StreamKind.Video, 0, "BitRate_Nominal"));
                }

                //check video codec
                string _fr = _mi.Get(StreamKind.Video, 0, "Format");
                if (!string.IsNullOrEmpty(_fr) && _fr.ToLowerInvariant().Contains("mpeg"))
                {
                    _result.Video.Flag = MediaInfoFlags.Video_mpeg;
                }
                
                string _videoCodec = _mi.Get(StreamKind.Video, 0, "CodecID/Hint");
                if (string.IsNullOrEmpty(_videoCodec))
                {
                    _videoCodec = _mi.Get(StreamKind.Video, 0, "CodecID");
                }

                // check videocodec
                _result.VideoCodec = GetCodecInfoText(_mi, StreamKind.Video);

                if (string.IsNullOrEmpty(_videoCodec))
                {
                    _videoCodec = _result.VideoCodec;
                }

                if (!string.IsNullOrEmpty(_videoCodec))
                {
                    _videoCodec = _videoCodec.ToLowerInvariant();
                }
                if (_videoCodec.Contains("xvid"))
                {
                    _result.Video.Flag = MediaInfoFlags.Video_Xvid;
                }
                if (_videoCodec.Contains("divx") || _videoCodec.Contains("dx"))
                {
                    _result.Video.Flag = MediaInfoFlags.Video_Divx;
                }
                if (_videoCodec.Contains("wmv"))
                {
                    _result.Video.Flag = MediaInfoFlags.Video_WMVHD;
                }
                if (_videoCodec.Contains("avc"))
                {
                    _result.Video.Flag = MediaInfoFlags.Video_H264;
                }
                if (!string.IsNullOrEmpty(_fr) && _fr.ToLowerInvariant().Contains("avc"))
                {
                    _result.Video.Flag = MediaInfoFlags.Video_H264;
                }


                // check language
                int _cnt = _mi.Count_Get(StreamKind.Audio);

                for (int _ia = 0; _ia < _cnt; _ia++)
                {
                    string _languageCode = _mi.Get(StreamKind.Audio, _ia, "Language");
                    if (string.IsNullOrEmpty(_languageCode))
                    {
                        _languageCode = FileManager.Configuration.Options.MovieSheetsOptions.DefaultAudioLanguage;
                    }
                    _result.LanguageCode = _ia == 0 || string.IsNullOrEmpty(_result.LanguageCode) ? _languageCode : _result.LanguageCode;
                    _result.LanguageCodes.Add(_languageCode);

                    if (!string.IsNullOrEmpty(_languageCode))
                    {
                        CultureInfo _ci = null;
                        try
                        {
                            _ci = Helpers.GetCultureInfo(_languageCode);
                        }
                        catch (Exception ex)
                        {
                            Loggy.Logger.DebugException("Lang2", ex);
                        }
                        if (_ci != null && _ci != CultureInfo.InvariantCulture)
                        {
                            _result.Language = _ia == 0 || string.IsNullOrEmpty(_result.Language) ? _ci.EnglishName : _result.Language;
                            _result.Languages.Add(_ci.EnglishName);
                        }
                    }
                }

                // check audio
                string _audioFormat = _mi.Get(StreamKind.Audio, 0, "Format").ToLowerInvariant();
                string _audioCodec = _mi.Get(StreamKind.Audio, 0, "CodecID").ToLowerInvariant();
                string _audioHint = _mi.Get(StreamKind.Audio, 0, "CodecID/Hint").ToLowerInvariant();
                string _audioChannels = _mi.Get(StreamKind.Audio, 0, "Channel(s)").ToLowerInvariant();
                string _audioProfile = _mi.Get(StreamKind.Audio, 0, "Format_Profile").ToLowerInvariant();

                _result.AudioBitrate = Helpers.GetFormattedBitrate(_mi.Get(StreamKind.Audio, 0, "BitRate"));
                if (string.IsNullOrEmpty(_result.AudioBitrate))
                {
                    _result.AudioBitrate = Helpers.GetFormattedBitrate(_mi.Get(StreamKind.Audio, 0, "BitRate_Nominal"));
                    if (string.IsNullOrEmpty(_result.AudioBitrate))
                    {
                        _result.AudioBitrate = _mi.Get(StreamKind.Audio, 0, "BitRate_Mode");
                    }
                }

                _result.AudioCodec = GetCodecInfoText(_mi, StreamKind.Audio);
                _result.AudioChannels = _audioChannels;

                if (_audioFormat.Contains("dts") || _audioCodec.Contains("dts") || _audioHint.Contains("dts"))
                {
                    _result.Audio.Flag = MediaInfoFlags.Audio_DTS;
                }
                if (_audioFormat.Contains("dts") && (_audioProfile.Contains("ma")))
                {
                    _result.Audio.Flag = MediaInfoFlags.Audio_DTSHD;
                }

                if (_audioFormat.Contains("mp3") || _audioCodec.Contains("55") || _audioHint.Contains("mp3"))
                {
                    _result.Audio.Flag = MediaInfoFlags.Audio_MP3;
                }
                if (_audioFormat.Contains("ac-3") || _audioCodec.Contains("ac3") || _audioHint.Contains("ac3"))
                {
                    if (_audioChannels.Contains("6"))
                    {
                        _result.Audio.Flag = MediaInfoFlags.Audio_DolbyDigital;
                    }
                    else
                    {
                        _result.Audio.Flag = MediaInfoFlags.Audio_DolbyStereo;
                    }
                }
                if (_audioFormat.Contains("truehd") || (_audioCodec.Contains("truehd")))
                {
                    _result.Audio.Flag = MediaInfoFlags.Audio_DolbyTrueHD;
                }

                if (_audioFormat.Contains("aac") || _audioCodec.Contains("aac") || _audioHint.Contains("aac"))
                {
                    if (_audioChannels.Contains("6"))
                    {
                        _result.Audio.Flag = MediaInfoFlags.Audio_AAC51;
                    }
                    else
                    {
                        //if (_audioProfile.Contains("lc"))
                        //{
                        //    _result.Audio.Flag = MediaInfoFlags.Audio_AACStereoLC;
                        //}
                        //else
                        //{
                        _result.Audio.Flag = MediaInfoFlags.Audio_AACStereo;
                        //}
                    }
                }
                if (_audioFormat.Contains("flac") || _audioCodec.Contains("flac"))
                {
                    _result.Audio.Flag = MediaInfoFlags.Audio_Flac;
                }
                if (_audioFormat.Contains("wma") || _audioCodec.Contains("162"))
                {
                    _result.Audio.Flag = MediaInfoFlags.Audio_WMA;
                }
                if (_audioFormat.Contains("vorbis") || _audioCodec.Contains("vorbis"))
                {
                    _result.Audio.Flag = MediaInfoFlags.Audio_Vorbis;
                }

                ProcessSubtitles(_result, _mi, filePath);

                // set this flag for the ThumbGen's GUI.. to signal presence of any kind of subtitles
                string _subsLanguage = _mi.Get(StreamKind.Text, 0, "Language").ToLowerInvariant();
                if (!string.IsNullOrEmpty(_subsLanguage))
                {
                    _result.Subtitles.Flag = !string.IsNullOrEmpty(_result.SubtitlesText) || _result.ExternalSubtitlesList.Count != 0 ? MediaInfoFlags.Subtitles_Generic : MediaInfoFlags.Unknown;
                }

                _mi.Close();
            }
            catch { }

            return _result;
        }

        private static void ProcessSubtitles(MediaInfoData result, MediaInfo mi, string filePath)
        {
            // check embedded subtitles
            // process each sub and add it to the list
            if (mi != null && !IsISO(filePath))
            {
                try
                {
                    int _subsCnt = mi.Count_Get(StreamKind.Text);
                    if (_subsCnt != 0)
                    {
                        for (int _i = 0; _i < _subsCnt; _i++)
                        {
                            EmbeddedSubtitle _sub = new EmbeddedSubtitle();
                            _sub.Format = mi.Get(StreamKind.Text, _i, "Format");
                            _sub.Language = mi.Get(StreamKind.Text, _i, "Language");
                            if (string.IsNullOrEmpty(_sub.Language))
                            {
                                _sub.Language = mi.Get(StreamKind.Text, _i, "Title");
                            }
                            _sub.Language = string.IsNullOrEmpty(_sub.Language) ? FileManager.Configuration.Options.MovieSheetsOptions.DefaultExternalSubtitlesLanguage : _sub.Language;
                            if (PopulateLanguages(_sub))
                            {
                                if (!string.IsNullOrEmpty(_sub.Language) && !string.IsNullOrEmpty(_sub.Format))
                                {
                                    result.EmbeddedSubtitles.Add(_sub);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("Embedded subs: ", ex);
                }
            }
            // process also the external subtitles
            try
            {
                ExternalSubtitlesInfo _subtitles = CollectExternalSubtitles(filePath, false);
                if (_subtitles.HasExternalSubtitles)
                {
                    foreach (ExtSubData _subData in _subtitles.SubFiles)
                    {
                        EmbeddedSubtitle _sub = new EmbeddedSubtitle();
                        _sub.Format = _subData.Format;
                        _sub.Language = _subData.TwoLetterLanguageCode;
                        if (PopulateLanguages(_sub))
                        {
                            if (!string.IsNullOrEmpty(_sub.Language) && !string.IsNullOrEmpty(_sub.Format))
                            {
                                result.ExternalSubtitlesList.Add(_sub);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("External subs: ", ex);
            }

        }

        private static bool PopulateLanguages(EmbeddedSubtitle sub)
        {
            bool _result = false;

            if (sub != null && !string.IsNullOrEmpty(sub.Language))
            {
                if (FileManager.Configuration.Options.MovieSheetsOptions.IgnoreOtherLanguages &&
                    sub.Language.ToLowerInvariant() != FileManager.Configuration.Options.MovieSheetsOptions.DefaultExternalSubtitlesLanguage.ToLowerInvariant())
                {
                    return false;
                }
                try
                {
                    CultureInfo _ci = null;
                    try
                    {
                        _ci = Helpers.GetCultureInfo(sub.Language != "iw" ? sub.Language : "he");
                    }
                    catch (Exception ex)
                    {
                        Loggy.Logger.DebugException("Cannot create culture from: " + sub.Language, ex);
                    }
                    if (_ci != null && _ci != CultureInfo.InvariantCulture)
                    {
                        // store the native displayname
                        TextInfo _UsaTextInfo = CultureInfo.GetCultureInfo("en-US").TextInfo;
                        try
                        {
                            sub.Language = _UsaTextInfo.ToTitleCase(_ci.NativeName);
                        }
                        catch
                        {
                            sub.Language = _ci.NativeName;
                        }
                        // store the EnglishName
                        sub.EnglishLanguage = _ci.EnglishName;
                        // ok
                        _result = true;
                    }
                }
                catch { }
            }

            return _result;
        }

        public class ExtSubData
        {
            public string Format { get; set; }
            public string Filename { get; set; }
            public string MovieName { get; set; }
            public string TwoLetterLanguageCode { get; set; }

            public ExtSubData()
            {

            }
        }

        public class ExternalSubtitlesInfo
        {
            public List<ExtSubData> SubFiles { get; set; }

            public bool HasExternalSubtitles
            {
                get
                {
                    return SubFiles.Count != 0;
                }
            }

            public ExternalSubtitlesInfo()
            {
                SubFiles = new List<ExtSubData>();
            }
        }

        public static ExternalSubtitlesInfo CollectExternalSubtitles(string movieFilename, bool doJustDetection)
        {
            ExternalSubtitlesInfo _result = new ExternalSubtitlesInfo();

            List<FileInfo> _temp = new FilesCollector().CollectFiles(Path.GetDirectoryName(movieFilename), false, SubtitlesManager.SubtitlesSupported).ToList<FileInfo>();
            if (_temp != null && _temp.Count != 0)
            {
                bool _hasIdxSub = false;
                // detect if .idx file is present
                foreach (FileInfo _info in _temp)
                {
                    if (_info.Extension == ".idx")
                    {
                        _hasIdxSub = true;
                        break;
                    }
                }

                foreach (FileInfo _info in _temp)
                {
                    // if we have an .idx file to process skip existing .sub as it does not contain a candidate sub
                    if (_hasIdxSub && _info.Extension == ".sub")
                    {
                        continue;
                    }

                    ExtSubData _data = null;
                    if (_info.Extension == ".idx")
                    {
                        // process special case: .idx/.sub
                        // try to load the file
                        string _input = File.ReadAllText(_info.FullName);
                        if (!string.IsNullOrEmpty(_input))
                        {
                            Regex _reg = new Regex("id: ([a-zA-Z]+), index:", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                            foreach (Match _match in _reg.Matches(_input))
                            {
                                string _code = _match.Groups[1].Value.Trim();
                                if (!string.IsNullOrEmpty(_code) && _code.Length == 2 && !CodeExists(_code, _result.SubFiles))
                                {
                                    _data = new ExtSubData();
                                    _data.Format = "IDX/SUB";
                                    _data.Filename = _info.FullName;
                                    _data.TwoLetterLanguageCode = _code;
                                    _data.MovieName = Path.GetFileNameWithoutExtension(_info.Name);
                                    if (string.Compare(_data.MovieName, Path.GetFileNameWithoutExtension(movieFilename), true, CultureInfo.InvariantCulture) == 0)
                                    {
                                        _result.SubFiles.Add(_data);
                                        if (doJustDetection)
                                        {
                                            return _result;
                                        }
                                    }
                                }
                            }
                        }
                        _input = null;
                    }
                    else
                    {
                        // process standard subtitles
                        // remove the extension
                        string _name = Path.GetFileNameWithoutExtension(_info.Name).ToLowerInvariant();
                        if (!string.IsNullOrEmpty(_name))
                        {
                            _data = new ExtSubData();
                            _data.Format = Path.GetExtension(_info.Name).Trim('.').ToUpperInvariant();
                            _data.Filename = _info.FullName;

                            // try to get the language code
                            string _code = Path.GetExtension(_name).Trim('.');
                            if (!string.IsNullOrEmpty(_code))
                            {
                                CultureInfo _ci = _code.Length == 2 ? Helpers.GetCultureInfo(_code) : Helpers.GetCultureInfoFromEnglishName(_code);
                                if (_ci != null && _ci != CultureInfo.InvariantCulture)
                                {
                                    // the file has a language code
                                    _data.TwoLetterLanguageCode = _ci.TwoLetterISOLanguageName;
                                    // what is before the language code is the moviename
                                    _data.MovieName = Path.GetFileNameWithoutExtension(_name);
                                }
                            }
                            if(string.IsNullOrEmpty(_data.MovieName))
                            {
                                // there is no language code, use the default one OR use Google Translate?
                                _data.TwoLetterLanguageCode = FileManager.Configuration.Options.MovieSheetsOptions.DefaultExternalSubtitlesLanguage;

                                // the movie file name is easy
                                _data.MovieName = Path.GetFileNameWithoutExtension(_info.Name);

                            }
                            if (string.Compare(_data.MovieName, Path.GetFileNameWithoutExtension(movieFilename), true, CultureInfo.InvariantCulture) == 0)
                            {
                                _result.SubFiles.Add(_data);
                                if (doJustDetection)
                                {
                                    return _result;
                                }
                            }
                        }
                    }
                }
            }


            return _result;
        }

        private static bool CodeExists(string code, IEnumerable<ExtSubData> subsList)
        {
            bool _result = false;

            if (subsList != null && subsList.Count() != 0)
            {
                foreach (ExtSubData _data in subsList)
                {
                    if (string.Compare(_data.TwoLetterLanguageCode, code, true) == 0)
                    {
                        _result = true;
                        break;
                    }
                }
            }

            return _result;
        }

        public static bool HasExternalSubtitles(string filePath)
        {
            return CollectExternalSubtitles(filePath, true).HasExternalSubtitles;
        }

        public static List<string> GetAllDistinctSubtitles(List<string> embedded, List<string> external)
        {
            try
            {
                return embedded.Union(external).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
