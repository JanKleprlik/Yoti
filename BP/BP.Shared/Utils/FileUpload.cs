using System.Threading.Tasks;
using System;

#if NETFX_CORE
using Windows.Storage;
using System.IO;
#endif

#if __ANDROID__
using Xamarin.Essentials;
using System.Collections.Generic;
#endif

namespace BP.Shared.Utils
{
	public static class FileUpload
	{
		public static async Task<byte[]> pickAndUploadFileAsync(Action<string> writeResult, object outputArrayLock, int maxSize_Mb)
		{
			byte[] outputArray;
			#region UWP
#if NETFX_CORE
			var picker = new Windows.Storage.Pickers.FileOpenPicker();
			picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
			picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
			picker.FileTypeFilter.Add(".wav");

			StorageFile file = await picker.PickSingleFileAsync();

			if (file != null)
			{
				var audioFileData = await file.OpenStreamForReadAsync();

				if ((int)audioFileData.Length > maxSize_Mb * 1024 * 1024)
				{
					writeResult($"File is too large.\nMaximum allowed size is {maxSize_Mb} Mb.");
					return null;
				}

				lock (outputArrayLock)
				{
					outputArray = new byte[(int)audioFileData.Length];
					audioFileData.Read(outputArray, 0, (int)audioFileData.Length);
				}
				writeResult(file.Name);
				return outputArray;
			}
			else
			{
				writeResult("No song uploaded.");
				return null;
			}

#endif
			#endregion


			#region ANDROID
#if __ANDROID__
			if (await Utils.Permissions.Droid.GetExternalStoragePermission())
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
					var audioFileData = await result.OpenReadAsync();
					if ((int)audioFileData.Length > maxSize_Mb * 1024 * 1024)
					{
						writeResult($"File is too large.\nMaximum allowed size is {maxSize_Mb} Mb.");
						return null;
					}
					lock (outputArrayLock)
					{
						outputArray = new byte[(int)audioFileData.Length];
						audioFileData.Read(outputArray, 0, (int)audioFileData.Length);
					}
					writeResult(result.FileName);
					return outputArray;
				}
				else
				{
					writeResult("No song uploaded");
					return null;
				}

			}
			else
			{
				writeResult("Acces to read storage denied.");
				return null;
			}
#endif
			#endregion
		}
	}
}
