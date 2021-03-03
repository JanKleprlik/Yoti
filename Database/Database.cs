using SQLite;
using System;
using System.IO;

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
			System.Diagnostics.Debug.WriteLine("[DEBUG] Path: " + databasePath);
			SQLiteConnection connection = new SQLiteConnection(databasePath);
#if DEBUG
			//create new database on each debug run
			if (exists)
			{
				File.Delete(databasePath);
			}
#endif
			//create tables
			if (!exists)
			{
				InitializeTables(connection);
			}

			this.connection = connection;
#if DEBUG
			var songs = connection.GetTableInfo("Songs");
			var fingerpints = connection.GetTableInfo("Fingerprints");
			System.Diagnostics.Debug.WriteLine(songs.ToString());
			System.Diagnostics.Debug.WriteLine(fingerpints.ToString());
			foreach (var song in songs)
			{
				System.Diagnostics.Debug.WriteLine(song.ToString());
			}
			foreach (var fp in fingerpints)
			{	
				System.Diagnostics.Debug.WriteLine(fp.ToString());
			}
#endif
			InsertDummyData();
			System.Diagnostics.Debug.WriteLine("[DEBUG] Leaving Database constructor");

		}

		public void AddSong(string name, string author)
		{
			Song song = new Song
			{
				Name = name,
				Author = author
			};
			connection.Insert(song);

			//SQLiteCommand addSongCmd = new SQLiteCommand(connection);
			//addSongCmd.CommandText = "INSERT INTO Songs (name, author) VALUES(? ,?)";
			//addSongCmd.Bind("name", name);
			//addSongCmd.Bind("author", name);

			//try
			//{
			//	addSongCmd.ExecuteNonQuery();
			//}
			//catch (Exception e)
			//{
			//	System.Diagnostics.Debug.WriteLine($"[DEBUG] Exception occured on add new song into database: {e.Message}");
			//}
		}

		public void PrintSongs()
		{
			var songs = connection.Query<Song>("SELECT * FROM Songs");

			foreach(Song song in songs)
			{
				System.Diagnostics.Debug.WriteLine($"{song.ID}\t{song.Author}\t{song.Name}");
			}
		}

		private void InitializeTables(SQLiteConnection connection)
		{
			System.Diagnostics.Debug.WriteLine("[DEBUG] Creating tables");

			//Songs table
			connection.CreateTable<Song>();
			//Fingerprints table
			connection.CreateTable<Fingerprint>();

			//SQLiteCommand addFingerprintTableCmd = new SQLiteCommand(connection);
			//addFingerprintTableCmd.CommandText = "CREATE TABLE Fingerprints(id INTEGER PRIMARY KEY AUTOINCREMENT, timeFrequencyPoints BLOB)";
			//addFingerprintTableCmd.ExecuteNonQuery();

		}

		private void InsertDummyData()
		{
			AddSong("A", "A");
			AddSong("B", "B");
			AddSong("C", "C");
			AddSong("D", "D");
			AddSong("E", "E");
		}
	}
}
