using AudioProcessing;
using AudioProcessing.AudioFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Database
{
	public class Song
	{
		public uint Id { get; set; }

		public string Name { get; set; }

		public string Author { get; set; }

		public int BPM { get; set; }
	}

	public class SongWavFormat
	{
		//public WavFormat Format { get; set; }
		public List<TimeFrequencyPoint> TFPs { get; set; }
		public string Name { get; set; }
		public string Author { get; set; }
		public int BPM {get; set;}
	}

}
