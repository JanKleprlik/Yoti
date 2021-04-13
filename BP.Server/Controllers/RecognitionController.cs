using BP.Server.Models;
using Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BP.Server.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class RecognitionController : ControllerBase
	{
		private readonly SongContext _context;
		private readonly SearchDataCollection _searchData;

		public RecognitionController(SongContext context, SearchDataCollection searchDataCollection)
		{
			_context = context;
			_searchData = searchDataCollection;
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

	}
}
