#if NETFX_CORE
using System;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using AudioProcessing.AudioFormats;

namespace BP.Shared.ViewModels
{
	public partial class MainPageViewModel : BaseViewModel
	{

		private async Task pickAndUploadFileUWPAsync(Action<string> writeResult)
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
				writeResult(file.Name);
				//UploadedSongText = file.Name;
			}
			else
			{
				writeResult("No song uploaded.");
				//UploadedSongText= "No song uploaded.";
			}
		}


		private async Task<IAudioFormat> getAudioFormatFromRecodingUWP()
		{
			byte[] recordedSong = await audioRecorder.GetDataFromStream(); //exception can be propagated
			// Metadata about recording are included
			return new WavFormat(recordedSong);
		}

	}
}

#endif