using DisCatSharp.Hosting;
using DisCatSharp.CommandsNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hosting
{
    public class Bot : DiscordHostedService
    {
        public Bot(IConfiguration config, ILogger<DiscordHostedService> logger, IServiceProvider provider) : base(config, logger, provider)
        {
            Logger.LogInformation("Trying to start up");
            Client.GetCommandsNext().RegisterCommands<TestCommands>();
            
        }
    }
}
