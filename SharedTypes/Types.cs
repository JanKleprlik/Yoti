using AudioRecognitionLibrary;
using AudioRecognitionLibrary.AudioFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SharedTypes
{
	/// <summary>
	/// Class keeping information about a song.
	/// </summary>
	public class Song
	{
		/// <summary>
		/// Id of the song.
		/// </summary>
		public uint Id { get; set; }
		/// <summary>
		/// Name of the song.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Name of the author of the song.
		/// </summary>
		public string Author { get; set; }
		/// <summary>
		/// Lyrics of the song.
		/// </summary>
		public string Lyrics { get; set; }
		/// <summary>
		/// BPM of the song.
		/// </summary>
		public int BPM { get; set; }

		public override string ToString()
		{
			return $"{Id} ----  {Name} ---- {Author} ---- {BPM}";
		}
	}
	/// <summary>
	/// Preprocessed song wrapper used for client-server comunication.
	/// </summary>
	public class PreprocessedSongData
	{
		/// <summary>
		/// Fingerprint of the song.<br></br>
		/// [address;(AbsAnchorTimes)]
		/// </summary>
		public Dictionary<uint, List<uint>> Fingerprint { get; set; }
		
		/// <summary>
		/// Number of TPFs to determine match accuracy on recognition.
		/// </summary>
		public int TFPCount { get; set; }

		/// <summary>
		/// Name of the song.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Name of the author of the song.
		/// </summary>
		public string Author { get; set; }
		/// <summary>
		/// Lyrics of the song.
		/// </summary>
		public string Lyrics { get; set; }
		/// <summary>
		/// BPM of the song.
		/// </summary>
		public int BPM {get; set;}
	}

	/// <summary>
	/// Recognition result wrapper used for client-server comunication..
	/// </summary>
	public class RecognitionResult
	{
		/// <summary>
		/// Recognized song.
		/// </summary>
		public Song Song { get; set; }
		/// <summary>
		/// Recognition accuracies of the songs
		/// </summary>
		public List<Tuple<uint, double>> SongAccuracies {get; set;}
	}

}
