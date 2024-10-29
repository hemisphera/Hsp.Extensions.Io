using System;
using System.Collections.Generic;

namespace Hsp.System.WindowsServices
{
  internal static class Extensions
  {
    public static void ForEach<T>(this IEnumerable<T> items, Action<T> act)
    {
      foreach (var item in items)
      {
        act(item);
      }
    }
  }
}