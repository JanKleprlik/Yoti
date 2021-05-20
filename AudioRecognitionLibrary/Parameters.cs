using System;
using System.Collections.Generic;
using System.Text;

namespace AudioRecognitionLibrary.Recognizer
{
	public partial class AudioRecognizer
	{
		internal static class Parameters
		{
			public const int TargetSamplingRate = 12000;
			/// <summary>
			/// Default size of FFT window
			/// </summary>
			public static int WindowSize = 4096;
			/// <summary>
			/// Default downsample coeficient
			/// </summary>
			public static int DownSampleCoef = 4;
			/// <summary>
			/// Default size of target zone
			/// </summary>
			public static int TargetZoneSize = 5;
			/// <summary>
			/// Default offset of anchor from first actual point
			/// </summary>
			public static int AnchorOffset = 2;
			/// <summary>
			/// Obligated portion of samples in TGZ
			/// </summary>
			public static double SamplesInTgzCoef = 0.55;
			/// <summary>
			/// Obligated portion of time coherent notes
			/// </summary>
			public static double CoherentNotesCoef = 0.4;
			/// <summary>e
			/// Low frequency limit for BPM detection
			/// </summary>
			public static float BPMLowFreq = 60f;
			/// <summary>
			/// High frequency liimt for BPM detection
			/// </summary>
			public static float BPMHighFreq = 180f;
			/// <summary>
			/// Nubmer of parts in a second for EnergyPeakDetection
			/// </summary>
			public static int PartsPerSecond = 2;
			/// <summary>
			/// Lowest allowed BPM
			/// </summary>
			public static float BPMLowLimit = 80f;
			/// <summary>
			/// Highest allowed BPM
			/// </summary>
			public static float BPMHighLimit= 170;
			/// <summary>
			/// Number of fllowing peaks to consider when getting BPM
			/// </summary>
			public static int PeakNeighbourRange = 10;
			/// <summary>
			/// Size of interval when approximating BPM
			/// </summary>
			public static int ApproximateIntervalSize = 5;
		}
	}
}
