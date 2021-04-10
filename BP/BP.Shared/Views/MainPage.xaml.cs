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
using System.Text;
using BP.Shared.Models;
using BP.Shared.ViewModels;
using Microsoft.Extensions.Logging;
using Uno.Extensions;


#if NETFX_CORE
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
#endif

#if __ANDROID__
using System.Diagnostics;
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
		private Settings settings;

		private object uploadedSongLock = new object();
		private byte[] uploadedSong { get; set; }

		private Database.Database database { get; set; }
		private Dictionary<uint, List<ulong>> songValueDatabase;

		private Storyboard flickerAnimation;
		private SettingsDialog settingsDialog;
		private CustomTextWriter textWriter;

		private SettingsViewModel settingsViewModel;

		public MainPage()
        {
            this.InitializeComponent();

			textWriter = new CustomTextWriter(outputTextBox);

			recorder = new AudioRecorder.Recorder();
			database = new Database.Database();
			recognizer = new AudioRecognizer(textWriter);
			settings = new Settings();
			settingsViewModel = new SettingsViewModel(settings);
			songValueDatabase = database.GetSearchData();
			

			setupFlickerAnimation();
			setupSettingsDialog(settings);
		}

		public void TestMethod(object sender, RoutedEventArgs e)
		{
			this.Log().LogInformation("INFORMATION");
			this.Log().LogDebug("DEBUG");
		}

		private void setupFlickerAnimation()
		{
			flickerAnimation = new Storyboard();
#region UWP
#if NETFX_CORE
			DoubleAnimation opacityAnimation = new DoubleAnimation()
			{
				From = 1.0,
				To = 0.0,
				BeginTime = TimeSpan.FromSeconds(0.5),
				AutoReverse = true,
				Duration = new Duration(TimeSpan.FromSeconds(0.18))
			};

			Storyboard.SetTarget(flickerAnimation, flickerIcon);
			Storyboard.SetTargetProperty(flickerAnimation, "Opacity");
			flickerAnimation.Children.Add(opacityAnimation);
			flickerAnimation.RepeatBehavior = RepeatBehavior.Forever;
#endif
#endregion

#if !NETFX_CORE
			// Android nor WASM supports animations in Uno Platform yet
			flickerIcon.Opacity = 1.0;
#endif

		}
		private void setupSettingsDialog(Settings settings)
		{
			settingsDialog = new SettingsDialog(settingsViewModel);
			settingsDialog.Visibility = Visibility.Collapsed;
			Grid.SetRowSpan(settingsDialog, 3);
			Grid.SetColumnSpan(settingsDialog, 3);			
		}

		private async void RecognizeBtn_Click(object sender, RoutedEventArgs e)
        {
			outputTextBox.Text = "";
#if __ANDROID__
			outputTextBox.Visibility = settings.DetailedInfo ? Visibility.Visible : Visibility.Collapsed;
#endif
#if !__WASM__
			//start UI recording response
			flickerIcon.Visibility = Visibility.Visible;
			flickerAnimation.Begin();
			RecognizeBtn.IsEnabled = false;

			//record audio
			displayInfoText("Recording...");
			await Task.Run( () => recorder.RecordAudio(settings.RecordingLength));

			//Stop UI recording response
			flickerAnimation.Pause();
			flickerIcon.Visibility = Visibility.Collapsed;

			//set replay button visible
			PlayBtn.Visibility = Visibility.Visible;
			
			//start UI recognition response
			RecognizeProgressBar.Visibility = Visibility.Visible;
#endif
			//recognize song
			await Task.Run( () => RecognizeFromRecording());
#if !__WASM__
			//stop UI recognition response
			RecognizeBtn.IsEnabled = true;
			RecognizeProgressBar.Visibility = Visibility.Collapsed;
#endif
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

		private async Task RecognizeFromRecording()
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
			recognizeWASM();
			return;
#endif
#endregion

			uint? ID = await Task.Run(() => recognizer.RecognizeSong(recordedAudioWav, songValueDatabase));
			await WriteRecognitionResults(ID);

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
			pickAndUploadFileWASM();
#endif
		}

#region Upload new song 

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

		private async Task WriteRecognitionResults(uint? ID)
		{
			if (ID == null)
			{
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				{
					displayInfoText($"Song was not recognized.");
				});
				return;
			}

			Song song;
			try
			{
				song = database.GetSongByID((uint)ID);
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				{
					displayInfoText($"\"{song.Name}\"\tby\t{song.Author}");
				});
			}
			catch(ArgumentException e)
			{
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				{
					displayInfoText(e.Message);
				});
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


#endregion

		// UI Navigation
		private async void SettingsBtn_Click(object sender, RoutedEventArgs e)
		{
			settingsDialog.Visibility = Visibility.Visible;
			ContentDialogResult result = await settingsDialog.ShowAsync();
		}
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


#region UI HELPERS

		private void displayInfoText(string text)
		{
			InformationTextBlk.Text = text;
		}

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



#endregion
	}
}
