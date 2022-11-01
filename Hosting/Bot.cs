using DisCatSharp.ApplicationCommands;
using DisCatSharp.CommandsNext;
using DisCatSharp.Examples.Hosting.Commands;
using DisCatSharp.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;

namespace DisCatSharp.Examples.Hosting
{
	public class FirstBot : DiscordHostedService
	{
		public FirstBot(IConfiguration config, ILogger<DiscordHostedService> logger, IServiceProvider provider,
			IHostApplicationLifetime appLifetime) : base(config, logger, provider, appLifetime, "FirstBot")
		{
			// You can do whatever you want here, for example logs or registering commands

			Logger.LogInformation("Trying to start up the first bot");
			Client.GetCommandsNext().RegisterCommands<TestCommands>();
		}
	}

	public class SecondBot : DiscordHostedService
	{
		public SecondBot(IConfiguration config, ILogger<DiscordHostedService> logger, IServiceProvider provider,
			IHostApplicationLifetime appLifetime) : base(config, logger, provider, appLifetime, "SecondBot")
		{
			// You can also configure all extensions here

			Logger.LogInformation("Trying to start up the second bot");

			var ext = Client.UseApplicationCommands();

			ext.RegisterGlobalCommands<AppCommands>();
		}

		protected override void OnInitializationError(Exception ex)
		{
			Logger.LogError(ex, "An error occurred during the initialization of the second bot");

			// By default, the app will be shutdown, but we can override this method and do whatever we want here.
			// Or just make the bot optional without shutting down the application, as in this example.
		}
	}
}
