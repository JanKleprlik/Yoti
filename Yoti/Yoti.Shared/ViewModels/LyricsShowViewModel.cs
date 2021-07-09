using SharedTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Yoti.Shared.ViewModels
{
    public class LyricsShowViewModel : BaseViewModel
    {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="song">Song whose lyrics are to be displayed.</param>
		public LyricsShowViewModel(Song song)
		{
			_song = song;
		}

		private Song _song;

		/// <summary>
		/// Lyrics of song that is supposed to be displayed.
		/// </summary>
		public string Lyrics
		{
			get => _song.Lyrics;
		}
		/// <summary>
		/// Name of the author of the song that is supposed to be displayed.
		/// </summary>
		public string Name
		{
			get => _song.Name;
		}
	}
}
