using SFML.Audio;
using SFML.Graphics;
using SFML.System;

namespace Visualizer.MusicModes
{
	class AmplitudeMode : AbstractMode
	{
		#region Constructors
		public AmplitudeMode(string path) : base(new SoundBuffer(path))
		{
			VA = new VertexArray(PrimitiveType.LineStrip, BufferSize);
			Song.Loop = true;
			Song.Play();
		}
		public AmplitudeMode(byte[] data) : base(new SoundBuffer(data))
		{
			VA = new VertexArray(PrimitiveType.LineStrip, BufferSize);
			Song.Loop = true;
			Song.Play();
		}
		public AmplitudeMode(short[] data, uint channelCount, uint sampleRate) : base(new SoundBuffer(data, channelCount, sampleRate))
		{
			VA = new VertexArray(PrimitiveType.LineStrip, BufferSize);
			Song.Loop = true;
			Song.Play();
		}

		public AmplitudeMode(SoundBuffer sb) : base(sb)
		{
			VA = new VertexArray(PrimitiveType.LineStrip, BufferSize);
			font = new Font("Resources/sansation.ttf");
			timeText = new Text("0", font);
			timeText.Position = new Vector2f(10f, 10f);
			timeText.CharacterSize = 30;


			Song.Loop = true;
			Song.Play();
		}

		#endregion


		private VertexArray VA;
		private SFML.Graphics.Text timeText;
		private Font font;


		public override void Draw(RenderWindow window)
		{
			window.Draw(VA);
			window.Draw(timeText);
		}

		public override void Update()
		{
			int offset = (int)(Song.PlayingOffset.AsSeconds() * SampleRate);
			timeText.DisplayedString = Song.PlayingOffset.AsSeconds().ToString();
			if (offset + BufferSize < SampleCount)
			{
				if (ChannelCount == 2)
				{
					//Takes every second sample (only the Left ones)
					for (uint i = 0; i < BufferSize; i++)
					{
						VA[i] = new Vertex(new Vector2f(i / 4 + 100, (float)(200 + Samples[(i + offset) * 2] * 0.008)));
					}
				}
				else
				{
					for (uint i = 0; i < BufferSize; i++)
					{
						VA[i] = new Vertex(new Vector2f(i / 4 + 100, (float)(200 + Samples[(i + offset)] * 0.008)));
					}
				}

			}
		}

	}
}
