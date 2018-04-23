using System;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Durandal.Services
{
  public class MessageService
  {
    private readonly DiscordSocketClient discord;
    private readonly CommandService commands;
    private IServiceProvider provider;

    public MessageService(
      IServiceProvider provider, 
      DiscordSocketClient discord, 
      CommandService commands)
    {
      this.provider = provider;
      this.discord = discord;
      this.commands = commands;

      this.discord.MessageReceived += MessageReceived;
    }

    public async Task Initialize(IServiceProvider provider)
    {
      this.provider = provider;
      await this.commands.AddModulesAsync(Assembly.GetEntryAssembly());
      // Add additional initialization code here...
    }

    private async Task MessageReceived(SocketMessage rawMessage)
    {
      // Ignore system messages and messages from bots
      if (!(rawMessage is SocketUserMessage message))
        return;
      if (message.Source != MessageSource.User)
        return;

      int argPos = 0;
      if (!message.HasMentionPrefix(discord.CurrentUser, ref argPos))
        return;

      var context = new SocketCommandContext(discord, message);
      var result = 
        await this.commands.ExecuteAsync(context, argPos, this.provider);

      if (result.Error.HasValue)
        if (result.Error.Value != CommandError.UnknownCommand)
          await context.Channel.SendMessageAsync(result.ToString());
    }
  }
}
