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
using System.Windows.Shapes;
using ThumbGen.MovieSheets;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using System.Web;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for SelectTemplateBox.xaml
    /// </summary>
    public partial class DebugTemplateBox : Window
    {
        private MovieInfo m_MovieInfo = new MovieInfo();

        public DebugTemplateBox()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += delegate { DragMove(); };
            this.Loaded += new RoutedEventHandler(SelectTemplateBox_Loaded);
            this.Closing += new System.ComponentModel.CancelEventHandler(DebugTemplateBox_Closing);
        }

        public override void EndInit()
        {
            base.EndInit();
            this.fsMetadata.SelectedFileChanged += fsMetadata_SelectedFileChanged;
            TheMovieInfoControl.MyDataInfo = m_MovieInfo;
            TheMovieInfoControl.SelectItemBySourceType(MovieInfoProviderItemType.MyOwn);
        }

        void DebugTemplateBox_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FileManager.Configuration.Options.TestNfoFile = HttpUtility.HtmlEncode(this.fsNfo.Filepath);
            FileManager.Configuration.Options.TestMovieFile = HttpUtility.HtmlEncode(this.fsMovieFile.Filepath);
            FileManager.Configuration.Options.TestBackground = HttpUtility.HtmlEncode(this.fsBackdrop.Filepath);
            FileManager.Configuration.Options.TestCover = HttpUtility.HtmlEncode(this.fsCover.Filepath);
            FileManager.Configuration.Options.TestFanart1 = HttpUtility.HtmlEncode(this.fsFanart1.Filepath);
            FileManager.Configuration.Options.TestFanart2 = HttpUtility.HtmlEncode(this.fsFanart2.Filepath);
            FileManager.Configuration.Options.TestFanart3 = HttpUtility.HtmlEncode(this.fsFanart3.Filepath);
            FileManager.Configuration.Options.TestMetadata = HttpUtility.HtmlEncode(this.fsMetadata.Filepath);
        }

        void SelectTemplateBox_Loaded(object sender, RoutedEventArgs e)
        {
            this.TemplateSelector.TemplatesMan.RefreshTemplates();
            this.TemplateSelector.TemplatesCombobox.SelectedValue = this.TemplateSelector.TemplatesMan.SelectedTemplate;

            this.fsNfo.Filepath = HttpUtility.HtmlDecode(FileManager.Configuration.Options.TestNfoFile);
            this.fsMovieFile.Filepath = HttpUtility.HtmlDecode(FileManager.Configuration.Options.TestMovieFile);
            this.fsBackdrop.Filepath = HttpUtility.HtmlDecode(FileManager.Configuration.Options.TestBackground);
            this.fsCover.Filepath = HttpUtility.HtmlDecode(FileManager.Configuration.Options.TestCover);
            this.fsFanart1.Filepath = HttpUtility.HtmlDecode(FileManager.Configuration.Options.TestFanart1);
            this.fsFanart2.Filepath = HttpUtility.HtmlDecode(FileManager.Configuration.Options.TestFanart2);
            this.fsFanart3.Filepath = HttpUtility.HtmlDecode(FileManager.Configuration.Options.TestFanart3);
            this.fsMetadata.Filepath = HttpUtility.HtmlDecode(FileManager.Configuration.Options.TestMetadata);

            if (!string.IsNullOrEmpty(this.fsMetadata.Filepath))
            {
                RefreshMetadataPreview(this.fsMetadata.Filepath);
            }
        }


        public static bool Show(Window owner)
        {
            bool _result = false;

            DebugTemplateBox _box = new DebugTemplateBox();
            _box.Owner = owner;
            _box.WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
            var res = _box.ShowDialog();
            if (res.HasValue && res.Value)
            {
                _result = true;
            }

            return _result;
        }

        private void RefreshMetadataPreview(string filePath)
        {
            MetadataControl.MovieSheetSmallImage.Source = null;
            MoviesheetsUpdateManager _man = MoviesheetsUpdateManager.CreateManagerFromMetadata(filePath, fsMovieFile.Filepath);
            MetadataControl.MovieSheetSmallImage.Source = Helpers.LoadImage(_man.GetPreview());
        }

        public void fsMetadata_SelectedFileChanged(object sender, FileChangedEventArgs args)
        {
            RefreshMetadataPreview(args.NewFilePath);
        }

        private string Render(bool showPreview)
        {
            string _tmpPath = Helpers.GetUniqueFilename(".jpg");

            string _moviePath = Helpers.IsDirectory(fsMovieFile.Filepath) ? System.IO.Path.Combine(fsMovieFile.Filepath, "dummy.mkv") : fsMovieFile.Filepath;

            using (MovieSheetsGenerator _gen = new MovieSheetsGenerator(SheetType.Main, _moviePath))
            {
                _gen.SelectedTemplate = this.TemplateSelector.TemplatesCombobox.SelectedItem as TemplateItem;

                if (tabMetadata.IsSelected)
                {
                    if (!_gen.CreateMoviesheetFromMetadata(this.fsMetadata.Filepath, _moviePath, _tmpPath, false))
                    {
                        if (!string.IsNullOrEmpty(_gen.LastError))
                        {
                            MessageBox.Show(_gen.LastError);
                        }
                    }
                }
                else if (tabCustom.IsSelected)
                {
                    if (!_gen.CreateMoviesheetFromCustomData(this.fsBackdrop.Filepath, this.fsCover.Filepath,
                                                        this.fsFanart1.Filepath, this.fsFanart2.Filepath, this.fsFanart3.Filepath,
                                                        this.fsNfo.Filepath, this.fsMovieFile.Filepath, _tmpPath, false))
                    {
                        if (!string.IsNullOrEmpty(_gen.LastError))
                        {
                            MessageBox.Show(_gen.LastError);
                        }
                    }

                }

                try
                {
                    this.Debug.Text = IndentXMLString(_gen.RenderedXML);
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("Indenting:", ex);
                }

                if (File.Exists(_tmpPath) && showPreview)
                {
                    PreviewImage.Show(this, _tmpPath);
                }
                _gen.ClearGarbage();
            }

            return _tmpPath;
        }

        private static string IndentXMLString(string xml)
        {
            string outXml = string.Empty;
            MemoryStream ms = new MemoryStream();
            // Create a XMLTextWriter that will send its output to a memory stream (file)
            XmlTextWriter xtw = new XmlTextWriter(ms, Encoding.Unicode);
            XmlDocument doc = new XmlDocument();

            try
            {
                // Load the unformatted XML text string into an instance 
                // of the XML Document Object Model (DOM)
                doc.LoadXml(xml);

                // Set the formatting property of the XML Text Writer to indented
                // the text writer is where the indenting will be performed
                xtw.Formatting = Formatting.Indented;

                // write dom xml to the xmltextwriter
                doc.WriteContentTo(xtw);
                // Flush the contents of the text writer
                // to the memory stream, which is simply a memory file
                xtw.Flush();

                // set to start of the memory stream (file)
                ms.Seek(0, SeekOrigin.Begin);
                // create a reader to read the contents of 
                // the memory stream (file)
                StreamReader sr = new StreamReader(ms);
                // return the formatted string to caller
                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("indenting2", ex);
                return string.Empty;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string _tmpPath = Render(true);

            if (File.Exists(_tmpPath))
            {
                File.Delete(_tmpPath);
            }
        }

        private void SaveOriginalImageButton_Click(object sender, RoutedEventArgs e)
        {
            string _tmpPath = Render(false);
            Helpers.SaveImageToDisk(this, _tmpPath);

        }

        private void Missing_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool _allowSave = tabControl.SelectedIndex == 0 || tabControl.SelectedIndex == 1;
            OkButton.IsEnabled = _allowSave;
            SaveOriginalImageButton.IsEnabled = _allowSave;
            GenerateMetadataButton.IsEnabled = tabControl.SelectedIndex == 1 && !string.IsNullOrEmpty(fsMovieFile.Filepath); // only for custom data and if movie file is set
            GenerateNfoFile.IsEnabled = tabControl.SelectedIndex == 3;
        }

        private void GenerateMetadataButton_Click(object sender, RoutedEventArgs e)
        {
            nfoFileType nfotype = nfoFileType.Unknown;

            string _tmpPath = Helpers.GetUniqueFilename(".jpg");
            try
            {

                MovieSheetsGenerator _gen = new MovieSheetsGenerator(SheetType.Main, fsMovieFile.Filepath);
                _gen.SelectedTemplate = this.TemplateSelector.TemplatesCombobox.SelectedItem as TemplateItem;

                if (_gen.CreateMoviesheetFromCustomData(this.fsBackdrop.Filepath, this.fsCover.Filepath,
                                                        this.fsFanart1.Filepath, this.fsFanart2.Filepath, this.fsFanart3.Filepath,
                                                        this.fsNfo.Filepath, this.fsMovieFile.Filepath, _tmpPath, false))
                {
                    SaveFileDialog _sfd = new SaveFileDialog();
                    _sfd.Title = "Select target metadata file name";
                    _sfd.Filter = this.FindResource("metadataFilter") as string;
                    _sfd.DefaultExt = ".tgmd";

                    if ((bool)_sfd.ShowDialog())
                    {

                        MoviesheetsUpdateManager _man = MoviesheetsUpdateManager.CreateManagerFromMetadata(_sfd.FileName, fsMovieFile.Filepath);
                        MoviesheetsUpdateManagerParams _params = new MoviesheetsUpdateManagerParams(fsBackdrop.Filepath,
                                                                                                    fsFanart1.Filepath,
                                                                                                    fsFanart2.Filepath,
                                                                                                    fsFanart3.Filepath,
                                                                                                    nfoHelper.LoadNfoFile(fsMovieFile.Filepath, fsNfo.Filepath, out nfotype),
                                                                                                    fsCover.Filepath,
                                                                                                    _gen.MovieSheetPreviewTempPath);
                        _man.GenerateUpdateFile(_params, _gen.SelectedTemplate.TemplateName);
                        _man = null;
                    }
                }
                else
                {
                    MessageBox.Show(_gen.LastError);
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

        private void GenerateNfoFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog _sfd = new SaveFileDialog();
            _sfd.Title = "Select target .nfo file name";
            _sfd.Filter = "All files (*.*)|*.*";
            _sfd.DefaultExt = ".nfo";

            if ((bool)_sfd.ShowDialog())
            {

                MediaInfoData _mi = string.IsNullOrEmpty(fsMovieFile.Filepath) ? null : MediaInfoManager.GetMediaInfoData(fsMovieFile.Filepath);
                nfoHelper.GenerateNfoFile(fsMovieFile.Filepath, m_MovieInfo, _mi, _sfd.FileName);
            }
        }
    }
}
