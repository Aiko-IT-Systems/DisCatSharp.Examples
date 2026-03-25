using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Voice;

using Microsoft.Extensions.Logging;

using Serilog;

namespace DisCatSharp.Examples.Interactivity;

/// <summary>
///     The program.
/// </summary>
internal class Program
{
	private static readonly Random Random = new();

	/// <summary>
	///     Entry point. Initializes the bot.
	/// </summary>
	/// <param name="args">The args.</param>
	private static async Task Main(string[] args)
	{
		Console.WriteLine("Starting bot...");

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Information()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();

		var token = ResolveToken(args);
		if (token == null)
		{
			Log.Logger.Error("Provide a bot token as the first argument or set DISCATSHARP_TOKEN / DISCORD_TOKEN.");
			return;
		}

		DiscordConfiguration discordConfiguration = new()
		{
			Token = token,
			LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger)
		};

		DiscordClient discordClient = new(discordConfiguration);

		discordClient.UseVoice();
		discordClient.UseInteractivity();
		discordClient.ComponentInteractionCreated += ComponentInteraction;
		discordClient.Logger.LogInformation("Registering application commands...");

		// In order not to list all the commands when adding, you can create a list of all commands with this.
		var appCommandModule = typeof(ApplicationCommandsModule);
		var commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => appCommandModule.IsAssignableFrom(t) && !t.IsNested).ToList();

		var appCommandExt = discordClient.UseApplicationCommands();

		// Register event handlers
		appCommandExt.SlashCommandExecuted += Slash_SlashCommandExecuted;
		appCommandExt.SlashCommandErrored += Slash_SlashCommandErrored;

		foreach (var command in commands)
			appCommandExt.RegisterGlobalCommands(command);

		discordClient.Logger.LogInformation("Application commands registered successfully");

		Console.WriteLine("Connecting to Discord...");
		await discordClient.ConnectAsync();
		discordClient.Logger.LogInformation("Connection success! Logged in as {UsernameWithDiscriminator} ({CurrentUserId})", discordClient.CurrentUser.UsernameWithDiscriminator, discordClient.CurrentUser.Id);

		using var shutdown = new CancellationTokenSource();
		Console.CancelKeyPress += (_, eventArgs) =>
		{
			eventArgs.Cancel = true;
			shutdown.Cancel();
		};

		try
		{
			await Task.Delay(Timeout.InfiniteTimeSpan, shutdown.Token);
		}
		catch (TaskCanceledException)
		{ }
		finally
		{
			await discordClient.DisconnectAsync();
			Log.CloseAndFlush();
		}
	}

	private static IReadOnlyList<DiscordButtonComponent> BuildRandomButtons(DiscordClient client, ulong ownerId)
		=>
		[
			new(ButtonStyle.Danger, $"rand_cancel:{ownerId}", "Cancel", false, new(DiscordEmoji.FromName(client, ":stop_button:"))),
			new(ButtonStyle.Success, $"rand_next:{ownerId}", "Next", false, new(DiscordEmoji.FromName(client, ":arrow_forward:")))
		];

	// CHALLENGE: Show reroll history or the current "winner" in this card so the panel teaches stateful UI, not just button clicks.
	private static DiscordContainerComponent CreateRandomCard(int value, ulong ownerId)
		=> new DiscordContainerComponent(accentColor: new DiscordColor("#5865F2"))
			.AddComponent(new DiscordTextDisplayComponent("## Owner-scoped randomizer"))
			.AddComponent(new DiscordTextDisplayComponent($$"""
				- Current result: `{{value}}`
				- Flow owner: <@{{ownerId}}>
				- Use **Next** to reroll or **Cancel** to close the panel.
				"""))
			.AddComponent(new DiscordTextDisplayComponent("> Persisting state here makes the panel feel closer to a real moderation or game tool."));

	private static string ResolveToken(string[] args)
		=> args.Length > 0
			? args[0]
			: Environment.GetEnvironmentVariable("DISCATSHARP_TOKEN") ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");

	/// <summary>
	///     Fires when the user uses the slash command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Slash_SlashCommandExecuted(ApplicationCommandsExtension sender, SlashCommandExecutedEventArgs e)
	{
		Log.Logger.Information($"Slash: {e.Context.CommandName}");
		return Task.CompletedTask;
	}

	/// <summary>
	///     Fires when an exception is thrown in the slash command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Slash_SlashCommandErrored(ApplicationCommandsExtension sender, SlashCommandErrorEventArgs e)
	{
		Log.Logger.Error($"Slash: {e.Exception.Message} | CN: {e.Context.CommandName} | IID: {e.Context.InteractionId}");
		return Task.CompletedTask;
	}

	/// <summary>
	///     You can create messages with buttons or select menus that will always work, even after the commands are completed.
	///     This example for the 'random' command.
	/// </summary>
	/// <param name="sender">Discord client</param>
	/// <param name="e">Event arguments</param>
	private static async Task ComponentInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
	{
		var idParts = e.Id.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
		if (idParts.Length != 2 || !idParts[0].StartsWith("rand_", StringComparison.Ordinal) || !ulong.TryParse(idParts[1], out var ownerId))
			return;

		// CHALLENGE: Expand this guard so moderators or thread participants can optionally collaborate on the same panel.
		if (e.User.Id != ownerId)
		{
			await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
			{
				Content = "This button flow belongs to another user.",
				IsEphemeral = true
			});
			return;
		}

		switch (idParts[0])
		{
			case "rand_next":
				// CHALLENGE: Add a reroll counter and disable the panel after a maximum number of presses.
				await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
					.WithV2Components()
					.ClearComponents()
					.AddComponents(
						CreateRandomCard(Random.Next(0, 100), ownerId),
						new DiscordActionRowComponent(BuildRandomButtons(sender, ownerId)))
					);
				break;
			case "rand_cancel":
				// CHALLENGE: Replace this close action with a confirmation modal if you want to teach safer destructive actions.
				await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
					.WithV2Components()
					.ClearComponents()
					.AddComponents(new DiscordContainerComponent(accentColor: new DiscordColor("#ED4245"))
						.AddComponent(new DiscordTextDisplayComponent("## Randomizer closed"))
						.AddComponent(new DiscordTextDisplayComponent($$"""
							- Closed by: <@{{ownerId}}>
							- The panel is no longer interactive.
							"""))));
				break;
		}
	}
}
