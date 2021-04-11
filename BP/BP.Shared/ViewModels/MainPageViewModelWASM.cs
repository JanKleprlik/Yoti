#if __WASM__
using AudioProcessing.AudioFormats;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation;
using static BP.Shared.AudioRecorder.Recorder;

namespace BP.Shared.ViewModels
{
	public partial class MainPageViewModel : BaseViewModel
	{
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
				await WebAssemblyRuntime.InvokeAsync($"pick_and_upload_file_by_parts({AudioRecorder.Recorder.Parameters.MaxRecordingUploadSize_Mb}, 0);"); //(size_limit, js metadata offset)
			}
		}

		private async Task pickAndUploadFileWASM()
		{
			DeleteDelegates();
			WasmSongEvent += OnNewSongUploadedEvent;
			stringBuilder = new StringBuilder();
			//Update UI
			//In WASM UI Thread will be blocked while uploading
			InformationText = "Processing uploaded file.\n Please wait ...";
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

				this.Log().LogDebug("[DEBUG] Channels: " + recordedAudioWav.Channels);
				this.Log().LogDebug("[DEBUG] SampleRate: " + recordedAudioWav.SampleRate);
				this.Log().LogDebug("[DEBUG] NumOfData: " + recordedAudioWav.NumOfDataSamples);
				this.Log().LogDebug("[DEBUG] ActualNumOfData: " + recordedAudioWav.Data.Length);

				uint? ID = recognizer.RecognizeSong(recordedAudioWav, Database.GetSearchData());

				this.Log().LogDebug($"[DEBUG] ID of recognized song is { ID }");
				WriteRecognitionResults(ID);
			}
			else
			{
				stringBuilder.Append(e.FileAsDataUrl);
				//repeat until e.isDone;
				WasmSongEvent += OnSongToRecognizeEvent;
			}
		}
		private async void OnNewSongUploadedEvent(object sender, WasmSongEventHandlerArgs e)
		{
			WasmSongEvent -= OnNewSongUploadedEvent;

			if (e.isDone)
			{
				this.Log().LogDebug($"Full song uploaded, now converting uploaded data...");
				
				uploadedSong = Convert.FromBase64String(stringBuilder.ToString()); //this is the data I want
				
				//Update UI
				UploadedSongText = e.FileAsDataUrl;
				InformationText = "File uploaded";

				this.Log().LogDebug($"Uploaded song converted to byte[].");
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
			this.Log().LogDebug("Deleting delegates...");
			//Delete delegates 
			WasmSongEvent -= OnSongToRecognizeEvent;
			WasmSongEvent -= OnNewSongUploadedEvent;
		}
		
		private void OnCancelEvent(object sender, EventArgs e)
		{
			DeleteDelegates();
		}

	}
}
#endif