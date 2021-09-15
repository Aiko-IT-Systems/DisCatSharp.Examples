using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace DisCatSharp.Examples.ApplicationCommands.Commands
{
    /// <summary>
    /// This simple command shows how to use the user context menu
    /// </summary>
    public class UserInfo : ApplicationCommandsModule
    {
        /// <summary>
        /// Unlike slash commands that use BeforeSlashExecutionAsync/AfterSlashExecutionAsync,
        /// context menu commands use BeforeContextMenuExecutionAsync/AfterContextMenuExecutionAsync
        /// </summary>
        /// <param name="context">Context menu context</param>
        public override async Task<bool> BeforeContextMenuExecutionAsync(ContextMenuContext context)
        {
            if (context.Guild == null)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new()
                {
                    Content = "Error: This is a guild command!",

                    // Make all errors visible to just the user, makes the channel more clean for everyone else.
                    IsEphemeral = true
                };

                // Send the response.
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get information about the user.
        /// </summary>
        /// <param name="context"></param>
        [ContextMenu(ApplicationCommandType.User, "Get info")]
        public static async Task Command(ContextMenuContext context)
        {
            // Create the response message
            DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();

            // Add info to message content.
            discordInteractionResponseBuilder.Content = $"Username: {context.TargetMember.Username + "#" + context.TargetMember.Discriminator}\n" +
                                                        $"Nickname: {context.TargetMember.Nickname??"`Empty`"}\n" +
                                                        $"Registered: {context.TargetMember.CreationTimestamp}\n" +
                                                        $"Joined: {context.TargetMember.JoinedAt}\n" +
                                                        $"Admin: {context.TargetMember.Permissions.HasPermission(Permissions.Administrator)}\n" +
                                                        $"Owner: {context.TargetMember.IsOwner}";
            
            // Send the message. InteractionResponseType.ChannelMessageWithSource means that the command executed within 3 seconds and has the results ready.
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);

            // CHALLENGE: Copy embeds/components to the new message
        }
    }
}