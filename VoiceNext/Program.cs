using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;
using DisCatSharp.Voice;

using Microsoft.Extensions.Logging;

using Serilog;

namespace DisCatSharp.Examples.VoiceNext;

/// <summary>
///     The program.
/// </summary>
internal class Program
{
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

		// Enable inbound audio so the recording commands can subscribe to VoiceReceived and write WAV files.
		discordClient.UseVoice(new VoiceConfiguration
		{
			EnableIncoming = true
		});

		// Let the user know that we're registering the commands.
		discordClient.Logger.LogInformation("Registering application commands...");

		// In order not to list all the commands when adding, you can create a list of all commands with this.
		var appCommandModule = typeof(ApplicationCommandsModule);
		var commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => appCommandModule.IsAssignableFrom(t) && !t.IsNested).ToList();

		var appCommandExt = discordClient.UseApplicationCommands();

		// Register event handlers
		appCommandExt.SlashCommandExecuted += Slash_SlashCommandExecutedAsync;
		appCommandExt.SlashCommandErrored += Slash_SlashCommandErroredAsync;

		foreach (var command in commands)
			appCommandExt.RegisterGuildCommands(command, 885510395295584289);

		discordClient.Logger.LogInformation("Application commands registered successfully");

		Console.WriteLine("Connecting to Discord...");
		await discordClient.ConnectAsync();

		// Use the default logger provided for easy reading
		discordClient.Logger.LogInformation("Connection success! Logged in as {CurrentUserUsername}#{CurrentUserDiscriminator} ({CurrentUserId})", discordClient.CurrentUser.Username, discordClient.CurrentUser.Discriminator, discordClient.CurrentUser.Id);

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

	private static string ResolveToken(string[] args)
	=> args.Length > 0
	? args[0]
	: Environment.GetEnvironmentVariable("DISCATSHARP_TOKEN") ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");

	/// <summary>
	///     Fires when the user uses the slash command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Slash_SlashCommandExecutedAsync(ApplicationCommandsExtension sender, SlashCommandExecutedEventArgs e)
	{
		sender.Client.Logger.LogInformation("Slash: {ContextCommandName}", e.Context.CommandName);
		return Task.CompletedTask;
	}

	/// <summary>
	///     Fires when an exception is thrown in the slash command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Slash_SlashCommandErroredAsync(ApplicationCommandsExtension sender, SlashCommandErrorEventArgs e)
	{
		sender.Client.Logger.LogError("Slash: {ExceptionMessage} | CN: {ContextCommandName} | IID: {ContextInteractionId}", e.Exception.Message, e.Context.CommandName, e.Context.InteractionId);
		return Task.CompletedTask;
	}
}
