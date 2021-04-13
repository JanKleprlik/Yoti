using BP.Server.Models;
using Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BP.Server.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class RecognitionController : ControllerBase
	{
		private readonly SongContext _context;
		private readonly SearchDataSingleton _searchDataInstance;
		private readonly ILogger _logger;
		public RecognitionController(SongContext context, SearchDataSingleton searchDataCollection, ILogger<RecognitionController> logger)
		{
			_context = context;
			_searchDataInstance = searchDataCollection;
			_logger = logger;
		}

		// GET: recognition/getsong/{id}
		#region Get song by Id
		[HttpGet("[action]/{id}")]
		public async Task<ActionResult<Song>> GetSong(int id)
		{
			var song = await _context.Songs.FindAsync(id);
			if (song == null)
			{
				return NotFound();
			}

			return song;
		}
		#endregion

		// GET: recognition/getsongs
		#region Get all songs
		[HttpGet("[action]")]
		public async Task<ActionResult<IEnumerable<Song>>> GetSongs()
		{
			return await _context.Songs.ToListAsync();
		}
		#endregion

		// POST: recognition/addnewsong
		#region Add new song
		[HttpPost("[action]")]
		public async Task<ActionResult<Song>> AddNewSong(Song song)
		{
			_context.Songs.Add(song);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetSong), new { id = song.Id }, song);
		}
		#endregion

		// DELETE: recognition/deletesong/{id}
		#region Delete song
		[HttpDelete("[action]/{id}")]
		public async Task<ActionResult<Song>> DeleteSong(int id)
		{
			var song = await _context.Songs.FindAsync(id);
			if (song == null)
			{
				return NotFound();
			}

			_context.Songs.Remove(song);
			await _context.SaveChangesAsync();

			return song;
		}
		#endregion

		// GET: recognition/test
		#region TEST GET
		[HttpGet("[action]")]
		public async Task<ActionResult<Dictionary<int, Dictionary<uint, List<ulong>>>>> TestGet()
		{
			return _searchDataInstance.SearchData;
		}
		#endregion

		// GET: recognition/test
		#region TEST POST
		[HttpPost("[action]")]
		public async Task<ActionResult<Dictionary<int, Dictionary<uint, List<ulong>>>>> TestPost()
		{

			if (_searchDataInstance.SearchData.ContainsKey(120) && _searchDataInstance.SearchData[120].ContainsKey(1)) //contains 120 BPM
			{
				//Add to song value
				_searchDataInstance.SearchData[120][1].Add((ulong)_searchDataInstance.SearchData[120][1].Count);
				DumpToDB();
			}
			else
			{
				//BPM -> songValues
				_searchDataInstance.SearchData.TryAdd(
					120, //BPM
					new Dictionary<uint, List<ulong>> { 
						{1, new List<ulong> { 0, 1, 2 } } 
					});
				DumpToDB();
			}

			return CreatedAtAction(nameof(TestGet), _searchDataInstance.SearchData);
		}
		#endregion



		public void DumpToDB()
		{

			//Delete old records
			_context.SearchDatas.RemoveRange(_context.SearchDatas);
			_context.SaveChanges();

			var searchDatas = new SearchData[_searchDataInstance.SearchData.Count];
			int index = 0;

			//Upload new records
			foreach (KeyValuePair<int, Dictionary<uint, List<ulong>>> entry in _searchDataInstance.SearchData)
			{
				searchDatas[index] = new SearchData
				{
					BPM = entry.Key,
					SongDataSerialized = JsonSerializer.Serialize(entry.Value),
				};

				index++;
			}
			_context.SearchDatas.AddRange(searchDatas);
			_context.SaveChanges();
		}
	}
}
