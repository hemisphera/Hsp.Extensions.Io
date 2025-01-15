using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// A temporary folder that is deleted upon dispose
  /// </summary>
  public class TempFolder : IDisposable
  {
    /// <summary>
    /// Indicates the full folder path of the temporary folder
    /// </summary>
    public string FolderPath => Folder.FullName;

    /// <summary>
    /// The folder object.
    /// </summary>
    public DirectoryInfo Folder { get; }


    /// <summary>
    /// </summary>
    /// <param name="folderPath">Specfies the folder name to be used. If this is empty, a random default name will be generated</param>
    public TempFolder(string folderPath = "")
    {
      if (string.IsNullOrEmpty(folderPath))
        folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      Folder = new DirectoryInfo(folderPath);
      Folder.Create();
    }


    /// <summary>
    /// Saves the contents of the folder as a ZIP file
    /// </summary>
    /// <param name="s">The output stream where the ZIP file will be placed</param>
    /// <param name="rewindStream">Specifies if the output stream should be rewinded back to position 0 after creation</param>
    public void SaveAsZip(Stream s, bool rewindStream = false)
    {
      var tf = new FileInfo(Path.GetTempFileName());
      try
      {
        SaveAsZip(tf.FullName);
        using (var fs = tf.OpenRead())
          fs.CopyTo(s);
      }
      finally
      {
        if (tf.Exists) tf.Delete();
      }

      if (rewindStream)
        s.Position = 0;
    }

    /// <summary>
    /// Saves the contents of the folder as a ZIP file
    /// </summary>
    /// <param name="filename">The full path to the ZIP filename to be created</param>
    public void SaveAsZip(string filename)
    {
      if (File.Exists(filename)) File.Delete(filename);
      ZipFile.CreateFromDirectory(FolderPath, filename);
    }

    /// <summary>
    /// Writes a string as file into the folder.
    /// </summary>
    /// <param name="filename">The name of the file to create.</param>
    /// <param name="data">The data of the file to write.</param>
    /// <param name="encoding">The encoding to use for the file. If null, UTF-8 is used.</param>
    /// <returns>The full path to the file created.</returns>
    public string WriteFile(string filename, string data, Encoding? encoding = null)
    {
      var bytes = (encoding ?? Encoding.UTF8).GetBytes(data);
      return WriteFile(filename, bytes);
    }

    /// <summary>
    /// Writes the strings as file into the folder terminating each line with [newline].
    /// </summary>
    /// <param name="filename">The name of the file to create.</param>
    /// <param name="data">The data of the file to write.</param>
    /// <param name="encoding">The encoding to use for the file. If null, UTF-8 is used.</param>
    /// <returns>The full path to the file created.</returns>
    public string WriteFile(string filename, IEnumerable<string> data, Encoding? encoding = null)
    {
      return WriteFile(filename, string.Join(Environment.NewLine, data), encoding);
    }

    /// <summary>
    /// Writes a byte array as file into the folder.
    /// </summary>
    /// <param name="filename">The name of the file to create.</param>
    /// <param name="data">The data of the file to write.</param>
    /// <returns>The full path to the file created.</returns>
    public string WriteFile(string filename, byte[] data)
    {
      using (var ms = new MemoryStream(data))
        return WriteFile(filename, ms);
    }

    /// <summary>
    /// Gets the file info of a file in the folder.
    /// </summary>
    /// <param name="path">The path to the file within the folder.</param>
    /// <returns>The file info.</returns>
    public FileInfo GetFile(string path)
    {
      return new FileInfo(Path.Combine(FolderPath, path));
    }

    /// <summary>
    /// Writes a stream as file into the folder.
    /// </summary>
    /// <param name="filename">The name of the file to create.</param>
    /// <param name="data">The data stream of the file to write.</param>
    /// <returns>The full path to the file created.</returns>
    public string WriteFile(string filename, Stream data)
    {
      var fi = GetFile(filename);
      fi.Directory?.Create();
      using (var fs = fi.Create())
        data.CopyTo(fs);

      return fi.FullName;
    }

    /// <summary>
    /// Extracts all the contents of a ZIP stream into the folder.
    /// </summary>
    /// <param name="s">The stream containing a ZIP file.</param>
    /// <param name="subFolder">An optional subfolder where to place the files in. If empty, the files are placed into the root.</param>
    /// <returns>The full path to the folder where the extracted files reside.</returns>
    public string ExtractArchive(Stream s, string subFolder = "")
    {
      using (var za = new ZipArchive(s))
      {
        return Folder.ExtractArchive(za, subFolder);
      }
    }

    /// <summary>
    /// Extracts all the contents of a ZIP data buffer into the folder.
    /// </summary>
    /// <param name="data">A byte array containing a ZIP file.</param>
    /// <param name="subFolder">An optional subfolder where to place the files in. If empty, the files are placed into the root.</param>
    /// <returns>The full path to the folder where the extracted files reside.</returns>
    public string ExtractArchive(byte[] data, string subFolder = "")
    {
      using (var ms = new MemoryStream(data))
        return ExtractArchive(ms, subFolder);
    }


    /// <inheritdoc />
    public void Dispose()
    {
      Folder.Refresh();
      if (!Folder.Exists) return;
      Folder.ForEachFile(file => File.SetAttributes(file.FullName, FileAttributes.Normal));
      Folder.Delete(true);
    }

    /// <inheritdoc />
    public override string ToString()
    {
      return FolderPath;
    }
  }
}