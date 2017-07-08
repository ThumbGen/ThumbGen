using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;
using System.Diagnostics;

namespace ThumbGen
{
    public class VideoScreenShot
    {
        public delegate void CaptureWorkerDelegate(BitmapFrame frame, object state);

        public static void CaptureScreenAsync(Uri source, TimeSpan timeSpan, object state, CaptureWorkerDelegate finalWorkerPrimary)
        {
            CaptureScreenAsync(source, timeSpan, state, -1, finalWorkerPrimary, null);
        }

        public static void CaptureScreenAsync(Uri source, Dictionary<TimeSpan, object> captureList, CaptureWorkerDelegate finalWorkerPrimary)
        {
            CaptureScreenAsync(source, captureList, -1, finalWorkerPrimary, null);
        }

        public static void CaptureScreenAsync(Uri source, TimeSpan timeSpan, object state, double scale, CaptureWorkerDelegate finalWorkerPrimary, CaptureWorkerDelegate finalWorkerThumbnail)
        {
            CaptureScreenAsync(source, new Dictionary<TimeSpan, object> { { timeSpan, state } }, scale, finalWorkerPrimary, finalWorkerThumbnail);
        }

        public static void CaptureScreenAsync(Uri source, Dictionary<TimeSpan, object> captureList, double scale, CaptureWorkerDelegate finalWorkerPrimary, CaptureWorkerDelegate finalWorkerThumbnail)
        {
            ThreadPool.QueueUserWorkItem(delegate { CaptureScreen(source, captureList, scale, finalWorkerPrimary, finalWorkerThumbnail); });
        }

        public static void CaptureScreen(Uri source, TimeSpan timeSpan, object state, CaptureWorkerDelegate finalWorkerPrimary)
        {
            CaptureScreen(source, timeSpan, state, -1, finalWorkerPrimary, null);
        }

        public static bool CaptureScreen(Uri source, Dictionary<TimeSpan, object> captureList, CaptureWorkerDelegate finalWorkerPrimary)
        {
            return CaptureScreen(source, captureList, -1, finalWorkerPrimary, null);
        }

        public static void CaptureScreen(Uri source, TimeSpan timeSpan, object state, double scale, CaptureWorkerDelegate finalWorkerPrimary, CaptureWorkerDelegate finalWorkerThumbnail)
        {
            CaptureScreen(source, new Dictionary<TimeSpan, object> { { timeSpan, state } }, scale, finalWorkerPrimary, finalWorkerThumbnail);
        }

        public static bool CaptureScreen(Uri source, Dictionary<TimeSpan, object> captureList, double scale, CaptureWorkerDelegate finalWorkerPrimary, CaptureWorkerDelegate finalWorkerThumbnail)
        {
            bool _result = false;

            var mutexLock = new Mutex(false, source.GetHashCode().ToString());
            mutexLock.WaitOne();

            var player = new MediaPlayer { Volume = 0, ScrubbingEnabled = true };

            player.Open(source);
            player.Pause();
            foreach (var pair in captureList)
            {
                var timeSpan = pair.Key;
                var state = pair.Value;

                //player.Play();
                player.Position = timeSpan;
                Thread.Sleep(1000);
                //player.Pause();

                int width = player.NaturalVideoWidth;
                int height = player.NaturalVideoHeight;

                if (player.NaturalVideoWidth != 0 && player.NaturalVideoHeight != 0)
                {
                    var rtb = new RenderTargetBitmap(player.NaturalVideoWidth, player.NaturalVideoHeight, 96, 96, PixelFormats.Pbgra32);
                    var dv = new DrawingVisual();

                    using (DrawingContext dc = dv.RenderOpen())
                        dc.DrawVideo(player, new Rect(0, 0, player.NaturalVideoWidth, player.NaturalVideoHeight));

                    rtb.Render(dv);
                    var frame = BitmapFrame.Create(rtb).GetCurrentValueAsFrozen();
                    if (finalWorkerPrimary != null)
                        finalWorkerPrimary(frame as BitmapFrame, state);

                    _result = true;
                }
                //if (scale > 0 && finalWorkerThumbnail != null)
                //{
                //    var thumbnailFrame =
                //        BitmapFrame.Create(new TransformedBitmap(frame as BitmapSource, new ScaleTransform(scale, scale))).
                //            GetCurrentValueAsFrozen();
                //    var encoder = new JpegBitmapEncoder();
                //    encoder.Frames.Add(thumbnailFrame as BitmapFrame);

                //    finalWorkerThumbnail(thumbnailFrame as BitmapFrame, state);
                //}
            }
            player.Stop();
            player.Close();
            player = null;
            mutexLock.ReleaseMutex();

            return _result;
        }

        static void player_MediaFailed(object sender, ExceptionEventArgs e)
        {

        }

        public static void GenerateThumbnail(List<string> inputSnapshots, string outputFilename)
        {
            bool _doSave = false;

            try
            {
                //string[] _indexes = SnapshotsIndexes.Text.Trim().Split(',');

                using (Bitmap thumb = new Bitmap((int)Helpers.ThumbnailSize.Width, (int)Helpers.ThumbnailSize.Height))
                {
                    using (Graphics g = Graphics.FromImage(thumb))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        //Set Image codec of JPEG type, the index of JPEG codec is "1"            
                        System.Drawing.Imaging.ImageCodecInfo codec = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()[1];
                        //Set the parameters for defining the quality of the thumbnail...         
                        System.Drawing.Imaging.EncoderParameters eParams = new System.Drawing.Imaging.EncoderParameters(1);
                        eParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                        int _X = 0;
                        int _Y = 0;

                        //Now draw the images on the instance of thumbnail Bitmap object            
                        foreach (string _file in inputSnapshots)
                        {
                            if (File.Exists(_file))
                            {
                                using (System.Drawing.Image src = Bitmap.FromFile(_file))
                                {
                                    int destWidth = Helpers.ThumbnailSize.Width;
                                    int destHeight = Helpers.ThumbnailSize.Height;

                                    float nPercent = 0;

                                    // always KeepAspectRatio for thumbs generated from snapshots
                                    //if (FileManager.Configuration.Options.KeepAspectRatio)
                                    {
                                        int sourceWidth = src.Width;
                                        int sourceHeight = src.Height;

                                        float nPercentW = 0;
                                        float nPercentH = 0;

                                        nPercentW = ((float)Helpers.ThumbnailSize.Width / (float)sourceWidth);
                                        nPercentH = ((float)Helpers.ThumbnailSize.Height / (float)sourceHeight);

                                        if (nPercentH < nPercentW)
                                            nPercent = nPercentH;
                                        else
                                            nPercent = nPercentW;

                                        destWidth = (int)(sourceWidth * nPercent);
                                        destHeight = (int)(sourceHeight * nPercent);
                                    }

                                    g.DrawImage(src, new Rectangle(_X, _Y, destWidth, destHeight));

                                    _X = 0;
                                    _Y = _Y + destHeight;
                                    _doSave = true;
                                }
                            }
                        }

                        if (_doSave)
                        {
                            thumb.Save(outputFilename, codec, eParams);
                        }
                    }
                }
            }
            catch { }

        }

        private static void makeJpeg(BitmapFrame frame, object state)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(frame);

            string filename = (string)state;
            filename = Path.ChangeExtension(filename, ".jpg");
            using (var fs = new FileStream(filename, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }

        public static bool MakeThumbnail(string movieFilename)
        {
            return MakeThumbnail(movieFilename, Helpers.GetCorrectThumbnailPath(movieFilename, true));
        }

        public static bool MakeBackdropSnapshot(string movieFilename, string targetFile)
        {
            bool _result = false;

            if (FileManager.Configuration.Options.IsMTNPathSpecified)
            {
                if (Path.GetExtension(movieFilename).ToLowerInvariant().Contains(".iso"))
                {
                    return false;
                }

                double _duration = 0d; 
                System.Windows.Size _size = new System.Windows.Size(); 
                MediaInfoManager.GetDurationAndVideoResolution(movieFilename, out _duration, out _size);

                // always skip 10% from the beginning and from the end of the movie
                int _minSkipSeconds = (int)((_duration / 1000) * 0.1);

                Random _rand = new Random();
                _duration = (int)(_duration / 1000);
                try
                {
                    int _omitSec = _rand.Next(_minSkipSeconds, (int)_duration/2 - _minSkipSeconds); // choose a number between minSkipSeconds and half of the duration
                    int _omitEnd = _rand.Next(_minSkipSeconds, (int)_duration/2 - _minSkipSeconds); // chose a number between minSkipSeconds and half of the duration

                    string _command = string.Format(" -o .tmp -B {0} -E {1} -t -c 1 -r 1 -i -b 0{2}50 -D 6 -P \"{3}\"", _omitSec, _omitEnd, Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator, movieFilename);
                    string _imageUrl = Path.ChangeExtension(movieFilename, ".tmp");

                    FileManager.AddToGarbageFiles(_imageUrl);

                    try
                    {
                        ProcessStartInfo _pi = new ProcessStartInfo(FileManager.Configuration.Options.MTNPath, _command);
                        _pi.CreateNoWindow = true;
                        _pi.UseShellExecute = false;
                        Process _p = Process.Start(_pi);
                        _p.WaitForExit(8000);

                        Thread.Sleep(1200); // wait for the file to become available
                        if (File.Exists(_imageUrl))
                        {
                            try
                            {
                                File.Copy(_imageUrl, targetFile, true);
                                _result = true;
                            }
                            catch (Exception ex)
                            {
                                Loggy.Logger.DebugException("Videosnapshot: ", ex);
                            }
                        }
                    }
                    finally
                    {
                        Helpers.RemoveFile(_imageUrl);
                    }
                }
                catch (Exception ex)
                {
                    Loggy.Logger.DebugException("Calculate skip ", ex);
                }
            }
            return _result;
        }

        public static bool MakeThumbnail(string movieFilename, string targetFile)
        {
            bool _result = false;

            if (FileManager.Configuration.Options.IsMTNPathSpecified)
            {
                if (Path.GetExtension(movieFilename).ToLowerInvariant().Contains(".iso"))
                {
                    return false;
                }

                int _cnt = 15;

                string _command = string.Format(" -o .tmp -w {0} -t -c 1 -h 10 -r {1} -i -b 0{2}50 -D 12 -P \"{3}\"",
                        FileManager.Configuration.Options.ThumbnailSize.Width, _cnt, Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator, movieFilename);
                string _imageUrl = Path.ChangeExtension(movieFilename, ".tmp");

                FileManager.AddToGarbageFiles(_imageUrl);

                try
                {
                    ProcessStartInfo _pi = new ProcessStartInfo(FileManager.Configuration.Options.MTNPath, _command);
                    _pi.CreateNoWindow = true;
                    _pi.UseShellExecute = false;
                    Process _p = Process.Start(_pi);
                    _p.WaitForExit(20000);

                    if (File.Exists(_imageUrl))
                    {
                        Helpers.CreateThumbnailImage(_imageUrl, targetFile, true, true, Helpers.ThumbnailSize, false, Helpers.MaxThumbnailFilesize);

                        if (FileManager.Configuration.Options.AutogenerateFolderJpg)
                        {
                            try
                            {
                                File.Copy(targetFile, FileManager.Configuration.GetFolderjpgPath(movieFilename, true), FileManager.Configuration.Options.OverwriteExistingThumbs);
                            }
                            catch { }
                        }
                        _result = true;
                    }
                }
                finally
                {
                    Helpers.RemoveFile(_imageUrl);
                }
            }
            return _result;
        }

        public static bool MakeThumbnail2(string movieFilename, string targetFile)
        {
            bool _result = false;

            double _duration = 0d; // MediaInfoManager.GetDurationMilliseconds(movieFilename);
            System.Windows.Size _size = new System.Windows.Size(); // MediaInfoManager.GetVideoResolution(movieFilename);
            MediaInfoManager.GetDurationAndVideoResolution(movieFilename, out _duration, out _size);

            if (_duration != 0d)
            {
                Collection<string> _snaps = new Collection<string>();

                Random _rnd = new Random();
                Dictionary<TimeSpan, object> _frames = new Dictionary<TimeSpan, object>();
                List<string> _filesList = new List<string>();
                try
                {
                    int _cnt = 5;
                    System.Windows.Size _thumbSize = FileManager.Configuration.Options.ThumbnailSize;
                    double _rap = 1;
                    if (_size.Width >= _size.Height)
                    {
                        _rap = _size.Width / _thumbSize.Width;
                        _cnt = (int)Math.Round(_thumbSize.Height / (_size.Height / _rap));
                    }
                    else
                    {
                        _rap = _size.Height / _thumbSize.Height;
                        _cnt = (int)Math.Round(_thumbSize.Width / (_size.Width / _rap));
                    }

                    for (int _i = 0; _i < _cnt; _i++)
                    {
                        string _thumb = Helpers.GetUniqueFilename(".jpg");
                        _filesList.Add(_thumb);
                        int _start = (int)((_i * _duration) / _cnt);
                        int _stop = (int)(((_i + 1) * _duration) / _cnt);
                        _frames.Add(TimeSpan.FromMilliseconds((double)_rnd.Next(_start, _stop)), _thumb);
                    }

                    if (VideoScreenShot.CaptureScreen(new Uri(movieFilename, UriKind.RelativeOrAbsolute), _frames, makeJpeg))
                    {
                        VideoScreenShot.GenerateThumbnail(_filesList, targetFile);

                        if (FileManager.Configuration.Options.AutogenerateFolderJpg)
                        {
                            try
                            {
                                File.Copy(targetFile, FileManager.Configuration.GetFolderjpgPath(movieFilename, true), FileManager.Configuration.Options.OverwriteExistingThumbs);
                            }
                            catch { }
                        }

                        _result = true;
                    }
                }
                finally
                {
                    foreach (string _file in _filesList)
                    {
                        try
                        {
                            if (File.Exists(_file))
                            {
                                File.Delete(_file);
                            }
                        }
                        catch { }
                    }
                }
            }

            return _result;
        }
    }
}
