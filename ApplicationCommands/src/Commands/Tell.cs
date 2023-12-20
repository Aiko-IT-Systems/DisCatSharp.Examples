using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;

using System.Threading.Tasks;

namespace DisCatSharp.Examples.ApplicationCommands.Commands;

/// <summary>
/// Shows usage of the ChoiceAttribute.
/// </summary>
public class Tell : ApplicationCommandsModule
{
	/// <summary>
	/// Shows advanced usage of ChoiceProvider attribute with Reflection
	/// Check if this command is executed in the guild.
	/// </summary>
	/// <param name="context">Interaction context</param>
	public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext context)
	{
		if (context.Guild == null)
		{
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
			{
				Content = "Error: This command can only be used in a guild!",
				IsEphemeral = true
			});

			return false;
		}

		return true;
	}

	/// <summary>
	/// Slash command registers the name and command description.
	/// </summary>
	/// <param name="context">Interaction context</param>
	/// <param name="victim">Who the bot is messaging</param>
	/// <param name="phrase">What to message to the victim</param>
	[SlashCommand("tell", "Sends someone a message.")]
	public static async Task CommandAsync(
		InteractionContext context,
		[Option("victim", "Who the bot is messaging.")] DiscordUser victim,
		[Choice("ModMail", "Please contact ModMail."), Choice("Behaviour", "Please stop being rude."), Choice("Advertisement", "Please stop advertising."), Choice("SFW", "Please keep things SFW."), Option("Phrase", "What to message to the victim.")]
		string phrase
	)
	{
		DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new()
		{
			IsEphemeral = true
		};
		// CHALLENGE: Add other potentional staff permissions.
		if (!context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.Administrator))
		{
			discordInteractionResponseBuilder.Content = $"Error: You're not part of staff! Missing the {"Administrator".InlineCode()} permission!";
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
			return;
		}

		var victimMember = await context.Guild.GetMemberAsync(victim.Id);

		try
		{
			// Dm the user, let them know that they were kicked.
			await victimMember.SendMessageAsync(phrase + $" - {context.User.Mention}");
		}
		// Sometimes people have bots blocked or dm's turned off. If that's the case, we catch the exception and ignore it.
		catch (UnauthorizedException)
		{
			discordInteractionResponseBuilder.Content = $"Error: Failed to message the victim! They don't have their dm's open.";
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
			return;
		}

		discordInteractionResponseBuilder.Content = $"Sucessfully messaged the user:\n{phrase} - {context.User.Mention}";
		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
		return;
	}
}
