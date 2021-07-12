using Yoti.Server.Models;
using SharedTypes;
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
using Azure.Identity;

namespace Yoti.Server
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
					
					// Delete database if exists
					// songContext.Database.EnsureDeleted();
					
					// Create database
					songContext.Database.EnsureCreated();
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
				.ConfigureAppConfiguration((context, config) =>
				{
					var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("YotiValutUri"));
					config.AddAzureKeyVault(
					keyVaultEndpoint,
					new DefaultAzureCredential());
				})
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
}
