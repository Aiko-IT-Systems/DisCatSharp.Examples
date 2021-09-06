using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.VoiceNext;

namespace DisCatSharp.Examples.VoiceNext.Commands
{
    public class ConnectionCommands : ApplicationCommandsModule
    {
        [SlashCommand("connect", "Join the voice channel")]
        public static async Task Connect(InteractionContext ctx)
        {
            // Check if the user is currently connected to the voice channel
            if (ctx.Member.VoiceState == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be connected to a voice channel to use this command!"
                });
                return;
            }

            // Connect to the channel
            _ = ctx.Member.VoiceState.Channel.ConnectAsync();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = $"The bot has joined the channel {Formatter.InlineCode(ctx.Member.VoiceState.Channel.Name)}"
            });
        }
        
        [SlashCommand("leave", "Leave the voice channel")]
        public static async Task Leave(InteractionContext ctx)
        {
            // Get the current VoiceNext connection in the guild.
            var vnext = ctx.Client.GetVoiceNext();
            var connection = vnext.GetConnection(ctx.Guild);

            // Check if the bot is currently connected to the voice channel
            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }

            connection.Disconnect();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "The bot left the voice channel"
            });
        }
    }
}