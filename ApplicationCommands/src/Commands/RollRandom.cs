using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace DisCatSharp.Examples.ApplicationCommands.Commands;

/// <summary>
///     Slash commands with enums.
/// </summary>
public class RollRandom : ApplicationCommandsModule
{
	/// <summary>
	///     By using an enum with the ChoiceName attribute, we can allow users to pick from a list without having to deal with
	///     arbiturary user input.
	/// </summary>
	public enum RandomChoice
	{
		[ChoiceName("Number")]
		Number,

		[ChoiceName("Role")]
		DiscordRole,

		[ChoiceName("User")]
		DiscordUser
	}

	/// <summary>
	///     Random command.
	/// </summary>
	/// <param name="context">Interaction context</param>
	/// <param name="randomChoice">Should a random number, role or user be picked?</param>
	[SlashCommand("roll_random", "Gets a random person, role or number.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public static async Task CommandAsync(InteractionContext context, [Option("random_choice", "Should a random number, role or user be picked?")] RandomChoice randomChoice = RandomChoice.Number)
	{
		var random = context.Services.GetRequiredService<Random>();
		if (randomChoice != RandomChoice.Number && context.Guild == null)
		{
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
			{
				Content = $"Error: {randomChoice} cannot be used outside of a guild!",
				IsEphemeral = true
			});
			return;
		}

		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());

		var detailLines = new List<string>();
		var webhook = new DiscordWebhookBuilder().WithAllowedMentions([]).WithV2Components();

		switch (randomChoice)
		{
			case RandomChoice.Number:
				detailLines.Add($"Result: 🎲 `{random.Next(1, 101)}`");
				detailLines.Add("Mode: `Number`");
				detailLines.Add("Range: `1-100`");
				break;
			case RandomChoice.DiscordRole:
				var roles = new List<DiscordRole>(context.Guild.Roles.Values);
				var selectedRole = roles[random.Next(roles.Count)];
				detailLines.Add($"Selected role: {selectedRole.Mention}");
				detailLines.Add("Mode: `Role`");
				detailLines.Add($"Sample size: `{roles.Count}`");
				break;
			case RandomChoice.DiscordUser:
				var guildMembers = await context.Guild.GetAllMembersAsync();
				var selectedMember = new List<DiscordMember>(guildMembers)[random.Next(guildMembers.Count)];
				detailLines.Add($"Selected member: {selectedMember.Mention}");
				detailLines.Add("Mode: `User`");
				detailLines.Add($"Sample size: `{guildMembers.Count}`");
				break;
			default:
				detailLines.Add("Error: Choice options are Number, Role or User! Please pick one of those.");
				break;
		}

		// CHALLENGE: Track the last few results per user and add buttons or follow-ups so rerolls become a real mini flow.
		var card = new DiscordContainerComponent(accentColor: new DiscordColor("#57F287"))
			.AddComponent(new DiscordTextDisplayComponent("## Random pick"))
			.AddComponent(new DiscordTextDisplayComponent(string.Join(Environment.NewLine, detailLines.Select(static line => $"- {line}"))))
			.AddComponent(new DiscordTextDisplayComponent("> Try adding buttons next if you want rerolls without re-running the slash command."));

		await context.EditResponseAsync(webhook.AddComponents(card));
	}
}
