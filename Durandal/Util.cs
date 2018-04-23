using System;
using System.Text;

namespace Durandal
{
  public static class Util
  {
    public static TimeSpan ParseHuman(string timeString)
    {
      TimeSpan span = TimeSpan.Zero;
      StringBuilder number = new StringBuilder();

      foreach (char next in timeString)
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
  }
}
