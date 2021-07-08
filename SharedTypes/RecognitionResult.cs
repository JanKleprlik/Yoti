using System;
using System.Collections.Generic;
using System.Text;

namespace SharedTypes
{
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
		public List<Tuple<uint, double>> SongAccuracies { get; set; }
	}

}
