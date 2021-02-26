using System;
using System.Collections.Generic;
using System.Text;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace AudioProcessing.Database
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

	[Serializable]
	public class Fingerprint
	{
		[PrimaryKey, AutoIncrement, Column("id")]
		public int ID { get; set; }

		[TextBlob("TimeFrequencyPointsBlobbed")]
		public List<TimeFrequencyPoint> TimeFrequencyPoints { get; set; }
		public string TimeFrequencyPointsBlobbed { get; set; }
	}
	
	public struct TimeFrequencyPoint
	{
		public uint Time { get; set; }
		public uint Frequency { get; set; }
	}
}
