using AudioRecognitionLibrary;
using AudioRecognitionLibrary.AudioFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if !__WASM__ && !__ANDROID__
using System.ComponentModel.DataAnnotations.Schema;
#endif

namespace SharedTypes
{
	/// <summary>
	/// Class keeping information about a song.
	/// </summary>
	public class Song
	{
		/// <summary>
		/// unsigned verison of Id of the song used throughout the application because
		/// the recognition algorithm uses unsigned int. <br></br>
		/// Conversion from int at SongId property is safe because all ids are postive and smaller than max value of int.
		/// </summary>
		// Mapping is only for Server part of the application - database
		#if !__WASM__ && !__ANDROID__ && !NETFX_CORE
		[NotMapped]
		#endif
		public uint Id { 
			get => (uint)SongId;
			set => SongId = (int)value;
		}
		/// <summary>
		/// Id wrapper for SQLServer as it does not support unsigned types. <br></br>
		/// Conversion to uint at Id property is safe because all ids are postive and smaller than max value of int.
		/// </summary>
		public int SongId {	get; set; }
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
