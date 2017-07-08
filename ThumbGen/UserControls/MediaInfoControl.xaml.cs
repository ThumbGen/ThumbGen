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
using MediaInfoLib;
using FileExplorer.ViewModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for MediaInfoControl.xaml
    /// </summary>
    public partial class MediaInfoControl : UserControl,IWeakEventListener
    {
        public static string MEDIA_INFO_OFF = "Media info is switched off. Use 'Options/File Browser/Show Media Info' to enable it";

        public MediaInfoControl()
        {
            InitializeComponent();
        }



        public bool AllowEditing
        {
            get { return (bool)GetValue(AllowEditingProperty); }
            set { SetValue(AllowEditingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowEditing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowEditingProperty =
            DependencyProperty.Register("AllowEditing", typeof(bool), typeof(MediaInfoControl), new UIPropertyMetadata(false));



        public MediaInfoData MediaData
        {
            get { return (MediaInfoData)GetValue(MediaDataProperty); }
            set { SetValue(MediaDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MediaData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaDataProperty =
            DependencyProperty.Register("MediaData", typeof(MediaInfoData), typeof(MediaInfoControl), new UIPropertyMetadata(null));

        public string TextContent
        {
            get { return (string)GetValue(TextContentProperty); }
            set { SetValue(TextContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextContentProperty =
            DependencyProperty.Register("TextContent", typeof(string), typeof(MediaInfoControl), new UIPropertyMetadata(null));

        private void OnCurrentDirectoryChanged(object sender, EventArgs args)
        {
            TextContent = null;

            if (FileManager.Configuration.Options.FileBrowserOptions.ShowMediaInfo)
            {
                MediaData = new MediaInfoData();

                DirInfo _dir = (DataContext as ExplorerWindowViewModel).CurrentDirectory;
                if (_dir.DirType == ObjectType.File)
                {
                    Refresh(_dir.Path);
                }
            }
            else
            {
                TextContent = MEDIA_INFO_OFF;
                MediaData = null;
            }
        }

        private void TheControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ExplorerWindowViewModel _mod = (e.NewValue as ExplorerWindowViewModel);
            if (_mod != null)
            {
                //PropertyChangedEventManager.AddListener(_mod, this, "CurrentDirectoryProperty");
                _mod.PropertyChanged += OnCurrentDirectoryChanged;
            }
        }

        private void Refresh(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    string _tmp = null;
                    MediaData = MediaInfoManager.GetMediaInfoData(fileName, true, false, true, out _tmp);
                    TextContent = _tmp;
                }, DispatcherPriority.ApplicationIdle);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            string _fileName = null;

            if (DataContext is ExplorerWindowViewModel)
            {
                DirInfo _dir = (DataContext as ExplorerWindowViewModel).CurrentDirectory;
                if (_dir.DirType == ObjectType.File)
                {
                    _fileName = _dir.Path;
                }
            }
            if (DataContext is string)
            {
                _fileName = DataContext as string;
            }

            Refresh(_fileName);
        }


        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(PropertyChangedEventManager))
            {
                var args = e as PropertyChangedEventArgs;
                if (sender == this)
                {
                    //if (args.PropertyName == MyModel.ValueProperty)
                    //{

                    //}

                    return true;
                }
            }
            return false;
        }
    }
}
