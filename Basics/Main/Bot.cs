using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;
using DisCatSharp.CommandsNext;
using DisCatSharp.Enums;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;

using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace DisCatSharp.Examples.Basics.Main;

/// <summary>
/// The bot.
/// </summary>
internal class Bot : IDisposable
{
#if DEBUG
	public static readonly string Prefix = "!";
#else
	public static readonly string Prefix = "%";
#endif
	//public static ulong devguild = ; //Set to register app command on guild

	public static CancellationTokenSource ShutdownRequest;
	public static DiscordClient Client;
	public static ApplicationCommandsExtension AppCommands;

	private InteractivityExtension _next;

	private CommandsNextExtension _cNext;

	/// <summary>
	/// Initializes a new instance of the <see cref="Bot"/> class.
	/// </summary>
	/// <param name="token">The token.</param>
	public Bot(string token)
	{
		ShutdownRequest = new();

		LogLevel logLevel;
#if DEBUG
		logLevel = LogLevel.Debug;
#else
		logLevel = LogLevel.Error;
#endif
		var cfg = new DiscordConfiguration
		{
			Token = token,
			TokenType = TokenType.Bot,
			AutoReconnect = true,
			MinimumLogLevel = logLevel,
			Intents = DiscordIntents.AllUnprivileged,
			MessageCacheSize = 2048
		};

		Client = new(cfg);

		Client.UseApplicationCommands();

		this._cNext = Client.UseCommandsNext(new()
		{
			StringPrefixes = [Prefix],
			CaseSensitive = true,
			EnableMentionPrefix = true,
			IgnoreExtraArguments = true,
			DefaultHelpChecks = null,
			EnableDefaultHelp = true,
			EnableDms = true
		});

		AppCommands = Client.GetApplicationCommands();

		this._next = Client.UseInteractivity(new()
		{
			PaginationBehaviour = PaginationBehaviour.WrapAround,
			PaginationDeletion = PaginationDeletion.DeleteMessage,
			PollBehaviour = PollBehaviour.DeleteEmojis,
			ButtonBehavior = ButtonPaginationBehavior.Disable
		});

		this.RegisterEventListener(Client, AppCommands, this._cNext);
		this.RegisterCommands(this._cNext, AppCommands);
	}

	/// <summary>
	/// Disposes the Bot.
	/// </summary>
	public void Dispose()
	{
		Client.Dispose();
		this._next = null;
		this._cNext = null;
		Client = null;
		AppCommands = null;
		Environment.Exit(0);
	}

	/// <summary>
	/// Starts the Bot.
	/// </summary>
	public async Task RunAsync()
	{
		await Client.ConnectAsync();
		while (!ShutdownRequest.IsCancellationRequested)
			await Task.Delay(2000);

		await Client.UpdateStatusAsync(null, UserStatus.Offline, null);
		await Client.DisconnectAsync();
		await Task.Delay(2500);
		this.Dispose();
	}

	/// <summary>
	/// Registers the event listener.
	/// </summary>
	/// <param name="client">The client.</param>
	/// <param name="cnext">The commandsnext extension.</param>
	private void RegisterEventListener(DiscordClient client, ApplicationCommandsExtension appCommands, CommandsNextExtension cnext)
	{
		/* Client Basic Events */
		client.SocketOpened += Client_SocketOpenedAsync;
		client.SocketClosed += Client_SocketClosedAsync;
		client.SocketErrored += Client_SocketErroredAsync;
		client.Heartbeated += Client_HeartbeatedAsync;
		client.Ready += Client_ReadyAsync;
		client.Resumed += Client_ResumedAsync;

		/* Client Events */
		//client.GuildUnavailable += Client_GuildUnavailable;
		//client.GuildAvailable += Client_GuildAvailable;

		/* CommandsNext Error */
		cnext.CommandErrored += CNext_CommandErroredAsync;

		/* Slash Infos */
		client.ApplicationCommandCreated += Discord_ApplicationCommandCreatedAsync;
		client.ApplicationCommandDeleted += Discord_ApplicationCommandDeletedAsync;
		client.ApplicationCommandUpdated += Discord_ApplicationCommandUpdatedAsync;
		appCommands.SlashCommandErrored += Slash_SlashCommandErroredAsync;
		appCommands.SlashCommandExecuted += Slash_SlashCommandExecutedAsync;
	}

	/// <summary>
	/// Registers the commands.
	/// </summary>
	/// <param name="cnext">The commandsnext extension.</param>
	/// <param name="appCommands">The appcommands extension.</param>
	private void RegisterCommands(CommandsNextExtension cnext, ApplicationCommandsExtension appCommands)
	{
		cnext.RegisterCommands<Commands.Main>(); // Commands.Main = Ordner.Class
		// appCommands.RegisterCommands<AppCommands.Main>(devguild); // use to register on guild
		appCommands.RegisterGlobalCommands<AppCommands.Main>(); // use to register global (can take up to an hour)
	}

	private static Task Client_ReadyAsync(DiscordClient dcl, ReadyEventArgs e)
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"Starting with Prefix {Prefix} :3");
		Console.WriteLine($"Starting {Client.CurrentUser.Username}");
		Console.WriteLine("Client ready!");
		Console.WriteLine($"Shard {dcl.ShardId}");
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("Loading Commands...");
		Console.ForegroundColor = ConsoleColor.Magenta;
		var commandlist = dcl.GetCommandsNext().RegisteredCommands;
		foreach (var command in commandlist)
			Console.WriteLine($"Command {command.Value.Name} loaded.");

		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("Bot ready!");
		return Task.CompletedTask;
	}

	private static Task Client_ResumedAsync(DiscordClient dcl, ReadyEventArgs e)
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("Bot resumed!");
		return Task.CompletedTask;
	}

	private static Task Discord_ApplicationCommandUpdatedAsync(DiscordClient sender, ApplicationCommandEventArgs e)
	{
		sender.Logger.LogInformation("Shard {SenderShardId} sent application command updated: {CommandName}: {CommandId} for {CommandApplicationId}", sender.ShardId, e.Command.Name, e.Command.Id, e.Command.ApplicationId);
		return Task.CompletedTask;
	}

	private static Task Discord_ApplicationCommandDeletedAsync(DiscordClient sender, ApplicationCommandEventArgs e)
	{
		sender.Logger.LogInformation("Shard {SenderShardId} sent application command deleted: {CommandName}: {CommandId} for {CommandApplicationId}", sender.ShardId, e.Command.Name, e.Command.Id, e.Command.ApplicationId);
		return Task.CompletedTask;
	}

	private static Task Discord_ApplicationCommandCreatedAsync(DiscordClient sender, ApplicationCommandEventArgs e)
	{
		sender.Logger.LogInformation("Shard {SenderShardId} sent application command created: {CommandName}: {CommandId} for {CommandApplicationId}", sender.ShardId, e.Command.Name, e.Command.Id, e.Command.ApplicationId);
		return Task.CompletedTask;
	}

	private static Task Slash_SlashCommandExecutedAsync(ApplicationCommandsExtension sender, SlashCommandExecutedEventArgs e)
	{
		sender.Client.Logger.LogInformation("Slash/Info: {ContextCommandName}", e.Context.CommandName);
		return Task.CompletedTask;
	}

	private static Task Slash_SlashCommandErroredAsync(ApplicationCommandsExtension sender, SlashCommandErrorEventArgs e)
	{
		sender.Client.Logger.LogError("Slash/Error: {ExceptionMessage} | CN: {ContextCommandName} | IID: {ContextInteractionId}", e.Exception.Message, e.Context.CommandName, e.Context.InteractionId);
		return Task.CompletedTask;
	}

	private static Task CNext_CommandErroredAsync(CommandsNextExtension sender, CommandErrorEventArgs e)
	{
		if (e.Command == null)
			sender.Client.Logger.LogInformation("{ExceptionMessage}", e.Exception.Message);
		else
			sender.Client.Logger.LogInformation("{CommandName}: {ExceptionMessage}", e.Command.Name, e.Exception.Message);

		return Task.CompletedTask;
	}

	private static Task Client_SocketOpenedAsync(DiscordClient dcl, SocketEventArgs e)
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("Socket opened");
		return Task.CompletedTask;
	}

	private static Task Client_SocketErroredAsync(DiscordClient dcl, SocketErrorEventArgs e)
	{
		Console.ForegroundColor = ConsoleColor.DarkRed;
		Console.WriteLine("Socket has an error! " + e.Exception.Message.ToString());
		return Task.CompletedTask;
	}

	private static Task Client_SocketClosedAsync(DiscordClient dcl, SocketCloseEventArgs e)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine("Socket closed: " + e.CloseMessage);
		return Task.CompletedTask;
	}

	private static Task Client_HeartbeatedAsync(DiscordClient dcl, HeartbeatEventArgs e)
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("Received Heartbeat:" + e.Ping);
		Console.ForegroundColor = ConsoleColor.Gray;
		return Task.CompletedTask;
	}
}
