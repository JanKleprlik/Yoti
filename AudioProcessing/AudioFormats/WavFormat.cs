using System;
using System.Collections.Generic;
using System.Linq;
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
			if (!IsCorrectFormat(new[] { rawData[0], rawData[1], rawData[2], rawData[3] }))
			{
				throw new InvalidCastException("Invalid data format given to Wav format constructor.");
			}

			int fmtOffset = FindOffset(rawData, new byte[] { 0x66, 0x6D, 0x74, 0x20 });

			this.Channels = Tools.Converter.BytesToUInt(new byte[] { rawData[fmtOffset + 2], rawData[fmtOffset + 3] });
			this.SampleRate = Tools.Converter.BytesToUInt(new byte[] { rawData[fmtOffset + 4], rawData[fmtOffset + 5], rawData[fmtOffset + 6], rawData[fmtOffset + 7] });

			int dataOffset = FindOffset(rawData, new byte[] { 0x64, 0x61, 0x74, 0x61 });

			//nubmer of bytes divide by two (short = 2 bytes && 1 sample = 1 short)
			this.NumOfDataSamples = Tools.Converter.BytesToInt(new byte[]
				{rawData[dataOffset - 4], rawData[dataOffset - 3], rawData[dataOffset - 2], rawData[dataOffset - 1]}) / 2;
			var byteData = rawData.Skip(dataOffset).Take(this.NumOfDataSamples * 2).ToArray();
			this.Data = GetSoundDataFromBytes(byteData);
		}

		public uint Channels { get; set; }
		public uint SampleRate { get; set; }
		public int NumOfDataSamples { get; set; }
		public short[] Data { get; set; }

		public static bool IsCorrectFormat(byte[] data)
		{
			//RIFF in ASCII
			if (data[0] == 0x52 && //R
				data[1] == 0x49 && //I
				data[2] == 0x46 && //F
				data[3] == 0x46)   //F
				return true;
			return false;
		}


		private static int FindDataOffset(byte[] data)
		{
			for (int i = 0; i < data.Length - 3; i++)
			{
				if (data[i] == 0x64 && //d
					data[i + 1] == 0x61 && //a
					data[i + 2] == 0x74 && //t
					data[i + 3] == 0x61)//a
				{
					return i + 8; //+4 is for 'data' bytes, +4 is for integer representing number of data bytes
				}
			}

			throw new ArgumentException("Part with data not found.");
		}

		/// <summary>
		/// Finds offset of data same as anchor
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static int FindOffset(byte[] data, byte[] anchor)
		{
			if (data.Length < anchor.Length)
				throw new ArgumentException("Unable to find offset: anchor is longer than data");
			for (int i = 0; i <= data.Length - anchor.Length; i++)
			{
				if (data[i] == anchor[0])
				{
					int correct = 1;
					for (int j = 1; j < anchor.Length; j++)
					{
						if (data[i + j] == anchor[j])
						{
							correct++;
						}
						else
						{
							break;
						}
					}
					if (correct == anchor.Length)
					{
						return i + 8;
					}
				}
			}
			throw new ArgumentException("Part with data not found.");
		}

		/// <summary>
		/// Transforms byte audio data into short audio data
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static short[] GetSoundDataFromBytes(byte[] data)
		{
			short[] dataShorts = new short[data.Length / 2];

			for (int i = 0; i < data.Length; i += 2)
			{
				dataShorts[i / 2] = Tools.Converter.BytesToShort(new byte[] { data[i], data[i + 1] });
			}

			return dataShorts;
		}
	}
}
