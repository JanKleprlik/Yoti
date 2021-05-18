using Yoti.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Yoti.Shared.ViewModels
{
	public class SettingsViewModel : BaseViewModel
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings">Settings Model reference</param>
		public SettingsViewModel(Settings settings)
		{
			Settings = settings;
		}

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
		/// <summary>
		/// Show detailed info about song recognition.
		/// </summary>
		public bool DetailedInfo
		{
			get => Settings.DetailedInfo;
			set
			{
				Settings.DetailedInfo = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Length of microphone recording for song recognition.
		/// </summary>
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

		/// <summary>
		/// An array of supported sample rates
		/// </summary>
		public int[] SupportedSamplingRates
		{
			get => _settings.SupportedSamplingRates;
		}

		/// <summary>
		/// An array of supported number of audio channels in recording.
		/// </summary>
		public int[] SupportedNumbersOfChannels
		{
			get => _settings.SupportedNumbersOfChannels;
		}

		/// <summary>
		/// An array of supported audio formats (wav, ...).
		/// </summary>
		public Type[] SupportedAudioFormats
		{
			get => _settings.SupportedAudioFormats;
		}

		/// <summary>
		/// Determines a way to obtain audio for song recognition.<br></br>
		/// True - Microphone <br></br>
		/// False - File Picker
		/// </summary>
		public bool UseMicrophone
		{
			get => Settings.UseMicrophone;
			set
			{
				Settings.UseMicrophone = value;
				OnPropertyChanged();
			}
		}
		
		/// <summary>
		/// Reset settings to default values
		/// </summary>
		public void Reset()
		{
			_settings.SetToDefault();
			Settings = _settings;
		}
	}
}
