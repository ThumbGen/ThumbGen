using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Controls;

namespace ThumbGen
{
    /// <summary>
    /// Interaction logic for MoviePlayer.xaml
    /// </summary>
    public partial class MoviePlayer : Window
    {
        public MoviePlayer()
        {
            InitializeComponent();
        }

        private double AspectRatio;
        DispatcherTimer timer;
        public ObservableCollection<ResultItemBase> SnapshotFiles = null;

        public double CurrentPositionMilliseconds
        {
            get { return (double)GetValue(CurrentPositionMillisecondsProperty); }
            set { SetValue(CurrentPositionMillisecondsProperty, value); }
        }



        public bool UseCropper
        {
            get { return (bool)GetValue(UseCropperProperty); }
            set { SetValue(UseCropperProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UseCropper.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UseCropperProperty =
            DependencyProperty.Register("UseCropper", typeof(bool), typeof(MoviePlayer), new UIPropertyMetadata(true));



        // Using a DependencyProperty as the backing store for CurrentPositionMilliseconds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentPositionMillisecondsProperty =
            DependencyProperty.Register("CurrentPositionMilliseconds", typeof(double), typeof(MoviePlayer), new UIPropertyMetadata((double)0));

        public string TimeInformation
        {
            get { return (string)GetValue(TimeInformationProperty); }
            set { SetValue(TimeInformationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TimeInformation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TimeInformationProperty =
            DependencyProperty.Register("TimeInformation", typeof(string), typeof(MoviePlayer), new UIPropertyMetadata(string.Empty));

        public static bool Show(Window owner, string movieFilename, ObservableCollection<ResultItemBase> snapshotFiles, Size cropperSize)
        {
            bool _result = false;

            MoviePlayer _mp = new MoviePlayer();
            _mp.Owner = owner;
            _mp.Closing += new System.ComponentModel.CancelEventHandler(_mp_Closing);
            if (cropperSize != null && cropperSize.Height != 0 && cropperSize.Width != 0)
            {
                _mp.TheCropper.Width = cropperSize.Width;
                _mp.TheCropper.Height = cropperSize.Height;
            }
            else
            {
                _mp.cbUseCropper.IsChecked = false;
                _mp.cbUseCropper.Visibility = Visibility.Collapsed;
                _mp.TheCropper.Visibility = Visibility.Collapsed;
            }

            _mp.MainGrid.DataContext = _mp;
            _mp.myMediaElement.DataContext = _mp;
            _mp.SnapshotFiles = snapshotFiles;
            _mp.myMediaElement.Source = new Uri(movieFilename, UriKind.RelativeOrAbsolute);
            _mp.myMediaElement.Play();
            var res = _mp.ShowDialog();
            _result = res.HasValue && res.Value;

            return _result;
        }

        static void _mp_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (sender as MoviePlayer).myMediaElement.Stop();
            (sender as MoviePlayer).myMediaElement.Source = null;
        }


        string Crop(string filename)
        {
            try
            {
                double rubberBandLeft = Canvas.GetLeft(TheCropper);
                double rubberBandTop = Canvas.GetTop(TheCropper);

                //create a new .NET 2.0 bitmap (which allowing saving) based on the bound bitmap url
                using (System.Drawing.Bitmap source = new System.Drawing.Bitmap(filename))
                {
                    //create a new .NET 2.0 bitmap (which allowing saving) to store cropped image in, should be 
                    //same size as rubberBand element which is the size of the area of the original image we want to keep
                    using (System.Drawing.Bitmap target = new System.Drawing.Bitmap((int)TheCropper.ActualWidth, (int)TheCropper.ActualHeight))
                    {
                        //create a new destination rectange
                        System.Drawing.RectangleF recDest = new System.Drawing.RectangleF(0.0f, 0.0f, (float)target.Width, (float)target.Height);
                        //different resolution fix prior to cropping image
                        float hd = 1.0f;// / (target.HorizontalResolution / source.h);
                        float vd = 1.0f;// / (target.VerticalResolution / source.VerticalResolution);
                        float hScale = 1.0f;// (float)zoomFactor;
                        float vScale = 1.0f;// (float)zoomFactor;
                        System.Drawing.RectangleF recSrc = new System.Drawing.RectangleF((hd * (float)rubberBandLeft) * hScale,
                                                                                         (vd * (float)rubberBandTop) * vScale,
                                                                                         (hd * (float)TheCropper.Width) * hScale,
                                                                                         (vd * (float)TheCropper.Height) * vScale);
                        using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(target))
                        {
                            gfx.DrawImage(source, recDest, recSrc, System.Drawing.GraphicsUnit.Pixel);
                        }

                        filename = Helpers.GetUniqueFilename(".jpg");
                        FileManager.GarbageFiles.Add(filename);

                        System.Drawing.Imaging.ImageCodecInfo codec = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()[1];
                        //Set the parameters for defining the quality of the thumbnail...         
                        System.Drawing.Imaging.EncoderParameters eParams = new System.Drawing.Imaging.EncoderParameters(1);
                        eParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 99L);

                        target.Save(filename, codec, eParams);

                        return filename;
                    }
                }
            }
            catch 
            {
                return filename;
            }
        }

        void Snap()
        {
            try
            {
                Size dpi = new Size(96, 96);
                RenderTargetBitmap bmp = new RenderTargetBitmap((int)ThePlayer.ActualWidth, (int)ThePlayer.ActualHeight, dpi.Width, dpi.Height, PixelFormats.Pbgra32);
                bmp.Render(myMediaElement);

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                string filename = Helpers.GetUniqueFilename(".jpg");
                // old file can be deleted at the end
                FileManager.GarbageFiles.Add(filename);

                using (FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    encoder.Save(fs);
                    fs.Close();
                }
                encoder = null;
                GC.Collect();

                if (UseCropper)
                {
                    // crop it
                    string _newFilename = Crop(filename);
                    if (_newFilename != filename)
                    {
                        filename = _newFilename;
                    }
                    // else something went wrong, keep the file
                }


                if (SnapshotFiles == null)
                {
                    Process.Start(filename);
                }
                else
                {
                    SnapshotFiles.Add(new ResultMovieItemSnapshot(SnapshotFiles.Count, string.Format("Snapshot {0}", SnapshotFiles.Count), filename, BaseCollector.VIDEOSNAP));
                }
            }
            catch { }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Snap();
        }

        // Play the media.
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            // The Play method will begin the media if it is not currently active or 
            // resume media if it is paused. This has no effect if the media is
            // already running.
            myMediaElement.Play();

            // Initialize the MediaElement property values.
            InitializePropertyValues();

        }

        // Pause the media.
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

            // The Pause method pauses the media if it is currently running.
            // The Play method can be used to resume.
            myMediaElement.Pause();

        }

        // Stop the media.
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {

            // The Stop method stops and resets the media to be played from
            // the beginning.
            myMediaElement.Stop();

        }

        // Change the volume of the media.
        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            if (myMediaElement != null)
            {
                myMediaElement.Volume = (double)volumeSlider.Value;
            }
        }

        private void Element_MediaOpened(object sender, EventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

            AspectRatio = (float)myMediaElement.NaturalVideoWidth / (float)myMediaElement.NaturalVideoHeight;
            timelineSlider.Maximum = myMediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
            timelineSlider.Value = 0;

            ResizeMovie();
        }

        void ResizeMovie()
        {
            if (myMediaElement != null)
            {
                double zoom = ZoomSlider.Value;
                double _width = myMediaElement.NaturalVideoWidth * zoom;

                myMediaElement.Width = Math.Min(this.ActualWidth, _width);
                myMediaElement.Height = (ThePlayer.Width / AspectRatio) * zoom;
            }
        }

        private bool m_SliderFrozen = false;
        void timer_Tick(object sender, EventArgs e)
        {
            m_SliderFrozen = true;
            try
            {
                timelineSlider.Value = (double)myMediaElement.Position.TotalMilliseconds;
                CurrentPositionMilliseconds = myMediaElement.Position.TotalMilliseconds;

                string _current = FormatTimeSpan(myMediaElement.Position.TotalMilliseconds);
                string _total = string.Empty;
                if (myMediaElement.NaturalDuration.HasTimeSpan)
                {
                    _total = FormatTimeSpan(myMediaElement.NaturalDuration.TimeSpan.TotalMilliseconds);
                }
                TimeInformation = string.Format("{0} / {1}", _current, _total);
            }
            finally
            {
                m_SliderFrozen = false;
            }
        }

        private string FormatTimeSpan(double milliseconds)
        {
            TimeSpan _tst = TimeSpan.FromMilliseconds(milliseconds);
            return string.Format("{0:00}:{1:00}:{2:00}.{3:00}", _tst.Hours, _tst.Minutes, _tst.Seconds, _tst.Milliseconds);
        }

        // When the media playback is finished. Stop() the media to seek to media start.
        private void Element_MediaEnded(object sender, EventArgs e)
        {
            myMediaElement.Stop();
        }

        private void myMediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(e.ErrorException != null ? e.ErrorException.Message : null, "Error during playback", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // rewind
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            //if (TheStoryboard.GetIsPaused(myMediaElement))
            {
                //myMediaElement.Position = myMediaElement.Position - TimeSpan.FromMilliseconds(500);
                //myMediaElement.Play();
                //myMediaElement.Pause();
            }
        }

        // forward
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            //myMediaElement.Position = myMediaElement.Position + TimeSpan.FromMilliseconds(500);
        }

        // Jump to different parts of the media (seek to). 
        private void SeekToMediaPosition(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            if (!m_SliderFrozen)
            {
                int SliderValue = (int)timelineSlider.Value;

                // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
                // Create a TimeSpan with miliseconds equal to the slider value.
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                myMediaElement.Position = ts;
            }
        }

        void InitializePropertyValues()
        {
            // Set the media's starting Volume and SpeedRatio to the current value of the
            // their respective slider controls.
            myMediaElement.Volume = (double)volumeSlider.Value;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            Snap();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ResizeMovie();
        }

    }

}
