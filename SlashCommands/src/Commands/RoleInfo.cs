using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace SlashCommands.Commands
{
    public class RoleInfo : SlashCommandModule
    {
        public override Task BeforeExecutionAsync(InteractionContext context)
        {
            if (context.Guild == null)
            {
                context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new()
                {
                    Content = "Error: This command can only be used in a guild!",
                    IsEphemeral = true
                });
            }

            return Task.CompletedTask;
        }

        [SlashCommand("role_info", "Gets general information about a role.")]
        public static async Task Command(InteractionContext context,
            // Option adds an argument to the command
            [Option("role", "The role to get information on.")] DiscordRole discordRole)
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
                // Since this is a large embed, let's make it to where only the user can see the command results.
                IsEphemeral = true
            });

            DiscordEmbedBuilder embedBuilder = new()
            {
                Title = $"Role Info for {discordRole.Name}",

                // If the role has a color, set the embed to the role's color. Otherwise, set it to a defined hex color.
                // The @everyone role should always use the defined hex color
                Color = discordRole.Color.Value != 0x000000 ? discordRole.Color : new DiscordColor("#7b84d1")
            };

            // If the guild has a custom guild icon, set the embed's thumbnail to that icon.
            if (context.Guild.IconUrl != null)
            {
                // CHALLENGE: Replace the jpg to the highest resolution png file using the Discord API.
                embedBuilder.WithThumbnail(context.Guild.IconUrl);
            }

            // Add fields to the embed, giving general information about the role that isn't typically available through the normal client.
            embedBuilder.AddField("Color", discordRole.Color.ToString(), true);

            // We use CultureInfo.InvariantCulture since Discord is used by everyone around the world.
            embedBuilder.AddField("Created At", discordRole.CreationTimestamp.UtcDateTime.ToString("MMMM dd, yyyy HH:mm:ss 'UTC'", CultureInfo.InvariantCulture), true);
            embedBuilder.AddField("Hoisted", discordRole.IsHoisted.ToString(), true);
            embedBuilder.AddField("Is Managed", discordRole.IsManaged.ToString(), true);
            embedBuilder.AddField("Is Mentionable", discordRole.IsMentionable.ToString(), true);
            embedBuilder.AddField("Role Id", discordRole.Id.ToString(CultureInfo.InvariantCulture), true);
            embedBuilder.AddField("Role Name", discordRole.Name, true);
            embedBuilder.AddField("Role Position", discordRole.Position.ToString(), true);
            embedBuilder.AddField("Permissions", discordRole.Permissions.ToPermissionString(), false);

            // Create the message builder.
            DiscordWebhookBuilder messageBuilder = new();

            // Didn't use initializer since the Embeds property is a readonly list.
            messageBuilder.AddEmbed(embedBuilder);

            // Change our previous "thinking" response to our actual result.
            await context.EditResponseAsync(messageBuilder);
        }
    }
}
