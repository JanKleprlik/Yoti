using System;
using System.Collections.Generic;
using System.Text;

namespace AudioProcessing.Tools
{
	public static class Arithmetics
	{
		public static short Average(short left, short right)
		{
			return (short)((left + right) / 2);
		}

		/// <summary>
		/// Swap two values
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="first">Value 1</param>
		/// <param name="second">Value 2</param>
		/// <returns>Value 2</returns>
		public static T Swap<T>(this T first, ref T second)
		{
			T tmp = second;
			second = first;
			return tmp;
		}

		/// <summary>
		/// Returns <c>_base</c> to the power of <c>exp</c>
		/// </summary>
		/// <param name="_base"></param>
		/// <param name="exp"></param>
		/// <returns></returns>
		public static int ToPowOf(this int _base, int exp)
		{
			int res = _base;
			for (int i = 0; i < exp; i++)
			{
				res *= _base;
			}

			return res;
		}

		/// <summary>
		/// Returns second power of <c>_base</c>
		/// </summary>
		/// <param name="_base"></param>
		/// <returns></returns>
		public static double Pow2(this double _base)
		{
			return _base * _base;
		}

		public static double GetComplexAbs(double real, double img)
		{
			return Math.Sqrt(real.Pow2() + img.Pow2());
		}

		public static bool IsPowOfTwo(int n)
		{
			if ((n & (n - 1)) != 0)
				return false;
			return true;
		}
	}
}
