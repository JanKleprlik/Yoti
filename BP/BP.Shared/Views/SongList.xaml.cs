using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Database;
using BP.Shared.RestApi;

namespace BP.Shared.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class SongList : Page
	{
		private ObservableCollection<Song> songsList;
		private RecognizerApi recognizerapi = new RecognizerApi();
		public SongList()
		{
			this.InitializeComponent();
			songsList = new ObservableCollection<Song>();

		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is List<Song>)
			{
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

		private void BackBtn_Click(object sender, RoutedEventArgs e)
		{ 
			Frame.Navigate(typeof(MainPage));
		}

		private async void SongBtn_Click(object sender, RoutedEventArgs e)
		{
			var song = (sender as FrameworkElement).Tag as Song;
			this.Log().LogDebug($"Deleting song ID: {song.id}\tName: {song.name}\tAuthor: {song.author}");

			//remove from database
			Song result = await recognizerapi.DeleteSong(song);

			this.Log().LogDebug(song.ToString());
			this.Log().LogDebug(result.ToString());
			//remove from currently displayed list
			songsList.Remove(song);
		}
	}
}
