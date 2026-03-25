using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using static DisCatSharp.Examples.Basics.Main.Bot;

namespace DisCatSharp.Examples.Basics.AppCommands;

/// <summary>
///     The main slash command module.
/// </summary>
internal class Main : ApplicationCommandsModule
{
	private static DiscordContainerComponent CreateCard(string title, IEnumerable<string> lines, string footer = null)
	{
		var body = new StringBuilder()
			.Append("## ")
			.Append(title)
			.AppendLine();

		foreach (var line in lines)
			body.Append("- ").AppendLine(line);

		if (!string.IsNullOrWhiteSpace(footer))
			body.AppendLine().Append("> ").Append(footer);

		return new DiscordContainerComponent(accentColor: new DiscordColor("#5865F2"))
			.AddComponent(new DiscordTextDisplayComponent(body.ToString()));
	}

	private static DiscordContainerComponent CreateAvatarCard(DiscordUser targetUser, DiscordUser requester)
	{
		// CHALLENGE: Let the user choose which assets appear in the gallery instead of always showing every available profile image.
		var galleryItems = new List<DiscordMediaGalleryItem>
		{
			new(targetUser.AvatarUrl, $"{targetUser.UsernameWithGlobalName}'s current avatar"),
			new(targetUser.DefaultAvatarUrl, $"{targetUser.UsernameWithGlobalName}'s default avatar fallback")
		};

		if (!string.IsNullOrWhiteSpace(targetUser.BannerUrl))
			galleryItems.Add(new DiscordMediaGalleryItem(targetUser.BannerUrl, $"{targetUser.UsernameWithGlobalName}'s banner"));

		if (!string.IsNullOrWhiteSpace(targetUser.AvatarDecorationUrl))
			galleryItems.Add(new DiscordMediaGalleryItem(targetUser.AvatarDecorationUrl, $"{targetUser.UsernameWithGlobalName}'s avatar decoration"));

		var section = new DiscordSectionComponent(
		[
			new DiscordTextDisplayComponent($"## Avatar preview · {targetUser.UsernameWithGlobalName}"),
			new DiscordTextDisplayComponent($$"""
				- Requested by: {{requester.Mention}}
				- The media gallery below compares the active avatar with fallback and optional profile assets.
				""")
		]).WithThumbnailComponent(targetUser.AvatarUrl, $"{targetUser.UsernameWithGlobalName}'s avatar");

		return new DiscordContainerComponent(accentColor: targetUser.BannerColor ?? new DiscordColor("#5865F2"))
			.AddComponent(section)
			.AddComponent(new DiscordMediaGalleryComponent(galleryItems))
			.AddComponent(new DiscordTextDisplayComponent("> The gallery compares the active avatar with optional profile assets without needing an embed."));
	}

	/// <summary>
	///     Pings you.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[SlashCommand("ping", "Send's the actual ping")]
	public static async Task PingAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());

		var stopwatch = Stopwatch.StartNew();
		await Task.Delay(150);
		stopwatch.Stop();

		// CHALLENGE: Add a section accessory button or link row so this card can jump to a live status page or dashboard.
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithV2Components().AddComponents(CreateCard("Latency snapshot",
		[
			"The bot responded successfully and gathered a couple of useful runtime stats.",
			$"Gateway latency: `{Client.Ping}ms`",
			$"Interaction turnaround: `{stopwatch.ElapsedMilliseconds}ms`",
			$"Current shard: `{ctx.Client.ShardId}`"
		], "Compare this slash-command reply with the text-command ping sample in Basics.")));
	}

	/// <summary>
	///     Shutdowns the bot.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[SlashCommand("shutdown", "Bot shutdown (restricted)")]
	public static async Task ShutdownAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Shutdown request"));
		if (ctx.Client.CurrentApplication.Members.Any(x => x.Id == ctx.User.Id))
		{
			await Task.Delay(5000);
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Shutdown request accepted."));
			await ShutdownRequest.CancelAsync();
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Shutting down!"));
		}
		else
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not allowed to execute this request!"));
	}

	/// <summary>
	///     Repeats what you say.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	/// <param name="message">The message to repeat.</param>
	[SlashCommand("say", "Say something via a simple Components V2 card")]
	public static async Task RepeatAsync(InteractionContext ctx, [Option("message", "Message to repeat")] string message)
	{
		// CHALLENGE: Add a follow-up button row so the user can resend, pin, or convert this card into a reminder.
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithV2Components()
			.AddComponents(CreateCard("Repeat message",
			[
				message,
				$"User: {ctx.User.Mention}"
			], "Try a section accessory or action row next once you want the reply to keep evolving.")));
	}

	/// <summary>
	///     Schedules a reminder and demonstrates an async follow-up flow.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	/// <param name="seconds">The delay in seconds.</param>
	/// <param name="note">The note to send later.</param>
	[SlashCommand("remind", "Schedule a reminder with an async follow-up.")]
	public static async Task RemindAsync(
		InteractionContext ctx,
		[Option("seconds", "Delay in seconds (1-900).")] long seconds,
		[Option("note", "The reminder text to send later.")] string note = "Check back on the work you started earlier."
	)
	{
		if (seconds is < 1 or > 900)
		{
			await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
			{
				Content = "Pick a delay between 1 and 900 seconds.",
				IsEphemeral = true
			});
			return;
		}

		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder
		{
			IsEphemeral = true
		});

		await ctx.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent($"Reminder scheduled. I'll post the note in {seconds} seconds."));

		try
		{
			await Task.Delay(TimeSpan.FromSeconds(seconds), ShutdownRequest.Token);
		}
		catch (TaskCanceledException)
		{
			return;
		}

		// CHALLENGE: Add a snooze button flow that reuses the same validation rules instead of posting only a one-shot reminder.
		await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithV2Components().AddComponents(CreateCard("Reminder ready",
		[
			$"Requested by: {ctx.User.Mention}",
			$"Delay: `{seconds}` seconds",
			$"Note: {note}"
		], "This follow-up arrives after the original command body has already finished running.")));
	}

	/// <summary>
	///     Gets the users avatar.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	/// <param name="user">The optional user.</param>
	[SlashCommand("avatar", "Get someone's avatar")]
	public static async Task AvatarAsync(InteractionContext ctx, [Option("user", "The user to get it for")] DiscordUser user = null)
	{
		user ??= ctx.Member;

		// CHALLENGE: Add a toggle button that swaps between a public card and an ephemeral personal preview.
		List<DiscordComponent> links =
		[
			new DiscordLinkButtonComponent(user.AvatarUrl, "Open avatar"),
			new DiscordLinkButtonComponent(user.ProfileUrl, "Open profile")
		];

		if (!string.IsNullOrWhiteSpace(user.BannerUrl))
			links.Add(new DiscordLinkButtonComponent(user.BannerUrl, "Open banner"));

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithV2Components()
			.AddComponents(
				CreateAvatarCard(user, ctx.User),
				new DiscordActionRowComponent(links)));
	}

	/// <summary>
	///     Gets the users avatar with context menu.
	/// </summary>
	/// <param name="ctx">The command context.</param>
	[ContextMenu(ApplicationCommandType.User, "Get avatar")]
	public static async Task AvatarAsync(ContextMenuContext ctx)
	{
		// CHALLENGE: Reuse the same avatar-card builder for a modal-driven "save favorite asset" workflow.
		List<DiscordComponent> links =
		[
			new DiscordLinkButtonComponent(ctx.TargetUser.AvatarUrl, "Open avatar"),
			new DiscordLinkButtonComponent(ctx.TargetUser.ProfileUrl, "Open profile")
		];

		if (!string.IsNullOrWhiteSpace(ctx.TargetUser.BannerUrl))
			links.Add(new DiscordLinkButtonComponent(ctx.TargetUser.BannerUrl, "Open banner"));

		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithV2Components()
			.AddComponents(
				CreateAvatarCard(ctx.TargetUser, ctx.User),
				new DiscordActionRowComponent(links)));
	}
}
