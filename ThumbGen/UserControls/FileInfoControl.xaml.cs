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

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for FileInfoControl.xaml
    /// </summary>
    public partial class FileInfoControl : UserControl
    {
        public FileInfoControl()
        {
            InitializeComponent();
        }

        public override void EndInit()
        {
            base.EndInit();
            TheGrid.DataContext = this;
        }

        public bool HasExternalSubtitles
        {
            get { return (bool)GetValue(HasExternalSubtitlesProperty); }
            set { SetValue(HasExternalSubtitlesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasExternalSubtitles.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasExternalSubtitlesProperty =
            DependencyProperty.Register("HasExternalSubtitles", typeof(bool), typeof(FileInfoControl), new UIPropertyMetadata(false));

        public bool HasMovieInfo
        {
            get { return (bool)GetValue(HasMovieInfoProperty); }
            set { SetValue(HasMovieInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasMovieInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasMovieInfoProperty =
            DependencyProperty.Register("HasMovieInfo", typeof(bool), typeof(FileInfoControl), new UIPropertyMetadata(false));

        public bool HasMoviesheet
        {
            get { return (bool)GetValue(HasMoviesheetProperty); }
            set { SetValue(HasMoviesheetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasMoviesheet.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasMoviesheetProperty =
            DependencyProperty.Register("HasMoviesheet", typeof(bool), typeof(FileInfoControl), new UIPropertyMetadata(false));

        public bool HasMoviesheetMetadata
        {
            get { return (bool)GetValue(HasMoviesheetMetadataProperty); }
            set { SetValue(HasMoviesheetMetadataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasMoviesheetMetadata.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasMoviesheetMetadataProperty =
            DependencyProperty.Register("HasMoviesheetMetadata", typeof(bool), typeof(FileInfoControl), new UIPropertyMetadata(false));


    }
}
