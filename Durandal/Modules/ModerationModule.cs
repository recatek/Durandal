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
    public async Task Purge(string timeString)
    {
      TimeSpan time = Util.ParseHuman(timeString);

      var messages = 
        await this.Context.Channel.GetMessagesAsync(MAX_PURGE).Flatten();
      List<IMessage> filtered = new List<IMessage>();

      var cutoff = this.Context.Message.Timestamp - time;
      foreach (var message in messages)
        if (message.Timestamp > cutoff)
          filtered.Add(message);

      string channel = MentionUtils.MentionChannel(this.Context.Message.Channel.Id);
      await this.Context.Channel.DeleteMessagesAsync(filtered);
      await this.ReplyAsync($"Purging {channel} from {cutoff} ({filtered.Count} deleted)");
    }
  }
}
