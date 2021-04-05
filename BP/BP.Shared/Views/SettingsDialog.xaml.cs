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
using BP.Shared.Models;
using BP.Shared.ViewModels;
// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

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
