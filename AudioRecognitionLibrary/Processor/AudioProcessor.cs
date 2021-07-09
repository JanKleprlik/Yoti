using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using AudioRecognitionLibrary.AudioFormats;
using AudioRecognitionLibrary.Tools;

[assembly: InternalsVisibleToAttribute("Visualizer")]
namespace AudioRecognitionLibrary.Processor
{
	/// <summary>
	/// Class with aduio processing methods not tightly related to the recognition algorithm used in song recognition algorithm.
	/// </summary>
	internal static class AudioProcessor
	{
		/// <summary>
		/// Resamples data from multiple channels to single one.
		/// </summary>
		/// <param name="audio">audio with</param>
		public static short[] ConvertToMono(IAudioFormat audio)
		{
			if (audio == null)
				throw new ArgumentNullException("Argument 'audio' is null.");
			if (audio.Data == null)
				throw new ArgumentNullException("Argument 'audio.Data' is null");

			switch (audio.Channels)
			{
				case 1:
					return audio.Data;

				case 2:
					short[] mono = new short[audio.NumOfDataSamples / 2];

					for (int i = 0; i < audio.NumOfDataSamples; i += 2) //4 bytes per loop are processed (2 left + 2 right samples)
					{
						mono[i / 2] = Arithmetics.Average(audio.Data[i], audio.Data[i + 1]);
					}

					return mono;
				default:
					throw new NotImplementedException($"Convert from {audio.Channels} channels to mono is not supported.");
			}
		}

		/// <summary>
		/// Downsamples data by a <c>downFactor</c>
		/// </summary>
		/// <param name="data">data to downsample</param>
		/// <param name="downFactor">factor of downsampling</param>
		/// <param name="sampleRate">original sample rate</param>
		/// <returns>Downsampled data</returns>
		public static double[] DownSample(double[] data, int downFactor, double sampleRate)
		{
			if (downFactor == 0 || downFactor == 1)
				return data;
			var res = new double[data.Length / downFactor];

			//filter out frequencies larger than the one that will be available
			//after downsampling by downFactor. To aviod audio aliasing.
			double cutOff = sampleRate / downFactor;
			double[] dataDouble = new double[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				dataDouble[i] = data[i];
			}


			var dataDoubleDownsampled = ButterworthFilter.Butterworth(dataDouble, sampleRate, cutOff); //4k samples

			//make average of every downFactor number of samples
			for (int i = 0; i < dataDoubleDownsampled.Length / downFactor; i++) //1k samples
			{
				double sum = 0;
				for (int j = 0; j < downFactor; j++)
				{
					sum += dataDouble[i * downFactor + j];
				}
				res[i] = sum / downFactor;
			}

			return res;
		}

	}
}
