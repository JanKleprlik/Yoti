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
		private Settings settings;
		private SettingsViewModel SettingsViewModel;
		public SettingsDialog(Settings settings)
		{
			this.InitializeComponent();
			SettingsViewModel = new SettingsViewModel(settings);
			this.settings = settings;
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			//reset settins to default
			settings.SetToDefault();
			SettingsViewModel.Settings = settings;

		}

		#region WITHOUT MVVM
		/*
		private void ConstQAlg_Switched(object sender, RoutedEventArgs e)
		{
			ToggleSwitch toggleSwitch = sender as ToggleSwitch;
			if (toggleSwitch != null)
			{
				settings.ConstQAlgorithm = toggleSwitch.IsOn;
				System.Diagnostics.Debug.WriteLine($"Const Q alg: {settings.ConstQAlgorithm}");
				
			}
		}

		private void DetailedInfo_Switched(object sender, RoutedEventArgs e)
		{
			ToggleSwitch toggleSwitch = sender as ToggleSwitch;
			if (toggleSwitch != null)
			{
				settings.DetailedInfo = toggleSwitch.IsOn;
				System.Diagnostics.Debug.WriteLine($"Detailed info: {settings.DetailedInfo}");
			}
		}

		private void RecordingLength_Changed(object sender, RangeBaseValueChangedEventArgs e)
		{
			Slider slider = sender as Slider;
			if (slider != null)
			{
				//settings.RecordingLength = Convert.ToInt32(slider.Value);
				//System.Diagnostics.Debug.WriteLine($"Recording length: {settings.RecordingLength}");
				System.Diagnostics.Debug.WriteLine($"Recording length: {slider.Value}");
			}
		}

		private void setConstQAlgorithm(bool value) => settings.ConstQAlgorithm= value;
		private void setDetailedInfo(bool value) => settings.DetailedInfo = value;
		private void setMicrophoneUse(bool value) => settings.UseMicrophone = value;
		private void setRecordingLength(double value) => settings.RecordingLength = Convert.ToInt32(value);

		#region WASM
#if __WASM__
		private void MicrophoneUse_Switched(object sender, RoutedEventArgs e)
		{
			ToggleSwitch toggleSwitch = sender as ToggleSwitch;
			if (toggleSwitch != null)
			{	
				settings.UseMicrophone = toggleSwitch.IsOn;
				System.Diagnostics.Debug.WriteLine($"Use microphone: {toggleSwitch.IsOn}");
			}
		}
#endif
		#endregion
		*/
		#endregion
	}
}
