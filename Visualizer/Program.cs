using AudioProcessing.Processor;
using System;

namespace Visualizer
{
	class Program
	{
		static void Main(string[] args)
		{
			//string[] files = new string[] {
			//	"Home.wav",
			//	"home_3.wav",
			//	"home_5.wav",
			//	"home_8.wav",
			//	"home_5b.wav",
			//	"Havana.wav",
			//	"havana_3.wav",
			//	"havana_5.wav",
			//	"havana_8.wav",
			//};
			//AudioProcessing.Recognizer.AudioRecognizer recognizer = new AudioProcessing.Recognizer.AudioRecognizer();
			//foreach(var file in files)
			//{
			//	var audio = Recorder.GetAudio($"Resources/Songs/{file}");
			//	Console.WriteLine($"FILE: {file}");
			//	Console.WriteLine(recognizer.GetBPM(audio));
			//}


			AudioProcessor.ConvertToMono(audio);
			var window = new global::Visualizer.Visualizer(audio.Data, audio.Channels, audio.SampleRate, VisualisationModes.Spectrogram);
			window.Run();
		}
	}
}
