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
	public class Recorder
	{
		private bool isRecording;
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
		private const int bufferLimit = 100000;
#endif
		#endregion

		public async void StartRecording()
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
				profile.Audio = AudioEncodingProperties.CreatePcm(48000, 1, 16);

				await audioCapture.StartRecordToStreamAsync(profile, buffer);
				isRecording = true;
				return;
#endif
				#endregion
				#region ANDROID
#if __ANDROID__
				if (!await getMicPermission())
					return;

				buffer = new byte[bufferLimit];
				rec = new AudioRecord(
					AudioSource.Mic,
					11025,
					ChannelIn.Mono,
					Android.Media.Encoding.Pcm16bit,
					bufferLimit
					);
				Console.Out.WriteLine("[DEBUG] starting to record ...");
				rec.StartRecording();
				isRecording = true;
				int totalBytesRead = 0;
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
				Console.Out.WriteLine("[DEBUG] finished recording");
#endif
				#endregion
			}
		}

		public async void StopRecording()
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
				//to disalbe fast RUN-STOP sequences
				Thread.Sleep(300);
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

		public async Task<byte[]> GetDataFromStream()
		{
#if NETFX_CORE
			if (buffer == null)
				return new byte[0];

			DataReader dataReader = new DataReader(buffer.GetInputStreamAt(0));
			byte[] data = new byte[buffer.Size];

			await dataReader.LoadAsync((uint)buffer.Size);
			dataReader.ReadBytes(data);
			return data;
#else 
			return buffer;
#endif
		}


		//replay functions
#region UWP
#if NETFX_CORE

		public async Task<bool> ReplayRecordingUWP(CoreDispatcher UIDispatcher)
		{
			//do nothign without buffer
			if (buffer == null)
				return false;

			MediaElement playback = new MediaElement();
			IRandomAccessStream audioBuffer = buffer.CloneStream();

			if (audioBuffer == null)
				throw new ArgumentNullException("buffer");

			StorageFolder recordingFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

			//delete if not empty
			if (!string.IsNullOrEmpty(filename))
			{
				StorageFile original = await recordingFolder.GetFileAsync(filename);
				await original.DeleteAsync();
			}

			//replay asynch
			await UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				StorageFile recordingFile = await recordingFolder.CreateFileAsync(audioFile, CreationCollisionOption.GenerateUniqueName);
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

			return true;
		}
#endif
#endregion
#region ANDROID
#if __ANDROID__
		public void ReplayRecordingANDROID()
		{
			AudioTrack audioTrack = new AudioTrack(
				// Stream type
				Android.Media.Stream.Music,
				// Frequency
				11025,
				// Mono or stereo
				ChannelOut.Mono,
				// Audio encoding
				Android.Media.Encoding.Pcm16bit,
				// Length of the audio clip.
				buffer.Length,
				// Mode. Stream or static.
				AudioTrackMode.Stream);

			audioTrack.Play();
			audioTrack.Write(buffer, 0, buffer.Length);
		}
#endif
#endregion


		// helper functons
#region UWP - helper functions
#if NETFX_CORE

		private async Task<bool> setupRecording()
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
			return true;

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
