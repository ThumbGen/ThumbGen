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
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public bool IsAbout
        {
            get { return (bool)GetValue(IsAboutProperty); }
            set { SetValue(IsAboutProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAbout.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAboutProperty =
            DependencyProperty.Register("IsAbout", typeof(bool), typeof(SplashWindow), new UIPropertyMetadata(false));


        public static void ShowAbout(Window owner)
        {
            SplashWindow _win = new SplashWindow();
            _win.Owner = owner;
            _win.IsAbout = true;
            if (owner != null)
            {
                _win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            _win.ShowDialog();

        }

        private void Twitter_Click(object sender, RoutedEventArgs args)
        {
            Helpers.OpenUrlInBrowser("http://twitter.com/ThumbGen");
        }
    }
}
