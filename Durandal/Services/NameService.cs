using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Net;
using Discord.Commands;
using Discord.WebSocket;

namespace Durandal.Services
{
  public class NameService
  {
    private readonly DiscordSocketClient discord;
    private readonly LoggingService logging;

    public NameService(
      DiscordSocketClient discord,
      LoggingService logging)
    {
      this.discord = discord;
      this.logging = logging;

      this.discord.GuildMemberUpdated += OnGuildMemberUpdated;
    }

    private async Task OnGuildMemberUpdated(
      SocketGuildUser oldUser, 
      SocketGuildUser newUser)
    {
      // Don't name-police administrators
      if (newUser?.GuildPermissions.Has(GuildPermission.Administrator) == true)
        return;

      try
      {
        if (newUser?.Nickname != null)
          if (newUser.Nickname.Equals(newUser.Username) == false)
            await newUser.ModifyAsync((p) => p.Nickname = newUser.Username);
      }
      catch (HttpException he)
      {
        if ((newUser != null) && (he.HttpCode == HttpStatusCode.Forbidden))
        {
          string name = newUser.ReadableName();
          this.logging.LogInternal(
            Util.CreateLog(
              LogSeverity.Error, 
              $"Attempting to reset forbidden user {name}"));
        }
        else
        {
          this.logging.LogInternal(
            Util.CreateLog(
              LogSeverity.Error, 
              he.ToString()));
        }
      }
    }
  }
}
