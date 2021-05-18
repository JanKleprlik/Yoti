using System;
using System.Collections.Generic;
using System.Text;

namespace AudioRecognitionLibrary.AudioFormats
{
	public interface IAudioFormat
	{
		/// <summary>
		/// Number of audio channels
		/// </summary>
		uint Channels { get; set; }

		/// <summary>
		/// Sample rate of the audio.
		/// </summary>
		uint SampleRate { get; set; }
		/// <summary>
		/// Number of Data Samples
		/// </summary>
		int NumOfDataSamples { get; set; }
		/// <summary>
		/// Raw audio data.
		/// </summary>
		short[] Data { get; set; }
	}
}