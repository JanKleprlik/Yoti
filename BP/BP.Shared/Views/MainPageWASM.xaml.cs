using System;
using Windows.UI.Xaml;

using Windows.UI.Xaml.Controls;
using AudioProcessing.AudioFormats;
using System.Threading.Tasks;

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

		private void recognizeWASM()
		{
			//can add if to swithc to recording if its set in settings
			//WasmSongEvent -= OnSongToRecognizeEvent;
			//WasmSongEvent += OnSongToRecognizeEvent;
			//WebAssemblyRuntime.InvokeJS("record_and_recognize();");

			WasmSongEvent -= OnSongToRecognizeEvent;
			WasmSongEvent += OnSongToRecognizeEvent;
			stringBuilder = new StringBuilder();
			WebAssemblyRuntime.InvokeJS("pick_and_upload_file_by_parts(5, 22);");
		}

		private async Task pickAndUploadFileWASM()
		{
			WasmSongEvent -= OnNewSongUploadedEvent;
			WasmSongEvent += OnNewSongUploadedEvent;
			stringBuilder = new StringBuilder();
			await WebAssemblyRuntime.InvokeAsync("pick_and_upload_file_by_parts(50, 22);");
		}


		private void OnSongToRecognizeEvent(object sender, WasmSongEventHandlerArgs e)
		{
			if (e.isDone)
			{
				WasmSongEvent -= OnSongToRecognizeEvent;
				
				var binData = Convert.FromBase64String(stringBuilder.ToString()); //this is the data I want
				var recordedAudioWav = new WavFormat(binData);

				Debug.WriteLine("[DEBUG] Channels: " + recordedAudioWav.Channels);
				Debug.WriteLine("[DEBUG] SampleRate: " + recordedAudioWav.SampleRate);
				Debug.WriteLine("[DEBUG] NumOfData: " + recordedAudioWav.NumOfDataSamples);
				Debug.WriteLine("[DEBUG] ActualNumOfData: " + recordedAudioWav.Data.Length);

				uint? ID = recognizer.RecognizeSong(recordedAudioWav, songValueDatabase);
				Debug.WriteLine($"[DEBUG] ID of recognized song is { ID }");
				WasmSongEvent -= OnSongToRecognizeEvent;
			}
			else
			{
				stringBuilder.Append(e.FileAsDataUrl);
			}
		}

		public static void ProcessEvent(string fileAsDataUrl, bool isDone) => WasmSongEvent?.Invoke(null, new WasmSongEventHandlerArgs(fileAsDataUrl, isDone));

		private void OnNewSongUploadedEvent(object sender, WasmSongEventHandlerArgs e)
		{
			if (e.isDone)
			{
				Stopwatch sw = new Stopwatch();
				WasmSongEvent -= OnNewSongUploadedEvent;

				Debug.WriteLine($"Full song uploaded:");
				sw.Start();
				uploadedSong = Convert.FromBase64String(stringBuilder.ToString()); //this is the data I want
				sw.Stop();
				Debug.WriteLine($"str->byte : {sw.ElapsedMilliseconds}");
			}
			else
			{
				stringBuilder.Append(e.FileAsDataUrl);
			}
		}

		private static event WasmSongEventHandler WasmSongEvent;

		private delegate void WasmSongEventHandler(object sender, WasmSongEventHandlerArgs args);

		private class WasmSongEventHandlerArgs
		{
			public string FileAsDataUrl { get; }
			public bool isDone { get; }
			public WasmSongEventHandlerArgs(string fileAsDataUrl, bool isDone)
			{
				FileAsDataUrl = fileAsDataUrl;
				this.isDone = isDone;
			}	

		}
#endif
		#endregion
	}
}
