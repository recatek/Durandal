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
    // TODO: What if we leave a server?

    private struct Timeout
    {
      public ulong ServerId { get; }
      public ulong UserId { get; }
      public DateTimeOffset Expiration { get; }

      public Timeout(
        ulong serverId,
        ulong userId, 
        DateTimeOffset expiration)
      {
        this.ServerId = serverId;
        this.UserId = userId;
        this.Expiration = expiration;
      }
    }

    private readonly DiscordSocketClient discord;
    private readonly DatabaseService database;
    private readonly ConcurrentDictionary<ulong, Timeout> timeouts;

    private volatile bool isRunning;

    private ISocketMessageChannel lastChannel_TEMP;

    public TimeoutService(
      DiscordSocketClient discord,
      DatabaseService database)
    {
      this.discord = discord;
      this.database = database;

      this.timeouts = new ConcurrentDictionary<ulong, Timeout>();
      this.isRunning = false;

      this.discord.Ready += this.OnReady;
      this.database.ServerDataLoaded += OnServerDataLoaded;
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
      this.lastChannel_TEMP = context.Channel;

      string timeFormatted = Util.PrintHuman(time);
      DateTimeOffset expiration = context.Message.Timestamp + time;
      ulong serverId = context.Guild.Id;

      // First update the database
      this.database.SetTimeout(
        serverId,
        user.Id,
        expiration.ToUnixTimeSeconds());

      // Note: This will overwrite an existing timeout
      this.timeouts[user.Id] = 
        new Timeout(
          serverId, 
          user.Id, 
          expiration);

      // Send confirmation
      await context.Channel.SendMessageAsync(
        $"{user.Mention} timed out for {timeFormatted} " +
        $"by {context.User.Mention}" +
        (string.IsNullOrEmpty(reason) ? "." : $", reason: {reason}"));
    }

    /// <summary>
    /// Retires an expired timeout.
    /// </summary>
    private void RetireTimeout(Timeout timeout)
    {
      // First update the database
      this.database.ClearTimeout(
        timeout.ServerId,
        timeout.UserId);

      // Remove from the local cache
      this.timeouts.Remove(timeout.UserId, out Timeout value);

      // TODO: What if the user is no longer a member?
      string mention = MentionUtils.MentionUser(timeout.UserId);
      this.lastChannel_TEMP.SendMessageAsync(
        $"{mention} is no longer timed out.");
    }

    /// <summary>
    /// Updates all timeouts and checks for expired ones.
    /// </summary>
    private void Update()
    {
      DateTime now = DateTime.Now;
      List<Timeout> toRemove = new List<Timeout>();

      foreach (var timeout in this.timeouts)
        if (timeout.Value.Expiration < now)
          toRemove.Add(timeout.Value);
      foreach (Timeout timeout in toRemove)
        this.RetireTimeout(timeout);
    }

    /// <summary>
    /// Main background loop for the timer update.
    /// </summary>
    private async Task RunTimer()
    {
      while (this.isRunning)
      {
        this.Update();
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

    /// <summary>
    /// Loads a server's stored timeouts and stages them for updates.
    /// </summary>
    private void OnServerDataLoaded(ulong serverId)
    {
      foreach (var timeout in this.database.GetTimeouts(serverId))
        this.timeouts[timeout.Key] = 
          new Timeout(
            serverId, 
            timeout.Key, 
            DateTimeOffset.FromUnixTimeSeconds(timeout.Value));
    }
  }
}
