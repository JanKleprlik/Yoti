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
using AudioProcessing.AudioFormats;
using AudioProcessing;
using AudioProcessing.Recognizer;
using System.IO;

namespace BP.Server.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class RecognitionController : ControllerBase
	{
		private readonly SongContext _context;
		private readonly SearchDataSingleton _searchDataInstance;
		private readonly ILogger _logger;
		private readonly AudioRecognizer _recognizer = new AudioRecognizer();
		public RecognitionController(SongContext context, SearchDataSingleton searchDataCollection, ILogger<RecognitionController> logger)
		{
			_context = context;
			_searchDataInstance = searchDataCollection;
			_logger = logger;
		}

		// POST:recognition/AddNewSong
		#region Upload new song
		[HttpPost("[action]")]
		public async Task<ActionResult<Song>> AddNewSong(SongWavFormat songToUpload)
		{
			Song newSong = new Song { Author = songToUpload.Author, Name = songToUpload.Name };

			_logger.LogInformation("Getting correct searchdata");
			Dictionary<uint, List<ulong>> searchData = GetSearchDataByBPM(songToUpload.BPM);
			
			_context.Songs.Add(newSong);
			_context.SaveChanges();
			uint maxId = _context.Songs.Max(song => song.Id);

			_logger.LogInformation("Addding TFPs to database");
			_recognizer.AddTFPToDataStructure(songToUpload.TFPs, maxId, searchData);

			//Update data in database
			_searchDataInstance.SaveToDB(songToUpload.BPM);
			_context.SaveChanges();

			return CreatedAtAction(nameof(GetSong), new { id = newSong.Id }, newSong);
		}
		#endregion

		// POST:recognition/RecognizeSong
		#region Recognize song
		[HttpPost("[action]")]
		public async Task<ActionResult<RecognitionResult>> RecognizeSong(SongWavFormat songToUpload)
		{
			var stringWriter = new StringWriter();

			_logger.LogDebug("Getting correct searchdata");
			Dictionary<uint, List<ulong>> searchData = GetSearchDataByBPM(songToUpload.BPM);

			_logger.LogDebug("Recognizing song");
			uint? song_id = _recognizer.RecognizeSong(songToUpload.TFPs, searchData, stringWriter);

			if (song_id == null)
			{
				return NoContent();
			}
			else
			{
				return new RecognitionResult
				{
					Song = await _context.Songs.FindAsync((uint)song_id),
					DetailInfo = stringWriter.ToString()
				};

			}
		}
		#endregion

		// DELETE:recognition/DeleteSong
		#region Delete test
		[HttpDelete("[action]")]
		public async Task<ActionResult<Song>> DeleteSong(Song song)
		{
			
			if (! await _context.Songs.ContainsAsync(song))
			{
				return NotFound();
			}

			_context.Songs.Remove(song);
			DeleteSongFromSearchData(song);
			_context.SaveChanges();

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

		// GET: recognition/getsong/{id}
		#region Get song by Id
		[HttpGet("[action]/{id}")]
		public async Task<ActionResult<Song>> GetSong(uint id)
		{
			var song = await _context.Songs.FindAsync(id);
			if (song == null)
			{
				return NotFound();
			}

			return song;
		}
		#endregion

		#region Private helpers
		private Dictionary<uint, List<ulong>> GetSearchDataByBPM(int BPM)
		{
			if (!_searchDataInstance.SearchData.ContainsKey(BPM)) //doesnt contains the BPM yet -> add it
			{
				_searchDataInstance.SearchData.TryAdd(
					BPM, //BPM
					new Dictionary<uint, List<ulong>>()); //empty SongData
			}
			return _searchDataInstance.SearchData[BPM];
		}

		private void SetSearchDataByBPM(int BPM, Dictionary<uint, List<ulong>> searchData)
		{
			//doesnt contains the BPM yet -> add it
			if (!_searchDataInstance.SearchData.ContainsKey(BPM)) 
			{
				_searchDataInstance.SearchData.TryAdd(
					BPM, //BPM
					searchData); //empty SongData
			}
			//replace current search data on the BPM
			else
			{
				_searchDataInstance.SearchData[BPM] = searchData;
			}
			
		}

		private void DeleteSongFromSearchData(Song song)
		{
			uint deleteSongId= song.Id;

			Dictionary<uint, List<ulong>> oldSearchData = GetSearchDataByBPM(song.BPM);
			Dictionary<uint, List<ulong>> newSearchData = new Dictionary<uint, List<ulong>>();


			foreach (KeyValuePair<uint, List<ulong>> entry in oldSearchData)
			{
				List<ulong> songDataList = new List<ulong>();

				foreach (ulong songData in entry.Value)
				{
					//do not add into new searchData if songID is same as deleteSongID
					//cast type to int is because ulong songID consists of:
					//32 bits of Absolute time of Anchor
					//32 bits of songID
					if (deleteSongId != (uint)songData)
					{
						//add songData to new search Data
						songDataList.Add(songData);
					}
				}

				//if some songs live on entry.Key 
				//put them into newSearchData
				if (songDataList.Count != 0)
				{
					newSearchData.Add(entry.Key, songDataList);
				}
			}

			_searchDataInstance.SearchData[song.BPM] = newSearchData;
			_searchDataInstance.SaveToDB(song.BPM);

		}

		#endregion

		/*/
		#region TEST API -- OBSOLETE
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
			}
			else
			{
				//BPM -> songValues
				_searchDataInstance.SearchData.TryAdd(
					120, //BPM
					new Dictionary<uint, List<ulong>> { 
						{1, new List<ulong> { 0, 1, 2 } } 
					});
			}
			_searchDataInstance.SaveToDB(120);

			return CreatedAtAction(nameof(TestGet), _searchDataInstance.SearchData);
		}
		#endregion

		// POST:recognition/uploadtest
		#region Upload new song test
		[HttpPost("[action]")]
		public async Task<ActionResult<Song>> uploadtest()
		{
			_logger.LogInformation("reading file");
			byte[] outputArray = System.IO.File.ReadAllBytes("./Home.wav");

			WavFormat waf = new WavFormat(outputArray);

			_logger.LogInformation("Getting time Frequencies");
			List<TimeFrequencyPoint> tfps = _recognizer.GetTimeFrequencyPoints(waf);

			SongWavFormat swf = new SongWavFormat
			{
				Author = "Martin Garrix",
				Name = "Home",
				BPM = 0,
				TFPs = tfps
			};
			return await AddNewSong(swf);
		}
		#endregion

		// POST:recognition/recognizetest
		#region Recognize song test
		[HttpPost("[action]")]
		public async Task<ActionResult<RecognitionResult>> recognizetest()
		{
			byte[] outputArray = System.IO.File.ReadAllBytes("./recording.wav");
			WavFormat waf = new WavFormat(outputArray);

			List<TimeFrequencyPoint> tfps = _recognizer.GetTimeFrequencyPoints(waf);

			SongWavFormat swf = new SongWavFormat
			{
				Author = "NONE",
				Name = "NONE",
				BPM = 0,
				TFPs = tfps
			};

			return await RecognizeSong(swf);
		}
		#endregion

		#endregion
		/**/
	}
}
