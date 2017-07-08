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
using System.ComponentModel;

namespace ThumbGen.Subtitles
{
    /// <summary>
    /// Interaction logic for ChooseSubtitles.xaml
    /// </summary>
    public partial class ChooseSubtitles : Window
    {
        public ChooseSubtitles()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += delegate { DragMove(); };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;            
        }

        public static subRes Show(Window owner, BindingList<subRes> candidates)
        {
            subRes _result = null;

            ChooseSubtitles _box = new ChooseSubtitles();
            _box.Owner = owner;
            _box.WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
            _box.SubtitlesBox.DataContext = candidates;
            var res = _box.ShowDialog();
            if (res.HasValue && res.Value && _box.SubtitlesBox.SelectedItem != null)
            {
                _result = _box.SubtitlesBox.SelectedItem as subRes;
            }

            return _result;
        }
    }
}
