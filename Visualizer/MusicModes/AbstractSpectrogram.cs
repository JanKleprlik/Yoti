using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using System;

namespace Visualizer.MusicModes
{
	abstract class AbstractSpectrogram : AbstractMode
	{
		protected AbstractSpectrogram(SoundBuffer sb) : base(sb)
		{
			VA = new VertexArray(PrimitiveType.Points, 512);
		}

		protected VertexArray VA { get; set; }

		/// <summary>
		/// Render one column of spectrogram.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="basePosition"></param>
		/// <param name="binsToRender"></param>
		public abstract void Render(double[] data, Vector2f basePosition, int binsToRender);

		/// <summary>
		/// <para>Mapping of intesity of data.</para>
		/// <para>Low: Black</para>
		/// <para>High: White</para>
		/// </summary>
		/// <param name="n">Size of the fft window.</param>
		/// <returns>Color of the pixel.</returns>
		protected static Color IntensityToColor(double real, double imaginary, int n)
		{
			//Black : 0,0,0
			//White: 255,255,255
			var normalized = 2 * Math.Sqrt((real * real + imaginary * imaginary) / n);
			var decibel = 20 * Math.Log10(normalized);
			byte colorIntensity;
			if (decibel < 0)
			{
				colorIntensity = 0;
			}
			else if (decibel > 255)
			{
				colorIntensity = 255;
			}
			else
			{
				colorIntensity = (byte)(int)decibel;
			}

			return new Color(colorIntensity, colorIntensity, colorIntensity);
		}
	}
}
