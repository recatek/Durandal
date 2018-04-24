using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Durandal.Services
{
  public class TimeoutService
  {
    private struct Timeout
    {
      public SocketUser User { get; }
      public DateTimeOffset Expiration { get; }
      public ISocketMessageChannel Channel { get; }

      public Timeout(
        SocketUser user, 
        DateTimeOffset expiration,
        ISocketMessageChannel channel)
      {
        this.User = user;
        this.Expiration = expiration;
        this.Channel = channel;
      }
    }

    private readonly DiscordSocketClient discord;
    private readonly ConcurrentBag<Timeout> timeouts;

    private volatile bool isRunning;

    public TimeoutService(
      DiscordSocketClient discord)
    {
      this.discord = discord;

      this.timeouts = new ConcurrentBag<Timeout>();
      this.isRunning = false;

      this.discord.Ready += this.OnReady;
    }

    public void AddTimeout(
      SocketCommandContext context,
      SocketUser user, 
      TimeSpan time,
      string reason)
    {
      string timeFormatted = Util.PrintHuman(time);
      DateTimeOffset expiration = context.Message.Timestamp + time;

      context.Channel.SendMessageAsync(
        $"{user.Mention} timed out for {timeFormatted} " +
        $"by {context.User.Mention}" +
        (string.IsNullOrEmpty(reason) ? "" : $", reason: {reason}"));
    }

    private void Update()
    {
      Console.WriteLine("Update()");
    }

    private async Task RunTimer()
    {
      while (this.isRunning)
      {
        this.Update();
        await Task.Delay(5000);
      }
    }

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
