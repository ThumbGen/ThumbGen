using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Xml.Serialization;
using System.Globalization;

namespace ThumbGen
{
    public enum MediaInfoFlagsType
    {
        Unknown,
        Resolution,
        Format,
        Video,
        Audio,
        Subtitles
    }

    public class EmbeddedSubtitle
    {
        public string Format { get; set; }
        public string Language { get; set; }
        public string EnglishLanguage { get; set; }

        public EmbeddedSubtitle()
        {
        }
    }

    public enum MediaInfoFlags
    {
        Unknown,
        Resolution_720p,
        Resolution_1080p,
        Resolution_288p,
        Resolution_480p,
        Resolution_576p,
        Format_Bluray,
        Format_DVD,
        Format_MKV,
        Format_Mpeg,
        Format_Mov,
        Format_Rmvb,
        Video_Divx,
        Video_Xvid,
        Video_WMVHD,
        Video_H264,
        Video_mpeg,
        Audio_AACStereo,
        Audio_AACStereoLC,
        Audio_AAC51,
        Audio_DolbyStereo,
        Audio_DolbyDigital,
        Audio_DolbyTrueHD,
        Audio_DTS,
        Audio_DTSHD,
        Audio_MP3,
        Audio_Flac,
        Audio_WMA,
        Audio_Vorbis,
        Subtitles_Generic
    }

    public class MediaModel: BaseNotifyPropertyChanged
    {
        public static Dictionary<MediaInfoFlags, string> MediaInfoImages = new Dictionary<MediaInfoFlags,string>()
        {
            {MediaInfoFlags.Unknown, "EMPTY.png"},
            {MediaInfoFlags.Resolution_720p, "720P.png"},
            {MediaInfoFlags.Resolution_1080p, "1080P.png"},
            {MediaInfoFlags.Resolution_288p, "288p.png"},
            {MediaInfoFlags.Resolution_480p, "480p.png"},
            {MediaInfoFlags.Resolution_576p, "576p.png"},
            {MediaInfoFlags.Format_Bluray, "BLURAY.png"},
            {MediaInfoFlags.Format_DVD, "DVD.png"},
            {MediaInfoFlags.Format_MKV, "MKV.png"},
            {MediaInfoFlags.Format_Mpeg, "Mpeg video.png"},
            {MediaInfoFlags.Format_Mov, "EMPTY.png"},
            {MediaInfoFlags.Format_Rmvb, "EMPTY.png"},
            {MediaInfoFlags.Video_Divx, "DIVX.png"},
            {MediaInfoFlags.Video_Xvid, "XVID.png"},
            {MediaInfoFlags.Video_WMVHD, "WMVHD.png"},
            {MediaInfoFlags.Video_H264, "H264.png"},
            {MediaInfoFlags.Video_mpeg, "Mpeg video.png"},
            {MediaInfoFlags.Audio_AAC51, "aac6ch.png"},
            {MediaInfoFlags.Audio_AACStereo, "aac.png"},
            {MediaInfoFlags.Audio_AACStereoLC, "aaclc2.png"},
            {MediaInfoFlags.Audio_DolbyDigital, "DOLBY51.png"},
            {MediaInfoFlags.Audio_DolbyStereo, "DOLBY21.png"},
            {MediaInfoFlags.Audio_DolbyTrueHD, "DOLBY51.png"},
            {MediaInfoFlags.Audio_DTS, "DTS.png"},
            {MediaInfoFlags.Audio_DTSHD, "DTS.png"},
            {MediaInfoFlags.Audio_MP3, "MP3 audio.png"},
            {MediaInfoFlags.Audio_Flac, "flac.png"},
            {MediaInfoFlags.Audio_WMA, "WMa2.png"},
            {MediaInfoFlags.Audio_Vorbis, "vorbis.png"},
            {MediaInfoFlags.Subtitles_Generic, "subtitles.png"}
        };

        public static Dictionary<MediaInfoFlags, string> MediaInfoText = new Dictionary<MediaInfoFlags, string>()
        {
            {MediaInfoFlags.Unknown, "Unknown"},
            {MediaInfoFlags.Resolution_720p, "720P"},
            {MediaInfoFlags.Resolution_1080p, "1080P"},
            {MediaInfoFlags.Resolution_288p, "288P"},
            {MediaInfoFlags.Resolution_480p, "480P"},
            {MediaInfoFlags.Resolution_576p, "576P"},
            {MediaInfoFlags.Format_Bluray, "BLURAY"},
            {MediaInfoFlags.Format_DVD, "DVD"},
            {MediaInfoFlags.Format_MKV, "MKV"},
            {MediaInfoFlags.Format_Mpeg, "mpeg4"},
            {MediaInfoFlags.Format_Mov, "Mov"},
            {MediaInfoFlags.Format_Rmvb, "rmvb"},
            {MediaInfoFlags.Video_Divx, "divx"},
            {MediaInfoFlags.Video_Xvid, "xvid"},
            {MediaInfoFlags.Video_WMVHD, "wmv"},
            {MediaInfoFlags.Video_H264, "avc"},
            {MediaInfoFlags.Video_mpeg, "mpeg"},
            {MediaInfoFlags.Audio_AAC51, "AAC51"},
            {MediaInfoFlags.Audio_AACStereo, "AAC"},
            {MediaInfoFlags.Audio_AACStereoLC, "AAC20"},
            {MediaInfoFlags.Audio_DolbyDigital, "DD51"},
            {MediaInfoFlags.Audio_DolbyStereo, "DD20"},
            {MediaInfoFlags.Audio_DolbyTrueHD, "DTRUEHD"},
            {MediaInfoFlags.Audio_DTS, "DTS51"},
            {MediaInfoFlags.Audio_DTSHD, "DTSHD"},
            {MediaInfoFlags.Audio_MP3, "MP3"},
            {MediaInfoFlags.Audio_Flac, "FLAC"},
            {MediaInfoFlags.Audio_WMA, "WMA"},
            {MediaInfoFlags.Audio_Vorbis, "VORBIS"},
            {MediaInfoFlags.Subtitles_Generic, "Subtitles"}
        };

        public static Dictionary<MediaInfoFlags, string> Resolutions = FilterMediaFlags(MediaInfoFlagsType.Resolution);
        public static Dictionary<MediaInfoFlags, string> Formats = FilterMediaFlags(MediaInfoFlagsType.Format);
        public static Dictionary<MediaInfoFlags, string> Videos = FilterMediaFlags(MediaInfoFlagsType.Video);
        public static Dictionary<MediaInfoFlags, string> Audios = FilterMediaFlags(MediaInfoFlagsType.Audio);
        public static Dictionary<MediaInfoFlags, string> Subtitles = FilterMediaFlags(MediaInfoFlagsType.Subtitles);

        static Dictionary<MediaInfoFlags, string> FilterMediaFlags(MediaInfoFlagsType type)
        {
            Dictionary<MediaInfoFlags, string> _result = new Dictionary<MediaInfoFlags, string>();

            _result.Add(MediaInfoFlags.Unknown, MediaModel.MediaInfoText[MediaInfoFlags.Unknown]);

            foreach (string flag in Enum.GetNames(typeof(MediaInfoFlags)))
            {
                MediaInfoFlags _flag = (MediaInfoFlags)Enum.Parse(typeof(MediaInfoFlags), flag);

                switch (type)
                {
                    case MediaInfoFlagsType.Resolution:
                        if (flag.Contains("Resolution"))
                        {
                            _result.Add(_flag, MediaModel.MediaInfoText[_flag]);
                        }
                        break;
                    case MediaInfoFlagsType.Audio:
                        if (flag.Contains("Audio"))
                        {
                            _result.Add(_flag, MediaModel.MediaInfoText[_flag]);
                        }
                        break;
                    case MediaInfoFlagsType.Format:
                        if (flag.Contains("Format"))
                        {
                            _result.Add(_flag, MediaModel.MediaInfoText[_flag]);
                        }
                        break;
                    case MediaInfoFlagsType.Subtitles:
                        if (flag.Contains("Subtitle"))
                        {
                            _result.Add(_flag, MediaModel.MediaInfoText[_flag]);
                        }
                        break;
                    case MediaInfoFlagsType.Video:
                        if (flag.Contains("Video"))
                        {
                            _result.Add(_flag, MediaModel.MediaInfoText[_flag]);
                        }
                        break;
                    default:
                        break;
                }
            }

            return _result;
        }


        public static void SaveMediaFlagImageToDisk(MediaInfoFlags flag, string dest)
        {
            if (!string.IsNullOrEmpty(dest))
            {
                using (FileStream _fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                {
                    PngBitmapEncoder _encoder = new PngBitmapEncoder();
                    _encoder.Frames.Add(BitmapFrame.Create(new Uri(MediaModel.GetImagePath(flag))));
                    _encoder.Save(_fs);
                }
            }
        }

        public static string GetImagePath(MediaInfoFlags flag)
        {
            string _img = MediaInfoImages[flag];
            if (!string.IsNullOrEmpty(_img))
            {
                return string.Format("pack://application:,,,/images/MediaFlags/{0}", _img);
            }
            else
            {
                return null;
            }
        }

        private MediaInfoFlags m_Flag = MediaInfoFlags.Unknown;
        public MediaInfoFlags Flag 
        {
            get
            {
                return m_Flag;
            }
            set
            {
                m_Flag = value;
                NotifyPropertyChanged("Flag");
                NotifyPropertyChanged("ImagePath");
            }
        }

        public string ImagePath
        {
            get
            {
                return GetImagePath(Flag);
            }
        }

        public MediaModel(MediaInfoFlags flag)
        {
            Flag = flag;
        }

        public MediaModel()
        {
            
        }
    }

    [Serializable]
//    [XmlRootAttribute(ElementName = "mediainfo", IsNullable = false)]
    public class MediaInfoData: DependencyObject
    {
        public MediaModel Resolution
        {
            get { return (MediaModel)GetValue(ResolutionProperty); }
            set { SetValue(ResolutionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Resolution.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResolutionProperty =
            DependencyProperty.Register("Resolution", typeof(MediaModel), typeof(MediaInfoData), new UIPropertyMetadata(new MediaModel()));

        [XmlElement(ElementName = "resolution")]
        public string ResolutionText
        {
            get
            {
                return MediaModel.MediaInfoText[Resolution.Flag];
            }
            set { }
        }

        public MediaModel Format
        {
            get { return (MediaModel)GetValue(FormatProperty); }
            set { SetValue(FormatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Format.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register("Format", typeof(MediaModel), typeof(MediaInfoData), new UIPropertyMetadata(new MediaModel()));

        [XmlElement(ElementName = "format")]
        public string FormatText
        {
            get
            {
                return MediaModel.MediaInfoText[Format.Flag];
            }
            set { }
        }

        public MediaModel Video
        {
            get { return (MediaModel)GetValue(VideoProperty); }
            set { SetValue(VideoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Video.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoProperty =
            DependencyProperty.Register("Video", typeof(MediaModel), typeof(MediaInfoData), new UIPropertyMetadata(new MediaModel()));

        [XmlElement(ElementName = "video")]
        public string VideoText
        {
            get
            {
                return MediaModel.MediaInfoText[Video.Flag];
            }
            set { }
        }

        public MediaModel Audio
        {
            get { return (MediaModel)GetValue(AudioProperty); }
            set { SetValue(AudioProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Audio.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AudioProperty =
            DependencyProperty.Register("Audio", typeof(MediaModel), typeof(MediaInfoData), new UIPropertyMetadata(new MediaModel()));

        [XmlElement(ElementName = "audio")]
        public string AudioText
        {
            get
            {
                return MediaModel.MediaInfoText[Audio.Flag];
            }
            set { }
        }

        public MediaModel Subtitles
        {
            get { return (MediaModel)GetValue(SubtitlesProperty); }
            set { SetValue(SubtitlesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Subtitles.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SubtitlesProperty =
            DependencyProperty.Register("Subtitles", typeof(MediaModel), typeof(MediaInfoData), new UIPropertyMetadata(new MediaModel()));

        public string SubtitlesText
        {
            get
            {
                return MediaModel.MediaInfoText[Subtitles.Flag];
            }
            set { }
        }

        [XmlElement(ElementName = "framerate")]
        public string FrameRate
        {
            get { return (string)GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FrameRate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FrameRateProperty =
            DependencyProperty.Register("FrameRate", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "aspectratio")]
        public string AspectRatio
        {
            get { return (string)GetValue(AspectRatioProperty); }
            set { SetValue(AspectRatioProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AspectRatio.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AspectRatioProperty =
            DependencyProperty.Register("AspectRatio", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "videoresolution")]
        public string VideoResolution
        {
            get { return (string)GetValue(VideoResolutionProperty); }
            set { SetValue(VideoResolutionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoResolution.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoResolutionProperty =
            DependencyProperty.Register("VideoResolution", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName="videocodec")]
        public string VideoCodec
        {
            get { return (string)GetValue(VideoCodecProperty); }
            set { SetValue(VideoCodecProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoCodec.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoCodecProperty =
            DependencyProperty.Register("VideoCodec", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "videobitrate")]
        public string VideoBitrate
        {
            get { return (string)GetValue(VideoBitrateProperty); }
            set { SetValue(VideoBitrateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoBitrate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoBitrateProperty =
            DependencyProperty.Register("VideoBitrate", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "overallbitrate")]
        public string OverallBitrate
        {
            get { return (string)GetValue(OverallBitrateProperty); }
            set { SetValue(OverallBitrateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OverallBitrate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OverallBitrateProperty =
            DependencyProperty.Register("OverallBitrate", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        

        [XmlElement(ElementName = "audiocodec")]
        public string AudioCodec
        {
            get { return (string)GetValue(AudioCodecProperty); }
            set { SetValue(AudioCodecProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AudioCodec.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AudioCodecProperty =
            DependencyProperty.Register("AudioCodec", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));
        
        [XmlElement(ElementName = "audiochannels")]
        public string AudioChannels
        {
            get { return (string)GetValue(AudioChannelsProperty); }
            set { SetValue(AudioChannelsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AudioChannels.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AudioChannelsProperty =
            DependencyProperty.Register("AudioChannels", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "audiobitrate")]
        public string AudioBitrate
        {
            get { return (string)GetValue(AudioBitrateProperty); }
            set { SetValue(AudioBitrateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AudioBitrate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AudioBitrateProperty =
            DependencyProperty.Register("AudioBitrate", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "durationseconds")]
        public string DurationSeconds
        {
            get { return (string)GetValue(DurationSecondsProperty); }
            set { SetValue(DurationSecondsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DurationSeconds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DurationSecondsProperty =
            DependencyProperty.Register("DurationSeconds", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty, OnDurationSecondsChanged));

        private static void OnDurationSecondsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MediaInfoData _mid = obj as MediaInfoData;
            if (_mid != null)
            {
                double _dur = GetDurationFromString(args.NewValue as string);
                // set FormattedDuration, DurationSeconds based on the new value
                if (_dur != 0)
                {
                    TimeSpan _ts = TimeSpan.FromMilliseconds(_dur * 1000); // _dur comes as seconds
                    string _s = string.Format("{0}h {1}m", _ts.Hours, _ts.Minutes);
                    if (string.Compare(_s, _mid.FormattedDuration, true) != 0)
                    {
                        _mid.FormattedDuration = _s;
                    }
                }
                string _d = ((int)_dur / 60).ToString();
                if (string.Compare(_d, _mid.Duration, true) != 0)
                {
                    _mid.Duration = _d;
                }
            }
        }


        [XmlElement(ElementName = "durationminutes")]
        public string Duration
        {
            get { return (string)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Duration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty, OnDurationChanged));

        private static void OnDurationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            return;
            //MediaInfoData _mid = obj as MediaInfoData;
            //if (_mid != null)
            //{
            //    double _dur = GetDurationFromString(args.NewValue as string);
            //    // set FormattedDuration, DurationSeconds based on the new value
            //    if (_dur != 0)
            //    {
            //        TimeSpan _ts = TimeSpan.FromMilliseconds(_dur * 60000);
            //        string _s = string.Format("{0}h {1}m", _ts.Hours, _ts.Minutes);
            //        if(string.Compare(_s, _mid.FormattedDuration, true) != 0)
            //        {
            //            _mid.FormattedDuration = _s;
            //        }
            //    }
            //    string _d = ((int)_dur * 60).ToString();
            //    if(string.Compare(_d, _mid.DurationSeconds, true) != 0)
            //    {
            //        _mid.DurationSeconds = _d; 
            //    }
            //}
        }

        [XmlElement(ElementName = "duration")]
        public string FormattedDuration
        {
            get { return (string)GetValue(FormattedDurationProperty); }
            set { SetValue(FormattedDurationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FormattedDuration.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FormattedDurationProperty =
            DependencyProperty.Register("FormattedDuration", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty, OnFormattedDurationChanged));

        private static void OnFormattedDurationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MediaInfoData _mid = obj as MediaInfoData;
            if (_mid != null)
            {
                long _dur = Helpers.GetDurationSeconds(args.NewValue as string); 
                
                long _ds = 0;
                if (string.IsNullOrEmpty(_mid.DurationSeconds) || long.TryParse(_mid.DurationSeconds, out _ds)) // seconds
                {
                    if (_dur != _ds && string.IsNullOrEmpty(_mid.DurationSeconds))
                    {
                        _mid.DurationSeconds = _dur.ToString();
                    }
                }
                _ds = 0;
                if (string.IsNullOrEmpty(_mid.Duration) || long.TryParse(_mid.Duration, out _ds)) // minutes
                {
                    if (_dur != _ds / 60)
                    {
                        _mid.Duration = ((int)(_dur / 60)).ToString();
                    }
                }
                
            }
        }

        [XmlElement(ElementName = "filesizebytes")]
        public string FileSizeBytes
        {
            get { return (string)GetValue(FileSizeBytesProperty); }
            set { SetValue(FileSizeBytesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FileSizeBytes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileSizeBytesProperty =
            DependencyProperty.Register("FileSizeBytes", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty, OnFileSizeChanged));

        private static void OnFileSizeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MediaInfoData _mid = obj as MediaInfoData;
            if (_mid != null && !string.IsNullOrEmpty(args.NewValue as string))
            {
                // comes in bytes, keep it formatted
                double _fs = 0;
                if (double.TryParse(args.NewValue as string, out _fs))
                {
                    _mid.FileSize = Helpers.GetFormattedFileSize(_fs);
                }
            }
        }
        

        [XmlElement(ElementName = "filesize")]
        public string FileSize
        {
            get { return (string)GetValue(FileSizeProperty); }
            set { SetValue(FileSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FileSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileSizeProperty =
            DependencyProperty.Register("FileSize", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "container")]
        public string ContainerFormat
        {
            get { return (string)GetValue(ContainerFormatProperty); }
            set { SetValue(ContainerFormatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ContainerFormat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContainerFormatProperty =
            DependencyProperty.Register("ContainerFormat", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "language")]
        public string Language
        {
            get { return (string)GetValue(LanguageProperty); }
            set { SetValue(LanguageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Language.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LanguageProperty =
            DependencyProperty.Register("Language", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "languagecode")]
        public string LanguageCode
        {
            get { return (string)GetValue(LanguageCodeProperty); }
            set { SetValue(LanguageCodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LanguageCode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LanguageCodeProperty =
            DependencyProperty.Register("LanguageCode", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));


        [XmlElement(ElementName = "languages")]
        public List<string> Languages
        {
            get { return (List<string>)GetValue(LanguagesProperty); }
            set { SetValue(LanguagesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Languages.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LanguagesProperty =
            DependencyProperty.Register("Languages", typeof(List<string>), typeof(MediaInfoData), new UIPropertyMetadata(null));

        [XmlElement(ElementName = "languagecodes")]
        public List<string> LanguageCodes
        {
            get { return (List<string>)GetValue(LanguageCodesProperty); }
            set { SetValue(LanguageCodesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LanguageCodes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LanguageCodesProperty =
            DependencyProperty.Register("LanguageCodes", typeof(List<string>), typeof(MediaInfoData), new UIPropertyMetadata(null));

        [XmlElement(ElementName = "embeddedsubtitles")]
        public List<EmbeddedSubtitle> EmbeddedSubtitles
        {
            get { return (List<EmbeddedSubtitle>)GetValue(EmbeddedSubtitlesProperty); }
            set { SetValue(EmbeddedSubtitlesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EmbeddedSubtitles.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EmbeddedSubtitlesProperty =
            DependencyProperty.Register("EmbeddedSubtitles", typeof(List<EmbeddedSubtitle>), typeof(MediaInfoData), new UIPropertyMetadata(null));

        [Obsolete]
        [XmlElement(ElementName = "embeddedsubtitleslist")]
        public List<string> EmbeddedSubtitlesList
        {
            get { return (List<string>)GetValue(EmbeddedSubtitlesListProperty); }
            set { SetValue(EmbeddedSubtitlesListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EmbeddedSubtitlesText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EmbeddedSubtitlesListProperty =
            DependencyProperty.Register("EmbeddedSubtitlesList", typeof(List<string>), typeof(MediaInfoData), new UIPropertyMetadata(null));

        [XmlElement(ElementName = "externalsubtitles")]
        public string ExternalSubtitles
        {
            get { return (string)GetValue(ExternalSubtitlesProperty); }
            set { SetValue(ExternalSubtitlesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExternalSubtitles.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExternalSubtitlesProperty =
            DependencyProperty.Register("ExternalSubtitles", typeof(string), typeof(MediaInfoData), new UIPropertyMetadata(string.Empty));

        [XmlElement(ElementName = "externalsubtitleslist")]
        public List<EmbeddedSubtitle> ExternalSubtitlesList
        {
            get { return (List<EmbeddedSubtitle>)GetValue(ExternalSubtitlesListProperty); }
            set { SetValue(ExternalSubtitlesListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExternalSubtitlesList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExternalSubtitlesListProperty =
            DependencyProperty.Register("ExternalSubtitlesList", typeof(List<EmbeddedSubtitle>), typeof(MediaInfoData), new UIPropertyMetadata(null));



        


        public MediaInfoData()
        {
            Resolution = new MediaModel();
            Video = new MediaModel();
            Audio = new MediaModel();
            Subtitles = new MediaModel();
            Format = new MediaModel();
            EmbeddedSubtitles = new List<EmbeddedSubtitle>();
            EmbeddedSubtitlesList = new List<string>(); // obsolete but keep it for backwardcompat
            ExternalSubtitlesList = new List<EmbeddedSubtitle>();
            Languages = new List<string>();
            LanguageCodes = new List<string>();
        }

        public List<string> GetSubtitlesList(bool nativeLanguage, List<EmbeddedSubtitle> source)
        {
            List<string> _result = new List<string>();

            if (source != null && source.Count != 0)
            {
                foreach (EmbeddedSubtitle _sub in source)
                {
                    if (nativeLanguage)
                    {
                        _result.Add(_sub.Language);
                    }
                    else
                    {
                        _result.Add(_sub.EnglishLanguage);
                    }
                }
            }

            return _result;
        }

        public static double GetDurationFromString(string duration)
        {
            double _result = 0;

            try
            {
                if (!string.IsNullOrEmpty(duration))
                {
                    _result = Double.Parse(duration, CultureInfo.InvariantCulture);
                }
            }
            catch { }

            return _result;
        }
    }

    
}
