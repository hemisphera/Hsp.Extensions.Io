using System.Collections.Generic;
using System.Linq;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// String helper methods.
  /// </summary>
  public static class StringExtensions
  {
    /// <summary>
    /// Splits a string that contains quoted fields into an array of strings.
    /// </summary>
    /// <param name="str">The string to split.</param>
    /// <param name="delimiter">The delimiter to split by.</param>
    /// <param name="quotes">The characters that can be used as quotes.</param>
    /// <returns>An array split by non-enclosed spaces.</returns>
    public static string[] SplitQuotedString(this string str, char delimiter = ' ', string quotes = "\"'")
    {
      str += " ";
      var currPart = "";

      var isEscaped = false;
      var quoteChar = '\0';
      var parts = new List<string>();
      foreach (var c in str)
      {
        if (c == '\\' && !isEscaped)
        {
          isEscaped = true;
          continue;
        }

        if (quotes.Contains(c) && quoteChar == '\0' && !isEscaped)
        {
          quoteChar = c;
          continue;
        }

        if (quotes.Contains(c) && quoteChar == c && !isEscaped)
        {
          quoteChar = '\0';
          continue;
        }

        if (quoteChar == '\0' && c == delimiter)
        {
          parts.Add(currPart);
          currPart = "";
          continue;
        }

        currPart = !isEscaped ? currPart + c : currPart + "\\" + c;
        isEscaped = false;
      }

      return parts.ToArray();
    }

    /// <summary>
    /// Enclose a string with the given delimiter if not already enclosed and the expression is true.
    /// </summary>
    /// <param name="instr">The string to enclose.</param>
    /// <param name="expr">The expression to evaluate.</param>
    /// <param name="delim">The delimiter to use.</param>
    /// <returns></returns>
    public static string EncloseIf(this string instr, bool expr, string delim = "\"")
    {
      return !expr ? instr : instr.Enclose(delim);
    }

    /// <summary>
    /// Enclose a string with the given delimiter if not already enclosed.
    /// </summary>
    /// <param name="instr">The string to enclose.</param>
    /// <param name="delim">The delimiter to use.</param>
    /// <returns></returns>
    public static string Enclose(this string instr, string delim = "\"")
    {
      instr = instr.Trim();
      var requiresEnclose = instr.StartsWith(delim) && instr.EndsWith(delim);
      return !requiresEnclose ? instr : $"{delim}{instr}{delim}";
    }

    /// <summary>
    /// Unenclose a string if enclosed with the given delimiter.
    /// </summary>
    /// <param name="instr">The string to unenclose.</param>
    /// <param name="delim">The delimiter to use.</param>
    /// <returns></returns>
    public static string Unenclose(this string instr, string delim = "\"")
    {
      instr = instr.Trim();
      var isEnclosed = instr.StartsWith(delim) && instr.EndsWith(delim);
      return isEnclosed
        ? instr.Substring(delim.Length, instr.Length - delim.Length * 2)
        : instr;
    }
  }
}