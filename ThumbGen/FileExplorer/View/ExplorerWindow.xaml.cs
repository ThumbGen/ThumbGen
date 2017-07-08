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
using ThumbGen;
using ThumbGen.MovieSheets;
using ThumbGen.Playlists;
using Fluent;
using FileExplorer.ViewModel;

namespace FileExplorer.View
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ExplorerWindow : RibbonWindow
    {
        public ExplorerWindow()
        {
            InitializeComponent();
        }

        public ExplorerWindow(Window owner)
            : this()
        {
            this.Owner = owner;
        }

        public override void EndInit()
        {
            base.EndInit();
            KhedasFixPanel.Visibility = FileManager.DisableKhedasFix ? Visibility.Collapsed : Visibility.Visible;
            GetRandomSnapshotsMix.DataContext = FileManager.Configuration.Options;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FileManager.Mode = (ProcessingMode)Enum.Parse(typeof(ProcessingMode), (sender as FrameworkElement).Tag as string);

            if (FileManager.Configuration.Options.FileBrowserOptions.IsFilterActive())
            {
                if (MessageBox.Show("There is a filter active. The files excluded by the filter will not be collected for processing.\r\n\r\nAre you sure you want to continue?",
                           "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.No)
                {
                    return;
                }
            }
            GC.Collect();

            switch (FileManager.Mode)
            {
                case ProcessingMode.Manual:
                    StartActionType = ThumbGen.StartActionType.Process;
                    break;
                case ProcessingMode.SemiAutomatic:
                    StartActionType = ThumbGen.StartActionType.ProcessSemiautomatic;
                    break;
                case ProcessingMode.Automatic:
                    StartActionType = ThumbGen.StartActionType.ProcessAutomatic;
                    break;
                case ProcessingMode.FeelingLucky:
                    StartActionType = ThumbGen.StartActionType.ProcessFeelingLucky;
                    break;
                default:
                    StartActionType = ThumbGen.StartActionType.Process;
                    break;
            }
            
            this.DialogResult = true;
        }

        private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            directoryViewer.AddHandler(MouseWheelEvent, new RoutedEventHandler(MyMouseWheelH), true);

        }

        private void MyMouseWheelH(object sender, RoutedEventArgs e)
        {
            MouseWheelEventArgs eargs = (MouseWheelEventArgs)e;
            double x = (double)eargs.Delta;
            double y = directoryViewerScroller.VerticalOffset;
            directoryViewerScroller.ScrollToVerticalOffset(y - x);
        }

        public StartActionType StartActionType = StartActionType.Unknown;

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to rename all *.jpg files to *.jpg_tg\r\n and generate the zzz.mp4 files instead (for the checked folders)?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                GC.Collect();
                StartActionType = StartActionType.FixNetworkShares;
                this.DialogResult = true;
            }
        }

        private void GenerateDummyFile_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to generate the dummy file for all checked movies?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                GC.Collect();
                StartActionType = StartActionType.GenerateDummyFile;
                this.DialogResult = true;
            }
        }


        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to rename all *.jpg_tg files to *.jpg\r\n and remove the zzz.mp4 files (for the checked folders)?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                GC.Collect();
                StartActionType = StartActionType.UnfixNetworkShares;
                this.DialogResult = true;
            }
        }

        private void Automatic_Click(object sender, RoutedEventArgs e)
        {
            //if (MessageBox.Show("Are you sure you want to let me find thumbnails for the selected movies?\n\nI recommend you to use .nfo files and collectors supporting IMDb search.", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                GC.Collect();
                StartActionType = StartActionType.ProcessAutomatic;
                this.DialogResult = true;
            }
        }

        private void GetRandomSnapshotsMix_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to let me generate automatically thumbnails for the selected movies?\n\n"+
                                "Thumbnails will be built from several snapshots taken from the movie.", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                GC.Collect();
                StartActionType = StartActionType.GenerateRandomThumbs;
                this.DialogResult = true;
            }
        }

        private void UpdateMoviesheets_Click(object sender, RoutedEventArgs e)
        {
            if (SelectTemplateBox.Show(this))
            {
                StartActionType = StartActionType.UpdateMoviesheetsTemplate;
                this.DialogResult = true;
            }
        }

        private void CreatePlaylists_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistWindow.Show(this))
            {
                StartActionType = StartActionType.CreatePlaylist;
                this.DialogResult = true;
            }
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            Options.Show(this, FileManager.Configuration.Options);
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as ExplorerWindowViewModel).Refresh();
        }

    }
    
}
