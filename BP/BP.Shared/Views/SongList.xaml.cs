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
using Database;

namespace BP.Shared.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class SongList : Page
	{
		public SongList()
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is List<Song>)
			{
				updateSongList(e.Parameter as List<Song>);
			}

			base.OnNavigatedTo(e);
		}

		private void updateSongList(List<Song> songs)
		{
			List<string> songNames = new List<string>();
			foreach (Song song in songs)
			{
				songNames.Add(song.Name);
				Console.Out.WriteLine($"[DEBUG] {song.Name}");
			}

			songList.ItemsSource = songNames;
		}

		private void BackBtn_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(MainPage));
		}

	}
}
