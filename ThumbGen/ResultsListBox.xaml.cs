using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Collections;
using Microsoft.Win32;
using System.Windows.Documents;
using System.Drawing;
using CookComputing.XmlRpc;
using ThumbGen.Subtitles;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows.Controls.Primitives;
using ThumbGen.MovieSheets;
using System.Windows.Data;
using Fluent;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for ResultsList.xaml
    /// </summary>
    public partial class ResultsListBox : RibbonWindow
    {
        public static RoutedCommand UseFromMetadataRoutedCommand = new RoutedCommand();

        public static List<string> GalleryImagesSupported = new List<string>()
            {
                "*.jpg", "*.png", "*.bmp"
            };

        private static string IMAGES_FILTER = "Image Files (JPEG,GIF,BMP,PNG)|*.jpg;*.jpeg;*.gif;*.bmp;*.png|JPEG Files(*.jpg;*.jpeg)|*.jpg;*.jpeg|GIF Files(*.gif)|*.gif|BMP Files(*.bmp)|*.bmp|PNG Files(*.png)|*.png";

        public MoviesheetsUpdateManager MetadataManager = null;
        public MoviesheetsUpdateManager ParentFolderMetadataManager = null;

        public MovieSheetsGenerator MainGenerator { get; set; }
        public MovieSheetsGenerator ExtraGenerator = null;
        public MovieSheetsGenerator SpareGenerator;

        private ResultsListBox()
        {
            InitializeComponent();
        }

        public bool IsLoading = false;

        private Dictionary<string, CollectorNode> CollectorNodes = new Dictionary<string, CollectorNode>();
        public static Dictionary<string, IMDBMovieInfoCacheItem> IMDbMovieInfoCache = new Dictionary<string, IMDBMovieInfoCacheItem>();

        // used temporary
        public string IMDbId;
        private bool IMDb_FromOutside;
        public string Keywords;

        public FileWatcher SubsWatcher = null;

        public MovieInfo IMDBData
        {
            get { return (MovieInfo)GetValue(IMDBDataProperty); }
            set { SetValue(IMDBDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IMDBData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IMDBDataProperty =
            DependencyProperty.Register("IMDBData", typeof(MovieInfo), typeof(ResultsListBox),
                new UIPropertyMetadata(null, OnIMDBDataChanged));

        private static void OnIMDBDataChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ResultsListBox _form = obj as ResultsListBox;
            if (_form != null)
            {
                ResultItemBase _selItem = _form.ResultsTree.SelectedItem as ResultItemBase;
                if (_selItem != null)
                {
                    _selItem.MovieInfo = MovieInfoControl.ApplyIMDbMovieInfoBehaviour(_selItem.MovieInfo, args.NewValue as MovieInfo);
                    _form.SkipSelecting = true;
                    _selItem.IsSelected = false;
                    _form.SkipSelecting = true;
                    _selItem.IsSelected = true;
                }
            }
        }

        public MovieInfo MetadataInfo
        {
            get { return (MovieInfo)GetValue(MetadataInfoProperty); }
            set { SetValue(MetadataInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MetadataInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MetadataInfoProperty =
            DependencyProperty.Register("MetadataInfo", typeof(MovieInfo), typeof(ResultsListBox), new UIPropertyMetadata(null, OnMetadataInfoChanged));

        private static void OnMetadataInfoChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {

        }

        public MovieInfo PrefCollectorMovieInfo
        {
            get { return (MovieInfo)GetValue(PrefCollectorMovieInfoProperty); }
            set { SetValue(PrefCollectorMovieInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrefCollectorMovieInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrefCollectorMovieInfoProperty =
            DependencyProperty.Register("PrefCollectorMovieInfo", typeof(MovieInfo), typeof(ResultsListBox), new UIPropertyMetadata(null, OnPrefCollectorMovieInfoChanged));

        private static void OnPrefCollectorMovieInfoChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ResultsListBox _form = obj as ResultsListBox;
            if (_form != null)
            {

            }
        }

        private static string CurrentFolder = null;
        //private static string CurrentMovie = null;

        public string CurrentMoviePath
        {
            get { return (string)GetValue(CurrentMoviePathProperty); }
            set { SetValue(CurrentMoviePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentMoviePath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentMoviePathProperty =
            DependencyProperty.Register("CurrentMoviePath", typeof(string), typeof(ResultsListBox), new UIPropertyMetadata(null));

        public List<BackdropBase> OwnBackdrops = new List<BackdropBase>();

        public ObservableCollection<FileInfoItem> CurrentSubtitles = new ObservableCollection<FileInfoItem>();
        public ObservableCollection<CultureInfo> CurrentLanguages = new ObservableCollection<CultureInfo>();

        public CollectorNode OwnVideoSnapshots { get; private set; }
        public ObservableCollection<ResultItemBase> OwnVideoSnapshotsResults { get; set; }
        public ResultMovieItem OwnVideoSnapshotMixed { get; set; }

        public CollectorNode OwnThumbnailFromDisk { get; private set; }
        public ResultMovieItem OwnThumbnailFromDiskItem { get; set; }
        public static string OWN_THUMBNAIL_FROM_DISK;

        public CollectorNode GalleryCollectorNode { get; private set; }
        public static ObservableCollection<ResultItemBase> MyGalleryResults { get; set; }

        //public LayoutsManager LayoutManager
        //{
        //    get { return (LayoutsManager)GetValue(LayoutManagerProperty); }
        //    set { SetValue(LayoutManagerProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for LayoutManager.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty LayoutManagerProperty =
        //    DependencyProperty.Register("LayoutManager", typeof(LayoutsManager), typeof(ResultsListBox), new UIPropertyMetadata(null));

        public static LayoutsManager LayoutManager = new LayoutsManager();

        public BaseCollector SelectedCollector { get; private set; }

        public CollectorNode PreferredCoverCollector { get; private set; }

        public static DialogResult Show(Window owner, Collection<ResultMovieItem> items,
                                        ObservableCollection<ResultItemBase> ownSnapshots, string currentMovieFilePath,
                                        string imdbId, string keywords)
        {
            DialogResult _result = null;

            string existingImage = Helpers.GetCorrectThumbnailPath(currentMovieFilePath, false);

            ResultsListBox _form = new ResultsListBox();
            _form.Owner = owner;
            _form.Closing += new System.ComponentModel.CancelEventHandler(_form_Closing);
            _form.Loaded += new RoutedEventHandler(_form_Loaded);
            _form.IsLoading = true;
            _form.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(_form_PreviewMouseLeftButtonDown);

            //_form.LayoutManager = new LayoutsManager();
            ResultsListBox.LayoutManager.DockManager = _form.DockManager;

            CurrentFolder = System.IO.Path.GetDirectoryName(currentMovieFilePath);
            _form.CurrentMoviePath = currentMovieFilePath;

            _form.MetadataManager = MoviesheetsUpdateManager.CreateManagerForMovie(currentMovieFilePath);
            _form.ParentFolderMetadataManager = MoviesheetsUpdateManager.CreateManagerForParentFolder(currentMovieFilePath);

            _form.MainGenerator = new MovieSheetsGenerator(SheetType.Main, currentMovieFilePath);
            _form.ExtraGenerator = _form.MainGenerator.Clone(false, SheetType.Extra);
            _form.SpareGenerator = new MovieSheetsGenerator(SheetType.Spare, currentMovieFilePath);
            _form.SpareGenerator.MovieInfo = new MovieInfo();
            string _parentFolderName = Helpers.GetMovieParentFolderName(currentMovieFilePath, "");
            if (Helpers.IsDVDPath(currentMovieFilePath))
            {
                _parentFolderName = Helpers.GetMovieParentFolderName(Helpers.GetDVDRootDirectory(currentMovieFilePath), "");
            }
            if (Helpers.IsBlurayPath(currentMovieFilePath))
            {
                _parentFolderName = Helpers.GetMovieParentFolderName(Helpers.GetBlurayRootDirectory(currentMovieFilePath), "");
            }
            _form.SpareGenerator.MovieInfo.Name = _parentFolderName;

            // store the imdbId received from outside
            _form.IMDbId = imdbId;
            _form.IMDb_FromOutside = !string.IsNullOrEmpty(imdbId);
            // store keywords used
            _form.Keywords = keywords;
            _form.SubsCombo.DataContext = _form.CurrentSubtitles;
            _form.LanguagesCombo.DataContext = _form.CurrentLanguages;
            _form.dcExternalSubs.Visibility = FileManager.DisableOpenSubtitles ? Visibility.Collapsed : Visibility.Visible;
            _form.MovieInfoControl.DataContext = _form;

            // query data from metadata file
            ThreadPool.QueueUserWorkItem(new WaitCallback(QueryMetadataInfo), _form);

            // query the MediaInfo for the current file
            if (!FileManager.DisableMediaInfo)
            {
                //ThreadPool.QueueUserWorkItem(new WaitCallback(QueryMediaInfo), _form);
                QueryMediaInfo(_form);
            }

            

            _form.Title = string.Format("Current movie: {0}", currentMovieFilePath);
            _form.ParentMovieFolderBlock.Text = Helpers.GetMovieParentFolderName(currentMovieFilePath, null);
            _form.ChooseSeriesThumb.Visibility = string.IsNullOrEmpty(_form.ParentMovieFolderBlock.Text) ? Visibility.Collapsed : Visibility.Visible;
            _form.CurrentFolderBlock.Text = Path.GetFileName(Path.GetDirectoryName(currentMovieFilePath));

            // show/hide the batch apply button (hide it if there is only one movie file in the current folder)
            _form.grpBatchProcessing.Visibility = new FilesCollector().CollectFiles(CurrentFolder, false).Count() == 1 ? Visibility.Collapsed : Visibility.Visible;

            // refresh existing image
            RefreshExistingThumbnail(existingImage, _form);

            // load folder.jpg if exists; for parent of current folder
            if (!string.IsNullOrEmpty(Path.GetDirectoryName(CurrentFolder)))
            {
                //string _folderJpg = Path.Combine(Path.GetDirectoryName(CurrentFolder), FileManager.Configuration.Options.NamingOptions.FolderjpgName);
                string _folderJpg = FileManager.Configuration.GetFolderjpgPath(Path.GetDirectoryName(currentMovieFilePath), false);
                if (!string.IsNullOrEmpty(_folderJpg) && File.Exists(_folderJpg))
                {
                    _form.FolderImage.Source = Helpers.LoadImage(_folderJpg);
                }
            }

            // load folder.jpg if exists; for current folder
            string _currentFolderJpg = FileManager.Configuration.GetFolderjpgPath(currentMovieFilePath, false);
            if (!string.IsNullOrEmpty(_currentFolderJpg) && File.Exists(_currentFolderJpg))
            {
                _form.CurrentFolderImage.Source = Helpers.LoadImage(_currentFolderJpg);
            }

            _form.CollectorNodes.Clear();
            // prepare list with collectors that have items
            foreach (ResultMovieItem item in items)
            {
                if (!_form.CollectorNodes.ContainsKey(item.CollectorName))
                // collector node was not found, add it
                {
                    CollectorNode _collector = new CollectorNode(item.CollectorName);
                    _collector.SetCollector(FileManager.CurrentCollector[item.CollectorName]);
                    _form.CollectorNodes.Add(item.CollectorName, _collector);
                    if (_collector.Name == BaseCollector.VIDEOSNAP)
                    {
                        _form.OwnVideoSnapshots = _collector;
                    }
                }
            }

            if (_form.OwnVideoSnapshots == null)
            {
                // add the video snapshots collector
                _form.OwnVideoSnapshots = new CollectorNode(BaseCollector.VIDEOSNAP);
                _form.CollectorNodes.Add(BaseCollector.VIDEOSNAP, _form.OwnVideoSnapshots);

                string _filename = Helpers.GetUniqueFilename(FileManager.Configuration.Options.NamingOptions.ThumbnailExtension);
                _form.OwnVideoSnapshotMixed = new ResultMovieItem(null, "Custom Thumbnail", _filename, BaseCollector.VIDEOSNAP);
                _form.OwnVideoSnapshotMixed.MovieInfo.IMDBID = imdbId;
                FileManager.GarbageFiles.Add(_filename);
                _form.OwnVideoSnapshots.Results.Add(_form.OwnVideoSnapshotMixed);

                _form.OwnVideoSnapshotsResults = new ObservableCollection<ResultItemBase>();
                _form.SnapshotsBox.DataContext = _form.OwnVideoSnapshotsResults;
            }

            // add "own from disk" collector
            if (_form.OwnThumbnailFromDisk == null)
            {
                _form.OwnThumbnailFromDisk = new CollectorNode(BaseCollector.OWNFROMDISK);
                _form.CollectorNodes.Add(BaseCollector.OWNFROMDISK, _form.OwnThumbnailFromDisk);
                _form.OwnThumbnailFromDiskItem = new ResultMovieItem(null, "Custom Thumbnail", OWN_THUMBNAIL_FROM_DISK, BaseCollector.OWNFROMDISK);
                _form.OwnThumbnailFromDiskItem.MovieInfo.IMDBID = imdbId;
                _form.OwnThumbnailFromDisk.Results.Add(_form.OwnThumbnailFromDiskItem);
            }

            // add MyGallery collector
            if (_form.GalleryCollectorNode == null)
            {
                if (MyGalleryResults.Count != 0)
                {
                    _form.GalleryCollectorNode = new CollectorNode(BaseCollector.GALLERY);
                    _form.CollectorNodes.Add(BaseCollector.GALLERY, _form.GalleryCollectorNode);
                    // add all items found in Gallery
                    _form.Dispatcher.BeginInvoke((Action)delegate
                    {
                        _form.GalleryCollectorNode.Results.AddRange(MyGalleryResults);
                    }, DispatcherPriority.Background);
                }
            }

            // if snapshots are provided, store them
            if (ownSnapshots != null && ownSnapshots.Count != 0)
            {
                foreach (ResultItemBase _item in ownSnapshots)
                {
                    _form.OwnVideoSnapshotsResults.Add(_item);
                }
            }

            _form.PreferredCoverCollector = null;

            // take each result image and build up movies folder 
            foreach (ResultMovieItem item in items)
            {
                if (_form.IMDb_FromOutside && !string.IsNullOrEmpty(item.MovieInfo.IMDBID) && string.Compare(_form.IMDbId, item.MovieInfo.IMDBID) != 0)
                {
                    // if IMDB id is provided from outside, skip all results that have a different IMDBId
                    continue;
                }

                CollectorNode _collector = _form.CollectorNodes[item.CollectorName];

                if (string.Compare(_collector.Name, FileManager.Configuration.Options.PreferedCoverCollector, true) == 0)
                {
                    _form.PreferredCoverCollector = _collector;
                }

                ResultMovieFolder _movie = null;
                // if the item has a movie id, create/locate movie node
                if (!string.IsNullOrEmpty(item.MovieId))
                {
                    // image belongs to a movie that has id, locate the item or create it if missing
                    foreach (ResultItemBase _match in _collector.Results)
                    {
                        if (_match is ResultMovieFolder && (string.Compare(_match.MovieId, item.MovieId, StringComparison.InvariantCulture) == 0))
                        {
                            _movie = _match as ResultMovieFolder;
                            break;
                        }
                    }
                    if (_movie == null)
                    {
                        _movie = new ResultMovieFolder(item.MovieId, item.Title, item.CollectorName);
                        _movie.MovieInfo = item.MovieInfo; // copy also movie info
                        _movie.CollectorMovieUrl = item.CollectorMovieUrl;
                        _collector.Results.Add(_movie);
                    }
                }
                // add the current image result to the _movie (if any) or under collector
                if (_movie != null)
                {
                    item.Title = string.Format("Image {0}", _movie.Images.Count + 1);
                    _movie.Images.Add(item);
                    // if item has no imdbid and the autofill option is set, use it for first item
                    if (FileManager.Configuration.Options.IMDBOptions.AutofillIMDbIdForFirstMovieIfMissing && _collector.Results.Count == 1 &&
                       !string.IsNullOrEmpty(imdbId) && string.IsNullOrEmpty(item.MovieInfo.IMDBID))
                    {
                        item.MovieInfo.IMDBID = imdbId;
                        // update also all backdrops with the imdbIDd
                        foreach (BackdropBase _bb in _collector.Collector.BackdropsList)
                        {
                            if (_bb.MovieId == item.MovieId)
                            {
                                _bb.IMDbId = imdbId;
                            }
                        }
                    }
                }
                else
                {
                    _collector.Results.Add(item);
                }
            }

            // set data context to the results list
            _form.ResultsTree.DataContext = _form.CollectorNodes.Values;
            // set data context for the autogenerate options
            _form.grdAutogen.DataContext = FileManager.Configuration.Options;
            _form.grdMoviesheetGen.DataContext = FileManager.Configuration.Options;
            _form.DockManager.DataContext = FileManager.Configuration.Options;
            _form.dcCustomText.DataContext = FileManager.Configuration.Options;
            _form.GetRandomSnapshotsMix.DataContext = FileManager.Configuration.Options;
            _form.Watermark.DataContext = FileManager.Configuration.Options;

            try
            {
                var res = _form.ShowDialog();
                //if (res.HasValue && res.Value)
                if(_form.Action == ResultsDialogAction.Done)
                {
                    _result = new DialogResult(_form.ResultsTree.SelectedItem as ResultMovieItem, _form.Action);
                }
                else
                {
                    _result = new DialogResult(null, _form.Action);
                }
                _result.SelectedMovieInfo = _form.GetSelectedMovieInfo();
            }
            catch
            {
                _result = null;
            }

            return _result;
        }

        private ResultItemBase GetSeasonCover(CollectorNode cnode)
        {
            ResultItemBase _result = null;

            try
            {
                if (cnode != null && cnode.Results != null && cnode.Results.Count() != 0)
                {
                    // checks if the results list has at least one movie and that movie has an image marked IsSeasonCover and returns it if yes
                    var _r = from c in (cnode.Results.ElementAt(0) as ResultMovieFolder).Images
                             where c is ResultMovieItem && (c as ResultMovieItem).IsSeasonCover
                             select c;
                    if (_r != null && _r.Count() != 0)
                    {
                        _result = _r.First();
                    }
                }
            }
            catch { }
            return _result;
        }

        public void SelectCoverByPreferredCollector()
        {

            // select first image available if no preferred collector available for covers
            try
            {
                CollectorNode _cn = this.PreferredCoverCollector == null ? this.CollectorNodes.First().Value : this.PreferredCoverCollector;

                ResultItemBase _selection = GetSeasonCover(_cn);
                if (_selection == null)
                {
                    _selection = _cn.Results.First();
                }
                if (_selection is ResultMovieFolder)
                {
                    _selection = (_selection as ResultMovieFolder).Images.First();
                }
                if (_selection != null)
                {
                    _selection.IsSelected = true;
                }
            }
            catch { }

        }

        static void _form_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ResultsListBox _form = sender as ResultsListBox;
            if (_form != null && _form.AutoAdorner != null)
            {
                Loggy.Logger.Debug("Autoadorner cancelled by click");
                _form.AutoAdorner.Cancel();
            }
        }

        private static void RefreshExistingThumbnail(string existingImage, ResultsListBox form)
        {
            if (form != null && File.Exists(existingImage))
            {
                // load existing image
                form.existingimage.Source = Helpers.LoadImage(existingImage);
                BitmapImage _bmp = form.existingimage.Source as BitmapImage;
                double _width = _bmp != null ? _bmp.PixelWidth : 0;
                double _height = _bmp != null ? _bmp.PixelHeight : 0;
                long _length = _bmp != null ? new FileInfo(existingImage).Length : 0;
                form.existingImageSize.Text = Helpers.GetFormattedImageSize(_bmp, _width, _height, _length);
            }
        }

        static void _form_Loaded(object sender, RoutedEventArgs e)
        {
            Loggy.Logger.Debug("ResultsPage loaded");
            ResultsListBox _form = sender as ResultsListBox;
            if (_form != null)
            {
                new OverlayAdornerHelper(_form.TheMainGrid, new LoadingScreen("Preparing results. Please wait...", false));

                _form.Dispatcher.BeginInvoke((Action)delegate
                {
                    try
                    {
                        Loggy.Logger.Debug("Deferred processing started");
                        // restore layout
                        string _lastLayout = FileManager.Configuration.Options.LastMovieResultsLayoutUsed;
                        if (string.IsNullOrEmpty(_lastLayout))
                        {
                            _lastLayout = ResultsListBox.LayoutManager.DefaultLayoutPath;
                        }
                        ResultsListBox.LayoutManager.RefreshProfiles(ResultsListBox.LayoutManager.GetProfileName(_lastLayout));
                        _form.LayoutSelector.ProfilesCombo.SelectedItem = ResultsListBox.LayoutManager.SelectedProfile;

                        // profile is refreshed now autoselect cover by preferred collector
                        _form.SelectCoverByPreferredCollector();
                        Loggy.Logger.Debug("Selected cover by pref collector");

                        if (!FileManager.Configuration.Options.DisableSearch)
                        {
                            // query the IMDB info
                            QueryIMDbInfo(_form);
                            Loggy.Logger.Debug("IMDb query sent");

                            // query data from the prefered collector
                            _form.QueryPrefCollectorInfo();
                            Loggy.Logger.Debug("Pref info collector sent");
                        }


                        _form.MovieInfoControl.SelectInfoSourceByPriority();
                        Loggy.Logger.Debug("Selected info by priority");

                        _form.WatermarkColor.ItemsSource = typeof(System.Windows.Media.Brushes).GetProperties();

                        try
                        {
                            _form.comboImageStretch.SelectedIndex = FileManager.Configuration.Options.KeepAspectRatio ? 0 : 1;
                        }
                        catch { }

                        try
                        {
                            _form.comboThumbSize.SelectedIndex = 0;
                            foreach (ComboBoxItem item in _form.comboThumbSize.Items)
                            {
                                if ((int)((System.Windows.Size)item.Tag).Width == (int)FileManager.Configuration.Options.ThumbnailSize.Width &&
                                   (int)((System.Windows.Size)item.Tag).Height == (int)FileManager.Configuration.Options.ThumbnailSize.Height)
                                {
                                    _form.comboThumbSize.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                        catch { }

                        _form.RefreshSubtitles();
                        Loggy.Logger.Debug("Subtitles refreshed");

                        _form.RefreshLanguage();
                        Loggy.Logger.Debug("Languages refreshed");
                        _form.RefreshExistingMoviesheetSmall();
                        Loggy.Logger.Debug("Existing sheets refreshed");

                        if (FileManager.EnableMovieSheets)
                        {
                            try
                            {
                                _form.TemplatesCombo.TemplatesMan.RefreshTemplates(FileManager.Configuration.Options.MovieSheetsOptions.TemplateName);
                                _form.TemplatesCombo.TemplatesCombobox.SelectedValue = _form.TemplatesCombo.TemplatesMan.SelectedTemplate;
                                _form.MainGenerator.SelectedTemplate = _form.TemplatesCombo.TemplatesMan.SelectedTemplate;

                                _form.TemplatesComboExtra.TemplatesMan.RefreshTemplates(FileManager.Configuration.Options.MovieSheetsOptions.ExtraTemplateName);
                                _form.TemplatesComboExtra.TemplatesCombobox.SelectedValue = _form.TemplatesComboExtra.TemplatesMan.SelectedTemplate;
                                _form.ExtraGenerator.SelectedTemplate = _form.TemplatesComboExtra.TemplatesMan.SelectedTemplate;

                                _form.TemplatesComboParentFolder.TemplatesMan.RefreshTemplates(FileManager.Configuration.Options.MovieSheetsOptions.ParentFolderTemplateName);
                                _form.TemplatesComboParentFolder.TemplatesCombobox.SelectedValue = _form.TemplatesComboParentFolder.TemplatesMan.SelectedTemplate;
                                _form.SpareGenerator.SelectedTemplate = _form.TemplatesComboParentFolder.TemplatesMan.SelectedTemplate;

                                Loggy.Logger.Debug("Available templates refreshed");
                            }
                            catch { }

                            try
                            {
                                // if required populate the moviesheet with default values
                                ResultMovieItem _selectedItem = _form.ResultsTree.SelectedItem as ResultMovieItem;

                                // process mediainfo
                                _form.MainGenerator.MediaInfo = _form.MediaInfoControl.MediaData;
                                _form.SpareGenerator.MediaInfo = _form.MediaInfoControl.MediaData;

                                // process movieinfo
                                _form.MainGenerator.MovieInfo = _form.GetSelectedMovieInfo();
                                _form.SpareGenerator.MovieInfo = _form.MainGenerator.MovieInfo;

                                // process / import images
                                ImagesProcessor _imgProcessor = new ImagesProcessor(_form.CurrentMoviePath);
                                _imgProcessor.MainGenerator = _form.MainGenerator;
                                _imgProcessor.ExtraGenerator = _form.ExtraGenerator;
                                _imgProcessor.SpareGenerator = _form.SpareGenerator;
                                _imgProcessor.DefaultCoverPath = _selectedItem != null ? _selectedItem.ImageUrl : string.Empty;

                                _imgProcessor.OwnBackdrops = _form.OwnBackdrops;

                                if (_form.SelectedCollector != null && _selectedItem != null)
                                {

                                    _imgProcessor.Backdrops = _form.GetBackdropsByMovie(_selectedItem, _form.SelectedCollector);
                                    Loggy.Logger.Debug("Backdrops by movie refreshed");
                                }

                                _imgProcessor.ImportImages();
                                Loggy.Logger.Debug("Autoselected images loaded");

                                _form.UpdateBackdropsList();
                                Loggy.Logger.Debug("Backdrops list updated");

                                if (_imgProcessor.IsMyOwnThumbnailFromDiskImageRequired)
                                {
                                    _form.UpdateMyOwnThumbnailFromDiskImage(_imgProcessor.CoverPath);
                                }

                                _imgProcessor = null;
                                Loggy.Logger.Debug("Images processor disposed");

                                if (FileManager.Configuration.Options.MovieSheetsOptions.AutorefreshPreview)
                                {
                                    _form.RefreshMovieSheetSmall(true);
                                    _form.RefreshMovieSheetSmallForParent();
                                    Loggy.Logger.Debug("Autorefresh sheets on - rendering done");
                                }
                                else
                                {
                                    Loggy.Logger.Debug("Autorefresh sheets off");
                                }

                                _form.RefreshMetadataMoviesheetSmall();
                                Loggy.Logger.Debug("Preview refreshed");
                            }
                            finally
                            {
                                OverlayAdornerHelper.RemoveAllAdorners(_form.TheMainGrid);
                                Loggy.Logger.Debug("Adorners removed");

                                if (FileManager.Mode == ProcessingMode.SemiAutomatic)
                                {
                                    Loggy.Logger.Debug("Starting semiauto timer");
                                    _form.AutoAdorner = new AutomaticAdornerHelper(_form.OK, FileManager.Configuration.Options.SemiautomaticTimeout);
                                }
                            }
                        }
                    }
                    finally
                    {
                        _form.IsLoading = false;
                        Loggy.Logger.Debug("Finalized deferred processing");
                    }
                }, DispatcherPriority.ApplicationIdle);
            }
        }



        private AutomaticAdornerHelper AutoAdorner;

        static void SubsWatcher_Changed(object sender, EventArgs e)
        {
            ResultsListBox _form = (e as MonitorEventArgs).Data as ResultsListBox;
            if (_form != null)
            {
                _form.RefreshSubtitles();
            }
        }

        private static void QueryMetadataInfo(object param)
        {
            try
            {
                ResultsListBox _form = param as ResultsListBox;
                if (_form != null)
                {
                    string _moviePath = null;
                    _form.Dispatcher.Invoke((Action)delegate
                    {
                        _moviePath = _form.CurrentMoviePath;
                    });
                    MovieInfo _temp = null;
                    _temp = MoviesheetsUpdateManager.CreateManagerForMovie(_moviePath).GetMovieInfo();
                    FileManager.CurrentCollector.AddCoversAndBackdropsToMovieInfo(_temp, _moviePath);
                    if (_temp != null)
                    {
                        _form.Dispatcher.Invoke((Action)delegate
                        {
                            // force triggering data changed
                            _form.MetadataInfo = null;
                            _form.MetadataInfo = _temp;
                        });
                    }
                }
            }
            catch { }
        }

        private static void QueryIMDbInfo(object param)
        {
            try
            {
                ResultsListBox _form = param as ResultsListBox;
                if (_form != null && !string.IsNullOrEmpty(_form.IMDbId))
                {
                    string _moviePath = null;
                    _form.Dispatcher.Invoke((Action)delegate
                    {
                        _moviePath = _form.CurrentMoviePath;
                    }, DispatcherPriority.Normal);

                    MovieInfo _temp = null;

                    //Country _country = _form.SelectedCollector != null ? _form.SelectedCollector.Country : Country.International;
                    string _country = FileManager.Configuration.Options.IMDBOptions.CertificationCountry;
                    string _cacheKey = _form.IMDbId + _country;

                    if (ResultsListBox.IMDbMovieInfoCache.ContainsKey(_cacheKey))
                    {
                        IMDBMovieInfoCacheItem _cacheItem = ResultsListBox.IMDbMovieInfoCache[_cacheKey];
                        if (_cacheItem != null && string.Compare(_cacheItem.CountryCode, _country, true) == 0)
                        {
                            _temp = _cacheItem.MovieInfo;
                        }
                    }
                    if (_temp == null)
                    {
                        _temp = new IMDBMovieInfo().GetMovieInfo(_form.IMDbId, _country);
                        ResultsListBox.IMDbMovieInfoCache.Add(_cacheKey, new IMDBMovieInfoCacheItem(_country, _temp));
                    }

                    _form.Dispatcher.Invoke((Action)delegate
                    {
                        if (_temp != null && _form.PrefCollectorMovieInfo != null && !string.IsNullOrEmpty(_form.PrefCollectorMovieInfo.Name))
                        {
                            _form.PrefCollectorMovieInfo = MovieInfoControl.ApplyIMDbMovieInfoBehaviour(_form.PrefCollectorMovieInfo, _temp);
                            MovieInfo _tmp = _form.PrefCollectorMovieInfo;
                            _form.PrefCollectorMovieInfo = null;
                            _form.PrefCollectorMovieInfo = _tmp;
                        }

                        FileManager.CurrentCollector.AddCoversAndBackdropsToMovieInfo(_temp, _moviePath);

                        // force triggering data changed
                        _form.IMDBData = null;
                        _form.IMDBData = _temp;
                    }, DispatcherPriority.Normal);
                }
            }
            catch { }
        }

        private void QueryPrefCollectorInfo()
        {
            try
            {
                MovieInfo _temp = null;

                if (!FileManager.Configuration.Options.MovieSheetsOptions.DisablePreferredInfoCollector)
                {
                    bool _found = false;
                    // check if the PreferredInfoCollector is already in the selected collectors (if yes, reuse info from that guy)
                    foreach (KeyValuePair<string, CollectorNode> _pair in this.CollectorNodes)
                    {
                        if (string.Compare(_pair.Key, FileManager.Configuration.Options.PreferedInfoCollector, true) == 0)
                        {
                            // we found our collector
                            BindingList<MovieInfo> _candidates = new BindingList<MovieInfo>();
                            foreach (ResultMovieFolder _mFolder in _pair.Value.Results)
                            {
                                if (this.IMDb_FromOutside && !string.IsNullOrEmpty(this.IMDbId) && !string.IsNullOrEmpty(_mFolder.MovieInfo.IMDBID) && this.IMDbId != _mFolder.MovieInfo.IMDBID)
                                {
                                    continue; // IMDBID does not match with ours
                                }
                                if (this.IMDb_FromOutside && !string.IsNullOrEmpty(this.IMDbId) && !string.IsNullOrEmpty(_mFolder.MovieInfo.IMDBID) && this.IMDbId == _mFolder.MovieInfo.IMDBID)
                                {
                                    // we found it by IMDBId
                                    _temp = _mFolder.MovieInfo;
                                    _found = true;

                                    if (_temp != null && !string.IsNullOrEmpty(_temp.TVDBID))
                                    {
                                        CurrentSeriesHelper.SeriesID = _temp.TVDBID;
                                        CurrentSeriesHelper.SeriesIMDBID = _temp.IMDBID;
                                        CurrentSeriesHelper.SeriesName = _temp.Name;
                                    }

                                    break;
                                }
                                else
                                {
                                    // put it to the candidates list
                                    _candidates.Add(_mFolder.MovieInfo);
                                }
                            }
                            if (_temp == null)
                            {
                                ChooseMovieDialogResult _dresult = ChooseMovieFromIMDb.GetCorrectMovie(this, _candidates, "", false);
                                _temp = _dresult != null ? _dresult.MovieInfo : null;
                                _found = _temp != null;
                            }
                            // jump out as we found the collector
                            break;
                        }
                    }

                    if (!_found)
                    {
                        Executor _exec = new Executor(this.CurrentMoviePath);
                        _temp = _exec.QueryPreferredCollector(this.IMDbId, this.Keywords);

                        // to be removed
                        //BaseCollector _prefCollector = BaseCollector.GetMovieCollector(FileManager.Configuration.Options.PreferedInfoCollector);
                        //if (_prefCollector != null)
                        //{
                        //    _prefCollector.IMDBID = this.IMDb_FromOutside ? this.IMDbId : null;
                        //    _prefCollector.CurrentMovie = new MovieItem(this.CurrentMoviePath);
                        //    _temp = _prefCollector.QueryMovieInfo(_prefCollector.IMDBID);
                        //    if ((_temp == null) || (string.IsNullOrEmpty(_temp.Name)))
                        //    {
                        //        // ask user
                        //        _prefCollector.ClearResults();
                        //        _prefCollector.GetResults(this.Keywords, _prefCollector.IMDBID, true);
                        //        BindingList<MovieInfo> _candidates = new BindingList<MovieInfo>();
                        //        foreach (ResultItemBase _rib in _prefCollector.ResultsList)
                        //        {
                        //            if (this.IMDb_FromOutside && !string.IsNullOrEmpty(this.IMDbId) && !string.IsNullOrEmpty(_rib.MovieInfo.IMDBID) && this.IMDbId != _rib.MovieInfo.IMDBID)
                        //            {
                        //                continue; // IMDBID does not match with ours
                        //            }
                        //            _candidates.Add(_rib.MovieInfo);
                        //        }
                        //        ChooseMovieDialogResult _dresult = ChooseMovieFromIMDb.GetCorrectMovie(this, _candidates, "", false);
                        //        _temp = _dresult != null ? _dresult.MovieInfo : null;
                        //    }
                        //}
                    }

                    if (_temp != null)
                    {
                        if (string.IsNullOrEmpty(_temp.IMDBID) && this.IMDb_FromOutside)
                        {
                            _temp.IMDBID = this.IMDbId;
                        }
                        if (this.IMDb_FromOutside)
                        {
                            _temp = MovieInfoControl.ApplyIMDbMovieInfoBehaviour(_temp, this.IMDBData);
                        }
                    }

                    // force triggering data changed
                    this.PrefCollectorMovieInfo = null;
                    this.PrefCollectorMovieInfo = _temp;
                }
            }

            catch { }
        }

        private static void QueryMediaInfo(object param)
        {
            try
            {
                ResultsListBox _form = param as ResultsListBox;
                if (_form != null)
                {
                    string _path = null;
                    _form.Dispatcher.Invoke((Action)delegate
                    {
                        _path = _form.CurrentMoviePath;
                        try
                        {
                            string _info = null; 
                            _form.MediaInfoControl.MediaData = MediaInfoManager.GetMediaInfoData(_path, true, false, true, out _info);
                            _form.MediaInfoControl.TextContent = _info;
                            _form.MainGenerator.MediaInfo = _form.MediaInfoControl.MediaData;
                            _form.SpareGenerator.MediaInfo = _form.MediaInfoControl.MediaData;
                        }
                        catch { }
                    });
                }
            }
            catch { }
        }

        static void DraggableExtender_Dragged(object sender, EventArgs e)
        {
            TextBlock _watermark = sender as TextBlock;
            if (_watermark != null)
            {

            }
        }

        public ResultsDialogAction Action { get; private set; }

        private static void _form_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                ResultsListBox _form = sender as ResultsListBox;
                if (_form != null)
                {
                    _form.UpdateThumbSizeAndDefaults();
                    _form.UpdateDefaultThumbnailSize();

                    

                    if ((_form.TemplatesCombo.TemplatesCombobox.SelectedItem as TemplateItem) != null)
                    {
                        FileManager.Configuration.Options.MovieSheetsOptions.TemplateName = (_form.TemplatesCombo.TemplatesCombobox.SelectedItem as TemplateItem).TemplateName;
                    }

                    if ((_form.TemplatesComboExtra.TemplatesCombobox.SelectedItem as TemplateItem) != null)
                    {
                        FileManager.Configuration.Options.MovieSheetsOptions.ExtraTemplateName = (_form.TemplatesComboExtra.TemplatesCombobox.SelectedItem as TemplateItem).TemplateName;
                    }

                    if ((_form.TemplatesComboParentFolder.TemplatesCombobox.SelectedItem as TemplateItem) != null)
                    {
                        FileManager.Configuration.Options.MovieSheetsOptions.ParentFolderTemplateName = (_form.TemplatesComboParentFolder.TemplatesCombobox.SelectedItem as TemplateItem).TemplateName;
                    }

                    try
                    {
                    ResultsListBox.LayoutManager.DockManager.SaveLayout(ResultsListBox.LayoutManager.SelectedProfile.ProfilePath);
                    }
                    catch (Exception ex)
                    {
                        Loggy.Logger.Error("Exception while storing layout: " + ex.Message);
                    }
                    Thread.Sleep(300);
                    FileManager.Configuration.Options.LastMovieResultsLayoutUsed = ResultsListBox.LayoutManager.SelectedProfile.ProfilePath;

                    //ResultsListBox.LayoutManager.Dispose();
                    //ResultsListBox.LayoutManager = null;

                    _form.OwnVideoSnapshotsResults.Clear();
                    _form.SnapshotsBox.DataContext = null;
                    FileManager.Configuration.Options.SubtitlesOptions.Language = (_form.LanguagesCombo.SelectedItem as ComboBoxItem).Tag.ToString();
                    _form.OwnBackdrops.Clear();
                    _form.OwnBackdrops = null;

                    _form.MainGenerator.ClearGarbage();
                    _form.ExtraGenerator.ClearGarbage();
                    _form.SpareGenerator.ClearGarbage();
                    _form.MainGenerator = null;
                    _form.ExtraGenerator = null;
                    _form.SpareGenerator = null;

                    _form.Closing -= _form_Closing;
                    _form.Loaded -= _form_Loaded;

                    _form = null;
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.Debug("{0} - {1}",ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            Executor _executor = new Executor(this.CurrentMoviePath);
            try
            {
                try
                {
                    // thumbnail + folder jpg
                    string _thumbPath = Helpers.GetCorrectThumbnailPath(this.CurrentMoviePath, true);
                    UserOptions _Options = FileManager.Configuration.Options;

                    // thumbnail
                    if (FileManager.Configuration.Options.AutogenerateThumbnail)
                    {
                        _executor.CreateThumbnail(m_SelectedCoverPath);
                    }

                    //extra thumbnail
                    if (FileManager.Configuration.Options.AutogenerateFolderJpg)
                    {
                        _executor.CreateExtraThumbnail(m_SelectedCoverPath);
                    }

                    // export cover
                    _executor.ExportCover(m_SelectedCoverPath);

                    MovieInfo _movieInfo = GetSelectedMovieInfo();

                    // moviesheet (if sheets enabled AND (the moviesheet needs to be generated OR we must generate a moviesheet for folder OR we must generate a spare sheet))
                    if (FileManager.EnableMovieSheets)
                    {
                        // update cover
                        if (!File.Exists(MainGenerator.CoverTempPath))
                        {
                            if (!string.IsNullOrEmpty(m_SelectedCoverPath))
                            {
                                MainGenerator.UpdateCover(m_SelectedCoverPath);
                            }
                        }
                        if (!File.Exists(SpareGenerator.CoverTempPath))
                        {
                            if (!string.IsNullOrEmpty(m_SelectedCoverPath))
                            {
                                SpareGenerator.UpdateCover(m_SelectedCoverPath);
                            }
                        }

                        // update movieinfo
                        if (MainGenerator.MovieInfo == null)
                        {
                            MainGenerator.MovieInfo = _movieInfo;
                        }
                        if (SpareGenerator.MovieInfo == null)
                        {
                            SpareGenerator.MovieInfo = _movieInfo;
                        }

                        // export backdrops
                        _executor.ExportBackdrop(MainGenerator.BackdropTempPath, MoviesheetImageType.Background);

                        _executor.ExportBackdrop(MainGenerator.Fanart1TempPath, MoviesheetImageType.Fanart1);

                        _executor.ExportBackdrop(MainGenerator.Fanart2TempPath, MoviesheetImageType.Fanart2);

                        _executor.ExportBackdrop(MainGenerator.Fanart3TempPath, MoviesheetImageType.Fanart3);

                        if (_Options.AutogenerateMovieSheet || _Options.AutogenerateMoviesheetForFolder || _Options.AutogenerateMoviesheetMetadata || _Options.GenerateParentFolderMetadata || _Options.AutogenerateMoviesheetForParentFolder)
                        {
                            // generate and replicate the final moviesheet
                            RenderAndReplicateFinalMoviesheet();
                        }
                    }

                    // movie info
                    if (_Options.AutogenerateMovieInfo)
                    {
                        _executor.CreateMovieInfoFile(MediaInfoControl.MediaData, _movieInfo);
                    }

                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("OKButton", ex);
                }
                // signal that processing is done
                this.Action = ResultsDialogAction.Done;
                Close();
            }
            finally
            {
                _executor = null;
                this.Cursor = Cursors.Arrow;
            }
        }

        private MovieInfo GetSelectedMovieInfo()
        {
            MovieInfo _movieInfo = MovieInfoControl.SelectedMovieInfo();
            FileManager.CurrentCollector.AddCoversAndBackdropsToMovieInfo(_movieInfo, MovieInfoControl.CurrentMoviePath);
            return _movieInfo;
        }

        private string m_SelectedCoverPath;

        private bool SkipSelecting = false;

        private void ResultsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SkipSelecting)
            {
                SkipSelecting = false;
                return;
            }

            Cursor = System.Windows.Input.Cursors.Wait;
            try
            {
                newImage.Source = null;
                m_SelectedCoverPath = null;

                ResultMovieItem _selectedImage = e.NewValue as ResultMovieItem;

                bool _isImageSelected = _selectedImage != null && !string.IsNullOrEmpty(_selectedImage.ImageUrl);
                //OK.IsEnabled = _isImageSelected;
                grpBatchProcessing.IsEnabled = _isImageSelected;
                ChooseSeriesThumb.IsEnabled = _isImageSelected && !string.IsNullOrEmpty(ParentMovieFolderBlock.Text);
                ChooseCurrentFolderThumb.IsEnabled = _isImageSelected && !string.IsNullOrEmpty(CurrentFolderBlock.Text);
                //imdbInfoButton.Visibility = _isImageSelected && _selectedImage.MovieInfo.IMDBID != null ? Visibility.Visible : Visibility.Collapsed;
                //movieInfoButton.Visibility = _isImageSelected && _selectedImage.CollectorMovieUrl != null ? Visibility.Visible : Visibility.Collapsed;
                SaveOriginalImageButton.Visibility = _isImageSelected ? Visibility.Visible : Visibility.Collapsed;
                GenerateThumbButton.IsEnabled = _isImageSelected;

                ResultItemBase _selectedItem = e.NewValue as ResultItemBase;

                BaseCollector _selectedCollector = _selectedItem != null ? (FileManager.CurrentCollector as AllProvidersCollector)[_selectedItem.CollectorName] : null;

                //if (SelectedCollector != _selectedCollector || )
                {
                    SelectedCollector = _selectedCollector;

                    // get backdrops for the movie
                    BackdropsBox.DataContext = GetBackdropsByMovie(_selectedItem, SelectedCollector);
                    // add OwnBackdrops to the list too
                    UpdateBackdropsList();
                }


                if (_selectedItem != null && !(string.IsNullOrEmpty(_selectedItem.MovieInfo.IMDBID)))
                {
                    // if you do not have IMDb id from outside and u got one in the current selection, use it; 
                    // if current selection has no imdbid then DO NOT reset existing one
                    if (!IMDb_FromOutside)
                    {
                        this.IMDbId = _selectedItem.MovieInfo.IMDBID;
                    }
                    this.IMDBData = null;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(QueryIMDbInfo), this);

                }
                else
                {
                    // the newly selected item has no IMDb id
                    if (!IMDb_FromOutside)
                    {
                        this.IMDbId = null;
                    }
                    this.IMDBData = null;
                }


                //if (_selectedItem != null && !(string.IsNullOrEmpty(_selectedItem.IMDBId)))
                //{
                //    _selectedItem.MovieInfo = MovieInfoControl.ApplyIMDbMovieInfoBehaviour(_selectedItem.MovieInfo, this.IMDBData);
                //}

                //tabMovieInfo.Visibility = _selectedItem != null && _selectedItem.MovieInfo != null ? Visibility.Visible : Visibility.Collapsed;


                // hide imagegrid (including watermark)
                NewImageGrid.Visibility = Visibility.Collapsed;

                if (_selectedImage != null)
                {
                    LoadingControl.Visibility = Visibility.Visible;
                    InfoPanel.Visibility = Visibility.Visible;
                    CoverHintBox.Visibility = Visibility.Collapsed;

                    // update image
                    if (!string.IsNullOrEmpty(_selectedImage.ImageUrl))
                    {
                        if (_selectedImage == this.OwnVideoSnapshotMixed && !File.Exists(_selectedImage.ImageUrl))
                        {
                            // no image yet
                            SetImageData(null, null, null);
                        }
                        else
                        {
                            m_SelectedCoverPath = _selectedImage.ImageUrl;
                            // async download the image and when ready, call SetImageData
                            AsyncImageDownloader.GetImageAsync(this, _selectedImage, SetImageData);
                        }
                        if (_selectedImage.ImageSize != null)
                        {
                            newImageSize.Text = string.Format("Original size: {0}", Helpers.GetFormattedImageSize(_selectedImage, _selectedImage.ImageSize.Width, _selectedImage.ImageSize.Height));
                        }
                    }
                    else
                    {
                        SetImageData(null, null, null);
                    }
                }
                else
                {
                    LoadingControl.Visibility = Visibility.Hidden;
                    InfoPanel.Visibility = Visibility.Hidden;
                    CoverHintBox.Visibility = Visibility.Visible;
                }

                if (_selectedItem != null && _selectedItem.MovieInfo != null)
                {
                    FileManager.CurrentCollector.AddCoversAndBackdropsToMovieInfo(_selectedItem.MovieInfo, this.CurrentMoviePath);
                }
            }
            finally
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        public List<BackdropBase> GetBackdropsByMovie(ResultItemBase selectedItem, BaseCollector selectedCollector)
        {
            List<BackdropBase> _result = new List<BackdropBase>();

            if (selectedItem != null /*&& selectedCollector != null*/)
            {
                if (!string.IsNullOrEmpty(selectedItem.MovieInfo.IMDBID))
                {
                    // collect backdrops from all Collectors based on IMDBId
                    foreach (KeyValuePair<string, CollectorNode> _pair in this.CollectorNodes)
                    {
                        if (_pair.Value.Collector != null && _pair.Value.Collector != selectedCollector)
                        {
                            _result.AddRange(_pair.Value.Collector.GetBackdropsByIMDbId(selectedItem.MovieInfo.IMDBID));
                        }
                    }
                }
                // return own backdrops based on movieid 
                if (selectedCollector != null)
                {
                    _result.AddRange(selectedCollector.GetBackdropsByMovieId(selectedItem.MovieId));
                }
            }

            return _result;
        }

        private void SetImageData(BitmapImage bmp, string imageUrl, object userData)
        {
            LoadingControl.Visibility = Visibility.Hidden;

            ResultMovieItem _movieItem = ResultsTree.SelectedItem as ResultMovieItem;

            if (bmp != null && _movieItem != null && string.Compare(imageUrl, _movieItem.ImageUrl, true) == 0)
            {
                try
                {
                    // show image
                    newImage.Source = bmp;
                    // store image data
                    double _width = bmp.PixelWidth;
                    double _height = bmp.PixelHeight;
                    newImageSize.Text = string.Format("Original size: {0}", Helpers.GetFormattedImageSize(bmp, _width, _height));
                    (ResultsTree.SelectedItem as ResultMovieItem).ImageSize = new System.Windows.Size(_width, _height);

                    // there is an image can safely show the image grid 
                    NewImageGrid.Visibility = Visibility.Visible;
                }
                catch { }
            }
            else
            {
                newImageSize.Text = "<no image>";
            }
        }

        private void ChangeQuery_Click(object sender, RoutedEventArgs e)
        {
            this.Action = ResultsDialogAction.ChangeQuery;
            Close();
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to abort all operations?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                this.Action = ResultsDialogAction.Aborted;
                Close();
            }
        }

        private void InfoLink_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GenerateThumbAndRefresh()
        {
            string _thumbPath = Helpers.GetCorrectThumbnailPath(CurrentMoviePath, true);
            // generate thumbnail
            new Executor(CurrentMoviePath).CreateThumbnail((ResultsTree.SelectedItem as ResultMovieItem).ImageUrl);
            // refresh existing image
            RefreshExistingThumbnail(_thumbPath, this);
        }

        private void GenerateThumbButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateThumbAndRefresh();
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                GenerateThumbAndRefresh();
            }
        }

        private void BatchApply_Click(object sender, RoutedEventArgs e)
        {
            ResultMovieItem _item = ResultsTree.SelectedItem as ResultMovieItem;
            if (_item != null)
            {
                bool _res = BatchApplyFolderBox.Show(this, CurrentFolder, _item.ImageUrl);
                if (_res)
                {
                    if ((bool)cbAutoSkipAfterBatch.IsChecked)
                    {
                        // user did NOT press Cancel, so skip further processing for CurrentFolder
                        this.Action = ResultsDialogAction.BatchApply;
                        Close();
                    }
                    else
                    {
                        string _path = CurrentMoviePath;
                        this.Dispatcher.BeginInvoke((Action)delegate
                        {
                            RefreshExistingThumbnail(Helpers.GetCorrectThumbnailPath(_path, false), this);
                        }, DispatcherPriority.Background);
                    }
                }
            }
        }

        private void imdbInfoButton_Click(object sender, RoutedEventArgs e)
        {
            ResultItemBase _movie = (sender as FrameworkElement).DataContext as ResultItemBase;
            if (_movie != null)
            {
                string _imdbId = _movie != null ? _movie.MovieInfo.IMDBID : !string.IsNullOrEmpty(IMDBData.IMDBID) ? IMDBData.IMDBID : null;
                if (_imdbId != null)
                {
                    Helpers.OpenUrlInBrowser(string.Format("http://www.imdb.com/title/{0}/", _imdbId));
                }
            }
        }

        private void movieInfoButton_Click(object sender, RoutedEventArgs e)
        {
            ResultItemBase _movie = (sender as FrameworkElement).DataContext as ResultItemBase;
            if (_movie != null)
            {
                Helpers.OpenUrlInBrowser(_movie.CollectorMovieUrl);

            }
        }

        private void ChooseSeriesThumb_Click(object sender, RoutedEventArgs e)
        {
            ResultMovieItem _movie = (ResultsTree.SelectedItem as ResultMovieItem);
            if (_movie != null)
            {
                string _imageUrl = _movie.ImageUrl;
                // take the parent of the current folder (eg: from c:\movies\PrisonBreak\Season1\ep1.avi take PrisonBreak
                string _folder = Path.GetDirectoryName(CurrentFolder);

                if (!string.IsNullOrEmpty(_folder))
                {
                    string _destPath = FileManager.Configuration.GetFolderjpgPath(Path.Combine(_folder, "dum.avi"), true);
                    ResultsListBox _form = this;
                    this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        Helpers.CreateExtraThumbnailImage(_imageUrl, _destPath);
                        _form.FolderImage.Source = Helpers.LoadImage(_destPath);

                    }, DispatcherPriority.Background);
                }
            }
        }

        private void ChooseCurrentFolderThumb_Click(object sender, RoutedEventArgs e)
        {
            ResultMovieItem _movie = (ResultsTree.SelectedItem as ResultMovieItem);
            if (_movie != null)
            {
                string _imageUrl = _movie.ImageUrl;
                // take the parent of the current folder (eg: from c:\movies\PrisonBreak\Season1\ep1.avi take PrisonBreak
                string _folder = CurrentFolder;

                if (!string.IsNullOrEmpty(_folder))
                {
                    string _destPath = FileManager.Configuration.GetFolderjpgPath(CurrentMoviePath, true); //Path.Combine(_folder, FileManager.Configuration.Options.NamingOptions.FolderjpgName(CurrentMoviePath));
                    ResultsListBox _form = this;
                    this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        Helpers.CreateExtraThumbnailImage(_imageUrl, _destPath);
                        _form.CurrentFolderImage.Source = Helpers.LoadImage(_destPath);

                    }, DispatcherPriority.Background);
                }
            }
        }

        private void SkipFolder_Click(object sender, RoutedEventArgs e)
        {
            this.Action = ResultsDialogAction.SkippedCompleteFolder;
            this.Close();
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            this.Action = ResultsDialogAction.Skip;
            this.Close();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.Action = ResultsDialogAction.Done;
            this.Close();
        }


        #region Image adjustments

        private void comboThumbSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateImageSize(e.AddedItems[0] as ComboBoxItem);
            UpdateStretch(comboImageStretch.SelectedItem as ComboBoxItem);
            UpdateThumbSizeAndDefaults();
        }

        private void UpdateStretch(ComboBoxItem selected)
        {
            if (comboImageStretch.SelectedIndex == 0)
            {
                // Keep Aspect Ratio
                newImage.Stretch = Stretch.Uniform; // keep aspect ratio (Uniform)
            }
            else
            {
                // Ignore Aspect Ratio
                newImage.Stretch = Stretch.Fill;
            }
        }

        private void UpdateImageSize(ComboBoxItem selected)
        {
            if (selected != null && selected.Tag != null)
            {
                newImage.Width = ((System.Windows.Size)selected.Tag).Width;
                newImage.Height = ((System.Windows.Size)selected.Tag).Height;
                CurrentSizeMarker.Width = newImage.Width;
                CurrentSizeMarker.Height = newImage.Height;
            }
        }

        public void UpdateThumbSizeAndDefaults()
        {
            Helpers.ThumbnailSize = new System.Drawing.Size((int)newImage.Width, (int)newImage.Height);

            FileManager.Configuration.Options.KeepAspectRatio = comboImageStretch.SelectedIndex == 0;
        }

        private void UpdateDefaultThumbnailSize()
        {
            if (comboThumbSize.SelectedIndex != -1)
            {
                FileManager.Configuration.Options.ThumbnailSize = (System.Windows.Size)(comboThumbSize.SelectedItem as ComboBoxItem).Tag;
            }
        }

        private void comboImageStretch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStretch(e.AddedItems[0] as ComboBoxItem);
            UpdateImageSize(comboThumbSize.SelectedItem as ComboBoxItem);
            UpdateThumbSizeAndDefaults();

            if (ResultsTree.SelectedItem == this.OwnVideoSnapshotMixed)
            {
                RebuildOwnVideoSnapshot();
                this.OwnVideoSnapshotMixed.IsSelected = false;
                this.OwnVideoSnapshotMixed.IsSelected = true;
            }
        }

        #endregion

        #region Watermark

        private void WatermarkColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Media.Color _color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(((System.Reflection.MemberInfo)(e.AddedItems[0])).Name);
            Watermark.Foreground = new SolidColorBrush(_color);
        }

        private void WatermarkFont_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (System.Windows.Media.FontFamily _ff in Fonts.SystemFontFamilies)
            {
                if (_ff.ToString() == FileManager.Configuration.Options.WatermarkOptions.FontFamily)
                {
                    WatermarkFont.SelectedItem = _ff;
                    break;
                }
            }
        }

        private void cbUseCaption_Click(object sender, RoutedEventArgs e)
        {
            FileManager.Configuration.Options.AddWatermark = (bool)(sender as System.Windows.Controls.CheckBox).IsChecked;
        }
        #endregion

        private void VideoSnapButton_Click(object sender, RoutedEventArgs e)
        {
            //MoviePlayer.Show(this, ResultsListBox.CurrentMovie, OwnVideoSnapshots.Results, 
            //    new Size(newImage.Width, newImage.Height));
            MoviePlayer.Show(this, this.CurrentMoviePath, OwnVideoSnapshotsResults,
                new System.Windows.Size(newImage.Width, newImage.Height));
        }

        private void SaveOriginalImageButton_Click(object sender, RoutedEventArgs e)
        {
            ResultMovieItem _selectedImage = ResultsTree.SelectedItem as ResultMovieItem;
            if (_selectedImage != null)
            {
                Helpers.SaveImageToDisk(this, _selectedImage.ImageUrl);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // snapshot added to collection

        }

        private void GetRandomSnapshotsMix_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            try
            {
                VideoScreenShot.MakeThumbnail(CurrentMoviePath, OwnVideoSnapshotMixed.ImageUrl);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
            this.OwnVideoSnapshotMixed.IsSelected = false;
            this.OwnVideoSnapshotMixed.IsSelected = true;
        }

        private void RebuildOwnVideoSnapshot()
        {
            //bool _doSave = false;

            List<string> _snaps = new List<string>();

            string[] _indexes = SnapshotsIndexes.Text.Trim().Split(',');
            foreach (string _index in _indexes)
            {
                ResultMovieItemSnapshot _snapshotItem = null;
                try
                {
                    if (OwnVideoSnapshotsResults.Count != 0)
                    {
                        _snapshotItem = OwnVideoSnapshotsResults[Int32.Parse(_index)] as ResultMovieItemSnapshot;
                    }
                }
                catch { }
                if (_snapshotItem != null)
                {
                    _snaps.Add(_snapshotItem.ImageUrl);
                }
            }

            VideoScreenShot.GenerateThumbnail(_snaps, OwnVideoSnapshotMixed.ImageUrl);

        }

        private void SnapshotsIndexes_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            RebuildOwnVideoSnapshot();
        }

        private void RefreshOwnSnapshotMix_Click(object sender, RoutedEventArgs e)
        {
            RebuildOwnVideoSnapshot();

            this.OwnVideoSnapshotMixed.IsSelected = false;
            this.OwnVideoSnapshotMixed.IsSelected = true;
        }

        private void tabMovieInfo_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                //dcThumbnail.SetAsActive();
            }
        }


        #region Subtitles

        private delegate subRes AskForSubtitlesHandler(BindingList<subRes> candidates);

        private subRes AskUserToChooseSubtitle(BindingList<subRes> candidates)
        {
            return this.Dispatcher.Invoke((AskForSubtitlesHandler)delegate
            {
                OverlayAdornerHelper.RemoveAllAdorners(this.dcExternalSubs.Content as UIElement);
                return Subtitles.ChooseSubtitles.Show(this, candidates);
            }, DispatcherPriority.Normal, new object[] { candidates }) as subRes;
        }

        private void GetSubs_Click(object sender, RoutedEventArgs e)
        {
            UIElement _adornedElement = dcExternalSubs.Content as UIElement;

            OverlayAdornerHelper _adornerHelper = new OverlayAdornerHelper(_adornedElement, new LoadingScreen("Searching subtitles...", false));
            Helpers.DoEvents();

            try
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(DoSearchAsync), this);
                //SubtitlesManager _SubsManager = new SubtitlesManager();
                //_SubsManager.NeedUserConfirmation = AskUserToChooseSubtitle;
                //if (_SubsManager != null && _SubsManager.Ready)
                //{
                //    if (_SubsManager.GetSubtitle(CurrentMovie, (CDsCombo.SelectedItem as ComboBoxItem).Tag.ToString(),
                //                        (LanguagesCombo.SelectedItem as ComboBoxItem).Tag.ToString(), IMDbId, Keywords))
                //    {
                //        MessageBox.Show("Selected subtitle was downloaded successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
                //        RefreshSubtitles();
                //    }
                //    else
                //    {
                //        MessageBox.Show("There was no subtitle found or selected.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                //    }
                //}
                //else
                //{
                //    MessageBox.Show("Server error: Cannot search for subtitles. Please try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                //}
                //ThreadPool.QueueUserWorkItem(new WaitCallback(DisposeSubsMan), _SubsManager);

            }
            finally
            {
                //OverlayAdornerHelper.RemoveAllAdorners(_adornedElement);
            }
        }

        private static void ShowFinalMessageBox(Window win, string text, string caption, MessageBoxImage image)
        {
            if (win != null)
            {
                win.Dispatcher.BeginInvoke((Action)delegate
                {
                    MessageBox.Show(text, caption, MessageBoxButton.OK, image);
                }, DispatcherPriority.Normal);
            }
        }

        private static void DoSearchAsync(object param)
        {
            ResultsListBox _form = param as ResultsListBox;
            if (_form != null)
            {
                string _CDs = null;
                string _language = null;
                string _currentMoviePath = null;

                _form.Dispatcher.Invoke((Action)delegate
                {
                    _CDs = (_form.CDsCombo.SelectedItem as ComboBoxItem).Tag.ToString();
                    _language = (_form.LanguagesCombo.SelectedItem as ComboBoxItem).Tag.ToString();
                    _currentMoviePath = _form.CurrentMoviePath;
                }, DispatcherPriority.Normal);

                SubtitlesManager _SubsManager = new SubtitlesManager();
                _SubsManager.NeedUserConfirmation = _form.AskUserToChooseSubtitle;
                if (_SubsManager != null && _SubsManager.Ready)
                {
                    if (_SubsManager.GetSubtitle(_currentMoviePath, _CDs, _language, _form.IMDbId, _form.Keywords))
                    {
                        ResultsListBox.ShowFinalMessageBox(_form, "Selected subtitle was downloaded successfully.", "Confirmation", MessageBoxImage.Information);
                        // must update the mediainfo here
                        QueryMediaInfo(_form);
                        _form.RefreshSubtitles();
                    }
                    else
                    {
                        ResultsListBox.ShowFinalMessageBox(_form, "There was no subtitle found or selected.", "Warning", MessageBoxImage.Warning);
                    }
                }
                else
                {
                    ResultsListBox.ShowFinalMessageBox(_form, "Server error: Cannot search for subtitles. Please try again.", "Warning", MessageBoxImage.Warning);
                }
                ThreadPool.QueueUserWorkItem(new WaitCallback(DisposeSubsMan), _SubsManager);
                try
                {
                    _form.Dispatcher.Invoke((Action)delegate
                    {
                        OverlayAdornerHelper.RemoveAllAdorners(_form.dcExternalSubs.Content as UIElement);
                    }, DispatcherPriority.Normal);
                }
                catch { }
            }
        }

        private static void DisposeSubsMan(object param)
        {
            try
            {
                SubtitlesManager _man = param as SubtitlesManager;
                if (_man != null)
                {
                    _man.Dispose();
                }
            }
            catch { }
        }

        private void TestSubsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(CurrentMoviePath);
            }
            catch
            {
                MessageBox.Show("Cannot find associated program.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SubsRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshSubtitles();
        }

        private void RefreshSubtitles()
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                CurrentSubtitles.Clear();
                List<FileInfo> _temp = new FilesCollector().CollectFiles(CurrentFolder, false, SubtitlesManager.SubtitlesSupported).ToList<FileInfo>();
                FileInfoItem _selected = null;
                foreach (FileInfo _info in _temp)
                {
                    FileInfoItem _subItem = new FileInfoItem(_info);
                    CurrentSubtitles.Add(_subItem);
                    if (string.Compare(Path.GetFileNameWithoutExtension(_info.Name), Path.GetFileNameWithoutExtension(this.CurrentMoviePath),
                                       true, CultureInfo.InvariantCulture) == 0)
                    {
                        _selected = _subItem;
                    }
                }
                if (_selected != null)
                {
                    _selected.IsSelected = true;
                    SubsCombo.SelectedItem = _selected;
                }

                ValidateSubtitleElements(_selected);

            }, DispatcherPriority.Background);
        }

        void ValidateSubtitleElements(FileInfoItem selected)
        {
            SubDelete.IsEnabled = selected != null;
            SubRename.IsEnabled = selected != null;
            dcExternalSubs.Foreground = selected == null ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Black;
        }

        private void RefreshLanguage()
        {
            this.Dispatcher.BeginInvoke((Action)delegate
            {
                foreach (ComboBoxItem _item in LanguagesCombo.Items)
                {
                    if (!string.IsNullOrEmpty(FileManager.Configuration.Options.SubtitlesOptions.Language) &&
                        FileManager.Configuration.Options.SubtitlesOptions.Language == _item.Tag.ToString())
                    {
                        _item.IsSelected = true;
                        break;
                    }
                }
            }, DispatcherPriority.Background);
        }

        private void SubRename_Click(object sender, RoutedEventArgs e)
        {
            FileInfoItem _selected = SubsCombo.SelectedItem as FileInfoItem;
            if (_selected != null)
            {
                string _newSubName = Path.ChangeExtension(this.CurrentMoviePath, Path.GetExtension(_selected.FileInfo.Name));
                try
                {
                    if (MessageBox.Show(string.Format("Are you sure that you want to rename\r\n{0}\r\nto\r\n{1}?",
                                    _selected.FileInfo.Name, Path.GetFileName(_newSubName)),
                                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        File.Move(_selected.FileInfo.FullName, Path.Combine(CurrentFolder, _newSubName));
                        RefreshSubtitles();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }

        private void SubDelete_Click(object sender, RoutedEventArgs e)
        {
            FileInfoItem _selected = SubsCombo.SelectedItem as FileInfoItem;
            if (_selected != null &&
                MessageBox.Show(string.Format("Are you sure that you want to delete {0}?", _selected.FileInfo.Name), "Confirmation",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                try
                {
                    string _path = _selected.FileInfo.FullName;
                    SubsCombo.SelectedItem = null;
                    File.Delete(_path);
                    RefreshSubtitles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SubsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FileInfoItem _selected = null;
            if (e.AddedItems != null && e.AddedItems.Count != 0)
            {
                _selected = e.AddedItems[0] as FileInfoItem;
            }

            ValidateSubtitleElements(_selected);
        }

        private void opensubsButton_Click(object sender, RoutedEventArgs e)
        {
            Helpers.OpenUrlInBrowser("http://www.opensubtitles.org");
        }

        #endregion

        private bool IsMainOrExtraSheetSelected()
        {
            return MoviesheetsEditorTabControl.SelectedIndex != 2;
        }

        private void LoadImageFromDiskButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog _ofd = new OpenFileDialog();
            _ofd.Filter = IMAGES_FILTER;
            _ofd.InitialDirectory = FileManager.Configuration.Options.LastCoverSelectedFolder;
            _ofd.CheckFileExists = true;
            _ofd.Multiselect = false;
            _ofd.Title = "Select an image to be used as thumbnail";
            if ((bool)_ofd.ShowDialog(this))
            {
                UpdateMyOwnThumbnailFromDiskImage(_ofd.FileName);
                //remember last selected folder
                FileManager.Configuration.Options.LastCoverSelectedFolder = Path.GetDirectoryName(_ofd.FileName);
            }
        }

        public void UpdateMyOwnThumbnailFromDiskImage(string newImagePath)
        {
            if (!string.IsNullOrEmpty(newImagePath) && File.Exists(newImagePath))
            {
                System.Drawing.Size _tmp = Helpers.GetImageSize(newImagePath);
                this.OwnThumbnailFromDiskItem.ImageSize = new System.Windows.Size(_tmp.Width, _tmp.Height);

                OWN_THUMBNAIL_FROM_DISK = newImagePath;
                this.OwnThumbnailFromDiskItem.ImageUrl = OWN_THUMBNAIL_FROM_DISK;

                this.OwnThumbnailFromDiskItem.IsSelected = false;
                this.OwnThumbnailFromDiskItem.IsSelected = true;
            }
        }

        #region Backdrops

        private void SaveBackdropButton_Click(object sender, RoutedEventArgs e)
        {
            BackdropItem _backdrop = (sender as ButtonBase).Tag as BackdropItem;
            if (_backdrop != null)
            {
                Helpers.SaveImageToDisk(this, _backdrop.OriginalUrl);
            }
        }

        private void PreviewBackdropButton_Click(object sender, RoutedEventArgs e)
        {
            BackdropItem _backdrop = (sender as ButtonBase).Tag as BackdropItem;
            if (_backdrop != null)
            {
                PreviewImage.Show(this, _backdrop.OriginalUrl);
            }
        }

        private void UseBackdropForMovieSheetButton_Click(object sender, RoutedEventArgs e)
        {
            UseBackdropInsideMoviesheet(MoviesheetImageType.Background, (sender as ButtonBase).Tag as BackdropItem, IsMainOrExtraSheetSelected());
        }

        private void UseBackdropForFanart1_Click(object sender, RoutedEventArgs e)
        {
            UseBackdropInsideMoviesheet(MoviesheetImageType.Fanart1, (sender as ButtonBase).Tag as BackdropItem, IsMainOrExtraSheetSelected());
        }

        private void UseBackdropForFanart2_Click(object sender, RoutedEventArgs e)
        {
            UseBackdropInsideMoviesheet(MoviesheetImageType.Fanart2, (sender as ButtonBase).Tag as BackdropItem, IsMainOrExtraSheetSelected());
        }

        private void UseBackdropForFanart3_Click(object sender, RoutedEventArgs e)
        {
            UseBackdropInsideMoviesheet(MoviesheetImageType.Fanart3, (sender as ButtonBase).Tag as BackdropItem, IsMainOrExtraSheetSelected());
        }

        private void UseBackdropInsideMoviesheet(MoviesheetImageType imgtype, string imagePath, bool main)
        {
            if (main)
            {
                MainGenerator.UpdateBackdrop(imgtype, imagePath);
                RefreshMovieSheetSmall();
            }
            else
            {
                SpareGenerator.UpdateBackdrop(imgtype, imagePath);
                RefreshMovieSheetSmallForParent();
            }
        }

        private void UseBackdropInsideMoviesheet(MoviesheetImageType imgtype, BackdropItem item, bool main)
        {
            if (item != null)
            {
                UseBackdropInsideMoviesheet(imgtype, item.OriginalUrl, main);
            }
        }

        private void UpdateBackdropsList()
        {
            List<BackdropBase> _backdrops = BackdropsBox.DataContext as List<BackdropBase>;
            if (_backdrops == null)
            {
                _backdrops = new List<BackdropBase>();
            }
            if (OwnBackdrops != null && OwnBackdrops.Count != 0)
            {
                _backdrops.InsertRange(0, OwnBackdrops);
            }

            BackdropsBox.DataContext = null;
            BackdropsBox.DataContext = _backdrops;
        }

        private void LoadBackdropFromDisk_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog _ofd = new OpenFileDialog();
            _ofd.Filter = IMAGES_FILTER;
            _ofd.InitialDirectory = FileManager.Configuration.Options.LastBackdropSelectedFolder;
            _ofd.CheckFileExists = true;
            _ofd.Multiselect = false;
            _ofd.Title = "Select an image to be used as backdrop/fanart";
            if ((bool)_ofd.ShowDialog(this))
            {
                AddOwnBackdropToTheList(_ofd.FileName);
                //remember last selected folder
                FileManager.Configuration.Options.LastBackdropSelectedFolder = Path.GetDirectoryName(_ofd.FileName);
            }


        }

        private void AddOwnBackdropToTheList(string fileName)
        {
            List<BackdropBase> _backdrops = BackdropsBox.DataContext as List<BackdropBase>;
            if (_backdrops == null)
            {
                _backdrops = new List<BackdropBase>();
            }

            if (File.Exists(fileName))
            {
                // create the item
                BackdropItem _item = new BackdropItem(null, null, string.Empty, fileName, fileName);
                // detect imagesize
                System.Drawing.Size _size = Helpers.GetImageSize(fileName);
                if (_size.Height != 0 && _size.Width != 0)
                {
                    _item.Width = _size.Width.ToString();
                    _item.Height = _size.Height.ToString();
                }
                // add it to OwnBackdrops to cache it
                OwnBackdrops.Insert(0, _item);
                // add it to existing list
                _backdrops.Insert(0, _item);

            }

            BackdropsBox.DataContext = null;
            BackdropsBox.DataContext = _backdrops;
        }

        private void AddOwnBackdropToTheList(BackdropItem backdrop)
        {
            if (backdrop == null)
            {
                return;
            }

            List<BackdropBase> _backdrops = BackdropsBox.DataContext as List<BackdropBase>;
            if (_backdrops == null)
            {
                _backdrops = new List<BackdropBase>();
            }

            OwnBackdrops.Insert(0, backdrop);
            _backdrops.Insert(0, backdrop);

            BackdropsBox.DataContext = null;
            BackdropsBox.DataContext = _backdrops;
        }


        private void GetRandomBackdrop_Click(object sender, RoutedEventArgs e)
        {
            if (!FileManager.Configuration.Options.IsMTNPathSpecified)
            {
                MessageBox.Show("Please select the path to mtn.exe in the Options window in order to make random snapshots.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Cursor = Cursors.Wait;
            try
            {
                ImagesProcessor _imgProcessor = new ImagesProcessor(CurrentMoviePath);
                AddOwnBackdropToTheList(_imgProcessor.GetRandomBackdrop());
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }

        }

        private void GetCustomSnapshotBackdrop_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ResultItemBase> _snaps = new ObservableCollection<ResultItemBase>();
            if (MoviePlayer.Show(this, this.CurrentMoviePath, _snaps, new System.Windows.Size()))
            {
                Cursor = Cursors.Wait;
                try
                {
                    if (_snaps.Count != 0)
                    {
                        foreach (ResultItemBase _item in _snaps)
                        {
                            ResultMovieItemSnapshot _snap = _item as ResultMovieItemSnapshot;
                            if (_snap != null)
                            {
                                AddOwnBackdropToTheList(_snap.ImageUrl);
                                FileManager.AddToGarbageFiles(_snap.ImageUrl);
                            }
                        }
                    }
                }
                finally
                {
                    Cursor = Cursors.Arrow;
                }
            }

        }

        #endregion

        #region Movie Sheets

        private void RefreshMovieSheetSmallForParent()
        {
            RefreshMovieSheetSmall(false, false, true, true);
        }

        private void RefreshMovieSheetSmall(bool forceRefresh)
        {
            RefreshMovieSheetSmall(true, true, false, forceRefresh);
        }

        private void RefreshMovieSheetSmall()
        {
            RefreshMovieSheetSmall(true, true, false, false);
        }

        private void RefreshMovieSheetSmall(bool doMain, bool doExtra, bool doParent, bool forceRefresh)
        {
            if (forceRefresh || FileManager.Configuration.Options.MovieSheetsOptions.AutorefreshPreview)
            {
                try
                {
                    if (this.MovieSheetSmallImage != null)
                    {
                        //ThreadPool.QueueUserWorkItem(new WaitCallback(DoRenderMovieSheet), this);
                        if (FileManager.EnableMovieSheets && RenderMovieSheet(true, doMain, doExtra, doParent))
                        {
                            //MovieSheetSmallImage.Source = null;
                            //MovieSheetSmallImage.Source = Helpers.LoadImage(MainGenerator.MovieSheetTempPath);

                            //ExtraMovieSheetSmallImage.Source = null;
                            //ExtraMovieSheetSmallImage.Source = Helpers.LoadImage(ExtraGenerator.MovieSheetTempPath);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(MainGenerator.LastError))
                            {
                                MessageBox.Show(MainGenerator.LastError, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            if (!string.IsNullOrEmpty(ExtraGenerator.LastError))
                            {
                                MessageBox.Show(ExtraGenerator.LastError, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            if (!string.IsNullOrEmpty(SpareGenerator.LastError))
                            {
                                MessageBox.Show(SpareGenerator.LastError, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                    }
                }
                catch { }
            }

            MovieSheetSmallImage.Source = null;
            MovieSheetSmallImage.Source = Helpers.LoadImage(MainGenerator.MovieSheetTempPath);

            ExtraMovieSheetSmallImage.Source = null;
            ExtraMovieSheetSmallImage.Source = Helpers.LoadImage(ExtraGenerator.MovieSheetTempPath);

            ParentFolderMovieSheetSmallImage.Source = null;
            ParentFolderMovieSheetSmallImage.Source = Helpers.LoadImage(SpareGenerator.MovieSheetTempPath);
        }

        private void RefreshExistingMoviesheetSmall()
        {
            try
            {
                if (FileManager.EnableMovieSheets)
                {
                    ExistingMovieSheetSmallImage.Source = Helpers.LoadImage(FileManager.Configuration.GetMoviesheetPath(CurrentMoviePath, false));
                    ExistingExtraMovieSheetSmallImage.Source = Helpers.LoadImage(FileManager.Configuration.GetMoviesheetForFolderPath(CurrentMoviePath, false));
                    ExistingParentFolderMovieSheetSmallImage.Source = Helpers.LoadImage(FileManager.Configuration.GetMoviesheetForParentFolderPath(CurrentMoviePath, false));
                }
            }
            catch
            {
            }
        }

        private void RefreshMetadataMoviesheetSmall()
        {
            try
            {
                if (FileManager.EnableMovieSheets)
                {
                    MetadataManager = null;
                    MetadataManager = MoviesheetsUpdateManager.CreateManagerForMovie(CurrentMoviePath);
                    MainMetadataControl.MovieSheetSmallImage.Source = Helpers.LoadImage(MetadataManager.GetPreview());

                    ParentFolderMetadataManager = null;
                    ParentFolderMetadataManager = MoviesheetsUpdateManager.CreateManagerForParentFolder(CurrentMoviePath);
                    ParentFolderMetadataControl.MovieSheetSmallImage.Source = Helpers.LoadImage(ParentFolderMetadataManager.GetPreview());
                }
            }
            catch
            {
            }
        }

        private bool RenderAndReplicateFinalParentFolderMoviesheet()
        {
            SpareGenerator.LastError = null;
            SpareGenerator.MediaInfo = this.MediaInfoControl.MediaData;
            return MovieSheetsGenerator.RenderAndReplicateFinalMoviesheet(null, null, SpareGenerator, false);
        }


        private bool RenderAndReplicateFinalMoviesheet()
        {
            MainGenerator.LastError = null;
            ExtraGenerator.LastError = null;
            SpareGenerator.LastError = null;
            MainGenerator.MediaInfo = this.MediaInfoControl.MediaData;
            SpareGenerator.MediaInfo = this.MediaInfoControl.MediaData;
            return MovieSheetsGenerator.RenderAndReplicateFinalMoviesheet(MainGenerator, ExtraGenerator, SpareGenerator, false);
        }

        private bool RenderMovieSheet(bool makeThumbnail, bool doMain, bool doExtra, bool doParentFolder)
        {
            if (doParentFolder)
            {
                SpareGenerator.LastError = null;
                SpareGenerator.MediaInfo = this.MediaInfoControl.MediaData;
                bool _res = SpareGenerator.RenderMoviesheet(makeThumbnail);
                return _res && string.IsNullOrEmpty(SpareGenerator.LastError);
            }
            else
            {
                MainGenerator.LastError = null;
                ExtraGenerator.LastError = null;
                MainGenerator.MediaInfo = this.MediaInfoControl.MediaData;
                bool _res = (doMain && MainGenerator.RenderMoviesheet(makeThumbnail)) || !doMain;
                bool _res1 = false;
                if (string.IsNullOrEmpty(MainGenerator.LastError))
                {
                    _res1 = (doExtra && ExtraGenerator.RenderMoviesheet(makeThumbnail)) || !doExtra;
                }
                return (_res || _res1) && string.IsNullOrEmpty(MainGenerator.LastError) && string.IsNullOrEmpty(ExtraGenerator.LastError);
            }
        }

        private void PreviewMovieSheetButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileManager.EnableMovieSheets)
            {
                if (IsMainOrExtraSheetSelected())
                {
                    bool _main = MoviesheetsEditorTabControl.SelectedIndex == 0;
                    if (RenderMovieSheet(false, _main, !_main, false))
                    {
                        if (_main)
                        {
                            PreviewImage.Show(this, MainGenerator.MovieSheetTempPath);
                        }
                        else
                        {
                            PreviewImage.Show(this, ExtraGenerator.MovieSheetTempPath);
                        }
                    }
                    else
                    {
                        ShowErrors();
                    }
                }
                else
                {
                    if (RenderMovieSheet(false, false, false, true))
                    {
                        PreviewImage.Show(this, SpareGenerator.MovieSheetTempPath);
                    }
                    else
                    {
                        ShowErrors();
                    }
                    RefreshMovieSheetSmallForParent();
                }
            }
        }

        private void ShowErrors()
        {
            if (!string.IsNullOrEmpty(MainGenerator.LastError))
            {
                MessageBox.Show("Error during processing:\r\n" + MainGenerator.LastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (!string.IsNullOrEmpty(ExtraGenerator.LastError))
            {
                MessageBox.Show("Error during processing:\r\n" + ExtraGenerator.LastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (!string.IsNullOrEmpty(SpareGenerator.LastError))
            {
                MessageBox.Show("Error during processing:\r\n" + SpareGenerator.LastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewExistingMovieSheetButton_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentSheetsTabControl.SelectedIndex)
            {
                case 0: // main 
                    PreviewImage.Show(this, FileManager.Configuration.GetMoviesheetPath(CurrentMoviePath, false));
                    break;
                case 1: // extra
                    PreviewImage.Show(this, FileManager.Configuration.GetMoviesheetForFolderPath(CurrentMoviePath, false));
                    break;
                case 2: // parent folder
                    PreviewImage.Show(this, FileManager.Configuration.GetMoviesheetForParentFolderPath(CurrentMoviePath, false));
                    break;
            }
        }

        private void SaveMovieSheetButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileManager.EnableMovieSheets)
            {
                if (IsMainOrExtraSheetSelected())
                {
                    bool _main = MoviesheetsEditorTabControl.SelectedIndex == 0;
                    if (RenderMovieSheet(false, _main, !_main, false))
                    {
                        if (_main)
                        {
                            Helpers.SaveImageToDisk(this, MainGenerator.MovieSheetTempPath);
                        }
                        else
                        {
                            Helpers.SaveImageToDisk(this, ExtraGenerator.MovieSheetTempPath);
                        }
                    }
                }
                else
                {
                    if (RenderMovieSheet(false, false, false, false))
                    {
                        Helpers.SaveImageToDisk(this, SpareGenerator.MovieSheetTempPath);
                    }
                }
            }
        }

        private void ResetMovieSheetButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to discard the current moviesheet data?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                if (IsMainOrExtraSheetSelected())
                {
                    MainGenerator.Init();
                    // recreate the ExtraGenerator
                    ExtraGenerator = MainGenerator.Clone(false, SheetType.Extra);
                    // set the selected template
                    this.TemplatesComboExtra.TemplatesMan.SelectedTemplate = this.TemplatesComboExtra.TemplatesCombobox.SelectedItem as TemplateItem;
                    ExtraGenerator.SelectedTemplate = this.TemplatesComboExtra.TemplatesMan.SelectedTemplate;
                    RefreshMovieSheetSmall();
                }
                else
                {
                    SpareGenerator.Init();
                    SpareGenerator.MovieInfo = new MovieInfo() { Name = Helpers.GetMovieParentFolderName(CurrentMoviePath, "") };

                    RefreshMovieSheetSmallForParent();
                }
            }
        }

        private void ResetSheetItem(MoviesheetImageType imgType, bool isCover)
        {
            string _msg = null;
            if (isCover)
            {
                _msg = "cover";
            }
            else
            {
                switch (imgType)
                {
                    default:
                    case MoviesheetImageType.Background:
                        _msg = "background";
                        break;
                    case MoviesheetImageType.Fanart1:
                        _msg = "fanart 1";
                        break;
                    case MoviesheetImageType.Fanart2:
                        _msg = "fanart 2";
                        break;
                    case MoviesheetImageType.Fanart3:
                        _msg = "fanart 3";
                        break;
                }
            }

            if (MessageBox.Show(string.Format("Are you sure you want to remove the current {0}?", _msg), "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                MovieSheetsGenerator _generator = IsMainOrExtraSheetSelected() ? MainGenerator : SpareGenerator;
                
                Delegate _handler = isCover ? new Action(delegate { _generator.UpdateCover(null); }) : new Action(delegate { _generator.UpdateBackdrop(imgType, null); });
                _handler.DynamicInvoke();

                _handler = IsMainOrExtraSheetSelected() ? new Action(delegate { RefreshMovieSheetSmall(); }) : new Action(delegate { RefreshMovieSheetSmallForParent(); });
                _handler.DynamicInvoke();
            }
        }

        private void ResetImage_Click(object sender, RoutedEventArgs e)
        {
            string _tag = (sender as FrameworkElement).Tag as string;
            bool _isCover = _tag == "Cover";
            MoviesheetImageType _imgType = MoviesheetImageType.Background;
            if (!_isCover)
            {
                _imgType = (MoviesheetImageType)Enum.Parse(typeof(MoviesheetImageType), _tag);
            }

            ResetSheetItem(_imgType, _isCover);
        }

        private void FlipSheetItem(MoviesheetImageType imgType)
        {
            MovieSheetsGenerator _generator = IsMainOrExtraSheetSelected() ? MainGenerator : SpareGenerator;
            string _path = null;
            switch (imgType)
            {
                default:
                case MoviesheetImageType.Background:
                    _path = _generator.BackdropTempPath;
                    break;
                case MoviesheetImageType.Fanart1:
                    _path = _generator.Fanart1TempPath;
                    break;
                case MoviesheetImageType.Fanart2:
                    _path = _generator.Fanart2TempPath;
                    break;
                case MoviesheetImageType.Fanart3:
                    _path = _generator.Fanart3TempPath;
                    break;
            }

            string _dest = Helpers.RotateFlip(_path, RotateFlipType.RotateNoneFlipX);
            if (!string.IsNullOrEmpty(_dest))
            {
                _generator.UpdateBackdrop(imgType, _dest);
                File.Delete(_dest);

                Delegate _handler = IsMainOrExtraSheetSelected() ? new Action(delegate { RefreshMovieSheetSmall(); }) : new Action(delegate { RefreshMovieSheetSmallForParent(); });
                _handler.DynamicInvoke();
            }
        }

        private void FlipImage_Click(object sender, RoutedEventArgs e)
        {
            string _tag = (sender as FrameworkElement).Tag as string;
            MoviesheetImageType _imgType = (MoviesheetImageType)Enum.Parse(typeof(MoviesheetImageType), _tag);

            FlipSheetItem(_imgType);

            //if (IsMainOrExtraSheetSelected())
            //{
            //    string _dest = Helpers.RotateFlip(this.MainGenerator.BackdropTempPath, RotateFlipType.RotateNoneFlipX);
            //    if (!string.IsNullOrEmpty(_dest))
            //    {
            //        this.MainGenerator.UpdateBackdrop(MoviesheetImageType.Background, _dest);
            //        File.Delete(_dest);
            //        RefreshMovieSheetSmall();
            //    }
            //}
            //else
            //{
            //    string _dest = Helpers.RotateFlip(this.ParentFolderGenerator.BackdropTempPath, RotateFlipType.RotateNoneFlipX);
            //    if (!string.IsNullOrEmpty(_dest))
            //    {
            //        this.ParentFolderGenerator.UpdateBackdrop(MoviesheetImageType.Background, _dest);
            //        File.Delete(_dest);
            //        RefreshMovieSheetSmallForParent();
            //    }
            //}

        }


        public void TemplateSelectorControl_TemplatesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoading)
            {
                if (e.AddedItems.Count != 0 && e.AddedItems[0] != null && MainGenerator.SelectedTemplate != e.AddedItems[0])
                {
                    try
                    {
                        MainGenerator.SelectedTemplate = e.AddedItems[0] as TemplateItem;
                        RefreshMovieSheetSmall(true, false, false, true);
                    }
                    catch { }
                }
            }
        }

        public void TemplateSelectorControl_ExtraTemplatesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoading)
            {
                if (e.AddedItems.Count != 0 && e.AddedItems[0] != null && ExtraGenerator.SelectedTemplate != e.AddedItems[0])
                {
                    try
                    {
                        ExtraGenerator.SelectedTemplate = e.AddedItems[0] as TemplateItem;
                        RefreshMovieSheetSmall(false, true, false, true);
                    }
                    catch { }
                }
            }
        }

        public void TemplatesComboParentFolder_TemplatesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoading)
            {
                if (e.AddedItems.Count != 0 && e.AddedItems[0] != null && SpareGenerator.SelectedTemplate != e.AddedItems[0])
                {
                    try
                    {
                        SpareGenerator.SelectedTemplate = e.AddedItems[0] as TemplateItem;
                        RefreshMovieSheetSmallForParent();
                    }
                    catch { }
                }
            }
        }

        private void UseCoverForMovieSheetButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsMainOrExtraSheetSelected())
            {
                MainGenerator.UpdateCover(m_SelectedCoverPath);
                RefreshMovieSheetSmall();
            }
            else
            {
                SpareGenerator.UpdateCover(m_SelectedCoverPath);
                RefreshMovieSheetSmallForParent();
            }
        }

        private void UseExistingImageAsCoverForMovieSheet_Click(object sender, RoutedEventArgs e)
        {
            string _existingThumbnail = Helpers.GetCorrectThumbnailPath(CurrentMoviePath, false);
            if (File.Exists(_existingThumbnail))
            {
                MainGenerator.UpdateCover(_existingThumbnail);
                RefreshMovieSheetSmall();
            }
        }

        private void GenerateMovieSheetButton_Click(object sender, RoutedEventArgs e)
        {
            ExistingMovieSheetSmallImage.Source = null;
            ExistingExtraMovieSheetSmallImage.Source = null;

            if (!RenderAndReplicateFinalMoviesheet())
            {
                ShowErrors();
            }
            else
            {
                MessageBox.Show("Moviesheet(s) and/or metadata created successfully!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshExistingMoviesheetSmall();
                RefreshMetadataMoviesheetSmall();
            }
        }

        private void ParentFolderSheetGenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            ExistingParentFolderMovieSheetSmallImage.Source = null;
            if (!RenderAndReplicateFinalParentFolderMoviesheet())
            {
                ShowErrors();
            }
            else
            {
                MessageBox.Show("Parent folder's moviesheet was created successfully!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshExistingMoviesheetSmall();
                RefreshMetadataMoviesheetSmall();
            }
        }


        #endregion

        private System.Windows.Point m_StartPoint;

        public void Source_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_StartPoint = e.GetPosition(null);
        }

        private bool VerifyDrag(MouseEventArgs e)
        {
            System.Windows.Point mousePos = e.GetPosition(null);
            Vector diff = m_StartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                Math.Abs(diff.X) > /*SystemParameters.MinimumHorizontalDragDistance*/ 1 &&
                Math.Abs(diff.Y) > /*SystemParameters.MinimumVerticalDragDistance)*/ 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void newImage_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (VerifyDrag(e))
            {
                // Initialize the drag & drop operation
                DataObject dragData = new DataObject(Helpers.DRAGDROP_COVER_FORMAT, ResultsTree.SelectedItem);
                DragDrop.DoDragDrop(newImage, dragData, DragDropEffects.Copy);
            }
        }

        private void Image_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (VerifyDrag(e))
            {
                // Initialize the drag & drop operation
                DataObject dragData = new DataObject(Helpers.DRAGDROP_BACKDROP_FORMAT,
                                        (sender as System.Windows.Controls.Image).DataContext as BackdropItem);
                DragDrop.DoDragDrop(sender as DependencyObject, dragData, DragDropEffects.Copy);
            }
        }

        public void MovieInfoControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            return;
            //if (VerifyDrag(e))
            //{
            //    // Initialize the drag & drop operation
            //    DataObject dragData = new DataObject(Helpers.DRAGDROP_MOVIEINFO_FORMAT,
            //                            (sender as MovieInfoControl).SelectedMovieInfo());
            //    DragDrop.DoDragDrop(sender as DependencyObject, dragData, DragDropEffects.Copy);
            //}
        }

        private void Border_DragEnter(object sender, DragEventArgs e)
        {
            string _tag = (sender as FrameworkElement).Tag.ToString();
            if (!e.Data.GetDataPresent(Helpers.DRAGDROP_COVER_FORMAT) &&
                !e.Data.GetDataPresent(Helpers.DRAGDROP_BACKDROP_FORMAT) &&
                !e.Data.GetDataPresent(Helpers.DRAGDROP_MOVIEINFO_FORMAT) &&
                !e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            string _tag = (sender as FrameworkElement).Tag.ToString();

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Array a = (Array)e.Data.GetData(DataFormats.FileDrop);
                if (a != null)
                {
                    // Extract string from first array element
                    // (ignore all files except first if number of files are dropped).
                    string s = a.GetValue(0).ToString();
                    // Call rendering asynchronously
                    // Explorer instance from which file is dropped is not responding
                    // all the time when DragDrop handler is active, so we need to return
                    // immidiately (especially if OpenFile shows MessageBox).
                    this.Dispatcher.BeginInvoke((Action<string>)delegate
                    {
                        UseBackdropInsideMoviesheet(MoviesheetImageType.Background, s, _tag == "Main");
                    }, DispatcherPriority.Background, new Object[] { s });
                    this.Activate();        // in the case Explorer overlaps this form

                }
                return;
            }
            if (e.Data.GetDataPresent(Helpers.DRAGDROP_COVER_FORMAT))
            {
                if (_tag == "Main")
                {
                    MainGenerator.UpdateCover(m_SelectedCoverPath);
                    RefreshMovieSheetSmall();
                }
                else
                {
                    SpareGenerator.UpdateCover(m_SelectedCoverPath);
                    RefreshMovieSheetSmallForParent();
                }
                return;
            }
            if (e.Data.GetDataPresent(Helpers.DRAGDROP_BACKDROP_FORMAT))
            {
                BackdropItem _backdrop = e.Data.GetData(Helpers.DRAGDROP_BACKDROP_FORMAT) as BackdropItem;
                if (_backdrop != null)
                {
                    UseBackdropInsideMoviesheet(MoviesheetImageType.Background, _backdrop, _tag == "Main");
                }
                return;
            }
            if (e.Data.GetDataPresent(Helpers.DRAGDROP_MOVIEINFO_FORMAT) && _tag == "Main")
            {
                MovieInfo _info = e.Data.GetData(Helpers.DRAGDROP_MOVIEINFO_FORMAT) as MovieInfo;
                MainGenerator.MovieInfo = _info;
                RefreshMovieSheetSmall();
                return;
            }

        }

        private void RefreshMovieSheetButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsMainOrExtraSheetSelected())
            {
                RefreshMovieSheetSmall(true);
            }
            else
            {
                RefreshMovieSheetSmallForParent();
            }
        }

        private void RefreshExistingMovieSheetButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshExistingMoviesheetSmall();
        }

        private void UseMovieInfo(MoviesheetsUpdateManager manager, MovieSheetsGenerator generator)
        {
            generator.MovieInfo = manager.GetMovieInfo();
            if (generator.MovieInfo != null)
            {
                try
                {
                    this.MediaInfoControl.MediaData = generator.MovieInfo.MediaInfo;
                }
                catch { }
            }
        }

        private void ExecutedUseFromMetadataCommand(object sender, ExecutedRoutedEventArgs e)
        {
            string _param = e.Parameter as string;
            MetadataControl _metaControl = e.Source as MetadataControl;
            if (_metaControl != null && !string.IsNullOrEmpty(_param))
            {
                MoviesheetsUpdateManager _manager = _metaControl.IsMain ? MetadataManager : ParentFolderMetadataManager;
                MovieSheetsGenerator _generator = IsMainOrExtraSheetSelected() ? MainGenerator : SpareGenerator;
                switch (_param)
                {
                    case "ALL":
                        _manager.GetImage(MoviesheetsUpdateManager.COVER_STREAM_NAME, _generator.CoverTempPath);
                        _manager.GetImage(MoviesheetsUpdateManager.BACKGROUND_STREAM_NAME, _generator.BackdropTempPath);
                        _manager.GetImage(MoviesheetsUpdateManager.FANART1_STREAM_NAME, _generator.Fanart1TempPath);
                        _manager.GetImage(MoviesheetsUpdateManager.FANART2_STREAM_NAME, _generator.Fanart2TempPath);
                        _manager.GetImage(MoviesheetsUpdateManager.FANART3_STREAM_NAME, _generator.Fanart3TempPath);
                        UseMovieInfo(_manager, _generator);
                        break;
                    case "COVER":
                        _manager.GetImage(MoviesheetsUpdateManager.COVER_STREAM_NAME, _generator.CoverTempPath);
                        break;
                    case "BACKGROUND":
                        _manager.GetImage(MoviesheetsUpdateManager.BACKGROUND_STREAM_NAME, _generator.BackdropTempPath);
                        break;
                    case "F1":
                        _manager.GetImage(MoviesheetsUpdateManager.FANART1_STREAM_NAME, _generator.Fanart1TempPath);
                        break;
                    case "F2":
                        _manager.GetImage(MoviesheetsUpdateManager.FANART2_STREAM_NAME, _generator.Fanart2TempPath);
                        break;
                    case "F3":
                        _manager.GetImage(MoviesheetsUpdateManager.FANART3_STREAM_NAME, _generator.Fanart3TempPath);
                        break;
                    case "NFO":
                        UseMovieInfo(_manager, _generator);
                        break;
                }
                if (IsMainOrExtraSheetSelected())
                {
                    RefreshMovieSheetSmall(true);
                }
                else
                {
                    RefreshMovieSheetSmallForParent();
                }
            }
        }

        private void CanExecuteUseFromMetadataCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            bool _result = false;

            MetadataControl _metaControl = e.Source as MetadataControl;
            if (_metaControl != null)
            {
                MoviesheetsUpdateManager _manager = _metaControl.IsMain ? MetadataManager : ParentFolderMetadataManager;
                if (_manager != null)
                {
                    string _param = e.Parameter as string;
                    if (!string.IsNullOrEmpty(_param))
                    {
                        switch (_param)
                        {
                            case "ALL":
                                _result = _manager.HasBackground || _manager.HasCover ||
                                          _manager.HasFanart1 || _manager.HasFanart2 ||
                                          _manager.HasFanart3 || _manager.HasNfo;
                                break;
                            case "COVER":
                                _result = _manager.HasCover;
                                break;
                            case "BACKGROUND":
                                _result = _manager.HasBackground;
                                break;
                            case "F1":
                                _result = _manager.HasFanart1;
                                break;
                            case "F2":
                                _result = _manager.HasFanart2;
                                break;
                            case "F3":
                                _result = _manager.HasFanart3;
                                break;
                            case "NFO":
                                _result = _manager.HasNfo;
                                break;
                            default:
                                _result = false;
                                break;
                        }
                    }
                }
            }
            e.CanExecute = _result;
        }

        private void UseMovieInfoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MovieInfo _mi = GetSelectedMovieInfo();
            if (IsMainOrExtraSheetSelected())
            {
                MainGenerator.MovieInfo = _mi;
                ExtraGenerator.MovieInfo = _mi;
            }
            else
            {
                SpareGenerator.MovieInfo = _mi;
            }
            RefreshMovieSheetSmall();
        }

        private void UseMovieInfoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 1680x1050
            this.Width = 1675;
            this.Height = 1020;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // 1280 x 1024
            this.Width = 1270;
            this.Height = 990;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // 1024 x 768
            this.Width = 1000;
            this.Height = 730;
        }

        private void MoviesheetsEditorTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TheMainSheetGenerateBtn.Visibility = IsMainOrExtraSheetSelected() ? Visibility.Visible : Visibility.Collapsed;
            TheParentFolderSheetGenerateBtn.Visibility = IsMainOrExtraSheetSelected() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            Options.Show(this, FileManager.Configuration.Options);
        }

        private void DockManager_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void btnTools_Click(object sender, RoutedEventArgs e)
        {
            btnTools.ContextMenu.IsOpen = true;
        }

        private void PreviewCoverButton_Click(object sender, RoutedEventArgs e)
        {
            ResultMovieItem _selectedImage = ResultsTree.SelectedItem as ResultMovieItem;
            if (_selectedImage != null)
            {
                PreviewImage.Show(this, _selectedImage.ImageUrl);
            }
        }


    }

    public enum ResultsDialogAction
    {
        Done,
        ChangeQuery,
        Aborted,
        BatchApply,
        Skip,
        SkippedCompleteFolder
    }

    public class DialogResult
    {
        public ResultMovieItem Item { get; private set; }
        public ResultsDialogAction Action { get; private set; }
        public MovieInfo SelectedMovieInfo { get; set; }

        public DialogResult(ResultMovieItem item, ResultsDialogAction action)
        {
            Item = item;
            Action = action;
        }
    }

}
