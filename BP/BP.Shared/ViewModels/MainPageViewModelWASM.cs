#if __WASM__
using AudioRecognitionLibrary.AudioFormats;
using Database;
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
			if (Settings.UseMicrophone)
			{
				stringBuilder = new StringBuilder();
				var escapedRecLength = WebAssemblyRuntime.EscapeJs((Settings.RecordingLength).ToString());
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
			InformationText = "Processing uploaded file." + Environment.NewLine + " Please wait ...";
			await WebAssemblyRuntime.InvokeAsync("pick_and_upload_file_by_parts(50, 22);"); //(size_limit, js metadata offset)
		}


		private async void OnSongToRecognizeEvent(object sender, WasmSongEventHandlerArgs e)
		{
			WasmSongEvent -= OnSongToRecognizeEvent;
			if (e.isDone)
			{
				this.Log().LogDebug(stringBuilder.ToString());
				var base64Data = Regex.Match(stringBuilder.ToString(), @"data:((audio)|(application))/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
				var binData = Convert.FromBase64String(base64Data); //this is the data I want byte[]



				try
				{
					if (!Settings.UseMicrophone)
					{
						this.Log().LogDebug("Uploaded song");
						uploadedSongFormat = new WavFormat(binData);
					}
					else
					{
						this.Log().LogDebug("Used microphone");
						uploadedSongFormat = new WavFormat(binData);
					}
					if (!IsSupported(uploadedSongFormat))
					{
						//release resources
						uploadedSongFormat = null;
						return;
					}
				}
				catch(ArgumentException ex)
				{
					this.Log().LogError(ex.Message);
					InformationText = "Problem with uploaded wav file occured." + Environment.NewLine + "Please try a different audio file.";
					return;
				}

				this.Log().LogDebug("[DEBUG] Channels: " + uploadedSongFormat.Channels);
				this.Log().LogDebug("[DEBUG] SampleRate: " + uploadedSongFormat.SampleRate);
				this.Log().LogDebug("[DEBUG] NumOfData: " + uploadedSongFormat.NumOfDataSamples);
				this.Log().LogDebug("[DEBUG] ActualNumOfData: " + uploadedSongFormat.Data.Length);

				//Name, Author and Lyrics is not important for recognition call
				SongWavFormat songWavFormat = CreateSongWavFormat("none", "none", "none");

				RecognitionResult result = await RecognizerApi.RecognizeSong(songWavFormat);
				WriteRecognitionResults(result);
			}
			else
			{
				stringBuilder.Append(e.FileAsDataUrl);

				//repeat until e.isDone == true;
				WasmSongEvent += OnSongToRecognizeEvent;
			}
		}
		private async void OnNewSongUploadedEvent(object sender, WasmSongEventHandlerArgs e)
		{
			WasmSongEvent -= OnNewSongUploadedEvent;

			if (e.isDone)
			{
				this.Log().LogDebug($"Full song uploaded, now converting uploaded data...");
				
				byte[] uploadedSong = Convert.FromBase64String(stringBuilder.ToString()); //this is the data I want
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
				catch(ArgumentException ex)
				{
					this.Log().LogError(ex.Message);
					InformationText = "Problem with uploaded wav file occured." + Environment.NewLine + "Please try a different audio file.";
					return;
				}

				//Update UI
				UploadedSongText = e.FileAsDataUrl;
				InformationText = "File picked";

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