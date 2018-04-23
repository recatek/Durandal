using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

namespace Durandal.Modules
{
  public class TimeoutModule : ModuleBase<SocketCommandContext>
  {
    [Command("timeout")]
    public async Task Timeout(SocketGuildUser user, string time, [Remainder]string reason)
    {
      await this.ReplyAsync($"{user.Mention} timed out for {time}: {reason}");
      await this.Context.Channel.DeleteMessagesAsync(new[] { this.Context.Message });
    }
  }
}
