using BP.Shared.AudioRecorder;
using BP.Shared.Utils;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using BP.Shared.Models;
using System.Threading.Tasks;
using System;
using AudioProcessing.AudioFormats;
using Windows.UI.Core;
using System.Collections.Generic;
using Database;
using BP.Shared.RestApi;
using AudioProcessing.Recognizer;
using System.Linq;

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
			bool sucessfullRecordingUpload = true;
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
				sucessfullRecordingUpload = await audioRecorder.UploadRecording(value => InformationText = value);
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

		//PORTED
		public async void UploadNewSong()
		{
#if NETFX_CORE || __ANDROID__
			byte[] uploadedSong = await FileUpload.pickAndUploadFileAsync(value => UploadedSongText = value, uploadedSongLock, maxSize_Mb:50);
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
				InformationText = "Problem with uploaded wav file occured.\nPlease try a different audio file.";
				return;
			}

#elif __WASM__
			await pickAndUploadFileWASM();
#else
			throw new NotImplementedException();
#endif
		}

		//PORTED
		public async void AddNewSong()
		{
			if (NewSongName == "")
			{
				InformationText = "Please enter song name.";
				return;
			}
			if (NewSongAuthor == "")
			{
				InformationText = "Please enter song author.";
				return;
			}
			if (uploadedSongFormat == null)
			{
				InformationText = "Please upload song file.";
				return;
			}

			if (uploadedSongFormat != null && NewSongName != "" && NewSongAuthor!= "")
			{
				string songName = NewSongName;
				string songAuthor = NewSongAuthor;

				IsUploading = true;
				InformationText = "Processing song";

				bool wasAdded = await AddNewSongToDatabase(songName, songAuthor);
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

		public bool IsProcessingRecognition => IsRecognizing || IsRecording;

		public bool IsRecognizingOrUploading => IsRecognizing || IsRecording || IsUploading;

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

				//Name and Author is not important for recognition call
				SongWavFormat songWavFormat = CreateSongWavFormat("none", "none");
				
				return await RecognizerApi.RecognizeSong(songWavFormat);
			}
			catch(ArgumentException e)
			{
				InformationText = "Problem with uploaded wav file occured.\nPlease try a different audio file.";
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
		}

		private async Task<bool> AddNewSongToDatabase(string songName, string songAuthor)
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

				SongWavFormat songWavFormat = CreateSongWavFormat(songAuthor, songName);

				this.Log().LogDebug("Calling rest api");
				await RecognizerApi.UploadSong(songWavFormat).ConfigureAwait(false);

				this.Log().LogDebug($"[DEBUG] Song {songName} by {songAuthor} added into database.");

				return true;
			}
			catch(ArgumentException e)
			{
				this.Log().LogError(e.Message);
				InformationText = "Problem with uploaded wav file occured.\nPlease try a different audio file.";
				return false;
			}
		}

		private bool IsSupported(IAudioFormat audioFormat)
		{

			if (!Array.Exists(settings.SupportedAudioFormats, element => element.Equals(audioFormat.GetType())))
			{
				InformationText = $"Unsupported audio format: {audioFormat.GetType()}\nSupported audio formats: {string.Join<Type>(", ", settings.SupportedAudioFormats)}";
				return false;
			}

			if (!Array.Exists(settings.SupportedNumbersOfChannels, element => element == audioFormat.Channels))
			{
				InformationText = $"Unsupported number of channels: {audioFormat.Channels}\nSupported numbers of channels: {string.Join<int>(", ", settings.SupportedNumbersOfChannels)}";
				return false;
			}
			
			if (!Array.Exists(settings.SupportedSamplingRates, element => element == audioFormat.SampleRate))
			{
				InformationText = $"Unsupported sampling rate: {audioFormat.SampleRate}\nSupported sampling rates: {string.Join<int>(", ", settings.SupportedSamplingRates)}";
				return false;
			}

			return true;
		}

		private SongWavFormat CreateSongWavFormat(string songAuthor, string songName)
		{
			int BPM = recognizer.GetBPM(uploadedSongFormat, approximate: true);
			this.Log().LogInformation($"BPM: {BPM}");
			var tfps = recognizer.GetTimeFrequencyPoints(uploadedSongFormat);

			SongWavFormat songWavFormat = new SongWavFormat
			{
				author = songAuthor,
				name = songName,
				bpm = BPM,
				tfps = tfps
			};

			return songWavFormat;
		}

#endregion
	}


}
