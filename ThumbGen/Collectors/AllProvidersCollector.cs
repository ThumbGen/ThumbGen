using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace ThumbGen
{
    public class AllProvidersCollector : BaseCollector
    {
        private List<BaseCollector> m_Collectors = new List<BaseCollector>();

        public ReadOnlyCollection<BaseCollector> Collectors;

        public AllProvidersCollector(List<BaseCollector> collectors)
        {
            Collectors = new ReadOnlyCollection<BaseCollector>(m_Collectors);

            m_TemplatesManager.RefreshTemplates();

            m_Collectors.AddRange(collectors);
        }

        public override Country Country
        {
            get { return Country.USA; }
        }

        public override string Host
        {
            get { return string.Empty; }
        }

        public override string CollectorName
        {
            get
            {
                return "AllProviders";
            }
        }

        public override void ClearResults()
        {
            base.ClearResults();

            foreach (BaseCollector _collector in m_Collectors)
            {
                _collector.ClearResults();
            }
        }

        public override bool GetResults(string keywords, string imdbID, bool skipImages)
        {
            bool _result = false;

            FileManager.CancellationPending = false;
            AutoResetEvent[] _events = new AutoResetEvent[m_Collectors.Count];

            if (m_Collectors.Count != 0)
            {
                //foreach (BaseCollector _collector in m_Collectors)
                //{
                //    bool _b = _collector.GetResults(keywords);
                //    ResultsList.AddRange(_collector.ResultsList);

                //    if (!_result) // if once u got true, remember it
                //    {
                //        _result = (bool)_b;
                //    }
                //}
                int _cnt = 0;
                foreach (BaseCollector _collector in m_Collectors)
                {
                    if (FileManager.CancellationPending)
                    {
                        break;
                    }
                    _collector.MainWindow = this.MainWindow;
                    _collector.IMDBID = this.IMDBID;
                    _collector.Year = this.Year;

                    AutoResetEvent _event = new AutoResetEvent(false);
                    _events[_cnt] = _event;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), new CollectorThreadParams(_collector, keywords, imdbID, skipImages, _event));
                    _cnt++;
                }
                try
                {
                    WaitHandle.WaitAll(_events, TimeSpan.FromMinutes(1));
                }
                catch { }
                if (FileManager.Mode == ProcessingMode.Manual || FileManager.Mode == ProcessingMode.SemiAutomatic)
                {
                    FileManager.CancellationPending = false;
                }
                _result = this.ResultsList.Count != 0;
            }

            return _result;
        }

        private object m_LockMe = new DateTime();

        private void DoWork(object param)
        {
            bool _result = false;

            CollectorThreadParams _params = param as CollectorThreadParams;
            if (_params != null)
            {
                _params.Collector.CurrentMovie = this.CurrentMovie;
                DateTime _start = DateTime.UtcNow;
                try
                {
                    _result = _params.Collector.GetResults(_params.Keywords, _params.ImdbId, _params.SkipImages);
                }
                finally
                {
                    DateTime _end = DateTime.UtcNow;
                    TimeSpan _ts = TimeSpan.FromMilliseconds((_end-_start).TotalMilliseconds);
                    _params.Collector.SearchTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", _ts.Hours, _ts.Minutes, _ts.Seconds, _ts.Milliseconds / 10);
                    Loggy.Logger.Debug("Collector: {0} Search time: {1}", _params.Collector.CollectorName, _params.Collector.SearchTime.ToString());
                }

            }
            if (_params.Collector != null)
            {
                lock (m_LockMe)
                {
                    ResultsList.AddRange(_params.Collector.ResultsList);
                }
            }

            if (_params.Event != null)
            {
                _params.Event.Set();
            }
        }

        public BaseCollector this[string collectorName]
        {
            get
            {
                var _result = from c in m_Collectors
                              where string.Compare(c.CollectorName, collectorName) == 0
                              select c;

                return _result.Count() != 0 ? _result.ElementAt(0) as BaseCollector : null;
            }
        }

        public string GetSearchTime(string collectorName)
        {
            string _result = string.Empty;

            foreach (BaseCollector _collector in m_Collectors)
            {
                if (string.Compare(_collector.CollectorName, collectorName, true) == 0)
                {
                    _result = _collector.SearchTime;
                    break;
                }
            }

            return _result;
        }
    }

    class CollectorThreadParams
    {
        public BaseCollector Collector { get; set; }
        public string Keywords { get; set; }
        public string ImdbId { get; set; }
        public bool SkipImages { get; set; }
        public AutoResetEvent Event { get; set; }

        public CollectorThreadParams(BaseCollector collector, string keywords, string imdbid, bool skipImages, AutoResetEvent eventDone)
        {
            Collector = collector;
            Keywords = keywords;
            ImdbId = imdbid;
            Event = eventDone;
            SkipImages = skipImages;
        }
    }

}
