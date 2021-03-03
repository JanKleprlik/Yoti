using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioProcessing.AudioFormats;
using AudioProcessing.Processor;

namespace AudioProcessing.Recognizer
{
	public partial class AudioRecognizer
	{
		public void AddNewSong(IAudioFormat audio)
		{
			AudioProcessor.ConvertToMono(audio);

			double[] data = Array.ConvertAll(audio.Data, item => (double)item);

			double[] downsampledData = AudioProcessor.DownSample(data, Parameters.DownSampleCoef, audio.SampleRate);

			int bufferSize = Parameters.WindowSize / Parameters.DownSampleCoef; //default: 4096/4 = 1024
			//var TimeFrequencyPoitns = CreateTimeFrequencyPoints(bufferSize, downsampledData, sensitivity: 1);
		}

		public string RecognizeSong()
		{
			return "Not recognized";
		}
	}
}
