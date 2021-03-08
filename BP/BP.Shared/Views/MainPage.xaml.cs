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
		private byte[] recordedSong;
		private Database.Database database;
		private Dictionary<uint, List<ulong>> songValueDatabase;
		public MainPage()
        {
            this.InitializeComponent();
			recorder = new Shared.AudioRecorder.Recorder();
			database = new Database.Database();
			recognizer = new AudioRecognizer();
			songValueDatabase = database.GetSearchData();


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
			AudioProcessing.AudioFormats.WavFormat recordedAudioWav;
			
			#region GETTING recordedAudioWav
#if !__WASM__
			recordedSong = await recorder.GetDataFromStream();
#endif
			#region UWP
#if NETFX_CORE
			recordedAudioWav = new AudioProcessing.AudioFormats.WavFormat(recordedSong);
#endif
			#endregion
			#region ANDROID
#if __ANDROID__

			//at android we only get raw data without metadata
			// so I have to convert them manually to shorts and then use different constructor
			short[] recordedDataShort = AudioProcessing.Tools.Converter.BytesToShorts(recordedSong);

			recordedAudioWav = new AudioProcessing.AudioFormats.WavFormat(
				Shared.AudioRecorder.Recorder.Parameters.SamplingRate,
				Shared.AudioRecorder.Recorder.Parameters.Channels,
				recordedDataShort.Length,
				recordedDataShort);
#endif
			#endregion
			#region WASM
#if __WASM__
			throw new NotImplementedException("Song recognition is not yet implemented in WASM");
#endif
			#endregion
			#endregion
			
			System.Diagnostics.Debug.WriteLine("[DEBUG] Channels: " + recordedAudioWav.Channels);
			System.Diagnostics.Debug.WriteLine("[DEBUG] SampleRate: " + recordedAudioWav.SampleRate);
			System.Diagnostics.Debug.WriteLine("[DEBUG] NumOfData: " + recordedAudioWav.NumOfDataSamples);
			System.Diagnostics.Debug.WriteLine("[DEBUG] ActualNumOfData: " + recordedAudioWav.Data.Length);

			uint? ID = recognizer.RecognizeSong(recordedAudioWav, songValueDatabase);

			textBlk.Text = $"ID of recognized song is {ID}";

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
			#endregion
		}

		private async void addNewSongBtn_Click(object sender, RoutedEventArgs e)
		{
			if (uploadedSong != null && nameTxtBox.Text != "" && authorTxtBox.Text != "")
			{
				System.Diagnostics.Debug.WriteLine("[DEBUG] Adding new song into database.");
				var audioWav = new AudioProcessing.AudioFormats.WavFormat(uploadedSong);
				var tfps = recognizer.GetTimeFrequencyPoints(audioWav);
				uint songID = database.AddSong(nameTxtBox.Text, authorTxtBox.Text);
				database.AddFingerprint(tfps);
				System.Diagnostics.Debug.WriteLine($"[DEBUG] DS.Count BEFORE:{songValueDatabase.Count}");
				recognizer.AddTFPToDataStructure(tfps, songID, songValueDatabase);
				database.UpdateSearchData(songValueDatabase);
				System.Diagnostics.Debug.WriteLine($"[DEBUG] DS.Count AFTER :{songValueDatabase.Count}");
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
#if __WASM__
			WebAssemblyRuntime.InvokeJS(
				@"
    var audioContext = new AudioContext();
    console.log('audio is starting up ...');
    console.log(audioContext.sampleRate);

    var BUFF_SIZE_RENDERER = 16384;
    var SIZE_SHOW = 1; // number of array elements to show in console output

    var audioInput = null,
    microphone_stream = null,
    gain_node = null,
    script_processor_node = null,
    script_processor_analysis_node = null,
    analyser_node = null;

    if (!navigator.getUserMedia)
        navigator.getUserMedia = navigator.getUserMedia || navigator.webkitGetUserMedia ||
    navigator.mozGetUserMedia || navigator.msGetUserMedia;

    if (navigator.getUserMedia){

        navigator.getUserMedia({audio:true}, 
            function(stream) {
                start_microphone(stream);
            },
            function(e) {
                alert('Error capturing audio.');
            }
            );

    } else { alert('getUserMedia not supported in this browser.'); }

    function process_microphone_buffer(event) {

        var i, N, inp, microphone_output_buffer;

        // not needed for basic feature set
        // microphone_output_buffer = event.inputBuffer.getChannelData(0); // just mono - 1 channel for now
    }
    
    function indexOfMax(arr) {
    if (arr.length === 0) {
        return -1;
    }

    var max = arr[0];
    var maxIndex = 0;

    for (var i = 1; i < arr.length; i++) {
        if (arr[i] > max) {
            maxIndex = i;
            max = arr[i];
        }
    }

    return maxIndex;
}

    function start_microphone(stream){

        gain_node = audioContext.createGain();
        gain_node.connect( audioContext.destination );

        microphone_stream = audioContext.createMediaStreamSource(stream);
        microphone_stream.connect(gain_node); 

        script_processor_node = audioContext.createScriptProcessor(BUFF_SIZE_RENDERER, 1, 1);
        script_processor_node.onaudioprocess = process_microphone_buffer;

        microphone_stream.connect(script_processor_node);

        // --- setup FFT

        script_processor_analysis_node = audioContext.createScriptProcessor(2048, 1, 1);
        script_processor_analysis_node.connect(gain_node);

        analyser_node = audioContext.createAnalyser();
        analyser_node.smoothingTimeConstant = 0;
        analyser_node.fftSize = 2048;

        microphone_stream.connect(analyser_node);

        analyser_node.connect(script_processor_analysis_node);

        var buffer_length = analyser_node.frequencyBinCount;

        var array_freq_domain = new Uint8Array(buffer_length);
        var array_time_domain = new Uint8Array(buffer_length);
        console.log(array_freq_domain);
        console.log('buffer_length ' + buffer_length);

        script_processor_analysis_node.onaudioprocess = function() {

            // get the average for the first channel
            analyser_node.getByteFrequencyData(array_freq_domain);
            analyser_node.getByteTimeDomainData(array_time_domain);

            // draw the spectrogram
            if (microphone_stream.playbackState == microphone_stream.PLAYING_STATE) {
                console.log(48000 * indexOfMax(array_freq_domain) / 2048);
            }
        };
    }
"
				);
#endif
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
