using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using System.Windows.Input;
using System.Collections.Generic;
using ThumbGen.MovieSheets;
using System.Globalization;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        public Options()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += delegate { DragMove(); };
        }

        public override void EndInit()
        {
            base.EndInit();
            this.Loaded += new RoutedEventHandler(Options_Loaded);
            this.Closing += new System.ComponentModel.CancelEventHandler(Options_Closing);
            this.fsMovieFile.SelectedFileChanged += this.fsMovieFile_SelectedFileChanged;
        }

        void Options_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FileManager.Configuration.Options.MovieSheetsOptions.TemplateName = (this.TemplateSelector.TemplatesCombobox.SelectedItem as TemplateItem).TemplateName;
            FileManager.Configuration.Options.MovieSheetsOptions.ExtraTemplateName = (this.TemplateSelectorExtra.TemplatesCombobox.SelectedItem as TemplateItem).TemplateName;
            FileManager.Configuration.Options.MovieSheetsOptions.ParentFolderTemplateName = (this.TemplateSelectorParentFolder.TemplatesCombobox.SelectedItem as TemplateItem).TemplateName;
        }

        void Options_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public UserOptions.Connection ConnectionOptions
        {
            get { return (UserOptions.Connection)GetValue(ConnectionOptionsProperty); }
            set { SetValue(ConnectionOptionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConnectionOptions.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConnectionOptionsProperty =
            DependencyProperty.Register("ConnectionOptions", typeof(UserOptions.Connection), typeof(Options), new UIPropertyMetadata(null));


        public static void Show(Window owner, UserOptions options)
        {
            Options _box = new Options();
            _box.Owner = owner;
            _box.DataContext = options;
            _box.cmbLanguages.DataContext = FileManager.Configuration.GetLanguages();
            _box.cmbTVShowsLanguages.DataContext = FileManager.Configuration.GetLanguages();
            _box.cmbDefaultAudioLanguage.DataContext = FileManager.Configuration.GetLanguages();
            _box.cbMulticoreCPU.Visibility = Environment.ProcessorCount > 1 ? Visibility.Visible : Visibility.Collapsed;
            _box.comboExportBackdropsType.ItemsSource = Enum.GetValues(typeof(ExportBackdropTypes));
            _box.TemplateSelector.TemplatesMan.RefreshTemplates(FileManager.Configuration.Options.MovieSheetsOptions.TemplateName);
            _box.TemplateSelector.TemplatesCombobox.SelectedValue = _box.TemplateSelector.TemplatesMan.SelectedTemplate;

            _box.TemplateSelectorExtra.TemplatesMan.RefreshTemplates(FileManager.Configuration.Options.MovieSheetsOptions.ExtraTemplateName);
            _box.TemplateSelectorExtra.TemplatesCombobox.SelectedValue = _box.TemplateSelectorExtra.TemplatesMan.SelectedTemplate;

            _box.TemplateSelectorParentFolder.TemplatesMan.RefreshTemplates(FileManager.Configuration.Options.MovieSheetsOptions.ParentFolderTemplateName);
            _box.TemplateSelectorParentFolder.TemplatesCombobox.SelectedValue = _box.TemplateSelectorParentFolder.TemplatesMan.SelectedTemplate;

            _box.WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
            _box.ShowDialog();
        }

        private string GetToken(string token, string file)
        {
            return string.Format("{0} = {1}", token, string.IsNullOrEmpty(file) ? "" : ConfigHelpers.GetTokenValue(token, file));
        }

        public void fsMovieFile_SelectedFileChanged(object sender, FileChangedEventArgs args)
        {
            tokenE.Text = GetToken("$E", args.NewFilePath);
            tokenF.Text = GetToken("$F", args.NewFilePath);
            tokenM.Text = GetToken("$M", args.NewFilePath);
            tokenN.Text = GetToken("$N", args.NewFilePath);
            tokenP.Text = GetToken("$P", args.NewFilePath);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.Configuration.RefreshProxy();
            DialogResult = true;
        }

        private void Default_Click(object sender, RoutedEventArgs e)
        {
            Button _b = sender as Button;
            switch ((string)_b.Tag)
            {
                case "1":
                    //thumbnail
                    FileManager.Configuration.Options.NamingOptions.ThumbnailExtension = NamingConventions.DEFAULT_THUMBNAIL_EXTENSION;
                    FileManager.Configuration.Options.NamingOptions.ThumbnailMask = NamingConventions.DEFAULT_THUMBNAIL_MASK;
                    break;
                case "2":
                    // folderjpg
                    FileManager.Configuration.Options.NamingOptions.FolderjpgExtension = NamingConventions.DEFAULT_FOLDERJPG_EXTENSION;
                    FileManager.Configuration.Options.NamingOptions.FolderjpgMask = NamingConventions.DEFAULT_FOLDERJPG_MASK;
                    break;
                case "3":
                    // movieinfo
                    FileManager.Configuration.Options.NamingOptions.MovieinfoExtension = NamingConventions.DEFAULT_MOVIEINFO_EXTENSION;
                    FileManager.Configuration.Options.NamingOptions.MovieinfoMask = NamingConventions.DEFAULT_MOVIEINFO_MASK;
                    break;
                case "33":
                    // movieinfo export
                    FileManager.Configuration.Options.NamingOptions.MovieinfoExportExtension = NamingConventions.DEFAULT_MOVIEINFO_EXTENSION;
                    FileManager.Configuration.Options.NamingOptions.MovieinfoExportMask = NamingConventions.DEFAULT_MOVIEINFO_MASK;
                    cmbNfoExportType.SelectedIndex = 0;
                    break;
                case "4":
                    //moviesheets
                    FileManager.Configuration.Options.NamingOptions.MoviesheetExtension = NamingConventions.DEFAULT_MOVIESHEET_EXTENSION;
                    FileManager.Configuration.Options.NamingOptions.MoviesheetMask = NamingConventions.DEFAULT_MOVIESHEET_MASK;
                    break;
                case "44":
                    //moviesheets
                    FileManager.Configuration.Options.NamingOptions.MoviesheetExtension = NamingConventions.DEFAULT_MOVIESHEET_EXTENSION;
                    FileManager.Configuration.Options.NamingOptions.MoviesheetMask = NamingConventions.DEFAULT_MSHEET_MOVIESHEET_MASK;
                    break;
                case "55":
                    //moviesheets for folder
                    FileManager.Configuration.Options.NamingOptions.MoviesheetForFolderExtension = NamingConventions.DEFAULT_MOVIESHEET_EXTENSION;
                    FileManager.Configuration.Options.NamingOptions.MoviesheetForFolderMask = NamingConventions.DEFAULT_MSHEET_MOVIESHEET_FOR_FOLDER_MASK;
                    break;
                case "6":
                    //moviesheet metadata
                    FileManager.Configuration.Options.NamingOptions.MoviesheetMetadataMask = NamingConventions.DEFAULT_MOVIESHEET_METADATA_MASK;
                    break;
                case "8":
                    //moviesheets for parent folder
                    FileManager.Configuration.Options.NamingOptions.MoviesheetForParentFolderExtension = NamingConventions.DEFAULT_MOVIESHEET_EXTENSION;
                    FileManager.Configuration.Options.NamingOptions.MoviesheetForParentFolderMask = NamingConventions.DEFAULT_MSHEET_MOVIESHEET_FOR_PARENTFOLDER_MASK;
                    break;
                case "9":
                    // parent folder metadata
                    FileManager.Configuration.Options.NamingOptions.ParentFolderMetadataMask = NamingConventions.DEFAULT_PARENTFOLDER_METADATA_MASK;
                    break;
            }
        }

        private void btnSelectMTNPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog _ofd = new OpenFileDialog();
            _ofd.Title = "Select path to the mtn.exe file";
            _ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            _ofd.CheckFileExists = true;
            _ofd.Filter = "movie thumbnailer executable (mtn.exe)|mtn.exe";
            if ((bool)_ofd.ShowDialog(this))
            {
                FileManager.Configuration.Options.MTNPath = _ofd.FileName;
            }
        }

        private void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to discard all your settings and restore the default values?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                FileManager.Configuration.Reset();
                this.DialogResult = true;
                this.Close();
                FileManager.Configuration.LoadConfiguration();
            }
        }

        private void btnViewLog_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainMoviesheetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KeyValuePair<string, PredefinedNames> _pair = ((KeyValuePair<string, PredefinedNames>)(e.AddedItems)[0]);
            if (_pair.Value != null && _pair.Key != "0")
            {
                FileManager.Configuration.Options.NamingOptions.MoviesheetMask = _pair.Value.Mask;
            }
        }

        private void ExtraMoviesheetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KeyValuePair<string, PredefinedNames> _pair = ((KeyValuePair<string, PredefinedNames>)(e.AddedItems)[0]);
            if (_pair.Value != null && _pair.Key != "0")
            {
                FileManager.Configuration.Options.NamingOptions.MoviesheetForFolderMask = _pair.Value.Mask;
            }
        }

        private void MoviesheetExt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MoviesheetForFolderExt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MoviesheetForParentFolderExt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


        private void tbMoviesheet_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Tab && e.Key != Key.Enter && e.Key != Key.Left && e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Right)
            {
                MainMoviesheetCombo.SelectedValue = "0";
            }
        }

        private void tbMoviesheetForFolder_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab && e.Key != Key.Enter && e.Key != Key.Left && e.Key != Key.Up && e.Key != Key.Down && e.Key != Key.Right)
            {
                ExtraMoviesheetCombo.SelectedValue = "0";
            }
        }


        private void MoveUp(ListBox listBox)
        {
            if (listBox == null)
            {
                throw new ArgumentNullException("listBox");
            }
            MovieInfoProviderItemType item = (MovieInfoProviderItemType)listBox.SelectedItem;

            int idx = listBox.SelectedIndex;
            if (idx > 0)
            {
                FileManager.Configuration.Options.MovieSheetsOptions.MovieInfoPriorities.RemoveAt(idx);
                FileManager.Configuration.Options.MovieSheetsOptions.MovieInfoPriorities.Insert(--idx, item);
                listBox.SelectedIndex = idx;
            }
        }

        private void MoveDown(ListBox listBox)
        {
            if (listBox == null)
            {
                throw new ArgumentNullException("listBox");
            }
            MovieInfoProviderItemType item = (MovieInfoProviderItemType)listBox.SelectedItem;
            int idx = listBox.SelectedIndex;
            if (idx < (listBox.Items.Count - 1))
            {
                FileManager.Configuration.Options.MovieSheetsOptions.MovieInfoPriorities.RemoveAt(idx);
                FileManager.Configuration.Options.MovieSheetsOptions.MovieInfoPriorities.Insert(++idx, item);
                listBox.SelectedIndex = idx;
            }

        }

        private void InvalidateButtons()
        {
            BtnMoveUp.IsEnabled = (PrioritiesListBox.SelectedIndex != 0);
            BtnMoveDown.IsEnabled = (PrioritiesListBox.SelectedIndex != FileManager.Configuration.Options.MovieSheetsOptions.MovieInfoPriorities.Count - 1);
        }

        private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            MoveUp(PrioritiesListBox);
        }

        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            MoveDown(PrioritiesListBox);
           
        }

        private void PrioritiesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InvalidateButtons();
        }

        private void cmbIMDBCountries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResultsListBox.IMDbMovieInfoCache.Clear();
        }

        private void btnChangeTelnetPass_Click(object sender, RoutedEventArgs e)
        {
            TelnetHelper.ChangePassword(this);
        }

        private void btnDatePreview_Click(object sender, RoutedEventArgs e)
        {
            txtPreview.Text = Helpers.GetFormattedDate(DateTime.Now.Date.ToShortDateString()); //string.Format(string.Format("{{0:{0}}}", FileManager.Configuration.Options.CustomDateFormat), DateTime.Now.Date);
        }


    }

    public class ConnectionToBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value.ToString() == parameter as string) && (value != null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                return (UserOptions.ConnectionType)Enum.Parse(typeof(UserOptions.ConnectionType), parameter as string);
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
