using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Durandal.Services;

namespace Durandal.Modules
{
  public class ModerationModule : ModuleBase<SocketCommandContext>
  {
    private const int MAX_PURGE = 100;

    private readonly TimeoutService timeout;

    public ModerationModule(TimeoutService timeout)
    {
      this.timeout = timeout;
    }

    #region Timeout
    [Priority(0)]
    [Command("timeout")]
    [Summary("Timeout a mentioned user for a given period of time.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public Task Timeout(
      SocketGuildUser user,
      string timeString)
    {
      return this.PerformTimeout(user, timeString);
    }

    [Priority(0)]
    [Command("timeout")]
    [Summary("Timeout a mentioned user for a given period of time.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public Task Timeout(
      SocketGuildUser user,
      string timeString,
      [Remainder]string reason)
    {
      return this.PerformTimeout(user, timeString, reason);
    }

    [Command("timeout")]
    [Summary("Timeout a mentioned user for a given period of time.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public Task Timeout()
    {
      return this.ReplyAsync("Usage: `!timeout <@user> <time> [<reason>]`");
    }

    [Command("timeout")]
    [Summary("Timeout a mentioned user for a given period of time.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public Task Timeout(
      [Remainder]string reason)
    {
      return this.ReplyAsync("Usage: `!timeout <@user> <time> [<reason>]`");
    }

    private async Task PerformTimeout(
      SocketGuildUser user,
      string timeString,
      string reason = null)
    {
      if (Util.TryParseHuman(timeString, out TimeSpan time) == false)
      {
        await this.ReplyAsync($"Invalid time: `{timeString}`.");
        return;
      }

      await this.Context.Channel.DeleteMessagesAsync(new[] { this.Context.Message });
      await this.timeout.AddTimeout(this.Context, user, time, reason);
    }
    #endregion

    #region Purge
    [Priority(0)]
    [Command("purge")]
    [Summary("Purge all messages sent within the given time.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Purge(string timeString)
    {
      return this.PerformPurge(Context.User, null, timeString);
    }

    [Priority(0)]
    [Command("purge")]
    [Summary("Purge all messages sent within the given time.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Purge(string timeString, [Remainder]string reason)
    {
      return this.PerformPurge(Context.User, null, timeString, reason);
    }

    [Priority(0)]
    [Command("purge")]
    [Summary("Purge all messages sent within the given time.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Purge(SocketGuildUser user, string timeString)
    {
      return this.PerformPurge(Context.User, user, timeString);
    }

    [Priority(0)]
    [Command("purge")]
    [Summary("Purge all messages sent within the given time.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Purge(SocketGuildUser user, string timeString, [Remainder]string reason)
    {
      return this.PerformPurge(Context.User, user, timeString, reason);
    }

    [Command("purge")]
    [Summary("Purge all messages sent within the given time.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Purge(SocketGuildUser user)
    {
      return this.ReplyAsync("Usage: `!purge [<@user>] <time> [<reason>]`");
    }

    [Command("purge")]
    [Summary("Purge all messages sent within the given time.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Purge()
    {
      return this.ReplyAsync("Usage: `!purge [<@user>] <time> [<reason>]`");
    }

    private async Task PerformPurge(
      SocketUser sender,
      SocketGuildUser user, // May be null
      string timeString,
      string reason = null)
    {
      if (Util.TryParseHuman(timeString, out TimeSpan time) == false)
      {
        await this.ReplyAsync($"Invalid time: `{timeString}`.");
        return;
      }

      // Gather the messages for purging
      var cutoff = this.Context.Message.Timestamp - time;
      var messages =
        await this.Context.Channel.GetMessagesAsync(MAX_PURGE).Flatten();
      List<IMessage> filtered = new List<IMessage>();

      // Perform the bulk delete
      foreach (var message in messages)
        if (message.Timestamp > cutoff)
          if ((user == null) || (message.Author.Id == user.Id))
            filtered.Add(message);
      await this.Context.Channel.DeleteMessagesAsync(filtered);

      // Format the channel and time
      string channel =
        MentionUtils.MentionChannel(this.Context.Message.Channel.Id);

      // Format how many messages were filtered
      int numFiltered = filtered.Count;
      if ((user == null) || (user.Id == sender.Id))
        numFiltered -= 1; // Don't count the command message
      string messageCount = 
        numFiltered + ((numFiltered == 1) ? " message" : " messages");

      await this.ReplyAsync(
        $"{sender.Mention} purged {messageCount} " +
        ((user == null) ? "" : $"by {user.Mention} ") +
        $"sent within {Util.PrintHuman(time)} in {channel}" +
        (string.IsNullOrEmpty(reason) ? "." : $", reason: {reason}"));
    }
    #endregion
  }
}
