using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using System.Collections.ObjectModel;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Database;

namespace BP.Shared.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class SongList : Page
	{
		private ObservableCollection<Song> songsList;
		private bool wasChange = false;
		public SongList()
		{
			this.InitializeComponent();
			//database = DatabaseSQLite.Instance;
			songsList = new ObservableCollection<Song>();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			UpdateSongsList();
			wasChange = false;
			base.OnNavigatedTo(e);
		}

		private void BackBtn_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(MainPage), wasChange);
		}

		private void UpdateSongsList()
		{
#if __WASM__ || __ANDROID__
			//var songs = database.GetSongs();
			//foreach (var song in songs)
			//{
			//	songsList.Add(song);
			//}
#else
			//songsList = new ObservableCollection<Song>(database.GetSongs());
#endif
			throw new NotImplementedException();
		}


		private async void SongBtn_Click(object sender, RoutedEventArgs e)
		{
			wasChange = true;
			var song = (sender as FrameworkElement).Tag as Song;
			this.Log().LogDebug($"Deleting song ID: {song.Id}\tName: {song.Name}\tAuthor: {song.Author}");

			//remove from database
			//await Task.Run(() => database.DeleteSong(song));
			throw new NotImplementedException("Delete song");

			//remove from currently displayed list
			songsList.Remove(song);
		}
	}
}
