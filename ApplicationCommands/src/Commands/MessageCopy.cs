using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using System.Threading.Tasks;

namespace DisCatSharp.Examples.ApplicationCommands.Commands;

/// <summary>
/// This simple command shows how to use the message context menu.
/// </summary>
public class MessageCopy : ApplicationCommandsModule
{
	/// <summary>
	/// Copies the message and sends it on its own username.
	/// </summary>
	/// <param name="context">Context menu context.</param>
	[ContextMenu(ApplicationCommandType.Message, "Copy message")]
	public static async Task CommandAsync(ContextMenuContext context)
	{
		// Create the response message
		DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new()
		{
			// Copying the content from the original message to the new one.
			Content = context.TargetMessage.Content
		};

		// Send the message. InteractionResponseType.ChannelMessageWithSource means that the command executed within 3 seconds and has the results ready.
		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);

		// CHALLENGE: Copy embeds/components to the new message
		// CHALLENGE #2: Send message via webhook (with username and avatar of the original message author)
	}
}
