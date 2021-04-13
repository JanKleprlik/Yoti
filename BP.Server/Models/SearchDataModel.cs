using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BP.Server.Models
{
	public class SearchData
	{
		public int Id { get; set; }
		public int BPM { get; set; }
		public string SongDataSerialized { get; set; }
	}
}
