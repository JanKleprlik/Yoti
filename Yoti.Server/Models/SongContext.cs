using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharedTypes;

namespace Yoti.Server.Models
{
	public class SongContext : DbContext
	{
		public SongContext(DbContextOptions<SongContext> options) : base(options)
		{
		}
		protected override void OnConfiguring(DbContextOptionsBuilder options)
			=> options.UseSqlServer("Data Source=tcp:yotisongdatabaseserver.database.windows.net,1433;Initial Catalog=YotiSongDatabase;User Id=YotiAdmin@yotisongdatabaseserver;Password=AdminPassword.");
			//=> options.UseSqlite(@"Data Source=Yoti.db");

		public DbSet<Song> Songs { get; set; }
		public DbSet<SearchData> SearchDatas { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Song>().ToTable("Songs");
			modelBuilder.Entity<SearchData>().ToTable("SearchData");
		}
	}
}
