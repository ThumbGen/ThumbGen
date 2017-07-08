using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace ThumbGen
{
    public class HyperlinkController: DependencyObject
    {


        public static bool GetNavigatesToUrl(DependencyObject obj)
        {
            return (bool)obj.GetValue(NavigatesToUrlProperty);
        }

        public static void SetNavigatesToUrl(DependencyObject obj, bool value)
        {
            obj.SetValue(NavigatesToUrlProperty, value);
        }

        // Using a DependencyProperty as the backing store for NavigatesToUrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NavigatesToUrlProperty =
            DependencyProperty.RegisterAttached("NavigatesToUrl", typeof(bool), typeof(HyperlinkController),
                new UIPropertyMetadata(false, OnNavigatesToUrlPropertyChanged));

        private static void OnNavigatesToUrlPropertyChanged(DependencyObject obj,  DependencyPropertyChangedEventArgs args)
        {
            Hyperlink _link = obj as Hyperlink;
            if (_link != null)
            {
                _link.Click += new RoutedEventHandler(link_Click);
            }
        }

        static void link_Click(object sender, RoutedEventArgs e)
        {
            Helpers.OpenUrlInBrowser((sender as Hyperlink).NavigateUri.OriginalString);
        }
    }
}
