using AudioProcessing.Processor;
using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using System.Threading.Tasks;

namespace Visualizer.MusicModes
{
	class FrequenciesMode : AbstractMode
	{
		public FrequenciesMode(SoundBuffer sb, int downSampleCoef) : base(sb)
		{
			VA = new VertexArray(PrimitiveType.LineStrip, (uint)(BufferSize / (2 * downSampleCoef)));
			font = new Font("Resources/sansation.ttf");
			timeText = new Text("0", font);
			timeText.Position = new Vector2f(10f, 10f);
			timeText.CharacterSize = 30;
			this.downSampleCoef = downSampleCoef;


			window = AudioProcessor.GenerateHammingWindow((uint)(BufferSize / downSampleCoef));
			bin = new double[(int)(BufferSize * (2d / downSampleCoef))];

			Song.Loop = true;
			Song.Play();
		}

		private VertexArray VA { get; set; }
		private SFML.Graphics.Text timeText;
		private Font font;
		private int downSampleCoef;
		private double[] bin { get; set; }
		private double[] window { get; set; }
		public override void Draw(RenderWindow window)
		{
			window.Draw(VA);
			window.Draw(timeText);
		}


		public override void Update()
		{
			int offset = (int)(Song.PlayingOffset.AsSeconds() * SampleRate);
			timeText.DisplayedString = Song.PlayingOffset.AsSeconds().ToString();
			double[] samples = new double[BufferSize]; //allocate array taht will be used at downsampling

			Task t1 = Task.Factory.StartNew(() => setImaginaryAsync());

			if (offset + BufferSize < SampleCount)
			{
				if (ChannelCount == 2)
				{
					for (uint i = 0; i < BufferSize; i++)
					{
						samples[i] = Samples[(i + offset) * 2];
					}
				}
				else
				{
					for (uint i = 0; i < BufferSize; i++)
					{
						samples[i] = Samples[(i + offset)];

					}
				}
			}

			//Filter out frequencies and then downsamples
			var cutOffData = AudioProcessor.DownSample(samples, downSampleCoef, SampleRate);

			t1.Wait(); //t1 is working with bin array
					   //enter complex values to the bin array
			for (int i = 0; i < BufferSize / downSampleCoef; i++)
			{
				bin[i * 2] = cutOffData[i] * window[i];
			}

			FastFourierTransformation.FFT(bin);

			for (uint i = 0; i < BufferSize / (2 * downSampleCoef); i++)
			{
				VA[i] = new Vertex(new Vector2f((float)(i * 0.5 * downSampleCoef + 100),
					(float)(200 - AudioProcessing.Tools.Arithmetics.GetComplexAbs(bin[2 * i], bin[2 * i + 1]) / 100000))); //100000 is to scale visualisation so it fits the window
			}

		}

		private void setImaginaryAsync()
		{
			for (int i = 0; i < BufferSize / downSampleCoef; i++)
			{
				bin[i * 2 + 1] = 0d;
			}
		}
	}
}
