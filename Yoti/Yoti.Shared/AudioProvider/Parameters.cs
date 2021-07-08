using System;
using System.Collections.Generic;
using System.Text;

namespace Yoti.Shared.AudioProvider
{
	public partial class AudioDataProvider
	{
		public static class Parameters
		{
			/// <summary>
			/// Audio sampling rate.
			/// </summary>
			public static uint SamplingRate { get; set; } = 44100;

			/// <summary>
			/// Number of audio channels.
			/// </summary>
			public static uint Channels { get; set; } = 1;

			/// <summary>
			/// Maximum size of file that can be uploaded for recognition.
			/// </summary>
			public static ulong MaxRecordingUploadSize_Mb { get; set; } = 1;

			/// <summary>
			/// Maximum size of file that can be uploaded to database.
			/// </summary>
			public static ulong MaxUploadSize_Mb { get; set; } = 50;
		}

	}
}
