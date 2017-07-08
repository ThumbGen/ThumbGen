using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Reflection;
using System.Web;

namespace ThumbGen
{
    public static class IMDbCountries
    {
        public static Dictionary<string, Country> Countries = new Dictionary<string, Country>()
            { 
                {"com", Country.International},
                {"de", Country.Germany},
                {"es", Country.Spain},
                {"it", Country.Italy},
                {"fr", Country.France},
                {"pt", Country.Portugal}
            };
    }

    internal  class IMDBCountryFactory
    {
        private XmlDocument m_Doc = new XmlDocument();
        private XmlElement m_CountryNode = null;

        private Encoding m_Encoding = Encoding.Default;
        public Encoding GetEncoding
        {
            get 
            {
                if (m_Encoding == Encoding.Default)
                {
                    Encoding _result = Encoding.UTF8;
                    string _e = m_CountryNode.GetAttribute("Charset");
                    if (!string.IsNullOrEmpty(_e))
                    {
                        try
                        {
                            _result = Encoding.GetEncoding(_e);
                        }
                        catch { }
                    }
                    m_Encoding = _result;
                }
                return m_Encoding;
            }
        }

        public string RE_ReleaseDateFast { get { return HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(m_CountryNode, "ReleaseDateFast")); } }
        public string RE_ReleaseDate { get { return HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(m_CountryNode, "ReleaseDate")); } }
        
        public string RE_Companies { get { return (Helpers.GetValueFromXmlNode(m_CountryNode, "Company")); } }
        public string RE_Plot { get { return (Helpers.GetValueFromXmlNode(m_CountryNode, "Plot")); } }
        public string RE_Overview { get { return HttpUtility.HtmlDecode(Helpers.GetValueFromXmlNode(m_CountryNode, "Overview")); } }
        public string RE_Genres { get { return (Helpers.GetValueFromXmlNode(m_CountryNode, "Genres")); } }
        public string RE_Directors { get { return (Helpers.GetValueFromXmlNode(m_CountryNode, "Director")); } }
        public string RE_Countries { get { return (Helpers.GetValueFromXmlNode(m_CountryNode, "Country")); } }

        public string Popular { get { return (Helpers.GetValueFromXmlNode(m_CountryNode, "Popular")); } }
        public string Exact { get { return (Helpers.GetValueFromXmlNode(m_CountryNode, "Exact")); } }
        public  string RE_Exact
        {
            get
            {
                return this.Exact.Replace("(", @"\(").Replace(")", @"\)");
            }
        }

        public string Approx { get { return (Helpers.GetValueFromXmlNode(m_CountryNode, "Approx")); } }
        public  string RE_Approx
        {
            get
            {
                return this.Approx.Replace("(", @"\(").Replace(")", @"\)");
            }
        }

        public string Partial { get { return (Helpers.GetValueFromXmlNode(m_CountryNode, "Partial")); } }
        public  string RE_Partial
        {
            get
            {
                return this.Partial.Replace("(", @"\(").Replace(")", @"\)");
            }
        }

        public string Language
        {
            get
            {
                return (Helpers.GetValueFromXmlNode(m_CountryNode, "Language"));
            }
        }

        public string LanguageCode { get; private set; }
        public string TargetHost
        {
            get
            {
                //return string.Format("http://www.imdb.{0}", LanguageCode);
                return m_CountryNode.GetAttribute("Host");
            }
        }

        public IMDBCountryFactory(string langCode)
        {
            LanguageCode = langCode;
            m_Doc.Load(Assembly.GetEntryAssembly().GetManifestResourceStream("ThumbGen.Collectors.imdbcountries.xml"));
            m_CountryNode = m_Doc.SelectSingleNode(string.Format("//Country[@Code='{0}']", langCode)) as XmlElement;
            if (m_CountryNode == null)
            {
                m_CountryNode = m_Doc.SelectSingleNode("//Country[@Code='com']") as XmlElement;
            }
        }
    }


}
