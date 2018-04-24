using System;
using System.Text;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

namespace Durandal
{
  public static class Util
  {
    public static bool TryParseHuman(string timeString, out TimeSpan span)
    {
      try
      {
        span = Util.ParseHuman(timeString);
        return true;
      }
      catch (FormatException)
      {
        return false;
      }
    }
  
    public static TimeSpan ParseHuman(string timeString)
    {
      TimeSpan span = TimeSpan.Zero;
      StringBuilder number = new StringBuilder();

      foreach (char next in timeString.ToLower())
      {
        if (char.IsDigit(next))
        {
          number.Append(next);
        }
        else
        {
          switch (next)
          {
            case 'd':
              span = span.Add(TimeSpan.FromDays(int.Parse(number.ToString())));
              break;

            case 'h':
              span = span.Add(TimeSpan.FromHours(int.Parse(number.ToString())));
              break;

            case 'm':
              span = span.Add(TimeSpan.FromMinutes(int.Parse(number.ToString())));
              break;

            case 's':
              span = span.Add(TimeSpan.FromSeconds(int.Parse(number.ToString())));
              break;

            default:
              throw new FormatException("Bad time value: " + next);
          }

          number.Clear();
        }
      }

      return span;
    }

    public static string PrintHuman(this TimeSpan time)
    {
      List<string> elements = new List<string>();

      if (time.Days > 0)
        elements.Add(time.Days + (time.Days > 1 ? " days" : " day"));
      if (time.Hours > 0)
        elements.Add(time.Hours + (time.Hours > 1 ? " hours" : " hour"));
      if (time.Minutes > 0)
        elements.Add(time.Minutes + (time.Minutes > 1 ? " minutes" : " minute"));
      if (time.Seconds > 0)
        elements.Add(time.Seconds + (time.Seconds > 1 ? " seconds" : " second"));

      return string.Join(", ", elements);
    }

    public static string ReadableName(this SocketUser user)
    {
      return $"{user.Username}#{user.Discriminator}";
    }

    public static LogMessage CreateLog(LogSeverity severity, string message)
    {
      return new LogMessage(severity, null, message);
    }

    public static string GetExtension(string filename)
    {
      string[] split = filename.ToLower().Split('.');
      return split[split.Length - 1];
    }
  }
}
