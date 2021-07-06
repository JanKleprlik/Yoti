using System;
using System.Collections.Generic;
using System.Text;

namespace AudioRecognitionLibrary.Recognizer
{
	public partial class AudioRecognizer
	{
		internal static class Parameters
		{
			/// <summary>
			/// Target sampling rate of audio used in recognition algorithm
			/// </summary>
			public static int TargetSamplingRate { get; set; } = 11025;
			/// <summary>
			/// Default size of FFT window
			/// </summary>
			public static int WindowSize { get; set; } = 4096;
			/// <summary>
			/// Default size of target zone
			/// </summary>
			public static int TargetZoneSize { get; set; } = 5;
			/// <summary>
			/// Default offset of anchor from first actual point
			/// </summary>
			public static int AnchorOffset { get; set; } = 2;
			/// <summary>
			/// Obligated portion of samples in TGZ
			/// </summary>
			public static double SamplesInTgzCoef { get; set; } = 0.55;
			/// <summary>
			/// Obligated portion of time coherent notes
			/// </summary>
			public static double TimeCoherentCoef { get; set; } = 0.4;
			/// <summary>e
			/// Low frequency limit for BPM detection
			/// </summary>
			public static float BPMLowFreq { get; set; } = 60f;
			/// <summary>
			/// High frequency liimt for BPM detection
			/// </summary>
			public static float BPMHighFreq { get; set; } = 180f;
			/// <summary>
			/// Nubmer of parts in a second for EnergyPeakDetection
			/// </summary>
			public static int PartsPerSecond { get; set; } = 2;
			/// <summary>
			/// Lowest allowed BPM
			/// </summary>
			public static float BPMLowLimit { get; set; } = 80f;
			/// <summary>
			/// Highest allowed BPM
			/// </summary>
			public static float BPMHighLimit { get; set; } = 170;
			/// <summary>
			/// Number of fllowing peaks to consider when getting BPM
			/// </summary>
			public static int PeakNeighbourRange { get; set; } = 10;
			/// <summary>
			/// Size of interval when approximating BPM
			/// </summary>
			public static int ApproximateIntervalSize { get; set; } = 5;
		}
	}
}
