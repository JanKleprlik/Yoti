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
using Windows.UI.Xaml.Media.Animation;
using System.Diagnostics;

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

namespace BP.Shared.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

		private AudioRecorder.Recorder recorder;
		private AudioRecognizer recognizer;

		private object uploadedSongLock = new object();
		private byte[] uploadedSong { get; set; }

		private Database.Database database { get; set; }
		private Dictionary<uint, List<ulong>> songValueDatabase;

		private Storyboard flickerAnimation;

		public MainPage()
        {
            this.InitializeComponent();

			Stopwatch sw = new Stopwatch();
			sw.Start();
			recorder = new Shared.AudioRecorder.Recorder();
			
			sw.Stop();
			System.Diagnostics.Debug.WriteLine($"[DEBUG] RECORDER elapsed time {sw.ElapsedMilliseconds}");
			sw.Reset();
			sw.Start();

			database = new Database.Database();

			sw.Stop();
			System.Diagnostics.Debug.WriteLine($"[DEBUG] DATABASE elapsed time {sw.ElapsedMilliseconds}");
			sw.Reset();
			sw.Start();

			recognizer = new AudioRecognizer();

			sw.Stop();
			System.Diagnostics.Debug.WriteLine($"[DEBUG] RECOGNIZER elapsed time {sw.ElapsedMilliseconds}");
			sw.Reset();
			sw.Start();

			songValueDatabase = database.GetSearchData();

			sw.Stop();
			System.Diagnostics.Debug.WriteLine($"[DEBUG] GET SEARCH DATA elapsed time {sw.ElapsedMilliseconds}");
			sw.Reset();
			sw.Start();

			setupFlickerAnimation();

			sw.Stop();
			System.Diagnostics.Debug.WriteLine($"[DEBUG] FLICKER ANIMATON elapsed time {sw.ElapsedMilliseconds}");

		}

		private void setupFlickerAnimation()
		{
			flickerAnimation = new Storyboard();
#if NETXF_CORE
			DoubleAnimation opacityAnimation = new DoubleAnimation()
			{
				From = 0.0,
				To = 1.0,
				BeginTime = TimeSpan.FromSeconds(1.0),
				AutoReverse = true,
				Duration = new Duration(TimeSpan.FromSeconds(0.18))
			};

			Storyboard.SetTarget(flickerAnimation, flickerIcon);
			Storyboard.SetTargetProperty(flickerAnimation, "Opacity");
			flickerAnimation.Children.Add(opacityAnimation);
			flickerAnimation.RepeatBehavior = RepeatBehavior.Forever;
#endif
#if !NETXF_CORE
			// Android nor WASM supports animations in Uno Platform yet
			flickerIcon.Opacity = 1.0;
#endif

		}

		private async void RecognizeBtn_Click(object sender, RoutedEventArgs e)
        {
			//start UI recording response
			flickerIcon.Visibility = Visibility.Visible;
			flickerAnimation.Begin();

			InformationTextBlk.Text = "BEFORE rec.";
			await Task.Run(recorder.RecordAudio);

			InformationTextBlk.Text = "AFTER rec.";

			//Stop UI recording response
			flickerAnimation.Pause();
			flickerIcon.Visibility = Visibility.Collapsed;

			//set replay button visible
			PlayBtn.Visibility = Visibility.Visible;
			
			//start UI recognition response
			RecognizeProgressBar.Visibility = Visibility.Visible;

			//recognize song
			await Task.Run( () => recognizeBtn_Click(sender, e));
			
			//stop UI recognition response
			RecognizeProgressBar.Visibility = Visibility.Collapsed;
		}

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
#if NETFX_CORE
			recorder.ReplayRecordingUWP(Dispatcher);
#endif
#if __ANDROID__
			recorder.ReplayRecordingANDROID();
#endif
		}

		private async void recognizeBtn_Click(object sender, RoutedEventArgs e)
		{
			AudioProcessing.AudioFormats.IAudioFormat recordedAudioWav;
#if NETFX_CORE
			recordedAudioWav = await getAudioFormatFromRecodingUWP();
#endif
#if __ANDROID__
			recordedAudioWav = await getAudioFormatFromRecordingANDROID();
#endif

#region WASM
#if __WASM__
			throw new NotImplementedException("Song recognition is not yet implemented in WASM");
#endif
#endregion

			System.Diagnostics.Debug.WriteLine("[DEBUG] Channels: " + recordedAudioWav.Channels);
			System.Diagnostics.Debug.WriteLine("[DEBUG] SampleRate: " + recordedAudioWav.SampleRate);
			System.Diagnostics.Debug.WriteLine("[DEBUG] NumOfData: " + recordedAudioWav.NumOfDataSamples);
			System.Diagnostics.Debug.WriteLine("[DEBUG] ActualNumOfData: " + recordedAudioWav.Data.Length);


			uint? ID = await Task.Run(() => recognizer.RecognizeSong(recordedAudioWav, songValueDatabase));

			System.Diagnostics.Debug.WriteLine($"[DEBUG] ID of recognized song is { ID }");

		}

		private async void UploadNewSongBtn_Click(object sender, RoutedEventArgs e)
		{
#if NETFX_CORE
			pickAndUploadFileUWPAsync();
#endif
#if __ANDROID__
			pickAndUploadFileANDROIDAsync();
#endif
#if __WASM__
			FileSelectedEvent -= OnNewSongUploadedEvent;
			FileSelectedEvent += OnNewSongUploadedEvent;
			WebAssemblyRuntime.InvokeJS(@"
				var input = document.createElement('input');
				input.type = 'file';
				input.accept = '.wav';
				input.onchange = e => {
					var file = e.target.files[0];
					//size in MBs cannot be bigger than 50
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
		}


		private async void AddNewSongBtn_Click(object sender, RoutedEventArgs e)
		{
			
			if (NewSongNameTB.Text == "")
			{
				displayInfoText("Please enter song name.");
				return;
			}
			if (NewSongAuthorTB.Text == "")
			{
				displayInfoText("Please enter song author.");
				return;
			}
			if (uploadedSong == null)
			{
				displayInfoText("Please upload song file.");
				return;
			}


			if (uploadedSong != null && NewSongNameTB.Text != "" && NewSongAuthorTB.Text != "")
			{
				string songName = NewSongNameTB.Text;
				string songAuthor = NewSongAuthorTB.Text;
				
				await Task.Run(() => addNewSong(songName, songAuthor));

				displayInfoText($"\"{songName}\" by \"{songAuthor}\" was added");
			}
			
		}

		private void addNewSong(string songName, string songAuthor)
		{
			System.Diagnostics.Debug.WriteLine($"[DEBUG] Adding {songName} by {songAuthor} into database.");
			
			AudioProcessing.AudioFormats.IAudioFormat audioWav;
			lock (uploadedSongLock)
			{
				audioWav = new AudioProcessing.AudioFormats.WavFormat(uploadedSong);
			}

			var tfps = recognizer.GetTimeFrequencyPoints(audioWav);
			uint songID = database.AddSong(songName, songAuthor);
			database.AddFingerprint(tfps);
			System.Diagnostics.Debug.WriteLine($"[DEBUG] DS.Count BEFORE:{songValueDatabase.Count}");
			recognizer.AddTFPToDataStructure(tfps, songID, songValueDatabase);
			database.UpdateSearchData(songValueDatabase);
			System.Diagnostics.Debug.WriteLine($"[DEBUG] DS.Count AFTER :{songValueDatabase.Count}");
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
		public static void SelectFile(string fileAsDataUrl) => FileSelectedEvent?.Invoke(null, new FileSelectedEventHandlerArgs(fileAsDataUrl));

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
			recordedSong = binData;
			Console.Out.WriteLine("Recognize song");
		}

		private void OnNewSongUploadedEvent(object sender, FileSelectedEventHandlerArgs e)
		{
			FileSelectedEvent -= OnNewSongUploadedEvent;
			var base64Data = Regex.Match(e.FileAsDataUrl, @"data:audio/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
			uploadedSong = Convert.FromBase64String(base64Data); //this is the data I want
#if DEBUG
			for (int i = 0; i < 10; i++)
			{
				Console.Out.Write((char)uploadedSong[i]);
			}
			Console.Out.WriteLine();
#endif
			Console.Out.WriteLine("New song");
			//uploadedSong = binData;
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

		// UI Navigation
		private void ListSongsBtn_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(SongList), database.GetSongs());
		}

		private  void OpenNewSongForm_Click(object sender, RoutedEventArgs e)
		{
			showAddNewSongUI();
		}
		
		private void CancelNewSong_Click(object sender, RoutedEventArgs e)
		{
			hideAddNewSongUI();
		}

		private async void SettingsBtn_Click(object sender, RoutedEventArgs e)
		{
			settingsContentDialog.Visibility = Visibility.Visible;
			ContentDialogResult result = await settingsContentDialog.ShowAsync();
		}


#region UI HELPERS
		private void hideAddNewSongUI()
		{
			UploadGrid.Visibility = Visibility.Collapsed;
			OpenNewSongFormBtn.Visibility = Visibility.Visible;
			ListSongsBtn.Visibility = Visibility.Visible;
		}

		private void showAddNewSongUI()
		{
			UploadGrid.Visibility = Visibility.Visible;
			OpenNewSongFormBtn.Visibility = Visibility.Collapsed;
			ListSongsBtn.Visibility = Visibility.Collapsed;
		}
		
		private void displayInfoText(string text)
		{
			InformationTextBlk.Text = text;
		}

#endregion
	}
}
