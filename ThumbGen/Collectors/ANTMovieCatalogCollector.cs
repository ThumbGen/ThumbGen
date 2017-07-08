using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.ANTMOVIECATALOG)]
    internal class ANTMovieCatalogCollector : XMLImportCollectorBase
    {
        public override string CollectorName
        {
            get
            {
                return BaseCollector.ANTMOVIECATALOG;
            }
        }

        protected override string XSLPath
        {
            get
            {
                //return @"d:\Work\ANT\ant_import.xslt";
                return "ant_import.xslt";
            }
            
        }

        protected override string OriginalXMLPath
        {
            get
            {
                return FileManager.Configuration.Options.ImportOptions.ANTXML;
            }
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            UseCustomSearch = FileManager.Configuration.Options.ImportOptions.ANTUseCustomSearch;
            CustomSearchRegex = FileManager.Configuration.Options.ImportOptions.ANTCustomSearchRegex;
            
            return base.GetResults(keywords, imdbID, skipImages);
        }

        public ANTMovieCatalogCollector():base()
        {

            //Binding _b = new Binding("ANTXML");
            //_b.Source = FileManager.Configuration.Options.ImportOptions;
            //_b.Converter = new XML2BoolConverter();
            //BindingOperations.SetBinding(this, BaseCollector.IsEnabledProperty, _b);
        }
    }
}
