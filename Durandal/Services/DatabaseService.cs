using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Discord;
using Discord.WebSocket;

using LiteDB;

namespace Durandal.Services
{
  public class DatabaseService
  {
    // TODO: What if we leave a server?

    private const string FILE_NAME = "data.db";

    private const string SERVER_COLLECTION = "servers";

    private class GuildData
    {
      [BsonId]
      public ulong GuildId { get; set; }

      public ulong LogChannelId { get; set; }
      public ulong TimeoutRoleId { get; set; }

      public ConcurrentDictionary<ulong, DateTimeOffset> Timeouts { get; set; }
      public ConcurrentDictionary<ulong, bool> Namelocks { get; set; }

      public GuildData() { }

      public GuildData(ulong guildId)
      {
        this.GuildId = guildId;

        this.Timeouts = new ConcurrentDictionary<ulong, DateTimeOffset>();
        this.Namelocks = new ConcurrentDictionary<ulong, bool>();
      }
    }

    public event Action<ulong> ServerDataLoaded;

    private readonly DiscordSocketClient discord;
    private readonly LoggingService logging;

    private readonly ConcurrentDictionary<ulong, GuildData> servers;

    private LiteDatabase database;
    private LiteCollection<GuildData> serverCollection;

    #region Servers
    public ICollection<ulong> GetOpenServers()
    {
      return this.servers.Keys;
    }
    #endregion

    #region LogChannel
    public void SetLogChannel(
      SocketGuild guild,
      ISocketMessageChannel logChannel)
    {
      if (this.VerifyGetServer(guild.Id, out GuildData data))
      {
        data.LogChannelId = logChannel.Id;
        this.serverCollection.Update(data);
      }
    }

    public ISocketMessageChannel GetLogChannel(
      SocketGuild guild)
    {
      if (this.VerifyGetServer(guild.Id, out GuildData data))
        return guild.GetChannel(data.LogChannelId) as ISocketMessageChannel;
      return null;
    }

    public bool TryGetLogChannel(
      SocketGuild guild,
      out ISocketMessageChannel channel)
    {
      channel = this.GetLogChannel(guild);
      return (channel != null);
    }
    #endregion

    #region Timeout
    public void SetTimeoutRole(
      SocketGuild guild,
      SocketRole role)
    {
      if (this.VerifyGetServer(guild.Id, out GuildData data))
      {
        data.TimeoutRoleId = role.Id;
        this.serverCollection.Update(data);
      }
    }

    public SocketRole GetTimeoutRole(
      SocketGuild guild)
    {
      if (this.VerifyGetServer(guild.Id, out GuildData data))
        return guild.GetRole(data.TimeoutRoleId);
      return default;
    }

    public void SetTimeout(
      SocketGuild guild,
      SocketUser user, 
      DateTimeOffset expiration)
    {
      if (this.VerifyGetServer(guild.Id, out GuildData data))
      {
        data.Timeouts[user.Id] = expiration;
        this.serverCollection.Update(data);
      }
    }

    /// <summary>
    /// Removes all expired timeouts given a time and updates the database.
    /// Returns tuples of (guildId, userId) of the expired timeouts.
    /// </summary>
    public List<Tuple<ulong, ulong>> ExpireTimeouts(
      DateTime currentTime)
    {
      // Pair is (guildId, userId)
      List<Tuple<ulong, ulong>> expired = new List<Tuple<ulong, ulong>>();
      List<ulong> toRemove = new List<ulong>();

      foreach (GuildData data in this.servers.Values)
      {
        toRemove.Clear();

        foreach (var timeout in data.Timeouts)
        {
          if (timeout.Value < currentTime)
          {
            expired.Add(new Tuple<ulong, ulong>(data.GuildId, timeout.Key));
            toRemove.Add(timeout.Key);
          }
        }

        if (toRemove.Count > 0)
        {
          foreach (ulong userId in toRemove)
            data.Timeouts.TryRemove(userId, out var unused);
          this.serverCollection.Update(data);
        }
      }

      return expired;
    }
    #endregion

    public DatabaseService(
      DiscordSocketClient discord,
      LoggingService logging)
    {
      this.discord = discord;
      this.logging = logging;

      this.servers = new ConcurrentDictionary<ulong, GuildData>();

      discord.GuildAvailable += this.OnGuildAvailable;
    }

    public Task Initialize(IServiceProvider provider)
    {
      this.database = new LiteDatabase(FILE_NAME);
      this.serverCollection = 
        this.database.GetCollection<GuildData>(SERVER_COLLECTION);
      this.serverCollection.EnsureIndex(x => x.GuildId);

      return Task.CompletedTask;
    }

    private Task OnGuildAvailable(SocketGuild guild)
    {
      this.logging.LogInternal(
        Util.CreateLog(
          LogSeverity.Info, 
          $"Opening database for {guild.Name}"));
      this.servers[guild.Id] = this.GetOrAdd(guild);

      this.ServerDataLoaded?.Invoke(guild.Id);

      return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a ServerData by Id and verifies its validity.
    /// </summary>
    private bool VerifyGetServer(ulong guildId, out GuildData data)
    {
      if (this.servers.TryGetValue(guildId, out data))
        return true;

      this.logging.LogInternal(
        Util.CreateLog(
          LogSeverity.Error,
          $"Unrecognized server {guildId}"));
      return false;
    }

    /// <summary>
    /// Gets the given guild from the server collection or adds it.
    /// </summary>
    private GuildData GetOrAdd(SocketGuild guild)
    {
      GuildData data = 
        this.serverCollection.FindOne(
          x => x.GuildId == guild.Id);

      if (data == null)
      {
        data = new GuildData(guild.Id);
        this.serverCollection.Insert(data);
      }

      return data;
    }
  }
}
