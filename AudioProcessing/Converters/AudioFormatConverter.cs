using System;
using System.Collections.Generic;
using System.Text;

namespace AudioProcessing.Converters
{
	public class AudioConverter
	{
		public static void MP4toWAV(byte[] data)
		{
			System.Diagnostics.Debug.WriteLine("[DEBUG] In Converter...");
			int limit = Math.Min(20, data.Length);
			for (int i = 0; i < limit; i++)
			{
				System.Diagnostics.Debug.WriteLine(data[i]);
			}
		}
	}
}
