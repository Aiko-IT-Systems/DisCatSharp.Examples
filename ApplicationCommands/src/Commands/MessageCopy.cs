using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace DisCatSharp.Examples.ApplicationCommands.Commands
{
    // This simple command shows how to use the message context menu
    public class MessageCopy : ApplicationCommandsModule
    {
        [ContextMenu(ApplicationCommandType.Message, "Copy message")]
        public static async Task Command(ContextMenuContext context)
        {
            // Create the response message
            DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();

            // Copying the content from the original message to the new one.
            discordInteractionResponseBuilder.Content = context.TargetMessage.Content;
            
            // Send the message. InteractionResponseType.ChannelMessageWithSource means that the command executed within 3 seconds and has the results ready.
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);

            // CHALLENGE: Copy embeds/components to the new message
            // CHALLENGE #2: Send message via webhook (with username and avatar of the original message author)
        }
    }
}