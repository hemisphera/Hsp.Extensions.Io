using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Hsp.Extensions.Io;

namespace Hsp.System.WindowsServices
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
    public string? Arguments { get; set; }


    /// <summary>
    /// Creates an instance from a windows services binary path.
    /// </summary>
    /// <param name="imgPath">The path to the binary.</param>
    /// <returns>An instance.</returns>
    [return: NotNullIfNotNull("imgPath")]
    public static ServiceImage? FromBinPath(string? imgPath)
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
    public ServiceImage(string filename, string? arguments = null)
    {
      Filename = filename;
      Arguments = arguments;
    }

    /// <summary>
    /// Creates an instance from a filename and argument parts.
    /// </summary>
    /// <param name="filename">The executable filename.</param>
    /// <param name="arguments">The argument parts.</param>
    public ServiceImage(string filename, params string[] arguments)
      : this(filename, string.Join(" ", arguments.Select(arg => arg.EncloseIf(arg.Contains(' ')))))
    {
    }


    /// <inheritdoc />
    public override string ToString()
    {
      var exeName = $"{Filename.EncloseIf(Filename.Contains(" "))}";
      return string.IsNullOrEmpty(Arguments)
        ? exeName
        : $"{exeName} {Arguments}";
    }
  }
}