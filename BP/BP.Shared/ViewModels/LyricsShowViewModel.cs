using Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace BP.Shared.ViewModels
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
			get => _song.lyrics.Replace("\r\n", Environment.NewLine); //unify NewLine according to app enviroment
			private set	{ /*nothing should happen*/ }
		}
		/// <summary>
		/// Name of the author of the song that is supposed to be displayed.
		/// </summary>
		public string Name
		{
			get => _song.name;
			private set { /*nothing should happen*/ }
		}
	}
}
