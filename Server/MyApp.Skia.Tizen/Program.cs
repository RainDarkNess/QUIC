using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace MyApp.Skia.Tizen
{
	public sealed class Program
	{
		static void Main(string[] args)
		{
			var host = new TizenHost(() => new MyApp.App());
			host.Run();
		}
	}
}
