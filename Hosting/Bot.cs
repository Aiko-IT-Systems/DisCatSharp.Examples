using System;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.CommandsNext;
using DisCatSharp.Examples.Hosting.Commands;
using DisCatSharp.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DisCatSharp.Examples.Hosting
{
    public class FirstBot : DiscordHostedService
    {
        public FirstBot(IConfiguration config, ILogger<DiscordHostedService> logger, IServiceProvider provider) : base(
            config, logger, provider, "FirstBot")
        {
            // You can do whatever you want here, for example logs or registering commands
            
            Logger.LogInformation("Trying to start up the first bot");
            Client.GetCommandsNext().RegisterCommands<TestCommands>();
        }
    }

    public class SecondBot : DiscordHostedService
    {
        public SecondBot(IConfiguration config, ILogger<DiscordHostedService> logger, IServiceProvider provider) : base(
            config, logger, provider, "SecondBot")
        {
            // You can also configure all extensions here
            
            Logger.LogInformation("Trying to start up the second bot");

            var ext = Client.UseApplicationCommands();
            
            ext.RegisterCommands<AppCommands>();
        }
    }
}
