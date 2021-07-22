﻿using Yoti.Shared.AudioProvider;
using Yoti.Shared.Utils;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Yoti.Shared.Models;
using System.Threading.Tasks;
using System;
using AudioRecognitionLibrary.AudioFormats;
using Windows.UI.Core;
using System.Collections.Generic;
using SharedTypes;
using Yoti.Shared.RestApi;
using AudioRecognitionLibrary.Recognizer;
using System.Linq;
using System.IO;
using Yoti.Shared.Views;
using System.Text;

namespace Yoti.Shared.ViewModels
{
	public partial class MainPageViewModel : BaseViewModel
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="outputTextBlock">TextBlock control to write Detailed info into.</param>
		/// <param name="settings">Starting settings for the application.</param>
		/// <param name="UIDispatcher">CoreDispatcher used for dispatching replay of captured audio.</param>
		public MainPageViewModel(Settings settings, SettingsViewModel settingsViewModel, CoreDispatcher UIDispatcher)
		{
			Settings = settings;
			this.UIDispatcher = UIDispatcher;
			this.settingsViewModel = settingsViewModel;
			recognizer = new AudioRecognizer();

			// Audio Provider on WASM is handled in javascript
#if NETFX_CORE || __ANDROID__
			audioRecorder = new AudioDataProvider();
#endif
		}


		#region private properties

		/// <summary>
		/// Song recognizer
		/// </summary>
		private AudioRecognizer recognizer { get; set; }

		/// <summary>
		/// Audio Format used for song recognition.
		/// </summary>
		private IAudioFormat uploadedSong { get; set; }

		/// <summary>
		/// Audio Format used for adding new song to database.
		/// </summary>
		private IAudioFormat songToBeAddedToDatabase { get; set; }

		/// <summary>
		/// API provider for communication with server side.
		/// </summary>
		private RecognizerApi recognizerApi { get; set; } = new RecognizerApi();

		/// <summary>
		/// UI dispatcher used to replay audio recording on UWP.
		/// </summary>
		private CoreDispatcher UIDispatcher { get; set; }

		/// <summary>
		/// Settings View Model connecting Settings Page and Main Page
		/// So changes in settings pages are immediately shown on Main Page.
		/// </summary>
		private SettingsViewModel settingsViewModel { get; set; }

#if NETFX_CORE || __ANDROID__
		/// <summary>
		/// Audio provider used to provide audio data on UWP a ANDROID,
		/// both by uploading file and recording by microphone.
		/// </summary>
		private AudioDataProvider audioRecorder { get; set; }

#endif
		#endregion

		#region Commands

#if NETFX_CORE || __ANDROID__
		/// <summary>
		/// Starts recording audio or opens filepicker, validates uploaded song format, queries server for song recognition and shows result on the screen. <br></br>
		/// Also handles necessary UI updates. <br></br>
		/// UWP and ANDROID only.
		/// </summary>
		public async void RecognizeSong()
		{
			// Update UI
			bool sucessfullRecordingUpload = true;
			UpdateRecognitionUI();

			// Use microphone to record audio
			if (Settings.UseMicrophone)
			{
				IsRecording = true;
				try
				{
					sucessfullRecordingUpload = await Task.Run(() => audioRecorder.RecordAudio(Settings.RecordingLength));
				}
				catch (UnauthorizedAccessException e)
				{
					sucessfullRecordingUpload = false;
				}
				IsRecording = false;

				// Inform user about failure
				if (!sucessfullRecordingUpload)
					InformationText = " Recording failed.";
				// Allow recording replay
				else
					FinishedRecording = true;
			}
			// Upload recording via FilePicker
			else
			{
				try
				{
					// UploadRecording accepts Action that sets InformationText string
					// so results can be shown to user throughout the process of uploading. 
					sucessfullRecordingUpload = await audioRecorder.UploadRecording(value => InformationText = value, AudioDataProvider.Parameters.MaxRecordingUploadSize_MB);
				}
				catch (FileLoadException e)
				{
					// Inform user about failure
					sucessfullRecordingUpload = false;

					InformationText = "Could not upload file.";
				}
			}

			// Update UI
			if (sucessfullRecordingUpload)
				FinishedRecording = true;

			// Proceed to song recognition
			if (sucessfullRecordingUpload)
			{
				IsRecognizing = true;
				InformationText = "Looking for a match ...";

				// Recognize song
				RecognitionResult recognitionResult = await RecognizeSongFromRecording();

				// Update UI
				if (recognitionResult != null)
					await WriteRecognitionResults(recognitionResult);



				IsRecognizing = false;
			}
		}

		/// <summary>
		/// Plays last recorded audio by microphone.<br></br>
		/// Supported on UWP and ANDROID only.
		/// </summary>
		public void ReplayRecording()
		{
			audioRecorder.ReplayRecording(UIDispatcher);
		}

		/// <summary>
		/// Opens platform specific FilePicker and allows the user to pick audio file. <br></br>
		/// UWP and ANDROID only.
		/// </summary>
		public async void UploadNewSong()
		{
			// Open platform specific FilePicker
			if (!await audioRecorder.UploadRecording(value => UploadedSongText = value, AudioDataProvider.Parameters.MaxUploadSize_MB))
				return;


			byte[] uploadedSong = await audioRecorder.GetDataFromStream(); // await FileUpload.PickAndUploadFileAsync(value => UploadedSongText = value, maxSize_MB:AudioDataProvider.Parameters.MaxUploadSize_MB);

			// File not picked
			if (uploadedSong == null)
				return;

			try
			{
				songToBeAddedToDatabase = new WavFormat(uploadedSong);
				if (!IsSupported(songToBeAddedToDatabase))
				{
					// Release resources
					songToBeAddedToDatabase = null;
					return;
				}
			}
			catch (ArgumentException e)
			{
				this.Log().LogError(e.Message);
				InformationText = "Problem with uploaded wav file occured." + Environment.NewLine + "Please try a different audio file.";
				return;
			}
		}
#endif

		/// <summary>
		/// Proces picked song and upload it into the server database.
		/// </summary>
		public async void AddNewSong()
		{
			// Reset UI
			WasRecognized = false;

			// Check that all form controls are filled.
			if (NewSongName.IsNullOrEmpty())
			{
				InformationText = "Please enter the song name.";
				return;
			}
			if (NewSongAuthor.IsNullOrEmpty())
			{
				InformationText = "Please enter the song author.";
				return;
			}
			if (songToBeAddedToDatabase == null)
			{
				InformationText = "Please upload the song.";
				return;
			}


			// Lyrics is not mandatory
			// Set lyrics as "Unknown" if not provided by the user
			if (NewSongLyrics.IsNullOrEmpty())
			{
				NewSongLyrics = "Unknown";
			}

			// Update UI
			IsUploading = true;
			InformationText = "Processing song";
			CloseNewSongForm();


			// Upload song on serever
			bool wasAdded = await AddNewSongToDatabase(NewSongName, NewSongAuthor, NewSongLyrics, songToBeAddedToDatabase);

			// Update UI
			IsUploading = false;
			if (wasAdded)
				InformationText = $"\"{NewSongName}\" by {NewSongAuthor} added.";
			else
				InformationText = $"\"{NewSongName}\" by {NewSongAuthor} could not added to the database.";


			// Release resources and reset 'Add song' form data
			songToBeAddedToDatabase = null;
			UploadedSongText = "Please upload audio file";
			NewSongAuthor = string.Empty;
			NewSongName = string.Empty;

			// Force GC to avoid memory struggles on older phones
			GC.Collect();
		}

		/// <summary>
		/// Open form for adding new song into the database.
		/// </summary>
		public void OpenNewSongForm()
		{
			ShowUploadUI = true;
		}

		/// <summary>
		/// Close form for adding new song into the database.
		/// </summary>
		public void CloseNewSongForm()
		{
			ShowUploadUI = false;
		}

		/// <summary>
		/// Show Lyrics dialog for recognized song.
		/// </summary>
		public async void ShowLyrics()
		{
			// Backup info to show when no song was recognized.

			if (RecognizedSong == null)
			{
				var lyricsShowDialog = new LyricsShowDialog(new Song()
				{
					Lyrics = "No record.",
					Author = "Unknown",
					Name = "Lyrics",
					BPM = 0
				});
				await ShowDialog(lyricsShowDialog);
			}
			else
			{
				var lyricsShowDialog = new LyricsShowDialog(RecognizedSong);
				await ShowDialog(lyricsShowDialog);
			}

		}

		/// <summary>
		/// Show Settings dialog.
		/// </summary>
		public async void ShowSettings()
		{
			var settingsDialog = new SettingsDialog(settingsViewModel);
			await ShowDialog(settingsDialog);
		}

		/// <summary>
		/// Show Lyrics editor dialog.
		/// </summary>
		public async void ShowLyricsEditDialog()
		{
			var lyricsDialog = new LyricsDialog(this);
			await ShowDialog(lyricsDialog);
			
		}
		#endregion

		#region Properties

		private Settings _settings;
		/// <summary>
		/// Settings of the application
		/// </summary>
		public Settings Settings 
		{ 
			get => _settings;
			set
			{
				_settings = value;
				OnPropertyChanged();
			}				 
		}

		private string _informationText = "You can start recording";
		/// <summary>
		/// Main text displaying current state of the application.
		/// </summary>
		public string InformationText
		{
			get => _informationText;

			set
			{
				_informationText = value;
				OnPropertyChanged();
			}

		}

		private string _detailedInfoText = "";
		/// <summary>
		/// Detailed info text displaying informatioun about 
		/// the process of recognition.
		/// </summary>
		public string DetailedInfoText
		{
			get => _detailedInfoText;
			set
			{
				_detailedInfoText = value;
				OnPropertyChanged();
			}
		}

		private string _uploadedSongText = "Please upload audio file";
		/// <summary>
		/// Name of uploaded song in 'Add song' form.
		/// </summary>
		public string UploadedSongText
		{
			get => _uploadedSongText;

			set
			{
				_uploadedSongText = value;
				OnPropertyChanged();
			}

		}

		private string _newSongName;
		/// <summary>
		/// Name of the song in 'Add song' form.
		/// </summary>
		public string NewSongName
		{
			get => _newSongName;

			set
			{
				_newSongName= value;
				OnPropertyChanged();
			}

		}

		private string _newSongAuthor;
		/// <summary>
		/// Name of the author in 'Add song' form.
		/// </summary>
		public string NewSongAuthor
		{
			get => _newSongAuthor;

			set
			{
				_newSongAuthor = value;
				OnPropertyChanged();
			}

		}

		private string _newSongLyrics;
		/// <summary>
		/// Lyrics of the song in 'Add song' form.
		/// </summary>
		public string NewSongLyrics
		{
			get => _newSongLyrics;

			set
			{
				_newSongLyrics = value;
				OnPropertyChanged();
			}

		}

		private Uri _youtubeLink;
		/// <summary>
		/// YouTube link redirecting to recognized song.
		/// </summary>
		public Uri YouTubeLink
		{
			get => _youtubeLink;

			set
			{
				_youtubeLink = value;
				OnPropertyChanged();
			}

		}

		private bool _showUploadUI;
		/// <summary>
		/// Controls visibility of 'Add song' form.
		/// </summary>
		public bool ShowUploadUI
		{
			get => _showUploadUI;
			set
			{
				_showUploadUI = value;
				OnPropertyChanged();
			}
		}

		private bool _isRecording;
		/// <summary>
		/// True - Recording via microphone is in progress.<br></br>
		/// False - Otherwise.
		/// </summary>
		public bool IsRecording
		{
			get => _isRecording;
			set
			{
				_isRecording = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsProcessingRecognition));
				OnPropertyChanged(nameof(IsRecognizingOrUploading));
			}
		}

		private bool _isRecognizing;
		/// <summary>
		/// True - Recognition is in progress.<br></br>
		/// False - Otherwise.
		/// </summary>
		public bool IsRecognizing
		{
			get => _isRecognizing;
			set
			{
				_isRecognizing = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsProcessingRecognition));
				OnPropertyChanged(nameof(IsRecognizingOrUploading));
			}
		}

		private bool _isUploading;
		/// <summary>
		/// True - File uploadin is in progress.<br></br>
		/// False - Otherwise.
		/// </summary>
		public bool IsUploading
		{
			get => _isUploading;
			set
			{
				_isUploading = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsRecognizingOrUploading));
			}
		}

		private bool _finishedRecording;
		/// <summary>
		/// True - At least one microphone recording was finished.<br></br>
		/// False - Otherwise.
		/// </summary>
		public bool FinishedRecording
		{
			get => _finishedRecording;
			set
			{
				_finishedRecording = value;
				OnPropertyChanged();
			}
		}

		private bool _wasRecognized;
		/// <summary>
		/// True - Recognition was succesfull.<br></br>
		/// False - Otherwise.
		/// </summary>
		public bool WasRecognized
		{
			get => _wasRecognized;
			set
			{
				_wasRecognized = value;
				OnPropertyChanged();
			}
		}

		public bool IsProcessingRecognition => IsRecognizing || IsRecording;

		public bool IsRecognizingOrUploading => IsRecognizing || IsRecording || IsUploading;

		/// <summary>
		/// Recognized song.
		/// </summary>
		public Song RecognizedSong { get; set; } = null;
		#endregion

		#region private Methods
		/// <summary>
		/// Platform specific song recognition from recording (either uploaded or captured with microphone).
		/// </summary>
		/// <returns></returns>
		private async Task<RecognitionResult> RecognizeSongFromRecording()
		{
			try
			{
				// Obtain audio data from recording
				uploadedSong = await GetAudioFormatFromRecording();


				if (!IsSupported(uploadedSong))
				{
					//release resources
					uploadedSong = null;
					return null;
				}

				// Preprocess audio data for server
				PreprocessedSongData preprocessedSong = PreprocessSongData(uploadedSong);
				
				// Call server for song recognition
				return await recognizerApi.RecognizeSong(preprocessedSong);
			}
			catch(ArgumentException e)
			{
				this.Log().LogError(e.Message);
				InformationText = "Problem with uploaded wav file occured." + Environment.NewLine + " Please try a different audio file.";
				return null;
			}
		}
		
		/// <summary>
		/// Setups UI for recognition process
		/// </summary>
		private void UpdateRecognitionUI()
		{
			//Hide YouTube Music link and lyrics button
			WasRecognized = false;
			// Delete previous detailed info
			DetailedInfoText = "";
			//notify user about the process of recognition
			InformationText = Settings.UseMicrophone ? "Recording ..." : "Uploading file ... ";
		}

		/// <summary>
		/// Opens dialog window.
		/// </summary>
		/// <param name="dialog">Dialog to open</param>
		private async Task ShowDialog(ContentDialog dialog)
		{
			try
			{
				ContentDialogResult resutl = await dialog.ShowAsync();
			}
			catch (Exception e)
			{
				// Do not open dialog if it's already opened.
				// Only one Content dialog can be openned at the time
				// else System.Exception is thrown
				return;
			}
		}

		/// <summary>
		/// Write recognition result to adequate properties so it can be displayed to the user.
		/// </summary>
		/// <param name="result"></param>
		private async Task WriteRecognitionResults(RecognitionResult result)
		{

			// Write result of top 10 song accuracies.
			result.SongAccuracies.Sort((a, b) => b.Item2.CompareTo(a.Item2));
			StringBuilder detailedInfoSB = new StringBuilder();
			for (int i = 0; i < Math.Min(10,result.SongAccuracies.Count); i++)
			{
				var songAccuracy = result.SongAccuracies[i];
				Song song = await recognizerApi.GetSongById(songAccuracy.Item1);

				detailedInfoSB.Append($"Song \"{song?.Name ?? "Unknown"}\" is a {Math.Min(100d, songAccuracy.Item2):##.#}% match. {Environment.NewLine}");
			}

			// If song was not recognized
			if (result.Song == null)
			{
				// Display recognition failure
				InformationText = $"Song was not recognized.";
				// Hide YouTube Music link and button for lyrics
				WasRecognized = false;
				return;
			}

			// Display recognition result
			InformationText = $"\"{result.Song.Name}\" by {result.Song.Author}";
			// Display detailed info about recognition
			DetailedInfoText = detailedInfoSB.ToString();
			// Generate YouTube Music link
			YouTubeLink = CreateYouTubeLink(result.Song.Name, result.Song.Author);
			// Set recognized song so lyrics can be displayed
			RecognizedSong = result.Song;
			// Display YouTube Music link and button for lyrics
			WasRecognized = true;
		}

		/// <summary>
		/// Create YouTube link to given song created by given author
		/// </summary>
		/// <param name="songName">Name of the song.</param>
		/// <param name="songAuthor">Name of the author of the song.</param>
		/// <returns></returns>
		private Uri CreateYouTubeLink(string songName, string songAuthor)
		{
			return new Uri($"https://music.youtube.com/search?q={songName.Replace(' ','+')}+by+{songAuthor.Replace(' ','+')}");
		}

		/// <summary>
		/// Processes and uploads given song into the server database.
		/// </summary>
		/// <param name="songName">Name of the song.</param>
		/// <param name="songAuthor">Name of the author of the song.</param>
		/// <param name="lyrics">Lyrics of the song.</param>
		/// <param name="audioData">Supported instance implementing IAudioFormat containing audio data.</param>
		/// <returns>True if song was sucessfully added, false otherwise.</returns>
		private async Task<bool> AddNewSongToDatabase(string songName, string songAuthor, string lyrics, IAudioFormat audioData)
		{
			try
			{
				if (!IsSupported(audioData))
				{
					//release resources
					audioData = null;
					return false;
				}

				// Preprocess audio data so it can be uploaded to server
				PreprocessedSongData preprocessedSong = PreprocessSongData(audioData, songAuthor, songName, lyrics);

				// Upload to server - dont wait for response
				await recognizerApi.UploadSong(preprocessedSong).ConfigureAwait(false);

				return true;
			}
			catch(ArgumentException e)
			{
				this.Log().LogError(e.Message);
				InformationText = "Problem with uploaded wav file occured." + Environment.NewLine + "Please try a different audio file.";
				return false;
			}
		}

		/// <summary>
		/// Checks if given instance implementing IAudioFormat is supported. <br></br>
		/// Checks for type of auioFormat, number of audio channels and sample rate.
		/// </summary>
		/// <param name="audioFormat">Audio format instance implementing IAudioFormat.</param>
		/// <returns>True if audioFormat is supported, false otherwise.</returns>
		private bool IsSupported(IAudioFormat audioFormat)
		{

			if (!Array.Exists(Settings.SupportedAudioFormats, element => element.Equals(audioFormat.GetType())))
			{
				InformationText = $"Unsupported audio format: {audioFormat.GetType()}" + Environment.NewLine + $"Supported audio formats: {string.Join<Type>(", ", Settings.SupportedAudioFormats)}";
				return false;
			}

			if (!Array.Exists(Settings.SupportedNumbersOfChannels, element => element == audioFormat.Channels))
			{
				InformationText = $"Unsupported number of channels: {audioFormat.Channels}" + Environment.NewLine + $"Supported numbers of channels: {string.Join<int>(", ", Settings.SupportedNumbersOfChannels)}";
				return false;
			}
			
			if (!Array.Exists(Settings.SupportedSamplingRates, element => element == audioFormat.SampleRate))
			{
				InformationText = $"Unsupported sampling rate: {audioFormat.SampleRate}" + Environment.NewLine + $"Supported sampling rates: {string.Join<int>(", ", Settings.SupportedSamplingRates)}";
				return false;
			}

			return true;
		}

		/// <summary>
		/// Creates song fingerprint, gets BPM of the song and wraps it together with song name,author name and lyrics 
		/// into PreprocessedSongData instance so it can be send to the server.<br></br>
		/// songAuthor, songName and lyrics are not mandatory if we only need the fingerprint, BPM and TFPCount
		/// </summary>
		/// <param name="audioData">Supported audio format containing audio data.</param>
		/// <param name="songAuthor">Name of the song - default = "none"</param>
		/// <param name="songName">Name of the author of the song - default = "none"</param>
		/// <param name="lyrics">Lyrics of the song - default = "none"</param>
		/// <returns></returns>
		private PreprocessedSongData PreprocessSongData(IAudioFormat audioData, string songAuthor = "none", string songName = "none", string lyrics = "none")
		{
			int BPM = recognizer.GetBPM(audioData, approximate: true);

			var fingerprintNoteCountTuple = recognizer.GetAudioFingerprint(audioData);

			PreprocessedSongData songWavFormat = new PreprocessedSongData
			{
				Author = songAuthor,
				Name = songName,
				Lyrics = lyrics,
				BPM = BPM,
				Fingerprint = fingerprintNoteCountTuple.Item1,
				TFPCount = fingerprintNoteCountTuple.Item2
			};

			return songWavFormat;
		}


		/// <summary>
		/// Creates IAudioFormat instance from raw recorded audio data.<br></br>
		/// UWP only.
		/// </summary>
		/// <returns></returns>
		private async Task<IAudioFormat> GetAudioFormatFromRecording()
		{
			#region UWP
#if NETFX_CORE
			// In UWP the data in the recording includes the metadata
			// so we can read information about sample rate, channel count etc.
			// from the included metadata

			byte[] recordedSong = await audioRecorder.GetDataFromStream();
																			
			return new WavFormat(recordedSong);
#endif
			#endregion
			#region ANDROID
#if __ANDROID__
			// On Android we only get raw data without metadata.
			// So we have to convert them manually to shorts and then use different 
			// manual constructor with values from Recorder.Parameters.

			byte[] recordedSong =  await audioRecorder.GetDataFromStream();
			short[] recordedDataShort = new short[recordedSong.Length / 2];
			Buffer.BlockCopy(recordedSong, 0, recordedDataShort, 0, recordedSong.Length);


			return new WavFormat(
				AudioProvider.AudioDataProvider.Parameters.SamplingRate,
				AudioProvider.AudioDataProvider.Parameters.Channels,
				recordedDataShort.Length,
				recordedDataShort);
#endif
			#endregion
			#region WASM
#if __WASM__
			// This method is not supposed to be called from WASM project
			// because it gets audioformat from recording via AudioDataProvider
			// which is not used in WASM project.
			throw new PlatformNotSupportedException("GetAudioFromRecording method is not supported on WASM");
#endif
			#endregion
		}
		#endregion
	}

}
