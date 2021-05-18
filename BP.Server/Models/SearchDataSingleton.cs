using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BP.Server.Models
{
	public class SearchDataSingleton
	{
		/// <summary>
		/// Search data in memory.<br></br>
		/// [BMP, [address, (songValue)]] <br></br>
		/// </summary>
		private readonly Dictionary<int ,Dictionary<uint, List<ulong>>> _searchData;

		/// <summary>
		/// Scope facotry for creating database scopes.
		/// </summary>
		private readonly IServiceScopeFactory scopeFactory;

		/// <summary>
		/// Base JSON serializer options
		/// </summary>
		private JsonSerializerOptions serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="scopeFactory"></param>
		public SearchDataSingleton(IServiceScopeFactory scopeFactory)
		{
			this.scopeFactory = scopeFactory;

			// Populate scopeFactory with data
			using (var scope = scopeFactory.CreateScope())
			{
				var songContext = scope.ServiceProvider.GetRequiredService<SongContext>();

				// Upload searchData form database to memory
				List<SearchData> searchDatas = songContext.SearchDatas.ToList();
				_searchData = new Dictionary<int, Dictionary<uint, List<ulong>>>();
				foreach(SearchData searchData in searchDatas)
				{
					var songData = JsonSerializer.Deserialize<Dictionary<uint, List<ulong>>>(searchData.SongDataSerialized, serializerOptions);
					_searchData.Add(searchData.BPM, songData);
				}				
			}
		}

		public Dictionary<int, Dictionary<uint, List<ulong>>> SearchData => _searchData;


		/// <summary>
		/// Save search data with given BPM from memory to database.
		/// </summary>
		/// <param name="BPM">BPM section to save to database</param>
		public void SaveToDB(int BPM)
		{
			using (var scope = scopeFactory.CreateScope())
			{
				var songContext = scope.ServiceProvider.GetRequiredService<SongContext>();

				// Delete existing data first
				SearchData dataToDelete = songContext.SearchDatas.Where(sData => sData.BPM == BPM).FirstOrDefault();
				if (dataToDelete != null)
				{
					// Remove SearchData with correct BPM
					songContext.SearchDatas.Remove(dataToDelete);
					songContext.SaveChanges();
				}
				SearchData searchDataToSave = new SearchData
				{
					BPM = BPM,
					SongDataSerialized = JsonSerializer.Serialize(_searchData[BPM], serializerOptions),
				};

				songContext.SearchDatas.Add(searchDataToSave);
				songContext.SaveChanges();
			}
		}
	}
}
