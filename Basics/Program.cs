using DisCatSharp.Examples.Basics.Main;

namespace DisCatSharp.Examples.Basics;

/// <summary>
/// The program.
/// </summary>
internal class Program
{
	/// <summary>
	/// Entry point.
	/// </summary>
	/// <param name="args">The args.</param>
	private static void Main(string[] args)
	{
		var token = args[0];
		using var bot = new Bot(token);
		bot.RunAsync().Wait();
	}
}