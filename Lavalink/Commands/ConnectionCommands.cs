using DisCatSharp.ApplicationCommands;
using DisCatSharp.Lavalink;

using System.Linq;
using System.Threading.Tasks;

namespace DisCatSharp.Examples.Lavalink.Commands
{
    public class ConnectionCommands : ApplicationCommandsModule
    {
        [SlashCommand("connect", "Join the voice channel")]
        public static async Task Connect(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            
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
            
            // Check if Lavalink connection is established
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The Lavalink connection is not established!"
                });
                return;
            }

            var node = lava.ConnectedNodes.Values.First();

            // Connect to the channel
            await ctx.Member.VoiceState.Channel.ConnectAsync(node);
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = $"The bot has joined the channel {Formatter.InlineCode(ctx.Member.VoiceState.Channel.Name)}"
            });
        }
        
        [SlashCommand("leave", "Leave the voice channel")]
        public static async Task Leave(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The Lavalink connection is not established!"
                });
                return;
            }

            var node = lava.ConnectedNodes.Values.First();
            
            // Get the current Lavalink connection in the guild.
            var connection = node.GetGuildConnection(ctx.Guild);
            
            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }

            await connection.DisconnectAsync();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "The bot left the voice channel"
            });
        }
    }
}