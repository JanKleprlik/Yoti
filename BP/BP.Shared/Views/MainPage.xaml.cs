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

		private DatabaseSQLite database { get; set; }
		private Dictionary<uint, List<ulong>> songValueDatabase;

		private Storyboard flickerAnimation;
		private SettingsDialog settingsDialog;
		private TextBlockTextWriter textWriter { get; set; }

		private SettingsViewModel SettingsVM;
		private MainPageViewModel MainPageVM;

		public MainPage()
        {
            this.InitializeComponent();

			textWriter = new TextBlockTextWriter(outputTextBox);

			recorder = new AudioRecorder.Recorder();
			database = new DatabaseSQLite();
			recognizer = new AudioRecognizer(textWriter);
			settings = new Settings();
			SettingsVM = new SettingsViewModel(settings);
			songValueDatabase = database.GetSearchData();

			MainPageVM = new MainPageViewModel(outputTextBox, settings);

			setupFlickerAnimation();
			setupSettingsDialog(settings);
		}

		/*
		public void TestMethod(object sender, RoutedEventArgs e)
		{
			this.Log().LogInformation("INFORMATION");
			this.Log().LogDebug("DEBUG");
		}
		*/
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
			settingsDialog = new SettingsDialog(SettingsVM);
			Grid.SetRowSpan(settingsDialog, 3);
			Grid.SetColumnSpan(settingsDialog, 3);			
		}

		//PORTED
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
		//PORTED
        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
#if NETFX_CORE
			
			recorder.ReplayRecordingUWP(Dispatcher);
#endif
#if __ANDROID__
			recorder.ReplayRecordingANDROID();
#endif
		}
		//PORTED
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

#region Upload new song 


		//PORTED
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



#endregion

		// THIS WILL STAY
		private async void SettingsBtn_Click(object sender, RoutedEventArgs e)
		{
			ContentDialogResult result = await settingsDialog.ShowAsync();
		}
		private void ListSongsBtn_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(SongList), database.GetSongs());
		}

#region UI HELPERS

		private void displayInfoText(string text)
		{
			InformationTextBlk.Text = text;
		}
#endregion
	}
}
