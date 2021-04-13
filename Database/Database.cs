using AudioProcessing;
using Microsoft.Extensions.Logging;
using Salar.Bois;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Database
{
	public sealed class DatabaseSQLite
	{
		/*/
		private SQLiteConnection connection;
		#region INITIALIZATION

		private static DatabaseSQLite instance = null;
		private static readonly object syncObject = new object();

		public static DatabaseSQLite Instance
		{
			get
			{
				if (instance == null)
				{
					lock (syncObject)
					{
						if (instance == null)
						{
							instance = new DatabaseSQLite();
						}
					}
				}
				return instance;
			}
		}

		private DatabaseSQLite()
		{
			this.Log().LogDebug("[DEBUG] In Database constructor");
			string databasePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "AudioDatabase.db");
			this.Log().LogDebug("[DEBUG] Path: " + databasePath);
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
			this.Log().LogDebug("[DEBUG] Leaving Database constructor");

		}	
		
		private void InitializeTables(SQLiteConnection connection)
		{

			this.Log().LogDebug("[DEBUG] Creating tables");

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

		public void DeleteSong(Song song)
		{
			int deleteSongID = song.ID;
			connection.Delete(song);

			Dictionary<uint, List<ulong>> oldSearchData = GetSearchData();
			Dictionary<uint, List<ulong>> newSearchData = new Dictionary<uint, List<ulong>>();


			foreach(KeyValuePair<uint, List<ulong>> entry in oldSearchData)
			{
				List<ulong> songDataList = new List<ulong>();

				foreach(ulong songData in entry.Value)
				{
					//do not add into new searchData if songID is same as deleteSongID
					//cast type to int is because ulong songID consists of:
					//32 bits of Absolute time of Anchor
					//32 bits of songID
					if (deleteSongID != (int)songData)
					{
						//add songData to new search Data
						songDataList.Add(songData);
					}
				}

				//if some songs live on entry.Key 
				//put them into newSearchData
				if (songDataList.Count != 0)
				{
					newSearchData.Add(entry.Key, songDataList);
				}
			}

			System.Diagnostics.Debug.WriteLine($"[DEBUG] oldSearchDataList.Count: {oldSearchData.Count}");
			System.Diagnostics.Debug.WriteLine($"[DEBUG] newearchDataList.Count: {newSearchData.Count}");

			UpdateSearchData(newSearchData);
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

				var searchData = searchDataQueryRes[0];
				var memStream = new MemoryStream();


				memStream.Write(searchData.serializedData, 0, searchData.serializedData.Length);
				memStream.Position = 0;




				var boisSerializer = new BoisSerializer();
				var deserialized = boisSerializer.Deserialize<Dictionary<uint, List<ulong>>>(memStream);
				return deserialized;
			}
			else
			{
				//database is empty
				this.Log().LogDebug("[DEBUG] Database is empty.");
				return new Dictionary<uint, List<ulong>>();
			}

		}


		public Song GetSongByID(uint ID)
		{
			var songs = connection.Table<Song>().Select(s => s).Where(s => s.ID == ID).ToList();
			if (songs.Count > 1 || songs.Count < 1)
			{
				throw new ArgumentException($"Found: {songs.Count} songs with ID {ID}. Exactly one should be found.");
			}
			else
			{
				return songs[0];
			}
		}

		#endregion

		#region DEBUG helpers

		public void PrintDatabase()
		{
			//Print songs
			var songs = connection.Query<Song>("SELECT * FROM Songs");
			this.Log().LogDebug($"Total songs:{songs.Count}");
			foreach(Song song in songs)
			{
				this.Log().LogDebug($"{song.ID}\t{song.Author}\t{song.Name}");
			}


			//print fingerprints
			var fps = connection.Query<Fingerprint>("SELECT * FROM Fingerprints");
			this.Log().LogDebug($"Total fps:{fps.Count}");
			foreach (Fingerprint fp in fps)
			{
				var memStream = new MemoryStream();
				var binFormatter = new BinaryFormatter();
				memStream.Write(fp.serializedData, 0, fp.serializedData.Length);
				memStream.Position = 0;

				List<TimeFrequencyPoint> fpData = binFormatter.Deserialize(memStream) as List<TimeFrequencyPoint>;
				this.Log().LogDebug($"{fp.ID}\t{fp.serializedData[0]} {fpData[1].Time} {fpData[2].Time} {fpData[3].Time} {fpData[4].Time}");
			}
		}
		#endregion
		/**/
	}
}
