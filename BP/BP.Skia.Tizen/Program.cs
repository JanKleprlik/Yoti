using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace BP.Skia.Tizen
{
	class Program
	{
		static void Main(string[] args)
		{
			var host = new TizenHost(() => new BP.App(), args);
			host.Run();
		}
	}
}
