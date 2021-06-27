using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DSharpPlusNextGen;
using DSharpPlusNextGen.CommandsNext;
using DSharpPlusNextGen.CommandsNext.Attributes;
using DSharpPlusNextGen.Entities;
using DSharpPlusNextGen.VoiceNext;

namespace DSharpPlusNextGen.Examples.Bots.Basics.Commands
{
    internal class General : BaseCommandModule
    {
        private static readonly Random random = new();

        [Command("yt"), Description("Generate YouTube Together Invite")]
        public async Task GenerateYouTubeTogetherInvite(CommandContext ctx, DiscordChannel channel)
        {
            DiscordInvite invite = await channel.CreateInviteAsync(0, 0, TargetType.EmbeddedApplication, TargetActivity.YouTubeTogether);

            await ctx.RespondAsync($"https://discord.gg/{invite.Code}");
        }

        [Command("fish"), Description("Generate Fishington Invite")]
        public async Task GenerateFishingtonInvite(CommandContext ctx, DiscordChannel channel)
        {
            DiscordInvite invite = await channel.CreateInviteAsync(0, 0, TargetType.EmbeddedApplication, TargetActivity.Fishington);

            await ctx.RespondAsync($"https://discord.gg/{invite.Code}");
        }

        [Command("webhooks"), Description("Get webhooks")]
        public async Task GetWebhooks(CommandContext ctx)
        {
            var webhooks = await ctx.Guild.GetWebhooksAsync();
            foreach(DiscordWebhook webhook in webhooks)
            {
                if (webhook.SourceGuild != null)
                {
                    await ctx.RespondAsync($"This webhook follows {webhook.SourceGuild.Name}'s channel {webhook.SourceChannel.Name} in {ctx.Guild.GetChannel(webhook.ChannelId).Mention}");
                } else
                {
                    await ctx.RespondAsync($"This webhook is a normal webhook for {ctx.Guild.GetChannel(webhook.ChannelId).Mention}");
                }
            }
        }

        [Command("preview"), Description("Preview guild")]
        public async Task GetGuildPreview(CommandContext ctx, ulong guild)
        {
            DiscordGuildPreview p = await ctx.Client.GetGuildPreviewAsync(guild);
            await ctx.RespondAsync($"Guild **{p.Name}** has description `{p.Description}` and features.");
            foreach(string feature in p.Features)
            {
                await ctx.Channel.SendMessageAsync(feature);
            }
        }

        [Command("guild"), Description("Get guild")]
        public async Task GetGuild(CommandContext ctx)
        {
            await ctx.RespondAsync($"Guild **{ctx.Guild.Name}** has description `{ctx.Guild.Description}` and system channel flags `{ctx.Guild.SystemChannelFlags}`. Updating to surpress join notification");
            await ctx.Guild.ModifyAsync(g => g.SystemChannelFlags = (ctx.Guild.SystemChannelFlags | SystemChannelFlags.SuppressJoinNotifications));
            await ctx.RespondAsync($"Guild **{ctx.Guild.Name}** has new system channel flags `{ctx.Guild.SystemChannelFlags}`.");
        }

        [Command("parent"), Description("Move to Parent")]
        public async Task MoveChannelParent(CommandContext ctx, DiscordChannel channel, ulong? parentId = null)
        {
            try
            {
                await channel.ModifyParentAsync(parentId, null, reason: "Test");
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("perm"), Description("p")]
        public async Task MoveChannelParent(CommandContext ctx, DiscordRole role)
        {
            try
            {
                Permissions perms = new Permissions();
                perms.Grant(role.Permissions);
                perms.Grant(Permissions.ManageThreads);
                perms.Grant(Permissions.UsePrivateThreads);
                perms.Grant(Permissions.UsePublicThreads);
                await role.ModifyAsync(permissions: perms);
            }
            catch (Exception ex)
            {
                await ctx.RespondAsync(ex.Message);
            }
        }

        [Command("move"), Description("Move Channel")]
        public async Task MoveChannel(CommandContext ctx, DiscordChannel channel, int pos)
        {
            try
            {
                await channel.ModifyPositionAsync(pos, reason: "Test");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Command("appid"), Description("Get application id")]
        public async Task GetApplicationId(CommandContext ctx, ulong msgid)
        {
            DiscordMessage msg = await ctx.Channel.GetMessageAsync(msgid);
            await ctx.RespondAsync($"Application ID: {msg.ApplicationId}");
        }

        [Command("invites"), Description("Gets invites")]
        public async Task GetInvites(CommandContext ctx)
        {
            var invites = await ctx.Guild.GetInvitesAsync();
            foreach (DiscordInvite invite in invites)
            {
                DiscordInvite test = await ctx.Client.GetInviteByCodeAsync(invite.Code, true, true);
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                builder.WithTitle($"Invite {test.Code}")
                    .WithAuthor(test.Inviter.Username, null, test.Inviter.AvatarUrl)
                    .WithTimestamp(test.CreatedAt)
                    .WithDescription($"Max uses: {invite.MaxUses}\n" +
                    $"Expires {test.ExpiresAt}\n" +
                    $"Max age: {test.MaxAge}\n" +
                    $"Type: {test.TargetType}\n" +
                    $"Channel: {test.Channel.Name}");
                if(test.TargetType == TargetType.EmbeddedApplication)
                {
                    builder.AddField("Activity", test.TargetApplication.Name);
                    builder.AddField("ActivityFlags", test.TargetApplication.Flags.ToString());
                }
                await ctx.RespondAsync(builder.Build());
            }
        }


        [Command("flags"), Description("Get Application Flags")]
        public async Task GetApplicationFlags(CommandContext ctx)
        {
            await ctx.RespondAsync($"Application Flags: {ctx.Client.CurrentApplication.Flags}");
        }

        [Command("dev"), Description("Dev!")]
        public async Task DeveloperInfo(CommandContext ctx)
        {
            try
            {
                var privacy = ctx.Client.CurrentApplication.PrivacyPolicyUrl;
                var tos = ctx.Client.CurrentApplication.TermsOfServiceUrl; 
                var app = Process.GetCurrentProcess();
                var emb = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(0xC665A))
                .WithAuthor($"{ctx.Client.CurrentUser.Username}#{ctx.Client.CurrentUser.Discriminator}", $"{ctx.Client.CurrentApplication.GenerateBotOAuth(Permissions.Administrator)}", $"{ctx.Client.CurrentUser.AvatarUrl}")
                .WithTitle("Developer Stuff")
                .WithThumbnail($"{ctx.Client.CurrentApplication.CoverImageUrl}")
                .WithDescription(":3")
                .AddField($"D# Version", ctx.Client.VersionString)
                .AddField($"TOS", ctx.Client.CurrentApplication.TermsOfServiceUrl)
                .AddField($"Privacy", ctx.Client.CurrentApplication.PrivacyPolicyUrl)
                .AddField($"Presence status", ctx.Client.Presences.Values.Where(g => g.User == ctx.Client.CurrentUser).First().Status.ToString())
                .WithFooter($"{ctx.Client.CurrentApplication.Name} developed by {ctx.Client.CurrentApplication.TeamName} | {ctx.Client.CurrentApplication.Team.CreationTimestamp.DateTime}", $"{ctx.Client.CurrentApplication.Team.Icon}");
                await ctx.Channel.SendMessageAsync("Dev Info ready :3", emb.Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " | " + ex.StackTrace);
            }
            finally
            {
                await ctx.Message.DeleteAsync("Command Hide");
            }
        }

        [Command("join")]
        public async Task Join(CommandContext ctx, DiscordChannel channel)
        {
            if (channel.Type != ChannelType.Stage)
                return;

            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            var vstat = ctx.Member?.VoiceState;
            if (vstat?.Channel == null && channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            if (channel == null)
                channel = vstat.Channel;

            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
            {
                await ctx.RespondAsync("Already connected in this guild.");
            }
            else
            {
                await vnext.ConnectAsync(channel);
            }

            await ctx.RespondAsync($"Connected to `{channel.Name}`");
        }

        [Command("openstage"), Description("Opens a stage")]
        public async Task OpenStage(CommandContext ctx, [Description("Stage channel")] DiscordChannel channel, [RemainingText, Description("Stage topic")] string topic)
        {
            if (channel.Type != ChannelType.Stage)
                return;

            var stage = await channel.OpenStageAsync(topic);
            var vnc = ctx.Client.GetVoiceNext().GetConnection(ctx.Guild);
            if (vnc != null)
            {
                await vnc.SendSpeakingAsync(true);
                DiscordMember self = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
                await self.UpdateVoiceStateAsync(channel, false);
            }

            await ctx.RespondAsync($"Opened stage {channel.Name} with topic `{topic}`. It has the ID {stage.Id}");
        }

        [Command("modifystage"), Description("Modifies a stage topic")]
        public async Task CloseStage(CommandContext ctx, [Description("Stage channel")] DiscordChannel channel, [RemainingText, Description("New topic")] string topic)
        {
            if (channel.Type != ChannelType.Stage)
                return;

            await channel.ModifyStageAsync(topic);
            await ctx.RespondAsync($"Modified stage {channel.Name} with new topic {topic}.");
        }

        [Command("closestage"), Description("Closes a stage")]
        public async Task CloseStage(CommandContext ctx, [Description("Stage channel")] DiscordChannel channel)
        {
            if (channel.Type != ChannelType.Stage)
                return;

            await channel.CloseStageAsync();
            await ctx.RespondAsync($"Closed stage {channel.Name}.");
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task WriteMessageAsync(CommandContext ctx, string message)
        {
            await ctx.TriggerTypingAsync();
            await Task.Delay(1000);
            await ctx.RespondAsync(message);
        }
    }
}
