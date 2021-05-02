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
using AudioRecognitionLibrary.AudioFormats;
using AudioRecognitionLibrary;
using AudioRecognitionLibrary.Recognizer;
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
			Song newSong = new Song { author = songToUpload.author, name = songToUpload.name, lyrics = songToUpload.lyrics, bpm = songToUpload.bpm};

			_logger.LogInformation("Getting correct searchdata");
			Dictionary<uint, List<ulong>> searchData = GetSearchDataByBPM(songToUpload.bpm);
			
			_context.Songs.Add(newSong);
			_context.SaveChanges();
			uint maxId = _context.Songs.Max(song => song.id);

			_logger.LogInformation("Addding TFPs to database");
			_recognizer.AddTFPToDataStructure(songToUpload.tfps, maxId, searchData);

			//Update data in database
			_searchDataInstance.SaveToDB(songToUpload.bpm);
			_context.SaveChanges();

			return CreatedAtAction(nameof(GetSong), new { id = newSong.id }, newSong);
		}
		#endregion

		// POST:recognition/RecognizeSong
		#region Recognize song
		[HttpPost("[action]")]
		public async Task<ActionResult<RecognitionResult>> RecognizeSong(SongWavFormat songToUpload)
		{
			var stringWriter = new StringWriter();

			_logger.LogDebug("Getting correct searchdata");
			Dictionary<uint, List<ulong>> searchData = GetSearchDataByBPM(songToUpload.bpm);

			_logger.LogDebug("Recognizing song");
			double maxProbability = 0;
			uint? songId = _recognizer.RecognizeSong(songToUpload.tfps, searchData, out maxProbability, stringWriter);

			if (songId == null)
			{
				_logger.LogDebug("Song not found by BPM");
				foreach(KeyValuePair<int, Dictionary<uint, List<ulong>>> entry in _searchDataInstance.SearchData)
				{
					if (entry.Key == songToUpload.bpm)
						continue; //skip searchdata that was already searched through


					searchData = GetSearchDataByBPM(entry.Key);
					uint? potentialSongId = _recognizer.RecognizeSong(songToUpload.tfps, searchData, out double probability, stringWriter);

					//if result is not null and probabilty is higher than current max
					//remember the id and new max probability
					if (potentialSongId != null && probability > maxProbability)
					{
						_logger.LogDebug($"New potential song id found: {potentialSongId} with proba: {probability} in BPM: {entry.Key}");
						songId = potentialSongId;
						maxProbability = probability;
					}
				}
			}
			//write result probability
			if (songId != null)
				await stringWriter.WriteLineAsync($"Recognized song with ID: {songId} is a {Math.Min(100d, maxProbability):##.#}% match.");

			stringWriter.Close();

			if (songId == null)
			{
				return new RecognitionResult
				{
					song = null,
					detailinfo = stringWriter.ToString()
				};
			}
			else
			{
				return new RecognitionResult
				{
					song = await _context.Songs.FindAsync((uint)songId),
					detailinfo = stringWriter.ToString()
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
		public async Task<ActionResult<List<Song>>> GetSongs()
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
			uint deleteSongId= song.id;

			Dictionary<uint, List<ulong>> oldSearchData = GetSearchDataByBPM(song.bpm);
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

			_searchDataInstance.SearchData[song.bpm] = newSearchData;
			_searchDataInstance.SaveToDB(song.bpm);

		}

		#endregion
	}
}
