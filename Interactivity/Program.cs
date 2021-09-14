using System;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;
using DisCatSharp.EventArgs;
using DisCatSharp.Examples.Interactivity.Commands;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.VoiceNext;

using Microsoft.Extensions.Logging;

namespace DisCatSharp.Examples.Interactivity
{
    class Program
    {
        static async Task Main(string[] args)
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

            DiscordClient discordClient = new(discordConfiguration);
            
            // Enabling VoiceNext for . You can also use lavalink.
            discordClient.UseVoiceNext();
            
            Console.WriteLine("Connecting to Discord...");
            await discordClient.ConnectAsync();

            // Enabling interactivity support
            discordClient.UseInteractivity();

            // Event handler registration for 'random' command.
            discordClient.ComponentInteractionCreated += ComponentInteraction;
            
            // Use the default logger provided for easy reading
            discordClient.Logger.LogInformation($"Connection success! Logged in as {discordClient.CurrentUser.Username}#{discordClient.CurrentUser.Discriminator} ({discordClient.CurrentUser.Id})");

            // Let the user know that we're registering the commands.
            discordClient.Logger.LogInformation("Registering application commands...");

            var appCommandExt = discordClient.UseApplicationCommands();

            // Register event handlers
            appCommandExt.SlashCommandExecuted += Slash_SlashCommandExecuted;
            appCommandExt.SlashCommandErrored += Slash_SlashCommandErrored;
            
            appCommandExt.RegisterCommands<InteractivityCommands>();
            appCommandExt.RegisterCommands<ThreadCommands>();
            appCommandExt.RegisterCommands<StageCommands>();
            
            discordClient.Logger.LogInformation("Application commands registered successfully");

            // Listen for commands by putting this method to sleep and relying off of DiscordClient's event listeners
            await Task.Delay(-1);
        }
        
        private static Task Slash_SlashCommandExecuted(ApplicationCommandsExtension sender, SlashCommandExecutedEventArgs e)
        {
            Console.WriteLine($"Slash/Info: {e.Context.CommandName}");
            return Task.CompletedTask;
        }

        private static Task Slash_SlashCommandErrored(ApplicationCommandsExtension sender, SlashCommandErrorEventArgs e)
        {
            Console.WriteLine($"Slash/Error: {e.Exception.Message} | CN: {e.Context.CommandName} | IID: {e.Context.InteractionId}");
            return Task.CompletedTask;
        }

        private static async Task ComponentInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            // You can create messages with buttons or select menus that will always work, even after the commands are completed.
            // This example for the 'random' command.
            
            if (e.Id == "rand_next")
                await e.Message.ModifyAsync(new Random().Next(0, 100).ToString());
            if (e.Id == "rand_cancel")
                await e.Message.DeleteAsync();
        }
    }
}