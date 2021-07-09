#if __WASM__
using AudioRecognitionLibrary.AudioFormats;
using SharedTypes;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation;
using static Yoti.Shared.AudioProvider.AudioDataProvider;

namespace Yoti.Shared.ViewModels
{
	public partial class MainPageViewModel : BaseViewModel
	{
		#region private properties
		private static StringBuilder stringBuilder { get; set; }
		#endregion

		#region Commands
		/// <summary>
		/// Starts recording audio or opens filepicker, validates uploaded song format, queries server for song recognition and shows result on the screen. <br></br>
		/// Also handles necessary UI updates. <br></br>
		/// WASM only.
		/// </summary>
		public void RecognizeSong_WASM()
		{
			// Setup Delegates
			DeleteDelegates();
			WasmSongEvent += OnSongToRecognizeEvent;

			// Update UI
			UpdateRecognitionUI();

			if (Settings.UseMicrophone)
			{
				IsRecording = true;
				stringBuilder = new StringBuilder();
				var escapedRecLength = WebAssemblyRuntime.EscapeJs((Settings.RecordingLength).ToString());
				WebAssemblyRuntime.InvokeJS($"recordAndUploadAudio({escapedRecLength}, {AudioProvider.AudioDataProvider.Parameters.SamplingRate}, {AudioProvider.AudioDataProvider.Parameters.Channels});");
			}
			else
			{
				stringBuilder = new StringBuilder();
				WebAssemblyRuntime.InvokeJS($"pickAndUploadAudioFile({AudioProvider.AudioDataProvider.Parameters.MaxRecordingUploadSize_MB}, 0);"); //(size_limit, js metadata offset)
			}
		}

		/// <summary>
		/// Opens FilePicker via javascript and allows user to pick audio file.<br></br>
		/// WASM only.
		/// </summary>
		/// <returns></returns>
		public async Task UploadNewSong_WASM()
		{
			// Setup delegates and resources
			DeleteDelegates();
			WasmSongEvent += OnNewSongUploadedEvent;
			stringBuilder = new StringBuilder();
			
			// In WASM UI Thread will be blocked while uploading
			// so we should give user some feedback before hand.
			InformationText = "Processing uploaded file." + Environment.NewLine + " Please wait ...";

			await WebAssemblyRuntime.InvokeAsync($"pickAndUploadAudioFile({AudioProvider.AudioDataProvider.Parameters.MaxUploadSize_MB});");
		}
		#endregion

		#region Events for javascript
		/// <summary>
		/// C# side processing of uploaded or recorded audio data when recognizing song.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">Instance with audio data and process status information.</param>
		private async void OnSongToRecognizeEvent(object sender, string fileAsDataUrl, bool isDone)
		{
			//remove EventHandler in case this is the last iteration
			WasmSongEvent -= OnSongToRecognizeEvent;
			// Audio data is fully uploaded from javascript to C#, now recognize the song
			if (isDone)
			{
				// Update UI
				IsRecording = false;
				IsUploading = false;
				IsRecognizing = true;
				InformationText = "Looking for a match ...";

				try
				{
					// Convert audio data from Base64 string to byteArray
					//string base64Data = Regex.Match(stringBuilder.ToString(), @"data:((audio)|(application))/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
					byte[] binData = Convert.FromBase64String(stringBuilder.ToString()); //this is the data I want byte[]

					// Convert byte array to wav format
					uploadedSong = new WavFormat(binData);
					if (!IsSupported(uploadedSong))
					{
						//release resources
						uploadedSong = null;
						return;
					}
				}

				// catch error that caused by faulty binData
				// ArgumentException at WavFormat(binData)
				// NullReferenceException at Regex.Match().Groups[]-Value
				catch(Exception ex)
				{
					this.Log().LogError(ex.Message);
					InformationText = "Error occured, please try again.";
					IsRecognizing = false;
					return;
				}

				//Debug print recorded audio properties
				this.Log().LogDebug("[DEBUG] Channels: " + uploadedSong.Channels);
				this.Log().LogDebug("[DEBUG] SampleRate: " + uploadedSong.SampleRate);
				this.Log().LogDebug("[DEBUG] NumOfData: " + uploadedSong.NumOfDataSamples);
				this.Log().LogDebug("[DEBUG] ActualNumOfData: " + uploadedSong.Data.Length);
				
				//Name, Author and Lyrics is not important for recognition call
				PreprocessedSongData songWavFormat = PreprocessSongData(uploadedSong);
				RecognitionResult result = await recognizerApi.RecognizeSong(songWavFormat);

				// Update UI
				await WriteRecognitionResults(result);
				IsRecognizing = false;
			}
			else
			{
				stringBuilder.Append(fileAsDataUrl);

				//repeat this event until e.isDone == true;
				WasmSongEvent += OnSongToRecognizeEvent;
			}
		}

		/// <summary>
		/// C# side of processing uploaded song from FilePicker when adding new song to the database.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">Instance with audio data and process status information.</param>
		private void OnNewSongUploadedEvent(object sender, string fileAsDataUrl, bool isDone)
		{
			//remove EventHandler in case this is the last iteration
			WasmSongEvent -= OnNewSongUploadedEvent;
			// Song is fully uploaded from javascript to C#, now convert uploaded data to Wav Format
			if (isDone)
			{
				// Convert audio data from Base64 string to byte array
				byte[] binData = Convert.FromBase64String(stringBuilder.ToString()); 
				
				try 
				{
					// Convert byte array to wav format
					songToBeAddedToDatabase = new WavFormat(binData);
					if (!IsSupported(songToBeAddedToDatabase))
					{
						// Release resources
						songToBeAddedToDatabase = null;
						return;
					}
				}
				catch(ArgumentException ex)
				{
					this.Log().LogError(ex.Message);
					InformationText = "Problem with uploaded wav file occured." + Environment.NewLine + "Please try a different audio file.";
					return;
				}

				// Update UI
				UploadedSongText = fileAsDataUrl;
				InformationText = "File picked";
			}
			else
			{
				stringBuilder.Append(fileAsDataUrl);
				// Repeat this event until e.isDone
				WasmSongEvent += OnNewSongUploadedEvent;
			}
		}
		#endregion


		/// <summary>
		/// Static method to be called from javascript invoking selected delegates.
		/// </summary>
		/// <param name="fileAsDataUrl">File data to be transfered.</param>
		/// <param name="isDone">Flag indicating wether complete file was transfered.</param>
		public static void ProcessEvent(string fileAsDataUrl, bool isDone) => WasmSongEvent?.Invoke(null, fileAsDataUrl, isDone);

		/// <summary>
		/// Upload event handler
		/// </summary>
		private static event WasmEventHandler WasmSongEvent;

		/// <summary>
		/// Event handler delegate.
		/// </summary>
		private delegate void WasmEventHandler(object sender, string fileAsDataUrl, bool isDone);

		/// <summary>
		/// Helper method deleting all buffered delegates.
		/// </summary>
		private void DeleteDelegates()
		{
			WasmSongEvent -= OnSongToRecognizeEvent;
			WasmSongEvent -= OnNewSongUploadedEvent;
		}
		
		/// <summary>
		/// File upload canceled handler.
		/// </summary>
		private void OnCancelEvent(object sender, EventArgs e)
		{
			DeleteDelegates();
		}

	}
}
#endif