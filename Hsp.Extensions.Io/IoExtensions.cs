using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// Helpers and utility functions for IO operations
  /// </summary>
  public static class FilesystemExtensions
  {
    /// <summary>
    /// Normalizes the directory separator of the given path to the current platform's directory separator.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string NormalizeDirectorySeparator(this string path)
    {
      return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Resolves the string as a relatve path from the given root path. 
    /// </summary>
    /// <param name="relativePath">The relative path to resolve.</param>
    /// <param name="rootPath">The root path to resolve from.</param>
    /// <returns>The resolved path.</returns>
    public static string ResolvePathFrom(this string relativePath, string rootPath)
    {
      if (string.IsNullOrEmpty(relativePath))
        relativePath = ".";
      return string.IsNullOrEmpty(rootPath)
        ? relativePath
        : Path.GetFullPath(Path.Combine(rootPath, relativePath));
    }


    /// <summary>
    /// Deletes all empty folders within a directory tree.
    /// </summary>
    /// <param name="dir">The root folder</param>
    /// <param name="excludeHidden">Specifies whether hidden folders are to be excluded. Folders starting with a '.' (dot) or the hidden attribute set are considered hidden</param>
    public static void DeleteEmptyFolders(this DirectoryInfo dir, bool excludeHidden = true)
    {
      if (!dir.Exists) throw new InvalidOperationException($"The directory '{dir.FullName}' does not exist.");

      var subFolders = dir.EnumerateDirectories();
      foreach (var subFolder in subFolders)
      {
        var canDelete = true;
        if (excludeHidden)
        {
          var isHidden = subFolder.Name.StartsWith(".") || subFolder.Attributes.HasFlag(FileAttributes.Hidden);
          canDelete = !isHidden;
        }

        if (canDelete)
          subFolder.DeleteEmptyFolders();
      }

      if (!dir.EnumerateFileSystemInfos().Any())
        dir.Delete();
    }

    /// <summary>
    /// Copies the results of a file collection operation to a new folder.
    /// </summary>
    /// <param name="files">The collected files.</param>
    /// <param name="targetFolder">The target folder.</param>
    /// <param name="deleteSource">Specifies whether the source files should be deleted after being copied.</param>
    public static void CopyTo(this CollectFilesResult files, string targetFolder, bool deleteSource = false)
    {
      foreach (var file in files)
      {
        var sourceFullPath = Path.Combine(files.Root.FullName, file);
        var targetFullPath = Path.Combine(targetFolder, file);
        var targetDirectory = Path.GetDirectoryName(targetFullPath);
        if (!string.IsNullOrEmpty(targetDirectory))
          Directory.CreateDirectory(targetDirectory);
        File.Copy(sourceFullPath, targetFullPath, true);
        if (deleteSource)
          File.Delete(sourceFullPath);
      }

      if (deleteSource)
        files.Root.DeleteEmptyFolders();
    }

    /// <summary>
    /// Copies an entire directory tree from source to destination
    /// </summary>
    /// <param name="dir">The source folder</param>
    /// <param name="targetPath">The target folder</param>
    public static void CopyTo(this DirectoryInfo dir, string targetPath)
    {
      dir.CopyTo(targetPath, false);
    }

    /// <summary>
    /// Copies an entire directory tree from source to destination
    /// </summary>
    /// <param name="dir">The source folder</param>
    /// <param name="targetPath">The target folder</param>
    /// <param name="deleteSourceFile">Specifies whether files should be deleted after being copied</param>
    public static void CopyTo(this DirectoryInfo dir, string targetPath, bool deleteSourceFile)
    {
      dir.CopyTo(targetPath, null, deleteSourceFile);
    }

    /// <summary>
    /// Copies an entire directory tree from source to destination
    /// </summary>
    /// <param name="dir">The source folder</param>
    /// <param name="targetPath">The target folder</param>
    /// <param name="callback">A callback that is called for every file found within the folder.</param>
    /// <param name="deleteSourceFile">Specifies whether files should be deleted after being copied</param>
    public static void CopyTo(this DirectoryInfo dir, string targetPath, Func<FileSystemInfo, bool?>? callback, bool deleteSourceFile = false)
    {
      dir.ForEachFolder(subDir =>
      {
        if (callback?.Invoke(subDir) == false) return;
        subDir.CopyTo(Path.Combine(targetPath, subDir.Name), deleteSourceFile);
      }, false);

      dir.ForEachFile(file =>
      {
        if (callback?.Invoke(file) == false) return;

        var targetFile = new FileInfo(Path.Combine(targetPath, file.Name));
        targetFile.Directory?.Create();
        file.CopyTo(targetFile.FullName, true);
        if (deleteSourceFile) file.Delete();
      }, false);

      if (deleteSourceFile)
        dir.DeleteEmptyFolders();
    }

    /// <summary>
    /// Checks if a file is currently locked by checking if it is writeable. Note that this function returns 'true' also for files where the current user does not have write permission
    /// </summary>
    /// <param name="file">The file to check</param>
    /// <returns>'true' if locked, 'false' otherwise</returns>
    public static bool IsLocked(this FileInfo file)
    {
      try
      {
        using (var fs = File.OpenWrite(file.FullName))
          fs.Lock(0, fs.Length);
        return false;
      }
      catch (IOException)
      {
        return true;
      }
    }

    /// <summary>
    /// Checks if a folder is locked. A folder is considered locked, if it contains at least one file that is locked
    /// </summary>
    /// <param name="dir">The folder to check</param>
    /// <param name="so">A search option that specifies how files are to be searched</param>
    /// <returns>'true' if locked, 'false' otherwise</returns>
    public static bool IsLocked(this DirectoryInfo dir, SearchOption so = SearchOption.AllDirectories)
    {
      var isLocked = false;
      Parallel.ForEach(dir.EnumerateFiles("*", so), file =>
      {
        if (isLocked) return;
        if (file.IsLocked()) isLocked = true;
      });
      return isLocked;
    }


    /// <summary>
    /// Runs an action for each file within the given folder.
    /// </summary>
    /// <param name="folder">The folder to run on.</param>
    /// <param name="callback">The action to run.</param>
    /// <param name="recurse">Specifies whether to recurse into subfolders.</param>
    public static void ForEachFile(this DirectoryInfo folder, Action<FileInfo> callback, bool recurse = true)
    {
      folder.ForEachEntry(item =>
      {
        if (item is FileInfo fi) callback(fi);
        return null;
      }, recurse);
    }

    /// <summary>
    /// Runs an action for each folder within the given folder.
    /// </summary>
    /// <param name="folder">The folder to run on.</param>
    /// <param name="callback">The action to run.</param>
    /// <param name="recurse">Specifies whether to recurse into subfolders.</param>
    public static void ForEachFolder(this DirectoryInfo folder, Action<DirectoryInfo> callback, bool recurse = true)
    {
      folder.ForEachEntry(item =>
      {
        if (item is DirectoryInfo di) callback(di);
        return null;
      }, recurse);
    }

    /// <summary>
    /// Collects all files from the given folder matching where the given callback returns true.
    /// </summary>
    /// <param name="folder">The folder to collect files from.</param>
    /// <param name="callback">The callback to determine whether to include the file or not.</param>
    /// <returns>The collection result.</returns>
    public static CollectFilesResult CollectFiles(this DirectoryInfo folder, Func<FileSystemInfo, bool?>? callback = null)
    {
      var items = new CollectFilesResult(folder);
      folder.ForEachEntry(args =>
      {
        var response = callback?.Invoke(args);
        if (response != false) items.Add(args.FullName);
        return response;
      });
      return items;
    }

    /// <summary>
    /// Collects all files from the given folder matching the given include and exclude patterns.
    /// </summary>
    /// <param name="folder">The source path to collect files from.</param>
    /// <param name="includePatterns">The include patterns. If this is not specified, all files are included.</param>
    /// <param name="excludePatterns">The exclude patterns.</param>
    /// <returns>The collection result.</returns>
    public static CollectFilesResult CollectFiles(this DirectoryInfo folder, string[]? includePatterns, string[]? excludePatterns = null)
    {
      if (includePatterns?.Any() != true)
        includePatterns = new[] { "**/*" };

      var matcher = new Matcher();
      matcher.AddIncludePatterns(includePatterns);
      if (excludePatterns?.Any() == true)
        matcher.AddExcludePatterns(excludePatterns);
      var matchResult = matcher.Execute(new DirectoryInfoWrapper(folder));
      var result = new CollectFilesResult(folder);
      foreach (var mr in matchResult.Files)
      {
        var fullPath = Path.Combine(folder.FullName, mr.Path);
        result.Add(fullPath);
      }

      return result;
    }

    /// <summary>
    /// Recursively executes the given callback for each file and folder within the given folder.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="recurse">Specifies whether to recurse into subfolders.</param>
    public static void ForEachEntry(this DirectoryInfo folder, Func<FileSystemInfo, bool?>? callback, bool recurse = false)
    {
      foreach (var subDir in folder.EnumerateDirectories())
      {
        var skip = callback?.Invoke(subDir) == false;
        if (recurse && !skip) subDir.ForEachEntry(callback, true);
      }

      foreach (var file in folder.EnumerateFiles())
      {
        callback?.Invoke(file);
      }
    }
  }
}