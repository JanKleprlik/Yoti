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
using System.Threading.Tasks;

using System.Threading;
using System.Text.RegularExpressions;
using Database;
using AudioProcessing.Recognizer;
using Windows.UI.Xaml.Media.Animation;
using System.Diagnostics;
using System.Text;
using BP.Shared.Models;
using BP.Shared.ViewModels;
using Microsoft.Extensions.Logging;
using Uno.Extensions;


#if NETFX_CORE
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
#endif

#if __ANDROID__
using System.Diagnostics;
using Android.Media;
using Android.Content.PM;
using Xamarin.Essentials;
#endif

#if __WASM__
using Uno.Foundation;
#endif

namespace BP.Shared.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
		private Storyboard flickerAnimation;
		private SettingsDialog settingsDialog;

		private Settings settings;
		private SettingsViewModel SettingsVM;
		private MainPageViewModel MainPageVM;

		public MainPage()
        {
            this.InitializeComponent();

			settings = new Settings();
			SettingsVM = new SettingsViewModel(settings);
			MainPageVM = new MainPageViewModel(outputTextBox, settings, Dispatcher);

			setupFlickerAnimation();
			setupSettingsDialog(settings);
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
		private void setupSettingsDialog(Settings settings)
		{
			settingsDialog = new SettingsDialog(SettingsVM);
			Grid.SetRowSpan(settingsDialog, 3);
			Grid.SetColumnSpan(settingsDialog, 3);			
		}

		// THIS WILL STAY
		private async void SettingsBtn_Click(object sender, RoutedEventArgs e)
		{
			ContentDialogResult result = await settingsDialog.ShowAsync();
		}
		private void ListSongsBtn_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(SongList), MainPageVM.Database.GetSongs());
		}
	}
}
