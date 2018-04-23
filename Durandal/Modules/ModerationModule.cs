using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;

namespace Durandal.Modules
{
  public class ModerationModule : ModuleBase<SocketCommandContext>
  {
    private const int MAX_PURGE = 100;

    [Command("purge")]
    [Summary("Purge all messages sent within the given time.")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public Task Purge(string timeString)
    {
      return this.PerformPurge(timeString);
    }

    [Command("purge")]
    [Summary("Purge all messages sent within the given time.")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public Task Purge(string timeString, [Remainder]string reason)
    {
      return this.PerformPurge(timeString, reason);
    }

    private async Task PerformPurge(string timeString, string reason = null)
    {
      if (Util.TryParseHuman(timeString, out TimeSpan time) == false)
      {
        await this.ReplyAsync("Invalid time format: " + timeString);
        return;
      }

      var cutoff = this.Context.Message.Timestamp - time;
      var messages =
        await this.Context.Channel.GetMessagesAsync(MAX_PURGE).Flatten();
      List<IMessage> filtered = new List<IMessage>();

      foreach (var message in messages)
        if (message.Timestamp > cutoff)
          filtered.Add(message);
      await this.Context.Channel.DeleteMessagesAsync(filtered);

      string channel =
        MentionUtils.MentionChannel(this.Context.Message.Channel.Id);
      string timeFormatted = Util.PrintHuman(time);
      await this.ReplyAsync(
        $"Purged {filtered.Count} messages sent " +
        $"within {timeFormatted} in {channel}" +
        ((reason != null) ? $" for: {reason}" : ""));
    }
  }
}
