using System.IO;
using System.IO.Compression;
using System.Text;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// Extensions for ZIP archives
  /// </summary>
  public static class ZipExtensions
  {
    /// <summary>
    /// Adds an entry with the given name and the text contents (and encoding) to the ZIP archive.
    /// </summary>
    /// <param name="archive">The ZIP archive.</param>
    /// <param name="entryName">The name of the archive.</param>
    /// <param name="content">The content to add.</param>
    /// <param name="encoding">The encoding to use for the contents.</param>
    public static void AddEntry(this ZipArchive archive, string entryName, string content, Encoding? encoding = null)
    {
      using (var ms = new MemoryStream())
      using (var sw = new StreamWriter(ms, encoding ?? Encoding.UTF8, 4096, true))
      {
        sw.Write(content);
        sw.Flush();
        ms.Position = 0;
        archive.AddEntry(entryName, ms);
      }
    }

    /// <summary>
    /// Adds an entry with the given name and the data contents to the ZIP archive.
    /// </summary>
    /// <param name="archive">The ZIP archive.</param>
    /// <param name="entryName">The name of the archive.</param>
    /// <param name="content">The content to add.</param>
    public static void AddEntry(this ZipArchive archive, string entryName, byte[] content)
    {
      using (var ms = new MemoryStream(content))
      {
        archive.AddEntry(entryName, ms);
      }
    }

    /// <summary>
    /// Adds an entry with the given name and the data contents to the ZIP archive.
    /// </summary>
    /// <param name="archive">The ZIP archive.</param>
    /// <param name="entryName">The name of the archive.</param>
    /// <param name="content">The content to add.</param>
    public static void AddEntry(this ZipArchive archive, string entryName, Stream content)
    {
      var entry = archive.CreateEntry(entryName);
      using (var s = entry.Open())
      {
        content.CopyTo(s);
      }
    }
  }
}