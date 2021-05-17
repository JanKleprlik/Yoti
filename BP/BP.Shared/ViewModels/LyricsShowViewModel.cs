using Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace BP.Shared.ViewModels
{
    public class LyricsShowViewModel : BaseViewModel
    {

		public LyricsShowViewModel(Song song)
		{
			_song = song;
		}

		private Song _song;

		public string Lyrics
		{
			get => _song.lyrics.Replace("\r\n", Environment.NewLine);
			private set	{ /*nothing should happen*/ }
		}

		private string _name;
		public string Name
		{
			get => _song.name;
			private set { /*nothing should happen*/ }
		}
	}
}
