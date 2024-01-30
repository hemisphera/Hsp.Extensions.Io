using System;
using System.IO;
using Ionic.Zip;

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
      if (String.IsNullOrEmpty(FolderPath))
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
      using (var zf = new ZipFile())
      {
        zf.AddDirectory(FolderPath);
        zf.Save(s);
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
      using (var fs = File.OpenWrite(filename))
        SaveAsZip(fs);
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
    /// Writes a stream as file into the folder.
    /// </summary>
    /// <param name="filename">The name of the file to create.</param>
    /// <param name="data">The data stream of the file to write.</param>
    /// <returns>The full path to the file created.</returns>
    public string WriteFile(string filename, Stream data)
    {
      var fullFilename = Path.Combine(FolderPath, filename);
      using (var fs = File.Create(fullFilename))
        data.CopyTo(fs);

      return fullFilename;
    }

    /// <summary>
    /// Extracts all the contents of a ZIP stream into the folder.
    /// </summary>
    /// <param name="s">The stream containing a ZIP file.</param>
    /// <param name="subFolder">An optional subfolder where to place the files in. If empty, the files are placed into the root.</param>
    /// <returns>The full path to the folder where the extracted files reside.</returns>
    public string ExtractArchive(Stream s, string subFolder = "")
    {
      var folder = FolderPath;
      using (var za = ZipFile.Read(s))
      {
        if (!String.IsNullOrEmpty(subFolder))
          folder = Path.Combine(folder, subFolder);
        za.ExtractAll(folder, ExtractExistingFileAction.OverwriteSilently);
      }

      return folder;
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