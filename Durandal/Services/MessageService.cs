using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Durandal.Services
{
  public class MessageService
  {
    // TODO: Get from config?
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
    private readonly DatabaseService database;
    private readonly LoggingService logging;

    private readonly char prefix;

    private IServiceProvider provider;

    public MessageService(
      DiscordSocketClient discord,
      CommandService commands,
      DatabaseService database,
      LoggingService logging,
      IConfiguration config)
    {
      this.discord = discord;
      this.commands = commands;
      this.database = database;
      this.logging = logging;
      this.prefix = char.Parse(config["prefix"] ?? "!");

      this.discord.MessageReceived += this.OnMessageReceived;
      this.discord.MessageDeleted += this.OnMessageDeleted;
    }

    public async Task Initialize(IServiceProvider provider)
    {
      this.provider = provider;

      await this.commands.AddModulesAsync(Assembly.GetEntryAssembly());
    }

    /// <summary>
    /// Process incoming messages.
    /// </summary>
    private async Task OnMessageReceived(SocketMessage rawMessage)
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
      if (message.HasCharPrefix(this.prefix, ref argPos) == false)
        return;
      var result = 
        await this.commands.ExecuteAsync(context, argPos, this.provider);

      if (result.Error.HasValue)
      {
        if (result.Error.Value == CommandError.ObjectNotFound)
        {
          await context.Channel.SendMessageAsync(result.ErrorReason);
          return;
        }

        string input = $"{message.Author.ReadableName()}: {message.Content}";
        string errorFull = input + " -- " + result.ToString();
        this.logging.LogInternal(Util.CreateLog(LogSeverity.Error, errorFull));
      }
    }

    /// <summary>
    /// Screen incoming messages for content.
    /// </summary>
    private bool ScreenMessage(
      SocketCommandContext context, 
      SocketMessage message)
    {
      // Don't screen messages from administrators
      if (context.User is IGuildUser user)
        if (user.GuildPermissions.Has(GuildPermission.Administrator))
          return false;

      ISocketMessageChannel channel = context.Channel;
      if (this.FilterMessageAttachment(message))
      {
        context.Channel.SendMessageAsync(
          $"Disallowed file format. ({message.Author.Mention})");
        channel.DeleteMessagesAsync(new[] { message });
        return true;
      }

      return false;
    }

    /// <summary>
    /// Search for attachments with illegal file extensions.
    /// </summary>
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

    /// <summary>
    /// Log any messages that were deleted containing pings (to catch people
    /// trolling with ping-delete).
    /// </summary>
    private async Task OnMessageDeleted(
      Cacheable<IMessage, ulong> cachedMessage,
      ISocketMessageChannel channel)
    {
      if (cachedMessage.Value is SocketUserMessage socketMessage)
      {
        int totalPings =
          socketMessage.MentionedUsers.Count +
          socketMessage.MentionedRoles.Count;
        if (totalPings == 0)
          return;

        if (channel is SocketTextChannel socketChannel)
        {
          if (this.database.TryGetLogChannel(socketChannel.Guild, out var logChannel))
          {
            // Don't freak out in the log channel
            if (socketChannel.Id == logChannel.Id)
              return;

            List<string> mentions = new List<string>();
            foreach (SocketUser mentionedUser in socketMessage.MentionedUsers)
              mentions.Add(mentionedUser.Mention);
            foreach (SocketRole mentionedRole in socketMessage.MentionedRoles)
              mentions.Add(mentionedRole.Mention);

            // Format the log string
            string username = socketMessage.Author.Mention;
            string joined = string.Join(", ", mentions);
            await logChannel.SendMessageAsync(
              $"Logging deleted message by {username} pinging: {joined}");
          }
        }
      }
    }
  }
}
