using System.Globalization;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace DisCatSharp.Examples.ApplicationCommands.Commands;

/// <summary>
///     Slash commands with arguments
/// </summary>
public class RoleInfo : ApplicationCommandsModule
{
	/// <summary>
	///     Get information about the role.
	/// </summary>
	/// <param name="context">Interaction context</param>
	/// <param name="discordRole">The role to get information on</param>
	[SlashCommand("role_info", "Gets general information about a role.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task CommandAsync(
		InteractionContext context,
		// Option adds an argument to the command
		[Option("role", "The role to get information on.")]
		DiscordRole discordRole
	)
	{
		// This is a guild command, make sure nobody can execute this command in dm's
		if (context.Guild == null)
		{
			DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new()
			{
				Content = "Error: This is a guild command!",

				// Make all errors visible to just the user, makes the channel more clean for everyone else.
				IsEphemeral = true
			};

			// Send the response.
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
			return;
		}

		// InteractionResponseType.DeferredChannelMessageWithSource let's the user know that we got the command, and that the bot is "thinking" (or really just taking a long time to execute).
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new()
		{
			// Since this is a richer status card, let's make it to where only the user can see the command results.
			IsEphemeral = true
		});

		var accentColor = discordRole.Color.Value != 0x000000 ? discordRole.Color : new DiscordColor("#7b84d1");
		var header = new DiscordSectionComponent(
		[
			new DiscordTextDisplayComponent($"## Role info · {discordRole.Name}"),
			new DiscordTextDisplayComponent($$"""
				- Mention: {{discordRole.Mention}}
				- Position: `{{discordRole.Position}}`
				""")
		]);

		if (context.Guild.IconUrl != null)
			// CHALLENGE: Replace the jpg to the highest resolution png file using the Discord API.
			header.WithThumbnailComponent(context.Guild.IconUrl, $"{context.Guild.Name} icon");

		// CHALLENGE: Surface live guild data such as member counts or channel overrides so the card goes beyond static metadata.
		var card = new DiscordContainerComponent(accentColor: accentColor)
			.AddComponent(header)
			.AddComponent(new DiscordTextDisplayComponent($$"""
				- Color: `{{discordRole.Color}}`
				- Created at: `{{discordRole.CreationTimestamp.UtcDateTime.ToString("MMMM dd, yyyy HH:mm:ss 'UTC'", CultureInfo.InvariantCulture)}}`
				- Hoisted: `{{discordRole.IsHoisted}}`
				- Managed: `{{discordRole.IsManaged}}`
				- Mentionable: `{{discordRole.IsMentionable}}`
				- Role ID: `{{discordRole.Id.ToString(CultureInfo.InvariantCulture)}}`
				- Permissions: {{discordRole.Permissions.ToPermissionString()}}
				"""))
			.AddComponent(new DiscordTextDisplayComponent("> Compare these static details with live guild data once you are ready to grow the sample."));

		// Change our previous "thinking" response to our actual result.
		await context.EditResponseAsync(new DiscordWebhookBuilder().WithV2Components().AddComponents(card));
	}
}
