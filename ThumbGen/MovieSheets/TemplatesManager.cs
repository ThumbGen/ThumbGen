using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace ThumbGen.MovieSheets
{
    public class TemplatesManager: DependencyObject
    {
        public ObservableCollection<TemplateItem> Templates { get; private set; }



        public TemplateItem SelectedTemplate
        {
            get { return (TemplateItem)GetValue(SelectedTemplateProperty); }
            set { SetValue(SelectedTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedTemplateProperty =
            DependencyProperty.Register("SelectedTemplate", typeof(TemplateItem), typeof(TemplatesManager), new UIPropertyMetadata(null));



        public TemplatesManager()
        {
            Templates = new ObservableCollection<TemplateItem>();
        }

        public TemplateItem GetTemplateItem(string templateName)
        {
            TemplateItem _result = null;

            IEnumerable<TemplateItem> _res = from c in Templates
                                             where !string.IsNullOrEmpty(c.TemplateName) &&
                                                   !string.IsNullOrEmpty(templateName) &&
                                                   c.TemplateName.ToLowerInvariant() == templateName.ToLowerInvariant()
                                             select c as TemplateItem;
            _result = _res != null && _res.Count() != 0 ? _res.First() : null;
            return _result;
        }

        public void RefreshTemplates()
        {
            RefreshTemplates(null);
        }

        public void RefreshTemplates(string selectedTemplateName)
        {
            TemplateItem _selectedTemplate = null;
            try
            {
                Templates.Clear();

                string _templatesFolder = FileManager.GetTemplatesFolder();
                if (Directory.Exists(_templatesFolder))
                {
                    string[] _templates = Directory.GetFiles(_templatesFolder, "template.xml", SearchOption.AllDirectories);
                    if (_templates != null && _templates.Count() != 0)
                    {
                        Array.Sort(_templates, new Comparison<string>(delegate(string d1, string d2)
                            {
                                return string.Compare(TemplateItem.GetTemplateName(d1), TemplateItem.GetTemplateName(d2), true);
                            }));

                        foreach (string _templatePath in _templates)
                        {
                            TemplateItem _template = new TemplateItem(_templatePath);
                            Templates.Add(_template);
                            if (!string.IsNullOrEmpty(selectedTemplateName) &&
                                 string.Compare(_template.TemplateName.ToLowerInvariant(), selectedTemplateName.ToLowerInvariant()) == 0)
                            {
                                _selectedTemplate = _template;
                            }
                        }

                        // select first available template by default if nothing selected already
                        if (_selectedTemplate == null && Templates.Count != 0)
                        {
                            _selectedTemplate = Templates[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Refresh templates", ex);
            }
            if (_selectedTemplate != null)
            {
                _selectedTemplate.IsSelected = true;
                SelectedTemplate = _selectedTemplate;
                Helpers.DoEvents();
            }
        }
    }
}
