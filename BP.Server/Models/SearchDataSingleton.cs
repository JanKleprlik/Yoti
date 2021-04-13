using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salar.Bois;
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
		//[BMP, [address, (songValue)]]
		private readonly Dictionary<int ,Dictionary<uint, List<ulong>>> _searchData;
		private readonly IServiceScopeFactory scopeFactory;

		public SearchDataSingleton(IServiceScopeFactory scopeFactory)
		{
			this.scopeFactory = scopeFactory;

			//populate scopeFactory with data
			using (var scope = scopeFactory.CreateScope())
			{
				var songContext = scope.ServiceProvider.GetRequiredService<SongContext>();

				//upload searchData form database to memory
				List<SearchData> searchDatas = songContext.SearchDatas.ToList();
				_searchData = new Dictionary<int, Dictionary<uint, List<ulong>>>();
				foreach(SearchData searchData in searchDatas)
				{
					var songData = JsonSerializer.Deserialize<Dictionary<uint, List<ulong>>>(searchData.SongDataSerialized);
					_searchData.Add(searchData.BPM, songData);
				}				
			}
		}

		public Dictionary<int, Dictionary<uint, List<ulong>>> SearchData => _searchData;

	}
}
