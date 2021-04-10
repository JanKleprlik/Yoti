using BP.Shared.AudioRecorder;
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
			Database = new DatabaseSQLite();
			recognizer = new AudioRecognizer(textWriter);

			//Popsat proč to tady dělám
			savedSongs = Database.GetSearchData();

		}

		#region Commands

		public async void RecognizeSong()
		{
			textWriter.Clear();

			IsRecording = true;
			InformationText = "Recording ...";
			await Task.Run(() => audioRecorder.RecordAudio(settings.RecordingLength));
			IsRecording = false;

			FinishedRecording = true;


			IsRecognizing = true;
			InformationText = "Looking for a match ...";
			uint? recognizedSongID = await Task.Run(() => RecognizeSongFromRecording());
#if __ANDROID__ || NETFX_CORE
			WriteRecognitionResults(recognizedSongID);
#endif
			IsRecognizing = false;
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
#if NETFX_CORE
			await pickAndUploadFileUWPAsync();
#endif
#if __ANDROID__
			pickAndUploadFileANDROIDAsync();
#endif
#if __WASM__
			await pickAndUploadFileWASM();
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
				await Task.Run(() => AddNewSongToDatabase(songName, songAuthor));
				IsUploading = false;

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
		#endregion

		#region private Methods

		private async Task<uint?> RecognizeSongFromRecording()
		{
			IAudioFormat recordedAudioWav;
#if NETFX_CORE
			recordedAudioWav = await getAudioFormatFromRecodingUWP();
#elif __ANDROID__
			recordedAudioWav = await getAudioFormatFromRecordingANDROID();
#elif __WASM__
			recognizeWASM();
			return 0; //Return 0 just to comply with method (recognition handling is done in recognizeWASM();
#else
			throw new NotImplementedException("RecognizeSongFromRecording feature is not implemented on your platform.");
#endif

			return await Task.Run(() => recognizer.RecognizeSong(recordedAudioWav, savedSongs));
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
				InformationText = e.Message;
			}
		}

		private void AddNewSongToDatabase(string songName, string songAuthor)
		{
			this.Log().LogDebug($"[DEBUG] Adding {songName} by {songAuthor} into database.");

			IAudioFormat audioWav;
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

#endregion
	}


}
