using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Exceptions;
using DisCatSharp.VoiceNext;

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp.ApplicationCommands.Attributes;

namespace DisCatSharp.Examples.Interactivity.Commands
{
    /// <summary>
    /// Stage channel management.
    /// </summary>
    public class StageCommands : ApplicationCommandsModule
    {
        /// <summary>
        /// The command for creating a stage channel.
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        /// <param name="name">Stage channel name</param>
        /// <param name="topic">Topic of the new stage</param>
        [SlashCommand("create_stage", "The command for creating a stage channel")]
        public static async Task CreateStage(InteractionContext ctx, [Option("name", "Stage channel name")] string name, 
            [Option("topic", "Topic of the new stage")] string topic)
        {
            // First, create a stage channel
            var channel = await ctx.Guild.CreateStageChannelAsync(name);
            
            // Secondly, open stage in the channel you just created
            await channel.OpenStageAsync(topic);
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "The stage channel has been successfully created."
            });
        }
        
        /// <summary>
        /// Close stage without deleting.
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        /// <param name="stage">Stage channel</param>
        [SlashCommand("close_stage", "Close stage without deleting")]
        public static async Task CloseStage(InteractionContext ctx, [Option("id", "Stage channel"), ChannelTypes(ChannelType.Stage)] DiscordChannel stage)
        {
            // Check whether the desired stage channel was found.
            if (stage == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Stage channel not found."
                });
                return;
            }

            // Any methods associated with updating a stage instance can throw a NotFoundException if stage is not currently open
            try
            {
                await stage.CloseStageAsync();
            
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Stage channel closed successfully"
                });
            }
            catch (NotFoundException)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Stage not opened!"
                });
            }

        }

        /// <summary>
        /// Permanently delete stage channel.
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        /// <param name="stage">Stage channel</param>
        [SlashCommand("delete_stage", "Permanently delete stage channel")]
        public static async Task DeleteStage(InteractionContext ctx, [Option("id", "Stage channel"), ChannelTypes(ChannelType.Stage)] DiscordChannel stage)
        {
            // Check whether the desired stage channel was found.
            if (stage == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Stage channel not found."
                });
                return;
            }

            await stage.DeleteAsync();
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "Stage channel has been successfully deleted."
            });
        }
        
        /// <summary>
        /// Get stage channel info.
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        /// <param name="stage">Stage channel</param>
        [SlashCommand("get_stage", "Get stage channel info")]
        public static async Task GetStage(InteractionContext ctx, [Option("id", "Stage channel"), ChannelTypes(ChannelType.Stage)] DiscordChannel stage)
        {
            // Check whether the desired stage channel was found.
            if (stage == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Stage channel not found."
                });
                return;
            }

            // Any methods associated with updating a stage instance can throw a NotFoundException if stage is not currently open
            try
            {
                // Get a stage instance to use its data
                var instance = await stage.GetStageAsync();
            
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = $"Topic: {instance.Topic}\nId: {instance.ChannelId}\n" +
                              $"Speakers: {string.Join(", ", stage.Users.Where(m => !m.VoiceState.IsSuppressed).Select(m => m.DisplayName))}"
                });
            }
            catch (NotFoundException)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Stage not opened!"
                });
            }
        }
        
        /// <summary>
        /// Update topic and stage channel publicity.
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        /// <param name="stage">Stage channel</param>
        /// <param name="topic">New topic</param>
        /// <param name="isPublic">Whether the stage channel will be private or public</param>
        [SlashCommand("modify_stage", "Update topic and stage channel publicity")]
        public static async Task ModifyStage(InteractionContext ctx, [Option("id", "Stage channel"), ChannelTypes(ChannelType.Stage)] DiscordChannel stage, [Option("topic", "New topic")] string topic,
            [Option("public", "Whether the stage channel will be private or public")] bool isPublic)
        {
            // Check whether the desired stage channel was found.
            if (stage == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Stage channel not found."
                });
                return;
            }
            
            // Any methods associated with updating a stage instance can throw a NotFoundException if stage is not currently open
            try
            {
                await stage.ModifyStageAsync(topic, isPublic ? StagePrivacyLevel.Public : StagePrivacyLevel.GuildOnly);
            
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Stage channel has been successfully updated."
                });
            }
            catch (NotFoundException)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Stage not opened."
                });
            }
        }
        
        /// <summary>
        /// Make a member a speaker, or move them to an audience.
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        /// <param name="user">The user who needs to change the voice state</param>
        [SlashCommand("speaker", "Make a member a speaker, or move them to an audience")]
        public static async Task SpeakerStage(InteractionContext ctx, [Option("user", "The user who needs to change the voice state")] DiscordUser user = null)
        {
            var member = (DiscordMember) user ?? ctx.Member;
            
            // Check if the user is currently connected to the stage channel
            if (member.VoiceState == null || !member.VoiceState.Channel.IsStage)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be connected to a stage channel to use this command!"
                });
                return;
            }
            
            // IsSuppressed - whether the user is currently a speaker, or in the audience
            if (member.VoiceState.IsSuppressed)
                await member.MakeSpeakerAsync();
            else
                await member.MoveToAudienceAsync();

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = "The member's voice state has been successfully updated."
            });
        }
        
        /// <summary>
        /// Play audio in stage channel.
        /// </summary>
        /// <param name="ctx">Interaction context</param>
        /// <param name="path">Path to the audio file</param>
        [SlashCommand("play_stage", "Play audio in stage channel")]
        public static async Task PlayStage(InteractionContext ctx, [Option("path", "Path to the audio file")] string path)
        {
            // This is a basic example of using VoiceNext with a stage channel (see the VoiceNext example for more details).
            // You can also use lavalink.
            
            // Check if the user is currently connected to the stage channel
            if (ctx.Member.VoiceState == null || !ctx.Member.VoiceState.Channel.IsStage)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    IsEphemeral = true,
                    Content = "You must be connected to a stage channel to use this command!"
                });
                return;
            }

            // Connect to the stage channel
            var connection = await ctx.Member.VoiceState.Channel.ConnectAsync();

            // Only those who are speakers can "speak" in the stage channel, so we make the bot the speaker.
            await (await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id)).MakeSpeakerAsync();

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
            
            _ = pcm.CopyToAsync(transmit);
            
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
            {
                Content = $"Now playing {Formatter.InlineCode(path)}"
            });
            
            // Get a stage instance to use its data
            var instance = await ctx.Member.VoiceState.Channel.GetStageAsync();
            
            // We change the topic of the stage so people know which audio file is currently playing.
            await ctx.Member.VoiceState.Channel.ModifyStageAsync(Path.GetFileNameWithoutExtension(path), instance.PrivacyLevel);
        }
    }
}