using System;
using SFML.Audio;
using SFML.Graphics;

namespace Visualizer.MusicModes
{
	/// <summary>
	/// Abstract class of Visualisation. <br></br>
	/// All visualization modes must inherit this class.
	/// </summary>
	abstract class AbstractMode : IVisualiserMode
	{
		#region Constructors
		protected AbstractMode(SoundBuffer sb)
		{
			Song = new Sound(sb);
			SampleRate = sb.SampleRate;
			SampleCount = (uint)sb.Samples.Length;
			BufferSize = 4096;
			Samples = sb.Samples;
			if (sb.ChannelCount > 2)
			{
				throw new ArgumentException($"Too many channels: {sb.ChannelCount}");
			}
			ChannelCount = sb.ChannelCount;
		}

		#endregion

		#region Properties
		protected Sound Song { get; set; }
		protected short[] Samples { get; }
		protected uint SampleRate { get; }
		protected uint SampleCount { get; }
		protected uint BufferSize { get; }
		protected uint ChannelCount { get; }
		#endregion

		#region  API
		/// <summary>
		/// Render visualisation.
		/// </summary>
		/// <param name="window">Window to render to.</param>
		public abstract void Draw(RenderWindow window);
		/// <summary>
		/// Update visualisation step.
		/// </summary>
		public abstract void Update();

		/// <summary>
		/// End visualisation.
		/// </summary>
		public virtual void Quit()
		{
			if (Song != null)
			{
				Song.Stop();
				Song.Dispose();
			}
		}

		#endregion

	}
}
