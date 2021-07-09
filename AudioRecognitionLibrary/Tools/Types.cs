using System;
using System.Collections.Generic;
using System.Text;

namespace AudioRecognitionLibrary
{
	/// <summary>
	/// Time Frequecny struct.
	/// </summary>
	internal struct TimeFrequencyPoint
	{
		public uint Time { get; set; }
		public uint Frequency { get; set; }
	}

	/// <summary>
	/// Energy peaks used for determining song BPM
	/// </summary>
	internal struct EnergyPeak
	{
		public float Energy { get; set; }
		public int Time { get; set; }
	}
}
