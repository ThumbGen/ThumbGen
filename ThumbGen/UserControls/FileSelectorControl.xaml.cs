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
using Microsoft.Win32;
using System.Windows.Forms;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for FileSelectorControl.xaml
    /// </summary>
    public partial class FileSelectorControl : System.Windows.Controls.UserControl
    {

        public EventHandler<FileChangedEventArgs> SelectedFileChanged { get; set; }

        public bool AllowFolder
        {
            get { return (bool)GetValue(AllowFolderProperty); }
            set { SetValue(AllowFolderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowFolder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowFolderProperty =
            DependencyProperty.Register("AllowFolder", typeof(bool), typeof(FileSelectorControl), new UIPropertyMetadata(false));

        

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Label.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(FileSelectorControl), new UIPropertyMetadata("File"));


        public string Filepath
        {
            get { return (string)GetValue(FilepathProperty); }
            set { SetValue(FilepathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Filepath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilepathProperty =
            DependencyProperty.Register("Filepath", typeof(string), typeof(FileSelectorControl), 
                new UIPropertyMetadata(""));

        public string Filter
        {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Filter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(string), typeof(FileSelectorControl), new UIPropertyMetadata(""));



        public FileSelectorControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog _ofd = new Microsoft.Win32.OpenFileDialog();
            _ofd.Filter = Filter;
            _ofd.Title = "Select file";
            _ofd.InitialDirectory = string.IsNullOrEmpty(Filepath) ? null : System.IO.Path.GetDirectoryName(Filepath);
            if ((bool)_ofd.ShowDialog())
            {
                this.Filepath = _ofd.FileName;
                TriggerSelectedFileChanged();
            }

        }


        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {

            FolderBrowserDialog _fbd = new FolderBrowserDialog();
            _fbd.RootFolder = Environment.SpecialFolder.MyComputer;
            _fbd.ShowNewFolderButton = true;
            _fbd.Description = "Select folder";
            if (_fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.Filepath = _fbd.SelectedPath;
                TriggerSelectedFileChanged();
            }
        }

        private void TriggerSelectedFileChanged()
        {
            if (SelectedFileChanged != null)
            {
                SelectedFileChanged(this, new FileChangedEventArgs(this.Filepath));
            }
        }
    }

    public class FileChangedEventArgs : EventArgs
    {
        public string NewFilePath { get; set; }

        public FileChangedEventArgs(string newFile)
        {
            NewFilePath = newFile;
        }
    }
}
