using System.Threading.Tasks;

using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Examples.Hosting.Services;

using Microsoft.Extensions.DependencyInjection;

namespace DisCatSharp.Examples.Hosting.Commands;

public class TestCommands : BaseCommandModule
{
	[Command("ping"), Description("Test command for Hosting")]
	public async Task TestAsync(CommandContext ctx)
	{
		var stats = ctx.Services.GetRequiredService<BotStatusService>().RecordCommand("FirstBot", "ping", ctx.Client.Ping, ctx.Client.Guilds.Count);

		// CHALLENGE: Surface real health-check values from the host so the text-command sample mirrors the slash-command panel.
		var card = new DiscordContainerComponent(accentColor: new DiscordColor("#3BA55D"))
			.AddComponent(new DiscordTextDisplayComponent($$"""
				## Hosted bot status
				- Latency: `{{stats.GatewayLatency}}ms`
				- Uptime: `{{stats.Uptime:g}}`
				- Guilds: `{{stats.GuildCount}}`
				- Command calls: `{{stats.InvocationCount}}`

				> Compare this with the slash-command hosting sample and decide which shared host data you want to expose.
				"""));

		await ctx.RespondAsync(new DiscordMessageBuilder().WithV2Components().AddComponents(card));
	}
}
