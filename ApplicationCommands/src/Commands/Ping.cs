using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;

namespace DisCatSharp.Examples.ApplicationCommands.Commands
{
    // Notice how Ping inherits the ApplicationCommandsModule
    public class Ping : ApplicationCommandsModule
    {
        // Slash command registers the name and command description.
        [SlashCommand("ping", "Checks the latency between the bot and the Discord API. Best used to see if the bot is lagging.")]
        public static async Task Command(InteractionContext context)
        {
            // Create the response message
            DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();

            // Make the message contain the websocket latency between Discord and the bot.
            discordInteractionResponseBuilder.Content = $"Pong! Webhook latency is {context.Client.Ping}ms";

            // Uncomment this in order for it to be visibile to *just* the command executer.
            //discordInteractionResponseBuilder.IsEphemeral = true;

            // Send the message. InteractionResponseType.ChannelMessageWithSource means that the command executed within 3 seconds and has the results ready.
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);

            // CHALLENGE: Turns this into a lambda
        }
    }
}