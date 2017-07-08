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

namespace ThumbGen.Playlists
{
    /// <summary>
    /// Interaction logic for PlaylistWindow.xaml
    /// </summary>
    public partial class PlaylistWindow : Window
    {
        

        public static Dictionary<string, string> Criterias = new Dictionary<string, string>()
        {
            {PlaylistManager.NOSPLIT_CRITERIA, "No split; one file ->"}, 
            {"Genre", "Genre"}, {"Year", "Year"}, {"Director", "Director"}, {"Certification", "Certification"}, 
            {"Countries", "Country"},  {"Rating", "Rating"}, {"ResolutionText", "Resolution"}, {"AudioText", "Audio"},
            {"Name", "Title (1st char)"}, {"OriginalTitle", "Original Title (1st char)"}, {"Cast", "Actor"}
        };

        public static Dictionary<string, string> SortCriterias = new Dictionary<string, string>()
        {
            {"Alpha", "Title"}, {"AlphaFirst", "Title (1st char)"}, {"AlphaOrg", "Original Title"}, {"AlphaOrgFirst", "Original Title (1st char)"},
            {"Year", "Year"}, {"Rating", "Rating"}, {"ReleaseDate", "ReleaseDate"}
        };

        public static Dictionary<string, string> SortCriterias2 = new Dictionary<string, string>()
        {
            {"Alpha", "Title"}, {"AlphaFirst", "Title (1st char)"}, {"AlphaOrg", "Original Title"}, {"AlphaOrgFirst", "Original Title (1st char)"},
            {"Year", "Year"}, {"Rating", "Rating"}, {"ReleaseDate", "ReleaseDate"}
        };

        public PlaylistWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += delegate { DragMove(); };
        }

        public static bool Show(Window owner)
        {
            bool _result = false;

            PlaylistWindow _box = new PlaylistWindow();
            _box.Owner = owner;
            _box.WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
            _box.btnRemoveJob.IsEnabled = false;
            var res = _box.ShowDialog();
            if (res.HasValue && res.Value)
            {
                _result = true;
            }

            return _result;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void JobsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (JobsBox != null)
            {
                btnRemoveJob.IsEnabled = FileManager.Configuration.Options.PlaylistsJobs.Count > 1;
            }
        }

        private void btnAddJob_Click(object sender, RoutedEventArgs e)
        {
            FileManager.Configuration.Options.PlaylistsJobs.Add(new UserOptions.Playlists());
        }

        private void btnRemoveJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileManager.Configuration.Options.PlaylistsJobs.RemoveAt(JobsBox.SelectedIndex);
            }
            catch { }
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (UserOptions.Playlists _item in FileManager.Configuration.Options.PlaylistsJobs)
            {
                _item.IsActive = true;
            }
        }

        private void btnUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (UserOptions.Playlists _item in FileManager.Configuration.Options.PlaylistsJobs)
            {
                _item.IsActive = false;
            }
        }
    }


    public class SingleFilenameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value as string) == PlaylistManager.NOSPLIT_CRITERIA;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class ListBoxItemIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ListBoxItem item = value as ListBoxItem;

            if (item != null)
            {
                ListBox view = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;

                return view.ItemContainerGenerator.IndexFromContainer(item) + 1;
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
