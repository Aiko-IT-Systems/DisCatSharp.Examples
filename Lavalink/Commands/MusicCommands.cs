using System;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;

namespace DisCatSharp.Examples.Lavalink.Commands
{
    public class MusicCommands : ApplicationCommandsModule
    {
        [SlashCommand("play", "Play music asynchronously")]
        public static async Task Play(InteractionContext ctx, [Option("query", "Search string or Youtube link")] string query)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be in the same voice channel as the bot!"
                });
                return;
            }

            LavalinkLoadResult tracks;
            
            // Check if query is valid url
            if (Uri.TryCreate(query, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                // Get track from the url
                tracks = await node.Rest.GetTracksAsync(uriResult);
            }
            else
            {
                // Search track in Youtube
                tracks = await node.Rest.GetTracksAsync(query);
            }
            
            // If something went wrong on Lavalink's end or it just couldn't find anything.
            if (tracks.LoadResultType is LavalinkLoadResultType.LoadFailed or LavalinkLoadResultType.NoMatches)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = $"Track search failed for `{query}`."
                });
                return;
            }
            
            // Get first track in the result
            var track = tracks.Tracks.First();

            await connection.PlayAsync(track);
            
            // CHALLENGE: Add a queue. You need to make sure that new tracks are added to a special queue instead of overwriting the current one
            // and automatically played after the end of the previous track.
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = $"Now playing {Formatter.InlineCode(track.Author)} - {Formatter.InlineCode(track.Title)}"
            });
        }
        
        [SlashCommand("pause", "Pause playback")]
        public static async Task Pause(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be in the same voice channel as the bot!"
                });
                return;
            }

            // Pause playback
            await connection.PauseAsync();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "Paused!"
            });
        }
        
        [SlashCommand("resume", "Resume playback")]
        public static async Task Resume(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be in the same voice channel as the bot!"
                });
                return;
            }

            // Resume playback
            await connection.ResumeAsync();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = $"Now playing `{connection.CurrentState.CurrentTrack.Title}`"
            });
        }

        [SlashCommand("stop", "Stop playback")]
        public static async Task Stop(InteractionContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be in the same voice channel as the bot!"
                });
                return;
            }

            await connection.StopAsync();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "Playback is stopped!"
            });
        }
        
        // BONUS: play music through the context menu!
        [ContextMenu(ApplicationCommandType.Message, "Play")]
        public static async Task Play(ContextMenuContext ctx)
        {
            var query = ctx.TargetMessage.Content;
            
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var connection = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.Channel)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be in the same voice channel as the bot!"
                });
                return;
            }

            LavalinkLoadResult tracks;
            
            // Check if query is valid url
            if (Uri.TryCreate(query, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                // Get track from the url
                tracks = await node.Rest.GetTracksAsync(uriResult);
            }
            else
            {
                // Search track in Youtube
                tracks = await node.Rest.GetTracksAsync(query);
            }
            
            //If something went wrong on Lavalink's end
            if (tracks.LoadResultType == LavalinkLoadResultType.LoadFailed
                //or it just couldn't find anything.
                || tracks.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = $"Track search failed for `{query}`."
                });
                return;
            }
            
            // Get first track in the result
            var track = tracks.Tracks.First();

            await connection.PlayAsync(track);
            
            // CHALLENGE: Add a queue. You need to make sure that new tracks are added to a special queue instead of overwriting the current one
            // and automatically played after the end of the previous track.
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = $"Now playing {Formatter.InlineCode(track.Author)} - {Formatter.InlineCode(track.Title)}"
            });
        }
    }
}