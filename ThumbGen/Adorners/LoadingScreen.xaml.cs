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
    /// Interaction logic for LoadingScreen.xaml
    /// </summary>
    public partial class LoadingScreen : UserControl
    {
        public LoadingScreen(): this(string.Empty, false)
        {
            
        }

        public LoadingScreen(string customMessage, bool showCancelSearch)
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(customMessage))
            {
                CustomMessage = customMessage;
            }
            CancelSearchButton.Visibility = showCancelSearch ? Visibility.Visible : Visibility.Collapsed;
            if (FileManager.Mode == ProcessingMode.Automatic || FileManager.Mode == ProcessingMode.SemiAutomatic || FileManager.Mode == ProcessingMode.FeelingLucky)
            {
                CustomAbortButtonText = "Abort processing";
            }
        }

        public override void EndInit()
        {
            base.EndInit();
        }

        public Brush CustomBackground
        {
            get { return (Brush)GetValue(CustomBackgroundProperty); }
            set { SetValue(CustomBackgroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CustomBackground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomBackgroundProperty =
            DependencyProperty.Register("CustomBackground", typeof(Brush), typeof(LoadingScreen), new UIPropertyMetadata(Brushes.White));



        public double CustomOpacity
        {
            get { return (double)GetValue(CustomOpacityProperty); }
            set { SetValue(CustomOpacityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CustomOpacity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomOpacityProperty =
            DependencyProperty.Register("CustomOpacity", typeof(double), typeof(LoadingScreen), new UIPropertyMetadata((double)0.75));



        public string CustomMessage
        {
            get { return (string)GetValue(CustomMessageProperty); }
            set { SetValue(CustomMessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CustomMessage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomMessageProperty =
            DependencyProperty.Register("CustomMessage", typeof(string), typeof(LoadingScreen), new UIPropertyMetadata("Loading..."));



        public string CustomAbortButtonText
        {
            get { return (string)GetValue(CustomAbortButtonTextProperty); }
            set { SetValue(CustomAbortButtonTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CustomAbortButtonText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomAbortButtonTextProperty =
            DependencyProperty.Register("CustomAbortButtonText", typeof(string), typeof(LoadingScreen), new UIPropertyMetadata("Abort search and view results"));



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).Content = "Cancelling...";
            (sender as Button).IsEnabled = false;
            FileManager.CancellationPending = true;
        }



    }
}
