using System;
using System.Collections.Generic;
using System.Text;

namespace AudioProcessing.AudioFormats
{
	public interface IAudioFormat
	{
		uint Channels { get; set; }
		uint SampleRate { get; set; }
		int NumOfDataSamples { get; set; }
		//Song PlayData
		short[] Data { get; set; }
		bool IsCorrectFormat(byte[] data);
	}
}
