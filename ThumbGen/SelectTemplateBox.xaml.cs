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
using System.Windows.Shapes;
using ThumbGen.MovieSheets;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for SelectTemplateBox.xaml
    /// </summary>
    public partial class SelectTemplateBox : Window
    {
        public SelectTemplateBox()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += delegate { DragMove(); };
            this.Loaded += new RoutedEventHandler(SelectTemplateBox_Loaded);
        }

        void SelectTemplateBox_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.TemplateSelector.TemplatesMan.RefreshTemplates(FileManager.Configuration.Options.MovieSheetsOptions.TemplateName);
                this.TemplateSelector.TemplatesCombobox.SelectedValue = this.TemplateSelector.TemplatesMan.SelectedTemplate;

                this.TemplateSelectorExtra.TemplatesMan.RefreshTemplates(FileManager.Configuration.Options.MovieSheetsOptions.ExtraTemplateName);
                this.TemplateSelectorExtra.TemplatesCombobox.SelectedValue = this.TemplateSelectorExtra.TemplatesMan.SelectedTemplate;

                this.TemplateSelectorParentFolder.TemplatesMan.RefreshTemplates(FileManager.Configuration.Options.MovieSheetsOptions.ParentFolderTemplateName);
                this.TemplateSelectorParentFolder.TemplatesCombobox.SelectedValue = this.TemplateSelectorParentFolder.TemplatesMan.SelectedTemplate;
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("selecttemplatebox_loaded", ex);
            }
        }

        public static bool Show(Window owner)
        {
            bool _result = false;

            SelectTemplateBox _box = new SelectTemplateBox();
            _box.Owner = owner;
            _box.WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
            var res = _box.ShowDialog();
            if (res.HasValue && res.Value)
            {
                // remember selected templates
                FileManager.Configuration.Options.MovieSheetsOptions.TemplateName = (_box.TemplateSelector.TemplatesCombobox.SelectedItem as TemplateItem).TemplateName;
                FileManager.Configuration.Options.MovieSheetsOptions.ExtraTemplateName = (_box.TemplateSelectorExtra.TemplatesCombobox.SelectedItem as TemplateItem).TemplateName;
                FileManager.Configuration.Options.MovieSheetsOptions.ParentFolderTemplateName = (_box.TemplateSelectorParentFolder.TemplatesCombobox.SelectedItem as TemplateItem).TemplateName;
                
                _result = true;
            }

            return _result;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            MoviesheetsUpdateManager.SelectedTemplates.Clear();

            // add first template to the Updater's templates list
            MoviesheetsUpdateManager.SelectedTemplates.Add(TemplateSelector.TemplatesCombobox.SelectedItem as TemplateItem);

            // add second template to the Updater's templates list
            MoviesheetsUpdateManager.SelectedTemplates.Add(TemplateSelectorExtra.TemplatesCombobox.SelectedItem as TemplateItem);

            // add third template to the Updater's templates list
            MoviesheetsUpdateManager.SelectedTemplates.Add(TemplateSelectorParentFolder.TemplatesCombobox.SelectedItem as TemplateItem);

            this.DialogResult = true;
        }
    }
}
