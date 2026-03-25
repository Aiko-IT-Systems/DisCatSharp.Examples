using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

using static DisCatSharp.Examples.Basics.Main.Bot;

namespace DisCatSharp.Examples.Basics.Commands;

/// <summary>
///     The main command module.
/// </summary>
internal class Main : BaseCommandModule
{
	private static DiscordContainerComponent CreateStatusCard(string title, IEnumerable<string> lines, string footer)
	{
		var body = string.Join(Environment.NewLine, lines.Select(static line => $"- {line}"));

		return new DiscordContainerComponent(accentColor: new DiscordColor("#5865F2"))
			.AddComponent(new DiscordTextDisplayComponent($"## {title}"))
			.AddComponent(new DiscordTextDisplayComponent(body))
			.AddComponent(new DiscordTextDisplayComponent($"> {footer}"));
	}

	/// <summary>
	///     Pings you.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[Command("ping"), Description("Test ping :3")]
	public async Task PingAsync(CommandContext ctx)
	{
		var stopwatch = Stopwatch.StartNew();
		var response = await ctx.RespondAsync("Checking bot health...");
		stopwatch.Stop();

		// CHALLENGE: Reuse this helper in another text command and decide what should stay shared versus command-specific.
		var card = CreateStatusCard("Bot health snapshot",
		[
			$"{ctx.User.Mention}, Pong! :3 miau!",
			$"Gateway latency: `{ctx.Client.Ping}ms`",
			$"Command turnaround: `{stopwatch.ElapsedMilliseconds}ms`",
			$"Channel: {ctx.Channel.Mention}"
		], "Compare this text-command flow with the slash-command ping sample.");

		await response.ModifyAsync(new DiscordMessageBuilder().WithV2Components().AddComponents(card));
		await ctx.Message.DeleteAsync("Command Hide");
	}

	/// <summary>
	///     Shutdowns the bot.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[Command("shutdown"), Description("Shuts the bot down safely."), RequireOwner]
	public async Task ShutdownAsync(CommandContext ctx)
	{
		await ShutdownRequest.CancelAsync();
		await ctx.RespondAsync("Shutting down");
		await ctx.Message.DeleteAsync("Command Hide");
	}

	/// <summary>
	///     Repeats what you say.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	/// <param name="msg">The message to repeat.</param>
	[Command("say"), Description("Says what you wrote.")]
	public async Task SayAsync(CommandContext ctx, [RemainingText] string msg)
	{
		// CHALLENGE: Offer a button-driven follow-up that pins, quotes, or resends the captured message.
		var card = CreateStatusCard("Repeat message",
		[
			msg,
			$"Requested by: {ctx.User.Mention}"
		], "This is intentionally simple so you can layer interactions onto it later.");

		await ctx.RespondAsync(new DiscordMessageBuilder().WithV2Components().AddComponents(card));
		await ctx.Message.DeleteAsync("Command Hide");
	}

	/// <summary>
	///     Schedules a short reminder and posts it back to the channel.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	/// <param name="seconds">How long to wait before sending the reminder.</param>
	/// <param name="reminder">The reminder text.</param>
	[Command("remind"), Aliases("remindme"), Description("Schedules a reminder to demonstrate async command flows.")]
	public async Task RemindAsync(CommandContext ctx, int seconds, [RemainingText] string reminder = "Check back on the task you started earlier.")
	{
		if (seconds is < 1 or > 900)
		{
			await ctx.RespondAsync("Pick a delay between 1 and 900 seconds.");
			return;
		}

		await ctx.RespondAsync($"Got it! I'll remind you in {seconds} seconds.");
		await ctx.Message.DeleteAsync("Command Hide");

		try
		{
			await Task.Delay(TimeSpan.FromSeconds(seconds), ShutdownRequest.Token);
		}
		catch (TaskCanceledException)
		{
			return;
		}

		// CHALLENGE: Add a snooze button once you're comfortable mixing delayed work with component interactions.
		var card = CreateStatusCard("Reminder ready",
		[
			$"For: {ctx.User.Mention}",
			$"Delay: `{seconds}` seconds",
			$"Note: {reminder}"
		], "This message is posted after the command has already completed.");

		await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithV2Components().AddComponents(card));
	}
}
