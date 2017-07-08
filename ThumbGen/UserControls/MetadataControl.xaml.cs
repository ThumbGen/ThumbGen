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
    /// Interaction logic for MetadataControl.xaml
    /// </summary>
    public partial class MetadataControl : UserControl
    {
        public MetadataControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public bool IsMain
        {
            get { return (bool)GetValue(IsMainProperty); }
            set { SetValue(IsMainProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMain.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMainProperty =
            DependencyProperty.Register("IsMain", typeof(bool), typeof(MetadataControl), new UIPropertyMetadata(true));

        public bool IsToolbarVisible
        {
            get { return (bool)GetValue(IsToolbarVisibleProperty); }
            set { SetValue(IsToolbarVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsToolbarVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsToolbarVisibleProperty =
            DependencyProperty.Register("IsToolbarVisible", typeof(bool), typeof(MetadataControl), new UIPropertyMetadata(true));

        
        


    }
}
