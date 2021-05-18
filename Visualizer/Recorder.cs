using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AudioRecognitionLibrary.AudioFormats;

namespace Visualizer
{
	public static class Recorder
	{
		/// <summary>
		/// Creates concrete audio format from file.
		/// </summary>
		/// <param name="path">File Path.</param>
		/// <returns>Concrete implementation of IAudioFormat</returns>
		public static IAudioFormat GetAudio(string path)
		{
			byte[] data = File.ReadAllBytes(path);
			IAudioFormat Sound;

			// Currently only .wav files are supported.
			if (path.EndsWith(".wav"))
			{
				//Check if the beginning starts with RIFF (currently only supported format of wav files)
				if (!WavFormat.IsCorrectFormat(new[] { data[0], data[1], data[2], data[3] }))
				{
					throw new ArgumentException($"File {path} formatted wrongly, not a 'wav' format.");
				}

				Sound = new WavFormat(data);
			}
			else
			{
				throw new NotImplementedException($"Format of file {path} is not implemented.");
			}

			return Sound;
		}
	}
}
