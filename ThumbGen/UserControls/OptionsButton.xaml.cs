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
    /// Interaction logic for OptionsButton.xaml
    /// </summary>
    public partial class OptionsButton : UserControl
    {
        public OptionsButton()
        {
            InitializeComponent();
        }

        private void AdvancedOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            Options.Show(null, FileManager.Configuration.Options);
        }
    }
}
