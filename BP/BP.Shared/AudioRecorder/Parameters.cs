using System;
using System.Collections.Generic;
using System.Text;

namespace BP.Shared.AudioRecorder
{
	public partial class Recorder
	{
		public static class Parameters
		{
			public static uint SamplingRate = 48000;

			public static uint Channels = 1;

			public static ulong MaxRecordingUploadSize_Mb = 1;
		}

	}
}
