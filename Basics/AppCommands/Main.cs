using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using System.Linq;
using System.Threading.Tasks;

using static DisCatSharp.Examples.Basics.Main.Bot;

namespace DisCatSharp.Examples.Basics.AppCommands;

/// <summary>
/// The main slash command module.
/// </summary>
internal class Main : ApplicationCommandsModule
{
	/// <summary>
	/// Pings you.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[SlashCommand("ping", "Send's the actual ping")]
	public static async Task PingAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Loading Ping, could take time. Please lay back <3"));
		await Task.Delay(2000);
		await ctx.Channel.SendMessageAsync($"Pong: {Client.Ping}");
	}

	/// <summary>
	/// Shutdowns the bot.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[SlashCommand("shutdown", "Bot shutdown (restricted)")]
	public static async Task ShutdownAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Shutdown request"));
		if (ctx.Client.CurrentApplication.Owners.Any(x => x == ctx.User))
		{
			await Task.Delay(5000);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Shutdown request accepted."));
			ShutdownRequest.Cancel();
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Shutting down!"));
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
	public static async Task RepeatAsync(InteractionContext ctx, [Option("message", "Message to repeat")] string message)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder()
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
	public static async Task AvatarAsync(InteractionContext ctx, [Option("user", "The user to get it for")] DiscordUser user = null)
	{
		user ??= ctx.Member;
		var embed = new DiscordEmbedBuilder
		{
			Title = $"Avatar",
			ImageUrl = user.AvatarUrl
		}.WithFooter($"Requested by {ctx.Member.DisplayName}", ctx.Member.AvatarUrl).WithAuthor($"{user.Username}", user.AvatarUrl, user.AvatarUrl);
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()));
	}

	/// <summary>
	/// Gets the users avatar with context menu.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[ContextMenu(ApplicationCommandType.User, "Get avatar")]
	public static async Task AvatarAsync(ContextMenuContext ctx)
	{
		var embed = new DiscordEmbedBuilder
		{
			Title = $"Avatar",
			ImageUrl = ctx.TargetUser.AvatarUrl
		}.WithFooter($"Requested by {ctx.Member.DisplayName}", ctx.Member.AvatarUrl).WithAuthor($"{ctx.TargetUser.Username}", ctx.TargetUser.AvatarUrl, ctx.TargetUser.AvatarUrl);
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()));
	}
}