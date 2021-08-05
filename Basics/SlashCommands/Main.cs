using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.Entities;
using DisCatSharp.SlashCommands;


using static DisCatSharp.Examples.Basics.Main.Bot;

namespace DisCatSharp.Examples.Basics.SlashCommands
{
    /// <summary>
    /// The main slash command module.
    /// </summary>
    internal class Main : SlashCommandModule
    {
        /// <summary>
        /// Pings you.
        /// </summary>
        /// <param name="ctx">The command context.</param>
        [SlashCommand("ping", "Send's the actual ping")]
        public static async Task Ping(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true).WithContent("Loading Ping, could take time. Please lay back <3"));
            await Task.Delay(2000);
            await ctx.Channel.SendMessageAsync($"Pong: {Client.Ping}");
        }

        /// <summary>
        /// Shutdowns the bot.
        /// </summary>
        /// <param name="ctx">The command context.</param>
        [SlashCommand("shutdown", "Bot shutdown (restricted)")]
        public static async Task Shutdown(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Shutdown request"));
            if (ctx.Client.CurrentApplication.Team.Members.Where(x => x.User == ctx.User).Any())
            {
                await Task.Delay(5000);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Shutdown request accepted."));
                ShutdownRequest.Cancel();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Shuting down!"));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
            }
        }

        /// <summary>
        /// Repeats what you say.
        /// </summary>
        /// <param name="ctx">The command context.</param>
        /// <param name="message">The message to repeat.</param>
        [SlashCommand("say", "Say something via embed")]
        public static async Task Repeat(InteractionContext ctx, [Option("message", "Message to repeat")] string message)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true).AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Repeat Message")
                        .WithDescription($"{message}\n" +
                        $"User: {ctx.Interaction.User.Username}\n").Build()));
        }

        /// <summary>
        /// Gets the users avatar.
        /// </summary>
        /// <param name="ctx">The command context.</param>
        /// <param name="user">The optional user.</param>
        [SlashCommand("avatar", "Get someone's avatar")]
        public static async Task Avatar(InteractionContext ctx, [Option("user", "The user to get it for")] DiscordUser user = null)
        {
            user ??= ctx.Member;
            var embed = new DiscordEmbedBuilder
            {
                Title = $"Avatar",
                ImageUrl = user.AvatarUrl
            }.
            WithFooter($"Requested by {ctx.Member.DisplayName}", ctx.Member.AvatarUrl).
            WithAuthor($"{user.Username}", user.AvatarUrl, user.AvatarUrl);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()));
        }
    }
}