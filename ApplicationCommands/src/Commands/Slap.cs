using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;

namespace DisCatSharp.Examples.ApplicationCommands.Commands;

/// <summary>
///     Shows how to use DiscordEntities + permissions.
/// </summary>
public class Slap : ApplicationCommandsModule
{
	/// <summary>
	///     Checks to see if the user has the KickMembers permission. If they do, it executes the command. If they don't, the
	///     command fails silently.
	/// </summary>
	/// <param name="context">Interaction context</param>
	public override Task<bool> BeforeSlashExecutionAsync(InteractionContext context)
		=> Task.FromResult(context.Member.Permissions.HasPermission(Permissions.KickMembers));

	/// <summary>
	///     Checks to see if the user has the KickMembers permission. If they do, it executes the command. If they don't, the
	///     command fails silently.
	/// </summary>
	/// <param name="context">Interaction context</param>
	public override Task<bool> BeforeContextMenuExecutionAsync(ContextMenuContext context)
		=> Task.FromResult(context.Member.Permissions.HasPermission(Permissions.KickMembers));

	/// <summary>
	///     Kick user from the server.
	/// </summary>
	/// <param name="context">Interaction context</param>
	/// <param name="victim">User to be kicked</param>
	[SlashCommand("slap", "Slaps the user so hard, it kicks them out of the guild.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task CommandAsync(InteractionContext context, [Option("victim", "Who should I slap?")] DiscordUser victim = null)
	{
		// For the sake of examples, if the user didn't provide someone to kick, let's assume that they kicked themselves.
		victim ??= context.User;

		if (context.Guild == null)
		{
			DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new()
			{
				Content = "Error: This is a guild command!",
				IsEphemeral = true
			};
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
			return;
		}

		// Let the user know that the bot is "thinking." We do this since having the bot dm people can take a long time.
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		var dmMessage = $"You were slapped out of the guild {context.Guild.Name} by {context.User.Mention}!";

		// CHALLENGE 1: Test if the user is in the guild.
		// CHALLENGE 2: Try testing if the bot can kick the victim before doing it. One less request to the Discord API.
		var victimMember = await context.Guild.GetMemberAsync(victim.Id);

		try
		{
			// Dm the user, let them know that they were kicked.
			await victimMember.SendMessageAsync(dmMessage);
		}
		// Sometimes people have bots blocked or dm's turned off. If that's the case, we catch the exception and ignore it.
		catch (UnauthorizedException)
		{ }

		// Actually kick the user from the guild.
		await victimMember.RemoveAsync();

		DiscordWebhookBuilder discordWebhookBuilder = new()
		{
			Content = $"{victim.Mention} was slapped so hard, that they flew out of the guild!"
		};
		await context.EditResponseAsync(discordWebhookBuilder);
	}

	/// <summary>
	///     Kick user from the server (context menu version).
	///     Note that several types of commands (slash/user/message) can be used in one class.
	/// </summary>
	/// <param name="context">Context menu context</param>
	[ContextMenu(ApplicationCommandType.User, "Slap", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task CommandAsync(ContextMenuContext context)
	{
		if (context.Guild == null)
		{
			DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new()
			{
				Content = "Error: This is a guild command!",
				IsEphemeral = true
			};
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
			return;
		}

		// Let the user know that the bot is "thinking." We do this since having the bot dm people can take a long time.
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		var dmMessage = $"You were slapped out of the guild {context.Guild.Name} by {context.User.Mention}!";

		// CHALLENGE 1: Test if the user is in the guild.
		// CHALLENGE 2: Try testing if the bot can kick the victim before doing it. One less request to the Discord API.
		var victimMember = await context.Guild.GetMemberAsync(context.TargetUser.Id);

		try
		{
			// Dm the user, let them know that they were kicked.
			await victimMember.SendMessageAsync(dmMessage);
		}
		// Sometimes people have bots blocked or dm's turned off. If that's the case, we catch the exception and ignore it.
		catch (UnauthorizedException)
		{ }

		// Actually kick the user from the guild.
		await victimMember.RemoveAsync();

		DiscordWebhookBuilder discordWebhookBuilder = new()
		{
			Content = $"{context.TargetUser.Mention} was slapped so hard, that they flew out of the guild!"
		};
		await context.EditResponseAsync(discordWebhookBuilder);
	}
}
