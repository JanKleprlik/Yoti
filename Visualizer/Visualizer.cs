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

		/// <summary>
		/// Create concrete Visualisation mode.
		/// </summary>
		/// <param name="soundBuffer">Sound buffer reference.</param>
		/// <param name="visualisationMode">Visualisation mode to be created.</param>
		/// <param name="downSampleCoef">Coefficient to downsample audio when visualisating.</param>
		/// <returns></returns>
		private static IVisualiserMode CreateMode(SoundBuffer soundBuffer, VisualisationModes visualisationMode, int downSampleCoef)
		{
			switch (visualisationMode)
			{
				case VisualisationModes.Amplitude:
					return new AmplitudeMode(soundBuffer);
				case VisualisationModes.Frequencies:
					return new FrequenciesMode(soundBuffer, downSampleCoef);
				case VisualisationModes.Spectrogram:
					return new Spectrogram(soundBuffer, downSampleCoef);
				default:
					throw new ArgumentException($"Mode {visualisationMode.ToString()} is not supported");
			}
		}

		internal readonly IVisualiserMode visualisation;
		private readonly SoundBuffer soundBuffer;
		private const int FPS = 30;
		private const int width = 1224;
		private const int height = 800;


		/// <summary>
		/// Runs main Visualisation loop.
		/// </summary>
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

		/// <summary>
		/// User input handler.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
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
