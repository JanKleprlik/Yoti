using AudioProcessing.Processor;
using System;

namespace Visualizer
{
	class Program
	{
		static void Main(string[] args)
		{
			var audio = Recorder.GetAudio("Resources/Songs/Hertz.wav");
			AudioProcessor.ConvertToMono(audio);
			var window = new global::Visualizer.Visualizer(audio.Data, audio.Channels, audio.SampleRate, VisualisationModes.Spectrogram);
			window.Run();
		}
	}
}
