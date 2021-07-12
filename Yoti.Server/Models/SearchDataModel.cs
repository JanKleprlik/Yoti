using System.ComponentModel.DataAnnotations.Schema;

namespace Yoti.Server.Models
{
	public class DatabaseHash
	{
		/// <summary>
		/// Id of the hash
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Hash value 
		/// </summary>
		public int HashDB { get; set; }

		/// <summary>
		/// Hash value wrapper used to convert unsigned to signed
		/// as SQLServer does not support unsigned types.
		/// </summary>
		[NotMapped]
		public uint Hash
		{
			get
			{
				return (uint)HashDB;
			}
			set
			{
				HashDB = (int)value;
			}
		}

		/// <summary>
		/// BPM of the associated song
		/// </summary>
		public int BPM { get; set; }

		/// <summary>
		/// Song value associated with the hash. <br></br>
		/// 32 bits - Absolute Anchor Time
		/// 32 bits - Song Id
		/// </summary>
		public long SongValueDB { get; set; }

		/// <summary>
		/// SongValue wrapper converting unsigned to signed
		/// as SQLServer does not support unsigned types.
		/// </summary>
		[NotMapped]
		public ulong SongValue
		{
			get
			{
				return (ulong)SongValueDB;
			}

			set
			{
				SongValueDB = (long)value;
			}
		}

	}
}
