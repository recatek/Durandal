using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Discord;
using Discord.Commands;

namespace Durandal.Modules
{
  public class InfoModule : ModuleBase<SocketCommandContext>
  {
    private readonly ulong ownerId;

    public InfoModule(
      IConfiguration config)
    {
      this.ownerId = ulong.Parse(config["ownerId"] ?? "0");
    }

    [Command("durandal")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Trouble() => this.ReplyAsync("T-R-O-U-B-L-E.");

    [Command("setstatus")]
    public async Task SetStatus([Remainder]string status)
    {
      // Only the bot owner can do this
      if (Context.User.Id != this.ownerId)
        return;

      await this.Context.Client.SetGameAsync(status);
    }
  }
}
