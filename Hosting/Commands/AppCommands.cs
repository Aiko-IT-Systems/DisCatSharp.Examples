using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

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
		DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new()
		{
			Content = $"Pong! Webhook latency is {context.Client.Ping}ms"
		};

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
	}
}
