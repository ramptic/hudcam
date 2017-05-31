using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;

using AForge.Video;
using AForge.Video.DirectShow;

namespace Hudcam.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private FilterInfo _currentDevice;
        private IVideoSource _videoSource;
        private Bitmap _raster;
        //private Bitmap _logo;

        public ObservableCollection<FilterInfo> VideoDevices { get; set; }

        public FilterInfo CurrentDevice
        {
            get { return _currentDevice; }
            set { _currentDevice = value; OnPropertyChanged("CurrentDevice"); }
        }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            GetVideoDevices();

            var rasterPath = System.IO.Path.Combine(Environment.CurrentDirectory, "raster.png");
            _raster = new Bitmap(rasterPath);

            //var logoPath = System.IO.Path.Combine(Environment.CurrentDirectory, "logo.png");
            //_logo = new Bitmap(logoPath);

            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopCamera();
        }

        private void GetVideoDevices()
        {
            VideoDevices = new ObservableCollection<FilterInfo>();
            foreach (FilterInfo filterInfo in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                VideoDevices.Add(filterInfo);
            }
            if (VideoDevices.Any())
            {
                CurrentDevice = VideoDevices[0];
            }
            else
            {
                MessageBox.Show("No video sources found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void video_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                BitmapImage bi;
                using (var frame = (Bitmap)eventArgs.Frame.Clone())
                {

                    var width = frame.Width;
                    var height = frame.Height;

                    // try overlay
                    Bitmap bitmapResult = new Bitmap(width, height, frame.PixelFormat);
                    Graphics g = Graphics.FromImage(bitmapResult);
                    g.DrawImage(frame, 0, 0, width, height);

                    // apply raster
                    g.DrawImage(_raster, 0, 0, width, height);

                    // apply logo
                    //g.DrawImage(_logo, width - 175, 25, 150, 121);

                    bi = bitmapResult.ToBitmapImage();

                    // TODO: Add frame to some output video stream
                }
                bi.Freeze(); // avoid cross thread operations and prevents leaks
                Dispatcher.BeginInvoke(new ThreadStart(delegate { imgPreview.Source = bi; }));
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error on _videoSource_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopCamera();
            }
        }

        private void StartCamera()
        {
            if (CurrentDevice != null)
            {
                _videoSource = new VideoCaptureDevice(CurrentDevice.MonikerString);
                _videoSource.NewFrame += video_NewFrame;
                _videoSource.Start();
            }
        }

        private void StopCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= new NewFrameEventHandler(video_NewFrame);
            }
        }

        public void OnStartClicked(object sender, EventArgs e)
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;

            StartCamera();
        }

        public void OnStopClicked(object sender, EventArgs e)
        {
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;

            StopCamera();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }
    }

    static class BitmapHelpers
    {
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }
    }
}
