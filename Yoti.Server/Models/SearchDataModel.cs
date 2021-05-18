using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Yoti.Server.Models
{
	public class SearchData
	{
		/// <summary>
		/// Id of search data part.
		/// </summary>
		public int Id { get; set; }
		/// <summary>
		/// BPM of songs in the search data part.
		/// </summary>
		public int BPM { get; set; }
		/// <summary>
		/// Serialized search data.
		/// </summary>
		public string SongDataSerialized { get; set; }
	}
}
