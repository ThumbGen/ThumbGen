using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace ThumbGen.MovieSheets
{
    public class TemplateItem : INotifyPropertyChanged
    {
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

        public string TemplatePath { get; set; }
        public string TemplateName
        {
            get
            {
                return GetTemplateName(TemplatePath);
            }
        }
        
        public static string GetTemplateName(string templatePath)
        {
            return !string.IsNullOrEmpty(templatePath) ? Path.GetFileNameWithoutExtension(Path.GetDirectoryName(templatePath)) : string.Empty;
        }

        public TemplateItem() : this(null)
        {
        }

        public TemplateItem(string templatePath)
        {
            TemplatePath = templatePath;
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
