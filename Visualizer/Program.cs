using AudioProcessing.Processor;
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
			AudioProcessing.Recognizer.AudioRecognizer recognizer = new AudioProcessing.Recognizer.AudioRecognizer();
			Console.WriteLine($"BPMLowFreq: {AudioProcessing.Recognizer.AudioRecognizer.Parameters.BPMLowFreq}\n" +
								$"BPMHighFreq: {AudioProcessing.Recognizer.AudioRecognizer.Parameters.BPMHighFreq}\n" +
								$"PartsPerSecond: {AudioProcessing.Recognizer.AudioRecognizer.Parameters.PartsPerSecond}\n" +
								$"BPMLowLimit: {AudioProcessing.Recognizer.AudioRecognizer.Parameters.BPMLowLimit}\n" +
								$"BPMHighLimit: {AudioProcessing.Recognizer.AudioRecognizer.Parameters.BPMHighLimit}\n" +
								$"PeakNeighbourRange: {AudioProcessing.Recognizer.AudioRecognizer.Parameters.PeakNeighbourRange}");
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
