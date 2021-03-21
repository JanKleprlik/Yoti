using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
#endif


#region ANDROID
#if __ANDROID__
#endif
#endregion


namespace BP.Shared.AudioRecorder
{
	public sealed partial class Recorder
	{
		private bool isRecording;

		private object bufferLock = new object();

		#region UWP
#if NETFX_CORE
		MediaCapture audioCapture;
		InMemoryRandomAccessStream buffer;
		string filename;
		string audioFile = "recording.wav";
#endif
		#endregion
		#region ANDROID
#if __ANDROID__
		private byte[] buffer;
		private AudioRecord rec;
		private const int bufferLimit = 480000;
#endif
		#endregion

		public async Task RecordAudio()
		{
			await StartRecording();

#if NETFX_CORE
			//record audio for 3000 seconds
			await Task.Delay(3000);
#endif

			await StopRecording();
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
				if (!await getMicPermission())
					return;


				ChannelIn channels = Parameters.Channels == 1 ? ChannelIn.Mono : ChannelIn.Stereo;

				rec = new AudioRecord(
					AudioSource.Mic,
					(int)Parameters.SamplingRate,
					channels,
					Android.Media.Encoding.Pcm16bit,
					bufferLimit
					);

				Console.Out.WriteLine("[DEBUG] starting to record ...");
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
							Console.Out.WriteLine("[DEBUG] " + e.Message);
							break;
						}
					}
				}
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
#if !__WASM__
		public async Task<byte[]> GetDataFromStream()
		{
#if NETFX_CORE
			if (buffer == null)
				return new byte[0];

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

			//StorageFolder recordingFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
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
				AudioTrack audioTrack = new AudioTrack(
					// Stream type
					Android.Media.Stream.Music,
					// Frequency
					(int)Parameters.SamplingRate,
					// Mono or stereo
					channels,
					// Audio encoding
					Android.Media.Encoding.Pcm16bit,
					// Length of the audio clip.
					buffer.Length,
					// Mode. Stream or static.
					AudioTrackMode.Stream);

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
				throw;
			}
		}

#endif
#endregion

#region ANDROID - helper functions
#if __ANDROID__
		private async Task<bool> getMicPermission()
		{
			CancellationTokenSource source = new CancellationTokenSource();
			CancellationToken token = source.Token;
			return await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.RecordAudio);
		}
#endif
#endregion
	}
}
