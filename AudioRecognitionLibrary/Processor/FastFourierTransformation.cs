using AudioRecognitionLibrary.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioRecognitionLibrary.Processor
{
	/// <summary>
	/// Fast Fourier Transformation provider.
	/// </summary>
	public class FastFourierTransformation
	{
		/// <summary>
		/// FFT specialized for audio
		/// Inspired by LomontFFT <see cref="https://www.lomont.org/software/misc/fft/LomontFFT.cs"/> and classic wikipedia implementation <see cref="https://en.wikipedia.org/wiki/Cooley%E2%80%93Tukey_FFT_algorithm"/>.
		/// Data are stored as real and imaginary doubles alternating.
		/// Data length must be a power of two.
		/// </summary>
		/// <param name="data">Complex valued data stored as doubles.
		/// Alternating between real and imaginary parts.</param>
		/// <exception cref="ArgumentException">data length is not power of two</exception>
		public static void FFT(double[] data, bool normalize = false)
		{
			int n = data.Length;
			if (!Tools.Arithmetics.IsPowOfTwo(n))
				throw new ArgumentException($"Data length: {n} is not power of two.");

			n /= 2; //data are represented as 1 double for Real part && 1 double for Imaginary part

			BitReverse(data);

			int max = 1;
			while (n > max) // while loop represents logarithm for loop implementation of https://en.wikipedia.org/wiki/Cooley%E2%80%93Tukey_FFT_algorithm
			{
				int step = 2 * max; // 2^s form wiki
									//helper variables for Real and Img separate computations
				double omegaReal = 1;
				double omegaImg = 0;
				double omegaCoefReal = Math.Cos(Math.PI / max);
				double omegaCoefImg = Math.Sin(Math.PI / max);
				for (int m = 0; m < step; m += 2) //2 because of Real + Img double
				{
					//2*n because we have double the amount of data (Re+Img)
					for (int k = m; k < 2 * n; k += 2 * step)
					{
						double tmpReal = omegaReal * data[k + step] - omegaImg * data[k + step + 1]; //t real part from wiki
						double tmpImg = omegaImg * data[k + step] + omegaReal * data[k + step + 1]; //t img part from wiki
																									//A[k+j+m/2] from wiki
						data[k + step] = data[k] - tmpReal;
						data[k + step + 1] = data[k + 1] - tmpImg;
						//A[k+j] from wiki
						data[k] = data[k] + tmpReal;
						data[k + 1] = data[k + 1] + tmpImg;
					}
					//compute new omega
					double tmp = omegaReal;
					omegaReal = omegaReal * omegaCoefReal - omegaImg * omegaCoefImg;
					omegaImg = omegaImg * omegaCoefReal + tmp * omegaCoefImg;
				}

				max = step; //move logarithm loop
			}

			if (normalize)
				Normalize(data);
		}

		/// <summary>
		/// BitReverse for array of doubles valued as complex number alternating real and imaginary part.
		/// Swaps data for every two indexes that are bit-reverses to each other
		/// Taken from Lomont implementation.
		/// Implementation from Knuth's The Art Of Computer Programming.
		/// </summary>
		/// <param name="data"></param>
		internal static void BitReverse(double[] data)
		{
			int n = data.Length / 2;
			int first = 0, second = 0;

			int top = n / 2;

			while (true)
			{
				//swapping real parts
				data[first + 2] = data[first + 2].Swap(ref data[second + n]);
				//swapping imaginary parts
				data[first + 3] = data[first + 3].Swap(ref data[second + n + 1]);

				if (first > second) //first and second met -> swap two more
				{
					//first
					//swapping real parts
					data[first] = data[first].Swap(ref data[second]);
					//swapping imaginary parts
					data[first + 1] = data[first + 1].Swap(ref data[second + 1]);

					//second
					//swapping real parts
					data[first + n + 2] = data[first + n + 2].Swap(ref data[second + n + 2]);
					//swapping imaginary parts
					data[first + n + 3] = data[first + n + 3].Swap(ref data[second + n + 3]);
				}

				//moving counters to next bit-reversed indexes
				second += 4;
				if (second >= n)
					break;
				int finder = top;
				while (first >= finder)
				{
					first -= finder;
					finder /= 2;
				}
				first += finder;
			}
		}

		/// <summary>
		/// Normalize data after fft - classical
		/// </summary>
		/// <param name="data"></param>
		private static void Normalize(double[] data)
		{
			int n = data.Length / 2; //div 2 because of Re+Img
			for (int i = 0; i < data.Length; i++)
			{
				data[i] *= Math.Pow(n, -1 / 2);
			}
		}

		#region Window Functions
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
		#endregion
	}
}
