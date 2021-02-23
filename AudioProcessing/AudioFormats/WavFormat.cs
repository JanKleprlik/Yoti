using System;
using System.Collections.Generic;
using System.Text;

namespace AudioProcessing.AudioFormats
{
	public class WavFormat : IAudioFormat
	{
		public WavFormat(uint sampleRate, uint channels, int numOfDataSamples, short[] data)
		{
			Channels = channels;
			SampleRate = sampleRate;
			NumOfDataSamples = numOfDataSamples;
			Data = data;
		}

		public WavFormat(byte[] rawData)
		{

		}

		public uint Channels { get; set; }
		public uint SampleRate { get; set; }
		public int NumOfDataSamples { get; set; }
		public short[] Data { get; set; }

		public bool IsCorrectFormat(byte[] data)
		{
			//RIFF in ASCII
			if (data[0] == 0x52 && //R
				data[1] == 0x49 && //I
				data[2] == 0x46 && //F
				data[3] == 0x46)   //F
				return true;
			return false;
		}
	}
}
