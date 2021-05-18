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
using BP.Shared.RestApi;

namespace BP.Shared.Views
{
    public sealed partial class MainPage : Page
    {

		/// <summary>
		/// Constructor
		/// </summary>
		public MainPage()
        {
            this.InitializeComponent();

			Settings appSettings = new Settings();

			SettingsViewModel = new SettingsViewModel(appSettings);
			MainPageViewModel = new MainPageViewModel(outputTextBox, appSettings, SettingsViewModel, Dispatcher);
			setupFlickerAnimation();
		}

		/// <summary>
		/// Main page view model.
		/// </summary>
		public MainPageViewModel MainPageViewModel;

		public SettingsViewModel SettingsViewModel;

		/// <summary>
		/// Animation indicating recording process.
		/// </summary>
		private Storyboard flickerAnimation;

		/// <summary>
		/// Navigation to Song List page handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async void NavigateToSongList(object sender, RoutedEventArgs e)
		{
			var recognizerApi = new RecognizerApi();
			List<Song> songs = await recognizerApi.GetSongs();

			Frame.Navigate(typeof(SongList), songs);
		}

		/// <summary>
		/// Updates settings if naviaget to from Settings page..
		/// </summary>
		/// <param name="e"></param>
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is Settings)
			{
				MainPageViewModel.Settings = e.Parameter as Settings;
			}

			base.OnNavigatedTo(e);
		}


		/// <summary>
		/// Flicker animation setup.
		/// </summary>
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
	}
}
