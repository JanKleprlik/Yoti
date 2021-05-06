using BP.Shared.AudioRecorder;
using BP.Shared.Utils;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using BP.Shared.Models;
using System.Threading.Tasks;
using System;
using AudioRecognitionLibrary.AudioFormats;
using Windows.UI.Core;
using System.Collections.Generic;
using Database;
using BP.Shared.RestApi;
using AudioRecognitionLibrary.Recognizer;
using System.Linq;
using System.IO;
using BP.Shared.Views;

namespace BP.Shared.ViewModels
{
	public partial class MainPageViewModel : BaseViewModel
	{
		#region private fields

		private Recorder audioRecorder { get; set; }
		private AudioRecognizer recognizer { get; set; }
		private TextBlockTextWriter textWriter { get; set; }
		private Settings settings { get; set; }
		private CoreDispatcher UIDispatcher { get; set; }

		private byte[] uploadedSong { get; set; }
		private IAudioFormat uploadedSongFormat { get; set; }
		private object uploadedSongLock = new object();

		#endregion

		public RecognizerApi RecognizerApi = new RecognizerApi();


		public MainPageViewModel(TextBlock outputTextBlock, Settings settings, CoreDispatcher UIDispatcher)
		{
			//Text writer to write recognition info into
			textWriter = new TextBlockTextWriter(outputTextBlock);
			this.settings = settings;
			this.UIDispatcher = UIDispatcher; 
			
			audioRecorder = new Recorder();
			recognizer = new AudioRecognizer(textWriter);

		}

		#region Commands

		public async void RecognizeSong()
		{	
			//Setup UI
			bool sucessfullRecordingUpload = true;
			WasRecognized = false;
			textWriter.Clear();


			InformationText = settings.UseMicrophone ? "Recording ..." : "Uploading file ... ";
			if (settings.UseMicrophone)
			{
				IsRecording = true;
				sucessfullRecordingUpload = await Task.Run(() => audioRecorder.RecordAudio(settings.RecordingLength));
				IsRecording = false;


				//Inform user about fail
				if (!sucessfullRecordingUpload)
				{
					InformationText = " Recording failed.";
				}
				//Allow recording replay
				else
				{
					FinishedRecording = true;
				}
			}
			else
			{
				try
				{
					sucessfullRecordingUpload = await audioRecorder.UploadRecording(value => InformationText = value);
				}
				catch(FileLoadException e)
				{
					sucessfullRecordingUpload = false;
					InformationText = "Could not upload file.";
				}
			}

			if (sucessfullRecordingUpload)
				FinishedRecording = true;


			if (sucessfullRecordingUpload)
			{
				IsRecognizing = true;
				InformationText = "Looking for a match ...";

				//null if there was problem with the recording
				RecognitionResult recognitionResult = await RecognizeSongFromRecording();

				//Display result only on Windows and Android
				//WASM is handled in event see MainPageViewModelWASM.cs
	#if __ANDROID__ || NETFX_CORE
				if (recognitionResult != null)
					WriteRecognitionResults(recognitionResult);
	#endif
				IsRecognizing = false;
			}
		}

		public void ReplayRecording()
		{
#if NETFX_CORE
			audioRecorder.ReplayRecordingUWP(UIDispatcher);
#endif
#if __ANDROID__
			audioRecorder.ReplayRecordingANDROID();
#endif
		}

		public async void UploadNewSong()
		{
#if NETFX_CORE || __ANDROID__
			byte[] uploadedSong = await FileUpload.pickAndUploadFileAsync(value => UploadedSongText = value, uploadedSongLock, maxSize_Mb:50);
			
			//file not picked
			if (uploadedSong == null)
				return;

			try
			{
				uploadedSongFormat = new WavFormat(uploadedSong);
				if (!IsSupported(uploadedSongFormat))
				{
					//release resources
					uploadedSongFormat = null;
					return;
				}
			}
			catch(ArgumentException e)
			{
				this.Log().LogError(e.Message);
				InformationText = "Problem with uploaded wav file occured." + Environment.NewLine + "Please try a different audio file.";
				return;
			}

#elif __WASM__
			await pickAndUploadFileWASM();
#else
			throw new NotImplementedException();
#endif
		}

		public async void AddNewSong()
		{
			//Reset UI
			WasRecognized = false;

			if (NewSongName.IsNullOrEmpty())
			{
				InformationText = "Please enter song name.";
				return;
			}
			if (NewSongAuthor.IsNullOrEmpty())
			{
				InformationText = "Please enter song author.";
				return;
			}
			if (uploadedSongFormat == null)
			{
				InformationText = "Please upload song file.";
				return;
			}

			if (NewSongLyrics.IsNullOrEmpty())
			{
				InformationText = "Please enter the lyrics.";
				return;
			}

			if (uploadedSongFormat != null && NewSongName != "" && NewSongAuthor!= "")
			{
				string songName = NewSongName;
				string songAuthor = NewSongAuthor;
				//unify end of line marks in database as '\r\n' so it can be replaced
				//by Environment.NewLine in runtime depending on the system
				string lyrics = NewSongLyrics.Replace('\r','\n').Replace("\n\n","\n").Replace("\n", "\r\n");

				IsUploading = true;
				InformationText = "Processing song";



				bool wasAdded = await AddNewSongToDatabase(songName, songAuthor, lyrics);
				IsUploading = false;

				if (wasAdded)
					InformationText = $"\"{songName}\" by {songAuthor} added";
				
				//close UI form
				CloseNewSongForm();

				//release resources and reset form input
				uploadedSongFormat = null;
				UploadedSongText = "Please upload audio file";
				NewSongAuthor = string.Empty;
				NewSongName = string.Empty;

				//Force GC to avoid memory struggles on older phones
				GC.Collect();
			}
		}

		public void OpenNewSongForm()
		{
			ShowUploadUI = true;
		}

		public void CloseNewSongForm()
		{
			ShowUploadUI = false;
		}

		public async void ShowLyrics()
		{
			if (RecognizedSong == null)
			{
				var lyricsShowDialog = new LyricsShowDialog("No record.", "Lyrics");
				await lyricsShowDialog.ShowAsync();
			}
			else
			{
				var lyricsShowDialog = new LyricsShowDialog(RecognizedSong.lyrics, RecognizedSong.name);
				await lyricsShowDialog.ShowAsync();
			}
		}

		public async void TestMethod()
		{
			this.Log().LogDebug("DEBUG");
		}
		#endregion

		#region Properties

		private string _informationText = "You can start recording";
		public string InformationText
		{
			get => _informationText;

			set
			{
				_informationText = value;
				OnPropertyChanged();
			}

		}

		private string _uploadedSongText = "Please upload audio file";
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

		public Song RecognizedSong = null;
		#endregion


		#region private Methods

		private async Task<RecognitionResult> RecognizeSongFromRecording()
		{
			try
			{
#if NETFX_CORE
				uploadedSongFormat = await getAudioFormatFromRecodingUWP();
#elif __ANDROID__
				uploadedSongFormat= await getAudioFormatFromRecordingANDROID();
#elif __WASM__
				recognizeWASM();
				//Return null just to comply with method (recognition handling is done in recognizeWASM();
				return null; 
#else
				throw new NotImplementedException("RecognizeSongFromRecording feature is not implemented on your platform.");
#endif

				if (!IsSupported(uploadedSongFormat))
				{
					//release resources
					uploadedSongFormat = null;
					return null;
				}

				//Name, Author and Lyrics is not important for recognition call
				SongWavFormat songWavFormat = CreateSongWavFormat("none", "none", "none");
				
				return await RecognizerApi.RecognizeSong(songWavFormat);
			}
			catch(ArgumentException e)
			{
				InformationText = "Problem with uploaded wav file occured." + Environment.NewLine + " Please try a different audio file.";
				return null;
			}
		}

		private void WriteRecognitionResults(RecognitionResult result)
		{
			textWriter.WriteLine(result.detailinfo);
			if (result.song == null)
			{
				InformationText = $"Song was not recognized.";
				return;
			}

			InformationText = $"\"{result.song.name}\" by {result.song.author}";
			YouTubeLink = CreateYouTubeLink(result.song.name, result.song.author);
			RecognizedSong = result.song;
			WasRecognized = true;
		}

		private Uri CreateYouTubeLink(string songName, string songAuthor)
		{
			return new Uri($"https://music.youtube.com/search?q={songName.Replace(' ','+')}+by+{songAuthor.Replace(' ','+')}");
		}

		private async Task<bool> AddNewSongToDatabase(string songName, string songAuthor, string lyrics)
		{
			this.Log().LogDebug($"[DEBUG] Adding {songName} by {songAuthor} into database.");
			try
			{
				if (!IsSupported(uploadedSongFormat))
				{
					//release resources
					uploadedSongFormat = null;
					return false;
				}

				SongWavFormat songWavFormat = CreateSongWavFormat(songAuthor, songName, lyrics);

				this.Log().LogDebug("Calling rest api");
				await RecognizerApi.UploadSong(songWavFormat).ConfigureAwait(false);

				this.Log().LogDebug($"[DEBUG] Song {songName} by {songAuthor} added into database.");

				return true;
			}
			catch(ArgumentException e)
			{
				this.Log().LogError(e.Message);
				InformationText = "Problem with uploaded wav file occured." + Environment.NewLine + "Please try a different audio file.";
				return false;
			}
		}

		private bool IsSupported(IAudioFormat audioFormat)
		{

			if (!Array.Exists(settings.SupportedAudioFormats, element => element.Equals(audioFormat.GetType())))
			{
				InformationText = $"Unsupported audio format: {audioFormat.GetType()}" + Environment.NewLine + $"Supported audio formats: {string.Join<Type>(", ", settings.SupportedAudioFormats)}";
				return false;
			}

			if (!Array.Exists(settings.SupportedNumbersOfChannels, element => element == audioFormat.Channels))
			{
				InformationText = $"Unsupported number of channels: {audioFormat.Channels}" + Environment.NewLine + $"Supported numbers of channels: {string.Join<int>(", ", settings.SupportedNumbersOfChannels)}";
				return false;
			}
			
			if (!Array.Exists(settings.SupportedSamplingRates, element => element == audioFormat.SampleRate))
			{
				InformationText = $"Unsupported sampling rate: {audioFormat.SampleRate}" + Environment.NewLine + $"Supported sampling rates: {string.Join<int>(", ", settings.SupportedSamplingRates)}";
				return false;
			}

			return true;
		}

		private SongWavFormat CreateSongWavFormat(string songAuthor, string songName, string lyrics)
		{

			int BPM = recognizer.GetBPM(uploadedSongFormat, approximate: true);
			this.Log().LogInformation($"BPM: {BPM}");

			var tfps = recognizer.GetTimeFrequencyPoints(uploadedSongFormat);

			this.Log().LogDebug($"tfps.Count: {tfps.Count}");

			for(int i = 0; i < Math.Min(10, tfps.Count); i++)
			{
				this.Log().LogDebug($"i: {i} freq: {tfps[i].Frequency}, time: {tfps[i].Time}");
			}

			SongWavFormat songWavFormat = new SongWavFormat
			{
				author = songAuthor,
				name = songName,
				lyrics = lyrics,
				bpm = BPM,
				tfps = tfps
			};

			return songWavFormat;
		}

#endregion
	}


}
