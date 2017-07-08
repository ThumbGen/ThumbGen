using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using FileExplorer.ViewModel;
using ThumbGen;

namespace FileExplorer.View
{
    /// <summary>
    /// Interaction logic for DirectoryViewer.xaml
    /// </summary>
    public partial class DirectoryViewer : UserControl
    {
        #region // Private members
        private ExplorerWindowViewModel _viewModel; 
        #endregion

        #region // .ctor
        public DirectoryViewer()
        {
            InitializeComponent();
            Loaded += DirectoryViewer_Loaded;
        }

        #endregion

        #region // Event Handlers
        void DirectoryViewer_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = this.DataContext as ExplorerWindowViewModel;
        }

        private void dirList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.DirViewVM.OpenCurrentObject();
        }

        private void dirList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.DirViewVM.OpenCurrentObject();
            }
        }

        private void PreviewImageButton_Click(object sender, RoutedEventArgs e)
        {
            PreviewImage.Show(Window.GetWindow(this), this.BackdropImage.Source as ImageSource);
        }

        #endregion

        
    }
}
