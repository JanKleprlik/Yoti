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
using System.Threading.Tasks;
using System.Threading;
#if NETFX_CORE
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
#endif

#if __ANDROID__
using Android.Media;
using Android.Content.PM;
#endif

#if __WASM__
using Uno.Foundation;
using System.Text.RegularExpressions;
#endif


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
#region ANDROID
#if __ANDROID__
        bool external = false;
        private string filePath;
        private MediaRecorder recorder;
        private Android.Media.MediaPlayer player;
#endif
#endregion
#region WASM

#endregion

		public MainPage()
        {
            this.InitializeComponent();
            textBlk.Text = "I am ready";
#region ANDROID
#if __ANDROID__
            if (external)
			{
                filePath = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath, "recording.mp4");
            }
            else
			{
                filePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "recording.mp4");
            }
#endif
#endregion
		}




		private async void recordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (record)
            {
                //already recored process  
            }
            else
            {
#region UWP
#if NETFX_CORE
				await RecordProcess();
                await capture.StartRecordToStreamAsync(MediaEncodingProfile.CreateWav(AudioEncodingQuality.Low), buffer);
                if (record)
                {
                    throw new InvalidOperationException();
                }

#endif
#endregion
#region ANDROID
#if __ANDROID__

                //check for permission
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;

                
                Console.Out.WriteLine("[DEBUG] granting access to microphone");
                bool canAccessMicrophone = await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.RecordAudio);
                if (!canAccessMicrophone)
				{
                    Console.Out.WriteLine("[DEBUG] cannot access microphone");
                    return;
				}

				if (external)
				{
                    bool canAccessStorageW = await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.WriteExternalStorage);
                    if (!canAccessStorageW)
                        return;                
                    bool canAccessStorageR = await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.ReadExternalStorage);
                    if (!canAccessStorageR)
                        return;
				}
                
                try
                {
                    
		            if (System.IO.File.Exists(filePath))
		            {
                        Console.Out.WriteLine("[DEBUG] deleting existing file");
			            System.IO.File.Delete(filePath);
		            }
                    
                    if (recorder == null)
                    {
                        recorder = new MediaRecorder(); // Initial state.
                    }
                    else
                    {
                        recorder.Reset();
                    }

                    Console.Out.WriteLine("[DEBUG] setting up recording options");
                    recorder.SetAudioSource(AudioSource.Mic);
                    recorder.SetOutputFormat(OutputFormat.Mpeg4);
                    recorder.SetAudioChannels(1);
                    recorder.SetAudioSamplingRate(11025);
                    recorder.SetAudioEncodingBitRate(44000);
                    recorder.SetAudioEncoder(AudioEncoder.HeAac);
                    Console.Out.WriteLine("[DEBUG] seting up outputfile");
                    Console.Out.WriteLine("[DEBUG]" + filePath);
                    recorder.SetOutputFile(filePath);
                    recorder.Prepare();
                    Console.Out.WriteLine("[DEBUG] starting to listen");
                    recorder.Start();


                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("[DEBUG]" + ex.StackTrace);
                }
#endif
#endregion

#region WASM
#if __WASM__

                MainPage.FileSelectedEvent -= OnFileSelectedEvent;
                MainPage.FileSelectedEvent += OnFileSelectedEvent;
                WebAssemblyRuntime.InvokeJS(@"
    console.log('calling javascript');
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.wav';
    input.onchange = e => {
        var file = e.target.files[0];
        if ((file.size /1024 /1024)>5){
            alert('File size exceeds 5 MB');
        }
        else
        {
            console.log(file.size);
            var reader = new FileReader();
            reader.readAsDataURL(file);
            reader.onload = readerEvent => {
                var content = readerEvent.target.result; // this is the content
                console.log('spitting out content');
                console.log(content);
                var selectFile = Module.mono_bind_static_method(" + "\"[BP.Wasm] BP.MainPage:SelectFile\""+@");
                selectFile(content);
        }
        };
    };
    input.click(); ");
#endif
#endregion

                record = true;
                textBlk.Text = "Recording ...";

            }
        }

        private async void stopBtn_Click(object sender, RoutedEventArgs e)
        {
#region UWP
#if NETFX_CORE
			Thread.Sleep(300);
            await capture.StopRecordAsync();
#endif
#endregion
#region ANDROID

#if __ANDROID__
            Console.Out.WriteLine("[DEBUG] STOP recording clicked.");
            if (recorder != null)
            {
                Console.Out.WriteLine("[DEBUG] Stopping the recording.");
                recorder.Stop();
                recorder.Release();
                recorder = null;
                Console.Out.WriteLine("[DEBUG] Stopped the recording.");
            }
#endif

#endregion
            record = false;
            textBlk.Text = "Stopped recording.";
        }

        private async void playBtn_Click(object sender, RoutedEventArgs e)
        {
            Console.Out.WriteLine("[DEBUG] PLAY clicked.");
#region UWP
#if NETFX_CORE
            await PlayRecordedAudio(Dispatcher);
#endif
#endregion
#region ANDROID

#if __ANDROID__

            if (player == null)
            {
                player = new Android.Media.MediaPlayer();
            }
            else
            {
                player.Reset();
            }
            Console.Out.WriteLine("[DEBUG] Playing the recording.");
            player.SetDataSource(filePath);
            player.Prepare();
            player.Start();
#endif
#endregion
            textBlk.Text = "Replaying captured sound.";
        }

        public static void SelectFile(string imageAsDataUrl) => FileSelectedEvent?.Invoke(null, new FileSelectedEventHandlerArgs(imageAsDataUrl));

        private void OnFileSelectedEvent(object sender, FileSelectedEventHandlerArgs e)
        {
            FileSelectedEvent -= OnFileSelectedEvent;
            var base64Data = Regex.Match(e.FileAsDataUrl, @"data:audio/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
            //Console.Out.WriteLine("[DEBUG] bas64Data:");
            //Console.Out.WriteLine("[DEBUG] len: " + base64Data.Length);
            var binData = Convert.FromBase64String(base64Data); //this is the data I want
			//Console.Out.WriteLine("[DEBUG] binData:");
            //Console.Out.WriteLine("[DEBUG] len: " + binData.Length);
            //print first 100 ascii chars
            for (int i = 0; i < 100; i++)
			{
			    Console.Out.WriteLine((char)binData[i]);
			}
		}

        private static event FileSelectedEventHandler FileSelectedEvent;

        private delegate void FileSelectedEventHandler(object sender, FileSelectedEventHandlerArgs args);

        private class FileSelectedEventHandlerArgs
        {
            public string FileAsDataUrl { get; }
            public FileSelectedEventHandlerArgs(string fileAsDataUrl) => FileAsDataUrl = fileAsDataUrl;

        }

    }
}
