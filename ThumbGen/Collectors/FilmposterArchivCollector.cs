using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Web;
using System.Threading;
using System.Windows;
using System.Text.RegularExpressions;

namespace ThumbGen
{
    [MovieCollector(BaseCollector.FILMPOSTERARCHIV)]
    internal class FilmposterArchivCollector : BaseCollector
    {
        public FilmposterArchivCollector()
        {
        }

        public override Country Country
        {
            get { return Country.Germany; }
        }

        public override string Host
        {
            get { return "http://www.filmposter-archiv.de"; }
        }

        public override string CollectorName
        {
            get { return FILMPOSTERARCHIV; }
        }

        protected override string SearchListRegex
        {
            get
            {
                return "<a\\sclass=\"thumbnail\"\\shref=\"(?<RelLink>filmplakat.php\\?id=(?<ID>[0-9]+))\"[^>]*>(?<Title>[^<]+)";
            }
        }

        protected override string CoverRegex
        {
            get
            {
                return "<img\\ssrc=\"(?<Cover>filmplakat[^\"]+)";
            }
        }

        private bool ProcessPage(string input, string id, bool skipImages, string link, string title, string imageId)
        {
            bool _result = false;

            if (!string.IsNullOrEmpty(input))
            {

                string _imageUrl = string.Format("{0}/{1}", Host, GetItem(input, CoverRegex, "Cover", RegexOptions.Singleline | RegexOptions.IgnoreCase));
                string _s = Helpers.GetPage(_imageUrl, null, Encoding.Default, "", true, false);
                    
                ResultMovieItem _movieItem = new ResultMovieItem(id, title, _imageUrl, this.CollectorName);
                _movieItem.CollectorMovieUrl = link;
                _movieItem.ImageId = imageId;
                _movieItem.DataQuerying = new EventHandler(CacheImage);
                _movieItem.DataReadyEvent = new ManualResetEvent(false);
                ResultsList.Add(_movieItem);
                _result = true;
            }

            return _result;
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            string _resultsPage = Helpers.GetPage(string.Format("{0}/suche.php?&filmtitel={1}&productSearch=productSearch&sent=1", Host, keywords), null, Encoding.GetEncoding("iso-8859-1"), "", false, false);
            if (!string.IsNullOrEmpty(_resultsPage))
            {
                Regex _reg = new Regex(SearchListRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (_reg.IsMatch(_resultsPage))
                {
                    // we got the results page
                    foreach (Match _match in _reg.Matches(_resultsPage))
                    {
                        string _relLink = string.Format("{0}/{1}", Host, _match.Groups["RelLink"].Value);
                        string _title = HttpUtility.HtmlDecode(_match.Groups["Title"].Value);
                        string _id = _title;
                        string _pageId = _match.Groups["ID"].Value;

                        if (FileManager.CancellationPending)
                        {
                            return ResultsList.Count != 0;
                        }

                        string _page = Helpers.GetPage(string.Format("{0}?id={1}", _relLink, _pageId), null, Encoding.GetEncoding("iso-8859-1"), "", false, false);
                        if (!string.IsNullOrEmpty(_page))
                        {
                            bool _r = ProcessPage(_page, _id, skipImages, _relLink, _title, _pageId);
                            if (_r)
                            {
                                _result = true;
                            }
                        }
                    }
                }
                //else
                //{
                //    // direct page
                //    bool _r = ProcessPage(_resultsPage, null, skipImages);
                //    if (_r)
                //    {
                //        _result = true;
                //    }
                //}

            }

            return _result;
        }

        void CacheImage(object sender, EventArgs e)
        {
            ResultMovieItem _movie = sender as ResultMovieItem;
            if (_movie != null)
            {
                //// start caching results...
                this.MainWindow.Dispatcher.BeginInvoke((Action)delegate
                {
                    WebBrowser _browser = (this.MainWindow as ThumbGenMainWindow).TheWebBrowser;
                    _browser.Tag = _movie;
                    _browser.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(browser_LoadCompleted);
                    _browser.Navigate(new Uri(string.Format("{0}/filmplakat.php?id={1}", Host, _movie.ImageId), UriKind.RelativeOrAbsolute));

                }, DispatcherPriority.Send);
            }
        }

        void browser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            WebBrowser _browser = sender as WebBrowser;
            if (_browser != null)
            {
                ResultMovieItem _movie = _browser.Tag as ResultMovieItem;
                // trigger dataready event
                if (_movie != null && _movie.DataReadyEvent != null)
                {
                    _movie.DataReadyEvent.Set();
                }
            }
        }
    }
}
