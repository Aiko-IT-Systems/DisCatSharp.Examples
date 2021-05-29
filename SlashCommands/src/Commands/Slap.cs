using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;

namespace SlashCommands.Commands
{
    public class Slap : SlashCommandModule
    {
        [SlashCommand("slap", "Slaps the user so hard, it kicks them out of the guild.")]
        public static async Task Command(InteractionContext context, [Option("victim", "Who should I slap?")] DiscordUser victim = null)
        {
            // For the sake of examples, if the user didn't provide someone to kick, let's assume that they kicked themselves.
            victim ??= context.User;

            if (context.Guild == null)
            {
                DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();
                discordInteractionResponseBuilder.Content = "Error: This is a guild command!";
                discordInteractionResponseBuilder.IsEphemeral = true;
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
                return;
            }

            // Let the user know that the bot is "thinking." We do this since having the bot dm people can take a long time.
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string dmMessage = $"You were slapped out of the guild {context.Guild.Name} by {context.User.Mention}!";

            // CHALLENGE 1: Test if the user is in the guild.
            // CHALLENGE 2: Try testing if the bot can kick the victim before doing it. One less request to the Discord API.
            DiscordMember victimMember = await context.Guild.GetMemberAsync(victim.Id);

            try
            {
                // Dm the user, let them know that they were kicked.
                await victimMember.SendMessageAsync(dmMessage);
            }
            // Sometimes people have bots blocked or dm's turned off. If that's the case, we catch the exception and ignore it.
            catch (UnauthorizedException) { }

            // Actually kick the user from the guild.
            await victimMember.RemoveAsync();

            DiscordWebhookBuilder discordWebhookBuilder = new()
            {
                Content = $"{victim.Mention} was slapped so hard, that they flew out of the guild!"
            };
            await context.EditResponseAsync(discordWebhookBuilder);
        }
    }
}