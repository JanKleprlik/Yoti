﻿using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;

namespace Yoti.Shared.ViewModels
{
    public class LyricsViewModel : BaseViewModel
    {

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mainPageViewModel">Reference to the main page view model</param>
		public LyricsViewModel(MainPageViewModel mainPageViewModel)
		{
			_mainPageViewModel = mainPageViewModel;
		}

		private MainPageViewModel _mainPageViewModel;

		/// <summary>
		/// Lyrics to be edited.
		/// </summary>
		public string Lyrics
		{
			get => _mainPageViewModel.NewSongLyrics;
			set
			{
				_mainPageViewModel.NewSongLyrics = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Add new line at the end of the Lyrics.
		/// </summary>
		public void AddNewLine()
		{
			Lyrics += "\r\n";
		}
    }
}
