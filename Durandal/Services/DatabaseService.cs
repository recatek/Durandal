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

    private class ServerData
    {
      [BsonId]
      public ulong ServerId { get; set; }

      public ulong LogChannelId { get; set; }
      public ulong TimeoutRoleId { get; set; }

      public ConcurrentDictionary<ulong, long> Timeouts { get; set; }
      public ConcurrentDictionary<ulong, bool> Namelocks { get; set; }

      public ServerData() { }

      public ServerData(ulong serverId)
      {
        this.ServerId = serverId;

        this.Timeouts = new ConcurrentDictionary<ulong, long>();
        this.Namelocks = new ConcurrentDictionary<ulong, bool>();
      }
    }

    public event Action<ulong> ServerDataLoaded;

    private readonly DiscordSocketClient discord;
    private readonly LoggingService logging;

    private readonly ConcurrentDictionary<ulong, ServerData> servers;

    private LiteDatabase database;
    private LiteCollection<ServerData> serverCollection;

    #region Timeout
    public void SetTimeout(
      ulong serverId,
      ulong playerId, 
      long expiration)
    {
      if (this.VerifyGetServer(serverId, out ServerData data))
      {
        data.Timeouts[playerId] = expiration;
        this.serverCollection.Update(data);
      }
    }

    public void ClearTimeout(
      ulong serverId,
      ulong playerId)
    {
      if (this.VerifyGetServer(serverId, out ServerData data))
      {
        data.Timeouts.TryRemove(playerId, out long _);
        this.serverCollection.Update(data);
      }
    }

    public IEnumerable<KeyValuePair<ulong, long>> GetTimeouts(ulong serverId)
    {
      if (this.VerifyGetServer(serverId, out ServerData data))
        return data.Timeouts;
      return Enumerable.Empty<KeyValuePair<ulong, long>>();
    }
    #endregion

    public DatabaseService(
      DiscordSocketClient discord,
      LoggingService logging)
    {
      this.discord = discord;
      this.logging = logging;

      this.servers = new ConcurrentDictionary<ulong, ServerData>();

      discord.GuildAvailable += this.OnGuildAvailable;
    }

    public Task Initialize(IServiceProvider provider)
    {
      this.database = new LiteDatabase(FILE_NAME);
      this.serverCollection = 
        this.database.GetCollection<ServerData>(SERVER_COLLECTION);
      this.serverCollection.EnsureIndex(x => x.ServerId);

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
    private bool VerifyGetServer(ulong serverId, out ServerData data)
    {
      if (this.servers.TryGetValue(serverId, out data))
        return true;

      this.logging.LogInternal(
        Util.CreateLog(
          LogSeverity.Error,
          $"Unrecognized server {serverId}"));
      return false;
    }

    /// <summary>
    /// Gets the given guild from the server collection or adds it.
    /// </summary>
    private ServerData GetOrAdd(SocketGuild guild)
    {
      ServerData data = 
        this.serverCollection.FindOne(
          x => x.ServerId == guild.Id);

      if (data == null)
      {
        data = new ServerData(guild.Id);
        this.serverCollection.Insert(data);
      }

      return data;
    }
  }
}
