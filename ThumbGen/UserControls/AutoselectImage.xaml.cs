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

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for AutoselectImage.xaml
    /// </summary>
    public partial class AutoselectImage : UserControl
    {

        public bool Autoselect
        {
            get { return (bool)GetValue(AutoselectProperty); }
            set { SetValue(AutoselectProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Autoselect.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoselectProperty =
            DependencyProperty.Register("Autoselect", typeof(bool), typeof(AutoselectImage), new UIPropertyMetadata(false));

        public string Filename
        {
            get { return (string)GetValue(FilenameProperty); }
            set { SetValue(FilenameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Filename.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilenameProperty =
            DependencyProperty.Register("Filename", typeof(string), typeof(AutoselectImage), new UIPropertyMetadata(string.Empty));

        public string PrefixText
        {
            get { return (string)GetValue(PrefixTextProperty); }
            set { SetValue(PrefixTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PrefixText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PrefixTextProperty =
            DependencyProperty.Register("PrefixText", typeof(string), typeof(AutoselectImage), new UIPropertyMetadata(string.Empty));


        public string TargetText
        {
            get { return (string)GetValue(TargetTextProperty); }
            set { SetValue(TargetTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TargetText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetTextProperty =
            DependencyProperty.Register("TargetText", typeof(string), typeof(AutoselectImage), new UIPropertyMetadata(string.Empty));

        public bool IsExtensionVisible
        {
            get { return (bool)GetValue(IsExtensionVisibleProperty); }
            set { SetValue(IsExtensionVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsExtensionVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsExtensionVisibleProperty =
            DependencyProperty.Register("IsExtensionVisible", typeof(bool), typeof(AutoselectImage), new UIPropertyMetadata(false));

        

        public string Extension
        {
            get { return (string)GetValue(ExtensionProperty); }
            set { SetValue(ExtensionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Extension.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExtensionProperty =
            DependencyProperty.Register("Extension", typeof(string), typeof(AutoselectImage), new UIPropertyMetadata(".jpg"));

        


        public AutoselectImage()
        {
            InitializeComponent();
            TheStackPanel.DataContext = this;
        }
    }
}
