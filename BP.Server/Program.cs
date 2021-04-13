using BP.Server.Models;
using Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BP.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var host = CreateHostBuilder(args).Build();

			CreateDbIfNotExists(host);

			//PopulateSearchData(host);

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
					songContext.Database.EnsureCreated();
					DbInitializer.Initialize(songContext);
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
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});
	}

	public static class DbInitializer
	{
		public static void Initialize(SongContext context)
		{
			// Look for any students.
			if (context.Songs.Any())
			{
				return;   // DB has been seeded
			}

			var songs = new Song[]
			{
				new Song{Name="1", Author="1"},
				new Song{Name="2", Author="2"},
				new Song{Name="3", Author="3"},
				new Song{Name="4", Author="4"},
				new Song{Name="5", Author="5"},
				new Song{Name="6", Author="6"},
				new Song{Name="7", Author="7"},
				new Song{Name="8", Author="8"},
				new Song{Name="9", Author="9"},
				new Song{Name="10", Author="10"},

			};

			context.Songs.AddRange(songs);
			context.SaveChanges();
		}
	}
}
