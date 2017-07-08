using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Markup;
using System.Xml;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for BatchApplyFolderBox.xaml
    /// </summary>
    public partial class BatchApplyFolderBox : Window
    {
        private BatchApplyFolderBox()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += delegate { DragMove(); };
        }

        private bool OverwriteAllFlag = false;

        

        public static bool Show(Window owner, string folder, string imageUrl)
        {
            BatchApplyFolderBox _box = new BatchApplyFolderBox();
            _box.Owner = owner;
            _box.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _box.TheFolder.Text = string.Format(@"{0}\", folder);

            _box.PromptWatermarkText.Visibility = (owner is ResultsListBox) ? (owner as ResultsListBox).Watermark.Visibility : Visibility.Collapsed;

            AsyncImageDownloader.GetImageAsync(_box, imageUrl, SetImageData);

            bool? _res = _box.ShowDialog();

            if (_res.HasValue && _res.Value)
            {
                bool _promptUser = FileManager.Configuration.Options.AddWatermark && (bool)_box.rbManual.IsChecked;
                _box.Dispatcher.BeginInvoke((Action)delegate
                {
                    IEnumerable<FileInfo> _movies = new FilesCollector().CollectFiles(folder, false);
                    string _season = string.Empty;

                    foreach (FileInfo _movie in _movies)
                    {
                        string _destFile = Helpers.GetCorrectThumbnailPath(_movie.FullName, true);
                        if (File.Exists(_destFile) && !_box.OverwriteAllFlag)
                        {
                            continue;
                        }

                        string _text = FileManager.Configuration.Options.WatermarkOptions.Text;

                        if (!_promptUser)
                        {
                            //detect season 
                            EpisodeData _epData = EpisodeData.GetEpisodeData(_movie.Name);
                            string _s = _epData.Season;
                            _season = string.IsNullOrEmpty(_s) ? _season : _s;
                            // detect episode
                            string _episode = _epData.Episode;
                            if (string.IsNullOrEmpty(_episode))
                            {
                                // detect Cd number
                                _episode = KeywordGenerator.ExtractCDNumber(_movie.Name);
                                if (string.IsNullOrEmpty(_episode))
                                {
                                    // default text
                                    _episode = FileManager.Configuration.Options.WatermarkOptions.Text;
                                }
                            }

                            // apply mask
                            string _mask = FileManager.Configuration.Options.BatchAutoMask;
                            if (string.IsNullOrEmpty(_mask))
                            {
                                _mask = "S$SE$E";
                            }

                            _text = _mask.Replace("$S", _season).Replace("$E", _episode).Trim();
                        }
                        else
                        {
                            InputBoxDialogResult _ibres = InputBox.Show(null, _text,
                                                                        "type text here",
                                                                        "Add custom text for " + System.IO.Path.GetFileName(_movie.FullName),
                                                                        false, false, null, false);
                            if (_ibres.SkipFolder)
                            {
                                return;
                            }
                            if (string.IsNullOrEmpty(_ibres.Keywords))
                            {
                                continue;
                            }
                            _text = _ibres.Keywords;
                        }
                        
                        FileManager.Configuration.Options.WatermarkOptions.Text = string.IsNullOrEmpty(_text) ? FileManager.Configuration.Options.WatermarkOptions.Text : _text;                        

                        Helpers.CreateThumbnailImage(imageUrl, _destFile, FileManager.Configuration.Options.KeepAspectRatio);
                    }
                }, _promptUser ? DispatcherPriority.Send : DispatcherPriority.Background);
            }
            return (bool)_res;
        }

        private static void SetImageData(BitmapImage bmp, string imageUrl, object userData)
        {
            BatchApplyFolderBox _box = userData as BatchApplyFolderBox;
            if (_box != null)
            {
                // just to be sure...
                _box.TheImage.Source = bmp;

                // clone the watermark from the Owner window... just for the sake of display consistency ;)
                ResultsListBox _form = _box.Owner as ResultsListBox;
                if (_form != null)
                {
                    try
                    {
                        RenderTargetBitmap _bmp = new RenderTargetBitmap((int)_form.newImage.Width, (int)_form.newImage.Height, 96, 96, PixelFormats.Pbgra32);
                        _bmp.Render(_form.NewImageCanvas);
                        _box.TheImage.Source = _bmp;
                    }
                    catch { }
                }
            }
        }

        private void UpdateMissing_Click(object sender, RoutedEventArgs e)
        {
            OverwriteAllFlag = false;
            this.DialogResult = true;
        }

        private void OverwriteAll_Click(object sender, RoutedEventArgs e)
        {
            OverwriteAllFlag = true;
            this.DialogResult = true;
        }


    }
}
