using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#if NETFX_CORE
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Core;
#endif

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        bool record;
        #region UWP
#if NETFX_CORE
        MediaCapture capture;
        InMemoryRandomAccessStream buffer;
        string filename;
        string audioFile = "UWP_recording.wav";
        private async Task<bool> RecordProcess()
        {
            if (buffer != null)
            {
                buffer.Dispose();
            }
            buffer = new InMemoryRandomAccessStream();
            if (capture != null)
            {
                capture.Dispose();
            }
            try
            {
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio
                };
                capture = new MediaCapture();
                await capture.InitializeAsync(settings);
                capture.RecordLimitationExceeded += (MediaCapture sender) =>
                {
                    //Stop  
                    // await capture.StopRecordAsync();  
                    record = false;
                    throw new Exception("Record Limitation Exceeded ");
                };
                capture.Failed += (MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs) =>
                {
                    record = false;
                    throw new Exception(string.Format("Code: {0}. {1}", errorEventArgs.Code, errorEventArgs.Message));
                };
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.GetType() == typeof(UnauthorizedAccessException))
                {
                    throw ex.InnerException;
                }
                throw;
            }
            return true;
        }
        public async Task PlayRecordedAudio(CoreDispatcher UiDispatcher)
        {
            MediaElement playback = new MediaElement();
            IRandomAccessStream audio = buffer.CloneStream();

            if (audio == null)
                throw new ArgumentNullException("buffer");
            StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            if (!string.IsNullOrEmpty(filename))
            {
                StorageFile original = await storageFolder.GetFileAsync(filename);
                await original.DeleteAsync();
            }
            await UiDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                StorageFile storageFile = await storageFolder.CreateFileAsync(audioFile, CreationCollisionOption.GenerateUniqueName);
                filename = storageFile.Name;
                using (IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await RandomAccessStream.CopyAndCloseAsync(audio.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
                    await audio.FlushAsync();
                    audio.Dispose();
                }
                IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.Read);
                playback.SetSource(stream, storageFile.FileType);
                playback.Play();
            });
        }
#endif
        #endregion
        public MainPage()
        {
            this.InitializeComponent();
            textBlk.Text = "I am ready";
        }




        private async void recordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (record)
            {
                //already recored process  
            }
            else
            {
                textBlk.Text = "Recording ...";
				#region UWP
#if NETFX_CORE
				await RecordProcess();
                await capture.StartRecordToStreamAsync(MediaEncodingProfile.CreateWav(AudioEncodingQuality.Low), buffer);
                if (record)
                {
                    throw new InvalidOperationException();
                }

                record = true;
#endif
				#endregion
			}
		}


        private async void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            textBlk.Text = "Stopped recording.";
			#region UWP
#if NETFX_CORE
			Thread.Sleep(300);
            await capture.StopRecordAsync();
#endif
            #endregion
        }

        private async void playBtn_Click(object sender, RoutedEventArgs e)
        {
            textBlk.Text = "Replaying captured sound.";
            #region UWP
#if NETFX_CORE
            await PlayRecordedAudio(Dispatcher);
#endif
            #endregion
        }

	}
}
