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
    private readonly NameService name;
    private readonly MessageService message;

    public ModerationModule(
      TimeoutService timeout,
      NameService name,
      MessageService message)
    {
      this.timeout = timeout;
      this.name = name;
      this.message = message;
    }

    #region Shownamelocks
    [Priority(0)]
    [Command("listnamelocks")]
    [Summary("Lists all active namelocks.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task ListNamelocks()
    {
      return this.PerformListNamelocks();
    }

    [Command("listnamelocks")]
    [Summary("Lists all active namelocks.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task ListNamelocks(
      [Remainder]string _)
    {
      return this.PerformListNamelocks();
    }

    private async Task PerformListNamelocks()
    {
      this.message.IgnoreMessage(this.Context.Message.Id);
      await this.Context.Channel.DeleteMessagesAsync(new[] { this.Context.Message });

      string namelocks = this.name.ListNamelocks(this.Context);
      if (string.IsNullOrEmpty(namelocks))
        await this.ReplyAsync("No active namelocks");
      else
        await this.ReplyAsync("Namelocks: " + namelocks);
    }
    #endregion

    #region Namelock
    [Priority(0)]
    [Command("namelock")]
    [Summary("Lock a given user to a given name.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Namelock(
      SocketGuildUser user,
      string name)
    {
      return this.PerformNamelock(user, name);
    }

    [Command("namelock")]
    [Summary("Lock a given user to a given name.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Namelock()
    {
      return this.ReplyAsync("Usage: `!namelock <@user> <name>`");
    }

    [Command("namelock")]
    [Summary("Lock a given user to a given name.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Namelock(
      [Remainder]string _)
    {
      return this.ReplyAsync("Usage: `!namelock <@user> <name>`");
    }

    private async Task PerformNamelock(
      SocketGuildUser user,
      string name)
    {
      this.message.IgnoreMessage(this.Context.Message.Id);
      await this.Context.Channel.DeleteMessagesAsync(new[] { this.Context.Message });

      await this.name.AddNamelock(this.Context, user, name);
    }
    #endregion

    #region Namerelease
    [Priority(0)]
    [Command("namerelease")]
    [Summary("Release a user from namelock.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Namerelease(
      SocketGuildUser user)
    {
      return this.PerformNamerelease(user);
    }

    [Command("namerelease")]
    [Summary("Release a user from namelock.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Namerelease()
    {
      return this.ReplyAsync("Usage: `!namerelease <@user>`");
    }

    [Command("namerelease")]
    [Summary("Release a user from namelock.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Namerelease(
      [Remainder]string _)
    {
      return this.ReplyAsync("Usage: `!namerelease <@user>`");
    }

    private async Task PerformNamerelease(
      SocketGuildUser user)
    {
      this.message.IgnoreMessage(this.Context.Message.Id);
      await this.Context.Channel.DeleteMessagesAsync(new[] { this.Context.Message });

      await this.name.RemoveNamelock(this.Context, user);
    }
    #endregion

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
      [Remainder]string _)
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

      this.message.IgnoreMessage(this.Context.Message.Id);
      await this.Context.Channel.DeleteMessagesAsync(new[] { this.Context.Message });

      await this.timeout.AddTimeout(this.Context, user, time, reason);
    }
    #endregion

    #region Untimeout
    [Priority(0)]
    [Command("untimeout")]
    [Summary("Timeout a mentioned user for a given period of time.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public Task Untimeout(
      SocketGuildUser user)
    {
      return this.PerformUntimeout(user);
    }

    [Command("timeout")]
    [Summary("Timeout a mentioned user for a given period of time.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public Task Untimeout()
    {
      return this.ReplyAsync("Usage: `!untimeout <@user>`");
    }

    [Command("timeout")]
    [Summary("Timeout a mentioned user for a given period of time.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public Task Untimeout(
      [Remainder]string _)
    {
      return this.ReplyAsync("Usage: `!untimeout <@user>`");
    }

    private async Task PerformUntimeout(
      SocketGuildUser user)
    {
      this.message.IgnoreMessage(this.Context.Message.Id);
      await this.Context.Channel.DeleteMessagesAsync(new[] { this.Context.Message });

      await this.timeout.RemoveTimeout(this.Context, user);
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

      // Clamp the time at ten days (avoid old delete errors)
      if (time.TotalSeconds > 864000)
        time = TimeSpan.FromDays(10);

      // Gather the messages for purging
      var cutoff = this.Context.Message.Timestamp - time;
      var messages =
        await this.Context.Channel.GetMessagesAsync(MAX_PURGE).Flatten();
      List<IMessage> filtered = new List<IMessage>();

      // Perform the bulk delete
      foreach (var msg in messages)
      {
        if (msg.Timestamp > cutoff)
        {
          if ((user == null) || (msg.Author.Id == user.Id))
          {
            // Ignore the message for pingdeletes
            this.message.IgnoreMessage(msg.Id);
            filtered.Add(msg);
          }
        }
      }

      // Clear all the caught messages
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
