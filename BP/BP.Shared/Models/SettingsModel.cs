using AudioRecognitionLibrary.AudioFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Yoti.Shared.Models
{
	public class Settings
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public Settings()
		{
			SetToDefault();
		}

		/// <summary>
		/// <b>True</b>: Use Constant Q algorithm to get frequency domain.<br/>
		/// <b>False</b>: Use FFT to get frequency domain.<br/>
		/// Default: False
		/// </summary>
		public bool Lyrics { get; set; }

		/// <summary>
		/// <b>True</b>: Print detailed information about recognition. <br/>
		/// <b>False</b>: Print only result.<br/>
		/// Default: False
		/// </summary>
		public bool DetailedInfo { get; set; }

		/// <summary>
		/// ONLY WASM <br/>
		/// <b>True</b>: Use microphone for recording.<br/>
		/// <b>False</b>: Use file upload for recording.<br/>
		/// Default: False
		/// </summary>
		public bool UseMicrophone { get; set; }

		/// <summary>
		/// The time for which the recording will be recorded.<br/>
		/// Minimum 3 sec <br/>
		/// Maximum 8 sec <br/>
		/// Default: 3 sec
		/// </summary>
		public int RecordingLength { 
			get	{
				return this._recordingLength;
			}
			set
			{
				this._recordingLength = Math.Max(Math.Min(value, 8), 3); //set min 3, max 8 secs
			}
		}
		private int _recordingLength;

		/// <summary>
		/// An array of supported number of channels.
		/// </summary>
		public int[] SupportedNumbersOfChannels { get; } = new int[] { 1, 2 };

		/// <summary>
		/// An array of supported sample rates.
		/// </summary>
		public int[] SupportedSamplingRates { get; } = new int[] { 48000};

		/// <summary>
		/// An array of supported Audio Formats
		/// </summary>
		public Type[] SupportedAudioFormats { get; } = new Type[] { typeof(WavFormat) };


		/// <summary>
		/// Resets settings to default values. <br></br>
		/// Lyrics - False <br></br>
		/// DetailedInfo - False <br></br>
		/// RecordingLength - 5 <br></br>
		/// UseMicrophone - True
		/// </summary>
		public void SetToDefault()
		{
			Lyrics = false;
			DetailedInfo = false;
			RecordingLength = 5;
			UseMicrophone = true;
		}


		public override string ToString()
		{
			string text = "SETTINGS: \n" +
				$"ConstQAlgorithm: {Lyrics}\n" +
				$"DetailedInfo: {DetailedInfo}\n" +
				$"UseMicrophone: {UseMicrophone}\n" +
				$"RecordingLength: {RecordingLength}";
			return text;
		}
	}
}
