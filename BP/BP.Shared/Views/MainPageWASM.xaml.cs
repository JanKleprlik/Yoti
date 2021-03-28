using System;
using Windows.UI.Xaml;

using Windows.UI.Xaml.Controls;
using AudioProcessing.AudioFormats;
using System.Threading.Tasks;
using static BP.Shared.AudioRecorder.Recorder;
using Uno.Extensions;

#if __WASM__
using System.Text;
using System.Text.RegularExpressions;
using Uno.Foundation;
using System.Diagnostics;
using BP.Shared.AudioRecorder;
#endif


namespace BP.Shared.Views
{
	public sealed partial class MainPage : Page
	{
		#region WASM
#if __WASM__
		private static StringBuilder stringBuilder;
		private async void recognizeWASM()
		{
			DeleteDelegates();
			WasmSongEvent += OnSongToRecognizeEvent;
			if (settings.UseMicrophone)
			{
				stringBuilder = new StringBuilder();
				var escapedRecLength = WebAssemblyRuntime.EscapeJs((settings.RecordingLength * 1000).ToString());
				WebAssemblyRuntime.InvokeJS($"record_and_recognize({escapedRecLength});");
			}
			else
			{
				stringBuilder = new StringBuilder();
				await WebAssemblyRuntime.InvokeAsync("pick_and_upload_file_by_parts(5, 0);"); //(size_limit, js metadata offset)
			}
		}

		private async Task pickAndUploadFileWASM()
		{
			DeleteDelegates();
			WasmSongEvent += OnNewSongUploadedEvent;
			stringBuilder = new StringBuilder();
			await WebAssemblyRuntime.InvokeAsync("pick_and_upload_file_by_parts(50, 22);"); //(size_limit, js metadata offset)
		}


		private async void OnSongToRecognizeEvent(object sender, WasmSongEventHandlerArgs e)
		{
			WasmSongEvent -= OnSongToRecognizeEvent;
			if (e.isDone)
			{

				var base64Data = Regex.Match(stringBuilder.ToString(), @"data:((audio)|(application))/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
				var binData = Convert.FromBase64String(base64Data); //this is the data I want byte[]

				IAudioFormat recordedAudioWav;
				
				if (settings.UseMicrophone)
				{
					recordedAudioWav = new WavFormat(binData);
				}
				else
				{
					//TODO: Extract raw pcm data form binData using ffmpeg
					var shortData = AudioProcessing.Tools.Converter.BytesToShorts(binData);
					recordedAudioWav = new WavFormat(Parameters.SamplingRate,Parameters.Channels, shortData.Length, shortData);
				}

				Debug.WriteLine("[DEBUG] Channels: " + recordedAudioWav.Channels);
				Debug.WriteLine("[DEBUG] SampleRate: " + recordedAudioWav.SampleRate);
				Debug.WriteLine("[DEBUG] NumOfData: " + recordedAudioWav.NumOfDataSamples);
				Debug.WriteLine("[DEBUG] ActualNumOfData: " + recordedAudioWav.Data.Length);

				uint? ID = recognizer.RecognizeSong(recordedAudioWav, songValueDatabase);
				Debug.WriteLine($"[DEBUG] ID of recognized song is { ID }");
				await WriteRecognitionResults(ID);
			}
			else
			{
				stringBuilder.Append(e.FileAsDataUrl);
				//repeat until e.isDone;
				WasmSongEvent += OnSongToRecognizeEvent;
			}
		}
		private void OnNewSongUploadedEvent(object sender, WasmSongEventHandlerArgs e)
		{
			WasmSongEvent -= OnNewSongUploadedEvent;

			if (e.isDone)
			{

				Debug.WriteLine($"Full song uploaded, now processing uploaded data...");
				uploadedSong = Convert.FromBase64String(stringBuilder.ToString()); //this is the data I want

			}
			else
			{
				stringBuilder.Append(e.FileAsDataUrl);
				//repeat until e.isDone
				WasmSongEvent += OnNewSongUploadedEvent;
			}
		}

		public static void ProcessEvent(string fileAsDataUrl, bool isDone) => WasmSongEvent?.Invoke(null, new WasmSongEventHandlerArgs(fileAsDataUrl, isDone));

		//Upload Event Handler

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

		private void DeleteDelegates()
		{
			Debug.WriteLine("Deleting delegates...");
			//Delete delegates 
			WasmSongEvent -= OnSongToRecognizeEvent;
			WasmSongEvent -= OnNewSongUploadedEvent;
		}
		
		private void OnCancelEvent(object sender, EventArgs e)
		{
			DeleteDelegates();
		}

#endif
		#endregion
	}
}
