using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;
using DisCatSharp.Lavalink;
using DisCatSharp.Net;

using Microsoft.Extensions.Logging;

using Serilog;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DisCatSharp.Examples.Lavalink;

/// <summary>
/// The program.
/// </summary>
internal class Program
{
	/// <summary>
	/// Entry point. Initializes the bot.
	/// </summary>
	/// <param name="args">The args.</param>
	private static async Task Main(string[] args)
	{
		// Logging! Let the user know that the bot started!
		Console.WriteLine("Starting bot...");

		// Create logger
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Information()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();

		// CHALLENGE: Try making sure the token is provided! Hint: A Try/Catch block may be needed!
		DiscordConfiguration discordConfiguration = new()
		{
			// The token is recieved from the command line arguments (bad practice in production!)
			// Example: dotnet run <someBotTokenHere>
			// CHALLENGE: Make it read from a file, optionally from a json file using System.Text.Json
			// CHALLENGE #2: Try retriving the token from environment variables
			Token = args[0],
			LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger)
		};

		DiscordClient discordClient = new(discordConfiguration);

		var host = args[1].Split(':');

		var endpoint = new ConnectionEndpoint
		{
			Hostname = host[0], // Lavalink server ip.
			Port = int.Parse(host[1]) // Lavalink server port
		};

		var lavalinkConfig = new LavalinkConfiguration
		{
			Password = args[2], // Lavalink server password.
			RestEndpoint = endpoint,
			SocketEndpoint = endpoint
		};

		var lavalink = discordClient.UseLavalink();

		// In order not to list all the commands when adding, you can create a list of all commands with this.
		var appCommandModule = typeof(ApplicationCommandsModule);
		var commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => appCommandModule.IsAssignableFrom(t) && !t.IsNested).ToList();

		var appCommandExt = discordClient.UseApplicationCommands();

		// Register event handlers
		appCommandExt.SlashCommandExecuted += Slash_SlashCommandExecutedAsync;
		appCommandExt.SlashCommandErrored += Slash_SlashCommandErroredAsync;

		foreach (var command in commands)
			appCommandExt.RegisterGlobalCommands(command);

		// Connect AFTER registering application commands
		Console.WriteLine("Connecting to Discord...");
		await discordClient.ConnectAsync();

		// Use the default logger provided for easy reading
		discordClient.Logger.LogInformation("Connection success! Logged in as {CurrentUserUsername}#{CurrentUserDiscriminator} ({CurrentUserId})", discordClient.CurrentUser.Username, discordClient.CurrentUser.Discriminator, discordClient.CurrentUser.Id);

		// Lavalink
		discordClient.Logger.LogInformation($"Connecting to lavalink...");
		await lavalink.ConnectAsync(lavalinkConfig); // Make sure this is after discordClient.ConnectAsync()
		discordClient.Logger.LogInformation($"Successful connection with lavalink!");

		// Listen for commands by putting this method to sleep and relying off of DiscordClient's event listeners
		await Task.Delay(-1);
	}

	/// <summary>
	/// Fires when the user uses the slash command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Slash_SlashCommandExecutedAsync(ApplicationCommandsExtension sender, SlashCommandExecutedEventArgs e)
	{
		sender.Client.Logger.LogInformation("Slash: {ContextCommandName}", e.Context.CommandName);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Fires when an exception is thrown in the slash command.
	/// </summary>
	/// <param name="sender">Application commands ext.</param>
	/// <param name="e">Event arguments.</param>
	private static Task Slash_SlashCommandErroredAsync(ApplicationCommandsExtension sender, SlashCommandErrorEventArgs e)
	{
		sender.Client.Logger.LogError("Slash: {ExceptionMessage} | CN: {ContextCommandName} | IID: {ContextInteractionId}", e.Exception.Message, e.Context.CommandName, e.Context.InteractionId);
		return Task.CompletedTask;
	}
}
