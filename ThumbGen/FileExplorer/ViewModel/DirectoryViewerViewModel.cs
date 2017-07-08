using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileExplorer.Model;
using System.Windows.Threading;

namespace FileExplorer.ViewModel
{
    /// <summary>
    /// View model for the right side pane
    /// </summary>
    public class DirectoryViewerViewModel : ViewModelBase
    {
        #region // Private variables
        private ExplorerWindowViewModel _evm;
        private DirInfo _currentItem; 
        #endregion

        #region // .ctor
        public DirectoryViewerViewModel(ExplorerWindowViewModel evm)
        {
            _evm = evm;
        } 
        #endregion

        #region // Public members
        /// <summary>
        /// Indicates the current directory in the Directory view pane
        /// </summary>
        public DirInfo CurrentItem
        {
            get { return _currentItem; }
            set { _currentItem = value; }
        } 
        #endregion

        #region // Public Methods
        /// <summary>
        /// processes the current object. If this is a file then open it or if it is a directory then return its subdirectories
        /// </summary>
        public void OpenCurrentObject()
        {
            try
            {
                if (CurrentItem != null)
                {
                    ObjectType objType = CurrentItem.DirType; //Dir/File type

                    if (CurrentItem.DirType == ObjectType.File)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(CurrentItem.Path);
                        }
                        catch
                        {
                            MessageBox.Show("Cannot find associated program.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        _evm.CurrentDirectory.IsExpanded = false;
                        _evm.CurrentDirectory.IsExpanded = true;
                        foreach (DirInfo _item in _evm.CurrentDirectory.SubDirectories)
                        {
                            if (_item.Path == CurrentItem.Path)
                            {
                                _item.IsSelected = true;

                                if (_evm.FileTreeVM.CurrentTreeItem != null)
                                {
                                    _evm.FileTreeVM.CurrentTreeItem.IsExpanded = true;
                                }
                                return;
                            }
                        }
                    }
                }
            }
            catch { }
        } 

        #endregion
    }
}
