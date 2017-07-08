using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Threading;

namespace ThumbGen
{
    internal class AsyncImageDownloader
    {
        class ThreadParams
        {
            public ResultMovieItem Movie { get; set; }
            public string ImageUrl {get; set;}
            public DispatcherObject Dispatcher { get; set; }
            public SetImageDataHandler Handler { get; set; }

            public ThreadParams(ResultMovieItem movie, DispatcherObject dispatcher, SetImageDataHandler handler): this(string.Empty, dispatcher, handler)
            {
                Movie = movie;
                if(Movie != null)
                {
                    ImageUrl = Movie.ImageUrl;
                }
            }

            public ThreadParams(string imageUrl, DispatcherObject dispatcher, SetImageDataHandler handler)
            {
                ImageUrl = imageUrl;
                Dispatcher = dispatcher;
                Handler = handler;
            }
        }

        public delegate void SetImageDataHandler(BitmapImage bmp, string imageUrl, object userData);

        public static void GetImageAsync(DispatcherObject dispatcher, ResultMovieItem movie, SetImageDataHandler handler)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), new ThreadParams(movie, dispatcher, handler));
        }

        public static void GetImageAsync(DispatcherObject dispatcher, string imageUrl, SetImageDataHandler handler)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), new ThreadParams(imageUrl, dispatcher, handler));
        }

        static void DoWork(object param)
        {
            ThreadParams _params = param as ThreadParams;
            string _imageUrl = _params.ImageUrl;

            if (string.IsNullOrEmpty(_imageUrl))
            {
                return;
            }

            bool _doIt = true;

            if (_params.Movie != null)
            {
                // if the ResultMovieItem needs to do something before querying data...execute and wait for event
                if (_params.Movie.DataQuerying != null)
                {
                    _params.Movie.DataQuerying(_params.Movie, new EventArgs());
                }

                // if Movie has DataReady event not null, wait for it (max 10 seconds)
                if (_params.Movie.DataReadyEvent != null)
                {
                    _doIt = _params.Movie.DataReadyEvent.WaitOne(10000);
                    _params.Movie.DataReadyEvent = null;
                }
            }

            BitmapImage _bmp = null;

            if (_doIt)
            {
                _bmp = DownloadImageFromUrl(_imageUrl);
            }

            _params.Dispatcher.Dispatcher.BeginInvoke((Action)delegate
            {
                if (_params.Handler != null)
                {
                    try
                    {
                        _params.Handler(_bmp, _params.ImageUrl, _params.Dispatcher);
                    }
                    catch { }
                }

            }, DispatcherPriority.Normal);
        }

        static BitmapImage DownloadImageFromUrl(string imageUrl)
        {
            BitmapImage _bmp = null;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                byte[] _downloadedData = Helpers.DownloadData(imageUrl);
                if (_downloadedData != null && _downloadedData.Length != 0)
                {
                    using (MemoryStream _ms = new MemoryStream(_downloadedData))
                    {
                        _ms.Position = 0;

                        try
                        {
                            _bmp = new BitmapImage();
                            _bmp.BeginInit();
                            _bmp.CacheOption = BitmapCacheOption.OnLoad;
                            _bmp.StreamSource = _ms;
                            _bmp.EndInit();
                            _bmp.Freeze();
                        }
                        catch
                        {
                            try
                            {
                                // sometimes the image has badmetadata and must be converted using GDI+
                                using (MemoryStream _badImageStream = Helpers.TempImageFromGDIplus(_ms) as MemoryStream)
                                {
                                    if (_badImageStream != null)
                                    {
                                        _bmp = new BitmapImage();
                                        _bmp.BeginInit();
                                        _bmp.CacheOption = BitmapCacheOption.OnLoad;
                                        _bmp.StreamSource = _badImageStream;
                                        _bmp.EndInit();
                                        _bmp.Freeze();
                                    }
                                }
                            }
                            catch 
                            {
                                _bmp = null;
                            }
                        }
                    }
                    _downloadedData = null;
                }
            }
            return _bmp;
        }
    }
}
