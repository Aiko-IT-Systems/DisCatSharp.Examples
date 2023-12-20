using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

using System.Threading.Tasks;

using static DisCatSharp.Examples.Basics.Main.Bot;

namespace DisCatSharp.Examples.Basics.Commands;

/// <summary>
/// The main command module.
/// </summary>
internal class Main : BaseCommandModule
{
	/// <summary>
	/// Pings you.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[Command("ping"), Description("Test ping :3")]
	public async Task PingAsync(CommandContext ctx)
	{
		await ctx.RespondAsync($"{ctx.User.Mention}, Pong! :3 miau!");
		await ctx.Message.DeleteAsync("Command Hide");
	}

	/// <summary>
	/// Shutdowns the bot.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[Command("shutdown"), Description("Shuts the bot down safely."), RequireOwner]
	public async Task ShutdownAsync(CommandContext ctx)
	{
		ShutdownRequest.Cancel();
		await ctx.RespondAsync("Shutting down");
		await ctx.Message.DeleteAsync("Command Hide");
	}

	/// <summary>
	/// Repeats what you say.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	/// <param name="msg">The message to repeat.</param>
	[Command("say"), Description("Says what you wrote.")]
	public async Task SayAsync(CommandContext ctx, [RemainingText] string msg)
	{
		await ctx.RespondAsync($"You said: {msg}");
		await ctx.Message.DeleteAsync("Command Hide");
	}
}
