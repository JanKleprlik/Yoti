using AudioProcessing;
using Salar.Bois;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Database
{
	public class Database
	{
		private SQLiteConnection connection;
		#region INITIALIZATION

		public Database()
		{
			System.Diagnostics.Debug.WriteLine("[DEBUG] In Database constructor");
			string databasePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "AudioDatabase.db");
			System.Diagnostics.Debug.WriteLine("[DEBUG] Path: " + databasePath);
			bool exists = File.Exists(databasePath);
			SQLiteConnection connection = new SQLiteConnection(databasePath);
			
			BoisSerializer.Initialize<Dictionary<uint, List<ulong>>>();

#if DEBUG
			//if (exists)
			//{
			//	connection.DropTable<Song>();
			//	connection.DropTable<Fingerprint>();
			//	connection.DropTable<SearchData>();
			//	exists = false;
			//}
#endif

			//create tables
			if (!exists)
			{
				InitializeTables(connection);
			}

			this.connection = connection;
			System.Diagnostics.Debug.WriteLine("[DEBUG] Leaving Database constructor");

		}
		private void InitializeTables(SQLiteConnection connection)
		{
			System.Diagnostics.Debug.WriteLine("[DEBUG] Creating tables");

			//Songs table
			connection.CreateTable<Song>();
			//Fingerprints table
			connection.CreateTable<Fingerprint>();
			//Data
			connection.CreateTable<SearchData>();

		}

		#endregion

		#region INSERTING API

		public uint AddSong(string name, string author)
		{
			Song song = new Song
			{
				Name = name,
				Author = author,
			};
			connection.Insert(song);
			return (uint)SQLite3.LastInsertRowid(connection.Handle);

		}

		public void AddFingerprint(List<TimeFrequencyPoint> fingerprint)
		{
			MemoryStream memStream = new MemoryStream();
			BinaryFormatter binFormatter = new BinaryFormatter();
			binFormatter.Serialize(memStream, fingerprint);

			Fingerprint fp = new Fingerprint
			{
				serializedData = memStream.ToArray()
			};
			connection.Insert(fp);
		}


		public void UpdateSearchData(Dictionary<uint, List<ulong>> newSearchData)
		{

			BoisSerializer serializer = new BoisSerializer();

			using (MemoryStream memStream = new MemoryStream())
			{
				serializer.Serialize(newSearchData, memStream);

				SearchData searchData = new SearchData
				{
					serializedData = memStream.ToArray()
				};

				//delete last version of searchData
				connection.DeleteAll<SearchData>();

				//create new 
				connection.Insert(searchData);

			}
		}

		#endregion

		#region QUERY API

		public List<Song> GetSongs()
		{
			return connection.Table<Song>().Select(s => s).ToList();
		}

		public Dictionary<uint, List<ulong>> GetSearchData()
		{
			var searchDataQueryRes = connection.Query<SearchData>("SELECT * FROM SearchData");
			if (searchDataQueryRes.Count != 0)
			{
				Stopwatch sw = new Stopwatch();
				sw.Start();



				System.Diagnostics.Debug.WriteLine("[DEBUG] Database is NOT empty.");
				var searchData = searchDataQueryRes[0];
				var memStream = new MemoryStream();

				sw.Stop();
				System.Diagnostics.Debug.WriteLine($"[DEBUG] common: {sw.ElapsedMilliseconds}");
				sw.Reset();
				sw.Start();

				memStream.Write(searchData.serializedData, 0, searchData.serializedData.Length);
				memStream.Position = 0;

				sw.Stop();
				System.Diagnostics.Debug.WriteLine($"[DEBUG] SERIALIZATION: {sw.ElapsedMilliseconds}");
				sw.Reset();
				sw.Start();


				var boisSerializer = new BoisSerializer();
				var deserialized = boisSerializer.Deserialize<Dictionary<uint, List<ulong>>>(memStream);

				sw.Stop();
				System.Diagnostics.Debug.WriteLine($"[DEBUG] DESERIALIZATION: {sw.ElapsedMilliseconds}");


				return deserialized;
			}
			else
			{
				//database is empty
				System.Diagnostics.Debug.WriteLine("[DEBUG] Database is empty.");
				return new Dictionary<uint, List<ulong>>();
			}

		}

		#endregion

		#region DEBUG helpers

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
				memStream.Write(fp.serializedData, 0, fp.serializedData.Length);
				memStream.Position = 0;

				List<TimeFrequencyPoint> fpData = binFormatter.Deserialize(memStream) as List<TimeFrequencyPoint>;
				System.Diagnostics.Debug.WriteLine($"{fp.ID}\t{fp.serializedData[0]} {fpData[1].Time} {fpData[2].Time} {fpData[3].Time} {fpData[4].Time}");
			}
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

		#endregion
	}
}
