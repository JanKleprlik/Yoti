using System;
using Windows.UI.Xaml;

namespace BP.Wasm
{
	public class Program
	{
		private static App _app;

		static int Main(string[] args)
		{
			SQLitePCL.Batteries.Init();
			Windows.UI.Xaml.Application.Start(_ => _app = new App());
			return 0;
		}
	}
}
