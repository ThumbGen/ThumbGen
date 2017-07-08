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
using System.ComponentModel;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for ChooseSubtitles.xaml
    /// </summary>
    public partial class ChooseMovieFromIMDb : Window
    {
        public ChooseMovieFromIMDb()
        {
            InitializeComponent();

            ChooseMovieDialogResult = new ChooseMovieDialogResult();
            this.DataContext = this;
            this.MouseLeftButtonDown += delegate { DragMove(); };
            this.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ChooseMovieFromIMDb_PreviewMouseLeftButtonDown);
            this.Loaded += new RoutedEventHandler(ChooseMovieFromIMDb_Loaded);
            this.Closing += new CancelEventHandler(ChooseMovieFromIMDb_Closing);
        }

        void ChooseMovieFromIMDb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (m_AutoAdorner != null)
            {
                m_AutoAdorner.Cancel();
            }
        }

        private AutomaticAdornerHelper m_AutoAdorner;



        public bool IMDBMode
        {
            get { return (bool)GetValue(IMDBModeProperty); }
            set { SetValue(IMDBModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IMDBMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IMDBModeProperty =
            DependencyProperty.Register("IMDBMode", typeof(bool), typeof(ChooseMovieFromIMDb), new UIPropertyMetadata(true));

        private ChooseMovieDialogResult ChooseMovieDialogResult;

        void ChooseMovieFromIMDb_Loaded(object sender, RoutedEventArgs e)
        {
            if (FileManager.Mode == ProcessingMode.SemiAutomatic || FileManager.Mode == ProcessingMode.Automatic)
            {
                m_AutoAdorner = new AutomaticAdornerHelper(this.OkButton, FileManager.Configuration.Options.SemiautomaticTimeout);
            }
        }

        void ChooseMovieFromIMDb_Closing(object sender, CancelEventArgs e)
        {
            if (m_AutoAdorner != null)
            {
                m_AutoAdorner.Cancel();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ChooseMovieDialogResult.MovieInfo = this.MoviesBox.SelectedItem as MovieInfo;
            this.DialogResult = true;
        }

        public static ChooseMovieDialogResult GetCorrectMovie(Window owner, string keywords, string year, bool imdbSelection)
        {
            if (!string.IsNullOrEmpty(keywords))
            {
                List<MovieInfo> _list = new IMDBMovieInfo().GetMovies(keywords, year, FileManager.Configuration.Options.IMDBOptions.MaxCountResults);
                if (_list != null && _list.Count > 0)
                {
                    BindingList<MovieInfo> _candidates = new BindingList<MovieInfo>();
                    foreach (MovieInfo _movie in _list)
                    {
                        _candidates.Add(_movie);
                    }
                    return GetCorrectMovie(owner, _candidates, keywords, imdbSelection);
                }
            }

            return new ChooseMovieDialogResult();
        }

        public static ChooseMovieDialogResult GetCorrectMovie(Window owner, BindingList<MovieInfo> candidates, string keywords, bool imdbSelection)
        {
            ChooseMovieDialogResult _result = new ChooseMovieDialogResult();

            if (FileManager.Mode == ProcessingMode.FeelingLucky)
            {
                if (candidates != null && candidates.Count > 0)
                {
                    return new ChooseMovieDialogResult() { MovieInfo = candidates[0] };
                }
                else
                {
                    return new ChooseMovieDialogResult();
                }
            }
            else
            {
                if (candidates != null && candidates.Count == 1)
                {
                    _result = new ChooseMovieDialogResult() { MovieInfo = candidates[0] };
                }
                else
                {
                    if (candidates != null && candidates.Count > 0)
                    {
                        ChooseMovieFromIMDb _box = new ChooseMovieFromIMDb();
                        _box.Owner = owner;
                        _box.IMDBMode = imdbSelection;
                        _box.panelCollectorInfo.Visibility = imdbSelection ? Visibility.Collapsed : Visibility.Visible;
                        _box.tbKeywords.Text = keywords;
                        _box.WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
                        _box.MoviesBox.DataContext = candidates;
                        var res = _box.ShowDialog();
                        if (res.HasValue && res.Value && _box.MoviesBox.SelectedItem != null)
                        {
                            _result = _box.ChooseMovieDialogResult;
                        }
                    }
                }
                // if the selected item is a series item, remembed the seriesid
                if (_result != null && _result.MovieInfo != null && !string.IsNullOrEmpty(_result.MovieInfo.TVDBID))
                {
                    CurrentSeriesHelper.SeriesID = _result.MovieInfo.TVDBID;
                    CurrentSeriesHelper.SeriesIMDBID = _result.MovieInfo.IMDBID;
                    CurrentSeriesHelper.SeriesName = _result.MovieInfo.Name;
                }

                return _result;
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (IMDBMode)
            {
                string _keywords = this.tbKeywords.Text.Trim();
                if (!string.IsNullOrEmpty(_keywords))
                {
                    this.MoviesBox.DataContext = null;
                    int _i = 0;
                    _keywords = KeywordGenerator.ExtractYearFromTitle(_keywords, true, out _i);
                    string _year = _i == 0 ? null : _i.ToString();

                    this.MoviesBox.DataContext = new IMDBMovieInfo().GetMovies(_keywords, _year, FileManager.Configuration.Options.IMDBOptions.MaxCountResults);
                }
            }
        }

        private void tbKeywords_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                btnRefresh_Click(this, new RoutedEventArgs());
            }
        }

        private void imdbInfoButton_Click(object sender, RoutedEventArgs e)
        {
            string _imdbid = (sender as FrameworkElement).Tag as string;
            if (!string.IsNullOrEmpty(_imdbid))
            {
                Helpers.OpenUrlInBrowser(string.Format("http://www.imdb.com/title/{0}/", _imdbid));
            }
        }

        private void MoviesBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ChooseMovieDialogResult.MovieInfo = this.MoviesBox.SelectedItem as MovieInfo;
            this.DialogResult = true;
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            ChooseMovieDialogResult.WasSkipMoviePressed = true;
            this.DialogResult = true; // important so the box result is correctly returned
        }

        private void Missing_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }

    public class ChooseMovieDialogResult
    {
        public MovieInfo MovieInfo { get; set; }
        public bool WasSkipMoviePressed { get; set; }

        public ChooseMovieDialogResult()
        {
            MovieInfo = null;
            WasSkipMoviePressed = false;
        }
    }
}
