using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;

#if __ANDROID__
using Xamarin.Essentials;
using System.Threading;
using System.Threading.Tasks;
using AudioProcessing.AudioFormats;

#endif

namespace BP.Shared.Views
{
	public sealed partial class MainPage : Page
	{
#if __ANDROID__
		private async void pickAndUploadFileANDROIDAsync()
		{
			if (await getExternalStoragePermission())
			{
				PickOptions options = new PickOptions
				{
					PickerTitle = "Please select a wav song file",
					FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
					{
						{DevicePlatform.Android, new[]{"audio/x-wav"} }
					})
				};

				FileResult result = await FilePicker.PickAsync(options);

				if (result != null)
				{
					UploadedSongText.Text = $"File selected: {result.FileName}";
					var audioFileData = await result.OpenReadAsync();
					lock (uploadedSongLock)
					{
						uploadedSong = new byte[(int)audioFileData.Length];
						audioFileData.Read(uploadedSong, 0, (int)audioFileData.Length);
					}
				}
				else
				{
					UploadedSongText.Text = "No song uploaded";
				}

			}
			else
			{
				UploadedSongText.Text = "Acces to read storage denied.";
			}
		}


		private async Task<bool> getExternalStoragePermission()
		{
			CancellationTokenSource source = new CancellationTokenSource();
			CancellationToken token = source.Token;
			return await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.ReadExternalStorage);
		}


		private async Task<IAudioFormat> getAudioFormatFromRecordingANDROID()
		{
			byte[] recordedSong = await recorder.GetDataFromStream();
			
			
			// On Android we only get raw data without .
			// So I have to convert them manually to shorts and then use different manual constructor
			short[] recordedDataShort = AudioProcessing.Tools.Converter.BytesToShorts(recordedSong);

			return new WavFormat(
				AudioRecorder.Recorder.Parameters.SamplingRate,
				AudioRecorder.Recorder.Parameters.Channels,
				recordedDataShort.Length,
				recordedDataShort);

		}

#endif
	}
}
