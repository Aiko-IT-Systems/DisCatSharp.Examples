namespace Basics.Commands.CommandsNext

open System.Threading
open System.Threading.Tasks
open DisCatSharp.CommandsNext
open DisCatSharp.CommandsNext.Attributes

[<Sealed>]
type Main() =
    inherit BaseCommandModule()

    /// <summary>
    /// Pings you.
    /// </summary>
    /// <param name="ctx">The command context.</param>
    [<Command("ping"); Description("Test ping :3")>]
    member _.PingAsync(ctx: CommandContext) =
        task {
            let! _ = ctx.RespondAsync $"%s{ctx.User.Mention}, Pong! :3 meow!"
            do! ctx.Message.DeleteAsync "Command Hide"
        } :> Task

    /// <summary>
    /// Shuts the bot down safely.
    /// </summary>
    /// <param name="ctx">The command context.</param>
    [<Command("shutdown"); Description("Shuts the bot down safely."); RequireOwner>]
    member _.ShutdownAsync(ctx: CommandContext) =
        task {
            (ctx.Services.GetService(typeof<CancellationTokenSource>) :?> CancellationTokenSource)
                .Cancel()

            let! _ = ctx.RespondAsync "Shutting down"
            do! ctx.Message.DeleteAsync "Command Hide"
        } :> Task

    /// <summary>
    /// Repeats what you say.
    /// </summary>
    /// <param name="ctx">The command context.</param>
    /// <param name="msg">The message to repeat.</param>
    [<Command("say"); Description("Repeats what you say.")>]
    member _.SayAsync (ctx: CommandContext, [<RemainingText>] msg) =
        task {
            let! _ = ctx.RespondAsync $"You said %s{msg}"
            do! ctx.Message.DeleteAsync "Command Hide"
        } :> Task
