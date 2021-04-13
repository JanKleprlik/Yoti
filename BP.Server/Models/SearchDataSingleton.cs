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


		public void SaveToDB()
		{
			using (var scope = scopeFactory.CreateScope())
			{
				var songContext = scope.ServiceProvider.GetRequiredService<SongContext>();
				//Delete old records
				songContext.SearchDatas.RemoveRange(songContext.SearchDatas);
				songContext.SaveChanges();

				var searchDatas = new SearchData[_searchData.Count];
				int index = 0;

				//Upload new records
				foreach (KeyValuePair<int, Dictionary<uint, List<ulong>>> entry in _searchData)
				{
					searchDatas[index] = new SearchData
					{
						BPM = entry.Key,
						SongDataSerialized = JsonSerializer.Serialize(entry.Value),
					};

					index++;
				}
				songContext.SearchDatas.AddRange(searchDatas);
				songContext.SaveChanges();
			}
		}

		public void SaveToDB(int BPM)
		{
			using (var scope = scopeFactory.CreateScope())
			{
				var songContext = scope.ServiceProvider.GetRequiredService<SongContext>();

				//delete existing data first
				SearchData dataToDelete = songContext.SearchDatas.Where(sData => sData.BPM == BPM).FirstOrDefault();
				if (dataToDelete != null)
				{
					//Remove SearchData with correct BPM
					songContext.SearchDatas.Remove(dataToDelete);
					songContext.SaveChanges();
				}
				SearchData searchDataToSave = new SearchData
				{
					BPM = BPM,
					SongDataSerialized = JsonSerializer.Serialize(_searchData[BPM]),
				};

				songContext.SearchDatas.Add(searchDataToSave);
				songContext.SaveChanges();
			}
		}
	}
}
