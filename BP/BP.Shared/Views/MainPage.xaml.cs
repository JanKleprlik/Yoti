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
using System.Text.RegularExpressions;
using Database;
using AudioProcessing.Recognizer;

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
using Xamarin.Essentials;
#endif

#if __WASM__
using Uno.Foundation;
#endif

namespace BP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

		private Shared.AudioRecorder.Recorder recorder;
		private AudioProcessing.Recognizer.AudioRecognizer recognizer;
		private bool isRecording = false;
		private bool wasRecording = false;
		private byte[] uploadedSong;
		private Database.Database database;

		public MainPage()
        {
            this.InitializeComponent();
			recorder = new Shared.AudioRecorder.Recorder();
			database = new Database.Database();
			recognizer = new AudioRecognizer();
			database.InsertDummyData();
			UpdateSongList();
            textBlk.Text = "I am ready";
		}

		private async void recordBtn_Click(object sender, RoutedEventArgs e)
        {
			if (!isRecording)
			{
				Task.Run(() => recorder.StartRecording());
				isRecording = true;
				textBlk.Text = "Called library and am recording...";
			} 
		}

		private async void stopBtn_Click(object sender, RoutedEventArgs e)
        {
			if (isRecording)
			{
				recorder.StopRecording();
				isRecording = false;
				wasRecording = true;
				textBlk.Text = "Stopped recording from lib.";
			}
        }

        private async void playBtn_Click(object sender, RoutedEventArgs e)
        {
		#region UWP
#if NETFX_CORE
			if (await recorder.ReplayRecordingUWP(Dispatcher))
			{
				textBlk.Text = "Replaying recorded sound.";				
			}
#endif
			#endregion
		#region ANDROID
#if __ANDROID__
			if (wasRecording)
			{
				recorder.ReplayRecordingANDROID();
				textBlk.Text = "Replaying recorded sound.";
			}
#endif
		#endregion
		}

		private async void recognizeBtn_Click(object sender, RoutedEventArgs e)
		{
#if !__WASM__
			AudioProcessing.Tools.Printer.Print(await recorder.GetDataFromStream());
#endif
			#region UWP
#if NETFX_CORE
			AudioProcessing.AudioFormats.WavFormat recordedAudio = new AudioProcessing.AudioFormats.WavFormat(await recorder.GetDataFromStream());

			System.Diagnostics.Debug.WriteLine("[DEBUG] Channels: " + recordedAudio.Channels);
			System.Diagnostics.Debug.WriteLine("[DEBUG] SampleRate: " + recordedAudio.SampleRate);
			System.Diagnostics.Debug.WriteLine("[DEBUG] NumOfData: " + recordedAudio.NumOfDataSamples);
			System.Diagnostics.Debug.WriteLine("[DEBUG] Data: ");
			AudioProcessing.Tools.Printer.PrintShortAsBytes(recordedAudio.Data);

#endif
			#endregion
			#region ANDROID
#if __ANDROID__
			//byte[] data = 
#endif
			#endregion

		}

		private async void uploadNewSongBtn_Click(object sender, RoutedEventArgs e)
		{
			#region UWP
#if NETFX_CORE
			var picker = new Windows.Storage.Pickers.FileOpenPicker();
			picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
			picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
			picker.FileTypeFilter.Add(".wav");

			StorageFile file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				var audioFileData = await file.OpenStreamForReadAsync();
				uploadedSong = new byte[(int)audioFileData.Length];
				audioFileData.Read(uploadedSong, 0, (int)audioFileData.Length);
				this.textBlk.Text = "Picked song: " + file.Name;
			}
			else
			{
				this.textBlk.Text = "Operation cancelled.";
			}

#endif
			#endregion

			#region ANDORID
#if __ANDROID__
			if (await getExternalStoragePermission())
			{
				PickOptions options = new PickOptions
				{
					PickerTitle = "Please select a wav song file",
					FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
					{
						{DevicePlatform.Android, new[]{"audio/x-wav"} }
					})
				};

				FileResult result = await FilePicker.PickAsync(options);

				if (result != null)
				{
					textBlk.Text = $"File selected: {result.FileName}";
					var audioFileData = await result.OpenReadAsync();
					uploadedSong = new byte[(int)audioFileData.Length];
					audioFileData.Read(uploadedSong, 0, (int)audioFileData.Length);
				}
				else
				{
					textBlk.Text = "No audio file selected";
				}

			}
			else
			{
				textBlk.Text = "Acces to read storage denied.";
			}
#endif
			#endregion
			#region WASM
#if __WASM__
			FileSelectedEvent -= OnNewSongUploadedEvent;
			FileSelectedEvent += OnNewSongUploadedEvent;
			WebAssemblyRuntime.InvokeJS(@"
				var input = document.createElement('input');
				input.type = 'file';
				input.accept = '.wav';
				input.onchange = e => {
					var file = e.target.files[0];
					//size in MBs cannot be bigger than 5
					if ((file.size / 1024 / 1024)>50){ 
						alert('File size exceeds 50 MB');
					}
					else
					{
						var reader = new FileReader();
						reader.readAsDataURL(file);
						reader.onload = readerEvent => {
							//this is the binary uploaded content
							var content = readerEvent.target.result; 
							//invoke C# method to get audio binary data
							var selectFile = Module.mono_bind_static_method(" + "\"[BP.Wasm] BP.MainPage:SelectFile\"" + @");
							selectFile(content);
						}
					};
				};
				input.click(); "
			);
#endif
			#endregion
		}

		private async void addNewSongBtn_Click(object sender, RoutedEventArgs e)
		{
			if (uploadedSong != null && nameTxtBox.Text != "" && authorTxtBox.Text != "")
			{
				var audioWav = new AudioProcessing.AudioFormats.WavFormat(uploadedSong);
				database.AddSong(nameTxtBox.Text, authorTxtBox.Text);
				var tfps = recognizer.GetTimeFrequencyPoints(audioWav);
				database.AddFingerprint(tfps);

				UpdateSongList();

				textBlk.Text = "New song added to database.";
			}
		}

		#region WASM
#if __WASM__
		private async void uploadBtn_Click(object sender, RoutedEventArgs e)
		{
            FileSelectedEvent -=OnSongToRecognizeUploadedEvent;
            FileSelectedEvent += OnSongToRecognizeUploadedEvent;
            WebAssemblyRuntime.InvokeJS(@"
				console.log('calling javascript');
				var input = document.createElement('input');
				input.type = 'file';
				input.accept = '.wav';
				input.onchange = e => {
					var file = e.target.files[0];
					//size in MBs cannot be bigger than 5
					if ((file.size / 1024 / 1024)>5){ 
						alert('File size exceeds 5 MB');
					}
					else
					{
						var reader = new FileReader();
						reader.readAsDataURL(file);
						reader.onload = readerEvent => {
							//this is the binary uploaded content
							var content = readerEvent.target.result; 
							//invoke C# method to get audio binary data
							var selectFile = Module.mono_bind_static_method(" + "\"[BP.Wasm] BP.MainPage:SelectFile\""+@");
							selectFile(content);
						}
					};
				};
				input.click(); "
			);
		}
		public static void SelectFile(string imageAsDataUrl) => FileSelectedEvent?.Invoke(null, new FileSelectedEventHandlerArgs(imageAsDataUrl));

		private void OnSongToRecognizeUploadedEvent(object sender, FileSelectedEventHandlerArgs e)
		{
			FileSelectedEvent -= OnSongToRecognizeUploadedEvent;
			var base64Data = Regex.Match(e.FileAsDataUrl, @"data:audio/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
			var binData = Convert.FromBase64String(base64Data); //this is the data I want
#if DEBUG
			for (int i = 0; i < 10; i++)
			{
				Console.Out.Write((char)binData[i]);
			}
			Console.Out.WriteLine();
#endif
			Console.Out.WriteLine("Recognize song");

		}

		private void OnNewSongUploadedEvent(object sender, FileSelectedEventHandlerArgs e)
		{
			FileSelectedEvent -= OnNewSongUploadedEvent;
			var base64Data = Regex.Match(e.FileAsDataUrl, @"data:audio/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
			var binData = Convert.FromBase64String(base64Data); //this is the data I want
#if DEBUG
			for (int i = 0; i < 10; i++)
			{
				Console.Out.Write((char)binData[i]);
			}
			Console.Out.WriteLine();
#endif
			Console.Out.WriteLine("New song");
			uploadedSong = binData;
		}

		private static event FileSelectedEventHandler FileSelectedEvent;

		private delegate void FileSelectedEventHandler(object sender, FileSelectedEventHandlerArgs args);

		private class FileSelectedEventHandlerArgs
		{
			public string FileAsDataUrl { get; }
			public FileSelectedEventHandlerArgs(string fileAsDataUrl) => FileAsDataUrl = fileAsDataUrl;

		}
#endif
		#endregion

		#region ANDROID - helper functions
#if __ANDROID__
		private async Task<bool> getExternalStoragePermission()
		{
			CancellationTokenSource source = new CancellationTokenSource();
			CancellationToken token = source.Token;
			return await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.ReadExternalStorage);
		}
#endif
#endregion
	
		private async void testBtn_Click(object sender, RoutedEventArgs e)
		{
			database.PrintDatabase();
			//if(uploadedSong != null)
			//{
			//	AudioProcessing.Tools.Printer.Print(uploadedSong);

			//	var audioWav = new AudioProcessing.AudioFormats.WavFormat(uploadedSong);
			//	System.Diagnostics.Debug.WriteLine("[DEBUG] Channels: " + audioWav.Channels);
			//	System.Diagnostics.Debug.WriteLine("[DEBUG] SampleRate: " + audioWav.SampleRate);
			//	System.Diagnostics.Debug.WriteLine("[DEBUG] NumOfData: " + audioWav.NumOfDataSamples);
			//}



		}
		private void UpdateSongList()
		{
			var songs = database.GetSongs();
			List<string> songNames = new List<string>();
			foreach(Song song in songs)
			{
				songNames.Add(song.Name);
			}
			songList.ItemsSource = songNames;
		}
	}
}
