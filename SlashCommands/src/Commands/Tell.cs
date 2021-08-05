using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Exceptions;
using DisCatSharp.SlashCommands;

namespace DisCatSharp.Examples.SlashCommands.Commands
{
    public class Tell : SlashCommandModule
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

        // Slash command registers the name and command description.
        [SlashCommand("tell", "Sends someone a message.")]
        public static async Task Command(InteractionContext context, [Option("victim", "Who the bot is messaging.")] DiscordUser victim,
            [Choice("ModMail", "Please contact ModMail.")]
            [Choice("Behaviour", "Please stop being rude.")]
            [Choice("Advertisement", "Please stop advertising.")]
            [Choice("SFW", "Please keep things SFW.")] [Option("Phrase", "What to message to the victim.")] string phrase)
        {
            DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();
            discordInteractionResponseBuilder.IsEphemeral = true;
            // CHALLENGE: Add other potentional staff permissions.
            if (!context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.Administrator))
            {
                discordInteractionResponseBuilder.Content = $"Error: You're not part of staff! Missing the {Formatter.InlineCode("Administrator")} permission!";
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, discordInteractionResponseBuilder);
                return;
            }

            DiscordMember victimMember = await context.Guild.GetMemberAsync(victim.Id);

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
}