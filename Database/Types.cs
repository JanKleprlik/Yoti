using AudioRecognitionLibrary;
using AudioRecognitionLibrary.AudioFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Database
{
	public class Song
	{
		public uint id { get; set; }

		public string name { get; set; }

		public string author { get; set; }

		public string lyrics { get; set; }

		public int bpm { get; set; }

		public override string ToString()
		{
			return $"{id} ----  {name} ---- {author} ---- {bpm}";
		}
	}

	public class PreprocessedSongData
	{
		//public WavFormat Format { get; set; }
		public List<TimeFrequencyPoint> tfps { get; set; }
		public string name { get; set; }
		public string author { get; set; }
		public string lyrics { get; set; }
		public int bpm {get; set;}
	}

	public class RecognitionResult
	{
		public Song song { get; set; }

		public string detailinfo { get; set; }
	}

}
