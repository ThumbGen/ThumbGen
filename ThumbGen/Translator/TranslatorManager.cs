using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ThumbGen.Translator
{
    public class TranslatorManager
    {
        private static Dictionary<string, string> m_Cache = new Dictionary<string, string>();

        public string Translate(string text)
        {
            return Translate(text, "auto", "en");
        }

        public string Translate(string text, string from, string to)
        {
            string _result = text;

            if (!string.IsNullOrEmpty(text))
            {
                string _key = text.Trim().ToLowerInvariant();
                if (m_Cache.ContainsKey(_key))
                {
                    _result = m_Cache[_key];
                }
                else
                {
                    string _detectedLanguageCode = string.Empty;
                    string _temp = GoogleTranslator.TranslateText(text, from, to, out _detectedLanguageCode);
                    _result = string.IsNullOrEmpty(_temp) ? text : _temp;
                    m_Cache.Add(_key, _result);
                }
            }
            return _result;
        }

        public void ClearCache()
        {
            m_Cache.Clear();
        }
        
        public TranslatorManager()
        {

        }
    }

    public static class GoogleTranslator
    {
        public static string GetLanguagePair(string from, string to)
        {
            return string.Format("{0}-{1}", from, to);
        }

        public static string TranslateText(string input, string from, string to, out string detectedLanguageCode)
        {
            detectedLanguageCode = string.Empty;
            try
            {
                string _url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", input.Replace(' ', '+'), GetLanguagePair(from, to));
                //WebClient webClient = new WebClient();
                //webClient.Encoding = System.Text.Encoding.UTF8;
                //string result = webClient.DownloadString(url);
                string _result = Helpers.GetPage(_url);

                //Match _match = Regex.Match(_result, "<span title=\"[^>]+onmouseover[^>]+>(?<Word>[^<]+)</span", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                //Match _match = Regex.Match(_result, string.Format("<input type=hidden name=langpair value=\"(?<Lang>[^\\|]+)\\|{0}\"><input type=hidden name=gtrans value=\"(?<Word>[^\"]+)\">", to), RegexOptions.Singleline | RegexOptions.IgnoreCase);
                Match _match = Regex.Match(_result, "onmouseout=\"this\\.style\\.backgroundColor='#fff'\">(?<Word>[^<]+)</span>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (_match.Success)
                {
                    try
                    {
                        detectedLanguageCode = _match.Groups["Lang"].Value.Trim();
                    }
                    catch
                    { detectedLanguageCode = string.Empty; }

                    return _match.Groups["Word"].Value.Trim();
                }
                else
                {
                    return input;
                }
            }
            catch (Exception ex)
            {
                Loggy.Logger.DebugException("Gtranslate", ex);
                return input;
            }

            //result = result.Substring(result.IndexOf("<span title=\"") + "<span title=\"".Length);
            //result = result.Substring(result.IndexOf(">") + 1);
            //result = result.Substring(0, result.IndexOf("</span>"));
            //return result.Trim();
        }

    }
}
