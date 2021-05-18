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

namespace Yoti.Shared.Utils
{
	public static class FileUpload
	{
		/// <summary>
		/// Opens File Picker and returns picked file as an array of bytes.
		/// </summary>
		/// <param name="writeResult">Action accepting string with FilePicker state description.</param>
		/// <param name="maxSize_Mb"></param>
		/// <returns></returns>
		public static async Task<byte[]> PickAndUploadFileAsync(Action<string> writeResult,  ulong maxSize_Mb)
		{
			byte[] outputArray;
			#region UWP
#if NETFX_CORE
			//Setup FilePicker
			var picker = new Windows.Storage.Pickers.FileOpenPicker();
			picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
			picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
			picker.FileTypeFilter.Add(".wav");

			// Open FilePicker
			StorageFile file = await picker.PickSingleFileAsync();

			// Process picked file
			if (file != null)
			{
				var audioFileData = await file.OpenStreamForReadAsync();

				if ((ulong)audioFileData.Length > maxSize_Mb * 1024 * 1024)
				{
					writeResult($"File is too large." + Environment.NewLine + "Maximum allowed size is {maxSize_Mb} Mb.");
					return null;
				}


				outputArray = new byte[(int)audioFileData.Length];
				audioFileData.Read(outputArray, 0, (int)audioFileData.Length);
				writeResult(file.Name);

				return outputArray;
			}
			// No file picked
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
				// Setup FilePicker
				PickOptions options = new PickOptions
				{
					PickerTitle = "Please select a wav song file",
					FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
					{
						{DevicePlatform.Android, new[]{"audio/x-wav", "audio/wav"} }
					})
				};

				// Open FilePicker
				FileResult result = await FilePicker.PickAsync(options);

				// Process picked file
				if (result != null)
				{
					var audioFileData = await result.OpenReadAsync();
					if ((ulong)audioFileData.Length > maxSize_Mb * 1024 * 1024)
					{
						writeResult($"File is too large." + Environment.NewLine + "Maximum allowed size is {maxSize_Mb} Mb.");
						return null;
					}

					outputArray = new byte[(int)audioFileData.Length];
					audioFileData.Read(outputArray, 0, (int)audioFileData.Length);
					writeResult(result.FileName);

					return outputArray;
				}
				// No file picked
				else
				{
					writeResult("No song uploaded");
					return null;
				}

			}
			// External Sotrage Permission not granted
			else
			{
				writeResult("Acces to read storage denied.");
				return null;
			}
#endif
			#endregion

//other platforms are not supported
#if !__ANDROID__ && !NETFX_CORE
			return null;
#endif
		}
	}
}
