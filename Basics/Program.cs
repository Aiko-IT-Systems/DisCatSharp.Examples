using System;
using System.Threading.Tasks;

using DisCatSharp.Examples.Basics.Main;

namespace DisCatSharp.Examples.Basics;

/// <summary>
///     The program.
/// </summary>
internal class Program
{
	/// <summary>
	///     Entry point.
	/// </summary>
	/// <param name="args">The args.</param>
	private static async Task Main(string[] args)
	{
		var token = args.Length > 0
			? args[0]
			: Environment.GetEnvironmentVariable("DISCATSHARP_TOKEN") ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");

		if (string.IsNullOrWhiteSpace(token))
		{
			Console.Error.WriteLine("Provide a bot token as the first argument or set DISCATSHARP_TOKEN / DISCORD_TOKEN.");
			return;
		}

		using var bot = new Bot(token);
		await bot.RunAsync();
	}
}
