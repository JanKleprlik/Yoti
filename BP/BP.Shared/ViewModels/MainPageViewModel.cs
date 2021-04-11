using BP.Shared.AudioRecorder;
using BP.Shared.Utils;
using AudioProcessing.Recognizer;
using Database;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using BP.Shared.Models;
using System.Threading.Tasks;
using System;
using AudioProcessing.AudioFormats;
using Windows.UI.Core;
using System.Collections.Generic;

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
		/// <summary>
		/// TODO: pořádně popsat co to je !!!
		/// </summary>
		private Dictionary<uint, List<ulong>> savedSongs { get; set; }

		private byte[] uploadedSong { get; set; }
		private object uploadedSongLock = new object();

		#endregion


		public DatabaseSQLite Database { get; private set; }

		public MainPageViewModel(TextBlock outputTextBlock, Settings settings, CoreDispatcher UIDispatcher)
		{
			//Text writer to write recognition info into
			textWriter = new TextBlockTextWriter(outputTextBlock);
			this.settings = settings;
			this.UIDispatcher = UIDispatcher; 
			
			audioRecorder = new Recorder();
			Database = DatabaseSQLite.Instance;
			recognizer = new AudioRecognizer(textWriter);

			//Popsat proč to tady dělám
			savedSongs = Database.GetSearchData();

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

				uint? recognizedSongID = await Task.Run(() => RecognizeSongFromRecording());

				//Display result only on Windows and Android
				//WASM is handled in event see MainPageViewModelWASM.cs
	#if __ANDROID__ || NETFX_CORE
				WriteRecognitionResults(recognizedSongID);
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
			uploadedSong = await FileUpload.pickAndUploadFileAsync(value => UploadedSongText = value, uploadedSongLock, maxSize_Mb:50);
#elif __WASM__
			await pickAndUploadFileWASM();
#else
			throw new NotImplementedException();
#endif
		}

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
			if (uploadedSong == null)
			{
				InformationText = "Please upload song file.";
				return;
			}

			if (uploadedSong != null && NewSongName != "" && NewSongAuthor!= "")
			{
				string songName = NewSongName;
				string songAuthor = NewSongAuthor;

				IsUploading = true;
				InformationText = "Processing song";
				Task adderTask = Task.Run(() => AddNewSongToDatabase(songName, songAuthor));
				await adderTask;
				
				IsUploading = false;
				if (adderTask.Status == TaskStatus.Faulted)
					InformationText = "Something went wrong...\nSong could not be uploaded.";
				else
					InformationText = $"\"{songName}\" by {songAuthor} added";
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

		public void TestMethod()
		{
			this.Log().LogDebug("DEBUG");
			this.Log().LogDebug(settings.ToString());
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

		private async Task<uint?> RecognizeSongFromRecording()
		{
			IAudioFormat recordedAudioWav;
			try
			{
#if NETFX_CORE
				recordedAudioWav = await getAudioFormatFromRecodingUWP();
#elif __ANDROID__
				recordedAudioWav = await getAudioFormatFromRecordingANDROID();
#elif __WASM__
				recognizeWASM();
				//Return 0 just to comply with method (recognition handling is done in recognizeWASM();
				return 0; 
#else
				throw new NotImplementedException("RecognizeSongFromRecording feature is not implemented on your platform.");
#endif
				return await Task.Run(() => recognizer.RecognizeSong(recordedAudioWav, savedSongs));
			}
			catch(InvalidOperationException e)
			{
				this.Log().LogError(e.Message);
				//null as not recognized
				return null;
			}
		}

		private void WriteRecognitionResults(uint? ID)
		{
			if (ID == null)
			{
				InformationText = $"Song was not recognized.";
				return;
			}

			try
			{
				Song song = Database.GetSongByID((uint)ID);
				InformationText = $"\"{song.Name}\" by {song.Author}";
			}
			catch (ArgumentException e)
			{
				this.Log().LogError(e.Message);
				InformationText = "Song was not recognized.";
			}
		}

		private void AddNewSongToDatabase(string songName, string songAuthor)
		{
			this.Log().LogDebug($"[DEBUG] Adding {songName} by {songAuthor} into database.");

			IAudioFormat audioWav;
			try
			{
				lock (uploadedSongLock)
				{
					audioWav = new WavFormat(uploadedSong);
				}

				//TODO: popsat!!!
				var tfps = recognizer.GetTimeFrequencyPoints(audioWav);
				uint songID = Database.AddSong(songName, songAuthor);
				recognizer.AddTFPToDataStructure(tfps, songID, savedSongs);
				Database.UpdateSearchData(savedSongs);
				this.Log().LogDebug($"[DEBUG] Song {songName} by {songAuthor} added into database.");

			}
			catch(Exception e)
			{
				this.Log().LogError(e.Message);
				throw e;
			}
		}

#endregion
	}


}
