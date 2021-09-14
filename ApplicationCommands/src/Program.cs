using System;
using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;
using DisCatSharp.Entities;
using DisCatSharp.Examples.ApplicationCommands.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DisCatSharp.Examples.ApplicationCommands
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
            ApplicationCommandsConfiguration appCommandsConfiguration = new()
            {
                Services = new ServiceCollection().AddSingleton<Random>().BuildServiceProvider()
            };

            // Let the user know that we're registering the commands.
            discordShardedClient.Logger.LogInformation("Registering application commands...");

            // If the guild ID is not provided, then register global commands.
            ulong? guildId = null;
            if (args.Length > 1)
            {
                guildId = ulong.Parse(args[1]);
            }

            Type appCommandModule = typeof(ApplicationCommandsModule);
            foreach (DiscordClient discordClient in discordShardedClient.ShardClients.Values)
            {
                ApplicationCommandsExtension appCommandShardExtension = discordClient.UseApplicationCommands(appCommandsConfiguration);

                // Register event handlers
                appCommandShardExtension.SlashCommandExecuted += Slash_SlashCommandExecuted;
                appCommandShardExtension.SlashCommandErrored += Slash_SlashCommandErrored;
                appCommandShardExtension.ContextMenuExecuted += Context_ContextMenuCommandExecuted;
                appCommandShardExtension.ContextMenuErrored += Context_ContextMenuCommandErrored;
                
                appCommandShardExtension.RegisterCommands<Ping>(guildId);
                appCommandShardExtension.RegisterCommands<RoleInfo>(guildId);
                appCommandShardExtension.RegisterCommands<RollRandom>(guildId);
                appCommandShardExtension.RegisterCommands<Tags>(guildId);
                appCommandShardExtension.RegisterCommands<Tell>(guildId);
                appCommandShardExtension.RegisterCommands<TriggerHelp>(guildId);

                // Currently, adding permissions to global commands during registration is not implemented
                if (guildId != null)
                {
                    appCommandShardExtension.RegisterCommands<Slap>((ulong) guildId, context =>
                    {
                        // Allow members with the specified role from the arguments to execute the command
                        if (args.Length > 2)
                            context.AddRole(ulong.Parse(args[2]), true);
                        
                        // Allow owners of the bot to execute the command
                        foreach (DiscordUser user in discordClient.CurrentApplication.Owners)
                        {
                            context.AddUser(user.Id, true);
                        }
                    });
                    appCommandShardExtension.RegisterCommands<ManagePermissions>((ulong) guildId, context =>
                    {
                        // Allow owners of the bot to execute the command
                        foreach (DiscordUser user in discordClient.CurrentApplication.Owners)
                        {
                            context.AddUser(user.Id, true);
                        }
                    });
                }

                // Context menu commands
                appCommandShardExtension.RegisterCommands<MessageCopy>();
                appCommandShardExtension.RegisterCommands<UserInfo>();
            }
            
            discordShardedClient.Logger.LogInformation("Application commands registered successfully");
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
        
        private static Task Context_ContextMenuCommandExecuted(ApplicationCommandsExtension sender, ContextMenuExecutedEventArgs e)
        {
            Console.WriteLine($"Context/Info: {e.Context.CommandName}");
            return Task.CompletedTask;
        }

        private static Task Context_ContextMenuCommandErrored(ApplicationCommandsExtension sender, ContextMenuErrorEventArgs e)
        {
            Console.WriteLine($"Context/Error: {e.Exception.Message} | CN: {e.Context.CommandName} | IID: {e.Context.InteractionId}");
            return Task.CompletedTask;
        }
    }
}
