using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace SlashCommands
{
    public class Program
    {
        public static void Main(string[] args) => MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();

        public static async Task MainAsync(string[] args)
        {
            // Logging! Let the user know that the bot started!
            Console.WriteLine("Starting bot...");

            // CHALLENGE: Try making sure the token is provided! Hint: A Try/Catch block may be needed!
            DiscordConfiguration discordConfiguration = new()
            {
                // The token is recieved from the command line arguments (bad practice in production!)
                // Example: dotnet run <someBotTokenHere>
                // CHALLENGE: Make it read from a file, optionally from a json file using System.Text.Json
                // CHALLENGE #2: Try retriving the token from environment variables
                Token = args[0]
            };

            // Slash commands do not support sharding at this time.
            DiscordClient discordClient = new(discordConfiguration);

            // Dependency injection does not currently work at this time, but if they did, this is how you would do it:
            //SlashCommandsConfiguration slashCommandsConfiguration = new()
            //{
            //    Services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider()
            //}

            SlashCommandsExtension slashCommandsExtension = discordClient.UseSlashCommands();

            // Let the user know that we're registering the commands.
            Console.WriteLine("Registering slash commands...");

            // Register commands manually, since they currently cannot be automatically picked from assembly like BaseCommandModule from CommandsNext
            // TODO: Remove 832354798153236510 (guild id) when you want commands to be global!
            //       It's recommended to register the command to a guild when testing, as the commands register much faster!
            // A simple 1 off command with no arguments
            slashCommandsExtension.RegisterCommands<SlashCommands.Commands.Ping>(832354798153236510);
            // Shows how to use arguments with slash commands
            slashCommandsExtension.RegisterCommands<SlashCommands.Commands.RoleInfo>(832354798153236510);
            // Shows how to use enums in commands
            slashCommandsExtension.RegisterCommands<SlashCommands.Commands.RollRandom>(832354798153236510);
            // Shows how to use Discord entities in commands
            slashCommandsExtension.RegisterCommands<SlashCommands.Commands.Slap>(832354798153236510);
            // A group command which contains more thorough examples
            slashCommandsExtension.RegisterCommands<SlashCommands.Commands.Tags>(832354798153236510);

            Console.WriteLine("Connecting to Discord...");
            await discordClient.ConnectAsync();
            // Use the default logger provided for easy reading
            discordClient.Logger.LogInformation($"Connection success! Logged in as {discordClient.CurrentUser.Username}#{discordClient.CurrentUser.Discriminator} ({discordClient.CurrentUser.Id})");
            // Listen for commands by putting this method to sleep and relying off of DiscordClient's event listeners
            await Task.Delay(-1);
        }
    }
}
