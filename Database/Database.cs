using SQLite;
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

			//create tables
			if (!exists)
			{
				connection.CreateTable<Song>();
				//connection.CreateTable<Fingerprint>();
			}

			this.connection = connection;
			System.Diagnostics.Debug.WriteLine("[DEBUG] Leaving Database constructor");

		}
	}
}
