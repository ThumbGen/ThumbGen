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
    /// Interaction logic for PopupInfoControl.xaml
    /// </summary>
    public partial class PopupInfoControl : UserControl
    {
        public PopupInfoControl()
        {
            InitializeComponent();
        }

        public override void EndInit()
        {
            base.EndInit();

            TheGrid.DataContext = this;
        }

        public bool SupportsIMDbSearch
        {
            get { return (bool)GetValue(SupportsIMDbSearchProperty); }
            set { SetValue(SupportsIMDbSearchProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SupportsIMDbSearch.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SupportsIMDbSearchProperty =
            DependencyProperty.Register("SupportsIMDbSearch", typeof(bool), typeof(PopupInfoControl), new UIPropertyMetadata(false));



        public bool SupportsMovieInfo
        {
            get { return (bool)GetValue(SupportsMovieInfoProperty); }
            set { SetValue(SupportsMovieInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SupportsMovieInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SupportsMovieInfoProperty =
            DependencyProperty.Register("SupportsMovieInfo", typeof(bool), typeof(PopupInfoControl), new UIPropertyMetadata(false));



        public bool SupportsBackdrops
        {
            get { return (bool)GetValue(SupportsBackdropsProperty); }
            set { SetValue(SupportsBackdropsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SupportsBackdrops.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SupportsBackdropsProperty =
            DependencyProperty.Register("SupportsBackdrops", typeof(bool), typeof(PopupInfoControl), new UIPropertyMetadata(false));




        public Country Country
        {
            get { return (Country)GetValue(CountryProperty); }
            set { SetValue(CountryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Country.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CountryProperty =
            DependencyProperty.Register("Country", typeof(Country), typeof(PopupInfoControl), new UIPropertyMetadata(Country.International));



    }
}
