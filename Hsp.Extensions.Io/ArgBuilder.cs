using System;
using System.Collections.Generic;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// Utility class for building command line arguments.
  /// </summary>
  public sealed class ArgBuilder
  {
    private readonly List<string> _args = new List<string>();

    private ArgBuilder()
    {
    }


    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <returns>A new instance.</returns>
    public static ArgBuilder Create()
    {
      return new ArgBuilder();
    }

    /// <summary>
    /// Add the given line to the argument list.
    /// </summary>
    /// <param name="line">The line to add.</param>
    /// <returns>The current instance.</returns>
    public ArgBuilder Add(string line)
    {
      _args.Add(line);
      return this;
    }

    /// <summary>
    /// Add the given line to the argument list, if the given value is not null or empty.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="line">The line to add.</param>
    /// <returns>The current instance.</returns>
    public ArgBuilder AddIf(string value, string line)
    {
      return AddIf(!String.IsNullOrEmpty(value), line);
    }

    /// <summary>
    /// Add the given line to the argument list, if the given expression is true.
    /// </summary>
    /// <param name="expr">The expression to check.</param>
    /// <param name="line">The line to add.</param>
    /// <returns>The current instance.</returns>
    public ArgBuilder AddIf(bool expr, string line)
    {
      if (expr) Add(line);
      return this;
    }

    /// <summary>
    /// Return the argument list as an array.
    /// </summary>
    /// <returns>The argument list as an array.</returns>
    public string[] ToArray()
    {
      return _args.ToArray();
    }

    /// <summary>
    /// Return the argument list as a string.
    /// </summary>
    /// <returns>The argument list as a string.</returns>
    public string ToArgString()
    {
      return string.Join(" ", ToArray());
    }
  }
}