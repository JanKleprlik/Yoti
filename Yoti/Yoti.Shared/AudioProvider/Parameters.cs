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
			public static uint SamplingRate = 48000;

			/// <summary>
			/// Number of audio channels.
			/// </summary>
			public static uint Channels = 1;

			/// <summary>
			/// Maximum size of file that can be recorded.
			/// </summary>
			public static ulong MaxRecordingUploadSize_Mb = 1;
		}

	}
}
