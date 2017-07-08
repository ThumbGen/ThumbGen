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
using ThumbGen.MovieSheets;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for TemplateSelectorControl.xaml
    /// </summary>
    public partial class TemplateSelectorControl : UserControl
    {
        public TemplateSelectorControl()
        {
            InitializeComponent();

            TemplatesMan = new TemplatesManager();
        }



        public bool IsMainTemplateSelector
        {
            get { return (bool)GetValue(IsMainTemplateSelectorProperty); }
            set { SetValue(IsMainTemplateSelectorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMainTemplateSelector.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMainTemplateSelectorProperty =
            DependencyProperty.Register("IsMainTemplateSelector", typeof(bool), typeof(TemplateSelectorControl), new UIPropertyMetadata(true, OnIsMainTemplateSelectorChanged));


        public static void OnIsMainTemplateSelectorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            
        }

        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LabelText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register("LabelText", typeof(string), typeof(TemplateSelectorControl), new UIPropertyMetadata("Select template:"));



        public bool ShowTvixieButton
        {
            get { return (bool)GetValue(ShowTvixieButtonProperty); }
            set { SetValue(ShowTvixieButtonProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowTvixieButton.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowTvixieButtonProperty =
            DependencyProperty.Register("ShowTvixieButton", typeof(bool), typeof(TemplateSelectorControl), new UIPropertyMetadata(false));




        public TemplatesManager TemplatesMan
        {
            get { return (TemplatesManager)GetValue(TemplatesManProperty); }
            set { SetValue(TemplatesManProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TemplatesMan.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TemplatesManProperty =
            DependencyProperty.Register("TemplatesMan", typeof(TemplatesManager), typeof(TemplateSelectorControl), new UIPropertyMetadata(null));

        public event EventHandler<SelectionChangedEventArgs> TemplatesSelectionChanged;

        public ComboBox TemplatesCombobox
        {
            get
            {
                return this.TemplatesCombo;
            }
        }

        public void TemplatesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplatesSelectionChanged != null)
            {
                TemplatesSelectionChanged(this, e);
            }
        }

        private void TemplatesRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            TemplatesMan.RefreshTemplates(this.TemplatesCombo.Text);
        }

        private void GotoTvixie_Click(object sender, RoutedEventArgs e)
        {
            Helpers.OpenUrlInBrowser("http://www.wdtvlive.net");
        }
    }
}
