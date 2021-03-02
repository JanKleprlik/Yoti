using System;
using AudioProcessing.Processor;
using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace Visualizer.MusicModes
{
	class Spectrogram : AbstractSpectrogram
	{
		public Spectrogram(SoundBuffer sb, int downSampleCoef) : base(sb)
		{
			//windowSize = (int)(BufferSize / downSampleCoef);
			window = AudioProcessor.GenerateHammingWindow((uint)(BufferSize / downSampleCoef));
			const int secInFrame = 60;
			rendersTillStop = (int)(secInFrame / ((double)BufferSize / SampleRate));
			this.downSampleCoef = downSampleCoef;
			bins = new double[(int)(BufferSize * (2d / downSampleCoef))]; //*2 because of Re+Im


			font = new Font("Resources/sansation.ttf");
			instructionsText = new Text("Press Enter to load next frame.", font);
			instructionsText.Position = new Vector2f(10f, 10f);
			instructionsText.CharacterSize = 16;



			//free up space
			Song.Dispose();
			Song = null;
		}




		/// <summary>
		/// <para>Accepts data array of strictly 1024 values (representing Complex values where Re and Im are alternating)
		/// that will be displayed as single row of spectrum.</para>
		/// <para>X-axis is time</para>
		/// <para>Y-axis is frequency</para>
		/// </summary>
		/// <param name="data"></param>
		/// <param name="basePosition"></param>
		public override void Render(double[] data, Vector2f basePosition, int binsToRender)
		{
			if (VA.VertexCount != binsToRender)
				VA.Resize((uint)binsToRender);

			if (!AudioProcessing.Tools.Arithmetics.IsPowOfTwo(data.Length))
				throw new ArgumentException($"Data length: {data.Length} is not power of two.");

			double Yscale = binsToRender / 512d; //scale so spectagram fits the screen
			for (uint i = 0; i < binsToRender; i++)
			{
				var color = IntensityToColor(data[i * 2], data[i * 2 + 1], 2048);

				VA[i] = new Vertex(basePosition + new Vector2f(0, (float)(-i / Yscale)), color);
			}
		}

		private double[] bins;
		private double[] window;
		//private int windowSize;
		private int rendersTillStop;
		private int offset = 0;
		private int downSampleCoef;
		private bool isFirstFrame = true;

		//text
		private SFML.Graphics.Text instructionsText;
		private Font font;

		public override void Draw(RenderWindow window)
		{
			if (!isFirstFrame)
				waitForEnter();
			else
				isFirstFrame = false;

			double[] samples = new double[BufferSize];


			for (int i = 0; i < rendersTillStop; i++)
			{
				if (offset + BufferSize < SampleCount)
				{
					for (int counter = 0; counter < BufferSize; counter++) //one line of spectogram
					{
						samples[counter] = Samples[offset + counter]; //4k samples
					}

					offset += (int)BufferSize;
					//filter out high frequencies and downsample audio data
					var cutOffData = AudioProcessor.DownSample(samples, downSampleCoef, SampleRate); //1k samples
					for (int index = 0; index < BufferSize / downSampleCoef; index++)
					{
						//data[index * 2] = cutOffData[index] * hammingWindow[index]; //apply hamming window
						bins[index * 2] = cutOffData[index] * this.window[index]; //apply hamming window
						bins[index * 2 + 1] = 0d; //set 0s for Img complex values
					}

					FastFourierTransformation.FFT(bins); //apply fft
														 //1024 = WIDTH			700=HEIGHT
					Render(bins, new Vector2f(100 + i * (1024 / rendersTillStop), 700), (int)BufferSize / (2 * downSampleCoef));
					window.Draw(VA);
				}
				else
				{
					//Draw last frame and then close window

					window.Draw(instructionsText);
					window.Display();
					waitForEnter();
					window.Close();
					return;
				}
			}


			window.Draw(instructionsText);
		}

		private void waitForEnter()
		{
			while (!SFML.Window.Keyboard.IsKeyPressed(Keyboard.Key.Enter))
			{ }
		}
		public override void Update()
		{
			//do nothing?
		}
	}
}
