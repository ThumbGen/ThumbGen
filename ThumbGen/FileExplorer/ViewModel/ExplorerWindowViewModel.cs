using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using FileExplorer.Model;
using System.IO;
using System.Collections.Specialized;
using System.Windows.Input;
using ThumbGen.Properties;
using System.Windows;
using ThumbGen;
using FileExplorer.View;
using System.Threading;
using System.Windows.Threading;

namespace FileExplorer.ViewModel
{
    public class ExplorerWindowViewModel : ViewModelBase, IDisposable
    {
        #region // Private Members
        //private DirInfo _currentDirectory;
        private DirInfo _Root;
        private FileExplorerViewModel _fileTreeVM;
        private DirectoryViewerViewModel _dirViewerVM;
        private IList<DirInfo> _currentItems;
        private bool _showDirectoryTree = true;
        private ICommand _showTreeCommand;
        #endregion

        #region // .ctor
        public ExplorerWindowViewModel()
        {
            FileTreeVM = new FileExplorerViewModel(this);
            DirViewVM = new DirectoryViewerViewModel(this);
            ShowTreeCommand = new RelayCommand(param => this.DirectoryTreeHideHandler());
        }
        #endregion

        #region // Public Properties

        public DirInfo CurrentDirectory
        {
            get { return (DirInfo)GetValue(CurrentDirectoryProperty); }
            set { SetValue(CurrentDirectoryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentDirectory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentDirectoryProperty =
            DependencyProperty.Register("CurrentDirectory", typeof(DirInfo), typeof(ExplorerWindowViewModel),
                                new UIPropertyMetadata(null, OnCurrentDirectoryChanged));

        static void OnCurrentDirectoryChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ExplorerWindowViewModel _model = sender as ExplorerWindowViewModel;
            if (_model != null)
            {
                _model.CurrentDirectory.SetValue(FileSystemTree.IsLoadingVisibleProperty, true);
                _model.RefreshCurrentItems(false);
                _model.CurrentDirectory.SetValue(FileSystemTree.IsLoadingVisibleProperty, false);
            }
        }

        //public DirInfo CurrentDirectory
        //{
        //    get { return _currentDirectory; }
        //    set
        //    {
        //        _currentDirectory = value;
        //        RefreshCurrentItems();
        //        OnPropertyChanged("CurrentDirectory");
        //    }
        //}

        /// <summary>
        /// Tree View model
        /// </summary>
        public FileExplorerViewModel FileTreeVM
        {
            get { return _fileTreeVM; }
            set
            {
                _fileTreeVM = value;
                OnPropertyChanged("FileTreeVM");
            }
        }

        public DirInfo Root
        {
            get
            {
                return _Root;
            }
            set
            {
                _Root = value;
                OnPropertyChanged("Root");
            }
        }

        /// <summary>
        /// Visibility of the 
        /// </summary>
        public bool ShowDirectoryTree
        {
            get { return _showDirectoryTree; }
            set
            {
                _showDirectoryTree = value;
                OnPropertyChanged("ShowDirectoryTree");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public ICommand ShowTreeCommand
        {
            get { return _showTreeCommand; }
            set
            {
                _showTreeCommand = value;
                OnPropertyChanged("ShowTreeCommand");
            }
        }

        /// <summary>
        /// Tree View model
        /// </summary>
        public DirectoryViewerViewModel DirViewVM
        {
            get { return _dirViewerVM; }
            set
            {
                _dirViewerVM = value;
                OnPropertyChanged("DirViewVM");
            }
        }

        /// <summary>
        /// Children of the current directory to show in the right pane
        /// </summary>
        public IList<DirInfo> CurrentItems
        {
            get
            {
                if (_currentItems == null)
                {
                    _currentItems = new List<DirInfo>();
                }
                return _currentItems;
            }
            set
            {
                _currentItems = value;
                OnPropertyChanged("CurrentItems");
            }
        }
        #endregion

        #region // methods
        private void DirectoryTreeHideHandler()
        {
            ShowDirectoryTree = false;
        }

        public string LastSelectedFolder { get; private set; }

        private void ProcessFolder(DirInfo parent, IList<string> result)
        {
            if (parent.SubDirectories.Count != 0)
            {
                foreach (DirInfo _item in parent.SubDirectories)
                {
                    if (_item.IsChecked2 == null)
                    {
                        // if we found a nulled item, we must check its children coz at least one must be checked
                        ProcessFolder(_item, result);
                    }
                    else
                    {
                        if (_item.IsChecked2.Value)
                        {
                            // if we found a checked item, we add it to list and skip processing SubDirectories, they will be anyway fully scanned
                            result.Add(_item.Path);
                            LastSelectedFolder = _item.Path;
                        }
                        else
                        {
                            // if we found a not checked item, we do nothing, just skip processing it

                        }
                    }
                }
            }
        }

        public IList<string> CollectSelectedFolders()
        {
            List<string> _result = new List<string>();

            this.LastSelectedFolder = null;

            foreach (DirInfo _root in FileTreeVM.SystemDirectorySource)
            {
                if (_root.SubDirectories.Count != 0)
                {
                    foreach (DirInfo _item in _root.SubDirectories)
                    {
                        ProcessFolder(_item, _result);
                    }
                }
            }

            if (!string.IsNullOrEmpty(this.LastSelectedFolder))
            {
                FileManager.Configuration.Options.LastSelectedFolder = this.LastSelectedFolder;
            }

            return _result;
        }

        public DirInfo SelectPath(string pathToSelect)
        {
            DirInfo _result = null;

            try
            {
                if (!string.IsNullOrEmpty(pathToSelect))
                {
                    string[] _folders = pathToSelect.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    foreach (string _folder in _folders)
                    {
                        foreach (DirInfo _dir in CurrentDirectory.SubDirectories)
                        {
                            if (_dir.Name == _folder)
                            {
                                _dir.IsSelected = false;
                                _dir.IsSelected = true;
                                _dir.IsExpanded = false;
                                _dir.IsExpanded = true;
                                CurrentDirectory = _dir;
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
            return _result;
        }

        public void Refresh()
        {
            // remember current selection
            DirInfo _oldSelection = CurrentDirectory;
            this.Root.IsSelected = true;
            this.Root.ClearChildren();
            this.RefreshCurrentItems(true);
            // select back old selection
            SelectPath(_oldSelection.Path);
        }

        /// <summary>
        /// this method gets the children of current directory and stores them in the CurrentItems Observable collection
        /// </summary>
        public void RefreshCurrentItems(bool reload)
        {
            IList<DirInfo> childDirList = new List<DirInfo>();
            IList<DirInfo> childFileList = new List<DirInfo>();

            if (reload)
            {
                CurrentDirectory.IsLoaded = false;
            }
            //If current directory is "My computer" then get the all logical drives in the system
            if (CurrentDirectory.Name.Equals(Resources.My_Computer_String))
            {
                childDirList = (from rd in FileSystemExplorerService.GetRootDirectories()
                                select new DirInfo(rd)).ToList();

                CurrentDirectory.LoadDirectories(childDirList);
            }
            else
            {
                //Combine all the subdirectories and files of the current directory
                if (ThumbGen.Helpers.IsDirectory(CurrentDirectory.Path))
                {
                    childDirList = (from dir in FileSystemExplorerService.GetChildDirectories(CurrentDirectory.Path)
                                    select new DirInfo(dir, CurrentDirectory)).ToList();

                    UserOptions _options = FileManager.Configuration.Options;
                    bool _isFilterActive = _options.FileBrowserOptions.IsFilterActive();

                    foreach (FileInfo _file in FileSystemExplorerService.GetChildFiles(CurrentDirectory.Path))
                    {
                        if (string.Compare(Path.GetFileName(_file.FullName), ThumbGen.MP4Tagger.MP4Manager.DEST_FILE_NAME) != 0)
                        {
                            bool _skip = false;

                            DirInfo _item = new DirInfo(_file, CurrentDirectory, _isFilterActive);

                            if (!_skip)
                            {
                                childFileList.Add(_item);
                            }
                        }
                    }
                }

                childDirList = childDirList.Concat(childFileList).ToList();

                CurrentDirectory.LoadDirectories(childDirList);
            }
            CurrentItems = childDirList;
        }
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (CurrentDirectory != null)
            {
                CurrentDirectory.Dispose();
            }
        }

        #endregion
    }
}
