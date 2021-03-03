using AudioProcessing;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Database
{
	public class Database
	{
		private SQLiteConnection connection;
		public Database()
		{
			System.Diagnostics.Debug.WriteLine("[DEBUG] In Database constructor");
			string databasePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "AudioDatabase.db");
			bool exists = File.Exists(databasePath);
			SQLiteConnection connection = new SQLiteConnection(databasePath);
#if DEBUG
			if (exists)
			{
				connection.DropTable<Song>();
				connection.DropTable<Fingerprint>();
				exists = false;
			}
#endif   
			System.Diagnostics.Debug.WriteLine("[DEBUG] Path: " + databasePath);

			//create tables
			if (!exists)
			{
				InitializeTables(connection);
			}

			this.connection = connection;
			System.Diagnostics.Debug.WriteLine("[DEBUG] Leaving Database constructor");

		}

		public void AddSong(string name, string author)
		{
			Song song = new Song
			{
				Name = name,
				Author = author,
			};
			connection.Insert(song);
		}

		public void AddFingerprint(List<TimeFrequencyPoint> fingerprint)
		{
			MemoryStream memStream = new MemoryStream();
			BinaryFormatter binFormatter = new BinaryFormatter();
			binFormatter.Serialize(memStream, fingerprint);

			Fingerprint fp = new Fingerprint
			{
				Data = memStream.ToArray()
			};
			connection.Insert(fp);
		}

		public List<Song> GetSongs()
		{
			return connection.Table<Song>().Select(s => s).ToList();
		}

		public void PrintDatabase()
		{
			//Print songs
			var songs = connection.Query<Song>("SELECT * FROM Songs");
			System.Diagnostics.Debug.WriteLine($"Total songs:{songs.Count}");
			foreach(Song song in songs)
			{
				System.Diagnostics.Debug.WriteLine($"{song.ID}\t{song.Author}\t{song.Name}");
			}


			//print fingerprints
			var fps = connection.Query<Fingerprint>("SELECT * FROM Fingerprints");
			System.Diagnostics.Debug.WriteLine($"Total fps:{fps.Count}");
			foreach (Fingerprint fp in fps)
			{
				var memStream = new MemoryStream();
				var binFormatter = new BinaryFormatter();
				memStream.Write(fp.Data, 0, fp.Data.Length);
				memStream.Position = 0;

				List<TimeFrequencyPoint> fpData = binFormatter.Deserialize(memStream) as List<TimeFrequencyPoint>;
				System.Diagnostics.Debug.WriteLine($"{fp.ID}\t{fp.Data[0]} {fpData[1].Time} {fpData[2].Time} {fpData[3].Time} {fpData[4].Time}");
				System.Diagnostics.Debug.WriteLine($"{fp.ID}\t{fp.Data[0]} {fpData[1].Frequency} {fpData[2].Frequency} {fpData[3].Frequency} {fpData[4].Frequency}");
			}
		}

		private void InitializeTables(SQLiteConnection connection)
		{
			System.Diagnostics.Debug.WriteLine("[DEBUG] Creating tables");

			//Songs table
			connection.CreateTable<Song>();
			//Fingerprints table
			connection.CreateTable<Fingerprint>();
		}

		public void InsertDummyData()
		{
			System.Diagnostics.Debug.WriteLine($"[DEBUG] Inserting dummy data");
			TimeFrequencyPoint A = new TimeFrequencyPoint { Time = 0, Frequency = 0 };
			TimeFrequencyPoint B = new TimeFrequencyPoint { Time = 1, Frequency = 1 };
			TimeFrequencyPoint C = new TimeFrequencyPoint { Time = 2, Frequency = 2 };
			TimeFrequencyPoint D = new TimeFrequencyPoint { Time = 3, Frequency = 3 };
			TimeFrequencyPoint E = new TimeFrequencyPoint { Time = 4, Frequency = 4 };

			List<TimeFrequencyPoint> l = new List<TimeFrequencyPoint>(
				new TimeFrequencyPoint[]{ A, B, D, C, E });
			
			//ADD 5 fingerpints
			for (int i = 0; i < 5; i++)
			{
				AddFingerprint(l);
			}

			//Add 5 sogns
			AddSong("A", "A");
			AddSong("B", "B");
			AddSong("C", "C");
			AddSong("D", "D");
			AddSong("E", "E");
			System.Diagnostics.Debug.WriteLine($"[DEBUG] Done inserting dummy data");
		}
	}
}
