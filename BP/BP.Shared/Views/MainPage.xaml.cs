using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using BP.Shared.Models;
using BP.Shared.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace BP.Shared.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
		private Settings settings;
		private SettingsViewModel SettingsVM;
		private MainPageViewModel MainPageVM;

		private Storyboard flickerAnimation;
		private SettingsDialog settingsDialog;

		public MainPage()
        {
            this.InitializeComponent();

			settings = new Settings();
			SettingsVM = new SettingsViewModel(settings);
			MainPageVM = new MainPageViewModel(outputTextBox, settings, Dispatcher);

			setupFlickerAnimation();
			setupSettingsDialog(settings);
		}

		public async void SettingsBtn_Click(object sender, RoutedEventArgs e)
		{
			ContentDialogResult result = await settingsDialog.ShowAsync();
		}
		public void ListSongsBtn_Click(object sender, RoutedEventArgs e)
		{
			//Frame.Navigate(typeof(SongList), MainPageVM.Database.GetSongs());
			throw new NotImplementedException();
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


		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is Boolean && (bool)e.Parameter)
			{
				MainPageVM.UpdateSavedSongs();				
			}

			base.OnNavigatedTo(e);
		}

	}
}
