using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Durandal.Services
{
  public class LogService
  {
    private readonly DiscordSocketClient discord;
    private readonly CommandService commands;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger discordLogger;
    private readonly ILogger commandLogger;

    public LogService(DiscordSocketClient discord, CommandService commands, ILoggerFactory loggerFactory)
    {
      this.discord = discord;
      this.commands = commands;

      this.loggerFactory = ConfigureLogging(loggerFactory);
      this.discordLogger = this.loggerFactory.CreateLogger("discord");
      this.commandLogger = this.loggerFactory.CreateLogger("command");

      this.discord.Log += LogDiscord;
      this.commands.Log += LogCommand;
    }

    private ILoggerFactory ConfigureLogging(ILoggerFactory factory)
    {
      factory.AddConsole();
      return factory;
    }

    private Task LogDiscord(LogMessage message)
    {
      this.discordLogger.Log(
          LogLevelFromSeverity(message.Severity),
          0,
          message,
          message.Exception,
          (_1, _2) => message.ToString(prependTimestamp: false));
      return Task.CompletedTask;
    }

    private Task LogCommand(LogMessage message)
    {
      // Return an error message for async commands
      if (message.Exception is CommandException command)
      {
        // Don't risk blocking the logging task by awaiting a message send; ratelimits!?
        var _ = command.Context.Channel.SendMessageAsync($"Error: {command.Message}");
      }

      this.commandLogger.Log(
          LogLevelFromSeverity(message.Severity),
          0,
          message,
          message.Exception,
          (_1, _2) => message.ToString(prependTimestamp: false));
      return Task.CompletedTask;
    }

    private static LogLevel LogLevelFromSeverity(LogSeverity severity)
        => (LogLevel)(Math.Abs((int)severity - 5));
  }
}