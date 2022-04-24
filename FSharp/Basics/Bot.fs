module Basics.Bot

open System
open System.Threading
open System.Threading.Tasks
open Basics.AppCommands
open Basics.Commands
open DisCatSharp
open DisCatSharp.ApplicationCommands
open DisCatSharp.ApplicationCommands.EventArgs
open DisCatSharp.CommandsNext
open DisCatSharp.Common.Utilities
open DisCatSharp.Entities
open DisCatSharp.EventArgs
open DisCatSharp.Interactivity
open DisCatSharp.Interactivity.Enums
open DisCatSharp.Interactivity.Extensions
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

[<Literal>]
let prefix = "!"

type Bot =
    { Client: DiscordClient
      AppCommands: ApplicationCommandsExtension
      Interactivity: InteractivityExtension
      CommandsNext: CommandsNextExtension
      ShutdownRequest: CancellationTokenSource }
    interface IDisposable with
        member this.Dispose() =
            this.ShutdownRequest.Dispose()
            this.Client.Dispose()

type private ClientHandler<'a when 'a :> AsyncEventArgs> = AsyncEventHandler<DiscordClient, 'a>

/// <summary>
/// Perform an action and return a task encapsulating it.
/// </summary>
/// <param name="f">The action to perform.</param>
/// <param name="m">The object to apply to the action.</param>
let inline private taskR f m = task { f m } :> Task

[<RequireQualifiedAccess>]
module rec Bot =
    /// <summary>
    /// Creates an instance of the <see cref="Bot"/> record.
    /// </summary>
    /// <param name="token">The Discord token to use in configuration.</param>
    let create token =
        let shutdownRequest = new CancellationTokenSource()
        // Create an IServiceProvider with the shutdown request so it can be
        // used by commands.
        let services =
            ServiceCollection()
                .AddSingleton<CancellationTokenSource>(shutdownRequest)
                .BuildServiceProvider()

        let logLevel =
#if DEBUG
            LogLevel.Debug
#else
            LogLevel.Error
#endif
        let cfg = DiscordConfiguration()
        cfg.Token <- token
        cfg.TokenType <- TokenType.Bot
        cfg.AutoReconnect <- true
        cfg.MinimumLogLevel <- logLevel
        cfg.Intents <- DiscordIntents.AllUnprivileged
        cfg.MessageCacheSize <- 2048

        let client = new DiscordClient(cfg)

        let commandsNextCfg = CommandsNextConfiguration()
        commandsNextCfg.StringPrefixes <- [| prefix |]
        commandsNextCfg.CaseSensitive <- true
        commandsNextCfg.EnableMentionPrefix <- true
        commandsNextCfg.IgnoreExtraArguments <- true
        commandsNextCfg.DefaultHelpChecks <- null
        commandsNextCfg.EnableDefaultHelp <- true
        commandsNextCfg.EnableDms <- true
        let commandsNext = client.UseCommandsNext commandsNextCfg

        let appCommandsCfg = ApplicationCommandsConfiguration()
        appCommandsCfg.Services <- services
        let appCommands = client.UseApplicationCommands()

        let interactivityCfg = InteractivityConfiguration()
        interactivityCfg.PaginationBehaviour <- PaginationBehaviour.WrapAround
        interactivityCfg.PaginationDeletion <- PaginationDeletion.DeleteMessage
        interactivityCfg.PollBehaviour <- PollBehaviour.DeleteEmojis
        interactivityCfg.ButtonBehavior <- ButtonPaginationBehavior.Disable
        let interactivity = client.UseInteractivity interactivityCfg

        let bot =
            { Client = client
              AppCommands = appCommands
              Interactivity = interactivity
              CommandsNext = commandsNext
              ShutdownRequest = shutdownRequest }

        registerEventListener bot
        registerCommands bot
        bot

    let runAsync bot =
        task {
            do! bot.Client.ConnectAsync()

            while not <| bot.ShutdownRequest.IsCancellationRequested do
                do! Task.Delay 2000

            do! bot.Client.UpdateStatusAsync(null, UserStatus.Offline, Nullable())
            do! bot.Client.DisconnectAsync()
            do! Task.Delay 2500
            (bot :> IDisposable).Dispose()
        }

    let registerEventListener
        { Client = client
          AppCommands = appCommands
          CommandsNext = commandsNext }
        =
        // Basic client events
        let client_SocketOpened =
            ClientHandler<SocketEventArgs>(fun c _ -> taskR c.Logger.LogInformation "Socket opened")

        let client_SocketErrored =
            ClientHandler<SocketErrorEventArgs>
                (fun c e -> taskR c.Logger.LogError $"Socket has an error! %s{e.Exception.Message}")

        let client_SocketClosed =
            ClientHandler<SocketCloseEventArgs>
                (fun c e -> taskR c.Logger.LogInformation $"Socket closed: %s{e.CloseMessage}")

        let client_Heartbeated =
            ClientHandler<HeartbeatEventArgs>(fun c e -> taskR c.Logger.LogDebug $"Received heartbeat: %i{e.Ping}")

        let client_Ready =
            let handler (client: DiscordClient) (args: ReadyEventArgs) =
                seq {
                    yield $"Starting with prefix %s{prefix}"
                    yield $"Starting %s{client.CurrentUser.Username}#%s{client.CurrentUser.Discriminator}"
                    yield "Client ready!"
                    yield $"Shard: %u{client.ShardId}"
                    yield "Loaded commands:"

                    yield!
                        client.GetCommandsNext().RegisteredCommands
                        |> Seq.map (fun kvp -> $"    %s{kvp.Value.Name}")

                    yield "Bot ready!"
                }
                |> taskR (Seq.iter client.Logger.LogInformation)

            ClientHandler<ReadyEventArgs>(handler)

        let client_Resumed =
            ClientHandler<ReadyEventArgs>(fun c _ -> taskR c.Logger.LogInformation "Bot resumed!")

        client.add_SocketOpened client_SocketOpened
        client.add_SocketErrored client_SocketErrored
        client.add_SocketClosed client_SocketClosed
        client.add_Heartbeated client_Heartbeated
        client.add_Ready client_Ready
        client.add_Resumed client_Resumed

        // Commands next
        let commandsNext_CommandErrored =
            AsyncEventHandler<CommandsNextExtension, CommandErrorEventArgs>
                (fun c e ->
                    match e.Command with
                    | null -> e.Exception.Message
                    | _ -> $"%s{e.Command.Name}: %s{e.Exception.Message}"
                    |> taskR c.Client.Logger.LogError)

        commandsNext.add_CommandErrored commandsNext_CommandErrored

        // Application commands
        let discord_ApplicationCommandCreated =
            ClientHandler<ApplicationCommandEventArgs>
                (fun c e ->
                    taskR
                        c.Logger.LogInformation
                        $"Shard %u{c.ShardId} sent application command created: %s{e.Command.Name}: (%u{e.Command.Id}) for %u{e.Command.ApplicationId}")

        let discord_ApplicationCommandDeleted =
            ClientHandler<ApplicationCommandEventArgs>(fun c e -> taskR c.Logger.LogInformation $"")

        let discord_ApplicationCommandUpdated =
            ClientHandler<ApplicationCommandEventArgs>(fun c e -> taskR c.Logger.LogInformation $"")

        let slash_SlashCommandErrored =
            AsyncEventHandler<ApplicationCommandsExtension, SlashCommandErrorEventArgs>
                (fun c e -> taskR c.Client.Logger.LogError $"")

        let slash_SlashCommandExecuted =
            AsyncEventHandler<ApplicationCommandsExtension, SlashCommandExecutedEventArgs>
                (fun c e -> taskR c.Client.Logger.LogError $"")

        client.add_ApplicationCommandCreated discord_ApplicationCommandCreated
        client.add_ApplicationCommandDeleted discord_ApplicationCommandDeleted
        client.add_ApplicationCommandUpdated discord_ApplicationCommandUpdated

        appCommands.add_SlashCommandErrored slash_SlashCommandErrored
        appCommands.add_SlashCommandExecuted slash_SlashCommandExecuted

        ()

    let registerCommands
        { CommandsNext = commandsNext
          AppCommands = appCommands }
        =
        commandsNext.RegisterCommands<CommandsNext.Main>()
        appCommands.RegisterCommands<AppCommands.Main>()

        ()
