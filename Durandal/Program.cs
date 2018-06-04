using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Durandal.Services;

namespace Durandal
{
  public class Program
  {
    public static Task Main(string[] args) => Program.RunAsync(args);

    public static async Task RunAsync(string[] args)
    {
      IConfiguration config = Program.BuildConfig();

      DiscordSocketConfig socketConfig = new DiscordSocketConfig
      {
        MessageCacheSize = int.Parse(config["cacheSize"] ?? "0")
      };
      DiscordSocketClient client = new DiscordSocketClient(socketConfig);

      IServiceProvider services = Program.ConfigureServices(client, config);
      services.GetRequiredService<LoggingService>();
      services.GetRequiredService<TimeoutService>();
      services.GetRequiredService<NameService>();

      await services.GetRequiredService<DatabaseService>().Initialize(services);
      await services.GetRequiredService<MessageService>().Initialize(services);

      await client.LoginAsync(TokenType.Bot, config["token"]);
      await client.StartAsync();

      await Task.Delay(-1);
    }

    private static IServiceProvider ConfigureServices(
      DiscordSocketClient client,
      IConfiguration config)
    {
      return new ServiceCollection()
        // Base
        .AddSingleton(client)
        .AddSingleton<CommandService>()
        .AddSingleton<TimeoutService>()
        .AddSingleton<NameService>()
        .AddSingleton<DatabaseService>()
        .AddSingleton<MessageService>()
        // Logging
        .AddLogging()
        .AddSingleton<LoggingService>()
        // Extra
        .AddSingleton(config)
        // Add additional services here...
        .BuildServiceProvider();
    }

    private static IConfiguration BuildConfig()
    {
      try
      {
        return new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("config.json")
          .Build();
      }
      catch (FileNotFoundException)
      {
        Console.WriteLine("ERROR: Could not find config.json file");
        Environment.Exit(-1);
        return null;
      }
    }
  }
}
