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
///     Group slash commands + optional arguments.
///     Notice how the GroupSlashCommand attribute isn't on this class, but the subclass.
/// </summary>
public class Tags : ApplicationCommandsModule
{
	private static TagStore GetTagStore(IServiceProvider services)
		=> services.GetRequiredService<TagStore>();

	private static DiscordInteractionResponseBuilder CreateErrorResponse(string content)
		=> new()
		{
			Content = content,
			IsEphemeral = true
		};

	// CHALLENGE: Add edit or delete buttons to this card and teach how to keep the display in sync after those actions.
	private static DiscordContainerComponent BuildTagCard(Tag tag)
		=> new DiscordContainerComponent(accentColor: new DiscordColor("#FEE75C"))
			.AddComponent(new DiscordTextDisplayComponent($"## Tag · {tag.Name}"))
			.AddComponent(new DiscordTextDisplayComponent(tag.Content))
			.AddComponent(new DiscordTextDisplayComponent($$"""
				- Owner: <@{{tag.OwnerId}}>
				- Uses: `{{tag.UseCount}}`
				- Created: `{{tag.CreatedAt:u}}`
				- Last used: `{{tag.LastUsedAt?.ToString("u") ?? "Never"}}`
				"""));

	/// <summary>
	///     Also inherits ApplicationCommandsModule
	///     SlashCommandGroup is what makes group commands!
	/// </summary>
	[SlashCommandGroup("tag_test", "Sends, modifies or deletes a premade message.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
	public class RealTags : ApplicationCommandsModule
	{
		/// <summary>
		///     Sends a premade message.
		/// </summary>
		/// <param name="context">Interaction context</param>
		/// <param name="tagName">The name of the tag to send</param>
		[SlashCommand("send", "Sends a premade message.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
		public static async Task SendAsync(InteractionContext context, [Autocomplete(typeof(TagsAutocompleteProvider)), Option("name", "The name of the tag to send", true)] string tagName)
		{
			if (context.Guild == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse("Error: This is a guild command!"));
				return;
			}

			var tagStore = GetTagStore(context.Services);
			var normalizedTagName = TagStore.NormalizeName(tagName);
			if (!tagStore.TryTouch(context.Guild.Id, normalizedTagName, out var tag))
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse($"Error: Tag {normalizedTagName.Sanitize().InlineCode()} not found!"));
				return;
			}

			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithV2Components()
				.WithAllowedMentions([])
				.AddComponents(BuildTagCard(tag)));
		}

		/// <summary>
		///     Creates a new tag for everyone to use.
		/// </summary>
		/// <param name="context">Interaction context</param>
		/// <param name="tagName">What to call the new tag</param>
		/// <param name="tagContent">What to fill the tag with</param>
		[SlashCommand("create", "Creates a new tag for everyone to use.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
		public static async Task CreateAsync(
			InteractionContext context,
			[Option("name", "What to call the new tag.")] string tagName,
			[Option("content", "What to fill the tag with.")] string tagContent = "I'm an empty tag :("
		)
		{
			if (context.Guild == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse("Error: This is a guild command!"));
				return;
			}

			var normalizedTagName = TagStore.NormalizeName(tagName);
			var tagStore = GetTagStore(context.Services);
			if (tagStore.Contains(context.Guild.Id, normalizedTagName))
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse($"Error: Tag {normalizedTagName.Sanitize().InlineCode()} already exists!"));
				return;
			}

			tagStore.TryCreate(new Tag
			{
				Name = normalizedTagName,
				GuildId = context.Guild.Id,
				OwnerId = context.User.Id,
				Content = string.IsNullOrWhiteSpace(tagContent) ? "I'm an empty tag :(" : tagContent.Trim()
			});

			// CHALLENGE: persist tags to JSON or a small database once you're ready to move beyond in-memory samples.
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
			{
				Content = $"Tag {normalizedTagName.Sanitize().InlineCode()} has been created!"
			});
		}

		/// <summary>
		///     Deletes a tag from the guild.
		/// </summary>
		/// <param name="context">Interaction context</param>
		/// <param name="tagName">The name of the tag that should be deleted</param>
		[SlashCommand("delete", "Deletes a tag from the guild.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
		public static async Task DeleteAsync(InteractionContext context, [Option("name", "The name of the tag that should be deleted.")] string tagName)
		{
			if (context.Guild == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse("Error: This is a guild command!"));
				return;
			}

			var tagStore = GetTagStore(context.Services);
			var normalizedTagName = TagStore.NormalizeName(tagName);
			var tag = tagStore.Get(context.Guild.Id, normalizedTagName);
			if (tag == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse($"Error: Tag {normalizedTagName.Sanitize().InlineCode()} not found!"));
				return;
			}

			if (tag.OwnerId == context.User.Id || context.User.Id == context.Guild.OwnerId || context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.ManageMessages))
			{
				tagStore.TryDelete(context.Guild.Id, normalizedTagName, out _);
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
				{
					Content = $"Tag {normalizedTagName.Sanitize().InlineCode()} was deleted!"
				});
				return;
			}

			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse($"Error: Tag {normalizedTagName.Sanitize().InlineCode()} could not be deleted! You're missing the {"ManageMessages".InlineCode()} permission!"));
		}

		/// <summary>
		///     Lists the most active tags in the current guild.
		/// </summary>
		/// <param name="context">Interaction context</param>
		[SlashCommand("list", "Lists the tags that currently exist in this guild.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
		public static async Task ListAsync(InteractionContext context)
		{
			if (context.Guild == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse("Error: This is a guild command!"));
				return;
			}

			var tags = GetTagStore(context.Services).List(context.Guild.Id);
			if (tags.Count == 0)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse("There are no tags yet. Create one with /tag_test create."));
				return;
			}

			var preview = string.Join(Environment.NewLine, tags.Take(10)
				.Select(tag => $"• {tag.Name.Sanitize().InlineCode()} — {tag.UseCount} uses by <@{tag.OwnerId}>"));

			// CHALLENGE: Add pagination or category filters so the catalog still feels practical once a guild has more than a handful of tags.
			var card = new DiscordContainerComponent(accentColor: new DiscordColor("#FEE75C"))
				.AddComponent(new DiscordTextDisplayComponent($"## Tag catalog for {context.Guild.Name}"))
				.AddComponent(new DiscordTextDisplayComponent(preview))
				.AddComponent(new DiscordTextDisplayComponent($$"""
					- Total tags: `{{tags.Count}}`
					- Preview size: `{{Math.Min(tags.Count, 10)}}`

					> This preview intentionally stays small so you can grow it into a richer catalog later.
					"""));

			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithV2Components()
				.WithAllowedMentions([])
				.AddComponents(card));
		}

		/// <summary>
		///     Shows metadata for a single tag.
		/// </summary>
		/// <param name="context">Interaction context</param>
		/// <param name="tagName">The tag to inspect.</param>
		[SlashCommand("info", "Shows metadata for a single tag.", allowedContexts: [InteractionContextType.Guild], integrationTypes: [ApplicationCommandIntegrationTypes.GuildInstall])]
		public static async Task InfoAsync(InteractionContext context, [Autocomplete(typeof(TagsAutocompleteProvider)), Option("name", "The tag to inspect.")] string tagName)
		{
			if (context.Guild == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse("Error: This is a guild command!"));
				return;
			}

			var tag = GetTagStore(context.Services).Get(context.Guild.Id, TagStore.NormalizeName(tagName));
			if (tag == null)
			{
				await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, CreateErrorResponse($"Error: Tag {tagName.Sanitize().InlineCode()} not found!"));
				return;
			}

			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithV2Components()
				.WithAllowedMentions([])
				.AddComponents(BuildTagCard(tag)));
		}
	}
}

/// <summary>
///     Generates a list of options.
///     Unlike the ChoiceProvider, which generates it once during the registration of commands, this generates it whenever
///     someone writes a command.
/// </summary>
internal sealed class TagsAutocompleteProvider : IAutocompleteProvider
{
	/// <summary>
	///     The method in which the list is generated. You can do whatever you want here, the main thing is to get a list with
	///     options.
	/// </summary>
	/// <param name="context">Special context of autocomplete.</param>
	/// <returns>List of the options</returns>
	public Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext context)
	{
		if (context.Guild == null)
			return Task.FromResult<IEnumerable<DiscordApplicationCommandAutocompleteChoice>>([]);

		var tagStore = context.Services.GetRequiredService<TagStore>();
		var searchTerm = context.FocusedOption?.Value?.ToString() ?? string.Empty;
		var tags = tagStore.List(context.Guild.Id)
			.Where(tag => searchTerm.Length == 0 || tag.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
			.Take(25)
			.Select(tag => new DiscordApplicationCommandAutocompleteChoice(tag.Name, tag.Name))
			.ToList();

		return Task.FromResult<IEnumerable<DiscordApplicationCommandAutocompleteChoice>>(tags);
	}
}
