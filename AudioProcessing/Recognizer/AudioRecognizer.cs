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
		public AudioRecognizer()
		{
			//TODO: setup database SQL_lite 3 ???
		}

		public void AddNewSong(IAudioFormat audio)
		{
			AudioProcessor.ConvertToMono(audio);

			double[] data = Array.ConvertAll(audio.Data, item => (double)item);

			AudioProcessor.DownSample(data, Parameters.DownSampleCoef, audio.SampleRate);

				
		}

		public string RecognizeSong()
		{
			return "Not recognized";
		}

		public void ListSongs(TextWriter output)
		{
			
		}
	}
}
