using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioProcessing.AudioFormats;
using AudioProcessing.Processor;

namespace AudioProcessing.Recognizer
{
	public partial class AudioRecognizer
	{
		private TextWriter output { get; set; } = null;
		private bool IsOutputSet = false;

		public AudioRecognizer(TextWriter textWriter)
		{
			IsOutputSet = true;
			output = textWriter;
		}

		public AudioRecognizer()
		{
			IsOutputSet = false;
			output = Console.Out;
		}
		/// <summary>
		/// WARNING: Converts audio into single channel.
		/// </summary>
		/// <param name="audio"></param>
		/// <returns></returns>
		public List<TimeFrequencyPoint> GetTimeFrequencyPoints(IAudioFormat audio)
		{
			AudioProcessor.ConvertToMono(audio);

			double[] data = Array.ConvertAll(audio.Data, item => (double)item);

			double[] downsampledData = AudioProcessor.DownSample(data, Parameters.DownSampleCoef, audio.SampleRate);

			int bufferSize = Parameters.WindowSize / Parameters.DownSampleCoef; //default: 4096/4 = 1024
			var TimeFrequencyPoints = AudioProcessor.CreateTimeFrequencyPoints(bufferSize, downsampledData, sensitivity: 1);

			return TimeFrequencyPoints;
		}

		public uint? RecognizeSong(List<TimeFrequencyPoint> timeFrequencyPoints, Dictionary<uint, List<ulong>> database, TextWriter textWriter = null)
		{
			//set custom output if not set at initialization
			if (!IsOutputSet && textWriter != null)
				output = textWriter;

			uint? finalSongID = FindBestMatch(database, timeFrequencyPoints);

			//unset custom output
			if (!IsOutputSet)
				output = null;

			return finalSongID;
		}

		public void AddTFPToDataStructure(List<TimeFrequencyPoint> timeFrequencyPoints, in uint songID, Dictionary<uint, List<ulong>> database)
		{
			/* spectogram:
			 *
			 * |
			 * |       X X
			 * |         X
			 * |     X     X
			 * |   X         X
			 * | X X X     X
			 * x----------------
			 */


			// -targetZoneSize: because of end limit 
			// -1: because of anchor point at -2 position target zone
			int stopIdx = timeFrequencyPoints.Count - Parameters.TargetZoneSize - Parameters.AnchorOffset;
			for (int i = 0; i < stopIdx; i++)
			{
				//anchor is at idx i
				//1st in TZ is at idx i+3
				//5th in TZ is at idx i+7

				uint anchorFreq = timeFrequencyPoints[i].Frequency;
				uint anchorTime = timeFrequencyPoints[i].Time;
				ulong SongValue = Tools.Builders.BuildSongValue(anchorTime, songID);
				for (int pointNum = 3; pointNum < Parameters.TargetZoneSize + 3; pointNum++)
				{
					uint pointFreq = timeFrequencyPoints[i + pointNum].Frequency;
					uint pointTime = timeFrequencyPoints[i + pointNum].Time;

					uint address = Tools.Builders.BuildAddress(anchorFreq, pointFreq, pointTime - anchorTime);

					if (!database.ContainsKey(address)) //create new instance if it doesnt exist
					{
						database.Add(address, new List<ulong>() { SongValue });
					}
					else //add SongValue to the list of
					{
						database[address].Add(SongValue);
					}
				}

			}
		}

		public uint? FindBestMatch(Dictionary<uint, List<ulong>> database, List<TimeFrequencyPoint> timeFrequencyPoints)
		{
			//[address;(AbsAnchorTimes)]
			Dictionary<uint, List<uint>> recordAddresses = CreateRecordAddresses(timeFrequencyPoints);
			//get quantities of each SongValue to determine wether they make a complete TGZ (5+ points correspond to the same SongValue)
			var quantities = GetSongValQuantities(recordAddresses, database);

			//filter songs and addresses that only 
			var filteredSongs = FilterSongs(recordAddresses, quantities, database);

			//get longest delta for each song that says how many notes from recording are time coherent to the song
			//[sognID, delta]
			var deltas = GetDeltas(filteredSongs, recordAddresses);

			//pick the songID with highest delta
			return MaximizeTimeCoherency(deltas, timeFrequencyPoints.Count);
		}

		/// <summary>
		/// WARNING: Converts audio into single chanel.
		/// </summary>
		/// <param name="audio"></param>
		/// <returns></returns>
		public int GetBPM(IAudioFormat audio)
		{
			AudioProcessor.ConvertToMono(audio);
			//using floats instead of doubles here because of filters from NAudio library
			float[] data = Array.ConvertAll(audio.Data, item => (float)item);


			FilterBPMFrequencies(data, (float)audio.SampleRate);

			EnergyPeak[] energyPeaks = GetEnergyPeaks(data, audio.SampleRate);

			return GetMostProbableBPM(energyPeaks, (float)audio.SampleRate);
		}

		private int GetMostProbableBPM(EnergyPeak[] energyPeaks, float sampleRate)
		{
			//<BPM, Count>
			Dictionary<int, int> BPMs = new Dictionary<int, int>();

			for(int currentIdx = 0; currentIdx < energyPeaks.Length; currentIdx++)
			{
				EnergyPeak currentPeak = energyPeaks[currentIdx];
				for(int neighbourNumber = 1; neighbourNumber < Parameters.PeakNeighbourRange; neighbourNumber++)
				{
					int neighbourIdx = currentIdx + neighbourNumber;
					//Out of EnergyPeaks bounds
					if (neighbourIdx >= energyPeaks.Length)
						break;
					
					EnergyPeak neighbourPeak = energyPeaks[neighbourIdx];
					int BPM = GetPeakBPM(currentPeak, neighbourPeak, sampleRate);

					//Save BPM into data structure
					if (BPMs.ContainsKey(BPM))
						BPMs[BPM]++; //raise number of occurances
					else
						BPMs.Add(BPM, 1); //start with 1 occurace
				}
			}

			int maxOccurance = 0;
			int resultBPM = 0;
			//BPM - COUNT
			List<KeyValuePair<int, int>> res = new List<KeyValuePair<int, int>>();
			foreach(KeyValuePair<int, int> entry in BPMs)
			{
				if (entry.Value > maxOccurance)
				{
					maxOccurance = entry.Value;
					resultBPM = entry.Key;
				}
			}
			return resultBPM;
		}

		private int GetPeakBPM(EnergyPeak currentPeak, EnergyPeak neighbourPeak, float sampleRate)
		{
			float deltaT = neighbourPeak.Time - currentPeak.Time;
			float potentialBPM = 60f * sampleRate / deltaT; //60 for minute

			//Raise BPM up by a factor of two until in range
			while (potentialBPM < Parameters.BPMLowLimit)
			{
				potentialBPM *= 2;
			}
			//Lower BPM down by a factor of two until in range
			while(potentialBPM > Parameters.BPMHighLimit)
			{
				potentialBPM /= 2;
			}

			return Convert.ToInt32(potentialBPM);
		}

		private EnergyPeak[] GetEnergyPeaks(float[] data, uint sampleRate)
		{
			int samplesInPart = (int)sampleRate / Parameters.PartsPerSecond;
			int totalParts = data.Length / samplesInPart;
			EnergyPeak[] peaks = new EnergyPeak[totalParts];

			//foreach part get maximum energy peak
			for (int i = 0; i < totalParts; i++)
			{
				peaks[i] = GetMaxEnergyPeak(data, i * samplesInPart, samplesInPart);
			}

			return peaks;
		}


		/// <summary>
		/// Get maximum EnergyPeak from data in interval [start, start+steps]
		/// </summary>
		/// <param name="data"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		private EnergyPeak GetMaxEnergyPeak(float[] data, int start, int steps)
		{
			//Set max with first value
			EnergyPeak max = new EnergyPeak()
			{
				Energy = data[start],
				Time = start
			};
			for (int i = start + 1; i < start + steps; i++)
			{
				//save max
				if (max.Energy < data[i])
				{
					max.Energy = data[i];
					max.Time = i;
				}
			}

			return max;
		}

		private void FilterBPMFrequencies(float[] data, float sampleRate)
		{

			BiQuadFilter lowpass = BiQuadFilter.LowPassFilter(sampleRate, Parameters.BPMHighFreq, 1f);
			BiQuadFilter highpass = BiQuadFilter.HighPassFilter(sampleRate, Parameters.BPMLowFreq, 1f);

			for (int i = 0; i < data.Length; i++)
			{
				data[i] = highpass.Transform(lowpass.Transform(data[i]));
			}

		}


		/// <summary>
		/// Picks song with the most notes corresponding to the recording.
		/// </summary>
		/// <param name="deltas">[songID, num of time coherent notes]</param>
		/// <param name="totalNotes">total number of notes in recording</param>
		/// <returns></returns>
		private uint? MaximizeTimeCoherency(Dictionary<uint, int> deltas, int totalNotes)
		{
			uint? songID = null;
			int longestCoherency = 0;
			System.Diagnostics.Debug.WriteLine($"   Total number of notes: {totalNotes}");
			foreach (var pair in deltas)
			{
				System.Diagnostics.Debug.WriteLine($"   Song ID: {pair.Key} has {pair.Value} coherent notes which is {(double)pair.Value / totalNotes * 100:##.###} %");
				output.WriteLineAsync($"   Song ID: {pair.Key} has {pair.Value} coherent notes which is {Math.Min(100d, (double)pair.Value / totalNotes * 100):##.###} %");
				if (pair.Value > longestCoherency && pair.Value > Parameters.CoherentNotesCoef * totalNotes)
				{
					longestCoherency = pair.Value;
					songID = pair.Key;
				}
			}
			if (songID != null)
				System.Diagnostics.Debug.WriteLine($"\n   Song ID: {songID} has {longestCoherency} coherent notes which is {(double)longestCoherency / totalNotes * 100:##.###} %");
				output.WriteLineAsync($"\n   Song ID: {songID} has {longestCoherency} coherent notes which is {Math.Min(100d, (double)longestCoherency / totalNotes * 100):##.###} %");
			return songID;
		}

		/// <summary>
		/// Returns 
		/// </summary>
		/// <param name="filteredSongs"></param>
		/// <param name="recordAddresses"></param>
		/// <returns></returns>
		private static Dictionary<uint, int> GetDeltas(Dictionary<uint, Dictionary<uint, List<uint>>> filteredSongs, Dictionary<uint, List<uint>> recordAddresses)
		{
			//[songID, delta]
			Dictionary<uint, int> maxTimeCoherentNotes = new Dictionary<uint, int>();
			foreach (var songID in filteredSongs.Keys)
			{
				//[delta, occurrence]
				Dictionary<long, int> songDeltasQty = new Dictionary<long, int>();
				foreach (var address in recordAddresses.Keys)
				{
					if (filteredSongs[songID].ContainsKey(address))
					{
						/*
						 * DEBUG NOTE: at 400Hz song, there are 5 different addresses
						 * 			- because: each TGZ has 5 samples so 5 offsets must be created
						 */
						foreach (var absSongAnchTime in filteredSongs[songID][address]) //foreach AbsSongAnchorTime at specific address 
						{
							foreach (var absRecAnchTime in recordAddresses[address]) //foreach AbsRecordAnchorTime at specific address
							{
								//delta can be negative (RecTime = 8 with mapped SongTime = 2)
								long delta = (long)absSongAnchTime - (long)absRecAnchTime;
								if (!songDeltasQty.ContainsKey(delta))
									songDeltasQty.Add(delta, 0);
								songDeltasQty[delta]++;
							}
						}
					}
				}
				//get number of notes that are coherent with the most deltas (each note has same delta from start of the song)
				int timeCohNotes = MaximizeDelta(songDeltasQty);
				maxTimeCoherentNotes.Add(songID, timeCohNotes);
			}
			return maxTimeCoherentNotes;
		}

		/// <summary>
		/// Gets the delta with the most occurrences
		/// </summary>
		/// <param name="deltasQty">[delta, quantity]</param>
		/// <returns></returns>
		private static int MaximizeDelta(Dictionary<long, int> deltasQty)
		{
			int maxOccrence = 0;
			foreach (var pair in deltasQty)
			{
				if (pair.Value > maxOccrence)
				{
					maxOccrence = pair.Value;
				}
			}
			return maxOccrence;
		}

		/// <summary>
		/// Gets quantities of song values connected with common addresses in recording.
		/// </summary>
		/// <param name="recordAddresses">addresses in recording</param>
		/// <returns>[songValue, occurrence]</returns>
		private static Dictionary<ulong, int> GetSongValQuantities(Dictionary<uint, List<uint>> recordAddresses, Dictionary<uint, List<ulong>> database)
		{
			var quantities = new Dictionary<ulong, int>();

			foreach (var address in recordAddresses.Keys)
			{
				if (database.ContainsKey(address))
				{
					foreach (var songValue in database[address])
					{
						if (!quantities.ContainsKey(songValue))
						{
							quantities.Add(songValue, 1);
						}
						else
						{
							quantities[songValue]++;

						}
					}
				}
			}

			return quantities;
		}

		/// <summary>
		/// Filter out songs that don't have enough common samples with recording
		/// </summary>
		/// <param name="recordAddresses">Addresses in recording</param>
		/// <param name="quantities">occurrences of songvalues common with recording</param>
		/// <returns>[songID, [address, (absSongAnchorTime)]]</returns>
		private Dictionary<uint, Dictionary<uint, List<uint>>> FilterSongs(Dictionary<uint, List<uint>> recordAddresses, Dictionary<ulong, int> quantities, Dictionary<uint, List<ulong>> database)
		{
			//[songID, [address, (absSongAnchorTime)]]
			Dictionary<uint, Dictionary<uint, List<uint>>> res = new Dictionary<uint, Dictionary<uint, List<uint>>>();
			//[songID, common couple amount]
			Dictionary<uint, int> commonCoupleAmount = new Dictionary<uint, int>();
			//[songID, common couples in TGZ amount]
			Dictionary<uint, int> commonTGZAmount = new Dictionary<uint, int>();

			//Create datastructure for fast search in time coherency check
			//SongID -> Address -> absSongAnchorTime
			foreach (var address in recordAddresses.Keys)
			{
				if (database.ContainsKey(address))
				{
					foreach (var songValue in database[address])
					{
						uint songID = (uint)songValue;
						if (!commonCoupleAmount.ContainsKey(songID))
							commonCoupleAmount.Add(songID, 0);
						commonCoupleAmount[songID]++;

						//filter out addresses that do not create TGZ
						if (quantities[songValue] >= Parameters.TargetZoneSize)      
						{
							if (!commonTGZAmount.ContainsKey(songID))
								commonTGZAmount.Add(songID, 0);
							commonTGZAmount[songID]++;


							uint AbsSongAnchTime = (uint)(songValue >> 32);

							if (!res.ContainsKey(songID)) //add songID entry
								res.Add(songID, new Dictionary<uint, List<uint>>());
							if (!res[songID].ContainsKey(address)) //add address entry
								res[songID].Add(address, new List<uint>());

							//add the actual Absolute Anchor Time of a song
							res[songID][address].Add(AbsSongAnchTime);
						}
					}
				}
			}

			//Print number of common couples in each song and record
			foreach (uint songID in res.Keys)
			{
				int couples = commonCoupleAmount[songID];
				int TGZs = commonTGZAmount[songID];
				//NOTE: result can be more than 100% (some parts of the songs may repeat - refrains)
				if ((double) TGZs / couples > 0.5)
				{
					output.WriteLineAsync($"   Song ID: {songID} has {couples} couples where {TGZs} are in target zones: {Math.Min(100d, (double)TGZs / couples * 100):##.###} %");
				}
				//Debug print all songs
				System.Diagnostics.Debug.WriteLine($"   Song ID: {songID} has {couples} couples where {TGZs} are in target zones: {(double)TGZs / couples * 100:##.###} %");

			}
			Dictionary<uint, Dictionary<uint, List<uint>>> filteredSongs = new Dictionary<uint, Dictionary<uint, List<uint>>>();

			//remove songs that have low ratio of couples that make a TGZ
			// also remove songs that have low amount of samples common with recording
			foreach (var songID in res.Keys)
			{
				double ratio = (double)commonTGZAmount[songID] / commonCoupleAmount[songID];
				//remove song if less than half of samples is not in TGZ
				//remove songs that don't have enough samples in TGZ
				//		- min is 1720 * coef (for noise cancellation)
				//			- avg 2. samples per bin common (2 out of 6) with about 860 bins per 10 (1000/11.7) seconds = 1720

				if ((commonTGZAmount[songID] < 1400 || ratio < Parameters.SamplesInTgzCoef) && //normal songs have a lot of samples in TGZ with not that high ratio (thus wont get deleted)
					ratio < 0.8d) //or test sounds (Hertz.wav or 400Hz.wav) dont have many samples in TGZ but have high ratio
				{
					//res.Remove(songID);
				}
				else
				{
					filteredSongs.Add(songID, res[songID]);
				}
			}
			return filteredSongs;
		}

		/// <summary>
		/// Creates Address to Absolute time anchor dictionary out of TFPs
		/// </summary>
		/// <param name="timeFrequencyPoints"></param>
		/// <returns></returns>
		private static Dictionary<uint, List<uint>> CreateRecordAddresses(List<TimeFrequencyPoint> timeFrequencyPoints)
		{
			Dictionary<uint, List<uint>> res = new Dictionary<uint, List<uint>>();


			// -targetZoneSize: because of end limit 
			// -1: because of anchor point at -2 position target zone
			int stopIdx = timeFrequencyPoints.Count - Parameters.TargetZoneSize - Parameters.AnchorOffset;
			for (int i = 0; i < stopIdx; i++)
			{
				//anchor is at idx i
				//1st in TZ is at idx i+3
				//5th in TZ is at idx i+7

				uint anchorFreq = timeFrequencyPoints[i].Frequency;
				uint anchorTime = timeFrequencyPoints[i].Time;
				for (int pointNum = 3; pointNum < Parameters.TargetZoneSize + 3; pointNum++)
				{
					uint pointFreq = timeFrequencyPoints[i + pointNum].Frequency;
					uint pointTime = timeFrequencyPoints[i + pointNum].Time;

					uint address = Tools.Builders.BuildAddress(anchorFreq, pointFreq, pointTime - anchorTime);

					if (!res.ContainsKey(address)) //create new instance if it doesnt exist
						res.Add(address, new List<uint>());
					res[address].Add(anchorTime); //add Anchor time to the list
				}
			}
			return res;
		}

	}
}
