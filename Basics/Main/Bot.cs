#region Using Directives
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DSharpPlusNextGen;
using DSharpPlusNextGen.CommandsNext;
using DSharpPlusNextGen.Entities;
using DSharpPlusNextGen.EventArgs;
using DSharpPlusNextGen.Interactivity;
using DSharpPlusNextGen.Interactivity.Extensions;
using DSharpPlusNextGen.VoiceNext;

using Microsoft.Extensions.Logging;
#endregion


namespace DSharpPlusNextGen.Examples.Bots.Basics.Main
{
    internal class Bot : IDisposable
    {
        public static CancellationTokenSource ShutdownRequest;

        public static string prefix = "!";

        public static DiscordClient Client { get; set; }
        private CommandsNextExtension CNext;
        private InteractivityExtension INext;
        public VoiceNextExtension Voice { get; set; }

        #region Main
        public Bot(InteractivityExtension iNext)
        {
            INext = iNext;
        }

        public Bot(string Token)
        {
            ShutdownRequest = new CancellationTokenSource();

            LogLevel logLevel = LogLevel.Debug;

            var cfg = new DiscordConfiguration
            {
                Token = Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MessageCacheSize = 2048,
                MinimumLogLevel = logLevel,
                ShardCount = 1,
                ShardId = 0,
                Intents = DiscordIntents.All
            };

            Client = new DiscordClient(cfg);

            CNext = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { prefix },
                CaseSensitive = true,
                EnableMentionPrefix = true,
                IgnoreExtraArguments = true,
                DefaultHelpChecks = null,
                EnableDefaultHelp = true,
                EnableDms = false
            });

            CNext.RegisterCommands<Commands.General>();

            CNext.CommandErrored += CNext_CommandErrored;

            Voice = Client.UseVoiceNext(new VoiceNextConfiguration
            {
                AudioFormat = AudioFormat.Default,
                EnableIncoming = false
            });

            INext = Client.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = Interactivity.Enums.PaginationBehaviour.WrapAround,
                PaginationDeletion = Interactivity.Enums.PaginationDeletion.DeleteMessage,
                PollBehaviour = Interactivity.Enums.PollBehaviour.DeleteEmojis,
                Timeout = TimeSpan.FromMinutes(1),
                PaginationEmojis = new PaginationEmojis()
                {
                    Left = DiscordEmoji.FromName(Client, ":arrow_backward:", false),
                    Right = DiscordEmoji.FromName(Client, ":arrow_forward:", false),
                    SkipLeft = DiscordEmoji.FromName(Client, ":arrow_left:", false),
                    SkipRight = DiscordEmoji.FromName(Client, ":arrow_right:", false),
                    Stop = DiscordEmoji.FromName(Client, ":stop_button:", false)
                }
            });;
        }
        
        private async Task CNext_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Command failed: {e.Exception.Message}");
            Console.ResetColor();

            await Task.Delay(200);
        }
        #endregion

        #region Startup
        public void Dispose()
        {
            Client.Dispose();
            INext = null;
            CNext = null;
            Client = null;
            Environment.Exit(0);
        }

        public async Task RunAsync()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            await Client.ConnectAsync();
            Console.WriteLine($"Starting with Prefix {prefix}");
            Console.WriteLine($"Starting {Client.CurrentUser.Username}");

            while (!ShutdownRequest.IsCancellationRequested)
            {
                await Task.Delay(2000);
            }
            await Client.UpdateStatusAsync(activity: null, userStatus: UserStatus.Offline, idleSince: null);

            await Client.DisconnectAsync();
            await Task.Delay(2500);
            Dispose();
        }
        #endregion
    }
}
