using System;
using System.Collections.Generic;
using System.Linq;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// The image of a windows service.
  /// </summary>
  public class ServiceImage
  {
    /// <summary>
    /// The filename to the executable.
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    /// The arguments string.
    /// </summary>
    public string Arguments => GetArgumentsString();

    /// <summary>
    /// The split parts of the arguments.
    /// </summary>
    public List<string> ArgumentParts { get; } = new List<string>();


    /// <summary>
    /// Creates an instance from a windows services binary path.
    /// </summary>
    /// <param name="imgPath">The path to the binary.</param>
    /// <returns>An instance.</returns>
    public static ServiceImage FromBinPath(string imgPath)
    {
      var parts = imgPath.SplitQuotedString().ToList();
      if (!parts.Any()) return null;

      var filename = parts[0];
      parts.RemoveAt(0);
      // try to cope with spaces in the filename that aren't correctly escaped with quotes
      // in this case just stupidly keep adding parts until one is found that ends with .exe
      if (!filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !filename.StartsWith("\""))
      {
        while (parts.Any())
        {
          var nextPart = parts[0];
          filename = filename + " " + nextPart;
          parts.RemoveAt(0);
          if (nextPart.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) break;
        }
      }

      return new ServiceImage(filename, parts.ToArray());
    }

    /// <summary>
    /// Creates an instance from a filename and argument string.
    /// </summary>
    /// <param name="filename">The executable filename.</param>
    /// <param name="arguments">The arguments string.</param>
    public ServiceImage(string filename, string arguments = null)
      : this(filename, arguments.SplitQuotedString())
    {
    }

    /// <summary>
    /// Creates an instance from a filename and argument parts.
    /// </summary>
    /// <param name="filename">The executable filename.</param>
    /// <param name="arguments">The argument parts.</param>
    public ServiceImage(string filename, params string[] arguments)
    {
      Filename = filename;
      ArgumentParts.AddRange(arguments);
    }


    /// <inheritdoc />
    public override string ToString()
    {
      var exeName = $"{Filename.EncloseIf(Filename.Contains(" "))}";
      return string.IsNullOrEmpty(Arguments)
        ? exeName
        : $"{exeName} {Arguments}";
    }

    /// <summary>
    /// Creates a string from the argument parts.
    /// </summary>
    /// <param name="enclose">Indicates whether to enclose parts with spaces.</param>
    /// <returns></returns>
    public string GetArgumentsString(bool enclose = true)
    {
      return string.Join(" ", ArgumentParts.Select(arg => arg.EncloseIf(enclose && arg.Contains(" "))));
    }
  }
}