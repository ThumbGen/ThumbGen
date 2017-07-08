using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.COLLECTORZMOVIE)]
    internal class CollectorzComCollector : XMLImportCollectorBase
    {
        public override string CollectorName
        {
            get
            {
                return BaseCollector.COLLECTORZMOVIE;
            }
        }

        protected override string XSLPath
        {
            get
            {
                //return @"D:\Work\Collectorz\collectorz_import.xslt";
                return "collectorz_import.xslt";
            }

        }

        protected override string OriginalXMLPath
        {
            get
            {
                return FileManager.Configuration.Options.ImportOptions.CollectorzXML;
            }
        }

        public CollectorzComCollector() : base()
        {
            //Binding _b = new Binding("CollectorzXML");
            //_b.Source = FileManager.Configuration.Options.ImportOptions;
            //_b.Converter = new XML2BoolConverter();
            //BindingOperations.SetBinding(this, BaseCollector.IsEnabledProperty, _b);
        }
    }
}
