using BP.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BP.Shared.ViewModels
{
	public class SettingsViewModel : BaseViewModel
	{
		public SettingsViewModel(Settings settings = null) => Settings = settings ?? new Settings();
		private Settings _settings;
		public Settings Settings
		{
			get => _settings;
			set
			{
				_settings = value;
				OnPropertyChanged(string.Empty);
			}
		}

		public bool DetailedInfo
		{
			get => Settings.DetailedInfo;
			set
			{
				Settings.DetailedInfo = value;
				OnPropertyChanged();
			}
		}

		public int RecordingLength
		{
			get => Settings.RecordingLength;
			set
			{
				Settings.RecordingLength = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(RecordingLengthText));
			}
		}

		public string RecordingLengthText => $"Recording length: {RecordingLength} sec";

		public int[] SupportedSamplingRates
		{
			get => _settings.SupportedSamplingRates;
		}

		public int[] SupportedNumbersOfChannels
		{
			get => _settings.SupportedNumbersOfChannels;
		}
		public Type[] SupportedAudioFormats
		{
			get => _settings.SupportedAudioFormats;
		}

		public bool UseMicrophone
		{
			get => Settings.UseMicrophone;
			set
			{
				Settings.UseMicrophone = value;
				OnPropertyChanged();
			}
		}
	
		public void Reset()
		{
			_settings.SetToDefault();
			Settings = _settings;
		}
	}
}
