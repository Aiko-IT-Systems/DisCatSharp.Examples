using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DisCatSharp.Examples.ApplicationCommands.Commands;

/// <summary>
/// Group slash commands + optional arguments.
/// Notice how the GroupSlashCommand attribute isn't on this class, but the subclass.
/// </summary>
public class Tags : ApplicationCommandsModule
{
	/// <summary>
	/// Also inherits ApplicationCommandsModule
	/// SlashCommandGroup is what makes group commands!
	/// </summary>
	[SlashCommandGroup("tag_test", "Sends, modifies or deletes a premade message.")]
	public class RealTags : ApplicationCommandsModule
	{
		/// <summary>
		/// Tags will be cleared when the bot restarts.
		/// </summary>
		public static List<Tag> Tags { get; private set; } = new();

		/// <summary>
		/// Sends a premade message.
		/// </summary>
		/// <param name="context">Interaction context</param>
		/// <param name="tagName">The name of the tag to send</param>
		[SlashCommand("send", "Sends a premade message.")]
		public static async Task SendAsync(InteractionContext context, [Autocomplete(typeof(TagsAutocompleteProvider)), Option("name", "The name of the tag to send", true)] string tagName)
		{
			DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();
			// This is a guild command, make sure nobody can execute this command in dm's
			if (context.Guild == null)
			{
				discordInteractionResponseBuilder.Content = "Error: This is a guild command!";
				discordInteractionResponseBuilder.IsEphemeral = true;
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
				return;
			}

			// Since everyone around the world uses Discord
			tagName = tagName.ToLowerInvariant();

			// Sort through the created tags.
			var tag = Tags.FirstOrDefault(listTag => listTag.GuildId == context.Guild.Id && listTag.Name == tagName);

			// If the tag wasn't found, let the user know.
			if (tag == null)
			{
				discordInteractionResponseBuilder.Content = $"Error: Tag {tagName.Sanitize().InlineCode()} not found!";

				// Hide the message from everyone else to prevent public embarassment and to create a cleaner chat for everyone else.
				discordInteractionResponseBuilder.IsEphemeral = true;

				// Send the error message.
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
				return;
			}

			// The tag was found, send it!
			discordInteractionResponseBuilder.Content = tag.Content;

			// Send the tag!
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
		}

		/// <summary>
		/// Creates a new tag for everyone to use.
		/// </summary>
		/// <param name="context">Interaction context</param>
		/// <param name="tagName">What to call the new tag</param>
		/// <param name="tagContent">What to fill the tag with</param>
		[SlashCommand("create", "Creates a new tag for everyone to use.")]
		public static async Task CreateAsync(
			InteractionContext context,
			[Option("name", "What to call the new tag.")] string tagName,
			// Be giving the tagContent an optional argument in C#, it becomes an optional argument in Discord too!
			[Option("content", "What to fill the tag with.")] string tagContent = "I'm an empty tag :("
		)
		{
			DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();
			if (context.Guild == null)
			{
				discordInteractionResponseBuilder.Content = "Error: This is a guild command!";
				discordInteractionResponseBuilder.IsEphemeral = true;
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
				return;
			}

			var tag = Tags.FirstOrDefault(listTag => listTag.GuildId == context.Guild.Id && listTag.Name == tagName.ToLowerInvariant());

			// The tag already exists, we can't allow duplicates to happen.
			if (tag != null)
			{
				discordInteractionResponseBuilder.Content = $"Error: Tag {tagName.Sanitize().InlineCode()} already exists!";
				discordInteractionResponseBuilder.IsEphemeral = true;
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
				return;
			}

			tag = new()
			{
				Name = tagName.ToLowerInvariant(),
				GuildId = context.Guild.Id,
				OwnerId = context.User.Id,

				// CHALLENGE: Escape user and role pings!
				Content = tagContent
			};
			Tags.Add(tag);
			discordInteractionResponseBuilder.Content = $"Tag {tag.Name.Sanitize().InlineCode()} has been created!";

			// Send the response.
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
		}

		/// <summary>
		/// Deletes a tag from the guild.
		/// </summary>
		/// <param name="context">Interaction context</param>
		/// <param name="tagName">The name of the tag that should be deleted</param>
		[SlashCommand("delete", "Deletes a tag from the guild.")]
		public static async Task DeleteAsync(InteractionContext context, [Option("name", "The name of the tag that should be deleted.")] string tagName)
		{
			DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();
			if (context.Guild == null)
			{
				discordInteractionResponseBuilder.Content = "Error: This is a guild command!";
				discordInteractionResponseBuilder.IsEphemeral = true;
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
				return;
			}

			var tag = Tags.FirstOrDefault(listTag => listTag.GuildId == context.Guild.Id && listTag.Name == tagName.ToLowerInvariant());
			if (tag == null)
			{
				discordInteractionResponseBuilder.Content = $"Error: Tag {tagName.Sanitize().InlineCode()} not found!";
				discordInteractionResponseBuilder.IsEphemeral = true;
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
			}

			if (tag.OwnerId == context.User.Id || tag.OwnerId == context.Guild.OwnerId
			                                   // This means if the command executor has the ManageMessages permission in one channel, they can delete a guild tag.
			                                   // CHALLENGE: Get the guild permissions of the member instead of the channel permissions.
			                                   || context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.ManageMessages))
			{
				Tags.Remove(tag);
				discordInteractionResponseBuilder.Content = $"Tag {tagName.Sanitize().InlineCode()} was deleted!";
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
			}
			else
			{
				discordInteractionResponseBuilder.Content = $"Error: Tag {tagName.Sanitize().InlineCode()} could not be deleted! You're missing the {"ManageMessages".InlineCode()} permission!";
				discordInteractionResponseBuilder.IsEphemeral = true;
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
			}
		}
	}
}

/// <summary>
/// Generates a list of options.
/// Unlike the ChoiceProvider, which generates it once during the registration of commands, this generates it whenever someone writes a command.
/// </summary>
internal class TagsAutocompleteProvider : IAutocompleteProvider
{
	/// <summary>
	/// The method in which the list is generated. You can do whatever you want here, the main thing is to get a list with options.
	/// </summary>
	/// <param name="context">Special context of autocomplete.</param>
	/// <returns>List of the options</returns>
#pragma warning disable 1998
	public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext context)
#pragma warning restore 1998
	{
		if (context.FocusedOption == null)
		{
			return null;
		}

		return Tags.RealTags.Tags.Where(listTag => listTag.GuildId == context.Interaction.Guild.Id)
			.Select(item => new DiscordApplicationCommandAutocompleteChoice(item.Name, item.Name)).ToList();
	}
}