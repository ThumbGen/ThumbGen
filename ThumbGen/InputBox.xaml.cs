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
using System.Collections.ObjectModel;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        private string CurrentMovieFile;
        private InputBoxDialogResult InputBoxDialogResult;
        private AutomaticAdornerHelper m_AutoAdorner;

        public InputBox()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += delegate { DragMove(); };
            this.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(InputBox_PreviewMouseLeftButtonDown);
        }

        void InputBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (m_AutoAdorner != null)
            {
                m_AutoAdorner.Cancel();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CaptionBox.Focus();
            if (!string.IsNullOrEmpty(CaptionBox.Text))
            {
                CaptionBox.SelectAll();
            }

            if (FileManager.Mode == ProcessingMode.SemiAutomatic)
            {
                Button _btn = FileManager.Configuration.Options.PromptBeforeSearch ? this.OkButton : this.Skip;

                m_AutoAdorner = new AutomaticAdornerHelper(_btn, FileManager.Configuration.Options.SemiautomaticTimeout);
            }
        }

        public static InputBoxDialogResult Show(Window owner, string title, string message, string description,
                                                bool showAbortAllButton, bool showSnapshotsButton, string currentMovieFile, bool showGotoResultsButton)
        {
            InputBox _box = new InputBox();
            _box.Owner = owner;
            _box.WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;

            _box.CurrentMovieFile = currentMovieFile;
            _box.CaptionBox.Tag = message;
            _box.CaptionBox.Text = title;
            _box.DescriptionBlock.Text = description;
            _box.Abort.Visibility = showAbortAllButton ? Visibility.Visible : Visibility.Collapsed;
            _box.TakeSnapshots.Visibility = showSnapshotsButton ? Visibility.Visible : Visibility.Collapsed;
            _box.GotoResults.Visibility = showGotoResultsButton ? Visibility.Visible : Visibility.Collapsed;
            _box.InputBoxDialogResult = new InputBoxDialogResult();

            bool? _res = _box.ShowDialog();

            //Helpers.DoEvents();
            return _box.InputBoxDialogResult;
            //if (_res.HasValue && _res.Value)
            //{
            //    return new InputBoxDialogResult(_box.CaptionBox.Text.Trim(), Results, false);
            //}
            //else
            //{
            //    if (_box.Aborted)
            //    {
            //        return new InputBoxDialogResult(null, null, true);
            //    }
            //    else
            //    {
            //        return new InputBoxDialogResult(string.Empty, null, false);
            //    }
            //}
        }

        private void SetDialogResult(bool value)
        {
            // crashes
            //if(System.Windows.Interop.ComponentDispatcher.IsThreadModal)
            //{

            //    DialogResult = value;
            //}
        }

        private void StoreKeywords()
        {
            this.InputBoxDialogResult.Keywords = CaptionBox.Text.Trim();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            StoreKeywords();
            this.InputBoxDialogResult.Abort = false;
            SetDialogResult(true);
            Close();
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to abort all operations?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                StoreKeywords();
                this.InputBoxDialogResult.Abort = true;
                SetDialogResult(false);
                Close();
            }
        }

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            OkButton.IsEnabled = !string.IsNullOrEmpty((sender as TextBox).Text.Trim());
        }

        private void TakeSnapshots_Click(object sender, RoutedEventArgs e)
        {
            StoreKeywords();
            if (!string.IsNullOrEmpty(this.CurrentMovieFile))
            {
                this.InputBoxDialogResult.Results = new ObservableCollection<ResultItemBase>();
                MoviePlayer.Show(this.Owner, this.CurrentMovieFile, this.InputBoxDialogResult.Results, FileManager.Configuration.Options.ThumbnailSize);
            }
            SetDialogResult(true);
            Close();
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            this.InputBoxDialogResult.Keywords = string.Empty;
            SetDialogResult(false);
            Close();
        }

        private void SkipFolder_Click(object sender, RoutedEventArgs e)
        {
            this.InputBoxDialogResult.Keywords = string.Empty;
            this.InputBoxDialogResult.SkipFolder = true;
            SetDialogResult(false);
            Close();
        }

        private void GotoResults_Click(object sender, RoutedEventArgs e)
        {
            StoreKeywords();
            this.InputBoxDialogResult.Results = new ObservableCollection<ResultItemBase>();
            this.InputBoxDialogResult.GotoResults = true;
            SetDialogResult(true);
            Close();
        }
    }

    public class InputBoxDialogResult
    {
        public ObservableCollection<ResultItemBase> Results { get; set; }
        public string Keywords { get; set; }
        public bool Abort { get; set; }
        public bool SkipFolder { get; set; }
        public bool GotoResults { get; set; }
    }
}
