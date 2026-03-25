using System;
using System.Diagnostics;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace DisCatSharp.Examples.ApplicationCommands.Commands;

/// <summary>
///     Notice how Ping inherits the ApplicationCommandsModule
/// </summary>
public class Ping : ApplicationCommandsModule
{
	/// <summary>
	///     Slash command registers the name and command description.
	/// </summary>
	/// <param name="context">Interaction context</param>
	[SlashCommand("ping", "Checks the latency between the bot and the Discord API. Best used to see if the bot is lagging.")]
	public static async Task CommandAsync(InteractionContext context)
	{
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());

		var stopwatch = Stopwatch.StartNew();
		await Task.Delay(150);
		stopwatch.Stop();

		// CHALLENGE: Turn this into a reusable health card and wire one action to a real dashboard or deployment status page.
		var card = new DiscordContainerComponent(accentColor: new DiscordColor("#5865F2"))
			.AddComponent(new DiscordTextDisplayComponent($$"""
				## Shard health snapshot
				- Gateway latency: `{{context.Client.Ping}}ms`
				- Command turnaround: `{{stopwatch.ElapsedMilliseconds}}ms`
				- Shard: `{{context.Client.ShardId}}`
				- Guild cache: `{{context.Client.Guilds.Count}}`

				> Deferred responses are a good place to gather runtime measurements before you render a richer card.
				"""));

		await context.EditResponseAsync(new DiscordWebhookBuilder().WithV2Components().AddComponents(card));
	}
}
