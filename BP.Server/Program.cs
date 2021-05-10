using BP.Server.Models;
using Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BP.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var host = CreateHostBuilder(args).Build();

			CreateDbIfNotExists(host);

			host.Run();
		}

		private static void CreateDbIfNotExists(IHost host)
		{
			using (var scope = host.Services.CreateScope())
			{
				var services = scope.ServiceProvider;
				try
				{
					var songContext = services.GetRequiredService<SongContext>();
					//Delete database if exists
					//songContext.Database.EnsureDeleted();
					//Create database
					songContext.Database.EnsureCreated();
					//DbInitializer.Initialize(songContext);
				}
				catch (Exception ex)
				{
					var logger = services.GetRequiredService<ILogger<Program>>();
					logger.LogError(ex, "An error occurred creating the DB.");
				}
			}
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureLogging(logging =>
				{
					logging.ClearProviders();
					logging.AddConsole();
				})
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});
	}

	public static class DbInitializer
	{
		public static void Initialize(SongContext context)
		{

			context.Songs.RemoveRange(context.Songs);
			context.SearchDatas.RemoveRange(context.SearchDatas);

			//WavFormat wavFormat = new WavFormat(outputArray);


			#region COMMENTED
			// Look for any students.
			if (context.Songs.Any())
			{
				return;   // DB has been seeded
			}

			var songs = new Song[]
			{
				new Song{name="1", author="1", bpm = 0},
				new Song{name="2", author="2", bpm = 0},
				new Song{name="3", author="3", bpm = 0},
				new Song{name="4", author="4", bpm = 0},
				new Song{name="5", author="5", bpm = 0},
				new Song{name="6", author="6", bpm = 0},
				new Song{name="7", author="7", bpm = 0},
				new Song{name="8", author="8", bpm = 0},
				new Song{name="9", author="9", bpm = 0},
				new Song{name="10", author="10", bpm = 0},

			};

			context.Songs.AddRange(songs);
			context.SaveChanges();

			//var searchData = new SearchData
			//{
			//	BPM = 0,
			//	SongDataSerialized = JsonSerializer.Serialize(new Dictionary<uint, List<ulong>>()),
			//};

			//context.SearchDatas.Add(searchData);
			//context.SaveChanges();
			#endregion
		}
	}
}
