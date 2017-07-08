using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace ThumbGen
{
    public abstract class ProfileItemBase : INotifyPropertyChanged
    {
        private string m_ProfilePath;
        public string ProfilePath 
        {
            get
            {
                return m_ProfilePath;
            }
            set
            {
                m_ProfilePath = value;
                NotifyPropertyChanged("ProfilePath");
            }
        }

        private string m_ProfileName;
        public string ProfileName 
        {
            get
            {
                return m_ProfileName;
            }
            set
            {
                m_ProfileName = value;
                NotifyPropertyChanged("ProfileName");
            }
        }

        private bool m_IsSelected;
        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }
            set
            {
                m_IsSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
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

    public class ProfileItem : ProfileItemBase
    {
        public ProfileItem()
            : this(null, null)
        {
        }

        public ProfileItem(string profilePath, string profileName)
        {
            ProfilePath = profilePath;
            ProfileName = profileName;
        }
    }
}
