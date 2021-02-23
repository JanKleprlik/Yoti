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
		private string filePath;
		private MediaRecorder recorder;
		private MediaPlayer player;

		public Recorder()
		{
			filePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "recording.mp4");
		}
#endif
		#endregion

		public async Task<bool> StartRecording()
		{
			if (isRecording)
			{
				//already recording
				return false;
			}
			else
			{
				#region UWP
#if NETFX_CORE
				await setupRecording();
				await audioCapture.StartRecordToStreamAsync(MediaEncodingProfile.CreateWav(AudioEncodingQuality.Low), buffer);
				isRecording = true;
				return true;
#endif
				#endregion
				#region ANDROID
#if __ANDROID__
				try
				{
					if (System.IO.File.Exists(filePath))
						System.IO.File.Delete(filePath);
								
					//setup recorder
					if (recorder == null)
					{
						recorder = new MediaRecorder(); // Initial state.
					}
					else
					{
						recorder.Reset();
					}

					//prepare recording settings
					recorder.SetAudioSource(AudioSource.Mic);
					recorder.SetOutputFormat(OutputFormat.Mpeg4);
					recorder.SetAudioChannels(1);
					recorder.SetAudioSamplingRate(11025);
					recorder.SetAudioEncodingBitRate(44000);
					recorder.SetAudioEncoder(AudioEncoder.HeAac);

					//setup 
					recorder.SetOutputFile(filePath);
					recorder.Prepare();

					//record
					recorder.Start();


					isRecording = true;
					return true;
				}
				catch (Exception ex)
				{
					Console.Out.WriteLine(ex.StackTrace);
					isRecording = false;
					return false;
				}
#endif
				#endregion
			}
			//true is returned in UWP or ANDROID if all goes well
			return false;
		}

		public async Task<bool> StopRecording()
		{
			if (!isRecording)
			{
				//not recording yet
				return false;
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
				recorder.Stop();
				recorder.Release();
#endif
				#endregion
				isRecording = false;
				return true;
			}

		}
#if !__WASM__
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
			if (filePath == null)
				return new byte[0];

			return await File.ReadAllBytesAsync(filePath);
#endif
		}
#endif
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
		public async Task<bool> ReplayRecordingANDROID()
		{
			//dont do anything if nothing was recorded
			if (filePath == null)
				return false;

			//reset player
			if (player == null)
			{
				player = new Android.Media.MediaPlayer();
			}
			else
			{
				player.Reset();
			}

			//replay the song
			player.SetDataSource(filePath);
			player.Prepare();
			player.Start();
			return true;
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
		public async Task<bool> getMicPermission()
		{
			CancellationTokenSource source = new CancellationTokenSource();
			CancellationToken token = source.Token;
			return await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.RecordAudio);
		}
#endif
#endregion
	}
}
