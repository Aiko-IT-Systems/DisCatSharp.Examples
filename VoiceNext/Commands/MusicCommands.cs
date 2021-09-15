using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.VoiceNext;

namespace DisCatSharp.Examples.VoiceNext.Commands
{
    /// <summary>
    /// Playback control with these commands.
    /// </summary>
    public class MusicCommands : ApplicationCommandsModule
    {
        /// <summary>
        /// Since we want to be able to pause and stop playback without waiting for the end, we need to store the streams somewhere
        /// </summary>
        private static readonly Dictionary<ulong, Stream> PlayBacks = new();
        
        /// <summary>
        /// Play local file asynchronously.
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        /// <param name="path">Path to the audio file</param>
        [SlashCommand("play", "Play local file asynchronously")]
        public static async Task Play(InteractionContext ctx, [Option("path", "Path to the audio file")] string path)
        {
            // Get the current VoiceNext connection in the guild.
            var vnext = ctx.Client.GetVoiceNext();
            var connection = vnext.GetConnection(ctx.Guild);

            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.TargetChannel)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be in the same voice channel as the bot!"
                });
                return;
            }
            
            // Stop playback if playing
            if (PlayBacks.ContainsKey(ctx.Guild.Id))
            {
                await PlayBacks[ctx.Guild.Id].DisposeAsync();
                PlayBacks.Remove(ctx.Guild.Id);
            }

            var transmit = connection.GetTransmitSink();

            // Please note that ffmpeg must be installed and added to PATH on the computer where the bot is launched.
            var ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $@"-i ""{path}"" -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });

            var pcm = ffmpeg.StandardOutput.BaseStream;
            
            // Without this, we cannot stop playback using the 'stop' command.
            // Also, without this, the command cannot complete and return a message to the user before the playback is complete.
            _ = pcm.CopyToAsync(transmit);
            PlayBacks.Add(ctx.Guild.Id, pcm);
            
            // CHALLENGE: Add a queue. You need to make sure that new audio files are added to a special queue instead of overwriting the current one
            // and automatically played after the end of the previous file.
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = $"Now playing {Formatter.InlineCode(path)}"
            });
        }
        
        /// <summary>
        /// Pause playback
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        [SlashCommand("pause", "Pause playback")]
        public static async Task Pause(InteractionContext ctx)
        {
            // Get the current VoiceNext connection in the guild.
            var vnext = ctx.Client.GetVoiceNext();
            var connection = vnext.GetConnection(ctx.Guild);

            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.TargetChannel)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be in the same voice channel as the bot!"
                });
                return;
            }

            // Pause playback
            connection.Pause();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "Paused!"
            });
        }
        
        /// <summary>
        /// Resume playback
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        [SlashCommand("resume", "Resume playback")]
        public static async Task Resume(InteractionContext ctx)
        {
            // Get the current VoiceNext connection in the guild.
            var vnext = ctx.Client.GetVoiceNext();
            var connection = vnext.GetConnection(ctx.Guild);

            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }
            
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.TargetChannel)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be in the same voice channel as the bot!"
                });
                return;
            }

            // Resume playback
            _ = connection.ResumeAsync();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "Audio file is playing"
            });
        }

        /// <summary>
        /// Stop playback
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        [SlashCommand("stop", "Stop playback")]
        public static async Task Stop(InteractionContext ctx)
        {
            // Get the current VoiceNext connection in the guild.
            var vnext = ctx.Client.GetVoiceNext();
            var connection = vnext.GetConnection(ctx.Guild);

            if (connection == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "The bot is not connected to the voice channel in this guild!"
                });
                return;
            }
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null || ctx.Member.VoiceState.Channel != connection.TargetChannel)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be in the same voice channel as the bot!"
                });
                return;
            }
            
            // Stop playback if playing
            if (PlayBacks.ContainsKey(ctx.Guild.Id))
            {
                await PlayBacks[ctx.Guild.Id].DisposeAsync();
                PlayBacks.Remove(ctx.Guild.Id);
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "Playback is stopped"
            });
        }
    }
}