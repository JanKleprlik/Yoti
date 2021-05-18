using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Database;
using Yoti.Shared.RestApi;

namespace Yoti.Shared.Views
{
	/// <summary>
	/// Page showing list of songs in the database.
	/// </summary>
	public sealed partial class SongList : Page
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		public SongList()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// List of songs to display.
		/// </summary>
		private ObservableCollection<Song> songsList = new ObservableCollection<Song>();

		/// <summary>
		/// API for the server
		/// </summary>
		private RecognizerApi recognizerApi = new RecognizerApi();


		/// <summary>
		/// Fills songs collection when navigated to this page.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is List<Song>)
			{
				// Initialization of ObservableCollection with List<Song> does not
				// work on Android and WASM thus songs must be added one by one.
#if __WASM__ || __ANDROID__
				var songs = e.Parameter as List<Song>;
				foreach (var song in songs)
				{
					songsList.Add(song);
				}
#else
				songsList = new ObservableCollection<Song>(e.Parameter as List<Song>);
#endif
			}

			base.OnNavigatedTo(e);
		}

		/// <summary>
		/// Back button handler.
		/// </summary>
		private void NavigateBack(object sender, RoutedEventArgs e)
		{ 
			Frame.Navigate(typeof(MainPage));
		}

		/// <summary>
		/// Lyrics button handler.
		/// </summary>
		private async void ShowLyrics(object sender, RoutedEventArgs e)
		{
			var song = (sender as FrameworkElement).Tag as Song;

			var lyricsShowDialog = new LyricsShowDialog(song);
			await lyricsShowDialog.ShowAsync();
		}

		/// <summary>
		/// Delete button handler.
		/// </summary>
		/// <param name="song"></param>
		private async void DeleteSong(object sender, RoutedEventArgs e)
		{
			var song = (sender as FrameworkElement).Tag as Song;

			// Remove song from database
			Song result = await recognizerApi.DeleteSong(song);

			// Remove song from currently displayed list
			songsList.Remove(song);
		}
	}
}
