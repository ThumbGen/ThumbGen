using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.IO;
using ThumbGen.MovieSheets;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for MovieInfoControl.xaml
    /// </summary>
    public partial class MovieInfoControl : UserControl
    {
        public static RoutedCommand UseMovieInfoCommand = new RoutedCommand();

        public MovieInfoControl()
        {
            InitializeComponent();
        }

        public MovieInfo SelectedMovieInfo()
        {
            if (MovieInfoCombo != null && MovieInfoCombo.SelectedItem != null)
            {
                return (MovieInfoCombo.SelectedItem as MovieInfoProviderItem).MovieInfo;
            }
            else
            {
                return null;
            }
        }

        public bool IsFullEditor
        {
            get { return (bool)GetValue(IsFullEditorProperty); }
            set { SetValue(IsFullEditorProperty, value); }
        }

        private bool m_IsMetadataInfoMissing = true;
        private bool m_IsNFoInfoMissing = true;
        private bool m_IsPrefCollectorInfoMissing = true;

        // Using a DependencyProperty as the backing store for IsFullEditor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFullEditorProperty =
            DependencyProperty.Register("IsFullEditor", typeof(bool), typeof(MovieInfoControl), new UIPropertyMetadata(true));

        

        public ResultItemBase CurrentMovieItem
        {
            get { return (ResultItemBase)GetValue(CurrentMovieItemProperty); }
            set { SetValue(CurrentMovieItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentMovieItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentMovieItemProperty =
            DependencyProperty.Register("CurrentMovieItem", typeof(ResultItemBase), typeof(MovieInfoControl),
            new UIPropertyMetadata(null, OnCurrentMovieItemChanged));


        public nfoFileType LoadedNfoFileType
        {
            get { return (nfoFileType)GetValue(LoadedNfoFileTypeProperty); }
            set { SetValue(LoadedNfoFileTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoadedNfoFileType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadedNfoFileTypeProperty =
            DependencyProperty.Register("LoadedNfoFileType", typeof(nfoFileType), typeof(MovieInfoControl), new UIPropertyMetadata(nfoFileType.ThumbGen));



        public MediaInfoData MediaInfo
        {
            get { return (MediaInfoData)GetValue(MediaInfoProperty); }
            set { SetValue(MediaInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MediaInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaInfoProperty =
            DependencyProperty.Register("MediaInfo", typeof(MediaInfoData), typeof(MovieInfoControl), new UIPropertyMetadata(null, OnMediaInfoChanged));

        private static void OnMediaInfoChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MovieInfoControl _control = obj as MovieInfoControl;
            if (_control != null)
            { }
        }

        public string CurrentMoviePath
        {
            get { return (string)GetValue(CurrentMoviePathProperty); }
            set { SetValue(CurrentMoviePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentMoviePath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentMoviePathProperty =
            DependencyProperty.Register("CurrentMoviePath", typeof(string), typeof(MovieInfoControl), new UIPropertyMetadata(null));

        public MovieInfo IMDBInfo
        {
            get { return (MovieInfo)GetValue(IMDBInfoProperty); }
            set { SetValue(IMDBInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IMDBInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IMDBInfoProperty =
            DependencyProperty.Register("IMDBInfo", typeof(MovieInfo), typeof(MovieInfoControl),
                new UIPropertyMetadata(null, OnIMDBInfoChanged));

        private static void OnIMDBInfoChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MovieInfoControl _control = obj as MovieInfoControl;
            if (_control != null)
            {
                // apply imdbinfo to the prefcollector
                if (args.NewValue != null && _control.PrefCollectorInfo != null)
                {
                    _control.PrefCollectorInfo = ApplyIMDbMovieInfoBehaviour(_control.PrefCollectorInfo, _control.IMDBInfo);
                }

                // apply imdbinfo to the my own nfo collector
                if (args.NewValue != null && _control.MyDataInfo != null)
                {
                    _control.MyDataInfo = ApplyIMDbMovieInfoBehaviour(_control.MyDataInfo, _control.IMDBInfo);
                }

                // apply imdbinfo to the my metadata info
                if (args.NewValue != null && _control.MetadataInfo != null)
                {
                    _control.MetadataInfo = ApplyIMDbMovieInfoBehaviour(_control.MetadataInfo, _control.IMDBInfo);
                }

                _control.SelectInfoSourceByPriority();
            }
        }


        public MovieInfo MyDataInfo
        {
            get { return (MovieInfo)GetValue(MyDataInfoProperty); }
            set { SetValue(MyDataInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyDataInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyDataInfoProperty =
            DependencyProperty.Register("MyDataInfo", typeof(MovieInfo), typeof(MovieInfoControl), new UIPropertyMetadata(null));

        public MovieInfo MetadataInfo
        {
            get { return (MovieInfo)GetValue(MetadataInfoProperty); }
            set { SetValue(MetadataInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MetadataInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MetadataInfoProperty =
            DependencyProperty.Register("MetadataInfo", typeof(MovieInfo), typeof(MovieInfoControl), new UIPropertyMetadata(null));


        public MovieInfo PrefCollectorInfo
        {
            get { return (MovieInfo)GetValue(PrefCollectorInfoProperty); }
            set { SetValue(PrefCollectorInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrefCollectorInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrefCollectorInfoProperty =
            DependencyProperty.Register("PrefCollectorInfo", typeof(MovieInfo), typeof(MovieInfoControl),
                new UIPropertyMetadata(null, OnPrefCollectorInfoChanged));


        public override void EndInit()
        {
            base.EndInit();
            MovieInfoCombo.SelectedIndex = 0;
            this.Loaded += new RoutedEventHandler(MovieInfoControl_Loaded);
            this.Unloaded += new RoutedEventHandler(MovieInfoControl_Unloaded);
        }

        void MovieInfoControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ResetMissingFlags(this);
        }

        private string PatchStringItem(string dest, string alternate)
        {
            return string.IsNullOrEmpty(dest) ? alternate : dest;
        }

        private List<T> PatchListItem<T>(List<T> dest, List<T> alternate)
        {
            return dest == null || dest.Count == 0 ? alternate : dest;
        }

        private void PatchMediainfo(MediaInfoData dest)
        {
            if (dest != null)
            {
                dest.EmbeddedSubtitles = PatchListItem<EmbeddedSubtitle>(dest.EmbeddedSubtitles, MediaInfo.EmbeddedSubtitles);
                dest.ExternalSubtitlesList = PatchListItem<EmbeddedSubtitle>(dest.ExternalSubtitlesList, MediaInfo.ExternalSubtitlesList);
                dest.ContainerFormat = PatchStringItem(dest.ContainerFormat, MediaInfo.ContainerFormat);
                dest.AudioCodec = PatchStringItem(dest.AudioCodec, MediaInfo.AudioCodec);
                dest.AudioBitrate = PatchStringItem(dest.AudioBitrate, MediaInfo.AudioBitrate);
                dest.VideoCodec = PatchStringItem(dest.VideoCodec, MediaInfo.VideoCodec);
                dest.VideoBitrate = PatchStringItem(dest.VideoBitrate, MediaInfo.VideoBitrate);
                dest.OverallBitrate = PatchStringItem(dest.OverallBitrate, MediaInfo.OverallBitrate);
                dest.AspectRatio = PatchStringItem(dest.AspectRatio, MediaInfo.AspectRatio);
                dest.FileSizeBytes = PatchStringItem(dest.FileSizeBytes, MediaInfo.FileSizeBytes);
                dest.Duration = PatchStringItem(dest.Duration, MediaInfo.Duration);
                dest.FrameRate = PatchStringItem(dest.FrameRate, MediaInfo.FrameRate);
                dest.Language = PatchStringItem(dest.Language, MediaInfo.Language);
                dest.LanguageCode = PatchStringItem(dest.LanguageCode, MediaInfo.LanguageCode);
                dest.LanguageCodes = PatchListItem<string>(dest.LanguageCodes, MediaInfo.LanguageCodes);
                dest.Languages = PatchListItem<string>(dest.Languages, MediaInfo.Languages);
            }
        }

        public void LoadMyData()
        {
            try
            {
                nfoFileType nfofiletype = nfoFileType.Unknown;
                MyDataInfo = nfoHelper.LoadNfoFile(CurrentMoviePath, out nfofiletype);
                LoadedNfoFileType = nfoFileType.Unknown;
                LoadedNfoFileType = nfofiletype;

                m_IsNFoInfoMissing = MyDataInfo == null || MyDataInfo.IsEmpty;

                if (LoadedNfoFileType != nfoFileType.Unknown)
                {
                    Loggy.Logger.Debug("nfo loaded for " + CurrentMoviePath);
                }
                if (MyDataInfo != null)
                {
                    if (MyDataInfo.MediaInfo != null && MediaInfo != null)
                    {
                        // be smart. if the detected mediainfo has new items not present in the .nfo file, use them
                        PatchMediainfo(MyDataInfo.MediaInfo);
                        MediaInfo = MyDataInfo.MediaInfo;
                    }
                }

                // load metadata and select it if present
                this.MetadataInfo = MoviesheetsUpdateManager.CreateManagerForMovie(CurrentMoviePath).GetMovieInfo();
                if (this.MetadataInfo != null)
                {
                    if (this.MetadataInfo.MediaInfo != null && MediaInfo != null)
                    {
                        // be smart. if the detected mediainfo has new items not present in the .nfo file, use them
                        PatchMediainfo(MetadataInfo.MediaInfo);
                        MediaInfo = MetadataInfo.MediaInfo;
                    }
                }
                m_IsMetadataInfoMissing = this.MetadataInfo == null || this.MetadataInfo.IsEmpty;

                // apply imdbinfo to the my own nfo collector
                if (MyDataInfo != null)
                {
                    this.MyDataInfo = ApplyIMDbMovieInfoBehaviour(this.MyDataInfo, this.IMDBInfo);
                }

                this.SelectInfoSourceByPriority();
            }
            catch (Exception ex)
            {
                try
                {
                    Loggy.Logger.DebugException("Load nfo:", ex);
                }
                catch { /*needed for designtime*/ }
            }
        }

        public void MovieInfoControl_Loaded(object sender, RoutedEventArgs e)
        {
            // load it from file just once, at the beginning; be aware the Loaded event comes every time the control gets focus
            if (this.MyDataInfo == null)
            {
                LoadMyData();
            }
        }

        public bool Readonly
        {
            get { return (bool)GetValue(ReadonlyProperty); }
            set { SetValue(ReadonlyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Readonly.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReadonlyProperty =
            DependencyProperty.Register("Readonly", typeof(bool), typeof(MovieInfoControl), new UIPropertyMetadata(true));


        private static void OnPrefCollectorInfoChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MovieInfoControl _control = obj as MovieInfoControl;
            if (_control != null)
            {
                _control.m_IsPrefCollectorInfoMissing = _control.PrefCollectorInfo == null || _control.PrefCollectorInfo.IsEmpty;

                if (_control.IMDBInfo != null)
                {
                    _control.PrefCollectorInfo = ApplyIMDbMovieInfoBehaviour(_control.PrefCollectorInfo, _control.IMDBInfo);
                }

                _control.SelectInfoSourceByPriority();
            }
        }

        private static void ResetMissingFlags(MovieInfoControl control)
        {
            if (control != null)
            {
                control.m_IsMetadataInfoMissing = false;
                control.m_IsNFoInfoMissing = false;
                control.m_IsPrefCollectorInfoMissing = false;
            }
        }

        private static void OnCurrentMovieItemChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ResetMissingFlags(obj as MovieInfoControl);
            //MovieInfoControl _control = obj as MovieInfoControl;
            //if (_control != null && _control.CurrentMovieItem != null && _control.CurrentMovieItem.MovieInfo != null)
            //{
            //    _control.CurrentMovieItem.MovieInfo = ApplyIMDbMovieInfoBehaviour(_control.CurrentMovieItem.MovieInfo, args.NewValue as MovieInfo);

            //}
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink _link = sender as Hyperlink;
            if (_link != null && _link.NavigateUri != null)
            {
                string _dest = null;
                try
                {
                    try
                    {
                        _dest = _link.NavigateUri.OriginalString;
                        if (!_dest.Contains("http"))
                        {
                            throw new Exception();
                        }
                    }
                    catch
                    {
                        try
                        {
                            _dest = string.Format("http://www.imdb.com/title/tt{0}/", _dest);
                        }
                        catch { }
                    }
                    if (!string.IsNullOrEmpty(_dest))
                    {
                        Helpers.OpenUrlInBrowser(_dest);
                    }
                }
                catch { }
            }
        }

        private MovieInfo GetInfoBySourceType(MovieInfoProviderItemType sourceType)
        {
            MovieInfo _result = new MovieInfo();

            switch (sourceType)
            {
                case MovieInfoProviderItemType.CurrentCollector:
                    _result = CurrentMovieItem != null ? CurrentMovieItem.MovieInfo : null;
                    break;
                case MovieInfoProviderItemType.IMDB:
                    _result = IMDBInfo;
                    break;
                case MovieInfoProviderItemType.MyOwn:
                    _result = MyDataInfo;
                    break;
                case MovieInfoProviderItemType.Metadata:
                    _result = MetadataInfo;
                    break;
                case MovieInfoProviderItemType.PrefCollector:
                    _result = PrefCollectorInfo;
                    break;
            }

            return _result == null ? new MovieInfo() : _result;
        }

        public void SelectItemBySourceType(MovieInfoProviderItemType sourceType)
        {
            int _i = GetComboIndexBySourceType(sourceType);
            if (_i >= 0)
            {
                MovieInfoCombo.SelectedIndex = _i;
            }
        }

        private int GetComboIndexBySourceType(MovieInfoProviderItemType sourceType)
        {
            int _result = 0;

            switch (sourceType)
            {
                case MovieInfoProviderItemType.CurrentCollector:
                    _result = 1;
                    break;
                case MovieInfoProviderItemType.IMDB:
                    _result = 2;
                    break;
                case MovieInfoProviderItemType.MyOwn:
                    _result = 3;
                    break;
                case MovieInfoProviderItemType.Metadata:
                    _result = 4;
                    break;
                case MovieInfoProviderItemType.PrefCollector:
                    _result = 0;
                    break;
                default:
                    _result = 0;
                    break;
            }

            return _result;
        }

        public void SelectInfoSourceByPriority()
        {
            //if current source is CurrentSelector then do nothing... that means user selected it 
            if (MovieInfoCombo.SelectedIndex == 1)
            {
                return;
            }

            int _index = 1; // by default select the CurrentSelector (in case nothing else exists/can be found)
            foreach (MovieInfoProviderItemType _sourceType in FileManager.Configuration.Options.MovieSheetsOptions.MovieInfoPriorities)
            {
                bool _Missing = false;
                if (_sourceType == MovieInfoProviderItemType.Metadata && m_IsMetadataInfoMissing)
                {
                    _Missing = true;
                }
                if (_sourceType == MovieInfoProviderItemType.MyOwn && m_IsNFoInfoMissing)
                {
                    _Missing = true;
                }
                if (_sourceType == MovieInfoProviderItemType.PrefCollector && m_IsPrefCollectorInfoMissing)
                {
                    _Missing = true;
                }

                MovieInfo _info = GetInfoBySourceType(_sourceType);
                if (_info != null && !_info.IsEmpty && !_Missing)
                {
                    _index = GetComboIndexBySourceType(_sourceType);
                    FirstAvailableMovieInfo = _info;
                    break;
                }
            }
            //MovieInfoCombo.SelectedIndex = _index;
            MovieInfoCombo.SelectedIndex = -1;
            MovieInfoCombo.SelectedItem = null;
            MovieInfoCombo.SelectedItem = MovieInfoCombo.Items[_index];
        }

        public MovieInfo FirstAvailableMovieInfo = new MovieInfo();

        public static void DoGenerateNfoFile(bool silent, string moviePath, MovieInfo info, MediaInfoData mediainfo)
        {
            MovieInfo _info = null;
            try
            {
                _info = info;
            }
            catch { }

            if (_info == null)
            {
                return;
            }

            bool _doIt = true;
            
            if(File.Exists(FileManager.Configuration.GetMovieInfoPath(moviePath, false, MovieinfoType.Export))) // always use the Export naming for save
            {
                _doIt = MessageBox.Show("A MovieInfo file already exists. Do you want to replace it?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes;
            }

            if (_doIt && string.IsNullOrEmpty(_info.IMDBID))
            {
                if (silent)
                {
                    _doIt = true;
                }
                else
                {
                    _doIt = MessageBox.Show("Your data does not contain IMDb Id.\n\nIt is recommended to fill in the IMDB Id for a later more accurate detection.\n\nContinue without IMDb Id?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
                }
            }

            if (_doIt)
            {
                try
                {
                    nfoHelper.GenerateNfoFile(moviePath, info, mediainfo);
                    if (!silent)
                    {
                        MessageBox.Show("A .nfo file with the current Movie Info data was generated.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void GenerateNfoFile_Click(object sender, RoutedEventArgs e)
        {
            if (GenerateNfoFile.Visibility == Visibility.Visible)
            {
                DoGenerateNfoFile(false, CurrentMoviePath, SelectedMovieInfo(), MediaInfo);
                // refresh data for My Own info
                LoadMyData();
            }
        }

        public static MovieInfo ApplyIMDbMovieInfoBehaviour(MovieInfo input, MovieInfo imdbInfo)
        {
            if (input != null)
            {
                switch (FileManager.Configuration.Options.IMDBOptions.UsageBehaviour)
                {
                    case IMDBMovieInfoBehaviour.DoNotUseIMDBMovieInfo:
                        // do nothing
                        return input;
                    case IMDBMovieInfoBehaviour.FillMissingDataFromIMDB:
                        // fillup the missing items
                        if (imdbInfo != null)
                        {
                            return input.FillUpMissingItems(imdbInfo, FileManager.Configuration.Options.IMDBOptions.AlwaysUseIMDbRating);
                        }
                        else
                        {
                            return input;
                        }
                    case IMDBMovieInfoBehaviour.UseOnlyIMDBMovieInfo:
                        // return the complete IMDb info
                        if (imdbInfo != null && !string.IsNullOrEmpty(imdbInfo.IMDBID))
                        {
                            return imdbInfo;
                        }
                        else
                        {
                            return input;
                        }
                    default:
                        return input;
                }
            }
            else
            {
                return input;
            }
        }

        private void MovieInfoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0 && (e.AddedItems[0] as MovieInfoProviderItem).MovieInfoProviderItemType == MovieInfoProviderItemType.MyOwn)
            {
                this.myNfoImage.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                this.myNfoImage.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

    }

}
