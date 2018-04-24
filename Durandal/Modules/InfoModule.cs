using System.Threading.Tasks;

using Discord;
using Discord.Commands;

namespace Durandal.Modules
{
  public class InfoModule : ModuleBase<SocketCommandContext>
  {
    [Command("durandal")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public Task Trouble() => this.ReplyAsync("T-R-O-U-B-L-E.");

    [Command("setstatus")]
    public Task SetStatus([Remainder]string status) => this.Context.Client.SetGameAsync(status);
  }
}
