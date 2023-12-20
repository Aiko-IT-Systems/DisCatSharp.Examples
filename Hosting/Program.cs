using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;

namespace DisCatSharp.Examples.Hosting;

public class Program
{
	public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

	public static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.UseSerilog(new LoggerConfiguration()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger())
			.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
