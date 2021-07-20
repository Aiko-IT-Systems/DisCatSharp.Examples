using System;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DisCatSharp.Examples.Bots.SlashCommands
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

            DiscordShardedClient discordShardedClient = new(discordConfiguration);

            Console.WriteLine("Connecting to Discord...");
            await discordShardedClient.StartAsync();

            // Use the default logger provided for easy reading
            discordShardedClient.Logger.LogInformation($"Connection success! Logged in as {discordShardedClient.CurrentUser.Username}#{discordShardedClient.CurrentUser.Discriminator} ({discordShardedClient.CurrentUser.Id})");

            // Register a Random class instance now for use later over in RollRandom.cs
            SlashCommandsConfiguration slashCommandsConfiguration = new()
            {
                Services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider()
            };

            // Let the user know that we're registering the commands.
            discordShardedClient.Logger.LogInformation("Registering slash commands...");
            
            Type slashCommandModule = typeof(SlashCommandModule);
            foreach (DiscordClient discordClient in discordShardedClient.ShardClients.Values)
            {
                SlashCommandsExtension slashCommandShardExtension = discordClient.UseSlashCommands();
                slashCommandShardExtension.RegisterCommands<Commands.Ping>();
                slashCommandShardExtension.RegisterCommands<Commands.RoleInfo>();
                slashCommandShardExtension.RegisterCommands<Commands.RollRandom>();
                slashCommandShardExtension.RegisterCommands<Commands.Slap>();
                slashCommandShardExtension.RegisterCommands<Commands.Tags>();
                slashCommandShardExtension.RegisterCommands<Commands.Tell>();
                slashCommandShardExtension.RegisterCommands<Commands.TriggerHelp>();
            }

            // Listen for commands by putting this method to sleep and relying off of DiscordClient's event listeners
            await Task.Delay(-1);
        }
    }
}
