using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using BP.Shared.Models;
using BP.Shared.ViewModels;
using Windows.UI.Xaml.Navigation;
using System.Collections.Generic;
using Database;
using Uno.Extensions;
using Microsoft.Extensions.Logging;

namespace BP.Shared.Views
{
    public sealed partial class MainPage : Page
    {
		private Settings settings;
		private SettingsViewModel SettingsVM;
		private MainPageViewModel MainPageVM;

		private Storyboard flickerAnimation;
		private SettingsDialog settingsDialog;
		private LyricsDialog lyricsDialog;

		public MainPage()
        {
            this.InitializeComponent();

			settings = new Settings();
			SettingsVM = new SettingsViewModel(settings);
			MainPageVM = new MainPageViewModel(outputTextBox, settings, Dispatcher);

			setupFlickerAnimation();
			setupContentDialogs();
		}

		public async void SettingsBtn_Click(object sender, RoutedEventArgs e)
		{
			ContentDialogResult result = await settingsDialog.ShowAsync();
		}

		public async void LyricsBtn_Click(object sender, RoutedEventArgs e)
		{
			ContentDialogResult resutl = await lyricsDialog.ShowAsync();
		}

		public async void ListSongsBtn_Click(object sender, RoutedEventArgs e)
		{

			List<Song> songs = await MainPageVM.RecognizerApi.GetSongs();

			Frame.Navigate(typeof(SongList), songs);

		}


		private void setupFlickerAnimation()
		{
			flickerAnimation = new Storyboard();

			DoubleAnimation opacityAnimation = new DoubleAnimation()
			{
				From = 1.0,
				To = 0.0,
				BeginTime = TimeSpan.FromSeconds(0.5),
				AutoReverse = true,
				Duration = new Duration(TimeSpan.FromSeconds(0.18))
			};

			Storyboard.SetTarget(flickerAnimation, flickerIcon);
			Storyboard.SetTargetProperty(flickerAnimation, "Opacity");
			flickerAnimation.Children.Add(opacityAnimation);
			flickerAnimation.RepeatBehavior = RepeatBehavior.Forever;
			flickerAnimation.Begin();
		}
		private void setupContentDialogs()
		{
			settingsDialog = new SettingsDialog(SettingsVM);
			Grid.SetRowSpan(settingsDialog, 3);
			Grid.SetColumnSpan(settingsDialog, 3);

			lyricsDialog = new LyricsDialog(MainPageVM);
			Grid.SetRowSpan(lyricsDialog, 3);
			Grid.SetColumnSpan(lyricsDialog, 3);
		}


		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
		}

	}
}
