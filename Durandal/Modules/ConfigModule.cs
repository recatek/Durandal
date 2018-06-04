using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Durandal.Services;

namespace Durandal.Modules
{
  public class ConfigModule : ModuleBase<SocketCommandContext>
  {
    private const int MAX_PURGE = 100;

    private readonly DatabaseService database;
    private readonly LoggingService logging;
    private readonly ulong ownerId;

    public ConfigModule(
      DatabaseService database,
      LoggingService logging,
      IConfiguration config)
    {
      this.database = database;
      this.logging = logging;
      this.ownerId = ulong.Parse(config["ownerId"] ?? "0");
    }

    #region ShowConfig
    [Priority(0)]
    [Command("showconfig")]
    [Summary("Displays the config data for the given server.")]
    public async Task ShowConfig()
    {
      // Only the bot owner can do this
      if (Context.User.Id != this.ownerId)
        return;

      var channel = this.database.GetLogChannel(this.Context.Guild);
      var role = this.database.GetTimeoutRole(this.Context.Guild);

      string channelMention = 
        (channel == null) 
        ? "None" 
        : MentionUtils.MentionChannel(channel.Id);
      string roleMention = role?.Mention ?? "None";

      await this.ReplyAsync(
        $"Log Channel: {channelMention}\n" +
        $"Timeout Role: {roleMention}");
    }

    [Command("showconfig")]
    [Summary("Displays the config data for the given server.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task ShowConfig([Remainder]string _)
    {
      // Only the bot owner can do this
      if (Context.User.Id != this.ownerId)
        return;

      await this.ShowConfig();
    }
    #endregion

    #region SetLog
    [Priority(0)]
    [Command("setlog")]
    [Summary("Set the current channel as the logging channel for this discord.")]
    public async Task SetLog()
    {
      // Only the bot owner can do this
      if (Context.User.Id != this.ownerId)
        return;

      this.database.SetLogChannel(
        this.Context.Guild,
        this.Context.Channel);

      if (this.database.TryGetLogChannel(this.Context.Guild, out var set))
        await set.SendMessageAsync("Confirmed, logging in this channel.");
      else
        this.logging.LogInternal(
          Util.CreateLog(
            LogSeverity.Error,
            $"Failed to set logging channel: {this.Context.Channel.Id}"));
    }

    [Command("setlog")]
    [Summary("Set the current channel as the logging channel for this discord.")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task SetLog([Remainder]string _)
    {
      // Only the bot owner can do this
      if (Context.User.Id != this.ownerId)
        return;

      await this.SetLog();
    }
    #endregion

    #region SetTimeout
    [Priority(0)]
    [Command("settimeout")]
    [Summary("Set the timeout role for this discord.")]
    public async Task SetTimeout(SocketRole role)
    {
      // Only the bot owner can do this
      if (Context.User.Id != this.ownerId)
        return;

      if (role != null)
      {
        this.database.SetTimeoutRole(this.Context.Guild, role);
        await this.ReplyAsync($"Setting timeout role to: {role.Mention}.");
      }
      else
      {
        await this.ReplyAsync($"Unrecognized role.");
      }
    }

    [Command("settimeout")]
    [Summary("Set the timeout role for this discord.")]
    public async Task SetTimeout()
    {
      // Only the bot owner can do this
      if (Context.User.Id != this.ownerId)
        return;

      await this.ReplyAsync("Usage: `!settimeout <@role>`");
    }

    [Command("settimeout")]
    [Summary("Set the timeout role for this discord.")]
    public async Task SetTimeout([Remainder]string _)
    {
      // Only the bot owner can do this
      if (Context.User.Id != this.ownerId)
        return;

      await this.ReplyAsync("Usage: `!settimeout <@role>`");
    }
    #endregion
  }
}
