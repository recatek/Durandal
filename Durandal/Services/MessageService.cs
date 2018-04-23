using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Durandal.Services
{
  public class MessageService
  {
    // TODO: Get from config
    private static readonly string[] ALLOWED_EXTENSIONS = 
      new string[] 
      {
        "png",
        "bmp",
        "tga",
        "jpg",
        "gif",
        "gifv",
        "mp4",
        "webm",
        "webp",
      };

    private static bool CheckExtension(string extension)
    {
      for (int i = 0; i < ALLOWED_EXTENSIONS.Length; i++)
        if (ALLOWED_EXTENSIONS[i].Equals(extension))
          return true;
      return false;
    }

    private readonly DiscordSocketClient discord;
    private readonly CommandService commands;
    private readonly LoggingService logging;
    private IServiceProvider provider;

    public MessageService(
      IServiceProvider provider, 
      DiscordSocketClient discord, 
      CommandService commands,
      LoggingService logging)
    {
      this.provider = provider;
      this.discord = discord;
      this.commands = commands;
      this.logging = logging;

      this.discord.MessageReceived += MessageReceived;
    }

    public async Task Initialize(IServiceProvider provider)
    {
      this.provider = provider;
      await this.commands.AddModulesAsync(Assembly.GetEntryAssembly());
      // Add additional initialization code here...
    }

    private async Task MessageReceived(SocketMessage rawMessage)
    {
      // Ignore system messages and messages from bots
      if (!(rawMessage is SocketUserMessage message))
        return;
      if (message.Source != MessageSource.User)
        return;

      var context = new SocketCommandContext(this.discord, message);
      if (this.ScreenMessage(context, message))
        return;

      int argPos = 0;
      // TODO: Get from config
      if (message.HasCharPrefix('!', ref argPos) == false)
        return;
      var result = 
        await this.commands.ExecuteAsync(context, argPos, this.provider);

      if (result.Error.HasValue)
      {
        string input = $"{message.Author.ReadableName()}: {message.Content}";
        string errorFull = input + " -- " + result.ToString();
        this.logging.LogInternal(Util.CreateLog(LogSeverity.Error, errorFull));
      }
    }

    private bool ScreenMessage(
      SocketCommandContext context, 
      SocketMessage message)
    {
      ISocketMessageChannel channel = context.Channel;
      if (FilterMessageAttachment(message))
      {
        context.Channel.SendMessageAsync(message.Author.Mention + " -- disallowed file format");
        channel.DeleteMessagesAsync(new[] { message });
        return true;
      }

      return false;
    }

    private bool FilterMessageAttachment(SocketMessage message)
    {
      foreach (var attachment in message.Attachments)
      {
        string extension = Util.GetExtension(attachment.Filename);
        if (MessageService.CheckExtension(extension) == false)
          return true;
      }

      return false;
    }
  }
}
