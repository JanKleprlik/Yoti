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

		// Mapping is only for Server part of the application. 
#if !__WASM__ && !__ANDROID__ && !NETFX_CORE
		[NotMapped]
#endif
		public uint Id
		{
			get => (uint)SongId;
			set => SongId = (int)value;
		}
		/// <summary>
		/// Id wrapper for SQLServer as it does not support unsigned types. <br></br>
		/// Conversion to uint at Id property is safe because all ids are postive and smaller than max value of int.
		/// </summary>
		public int SongId { get; set; }
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

}
