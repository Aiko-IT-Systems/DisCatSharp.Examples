using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Examples.Hosting.Services;

using Microsoft.Extensions.DependencyInjection;

namespace DisCatSharp.Examples.Hosting.Commands;

/// <summary>
///     Notice how Ping inherits the ApplicationCommandsModule
/// </summary>
public class AppCommands : ApplicationCommandsModule
{
	/// <summary>
	///     Slash command registers the name and command description.
	/// </summary>
	/// <param name="context">Interaction context</param>
	[SlashCommand("ping", "Test command for Hosting.")]
	public static async Task CommandAsync(InteractionContext context)
	{
		var stats = context.Services.GetRequiredService<BotStatusService>().RecordCommand("SecondBot", "ping", context.Client.Ping, context.Client.Guilds.Count);
		// CHALLENGE: Pull health checks, feature flags, or configuration from DI so this becomes a real readiness panel.
		var card = new DiscordContainerComponent(accentColor: new DiscordColor("#3BA55D"))
			.AddComponent(new DiscordTextDisplayComponent($$"""
				## Hosted slash command status
				- Latency: `{{stats.GatewayLatency}}ms`
				- Uptime: `{{stats.Uptime:g}}`
				- Guilds: `{{stats.GuildCount}}`
				- Invocation count: `{{stats.InvocationCount}}`

				> This card stays intentionally small so you can layer real host data onto it.
				"""));

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithV2Components().AddComponents(card));
	}
}
