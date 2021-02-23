using System;
using System.Collections.Generic;
using System.Text;

namespace AudioProcessing.Tools
{
	public static class Printer
	{
		public static void Print(byte[] data)
		{
			int limit = Math.Min(20, data.Length);
			for (int i = 0; i < limit; i++)
			{
				System.Diagnostics.Debug.WriteLine(data[i]);
			}
		}

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
