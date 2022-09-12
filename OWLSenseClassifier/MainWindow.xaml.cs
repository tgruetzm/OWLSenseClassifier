using Spectrogram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Spectrogram;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic.Devices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.ComponentModel.Design.Serialization;
using NAudio.Wave;
using System.Threading;
using System.Collections.Concurrent;
using NAudio.Wave.SampleProviders;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace OWLSenseClassifier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        AudioParser audioParser;
        Task currentAudio;
        CancellationTokenSource ts;
        bool paused = false;
 
    public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(RightArrow_KeyPress);
            this.KeyDown += new KeyEventHandler(LeftArrow_KeyPress);
            this.KeyDown += new KeyEventHandler(Space_KeyPress);
            ClassComboBox.ItemsSource = InferenceClasses.GetClasses();

            LoadBlankPCM();//load blank spectrogram
            //LoadRawPCM();
            //LoadNext();

            /*Task monitor = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (!paused)
                    {
                        if (currentAudio is not null && !currentAudio.IsCompleted)
                        {
                            currentAudio.Wait();
                            if (currentAudio.IsCanceled)
                            {
                                Thread.Sleep(500);
                                continue;
                            }
                            if (paused)
                                continue;
                        }
                        if (rawReader.Position < rawReader.Length)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                LoadNext();
                            });
                        }
                    }
                }
            },
            TaskCreationOptions.LongRunning);*/

        }

        void LoadSpectrogram(InferenceData data)
        {
            CancelCurrentAudio();
            double[] audio = data.ReadWavMono();
            var sg = new SpectrogramGenerator(AudioParser.SAMPLE_RATE, fftSize: 1024, stepSize: 25, maxFreq: 4000);
            sg.Add(audio);
            Bitmap bitmap = sg.GetBitmap(50);
            Bitmap scale = sg.GetVerticalScale(200, 0, 25);
            ImageScale.Source = ToBitmapImage(scale);
            ImageSpectrogram.Source = ToBitmapImage(bitmap);
            ts = new CancellationTokenSource();
            CancellationToken ct = ts.Token;
            currentAudio = Task.Run(() => {
                using (var outputDevice = new WaveOutEvent())
                {
                    using (var audioStream = data.GetAudio())
                    {
                        outputDevice.Init(audioStream);
                        outputDevice.Play();
                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            if (ct.IsCancellationRequested)
                                break;
                            Thread.Sleep(10);
                        }
                    }
                }
            },ct);

            //update UI with data
            if (audioParser != null)
                RecordsTextBlock.Text = audioParser.GetStatus();
            SensorTextBlock.Text = data.SensorId.ToString();
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(data.Epoch);
            DateTextBlock.Text = dateTime.DateTime.ToLocalTime().ToString();
            ClassComboBox.SelectedItem = data.Label;
            ProbabilityTextBlock.Text = data.Probability.ToString();
            TempTextBlock.Text = data.TemperatureC.ToString();
            LocationTextBlock.Text = data.Latitude.ToString() + "\r\n" + data.Longitude.ToString();

        }

        void CancelCurrentAudio()
        {
            if (currentAudio is not null && !currentAudio.IsCompleted && !currentAudio.IsCanceled)
            {
                try
                {
                    ts.Cancel();
                    currentAudio.Wait();
                }
                catch (AggregateException ae) 
                {
                    foreach(Exception ex in ae.InnerExceptions)
                    {
                        if (ex is not OperationCanceledException)
                            throw ex;
                    }
                }
            }
        }

        void RightArrow_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                CancelCurrentAudio();
                LoadSpectrogram(audioParser.GetNext());
            }
        }

        void LeftArrow_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                if (currentAudio is not null && !currentAudio.IsCompleted)
                {
                    CancelCurrentAudio();
                    LoadSpectrogram(audioParser.GetPrevious());
                }
                LoadSpectrogram(audioParser.GetCurrent());
            }
        }


        void Space_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                paused = !paused;
            }
        }

        void LoadBlankPCM()
        {
            LoadSpectrogram(new InferenceData());
        }

       


        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            paused = !paused;
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "inferences.raw"; // Default file name
            dialog.DefaultExt = ".raw"; // Default file extension
            dialog.Filter = "raw pcm file(.raw)|*.raw"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                string[] fileSplit = dialog.FileName.Split('.');
                string metaFile = "";
                for(int i = 0; i < fileSplit.Length -1; i++)
                {
                    metaFile += fileSplit[i];
                }
                metaFile += ".meta";
                if (!File.Exists(metaFile))
                {
                    MessageBox.Show("Metadata file does not exist: " + metaFile, "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string[] fs1 = dialog.FileName.Split("\\");
                StringBuilder root = new StringBuilder();
                for(int i = 0; i < fs1.Length-1; ++i)
                {
                    root.Append(fs1[i]);
                }
                string[] fs2 = metaFile.Split("\\");
                FileTextBlock.Text = "..\\" +fs1[fs1.Length-1] + "\r\n" + "..\\" + fs2[fs2.Length - 1];
                audioParser = new AudioParser(dialog.FileName,metaFile);
                LoadSpectrogram(audioParser.GetCurrent());
            }
        }
    }
}
