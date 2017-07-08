using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileExplorer.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using ThumbGen.Properties;

namespace FileExplorer.ViewModel
{
    public class FileExplorerViewModel : ViewModelBase
    {
        #region // Private fields
        private ExplorerWindowViewModel _evm;
        private DirInfo _currentTreeItem;
        private IList<DirInfo> _sysDirSource; 
        #endregion

        #region // Public properties
        /// <summary>
        /// list of the directories 
        /// </summary>
        public IList<DirInfo> SystemDirectorySource
        {
            get { return _sysDirSource; }
            set
            {
                _sysDirSource = value;
                OnPropertyChanged("SystemDirectorySource");
            }
        }

        /// <summary>
        /// Current selected item in the tree
        /// </summary>
        public DirInfo CurrentTreeItem
        {
            get { return _currentTreeItem; }
            set
            {
                _currentTreeItem = value;
                _evm.CurrentDirectory = _currentTreeItem;
            }
        } 
        #endregion

        #region // .ctor
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="evm"></param>
        public FileExplorerViewModel(ExplorerWindowViewModel evm)
        {
            _evm = evm;

            //create a node for "my computer"
            // this will be the root for the file system tree
            DirInfo rootNode = new DirInfo(Resources.My_Computer_String, null);
            rootNode.Path = Resources.My_Computer_String;
            _evm.CurrentDirectory = rootNode; //make root node as the current directory
            // store Root in ExplorerWindowViewModel too
            evm.Root = rootNode;

            SystemDirectorySource = new List<DirInfo> { rootNode };
        } 
        #endregion

        //#region // public methods
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="curDir"></param>
        //public void ExpandToCurrentNode(DirInfo curDir)
        //{
        //    //expand the current selected node in tree 
        //    //if this is an ancestor of the directory we want to navigate or "My Computer" current node 
        //    if (CurrentTreeItem != null && (curDir.Path.Contains(CurrentTreeItem.Path) || CurrentTreeItem.Path == Resources.My_Computer_String))
        //    {
        //        // expand the current node
        //        // If the current node is already expanded then first collapse it n then expand it
        //        CurrentTreeItem.IsExpanded = false;
        //        CurrentTreeItem.IsExpanded = true;
        //        CurrentTreeItem.IsSelected = false;

        //        curDir.IsSelected = false;
        //        curDir.IsSelected = true;
        //    }
        //} 
        //#endregion  
    }
}
