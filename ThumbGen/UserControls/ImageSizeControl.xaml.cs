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
    /// Interaction logic for ImageSizeControl.xaml
    /// </summary>
    public partial class ImageSizeControl : UserControl
    {
        public ImageSizeControl()
        {
            InitializeComponent();
            this.TheCheckbox.DataContext = this;
        }



        public bool IsResizing
        {
            get { return (bool)GetValue(IsResizingProperty); }
            set { SetValue(IsResizingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsResizing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsResizingProperty =
            DependencyProperty.Register("IsResizing", typeof(bool), typeof(ImageSizeControl), new UIPropertyMetadata(false));

        

        public int WidthPx
        {
            get { return (int)GetValue(WidthPxProperty); }
            set { SetValue(WidthPxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WidthPx.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WidthPxProperty =
            DependencyProperty.Register("WidthPx", typeof(int), typeof(ImageSizeControl), new UIPropertyMetadata(0));



        public int HeightPx
        {
            get { return (int)GetValue(HeightPxProperty); }
            set { SetValue(HeightPxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeightPx.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeightPxProperty =
            DependencyProperty.Register("HeightPx", typeof(int), typeof(ImageSizeControl), new UIPropertyMetadata(0));

        
    }

}
