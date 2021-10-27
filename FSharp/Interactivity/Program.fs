open System
open System.Reflection
open System.Threading.Tasks
open DisCatSharp
open DisCatSharp.ApplicationCommands
open DisCatSharp.ApplicationCommands.EventArgs
open DisCatSharp.Common.Utilities
open DisCatSharp.Entities
open DisCatSharp.EventArgs
open DisCatSharp.Interactivity
open DisCatSharp.VoiceNext
open DisCatSharp.Interactivity.Extensions
open Microsoft.Extensions.Logging
open Serilog

let ComponentInteraction =
    AsyncEventHandler<DiscordClient, ComponentInteractionCreateEventArgs>
        (fun c e ->
            (task {
                do!
                    match e.Id with
                    | "rand_next" ->
                        e.Message.ModifyAsync(
                            Random().Next(0, 100).ToString()
                            |> Optional.FromValue
                        )
                        :> Task
                    | "rand_cancel" -> e.Message.DeleteAsync()
                    | _ -> Task.CompletedTask
             })
            :> Task)

let SlashCommandExecuted =
    AsyncEventHandler<ApplicationCommandsExtension, SlashCommandExecutedEventArgs>
        (fun c e -> (task { Log.Information $"Slash: %s{e.Context.CommandName}" } :> Task))

let SlashCommandErrored =
    AsyncEventHandler<ApplicationCommandsExtension, SlashCommandErrorEventArgs>
        (fun c e ->
            (task {
                Log.Error $"Slash: %O{e.Exception} | CN: %s{e.Context.CommandName} | IID: %i{e.Context.InteractionId}" }
            :> Task))

[<EntryPoint>]
let main argv =
    // Create logger, using SeriLog
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger()

    Log.Information "Starting bot"

    // Configure the minimum log level to record
    // CHALLENGE: Read this from a configuration file
    let logLevel =
#if DEBUG
        LogLevel.Debug
#else
        LogLevel.Error
#endif

    let discordConfiguration = DiscordConfiguration()
    // The token is received as the first command line argument.
    // This isn't ideal in the long run and serves as a quick way of doing this demo mostly.
    // CHALLENGE: Try making sure the token is provided. Hint: are there other ways of getting an item from an array?
    // CHALLENGE: Try getting the token from a file or an environment variable
    discordConfiguration.Token <- argv.[0]
    discordConfiguration.TokenType <- TokenType.Bot
    discordConfiguration.AutoReconnect <- true
    discordConfiguration.MinimumLogLevel <- logLevel
    discordConfiguration.Intents <- DiscordIntents.AllUnprivileged

    // Create the client
    use client = new DiscordClient(discordConfiguration)

    client.UseVoiceNext()
    |> ignore<VoiceNextExtension>

    Log.Information "Connecting to Discord..."
    client.ConnectAsync().Wait()
    Log.Information $"Connected as %A{client.CurrentUser.UsernameWithDiscriminator}"

    client.UseInteractivity()
    |> ignore<InteractivityExtension>

    Log.Information "Registering application commands..."
    let appCommandsExt = client.UseApplicationCommands()
    appCommandsExt.add_SlashCommandExecuted SlashCommandExecuted
    appCommandsExt.add_SlashCommandErrored SlashCommandErrored

    // Register slash commands
    Assembly.GetExecutingAssembly().GetTypes()
    |> Seq.filter
        (fun t ->
            t.IsAssignableFrom typeof<ApplicationCommandsModule>
            && not t.IsNested)
    |> Seq.iter appCommandsExt.RegisterCommands

    // This handler is used for the "random" command
    client.add_ComponentInteractionCreated ComponentInteraction

    // Prevent the program from exiting so that it can listen for commands
    Task.Delay(-1).Wait()

    0 // return an integer exit code
