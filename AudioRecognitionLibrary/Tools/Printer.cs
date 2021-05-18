using System;
using System.Collections.Generic;
using System.Text;

namespace AudioRecognitionLibrary.Tools
{
	/// <summary>
	/// Helper class used for debugging.
	/// </summary>
	public class Printer
	{
		/// <summary>
		/// Prints maximum of first ten bytes to debug output window.
		/// </summary>
		/// <param name="data"></param>
		public static void Print(byte[] data)
		{
			int limit = Math.Min(10, data.Length);
			for (int i = 0; i < limit; i++)
			{
				System.Diagnostics.Debug.WriteLine((char)data[i]);
			}
		}

		/// <summary>
		/// Prints maximum of first 50 shorts as bytes to debug output window.
		/// </summary>
		/// <param name="data"></param>
		public static void PrintShortAsBytes(short[] data)
		{
			int limit = Math.Min(50, data.Length);
			for (int i = 0; i < limit; i++)
			{
				byte first = (byte)(data[i] << 4);
				byte second = (byte)data[i];
				System.Diagnostics.Debug.Write(BitConverter.ToString(new[] { first }) + " ");
				System.Diagnostics.Debug.Write(BitConverter.ToString(new[] { second }) + " ");
			}
		}
	}
}
