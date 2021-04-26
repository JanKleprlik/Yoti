using System;
using System.Collections.Generic;
using System.Text;

using AudioRecognitionLibrary.AudioFormats;
using AudioRecognitionLibrary.Tools;


namespace AudioRecognitionLibrary.Processor
{
	static class AudioProcessor
	{
		/// <summary>
		/// Resamples data from multiple channels to single one.
		/// </summary>
		/// <param name="audio">audio with</param>
		public static void ConvertToMono(IAudioFormat audio)
		{
			if (audio == null)
				throw new ArgumentNullException("Argument 'audio' is null.");
			if (audio.Data == null)
				throw new ArgumentNullException("Argument 'audio.Data' is null");

			switch (audio.Channels)
			{
				case 1:
					break;

				case 2:
					short[] mono = new short[audio.NumOfDataSamples / 2];

					for (int i = 0; i < audio.NumOfDataSamples; i += 2) //4 bytes per loop are processed (2 left + 2 right samples)
					{
						mono[i / 2] = Arithmetics.Average(audio.Data[i], audio.Data[i + 1]);
					}

					// number of bytes is halved (Left and Right bytes are merget into one)
					audio.Data = mono; //set new SampleData
					audio.Channels = 1; //lower number of channels
					audio.NumOfDataSamples /= 2;  //lower number of data samples
					break;
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

		public static double[] GenerateHammingWindow(uint windowSize)
		{
			var Window = new double[windowSize];

			for (uint i = 0; i < windowSize; i++)
			{
				Window[i] = 0.54 - 0.46 * Math.Cos((2 * Math.PI * i) / windowSize);
			}

			return Window;
		}
		public static double[] GenerateBlackmannHarrisWindow(uint windowSize)
		{
			var Window = new double[windowSize];

			for (uint i = 0; i < windowSize; i++)
			{
				Window[i] = 0.35875 - (0.48829 * Math.Cos((2 * Math.PI * i) / windowSize)) + (0.14128 * Math.Cos((4 * Math.PI * i) / windowSize)) - (0.01168 * Math.Cos((6 * Math.PI * i) / windowSize));
			}

			return Window;
		}
		public static double[] GenerateHannWindow(uint windowSize)
		{
			var Window = new double[windowSize];

			for (uint i = 0; i < windowSize; i++)
			{
				Window[i] = 0.5 * (1 - Math.Cos((2 * Math.PI * i) / windowSize));
			}

			return Window;
		}

		public static List<TimeFrequencyPoint> CreateTimeFrequencyPoints(int bufferSize, double[] data, double sensitivity = 0.9)
		{
			List<TimeFrequencyPoint> TimeFrequencyPoitns = new List<TimeFrequencyPoint>();
			double[] HammingWindow = GenerateHammingWindow((uint)bufferSize);
			double Avg = 0d;// = GetBinAverage(data, HammingWindow);

			int offset = 0;
			var sampleData = new double[bufferSize * 2]; //*2  because of Re + Im
			uint AbsTime = 0;
			while (offset < data.Length)
			{
				if (offset + bufferSize < data.Length)
				{
					for (int i = 0; i < bufferSize; i++) //setup for FFT
					{
						sampleData[i * 2] = data[i + offset] * HammingWindow[i];
						sampleData[i * 2 + 1] = 0d;
					}

					FastFourierTransformation.FFT(sampleData);
					double[] maxs =
					{
						GetStrongestBin(data, 0, 10),
						GetStrongestBin(data, 10, 20),
						GetStrongestBin(data, 20, 40),
						GetStrongestBin(data, 40, 80),
						GetStrongestBin(data, 80, 160),
						GetStrongestBin(data, 160, 512),
					};


					for (int i = 0; i < maxs.Length; i++)
					{
						Avg += maxs[i];
					}

					Avg /= maxs.Length;
					//get doubles of frequency and time 
					RegisterTFPoints(sampleData, Avg, AbsTime, ref TimeFrequencyPoitns, sensitivity);

				}

				offset += bufferSize;
				AbsTime++;
			}

			return TimeFrequencyPoitns;
		}

		/// <summary>
		/// Returns normalized value of the strongest bin in given bounds
		/// </summary>
		/// <param name="bins">Complex values alternating Real and Imaginary values</param>
		/// <param name="from">lower bound</param>
		/// <param name="to">upper bound</param>
		/// <returns>Normalized value of the strongest bin</returns>
		private static double GetStrongestBin(double[] bins, int from, int to)
		{
			var max = double.MinValue;
			for (int i = from; i < to; i++)
			{
				var normalized = 2 * Math.Sqrt((bins[i * 2] * bins[i * 2] + bins[i * 2 + 1] * bins[i * 2 + 1]) / 2048);
				var decibel = 20 * Math.Log10(normalized);

				if (decibel > max)
				{
					max = decibel;
				}

			}

			return max;
		}

		/// <summary>
		/// Filter outs the strongest bins of logarithmically scaled parts of bins. Chooses the strongest and remembers it if its value is above average. Those points are
		/// chornologically added to the <c>timeFrequencyPoints</c> List.
		/// </summary>
		/// <param name="data">bins to choose from, alternating real and complex values as doubles. Must contain 512 complex values</param>
		/// <param name="average">Limit that separates weak spots from important ones.</param>
		/// <param name="absTime">Absolute time in the song.</param>
		/// <param name="timeFrequencyPoitns">List to add points to.</param>
		private static void RegisterTFPoints(double[] data, in double average, in uint absTime, ref List<TimeFrequencyPoint> timeFrequencyPoitns, double coefficient = 0.9)
		{
			int[] BinBoundries =
			{
				//low   high
				0 , 10,
				10, 20,
				20, 40,
				40, 80,
				80, 160,
				160,512
			};

			//loop through logarithmically scalled sections of bins
			for (int i = 0; i < BinBoundries.Length / 2; i++)
			{
				//get strongest bin from a section if its above average
				var idx = GetStrongestBinIndex(data, BinBoundries[i * 2], BinBoundries[i * 2 + 1], average, coefficient);
				if (idx != null)
				{
					//idx is divided by 2 because of (Re + Im)
					timeFrequencyPoitns.Add(new TimeFrequencyPoint { Time = absTime, Frequency = (uint)idx/2});
				}
			}
		}

		/// <summary>
		/// Finds the strongest bin above limit in given segment.
		/// </summary>
		/// <param name="bins">Complex values alternating Real and Imaginary values</param>
		/// <param name="from">lower bound</param>
		/// <param name="to">upper bound</param>
		/// <param name="limit">limit indicating weak bin</param>
		/// <param name="sensitivity">sensitivity of the limit (the higher the lower sensitivity)</param>
		/// <returns>index of strongest bin or null if none of the bins is strong enought</returns>
		private static int? GetStrongestBinIndex(double[] bins, int from, int to, double limit, double sensitivity = 0.9d)
		{
			var max = double.MinValue;
			int? index = null;
			for (int i = from; i < to; i++)
			{
				var normalized = 2 * Math.Sqrt((bins[i * 2] * bins[i * 2] + bins[i * 2 + 1] * bins[i * 2 + 1]) / 2048);
				var decibel = 20 * Math.Log10(normalized);

				if (decibel > max && decibel * sensitivity > limit)
				{
					max = decibel;
					index = i * 2;
				}

			}

			return index;
		}

	}
}
