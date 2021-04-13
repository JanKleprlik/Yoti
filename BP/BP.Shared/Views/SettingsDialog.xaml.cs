using Windows.UI.Xaml.Controls;
using BP.Shared.ViewModels;


namespace BP.Shared.Views
{
	public sealed partial class SettingsDialog : ContentDialog
	{
		private SettingsViewModel SettingsViewModel;
		public SettingsDialog(SettingsViewModel settingsVM)
		{
			this.InitializeComponent();
			SettingsViewModel = settingsVM;
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			SettingsViewModel.Reset();
		}
	}
}
