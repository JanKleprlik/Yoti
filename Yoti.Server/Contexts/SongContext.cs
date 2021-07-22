using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using SharedTypes;
using System;
using System.Data;

namespace Yoti.Server.Models
{
	public class SongContext : DbContext
	{

		public SongContext(DbContextOptions<SongContext> options, IConfiguration configuration) : base(options)
		{
			// Initialize database connection
		}

		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			if (!options.IsConfigured)
				options.UseSqlite(@"Data Source=FingerPrintDatabase.db");
				//options.UseSqlServer(dbConnection.ToString());
		}

		public DbSet<Song> Songs { get; set; }
		public DbSet<DatabaseHash> DatabaseHashes { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Song>().ToTable("Songs");
			modelBuilder.Entity<DatabaseHash>().ToTable("DatabaseHash");
		}
	}
}
