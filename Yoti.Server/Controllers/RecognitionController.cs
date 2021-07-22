using Yoti.Server.Models;
using SharedTypes;
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

namespace Yoti.Server.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class RecognitionController : ControllerBase
	{
		public RecognitionController(SongContext context, ILogger<RecognitionController> logger)
		{
			_context = context;
			_logger = logger;
		}

		#region private fields

		/// <summary>
		/// Database context.
		/// </summary>
		private readonly SongContext _context;

		/// <summary>
		/// Logger.
		/// </summary>
		private readonly ILogger _logger;

		/// <summary>
		/// Recognizer algorithm .
		/// </summary>
		private readonly AudioRecognizer _recognizer = new AudioRecognizer();
		#endregion


		/// <summary>
		/// POST: recognition/AddNewSong
		/// </summary>
		/// <param name="songToUpload">Preprocessed song to be added into the database.</param>
		/// <returns>Uploaded song.</returns>
		#region Upload new song
		[HttpPost("[action]")]
		public ActionResult<Song> AddNewSong(PreprocessedSongData songToUpload)
		{
			Song newSong = new Song { Author = songToUpload.Author, Name = songToUpload.Name, Lyrics = songToUpload.Lyrics, BPM = songToUpload.BPM };
	
			// Creaty empty SearchData data structure that will be saved into the database
			Dictionary<uint, List<ulong>> searchData = new Dictionary<uint, List<ulong>>();

			// Save song metadata
			_context.Songs.Add(newSong);
			_context.SaveChanges();

			// Get Id manually so we can use it at fingerprint creation
			int maxId = _context.Songs.Max(song => song.SongId);

			_logger.LogInformation("Addding TFPs to database");
			// Conversion of maxId from int to uint is safe because SongId is always positive and smaller than max value of int
			_recognizer.AddFingerprintToDatabase(songToUpload.Fingerprint, Convert.ToUInt32(maxId), searchData);

			// Update data in database
			AddSearchDataToDB(searchData, newSong.BPM);
			_context.SaveChanges();

			// Return added song
			return CreatedAtAction(nameof(GetSong), new { id =newSong.Id }, newSong);
		}
		#endregion

		/// <summary>
		/// POST: recognition/RecognizeSong
		/// </summary>
		/// <param name="songToUpload">Preprocessed audio to be recognized.</param>
		/// <returns>Wrapped recognized song with info about recognition process.</returns>
		#region Recognize song
		[HttpPost("[action]")]
		public async Task<ActionResult<RecognitionResult>> RecognizeSong(PreprocessedSongData songToUpload)
		{
			_logger.LogDebug("Getting correct searchdata");
			Dictionary<uint, List<ulong>> searchData = GetSearchDataByBPM(songToUpload.BPM);

			// recognize song
			_logger.LogDebug("Recognizing song");
			var recognitionResult = _recognizer.RecognizeSong(songToUpload.Fingerprint, searchData, songToUpload.TFPCount);

			// parse recognition result
			uint? songId = recognitionResult.Item1;
			List<Tuple<uint, double>> songAccuracies = recognitionResult.Item2;

			//find accuracy of recognition result song
			double maxAccuracy = GetSongAccuracy(songId, songAccuracies);

			// song was not found in chosen BPM sector
			if (songId == null)
			{
				_logger.LogDebug("Song not found by BPM");
				// Iterate through all other BPM (from 60 to 180) and try to find
				// identify the song
				for (int potentialBPM = 60; potentialBPM < 180; potentialBPM += 5)
				{
					//dont search in BPM that was already searched through
					if (potentialBPM == songToUpload.BPM)
						continue;

					//get SearchData by bpm
					searchData = GetSearchDataByBPM(potentialBPM);
					var potentialRecognitionResult = _recognizer.RecognizeSong(songToUpload.Fingerprint, searchData, songToUpload.TFPCount);

					uint? potentialSongId = potentialRecognitionResult.Item1;
					double accuracy = GetSongAccuracy(potentialSongId, potentialRecognitionResult.Item2);

					// If result is not null and accuracy is higher than current max
					// remember the id and new max accuracy and accuracies of other songs
					if (potentialSongId != null && accuracy > maxAccuracy)
					{
						_logger.LogDebug($"New potential song id found: {potentialSongId} with proba: {accuracy} in BPM: {potentialBPM}");
						songId = potentialSongId; //remember songId
						maxAccuracy = accuracy; //remember max accuracy
						songAccuracies = potentialRecognitionResult.Item2; //remember accuracies of other song in BPM batch
					}

				}
			}

			if (songId == null)
			{
				return new RecognitionResult
				{
					Song = null,
					SongAccuracies = songAccuracies,
				};
			}
			else
			{
				return new RecognitionResult
				{
					// Conversion of songId from uint to int is safe, because ids are smaller than max value of int
					// also songId is checked to not be null
					Song = await _context.Songs.FindAsync((int)songId),
					SongAccuracies = songAccuracies,
				};

			}
		}
		#endregion


		/// <summary>
		/// DELETE: recognition/DeleteSong
		/// </summary>
		/// <param name="song">Song to delete.</param>
		/// <returns>Deleted song.</returns>
		#region Delete song
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


		/// <summary>
		/// GET: recognition/getsongs
		/// </summary>
		/// <returns>List of songs in the database.</returns>
		#region Get all songs
		[HttpGet("[action]")]
		public async Task<ActionResult<List<Song>>> GetSongs()
		{
			return await _context.Songs.ToListAsync();
		}
		#endregion


		/// <summary>
		/// GET: recognition/getsong/{id}
		/// </summary>
		/// <param name="id">Id of the song to return.</param>
		/// <returns>Song with corresponding Id.</returns>
		#region Get song by Id
		[HttpGet("[action]/{id}")]
		public async Task<ActionResult<Song>> GetSong(uint id)
		{
			// Conversion of id from uint to int is safe because
			// all ids are smaller than max value of int
			var song = await _context.Songs.FindAsync((int)id);
			if (song == null)
			{
				return NotFound();
			}

			return song;
		}
		#endregion


		/// <summary>
		/// DELETE: recognition/deletesongbyid/{id}
		/// </summary>
		/// <param name="id">Id of the song to be deleted.</param>
		/// <returns>Deleted song.</returns>
		#region Delete song by Id
		[HttpDelete("[action]/{id}")]
		public async Task<ActionResult<Song>> DeleteSongById(uint id)
		{
			// Conversion of id from uint to int is safe because
			// all ids are smaller than max value of int
			var song = await _context.Songs.FindAsync((int)id);
			if (song == null)
			{
				return NotFound();
			}

			if (!await _context.Songs.ContainsAsync(song))
			{
				return NotFound();
			}

			_context.Songs.Remove(song);
			DeleteSongFromSearchData(song);
			_context.SaveChanges();

			return song;
		}
		#endregion

		#region Private helpers

		/// <summary>
		/// Obtain search data from database by BPM.
		/// </summary>
		/// <param name="BPM">BPM determining search data to be returned.</param>
		/// <returns>Search data of songs with given BPM.</returns>
		private Dictionary<uint, List<ulong>> GetSearchDataByBPM(int BPM)
		{
			//Get list of hashes with correct BPM
			var searchDataByBPM = _context.DatabaseHashes.Where(v => v.BPM == BPM).ToList();

			Dictionary<uint, List<ulong>> SearchData = new Dictionary<uint, List<ulong>>();
			
			//Create Dictionary datastructure of hashes for fast search of the songs
			foreach(DatabaseHash DbHash in searchDataByBPM)
			{
				if (SearchData.ContainsKey(DbHash.Hash))
				{
					SearchData[DbHash.Hash].Add(DbHash.SongValue);
				}
				else
				{
					SearchData.Add(DbHash.Hash, new List<ulong>() { DbHash.SongValue });
				}
			}

			return SearchData;
		}

		/// <summary>
		/// Save SearchData datastructure to the database
		/// </summary>
		/// <param name="searchData"></param>
		/// <param name="BPM"></param>
		private void AddSearchDataToDB(Dictionary<uint, List<ulong>> searchData, int BPM)
		{
			// Iterate through all hashes
			foreach (KeyValuePair<uint, List<ulong>> hashWithSongValues in searchData)
			{
				// Iterate through all songValues
				foreach(ulong songValue in hashWithSongValues.Value)
				{
					// Create database Hash
					DatabaseHash databaseHash = new DatabaseHash
					{
						BPM = BPM,
						Hash = hashWithSongValues.Key,
						SongValue = songValue,
					};
					
					// Add hash to the database
					_context.Add(databaseHash);
				}
			}
		}

		/// <summary>
		/// Delete search data of given song from database.
		/// </summary>
		/// <param name="song">Song whose search data are to be deleted.</param>
		private void DeleteSongFromSearchData(Song song)
		{
			uint deleteSongId= song.Id;

			foreach (var hash in _context.DatabaseHashes.Where(v => v.BPM == song.BPM && (uint)v.SongValueDB == song.Id))
			{
				_context.DatabaseHashes.Remove(hash);
			}
		}

		/// <summary>
		/// Get accuracy of specific song by its id
		/// </summary>
		/// <param name="songId">Id of the song</param>
		/// <param name="songAccuracies">List of song accuracies</param>
		/// <returns>Accuracy of the song. 0 if songId is null</returns>
		private double GetSongAccuracy(uint? songId, List<Tuple<uint, double>> songAccuracies)
		{
			//find probability of recognition result song
			if (songId != null)
			{
				foreach (var accurs in songAccuracies)
				{
					if (accurs.Item1 == songId)
					{
						return accurs.Item2;
					}
				}
			}

			return 0d;
		}

		#endregion
	}
}
