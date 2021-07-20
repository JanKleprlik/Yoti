using AudioRecognitionLibrary.Processor;
using System;

namespace Visualizer
{
	class Program
	{
		static void Main(string[] args)
		{
			// Write names of the songs you want to visualize
			string[] files = new string[] {
				"Home.wav",
			};


			AudioRecognitionLibrary.Recognizer.AudioRecognizer recognizer = new AudioRecognitionLibrary.Recognizer.AudioRecognizer();

			foreach (var file in files)
			{
				// Compute BPM and write result into the console
				var audio = Recorder.GetAudio($"Resources/Songs/{file}");
				Console.WriteLine($"FILE: {file}");
				Console.WriteLine(recognizer.GetBPM(audio, true));
				
				
				// Run visualisation
				AudioProcessor.ConvertToMono(audio);
				// Change last parameter to see other visualisations
				var window = new global::Visualizer.Visualizer(audio.Data, audio.Channels, audio.SampleRate, VisualisationModes.Frequencies);
				window.Run();
			}
		}
	}
}
