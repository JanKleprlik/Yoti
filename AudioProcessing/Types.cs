using System;
using System.Collections.Generic;
using System.Text;

namespace AudioProcessing
{
	[Serializable]
	public struct TimeFrequencyPoint
	{
		public uint Time { get; set; }
		public uint Frequency { get; set; }
	}
}
