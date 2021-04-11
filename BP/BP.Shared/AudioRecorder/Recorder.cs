using System;
using System.Threading.Tasks;

#if NETFX_CORE
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

#endif

#if __ANDROID__
using Android.Media;
using System.IO;
using System.Threading;
using Xamarin.Essentials;
using System.Collections.Generic;
#endif


namespace BP.Shared.AudioRecorder
{
	public sealed partial class Recorder
	{
		private bool isRecording;

		private object bufferLock = new object();

		#region UWP
#if NETFX_CORE
		MediaCapture audioCapture;
		IRandomAccessStream buffer;
		string filename;
		string audioFile = "recording.wav";
#endif
		#endregion
		#region ANDROID
#if __ANDROID__
		private byte[] buffer;
		private AudioRecord rec;
		private int bufferLimit = 480000;
#endif
		#endregion

		public async Task<bool> UploadRecording(Action<string> writeResult)
		{
			#region UWP
#if NETFX_CORE
			var picker = new Windows.Storage.Pickers.FileOpenPicker();
			picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
			picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
			picker.FileTypeFilter.Add(".wav");

			StorageFile file = await picker.PickSingleFileAsync();
			
			if (file == null)
			{
				writeResult("No song uploaded.");
				return false;
			}
			
			var fileProperties = await file.GetBasicPropertiesAsync();
			if (fileProperties.Size > Parameters.MaxRecordingUploadSize_Mb * 1024 * 1024)
			{
				writeResult($"Selected file is too big.\nMaximum allowed size is {Parameters.MaxRecordingUploadSize_Mb} Mb.");
				return false;
			}
			else 
			{
				buffer = await file.OpenAsync(FileAccessMode.ReadWrite);
				writeResult(file.Name);
				return true;
			}

#endif
			#endregion

			#region ANDROID
#if __ANDROID__

			buffer = await Utils.FileUpload.pickAndUploadFileAsync(writeResult, bufferLock, Recorder.Parameters.MaxRecordingUploadSize_Mb);
			return buffer != null;
#endif
			#endregion

			#region WASM
#if __WASM__
			return true;
#endif
			#endregion

			//fallback return value for unsupported platforms
			return false;
		}


		#region RECORDING
		public async Task<bool> RecordAudio(int recordingLength = 3)
		{
#if __ANDROID__
			if (!await Utils.Permissions.Droid.GetMicPermission())
			{
				System.Diagnostics.Debug.WriteLine("[DEBUG] microphone acecss denied.");
				return false;
			}

			bufferLimit = getBufferLimitFromTime(recordingLength);
#endif

			await StartRecording();

#if NETFX_CORE
			//record audio for recordingLength seconds
			await Task.Delay(recordingLength * 1000);
#endif

			await StopRecording();
			return true;
		}

		public async Task StartRecording()
		{
			if (isRecording)
			{
				//already recording
				return;
			}
			else
			{
				#region UWP
#if NETFX_CORE
				await setupRecording();
				var profile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto);
				profile.Audio = AudioEncodingProperties.CreatePcm((uint)Parameters.SamplingRate, (uint)Parameters.Channels, 16);
				
				//start recording
				isRecording = true;
				await audioCapture.StartRecordToStreamAsync(profile, buffer);
				return;
#endif
				#endregion
				#region ANDROID
#if __ANDROID__

				ChannelIn channels = Parameters.Channels == 1 ? ChannelIn.Mono : ChannelIn.Stereo;

				rec = new AudioRecord(
					AudioSource.Mic,
					(int)Parameters.SamplingRate,
					channels,
					Android.Media.Encoding.Pcm16bit,
					bufferLimit
					);

				System.Diagnostics.Debug.WriteLine("[DEBUG] starting to record ...");
				//Console.Out.WriteLine("[DEBUG] starting to record ...");
				isRecording = true;
				int totalBytesRead = 0;

				lock (bufferLock)
				{
					buffer = new byte[bufferLimit];
					rec.StartRecording();
					while (totalBytesRead < bufferLimit)
					{
						try
						{
							int bytesRead = rec.Read(buffer, 0, bufferLimit);
							if (bytesRead < 0)
							{
								throw new Exception(String.Format("Exception code: {0}", bytesRead));
							}
							else
							{
								totalBytesRead += bytesRead;
							}
						}
						catch(Exception e)
						{
							System.Diagnostics.Debug.WriteLine("[DEBUG] " + e.Message);
							Console.Out.WriteLine("[DEBUG] " + e.Message);
							break;
						}
					}
				}
				System.Diagnostics.Debug.WriteLine("[DEBUG] finished recording");
				Console.Out.WriteLine("[DEBUG] finished recording");

#endif
#endregion
			}
		}

		public async Task StopRecording()
		{
			if (!isRecording)
			{
				//not recording yet
				return;
			}
			else
			{
#region UWP
#if NETFX_CORE
				await audioCapture.StopRecordAsync();
#endif
#endregion
#region ANDROID
#if __ANDROID__
				rec.Stop();
				rec.Dispose();
#endif
#endregion
				isRecording = false;
			}
		}

		#endregion


#if !__WASM__
		public async Task<byte[]> GetDataFromStream()
		{
#if NETFX_CORE
			if (buffer == null)
				throw new InvalidOperationException("Buffer should not be null");

			byte[] data = new byte[buffer.Size];
			DataReader dataReader = new DataReader(buffer.GetInputStreamAt(0));

			await dataReader.LoadAsync((uint)buffer.Size);
			dataReader.ReadBytes(data);
			return data;
#else
			return buffer;
#endif
		}
#endif
		//replay functions
#region UWP
#if NETFX_CORE

		public async void ReplayRecordingUWP(CoreDispatcher UIDispatcher)
		{
			//do nothign without buffer
			if (buffer == null)
				return;


			MediaElement playback = new MediaElement();
			IRandomAccessStream audioBuffer;

			lock (bufferLock)
			{
				audioBuffer = buffer.CloneStream();
			}

			if (audioBuffer == null)
				throw new ArgumentNullException("buffer");

			StorageFolder recordingFolder = ApplicationData.Current.TemporaryFolder;

			//delete if not empty
			if (!string.IsNullOrEmpty(filename))
			{
				StorageFile original = await recordingFolder.GetFileAsync(filename);
				await original.DeleteAsync();
			}

			//replay async
			await UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				StorageFile recordingFile = await recordingFolder.CreateFileAsync(audioFile, CreationCollisionOption.ReplaceExisting);
				filename = recordingFile.Name;

				//save buffer into file
				using (IRandomAccessStream fs = await recordingFile.OpenAsync(FileAccessMode.ReadWrite))
				{
					await RandomAccessStream.CopyAndCloseAsync(audioBuffer.GetInputStreamAt(0), fs.GetOutputStreamAt(0));
					await audioBuffer.FlushAsync();
					audioBuffer.Dispose();
				}

				//replay actuall recording
				IRandomAccessStream replayStream = await recordingFile.OpenAsync(FileAccessMode.Read);
				playback.SetSource(replayStream, recordingFile.FileType);
				playback.Play();
			});

		}
#endif
#endregion
#region ANDROID
#if __ANDROID__
		public async void ReplayRecordingANDROID()
		{
			ChannelOut channels = Parameters.Channels == 1 ? ChannelOut.Mono : ChannelOut.Stereo;
			lock (bufferLock)
			{
				AudioTrack audioTrack = new AudioTrack.Builder()
					.SetAudioAttributes(new AudioAttributes.Builder()
						.SetUsage(AudioUsageKind.Media)
						.SetContentType(AudioContentType.Music)
						.Build())
					.SetAudioFormat(new AudioFormat.Builder()
						.SetEncoding(Encoding.Pcm16bit)
						.SetSampleRate((int) Parameters.SamplingRate)
						.SetChannelMask(channels)
						.Build())
					.SetBufferSizeInBytes(buffer.Length)
					.Build();

				audioTrack.Play();
				audioTrack.Write(buffer, 0, buffer.Length);
			}
		}
#endif
#endregion


		// helper functons
#region UWP - helper functions
#if NETFX_CORE

		private async Task setupRecording()
		{
			if (buffer != null)
				buffer.Dispose();
			if (audioCapture != null)
				audioCapture.Dispose();

			buffer = new InMemoryRandomAccessStream();

			try
			{
				MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
				{
					StreamingCaptureMode = StreamingCaptureMode.Audio,
				};

				audioCapture = new MediaCapture();
				//initialize setup
				await audioCapture.InitializeAsync(settings);
				//exceed limitation handler
				audioCapture.RecordLimitationExceeded += async (MediaCapture sender) =>
				{
					await audioCapture.StopRecordAsync();
					isRecording = false;
					throw new Exception("Record limitation exceeded.");
				};
				//on fail handler
				audioCapture.Failed += (MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs) =>
				{
					isRecording = false;
					throw new Exception(string.Format("Audio Capture failed. CODE: {0}. {1}", errorEventArgs.Code, errorEventArgs.Message));
				};
			}
			catch(Exception e)
			{
				throw e;
			}
		}

#endif
#endregion

#region ANDROID - helper functions
#if __ANDROID__
		private int getBufferLimitFromTime(int recordingLength)
		{
			//seconds * (2 bytes per 1 sample) * samplingRate
			return recordingLength * 2 * 48000;
		}
#endif
#endregion
	}
}
