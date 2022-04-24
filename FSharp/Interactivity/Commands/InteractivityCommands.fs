namespace Interactivity.Commands.InteractivityCommands

open System
open System.Threading.Tasks
open DisCatSharp
open DisCatSharp.ApplicationCommands
open DisCatSharp.Entities
open DisCatSharp.Enums
open DisCatSharp.Interactivity.Extensions

[<Sealed>]
type Interactivity() =
    inherit ApplicationCommandsModule()

    [<SlashCommand("message", "Wait for a message")>]
    static member Message(ctx: InteractionContext) =
        task {
            let interactivity = ctx.Client.GetInteractivity()

            let builder =
                "Please send a message"
                |> DiscordMessageBuilder().WithContent
                |> DiscordInteractionResponseBuilder

            do! ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder)

            let! result =
                interactivity.WaitForMessageAsync((fun m -> m.Author.Equals ctx.Member), TimeSpan.FromMinutes 5.0)

            let builder =
                DiscordWebhookBuilder()
                    .WithContent(
                        if result.TimedOut then
                            "Timed out"
                        else
                            result.Result.Content
                    )

            let! _ = ctx.EditResponseAsync builder

            if not result.TimedOut then
                do! result.Result.DeleteAsync()
        }
        :> Task

    [<SlashCommand("reaction", "Wait for a reaction")>]
    static member Reaction(ctx: InteractionContext) =
        task {
            let interactivity = ctx.Client.GetInteractivity()

            let emoji =
                DiscordEmoji.FromName(ctx.Client, ":white_check_mark:")

            let builder =
                $"To confirm, select the reaction %O{emoji}"
                |> DiscordMessageBuilder().WithContent
                |> DiscordInteractionResponseBuilder

            do! ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder)

            let! message = ctx.GetOriginalResponseAsync()
            do! message.CreateReactionAsync emoji

            let! result =
                interactivity.WaitForReactionAsync(
                    (fun react ->
                        react.Message.Equals message
                        && react.User.Equals ctx.Member
                        && react.Emoji.Equals emoji),
                    TimeSpan.FromMinutes 5.0
                )

            let! _ =
                if result.TimedOut then
                    "Timed out"
                else
                    $"You added a reaction: %O{emoji}"
                |> DiscordWebhookBuilder().WithContent
                |> ctx.EditResponseAsync

            ()
        }
        :> Task

    [<SlashCommand("button", "Wait for a button to be clicked")>]
    static member Button(ctx: InteractionContext) =
        task {
            let interactivity = ctx.Client.GetInteractivity()

            let buttons =
                [ ButtonStyle.Primary, "btn1", "Button 1", false, ":one:"
                  ButtonStyle.Secondary, "btn2", "Button 2", false, ":two:" ]
                |> List.map
                    (fun (style, customId, label, disabled, emoji) ->
                        DiscordButtonComponent(
                            style,
                            customId,
                            label,
                            disabled,
                            DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, emoji))
                        )
                        :> DiscordComponent)

            let builder =
                DiscordMessageBuilder()
                    .WithContent("Click on one of the buttons below.")
                    .AddComponents(buttons)
                |> DiscordInteractionResponseBuilder

            do! ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder)
            let! original = ctx.GetOriginalResponseAsync()
            let! result = interactivity.WaitForButtonAsync(original, ctx.Member, TimeSpan.FromMinutes 5.0)

            let! _ =
                if result.TimedOut then
                    "Timed out"
                else
                    let buttonName =
                        if result.Result.Id = "btn1" then
                            "first"
                        else
                            "second"

                    $"You pressed the %s{buttonName} button"
                |> DiscordWebhookBuilder().WithContent
                |> ctx.EditResponseAsync

            ()
        }
        :> Task

    [<SlashCommand("select_menu", "Wait for a select menu")>]
    static member SelectMenu(ctx: InteractionContext) =
        task {
            let interactivity = ctx.Client.GetInteractivity()

            let options =
                let items =
                    [ "one", "One", "", false, ":one:"
                      "two", "Two", "", false, ":two:"
                      "three", "Three", "", false, ":three:" ]
                    |> List.map
                        (fun (label, value, description, isDefault, emoji) ->
                            DiscordSelectComponentOption(
                                label,
                                value,
                                description,
                                isDefault,
                                DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, emoji))
                            ))

                DiscordSelectComponent("select_menu", "Choose something", items)

            let builder =
                DiscordMessageBuilder()
                    .WithContent("Choose something from the select menu below.")
                    .AddComponents(options)
                |> DiscordInteractionResponseBuilder

            do! ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder)
            let! message = ctx.GetOriginalResponseAsync()
            let! result = interactivity.WaitForSelectAsync(message, ctx.Member, "select_menu", TimeSpan.FromMinutes 5.0)

            let! _ =
                if result.TimedOut then
                    "Timed out"
                else
                    $"You selected %O{result.Result.Values.[0]}"
                |> DiscordWebhookBuilder().WithContent
                |> ctx.EditResponseAsync

            ()
        }
        :> Task

    [<SlashCommand("random", "Wait for a button to be pressed after executing a command")>]
    static member Random(ctx: InteractionContext) =
        task {
            let buttons =
                [ ButtonStyle.Danger, "rand_cancel", "Cancel", false, ":stop_button:"
                  ButtonStyle.Success, "rand_next", "Next", false, ":arrow_forward:" ]
                |> List.map
                    (fun (style, customId, label, disabled, emoji) ->
                        DiscordButtonComponent(
                            style,
                            customId,
                            label,
                            disabled,
                            DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, emoji))
                        )
                        :> DiscordComponent)

            let random = Random()

            let builder =
                DiscordMessageBuilder()
                    .WithContent(random.Next(0, 100) |> string)
                    .AddComponents(buttons)
                |> DiscordInteractionResponseBuilder

            do! ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder)
        }
        :> Task
