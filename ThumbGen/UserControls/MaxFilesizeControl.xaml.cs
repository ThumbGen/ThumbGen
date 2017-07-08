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
    /// Interaction logic for MaxFilesizeControl.xaml
    /// </summary>
    public partial class MaxFilesizeControl : UserControl
    {
        public MaxFilesizeControl()
        {
            InitializeComponent();
            this.TheGroupBox.DataContext = this;
        }



        public bool IsMaxQuality
        {
            get { return (bool)GetValue(IsMaxQualityProperty); }
            set { SetValue(IsMaxQualityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMaxQuality.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMaxQualityProperty =
            DependencyProperty.Register("IsMaxQuality", typeof(bool), typeof(MaxFilesizeControl), new UIPropertyMetadata(true));

        


        public int LimitBytes
        {
            get { return (int)GetValue(LimitBytesProperty); }
            set { SetValue(LimitBytesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LimitBytes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LimitBytesProperty =
            DependencyProperty.Register("LimitBytes", typeof(int), typeof(MaxFilesizeControl), new UIPropertyMetadata(500000));

        

    }
}
