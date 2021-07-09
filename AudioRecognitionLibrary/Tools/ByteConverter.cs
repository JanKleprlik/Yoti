using System;
using System.Collections.Generic;
using System.Text;

namespace AudioRecognitionLibrary.Tools
{
	/// <summary>
	/// Primitive value converters.
	/// </summary>
	internal static partial class Converter
	{
		/// <summary>
		/// Converts an array of two bytes to one short.
		/// </summary>
		/// <param name="bytes">Must be of size 2!</param>
		/// <returns></returns>
		public static short BytesToShort(byte[] bytes)
		{
			if (bytes.Length != 2)
				throw new ArgumentException($"Exactly 2 bytes requiered, got {bytes.Length} bytes.");
			short res = bytes[1];
			res = (short)(res << 8);
			res += bytes[0];
			return res;
		}

		/// <summary>
		/// Converts an array of four bytes to one int.
		/// </summary>
		/// <param name="bytes">Must be of size 4!</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If bytes is not of size 4.</exception>
		public static int BytesToInt(byte[] bytes)
		{
			if (bytes.Length != 4)
				throw new ArgumentException($"Exactly 4 bytes requiered, got {bytes.Length} bytes.");
			int res = bytes[3];
			for (int i = 2; i >= 0; i--)
			{
				res = res << 8;
				res += bytes[i];
			}

			return res;
		}

		/// <summary>
		/// Converts an array of bytes to one uint.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static uint BytesToUInt(byte[] bytes)
		{
			uint res = bytes[bytes.Length - 1];
			for (int i = bytes.Length - 2; i >= 0; i--)
			{
				res = res << 8;
				res += bytes[i];
			}

			return res;
		}


		/// <summary>
		/// Transforms byte audio data into short audio data
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static short[] BytesToShorts(byte[] data)
		{
			int dataLength = data.Length %2 == 0 ? data.Length : data.Length-1;



			short[] dataShorts = new short[dataLength / 2];

			for (int i = 0; i < dataLength; i += 2)
			{
				dataShorts[i / 2] = Tools.Converter.BytesToShort(new byte[] { data[i], data[i + 1] });
			}

			return dataShorts;
		}
	}
}
