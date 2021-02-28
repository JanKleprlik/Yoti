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
	}
}
