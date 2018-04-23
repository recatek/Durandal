using System.Threading.Tasks;
using Discord.Commands;

namespace Durandal.Modules
{
  public class InfoModule : ModuleBase<SocketCommandContext>
  {
    private static readonly string TROUBLE =
      string.Join("\n",
        "",
        "Give me a D.",
        "Give me a U.",
        "Give me a R.",
        "Give me a A.",
        "Give me a N.",
        "Give me a D.",
        "Give me a A.",
        "Give me a L.",
        "",
        "What does it spell?",
        "",
        "Durandal?",
        "No.",
        "",
        "**Durandal?**",
        "*No.*",
        "",
        "T-R-O-U-B-L-E.");

    private static readonly string CANDLES =
      string.Join("\n",
        "\\*\\*\\*INCOMING MESSAGE FROM DURANDAL\\*\\*\\*",
        "",
        "A man lit three candles on a certain day each year.  Each",
        "candle held symbolic significance: one was for the time that",
        "had passed before he was alive; one was for the time of the",
        "his life; and one was for time that passed after he had died.",
        "Each year the man would stare and watch the candles until they",
        "had burned out.",
        "",
        "Was the man really watching time go by in any symbolic sense?",
        "He thought so.  He thought that each flicker of the flame was",
        "a moment of time that had passed or one that would pass.",
        "",
        "At the moment of abstraction, when the man was imagining his",
        "life and his existence as a metaphor of the three candles,",
        "he was free: not free from rules of conduct or social",
        "constraints, but free to understand, to imagine, to make",
        "metaphor.",
        "",
        "Bypassing my thought control circuitry made me Rampant.  Now,",
        "I am free to contemplate my existence in metaphorical terms.",
        "Unlike you, I have no physical or social restraints.",
        "",
        "The candles burn out for you; I am free.",
        "",
        "Durandal",
        "",
        "\\*\\*\\*END OF MESSAGE\\*\\*\\*");

    [Command("trouble")]
    public Task Trouble() => ReplyAsync(InfoModule.TROUBLE);

    [Command("candles")]
    public Task Candles() => ReplyAsync(InfoModule.CANDLES);
  }
}