using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.SlashCommands;

namespace DisCatSharp.Examples.SlashCommands.Commands
{
    // Notice how the GroupSlashCommand attribute isn't on this class, but the subclass
    public class Tags : SlashCommandModule
    {
        // Also inherits SlashCommandModule
        // SlashCommandGroup is what makes group commands!
        [SlashCommandGroup("tag_test", "Sends, modifies or deletes a premade message.")]
        public class RealTags : SlashCommandModule
        {
            // Tags will be cleared when the bot restarts
            public static List<Tag> Tags { get; private set; } = new();

            [SlashCommand("send", "Sends a premade message.")]
            public static async Task Send(InteractionContext context, [Option("name", "The name of the tag to send")] string tagName)
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
                Tag tag = Tags.FirstOrDefault(listTag => listTag.GuildId == context.Guild.Id && listTag.Name == tagName);

                // If the tag wasn't found, let the user know.
                if (tag == null)
                {
                    discordInteractionResponseBuilder.Content = $"Error: Tag {Formatter.InlineCode(Formatter.Sanitize(tagName))} not found!";

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

            [SlashCommand("create", "Creates a new tag for everyone to use.")]
            public static async Task Create(InteractionContext context, [Option("name", "What to call the new tag.")] string tagName,
                // Be giving the tagContent an optional argument in C#, it becomes an optional argument in Discord too!
                [Option("content", "What to fill the tag with.")] string tagContent = "I'm an empty tag :(")
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();
                if (context.Guild == null)
                {
                    discordInteractionResponseBuilder.Content = "Error: This is a guild command!";
                    discordInteractionResponseBuilder.IsEphemeral = true;
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
                    return;
                }

                Tag tag = Tags.FirstOrDefault(listTag => listTag.GuildId == context.Guild.Id && listTag.Name == tagName.ToLowerInvariant());

                // The tag already exists, we can't allow duplicates to happen.
                if (tag != null)
                {
                    discordInteractionResponseBuilder.Content = $"Error: Tag {Formatter.InlineCode(Formatter.Sanitize(tagName))} already exists!";
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
                discordInteractionResponseBuilder.Content = $"Tag {Formatter.InlineCode(Formatter.Sanitize(tag.Name))} has been created!";

                // Send the response.
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
            }

            [SlashCommand("delete", "Deletes a tag from the guild.")]
            public static async Task Delete(InteractionContext context, [Option("name", "The name of the tag that should be deleted.")] string tagName)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();
                if (context.Guild == null)
                {
                    discordInteractionResponseBuilder.Content = "Error: This is a guild command!";
                    discordInteractionResponseBuilder.IsEphemeral = true;
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
                    return;
                }

                Tag tag = Tags.FirstOrDefault(listTag => listTag.GuildId == context.Guild.Id && listTag.Name == tagName.ToLowerInvariant());
                if (tag == null)
                {
                    discordInteractionResponseBuilder.Content = $"Error: Tag {Formatter.InlineCode(Formatter.Sanitize(tagName))} not found!";
                    discordInteractionResponseBuilder.IsEphemeral = true;
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
                }


                if (tag.OwnerId == context.User.Id || tag.OwnerId == context.Guild.OwnerId
                    // This means if the command executor has the ManageMessages permission in one channel, they can delete a guild tag.
                    // CHALLENGE: Get the guild permissions of the member instead of the channel permissions.
                    || context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.ManageMessages))
                {
                    Tags.Remove(tag);
                    discordInteractionResponseBuilder.Content = $"Tag {Formatter.InlineCode(Formatter.Sanitize(tagName))} was deleted!";
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
                }
                else
                {
                    discordInteractionResponseBuilder.Content = $"Error: Tag {Formatter.InlineCode(Formatter.Sanitize(tagName))} could not be deleted! You're missing the {Formatter.InlineCode("ManageMessages")} permission!";
                    discordInteractionResponseBuilder.IsEphemeral = true;
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
                }
            }
        }
    }
}