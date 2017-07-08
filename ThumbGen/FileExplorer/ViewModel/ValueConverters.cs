using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.IO;
using FileExplorer.Model;
using System.Windows;

namespace FileExplorer.ViewModel
{
    public class GetFileSysemInformationConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try {
                DirInfo nodeToExpand = value as DirInfo;
                if (nodeToExpand == null)
                    return null;

                 //return the subdirectories of the Current Node
                if (nodeToExpand.IsLoaded)
                {
                    return nodeToExpand.SubDirectories;
                }
                else
                {
                    if ((ObjectType)nodeToExpand.DirType == ObjectType.MyComputer)
                    {
                        return (from sd in FileSystemExplorerService.GetRootDirectories()
                                select new DirInfo(sd)).ToList();
                    }
                    else
                    {
                        IList<DirInfo> _tmp =
                            (from dirs in FileSystemExplorerService.GetChildDirectories(nodeToExpand.Path)
                                select new DirInfo(dirs, nodeToExpand)).ToList();
                        foreach (DirInfo _dir in _tmp)
                        {
                            nodeToExpand.SubDirectories.Add(_dir);
                        }
                        return _tmp;
                    }
                }
            }
            catch { return null; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class ControlVisibilityConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value == true)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
