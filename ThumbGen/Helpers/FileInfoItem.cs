using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace ThumbGen
{
    public class FileInfoItem : INotifyPropertyChanged
    {
        public bool IsSelected { get; set; }

        FileInfo m_FileInfo;
        public FileInfo FileInfo
        {
            get
            {
                return m_FileInfo;
            }
            set
            {
                m_FileInfo = value;
                NotifyPropertyChanged("FileInfo");
            }
        }


        public FileInfoItem(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion
    }

    public enum MovieInfoProviderItemType
    {
        CurrentCollector,
        IMDB,
        MyOwn,
        Metadata,
        PrefCollector
    }

    public class MovieInfoProviderItem : FrameworkElement, INotifyPropertyChanged
    {
        public bool IsSelected { get; set; }

        string m_ProviderName;
        public string ProviderName
        {
            get
            {
                return m_ProviderName;
            }
            set
            {
                m_ProviderName = value;
                NotifyPropertyChanged("ProviderName");
            }
        }

        MovieInfoProviderItemType m_MovieInfoProviderItemType;
        public MovieInfoProviderItemType MovieInfoProviderItemType
        {
            get
            {
                return m_MovieInfoProviderItemType;
            }
            set
            {
                m_MovieInfoProviderItemType = value;
                NotifyPropertyChanged("MovieInfoProviderItemType");
            }
        }
        
        FileInfo m_FileInfo;
        public FileInfo FileInfo
        {
            get
            {
                return m_FileInfo;
            }
            set
            {
                m_FileInfo = value;
                NotifyPropertyChanged("FileInfo");
            }
        }

        public MovieInfo MovieInfo
        {
            get { return (MovieInfo)GetValue(MovieInfoProperty); }
            set { SetValue(MovieInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MovieInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MovieInfoProperty =
            DependencyProperty.Register("MovieInfo", typeof(MovieInfo), typeof(MovieInfoProviderItem), new UIPropertyMetadata(null));


        public MovieInfoProviderItem()
        {

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion
    }
}
