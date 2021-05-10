using System;
using Visualizer.MusicModes;
using SFML.Audio;
using AudioRecognitionLibrary.AudioFormats;

namespace Visualizer
{
	public enum VisualisationModes
	{
		Amplitude,
		Frequencies,
		Spectrogram
	}
	
	public class Visualizer
	{
		#region Constructors
		public Visualizer(short[] data, uint channelCount, uint sampleRate, VisualisationModes vm, int downSampleCoef = 1)
		{
			soundBuffer = new SoundBuffer(data, channelCount, sampleRate);
			visualisation = CreateMode(soundBuffer, vm, downSampleCoef);
		}

		public Visualizer(IAudioFormat audio, VisualisationModes vm, int downSampleCoef = 1)
		{
			soundBuffer = new SoundBuffer(audio.Data, audio.Channels, audio.SampleRate);
			visualisation = CreateMode(soundBuffer, vm, downSampleCoef);
		}

		#endregion

		private static IVisualiserMode CreateMode(SoundBuffer sb, VisualisationModes vm, int downSampleCoef)
		{
			switch (vm)
			{
				case VisualisationModes.Amplitude:
					return new AmplitudeMode(sb);
				case VisualisationModes.Frequencies:
					return new FrequenciesMode(sb, downSampleCoef);
				case VisualisationModes.Spectrogram:
					return new Spectrogram(sb, downSampleCoef);
				default:
					throw new ArgumentException($"Mode {vm.ToString()} is not supported");
			}
		}

		internal readonly IVisualiserMode visualisation;
		private readonly SoundBuffer soundBuffer;
		private const int FPS = 30;
		private const int width = 1224;
		private const int height = 800;

		public void Run()
		{
			var mode = new SFML.Window.VideoMode(width, height);
			var window = new SFML.Graphics.RenderWindow(mode, visualisation.GetType().ToString());
			window.KeyPressed += Window_KeyPressed;
			window.SetFramerateLimit(FPS);

			// Start the game loop
			while (window.IsOpen)
			{
				visualisation.Update();
				window.Clear();

				// Process events
				window.DispatchEvents();
				visualisation.Draw(window);
				// Finally, display the rendered frame on screen
				window.Display();
			}

			visualisation.Quit();
			soundBuffer.Dispose();
			window.Close();
		}

		private void Window_KeyPressed(object sender, SFML.Window.KeyEventArgs e)
		{
			var window = (SFML.Window.Window)sender;
			if (e.Code == SFML.Window.Keyboard.Key.Escape)
			{
				window.Close();
			}
		}



	}
}
