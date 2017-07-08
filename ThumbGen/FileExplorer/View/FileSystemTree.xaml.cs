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
using FileExplorer.ViewModel;
using ThumbGen;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Threading;

namespace FileExplorer.View
{
    /// <summary>
    /// Interaction logic for FileSystemTree.xaml
    /// </summary>
    public partial class FileSystemTree : UserControl
    {
        #region // Private Variables
        private ExplorerWindowViewModel myViewModel;
        #endregion

        #region // .ctor
        public FileSystemTree()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(ViewLoaded);
        }
        #endregion




        public static bool GetIsLoadingVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsLoadingVisibleProperty);
        }

        public static void SetIsLoadingVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsLoadingVisibleProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsLoadingVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoadingVisibleProperty =
            DependencyProperty.RegisterAttached("IsLoadingVisible", typeof(bool), typeof(FileSystemTree), new UIPropertyMetadata(false));

                
        

        #region // Event Handlers
        private void ViewLoaded(object sender, RoutedEventArgs r)
        {
            myViewModel = this.DataContext as ExplorerWindowViewModel;
            (DirectoryTree.Items[0] as DirInfo).IsSelected = true;
            (DirectoryTree.Items[0] as DirInfo).IsExpanded = true;
        }

        private void DirectoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DirectoryTree.SelectedItem != null && myViewModel != null)
            {
                myViewModel.FileTreeVM.CurrentTreeItem = DirectoryTree.SelectedItem as DirInfo;
            }

        }

        private void TreeView_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem currentTreeNode = sender as TreeViewItem;
            if (currentTreeNode == null)
                return;

            if (currentTreeNode.ItemsSource == null)
                return;

            DirInfo parentDirectory = currentTreeNode.Header as DirInfo;
            if (parentDirectory == null)
                return;

            foreach (DirInfo d in currentTreeNode.ItemsSource)
            {
                if (myViewModel.CurrentDirectory.Path.Equals(d.Path))
                {
                    d.IsSelected = true;
                    d.IsExpanded = true;
                    break;
                }
            }

            DirInfo _current = (currentTreeNode.DataContext as DirInfo);
            _current.IsSelected = true;

            if (currentTreeNode.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                EventHandler itemsGenerated = null;
                itemsGenerated = delegate(object s, EventArgs args)
                {
                    if ((s as ItemContainerGenerator).Status == GeneratorStatus.ContainersGenerated)
                    {
                        (s as ItemContainerGenerator).StatusChanged -= itemsGenerated;
                        currentTreeNode.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, (ThreadStart)delegate
                        {
                            Mouse.OverrideCursor = null;
                            _current.SetValue(FileSystemTree.IsLoadingVisibleProperty, false);
                        });
                    }
                };
                currentTreeNode.ItemContainerGenerator.StatusChanged += itemsGenerated;
                Mouse.OverrideCursor = Cursors.Wait;
                _current.SetValue(FileSystemTree.IsLoadingVisibleProperty, true);
                try
                {
                    Helpers.DoEvents();
                }
                catch { }
            }

            foreach (DirInfo _child in _current.SubDirectories)
            {
                if (_current.IsChecked2.HasValue)
                {
                    _child.IsChecked2 = _current.IsChecked2;
                }
            }

            e.Handled = true;
        }
    
        #endregion

        private void treeScroller_Loaded(object sender, RoutedEventArgs e)
        {
            DirectoryTree.AddHandler(MouseWheelEvent, new RoutedEventHandler(MyMouseWheelH), true);
        }

        private void MyMouseWheelH(object sender, RoutedEventArgs e)
        {
            MouseWheelEventArgs eargs = (MouseWheelEventArgs)e;
            double x = (double)eargs.Delta;
            double y = treeScroller.VerticalOffset;
            treeScroller.ScrollToVerticalOffset(y - x);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (this.myViewModel != null)
            {
                this.myViewModel.Refresh();
            }
        }

        private void Resort_Click(object sender, RoutedEventArgs e)
        {
            this.myViewModel.Refresh();
        }

    }
}
