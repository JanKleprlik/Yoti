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

		public async void Recognize(string data)
		{
			Debug.WriteLine("--------------C#--------------");
			Debug.WriteLine($"data.length: {data.Length}");
			//System.Diagnostics.Debug.WriteLine(data);

			var base64Data = Regex.Match(data, @"data:application/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
			var binData = Convert.FromBase64String(base64Data); //this is the data I want
			System.Diagnostics.Debug.WriteLine($"binData.length: {binData.Length}");

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

		}

		public async void UploadFileByParts(object sender, RoutedEventArgs e)
		{
			FileSelectedEvent -= OnNewSongUploadedEvent;
			FileSelectedEvent += OnNewSongUploadedEvent;
			Debug.WriteLine("Setting up stringBuilder");
			stringBuilder = new StringBuilder();
			Debug.WriteLine($"Is null: {stringBuilder == null}");
			WebAssemblyRuntime.InvokeJS("pick_and_upload_file_by_parts(); ");
			//this.ExecuteJavascript("pick_and_upload_file_by_parts();");
		}


		public static void ProcessUploadedPart(string data)
		{
			Debug.WriteLine("In ProcessUploadedPart");
			Debug.WriteLine($"Is null: {stringBuilder == null}");
			if (stringBuilder == null)
			{
				Debug.WriteLine("ITS FKIN NULL???");
				stringBuilder = new StringBuilder();
			}

			stringBuilder.Append(data);
		}
		
		public static void ProcessFullSong()
		{
			Debug.WriteLine(stringBuilder.ToString());
		}


		#region OLD


		private async void uploadBtn_Click(object sender, RoutedEventArgs e)
		{
			FileSelectedEvent -= OnSongToRecognizeUploadedEvent;
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
							var selectFile = Module.mono_bind_static_method(" + "\"[BP.Wasm] BP.MainPage:SelectFile\"" + @");
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
			//recordedSong = binData;
			Console.Out.WriteLine("Recognize song");
		}

		private void OnNewSongUploadedEvent(object sender, FileSelectedEventHandlerArgs e)
		{
			//FileSelectedEvent -= OnNewSongUploadedEvent;
			//var base64Data = Regex.Match(e.FileAsDataUrl, @"data:audio/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
			//uploadedSong = Convert.FromBase64String(base64Data); //this is the data I want
			if (e.FileAsDataUrl == null)
			{
				Debug.WriteLine("IS NULL");
				return;
			}

			//uploadedSong = Convert.FromBase64String(e.FileAsDataUrl); //this is the data I want
			//Debug.WriteLine($"uploadedSong.Length : {uploadedSong.Length}");
//#if DEBUG
//			for (int i = 0; i < 1; i++)
//			{
//				Console.Out.Write((char)uploadedSong[i]);
//			}
//			Console.Out.WriteLine();
//#endif
			//Debug.WriteLine($"Is null: {stringBuilder == null}");
			stringBuilder.Append(e.FileAsDataUrl);
			Debug.WriteLine(stringBuilder.Length);
			//uploadedSong = binData;
		}

		private static event FileSelectedEventHandler FileSelectedEvent;

		private delegate void FileSelectedEventHandler(object sender, FileSelectedEventHandlerArgs args);

		private class FileSelectedEventHandlerArgs
		{
			public string FileAsDataUrl { get; }
			public FileSelectedEventHandlerArgs(string fileAsDataUrl) => FileAsDataUrl = fileAsDataUrl;

		}
		#endregion
#endif
		#endregion



	}
}
