using System;
using System.Net;
using System.Text;
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
    private static IEnumerable<string> FormatNamelocks(
      IEnumerable<Tuple<ulong, string>> namelocks)
    {
      foreach (var value in namelocks)
        yield return
          "[" +
          MentionUtils.MentionUser(value.Item1) +
          ": " +
          value.Item2 +
          "]";
    }

    private readonly DiscordSocketClient discord;
    private readonly DatabaseService database;
    private readonly LoggingService logging;

    public NameService(
      DiscordSocketClient discord,
      DatabaseService database,
      LoggingService logging)
    {
      this.discord = discord;
      this.database = database;
      this.logging = logging;

      this.discord.UserJoined += OnUserJoined;
      this.discord.GuildMemberUpdated += OnGuildMemberUpdated;
    }

    /// <summary>
    /// Adds a namelock to the given user with the given name.
    /// </summary>
    public async Task AddNamelock(
      SocketCommandContext context,
      SocketUser user,
      string name)
    {
      if (user is IGuildUser guildUser)
      {
        // Update the database
        this.database.SetNamelock(
          context.Guild,
          user,
          name);

        // Police the user
        await this.SetName(guildUser, name);

        // Send confirmation message
        string message =
          $"{user.Mention} namelocked " +
          $"by {context.User.Mention}";
        await context.Channel.SendMessageAsync(message);
        if (this.database.TryGetLogChannel(context.Guild, out var logChannel))
          await logChannel.SendMessageAsync(message);
      }
    }

    /// <summary>
    /// Clears and removes an existing namelock.
    /// </summary>
    public async Task RemoveNamelock(
      SocketCommandContext context,
      SocketUser user)
    {
      if (user is IGuildUser guildUser)
      {
        // Update the database
        if (this.database.ClearNamelock(context.Guild, user))
        {
          // Send confirmation message
          string message =
            $"{user.Mention} namelock removed " +
            $"by {context.User.Mention}";
          await context.Channel.SendMessageAsync(message);
          if (this.database.TryGetLogChannel(context.Guild, out var logChannel))
            await logChannel.SendMessageAsync(message);
        }
        else
        {
          // Send error message
          string message =
            $"{user.Mention} namelock not found";
          await context.Channel.SendMessageAsync(message);
        }
      }
    }

    /// <summary>
    /// Prints a list of data namelocks into a readable format.
    /// </summary>
    public string ListNamelocks(
      SocketCommandContext context)
    {
      return string.Join(
        ", ",
        NameService.FormatNamelocks(
          this.database.GetNamelocks(context.Guild)));
    }

    private Task OnUserJoined(
      SocketGuildUser newUser)
    {
      return this.PoliceUser(newUser);
    }

    private Task OnGuildMemberUpdated(
      SocketGuildUser oldUser,
      SocketGuildUser newUser)
    {
      return this.PoliceUser(newUser);
    }

    private async Task PoliceUser(SocketGuildUser user)
    {
      if (this.database.TryGetNamelock(user.Guild, user, out string name))
        if (string.IsNullOrEmpty(name) == false)
          if (string.Equals(user.Nickname, name) == false)
            await this.SetName(user, name);
    }

    private async Task SetName(IGuildUser user, string name)
    {
      // Don't name-police administrators
      if (user?.GuildPermissions.Has(GuildPermission.Administrator) == true)
        return;

      try
      {
        await user.ModifyAsync((p) => p.Nickname = name);
      }
      catch (HttpException he)
      {
        if ((user != null) && (he.HttpCode == HttpStatusCode.Forbidden))
        {
          this.logging.LogInternal(
              Util.CreateLog(
              LogSeverity.Error,
              $"Attempting to reset forbidden user {user.Mention}"));
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
