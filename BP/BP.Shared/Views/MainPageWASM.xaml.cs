using System;
using Windows.UI.Xaml;

using Windows.UI.Xaml.Controls;
using AudioProcessing.AudioFormats;

#if __WASM__
using System.Text;
using System.Text.RegularExpressions;
using Uno.Foundation;
using System.Diagnostics;
#endif


namespace BP.Shared.Views
{
	public sealed partial class MainPage : Page
	{
		#region WASM
#if __WASM__
		private static StringBuilder stringBuilder;

		private void OnSongToRecognizeEvent(object sender, FileSelectedEventHandlerArgs e)
		{
			string data = e.FileAsDataUrl;
			Debug.WriteLine("--------------C#--------------");
			Debug.WriteLine($"data.length: {data.Length}");

			var base64Data = Regex.Match(data, @"data:application/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
			var binData = Convert.FromBase64String(base64Data); //this is the data I want
			Debug.WriteLine($"binData.length: {binData.Length}");

			short[] recordedDataShort = AudioProcessing.Tools.Converter.BytesToShorts(binData);

			var recordedAudioWav = new WavFormat(
				AudioRecorder.Recorder.Parameters.SamplingRate,
				AudioRecorder.Recorder.Parameters.Channels,
				recordedDataShort.Length,
				recordedDataShort);

			Debug.WriteLine("[DEBUG] Channels: " + recordedAudioWav.Channels);
			Debug.WriteLine("[DEBUG] SampleRate: " + recordedAudioWav.SampleRate);
			Debug.WriteLine("[DEBUG] NumOfData: " + recordedAudioWav.NumOfDataSamples);
			Debug.WriteLine("[DEBUG] ActualNumOfData: " + recordedAudioWav.Data.Length);
			
			//TODO: test with working database
			uint? ID = recognizer.RecognizeSong(recordedAudioWav, songValueDatabase);

			//uint? ID = await Task.Run(() => recognizer.RecognizeSong(recordedAudioWav, songValueDatabase));

			Debug.WriteLine($"[DEBUG] ID of recognized song is { ID }");
			FileSelectedEvent -= OnSongToRecognizeEvent;
		}

		public async void UploadFileByParts(object sender, RoutedEventArgs e)
		{
			FileSelectedEvent -= OnNewSongUploadedEvent;
			FileSelectedEvent += OnNewSongUploadedEvent;
			Debug.WriteLine("Setting up stringBuilder");
			stringBuilder = new StringBuilder();
			Debug.WriteLine($"Is null: {stringBuilder == null}");
			await WebAssemblyRuntime.InvokeAsync("(async () => await pick_and_upload_file_by_parts())();");
			//await WebAssemblyRuntime.InvokeAsync("()();pick_and_upload_file_by_parts(); ");
			Debug.WriteLine($"Stringbuilder.Length: {stringBuilder.Length}");
		}


		public static void ProcessFileByParts(string fileAsDataUrl, bool isDone) => FileSelectedEvent?.Invoke(null, new FileSelectedEventHandlerArgs(fileAsDataUrl, isDone));

		private void OnNewSongUploadedEvent(object sender, FileSelectedEventHandlerArgs e)
		{
			if (e.isDone)
			{
				FileSelectedEvent -= OnNewSongUploadedEvent;
				Debug.WriteLine($"Full song processed IN EVENT: {stringBuilder.Length}");
				byte[] uploadedSong = Convert.FromBase64String(stringBuilder.ToString()); //this is the data I want
				Debug.WriteLine($" Got : {uploadedSong.Length} bytes");
				var recordedAudioWav = new WavFormat(uploadedSong);

				Debug.WriteLine("[DEBUG] Channels: " + recordedAudioWav.Channels);
				Debug.WriteLine("[DEBUG] SampleRate: " + recordedAudioWav.SampleRate);
				Debug.WriteLine("[DEBUG] NumOfData: " + recordedAudioWav.NumOfDataSamples);
				Debug.WriteLine("[DEBUG] ActualNumOfData: " + recordedAudioWav.Data.Length);

				var tfps = recognizer.GetTimeFrequencyPoints(recordedAudioWav);
				uint songID = database.AddSong("World", "IDK");
				database.AddFingerprint(tfps);
				Debug.WriteLine($"[DEBUG] DS.Count BEFORE:{songValueDatabase.Count}");
				recognizer.AddTFPToDataStructure(tfps, songID, songValueDatabase);
				database.UpdateSearchData(songValueDatabase);
				Debug.WriteLine($"[DEBUG] DS.Count AFTER :{songValueDatabase.Count}");
			}
			else
			{
				stringBuilder.Append(e.FileAsDataUrl);
				Debug.WriteLine(stringBuilder.Length);
			}
		}

		private static event FileSelectedEventHandler FileSelectedEvent;

		private delegate void FileSelectedEventHandler(object sender, FileSelectedEventHandlerArgs args);

		private class FileSelectedEventHandlerArgs
		{
			public string FileAsDataUrl { get; }
			public bool isDone { get; }
			public FileSelectedEventHandlerArgs(string fileAsDataUrl, bool isDone)
			{
				FileAsDataUrl = fileAsDataUrl;
				this.isDone = isDone;
			}	

		}
#endif
		#endregion
	}
}
