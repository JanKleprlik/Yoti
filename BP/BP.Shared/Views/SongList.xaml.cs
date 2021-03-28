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
using System.Collections.ObjectModel;

namespace BP.Shared.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class SongList : Page
	{
		private ObservableCollection<Song> songsList;
		public SongList()
		{
			this.InitializeComponent();
			songsList = new ObservableCollection<Song>();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is List<Song>)
			{
				var songs = e.Parameter as List<Song>;

#if __WASM__ || __ANDROID__
				foreach (var song in songs)
				{
					songsList.Add(song);
				}
#else
				songsList = new ObservableCollection<Song>(songs);
#endif
			}

			base.OnNavigatedTo(e);
		}

		private void BackBtn_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(MainPage));
		}

	}
}
