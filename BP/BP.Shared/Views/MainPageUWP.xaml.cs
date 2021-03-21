using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;
#if NETFX_CORE
using System.Threading;
using Windows.Storage;
using System.IO;
using AudioProcessing.AudioFormats;
using System.Threading.Tasks;

#endif

namespace BP.Shared.Views
{
	public sealed partial class MainPage : Page
	{
#if NETFX_CORE

		private async void pickAndUploadFileUWPAsync()
		{
			var picker = new Windows.Storage.Pickers.FileOpenPicker();
			picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
			picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
			picker.FileTypeFilter.Add(".wav");

			StorageFile file = await picker.PickSingleFileAsync();

			if (file != null)
			{
				var audioFileData = await file.OpenStreamForReadAsync();
				lock (uploadedSongLock)
				{
					uploadedSong = new byte[(int)audioFileData.Length];
					audioFileData.Read(uploadedSong, 0, (int)audioFileData.Length);
				}
				UploadedSongText.Text = file.Name;
			}
			else
			{
				UploadedSongText.Text = "No song uploaded.";
			}
		}


		private async Task<IAudioFormat> getAudioFormatFromRecodingUWP()
		{
			byte[] recordedSong = await recorder.GetDataFromStream();
			// Metadata about recording are included
			return new WavFormat(recordedSong);
		}

#endif

	}
}
