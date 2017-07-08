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

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for PreviewImage.xaml
    /// </summary>
    public partial class PreviewImage : Window
    {
        public PreviewImage()
        {
            InitializeComponent();
            TheImage.DataContext = this;
        }

        public string ImageUrl
        {
            get { return (string)GetValue(ImageUrlProperty); }
            set { SetValue(ImageUrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageUrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageUrlProperty =
            DependencyProperty.Register("ImageUrl", typeof(string), typeof(PreviewImage), new UIPropertyMetadata(null));



        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(PreviewImage), new UIPropertyMetadata(null));


        public static void Show(Window owner, string imageUrl)
        {
            Show(owner, imageUrl, null);
        }

        public static void Show(Window owner, ImageSource imgsrc)
        {
            Show(owner, null, imgsrc);
        }

        private static void Show(Window owner, string imageUrl, ImageSource imgsrc)
        {
            PreviewImage _form = new PreviewImage();
            _form.Owner = owner;
            if (owner != null)
            {
                _form.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            if (imgsrc == null)
            {
                _form.TheImage.Source = Helpers.LoadImage(imageUrl);
            }
            else
            {
                _form.TheImage.Source = imgsrc;
            }
            switch (FileManager.Configuration.Options.PreviewType)
            {
                case PreviewType.ActualSize:
                    _form.SetActualSize();
                    break;
                case PreviewType.BestFit:
                default:
                    _form.SetBestFit();
                    break;
            }
            _form.Show();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void SetActualSize()
        {
            TheImage.Stretch = Stretch.None;
        }

        private void SetBestFit()
        {
            TheImage.Stretch = Stretch.Uniform;
        }

        private void ViewActualSizeButton_Click(object sender, RoutedEventArgs e)
        {
            SetActualSize();
            FileManager.Configuration.Options.PreviewType = PreviewType.ActualSize;
        }

        private void ViewFullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            SetBestFit();
            FileManager.Configuration.Options.PreviewType = PreviewType.BestFit;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }
    }

    public enum PreviewType
    {
        BestFit,
        ActualSize,
    }
}
