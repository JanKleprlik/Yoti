using System;
using SFML.Audio;
using SFML.Graphics;

namespace Visualizer.MusicModes
{
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
		public abstract void Draw(RenderWindow window);
		public abstract void Update();

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
