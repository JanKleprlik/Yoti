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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

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

		private async void AddNewSong_Click(object sender, RoutedEventArgs e)
		{
			ShowAddNewSongUI();
		}		
		private async void CancelNewSong_Click(object sender, RoutedEventArgs e)
		{
			HideAddNewSongUI();
		}

		private async void ShowTermsOfUseContentDialogButton_Click(object sender, RoutedEventArgs e)
		{
			ContentDialogResult result = await settingsContentDialog.ShowAsync();
		}


		#region HELPERS
		private void HideAddNewSongUI()
		{
			UploadGrid.Visibility = Visibility.Collapsed;
			AddNewSongBtn.Visibility = Visibility.Visible;
			ListSongsBtn.Visibility = Visibility.Visible;
		}
		
		private void ShowAddNewSongUI()
		{
			UploadGrid.Visibility = Visibility.Visible;
			AddNewSongBtn.Visibility = Visibility.Collapsed;
			ListSongsBtn.Visibility = Visibility.Collapsed;
		}
		#endregion
	}
}
