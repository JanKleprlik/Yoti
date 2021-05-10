using System;
using System.Collections.Generic;
using System.Text;

namespace AudioRecognitionLibrary
{
	[Serializable]
	public struct TimeFrequencyPoint
	{
		public uint Time { get; set; }
		public uint Frequency { get; set; }
	}

	public struct EnergyPeak
	{
		public float Energy { get; set; }
		public int Time { get; set; }
	}
}
