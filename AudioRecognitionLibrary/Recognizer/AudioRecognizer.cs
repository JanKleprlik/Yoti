using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AudioRecognitionLibrary.AudioFormats;
using AudioRecognitionLibrary.Processor;

namespace AudioRecognitionLibrary.Recognizer
{
	public partial class AudioRecognizer
	{
		/// <summary>
		/// Information about recognition process is written to given TextWriter.
		/// </summary>
		/// <param name="textWriter">TextWriter to write recognition process info into.</param>
		public AudioRecognizer(TextWriter textWriter)
		{
			IsOutputSet = true;
			output = textWriter;
		}

		/// <summary>
		/// Information about recognition process is written to Console.Out.
		/// </summary>
		public AudioRecognizer()
		{
			IsOutputSet = false;
			output = Console.Out;
		}

		#region private properties
		/// <summary>
		/// TextWriter to write recognition process information into.
		/// </summary>
		private TextWriter output { get; set; } = null;
		/// <summary>
		/// Flag determining wether to use special TextWriter at specific methods or use output set at initialization.
		/// </summary>
		private bool IsOutputSet { get; set; } = false;
		#endregion

		#region public API

		/// <summary>
		/// Creates fingerprint of given audio and returns it together with number of valid notes. (needed to compute recognition accuracy). <br></br>
		/// WARNING: Converts audio into single channel.
		/// </summary>
		/// <param name="audio">Audio whos fingerprint we want.</param>
		/// <returns>Tuple where Item1 is fingerprint, Item2 is total number of notes. <br></br>fingerprint: [address; (absolute anchor times)]<br></br>Note: Address is the hash.</returns>
		public Tuple<Dictionary<uint, List<uint>>, int> GetAudioFingerprint(IAudioFormat audio)
		{
			AudioProcessor.ConvertToMono(audio);

			double[] data = Array.ConvertAll(audio.Data, item => (double)item);

			double[] downsampledData = AudioProcessor.DownSample(data, Parameters.DownSampleCoef, audio.SampleRate);

			int bufferSize = Parameters.WindowSize / Parameters.DownSampleCoef; //default: 4096/4 = 1024
			var timeFrequencyPoints = CreateTimeFrequencyPoints(bufferSize, downsampledData, sensitivity: 1);

			//[address;(AbsAnchorTimes)]
			Dictionary<uint, List<uint>> fingerprint = CreateRecordAddresses(timeFrequencyPoints);

			return new Tuple<Dictionary<uint, List<uint>>, int> (fingerprint, timeFrequencyPoints.Count);
		}

		/// <summary>
		/// Recognizes song in provided database of fingerprints.
		/// </summary>
		/// <param name="fingerprint">Fingerprint of audio to be recognized.</param>
		/// <param name="database">Database of fingerprints to find match at.</param>
		/// <param name="accuracy">Accuracy of final match.</param>
		/// <param name="TFPCount">Original count of TFP before fingerprint creation. Needed to compute accuracy.</param>
		/// <param name="textWriter">TextWriter to write information about recognition process into.</param>
		/// <returns>Id of recognized song.</returns>
		public uint? RecognizeSong(Dictionary<uint, List<uint>> fingerprint, Dictionary<uint, List<ulong>> database, out double accuracy, int TFPCount, TextWriter textWriter = null)
		{
			//set custom output if not set at initialization
			if (!IsOutputSet && textWriter != null)
				output = textWriter;

			uint? finalSongID = FindBestMatch(database, fingerprint, out accuracy, TFPCount);

			//unset custom output
			if (!IsOutputSet)
				output = null;

			return finalSongID;
		}

		/// <summary>
		/// Adds fingerprint into database.
		/// </summary>
		/// <param name="fingerprint">Fingerprint to be added.</param>
		/// <param name="songID">Id of song whose fingerprint is to be added.</param>
		/// <param name="database">Part of database to add fingerprint into.</param>
		public void AddFingerprintToDatabase(Dictionary<uint, List<uint>> fingerprint, in uint songID, Dictionary<uint, List<ulong>> database)
		{
			// Iterate all hashes in fingerprint and add them to database
			foreach(KeyValuePair<uint, List<uint>> hash in fingerprint)
			{
				// Create entry in database with given Address (hash.key) if it does not exist yet.
				if (!database.ContainsKey(hash.Key))
					database.Add(hash.Key, new List<ulong>());

				// For each absolute anchor time create songValue (32 bits anchorTime & 32 bits songID)
				// and add it to database.
				foreach(uint anchorTime in hash.Value)
				{
					ulong songValue = Tools.Builders.BuildSongValue(anchorTime, songID);
					database[hash.Key].Add(songValue);
				}
			}
		}

		/// <summary>
		/// Obtains BPM of given audio.
		/// WARNING: Converts audio into single chanel.
		/// </summary>
		/// <param name="song">Audio to get BPM of.</param>
		/// <param name="approximate">Flag wether to approximate BPM in intervals of size set at <see cref="AudioRecognizer.Parameters"/></param>
		/// <returns>BPM of the song.</returns>
		public int GetBPM(IAudioFormat song,bool approximate = false)
		{
			// Multiple audio channels do not improve BPM identification.
			AudioProcessor.ConvertToMono(song);

			// Using floats instead of doubles here because of filters from NAudio library
			float[] data = Array.ConvertAll(song.Data, item => (float)item);


			FilterBPMFrequencies(data, (float)song.SampleRate);

			EnergyPeak[] energyPeaks = GetEnergyPeaks(data, song.SampleRate);

			return GetMostProbableBPM(energyPeaks, (float)song.SampleRate, approximate);
		}

		#endregion

		#region BPM helpers
		/// <summary>
		/// Computes most frequent BPM from given EnergyPeaks.
		/// </summary>
		/// <param name="energyPeaks">Energy peaks in the original audio.</param>
		/// <param name="sampleRate">Original sample rate of the audio.</param>
		/// <param name="approximate">Flag wether to approximate BPM in intervals of size set at <see cref="AudioRecognizer.Parameters"/></param>
		/// <returns>Song's most probable BPM</returns>
		private int GetMostProbableBPM(EnergyPeak[] energyPeaks, float sampleRate,bool approximate)
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
					int BPM = GetPeakBPM(currentPeak, neighbourPeak, sampleRate, approximate);

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

		/// <summary>
		///  Computes most probable BPM of two given peaks.
		/// </summary>
		/// <param name="currentPeak">Peak from which we compute the BPM.</param>
		/// <param name="neighbourPeak">neighbour peak which we take into account when computing BPM.</param>
		/// <param name="sampleRate">Original sample rate of the audio.</param>
		/// <param name="approximate">Flag wether to approximate BPM in intervals of size set at <see cref="AudioRecognizer.Parameters"/></param>
		/// <returns>BPM determined by currentPeak</returns>
		private int GetPeakBPM(EnergyPeak currentPeak, EnergyPeak neighbourPeak, float sampleRate, bool approximate)
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
			if (!approximate)
				return Convert.ToInt32(potentialBPM);
			else
				return GetApproxBPM(potentialBPM);
		}

		/// <summary>
		/// Approximates BPM into intervals of size set at <see cref="AudioRecognizer.Parameters"/>.
		/// </summary>
		/// <param name="inputBPM">Original BPM</param>
		/// <returns>Approximated BPM.</returns>
		private int GetApproxBPM(float inputBPM)
		{
			//round inputBPM to closest multiple of 5
			int intervalizedBPM = Convert.ToInt32(inputBPM / Parameters.ApproximateIntervalSize) * Parameters.ApproximateIntervalSize;
			//return BPM in interval [BPMLowLimit, BPMHighLimit]
			return Math.Min(Math.Max(intervalizedBPM, Convert.ToInt32(Parameters.BPMLowLimit)), Convert.ToInt32(Parameters.BPMHighLimit));
		}

		/// <summary>
		/// Creates Energy Peaks form audio data.
		/// </summary>
		/// <param name="data">Audio data.</param>
		/// <param name="sampleRate">Original sample rate of the audio.</param>
		/// <returns></returns>
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
		/// <param name="data">Audio data.</param>
		/// <param name="start">Interval start.</param>
		/// <param name="steps">Size of the interval.</param>
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

		/// <summary>
		/// Filters out frequencies not necessary for BPM recognition.<br></br>
		/// Frequency limits are set in <see cref="AudioRecognizer.Parameters"/>.
		/// </summary>
		/// <param name="data">Audio data.</param>
		/// <param name="sampleRate">Original sample rate of the audio.</param>
		private void FilterBPMFrequencies(float[] data, float sampleRate)
		{

			BiQuadFilter lowpass = BiQuadFilter.LowPassFilter(sampleRate, Parameters.BPMHighFreq, 1f);
			BiQuadFilter highpass = BiQuadFilter.HighPassFilter(sampleRate, Parameters.BPMLowFreq, 1f);

			for (int i = 0; i < data.Length; i++)
			{
				data[i] = highpass.Transform(lowpass.Transform(data[i]));
			}

		}

		#endregion

		#region Song recognition helpers

		/// <summary>
		/// Finds song from database that corresponds the best to the recording represented as timeFrequencyPoints.
		/// </summary>
		/// <param name="database">Database to look for matches in.</param>
		/// <param name="timeFrequencyPoints">TFPs of recording</param>
		/// <param name="probability">Probability of correct match.</param>
		/// <returns></returns>
		private uint? FindBestMatch(Dictionary<uint, List<ulong>> database, Dictionary<uint, List<uint>> fingerprint, out double probability, int TFPCount)
		{
			// Get quantities of each SongValue to determine wether they make a complete TGZ (5+ points correspond to the same SongValue)
			var quantities = GetSongValQuantities(fingerprint, database);

			// Filter songs and addresses that only 
			var filteredSongs = FilterSongs(fingerprint, quantities, database);

			// Get longest delta for each song that says how many notes from recording are time coherent to the song
			//[sognID, delta]
			var deltas = GetDeltas(filteredSongs, fingerprint);

			// Pick the songID with highest delta occurance
			return MaximizeTimeCoherency(deltas, TFPCount, out probability);
		}

		/// <summary>
		/// Creates Time Frequency Points from raw audio data.
		/// </summary>
		/// <param name="bufferSize">Size of FFT buffer.</param>
		/// <param name="data">Complex values alternating Real and Imaginary values.</param>
		/// <param name="sensitivity">Sensitivity coeficient determining wether TFP is strong enought to be used or not.</param>
		/// <returns>List of Time Frequency Points.</returns>
		private static List<TimeFrequencyPoint> CreateTimeFrequencyPoints(int bufferSize, double[] data, double sensitivity = 0.9)
		{
			List<TimeFrequencyPoint> TimeFrequencyPoitns = new List<TimeFrequencyPoint>();
			double[] HammingWindow = FastFourierTransformation.GenerateHammingWindow((uint)bufferSize);
			double Avg = 0d;// = GetBinAverage(data, HammingWindow);

			int offset = 0;
			var sampleData = new double[bufferSize * 2]; //*2  because of Re + Im
			uint AbsTime = 0;
			while (offset < data.Length)
			{
				if (offset + bufferSize < data.Length)
				{
					for (int i = 0; i < bufferSize; i++) //setup for FFT
					{
						sampleData[i * 2] = data[i + offset] * HammingWindow[i];
						sampleData[i * 2 + 1] = 0d;
					}

					FastFourierTransformation.FFT(sampleData);
					double[] maxs =
					{
						GetStrongestBin(data, 0, 10),
						GetStrongestBin(data, 10, 20),
						GetStrongestBin(data, 20, 40),
						GetStrongestBin(data, 40, 80),
						GetStrongestBin(data, 80, 160),
						GetStrongestBin(data, 160, 512),
					};


					for (int i = 0; i < maxs.Length; i++)
					{
						Avg += maxs[i];
					}

					Avg /= maxs.Length;
					//get doubles of frequency and time 
					RegisterTFPoints(sampleData, Avg, AbsTime, ref TimeFrequencyPoitns, sensitivity);

				}

				offset += bufferSize;
				AbsTime++;
			}

			return TimeFrequencyPoitns;
		}

		/// <summary>
		/// Returns normalized value of the strongest bin in given bounds
		/// </summary>
		/// <param name="bins">Complex values alternating Real and Imaginary values.</param>
		/// <param name="from">Lower bound.</param>
		/// <param name="to">Upper bound.</param>
		/// <returns>Normalized value of the strongest bin</returns>
		private static double GetStrongestBin(double[] bins, int from, int to)
		{
			var max = double.MinValue;
			for (int i = from; i < to; i++)
			{
				var normalized = 2 * Math.Sqrt((bins[i * 2] * bins[i * 2] + bins[i * 2 + 1] * bins[i * 2 + 1]) / 2048);
				var decibel = 20 * Math.Log10(normalized);

				if (decibel > max)
				{
					max = decibel;
				}

			}

			return max;
		}

		/// <summary>
		/// Filter outs the strongest bins of logarithmically scaled parts of bins. Chooses the strongest and remembers it if its value is above average. Those points are
		/// chornologically added to the <c>timeFrequencyPoints</c> List.
		/// </summary>
		/// <param name="data">bins to choose from, alternating real and complex values as doubles. Must contain 512 complex values</param>
		/// <param name="average">Limit that separates weak spots from important ones.</param>
		/// <param name="absTime">Absolute time in the song.</param>
		/// <param name="timeFrequencyPoitns">List to add points to.</param>
		/// <param name="coefficient">Sensitivity coefficien determining wether TFP is strong enought to be registered.</param>
		private static void RegisterTFPoints(double[] data, in double average, in uint absTime, ref List<TimeFrequencyPoint> timeFrequencyPoitns, double coefficient = 0.9)
		{
			int[] BinBoundries =
			{
				//low   high
				0 , 10,
				10, 20,
				20, 40,
				40, 80,
				80, 160,
				160,512
			};

			//loop through logarithmically scalled sections of bins
			for (int i = 0; i < BinBoundries.Length / 2; i++)
			{
				//get strongest bin from a section if its above average
				var idx = GetStrongestBinIndex(data, BinBoundries[i * 2], BinBoundries[i * 2 + 1], average, coefficient);
				if (idx != null)
				{
					//idx is divided by 2 because of (Re + Im)
					timeFrequencyPoitns.Add(new TimeFrequencyPoint { Time = absTime, Frequency = (uint)idx / 2 });
				}
			}
		}

		/// <summary>
		/// Finds the strongest bin above limit in given segment.
		/// </summary>
		/// <param name="bins">Complex values alternating Real and Imaginary values</param>
		/// <param name="from">lower bound</param>
		/// <param name="to">upper bound</param>
		/// <param name="limit">limit indicating weak bin</param>
		/// <param name="sensitivity">sensitivity of the limit (the higher the lower sensitivity)</param>
		/// <returns>index of strongest bin or null if none of the bins is strong enought</returns>
		private static int? GetStrongestBinIndex(double[] bins, int from, int to, double limit, double sensitivity = 0.9d)
		{
			var max = double.MinValue;
			int? index = null;
			for (int i = from; i < to; i++)
			{
				var normalized = 2 * Math.Sqrt((bins[i * 2] * bins[i * 2] + bins[i * 2 + 1] * bins[i * 2 + 1]) / 2048);
				var decibel = 20 * Math.Log10(normalized);

				if (decibel > max && decibel * sensitivity > limit)
				{
					max = decibel;
					index = i * 2;
				}

			}

			return index;
		}

		/// <summary>
		/// Picks the song with the most notes setting the recording to the same offset int the original song.
		/// </summary>
		/// <param name="deltas">[songID, num of time coherent notes]</param>
		/// <param name="totalNotes">total number of notes in recording</param>
		/// <returns></returns>
		private uint? MaximizeTimeCoherency(Dictionary<uint, int> deltas, int totalNotes, out double probability)
		{
			uint? songID = null;
			int longestCoherency = 0;
			probability = 0d;
			foreach (var pair in deltas)
			{
				output.WriteLineAsync($"Song ID: {pair.Key} is {Math.Min(100d, (double)pair.Value / totalNotes * 100):##.#} % time coherent.");
				if (pair.Value > longestCoherency && pair.Value > Parameters.CoherentNotesCoef * totalNotes)
				{
					longestCoherency = pair.Value;
					probability = (double)longestCoherency / totalNotes * 100;
					songID = pair.Key;
				}
			}

			return songID;
		}

		/// <summary>
		/// Computes maximum number of time coherent notes for each song.
		/// </summary>
		/// <param name="filteredSongs">Filtered songs from database with their fingerprints.<br></br>[songId, [address, (absolute anchor time)]]<br></br>Note: address is eqv. to the hash.</param>
		/// <param name="recordAddresses">Recording addresses <br></br>[address, (absolute anchor time)]<br></br>Note: address is eqv. to the hash.</param>
		/// <returns>Dict of songs with their number of maxium coherent notes.<br></br>[songId, timeCoherentNotes]</returns>
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
				// Get number of notes that are coherent with the most deltas (each note has same delta from start of the song)
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
			// [songID, [address, (absSongAnchorTime)]]
			Dictionary<uint, Dictionary<uint, List<uint>>> res = new Dictionary<uint, Dictionary<uint, List<uint>>>();
			// [songID, common couple amount]
			Dictionary<uint, int> commonCoupleAmount = new Dictionary<uint, int>();
			// [songID, common couples in TGZ amount]
			Dictionary<uint, int> commonTGZAmount = new Dictionary<uint, int>();

			// Create datastructure for fast search in time coherency check
			// SongID -> Address -> absSongAnchorTime
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
		/// <param name="timeFrequencyPoints">Time Frequency points of the aduio.</param>
		/// <returns>[address (absolute anchor times)]</returns>
		private static Dictionary<uint, List<uint>> CreateRecordAddresses(List<TimeFrequencyPoint> timeFrequencyPoints)
		{
			Dictionary<uint, List<uint>> res = new Dictionary<uint, List<uint>>();
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

		#endregion
	}
}
