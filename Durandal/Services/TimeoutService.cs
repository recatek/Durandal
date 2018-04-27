using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Durandal.Services
{
  public class TimeoutService
  {
    private readonly DiscordSocketClient discord;
    private readonly DatabaseService database;

    private volatile bool isRunning;

    public TimeoutService(
      DiscordSocketClient discord,
      DatabaseService database)
    {
      this.discord = discord;
      this.database = database;

      this.isRunning = false;

      this.discord.Ready += this.OnReady;
    }

    /// <summary>
    /// Creates a new timeout. Will overwrite a prior one.
    /// </summary>
    public async Task AddTimeout(
      SocketCommandContext context,
      SocketUser user, 
      TimeSpan time,
      string reason)
    {
      string timeFormatted = Util.PrintHuman(time);
      DateTimeOffset expiration = context.Message.Timestamp + time;

      // Update the database
      this.database.SetTimeout(
        context.Guild,
        user,
        expiration);

      // Send confirmation message
      string message =
        $"{user.Mention} timed out for {timeFormatted} " +
        $"by {context.User.Mention}" +
        (string.IsNullOrEmpty(reason) ? "." : $", reason: {reason}");
      await context.Channel.SendMessageAsync(message);
      if (this.database.TryGetLogChannel(context.Guild, out var logChannel))
        await logChannel.SendMessageAsync(message);
    }

    /// <summary>
    /// Retires an expired timeout.
    /// </summary>
    private async Task RetireTimeout(ulong guildId, ulong userId)
    {
      SocketGuild guild = this.discord.GetGuild(guildId);
      SocketUser user = guild?.GetUser(userId);
      if ((guild == null) || (user == null))
        return;

      // TODO: Roles

      string mention = MentionUtils.MentionUser(userId);
      string message = $"{mention} is no longer timed out.";
      if (this.database.TryGetLogChannel(guild, out var logChannel))
        await logChannel.SendMessageAsync(message);
    }

    /// <summary>
    /// Updates all timeouts and checks for expired ones.
    /// </summary>
    private async Task Update()
    {
      DateTime now = DateTime.Now;
      foreach (var timeout in this.database.ExpireTimeouts(now))
        await this.RetireTimeout(timeout.Item1, timeout.Item2);
    }

    /// <summary>
    /// Main background loop for the timer update.
    /// </summary>
    private async Task RunTimer()
    {
      while (this.isRunning)
      {
        await this.Update();
        await Task.Delay(5000);
      }
    }

    /// <summary>
    /// Begins the timer once we're ready to go.
    /// </summary>
    private Task OnReady()
    {
      if (this.isRunning == false)
      {
        this.isRunning = true;
        Task timer = this.RunTimer();
      }

      return Task.CompletedTask;
    }
  }
}
