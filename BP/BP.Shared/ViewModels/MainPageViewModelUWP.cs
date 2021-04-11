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
		private async Task<IAudioFormat> getAudioFormatFromRecodingUWP()
		{
			byte[] recordedSong = await audioRecorder.GetDataFromStream(); //exception can be propagated
			// Metadata about recording are included
			return new WavFormat(recordedSong);
		}

	}
}

#endif