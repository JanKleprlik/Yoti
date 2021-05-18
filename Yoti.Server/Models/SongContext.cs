using Microsoft.EntityFrameworkCore;
using SharedTypes;

namespace Yoti.Server.Models
{
	public class SongContext : DbContext
	{
		public SongContext(DbContextOptions<SongContext> options) : base(options)
		{
		}
		protected override void OnConfiguring(DbContextOptionsBuilder options)
			=> options.UseSqlite(@"Data Source=Yoti.db");

		public DbSet<Song> Songs { get; set; }
		public DbSet<SearchData> SearchDatas { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Song>().ToTable("Songs");
			modelBuilder.Entity<SearchData>().ToTable("SearchData");
		}
	}
}
