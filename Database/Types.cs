using System;
using System.Collections.Generic;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace Database
{
	[Table("Songs")]
	public class Song
	{
		[PrimaryKey, AutoIncrement, Column("id")]
		public int ID { get; set; }

		[Column("name"), MaxLength(20)]
		public string Name { get; set; }

		[Column("author"), MaxLength(20)]
		public string Author { get; set; }
	}

	[Table("Fingerprints")]
	public class Fingerprint
	{
		[PrimaryKey, AutoIncrement, Column("id")]
		public int ID { get; set; }
		[Column("data")]
		public byte[] Data { get; set; }
	}
	[Serializable]
	public struct TimeFrequencyPoint
	{
		public uint Time { get; set; }
		public uint Frequency { get; set; }
	}
}
