namespace Basics.AppCommands.AppCommands

open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open DisCatSharp
open DisCatSharp.ApplicationCommands
open DisCatSharp.Entities

[<Sealed>]
type Main() =
    inherit ApplicationCommandsModule()

    /// <summary>
    /// Reports the bot's ping
    /// </summary>
    /// <param name="ctx">The interaction context.</param>
    [<SlashCommand("ping", "Sends the actual ping")>]
    static member Ping(ctx: InteractionContext) =
        task {
            do!
                ctx.CreateResponseAsync(
                    InteractionResponseType.DeferredChannelMessageWithSource,
                    DiscordInteractionResponseBuilder()
                        .AsEphemeral(true)
                        .WithContent("loading Ping, could take time...")
                )

            do! Task.Delay(2000)

            let! _ = ctx.Channel.SendMessageAsync $"Pong: %i{ctx.Client.Ping}"
            ()
        } :> Task

    [<SlashCommand("shutdown", "Shuts the bot down (restricted)")>]
    static member Shutdown(ctx: InteractionContext) =
        if ctx.Client.CurrentApplication.Owners
           |> Seq.contains ctx.User then
            task {
                let! _ =
                    ctx.CreateResponseAsync(
                        InteractionResponseType.DeferredChannelMessageWithSource,
                        DiscordInteractionResponseBuilder()
                            .WithContent("Shutdown request accepted.")
                    )

                do! Task.Delay 5000

                (ctx.Services.GetService(typeof<CancellationTokenSource>) :?> CancellationTokenSource)
                    .Cancel()

                let! _ =
                    ctx.EditResponseAsync(
                        DiscordWebhookBuilder()
                            .WithContent("Shutting down!")
                    )

                ()
            }
            :> Task
        else
            ctx.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                DiscordInteractionResponseBuilder()
                    .WithContent("Shutdown request denied.")
            )

    /// <summary>
    /// Repeats what you say.
    /// </summary>
    /// <param name="ctx">The command context.</param>
    /// <param name="message">The message to repeat.</param>
    [<SlashCommand("say", "Say something via embed")>]
    static member Say(ctx: InteractionContext, [<Option("message", "Message to repeat")>] message) =
        ctx.CreateResponseAsync(
            InteractionResponseType.ChannelMessageWithSource,
            DiscordInteractionResponseBuilder()
                .AsEphemeral(true)
                .AddEmbed(
                    DiscordEmbedBuilder()
                        .WithTitle("Repeat Message")
                        .WithDescription(message)
                        .WithFooter($"User: %s{ctx.Interaction.User.Username}")
                        .Build()
                )
        )

    /// <summary>
    /// Get the user's avatar.
    /// </summary>
    /// <param name="ctx">The command context.</param>
    /// <param name="user">The optional user.</param>
    [<SlashCommand("avatar", "Fetch a user's avatar")>]
    static member Avatar
        (
            ctx: InteractionContext,
            [<Option("user", "The user to fetch the avatar of"); Optional; DefaultParameterValue(null: DiscordUser)>] user: DiscordUser
        ) =
        let user = if isNull user then ctx.User else user

        let embed =
            DiscordEmbedBuilder()
                .WithTitle("Avatar")
                .WithImageUrl(user.AvatarUrl)
                .WithFooter($"Requested by %s{ctx.Member.DisplayName}", ctx.Member.AvatarUrl)
                .WithAuthor(user.Mention, user.AvatarUrl, user.AvatarUrl)

        ctx.CreateResponseAsync(
            InteractionResponseType.ChannelMessageWithSource,
            DiscordInteractionResponseBuilder()
                .AddEmbed(embed.Build())
        )
