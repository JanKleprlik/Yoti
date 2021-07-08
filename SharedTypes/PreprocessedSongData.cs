using System;
using System.Collections.Generic;
using System.Text;

namespace SharedTypes
{
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
		public int BPM { get; set; }
	}

}
