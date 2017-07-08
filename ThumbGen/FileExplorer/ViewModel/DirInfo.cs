using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Collections;
using System.ComponentModel;
using FileExplorer.Model;
using System.Windows.Threading;
using System.Windows.Media;
using ThumbGen;
using System.Windows.Media.Imaging;


namespace FileExplorer.ViewModel
{
    /// <summary>
    /// Enum to hold the Types of different file objects
    /// </summary>
    public enum ObjectType
    {
        MyComputer = 0,
        DiskDrive = 1,
        Directory = 2,
        File = 3
    }

    /// <summary>
    /// Class for containing the information about a Directory/File
    /// </summary>
    public class DirInfo : DependencyObject, IDisposable
    {
        #region // Public Properties
        public string Name { get; set; }
        public string Path { get; set; }
        public string Root { get; set; }
        public string Size { get; set; }
        public string Ext { get; set; }
        public ObjectType DirType { get; set; }
        public DirInfo Parent { get; set; }
        public bool IsChecking { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsDummy { get; private set; }
        public string ImgSource { get; private set; }
        #endregion

        #region // Dependency Properties



        public bool? IsChecked2
        {
            get { return (bool?)GetValue(IsChecked2Property); }
            set { SetValue(IsChecked2Property, value); }
        }

        // Using a DependencyProperty as the backing store for IsChecked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsChecked2Property =
            DependencyProperty.Register("IsChecked2", typeof(bool?), typeof(DirInfo), new UIPropertyMetadata(false, IsCheckedPropertyChanged));

        static void IsCheckedPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            DirInfo _dirInfo = obj as DirInfo;

            if (!_dirInfo.IsChecking)
            {
                _dirInfo.IsSelected = true;
            }

            if (_dirInfo.IsChecking)
            {
                _dirInfo.IsChecking = false;
                return;
            }

            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                _dirInfo.SetIsChecked((bool?)e.OldValue, (bool?)e.NewValue, true, true);
            }, DispatcherPriority.Loaded);

        }

        void SetIsChecked(bool? oldvalue, bool? value, bool updateChildren, bool updateParent)
        {
            this.IsChecking = true;

            if (value == oldvalue)
                return;

            IsChecked2 = value;

            try
            {
                if (updateChildren && IsChecked2.HasValue)
                {
                    foreach (DirInfo _dir in this.SubDirectories)
                    {
                        _dir.SetIsChecked(_dir.IsChecked2, IsChecked2, true, false);
                    }
                }
            }
            catch { }
            try
            {
                if (updateParent && this.Parent != null)
                    Parent.VerifyCheckState();
            }
            catch { }
            this.IsChecking = false;
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < this.SubDirectories.Count; ++i)
            {
                bool? current = this.SubDirectories[i].IsChecked2;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            this.SetIsChecked(IsChecked2, state, false, true);
        }

        public static readonly DependencyProperty propertyChilds = DependencyProperty.Register("Childs", typeof(IList<DirInfo>), typeof(DirInfo));
        public ObservableCollection<DirInfo> SubDirectories
        {
            get { return (ObservableCollection<DirInfo>)GetValue(propertyChilds); }
            set { SetValue(propertyChilds, value); }
        }

        public static readonly DependencyProperty propertyIsExpanded = DependencyProperty.Register("IsExpanded", typeof(bool), typeof(DirInfo));
        public bool IsExpanded
        {
            get { return (bool)GetValue(propertyIsExpanded); }
            set { SetValue(propertyIsExpanded, value); }
        }

        public static readonly DependencyProperty propertyIsSelected = DependencyProperty.Register("IsSelected", typeof(bool), typeof(DirInfo));
        public bool IsSelected
        {
            get { return (bool)GetValue(propertyIsSelected); }
            set { SetValue(propertyIsSelected, value); }
        }

        public bool HasExternalSubtitles
        {
            get { return (bool)GetValue(HasExternalSubtitlesProperty); }
            set { SetValue(HasExternalSubtitlesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasExternalSubtitles.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasExternalSubtitlesProperty =
            DependencyProperty.Register("HasExternalSubtitles", typeof(bool), typeof(DirInfo), new UIPropertyMetadata(false));

        public bool HasMovieInfo
        {
            get { return (bool)GetValue(HasMovieInfoProperty); }
            set { SetValue(HasMovieInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasMovieInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasMovieInfoProperty =
            DependencyProperty.Register("HasMovieInfo", typeof(bool), typeof(DirInfo), new UIPropertyMetadata(false));

        public bool HasMoviesheet
        {
            get { return (bool)GetValue(HasMoviesheetProperty); }
            set { SetValue(HasMoviesheetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasMoviesheet.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasMoviesheetProperty =
            DependencyProperty.Register("HasMoviesheet", typeof(bool), typeof(DirInfo), new UIPropertyMetadata(false));



        public bool HasMoviesheetMetadata
        {
            get { return (bool)GetValue(HasMoviesheetMetadataProperty); }
            set { SetValue(HasMoviesheetMetadataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasMoviesheetMetadata.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasMoviesheetMetadataProperty =
            DependencyProperty.Register("HasMoviesheetMetadata", typeof(bool), typeof(DirInfo), new UIPropertyMetadata(false));



        #endregion

        #region // .ctor(s)
        public DirInfo()
        {
            IsDummy = false;
            Parent = null;
            SubDirectories = new ObservableCollection<DirInfo>();
            SubDirectories.Add(new DirInfo(this));

        }

        private DirInfo(DirInfo parent)
        {
            Parent = parent;
            IsDummy = true;
            SubDirectories = new ObservableCollection<DirInfo>();
        }

        public DirInfo(string directoryName, DirInfo parent)
            : this()
        {
            Name = directoryName;
            Parent = parent;
        }

        public DirInfo(DirectoryInfo dir, DirInfo parent)
            : this()
        {
            Parent = parent;
            Name = dir.Name;
            Root = dir.Root.Name;
            Path = dir.FullName;
            DirType = ObjectType.Directory;
            SetImage();
        }

        public DirInfo(FileInfo fileobj, DirInfo parent, bool isFilterActive)
            : this()
        {
            Parent = parent;
            Name = fileobj.Name;
            Path = fileobj.FullName;
            Root = parent.Root;
            DirType = ObjectType.File;
            Size = Helpers.GetFormattedFileSize(fileobj.Length); //(fileobj.Length / 1024).ToString() + " KB";
            Ext = fileobj.Extension + " File";
            SubDirectories.Clear();
            SetImage();
            if (FileManager.Configuration.Options.FileBrowserOptions.ShowHasExternalSubtitles || isFilterActive)
            {
                SetHasSubtitles(isFilterActive);
            }

            if (FileManager.Configuration.Options.FileBrowserOptions.ShowHasMovieInfo || isFilterActive)
            {
                SetHasMovieInfo(isFilterActive);
            }

            if (FileManager.Configuration.Options.FileBrowserOptions.ShowHasMoviesheet || isFilterActive)
            {
                SetHasMoviesheet(isFilterActive);
            }

            if (FileManager.Configuration.Options.FileBrowserOptions.ShowHasMoviesheetMetadata)
            {
                SetHasMoviesheetMetadata(isFilterActive);
            }
        }

        public DirInfo(DriveInfo driveobj)
            : this()
        {
            if (driveobj.Name.EndsWith(@"\"))
                Name = driveobj.Name.Substring(0, driveobj.Name.Length - 1);
            else
                Name = driveobj.Name;

            Path = driveobj.Name;
            DirType = ObjectType.DiskDrive;
            //it's way too slow for floppy drives.......
            //try
            //{
            //    if (driveobj.IsReady)
            //    {
            //        Ext = string.Format("[ {0} ]", driveobj.VolumeLabel);
            //    }
            //}
            //catch { }
            SetImage();
        }
        #endregion

        public void ClearChildren()
        {
            this.IsLoaded = false;
            this.LoadDirectories(new List<DirInfo>());
        }

        private void SetImage()
        {
            ImgSource = GetThumbnailPath();
        }

        private void SetHasSubtitles(bool isFilterActive)
        {
            if (isFilterActive)
            {
                this.HasExternalSubtitles = MediaInfoManager.HasExternalSubtitles(this.Path);
            }
            else
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    this.HasExternalSubtitles = MediaInfoManager.HasExternalSubtitles(this.Path);
                }, DispatcherPriority.ApplicationIdle);
        }

        private void SetHasMovieInfo(bool isFilterActive)
        {
            if (isFilterActive)
            {
                this.HasMovieInfo = nfoHelper.HasMovieInfoFile(this.Path);
            }
            else
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    this.HasMovieInfo = nfoHelper.HasMovieInfoFile(this.Path);
                }, DispatcherPriority.ApplicationIdle);
        }

        private void SetHasMoviesheet(bool isFilterActive)
        {
            if (isFilterActive)
            {
                this.HasMoviesheet = FileManager.Configuration.HasMoviesheet(this.Path);
            }
            else
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    this.HasMoviesheet = FileManager.Configuration.HasMoviesheet(this.Path);
                }, isFilterActive ? DispatcherPriority.Send : DispatcherPriority.ApplicationIdle);
        }

        private void SetHasMoviesheetMetadata(bool isFilterActive)
        {
            if (isFilterActive)
            {
                this.HasMoviesheetMetadata = FileManager.Configuration.HasMoviesheetMetadata(this.Path);
            }
            else
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    this.HasMoviesheetMetadata = FileManager.Configuration.HasMoviesheetMetadata(this.Path);
                }, isFilterActive ? DispatcherPriority.Send : DispatcherPriority.ApplicationIdle);
        }

        private string GetThumbnailPath()
        {
            string _result = null;
            switch (this.DirType)
            {
                case ObjectType.Directory:
                    _result = Helpers.GetCorrectThumbnailPath(this.Path, true, false);
                    _result = !string.IsNullOrEmpty(_result) && File.Exists(_result) ? _result : "/Images/folder.png";
                    break;
                case ObjectType.DiskDrive:
                    _result = "/Images/diskdrive.png";
                    break;
                case ObjectType.File:
                    _result = Helpers.GetCorrectThumbnailPath(this.Path, false);
                    _result = !string.IsNullOrEmpty(_result) && File.Exists(_result) ? _result : "/Images/file.png";
                    break;
                case ObjectType.MyComputer:
                    _result = "/Images/mycomputer.png";
                    break;
            }
            return _result;
        }

        public void LoadDirectories(IEnumerable<DirInfo> children)
        {
            if (!IsLoaded)
            {
                SubDirectories.Clear();
                foreach (DirInfo _child in children)
                {
                    _child.Parent = this;
                    //_child.SetIsChecked(_child.IsChecked2, this.IsChecked2, true, false);
                    SubDirectories.Add(_child);
                }
                //SubDirectories = children.ToList<DirInfo>();
            }
            IsLoaded = true;

        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(ImgSource) && File.Exists(ImgSource))
            {
                ImgSource = null;
            }
        }

        #endregion
    }
}
