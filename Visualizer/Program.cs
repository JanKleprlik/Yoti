using AudioRecognitionLibrary.Processor;
using System;

namespace Visualizer
{
	class Program
	{
		static void Main(string[] args)
		{
			string[] files = new string[] {
				"Home.wav",
				"home_3.wav",
				"home_5.wav",
				"home_8.wav",
				"home_5b.wav",
				"Havana.wav",
				"havana_3.wav",
				"havana_5.wav",
				"havana_8.wav",
			};
			AudioRecognitionLibrary.Recognizer.AudioRecognizer recognizer = new AudioRecognitionLibrary.Recognizer.AudioRecognizer();
			Console.WriteLine($"BPMLowFreq: {AudioRecognitionLibrary.Recognizer.AudioRecognizer.Parameters.BPMLowFreq}\n" +
								$"BPMHighFreq: {AudioRecognitionLibrary.Recognizer.AudioRecognizer.Parameters.BPMHighFreq}\n" +
								$"PartsPerSecond: {AudioRecognitionLibrary.Recognizer.AudioRecognizer.Parameters.PartsPerSecond}\n" +
								$"BPMLowLimit: {AudioRecognitionLibrary.Recognizer.AudioRecognizer.Parameters.BPMLowLimit}\n" +
								$"BPMHighLimit: {AudioRecognitionLibrary.Recognizer.AudioRecognizer.Parameters.BPMHighLimit}\n" +
								$"PeakNeighbourRange: {AudioRecognitionLibrary.Recognizer.AudioRecognizer.Parameters.PeakNeighbourRange}");
			foreach (var file in files)
			{

				var audio = Recorder.GetAudio($"Resources/Songs/{file}");
				Console.WriteLine($"FILE: {file}");
				Console.WriteLine(recognizer.GetBPM(audio, true));
			}

			//string file = "Home.wav";
			//var audio = Recorder.GetAudio($"Resources/Songs/{file}");
			//AudioProcessor.ConvertToMono(audio);
			//var window = new global::Visualizer.Visualizer(audio.Data, audio.Channels, audio.SampleRate, VisualisationModes.Spectrogram);
			//window.Run();
		}
	}
}
