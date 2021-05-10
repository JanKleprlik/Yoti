﻿#if __ANDROID__

using AudioRecognitionLibrary.AudioFormats;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace BP.Shared.ViewModels
{
	public partial class MainPageViewModel : BaseViewModel
	{
		private async Task<IAudioFormat> getAudioFormatFromRecordingANDROID()
		{
			byte[] recordedSong = await audioRecorder.GetDataFromStream();


			// On Android we only get raw data without .
			// So I have to convert them manually to shorts and then use different manual constructor
			short[] recordedDataShort = AudioRecognitionLibrary.Tools.Converter.BytesToShorts(recordedSong);

			return new WavFormat(
				AudioRecorder.Recorder.Parameters.SamplingRate,
				AudioRecorder.Recorder.Parameters.Channels,
				recordedDataShort.Length,
				recordedDataShort);

		}

	}
}
#endif