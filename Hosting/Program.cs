using DisCatSharp.Hosting;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hosting
{
    public class Program
    {
        public static IHost? host = null;
        public static void Main(string[] args)
        {
            host = CreateHostBuilder("appsettings.json").Build();
            var service = host.Services.GetRequiredService<IDiscordHostedService>();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string filename) =>
            Host.CreateDefaultBuilder()
                .ConfigureServices(services => services.AddSingleton<IDiscordHostedService, Bot>())
                .ConfigureHostConfiguration(builder => builder.AddJsonFile(filename))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
