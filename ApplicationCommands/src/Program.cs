using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;

namespace DisCatSharp.Examples.ApplicationCommands;

// TODO: rewrite, this is kinda weird
/// <summary>
///     The program.
/// </summary>
public class Program
{
	/// <summary>
	///     Entry point.
	/// </summary>
	/// <param name="args">The args.</param>
	public static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

	/// <summary>
	///     Asynchronous method in which the bot is initialized.
	/// </summary>
	/// <param name="args">The args.</param>
	public static async Task MainAsync(string[] args)
	{
		Console.WriteLine("Starting bot...");

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
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

		DiscordShardedClient discordShardedClient = new(discordConfiguration);

		ApplicationCommandsConfiguration appCommandsConfiguration = new(new ServiceCollection()
			.AddSingleton(Random.Shared)
			.AddSingleton<TagStore>()
			.BuildServiceProvider())
		{
			DebugStartup = true
		};

		discordShardedClient.Logger.LogInformation("Registering application commands...");

		var guildId = ResolveGuildId(args);

		foreach (var discordClient in discordShardedClient.ShardClients.Values)
		{
			var appCommandShardExtension = discordClient.UseApplicationCommands(appCommandsConfiguration);

			appCommandShardExtension.SlashCommandExecuted += Slash_SlashCommandExecutedAsync;
			appCommandShardExtension.SlashCommandErrored += Slash_SlashCommandErroredAsync;
			appCommandShardExtension.ContextMenuExecuted += Context_ContextMenuCommandExecutedAsync;
			appCommandShardExtension.ContextMenuErrored += Context_ContextMenuCommandErroredAsync;

			if (guildId != null)
				appCommandShardExtension.RegisterGuildCommands(Assembly.GetExecutingAssembly(), (ulong)guildId);
			else
				appCommandShardExtension.RegisterGlobalCommands(Assembly.GetExecutingAssembly());
		}

		discordShardedClient.Logger.LogInformation("Application commands registered successfully");

		using var shutdown = new CancellationTokenSource();
		Console.CancelKeyPress += (_, eventArgs) =>
		{
			eventArgs.Cancel = true;
			shutdown.Cancel();
		};

		Log.Logger.Information("Connecting to Discord...");
		await discordShardedClient.StartAsync();

		discordShardedClient.Logger.LogInformation("Connection success! Logged in as {UsernameWithDiscriminator} ({CurrentUserId})", discordShardedClient.CurrentUser.UsernameWithDiscriminator, discordShardedClient.CurrentUser.Id);

		try
		{
			await Task.Delay(Timeout.InfiniteTimeSpan, shutdown.Token);
		}
		catch (TaskCanceledException)
		{ }
		finally
		{
			await discordShardedClient.StopAsync();
			Log.CloseAndFlush();
		}
	}

	private static ulong? ResolveGuildId(string[] args)
		=> args.Length > 1 && ulong.TryParse(args[1], out var guildId) ? guildId : null;

	private static string ResolveToken(string[] args)
		=> args.FirstOrDefault(static argument => !string.IsNullOrWhiteSpace(argument))
			?? Environment.GetEnvironmentVariable("DISCATSHARP_TOKEN")
			?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");

	/// <summary>
	///     Fires when the user uses the slash command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Slash_SlashCommandExecutedAsync(ApplicationCommandsExtension sender, SlashCommandExecutedEventArgs e)
	{
		Log.Logger.Information("Slash: {ContextCommandName}", e.Context.CommandName);
		return Task.CompletedTask;
	}

	/// <summary>
	///     Fires when an exception is thrown in the slash command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Slash_SlashCommandErroredAsync(ApplicationCommandsExtension sender, SlashCommandErrorEventArgs e)
	{
		Log.Logger.Error("Slash: {ExceptionMessage} | CN: {ContextCommandName} | IID: {ContextInteractionId}", e.Exception.Message, e.Context.CommandName, e.Context.InteractionId);
		return Task.CompletedTask;
	}

	/// <summary>
	///     Fires when the user uses the context menu command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Context_ContextMenuCommandExecutedAsync(ApplicationCommandsExtension sender, ContextMenuExecutedEventArgs e)
	{
		Log.Logger.Information("Context: {ContextCommandName}", e.Context.CommandName);
		return Task.CompletedTask;
	}

	/// <summary>
	///     Fires when an exception is thrown in the context menu command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Context_ContextMenuCommandErroredAsync(ApplicationCommandsExtension sender, ContextMenuErrorEventArgs e)
	{
		Log.Logger.Error("Context: {ExceptionMessage} | CN: {ContextCommandName} | IID: {ContextInteractionId}", e.Exception.Message, e.Context.CommandName, e.Context.InteractionId);
		return Task.CompletedTask;
	}
}
