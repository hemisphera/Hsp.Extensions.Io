using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// A lock file is a file that is used to indicate that a process is currently running.
  /// </summary>
  public class LockFile : IDisposable
  {
    private readonly string _filePath;

    private static readonly TimeSpan ProcessCheckFrequency = TimeSpan.FromSeconds(5);


    /// <summary>
    /// Creates a new lock file.
    /// </summary>
    /// <param name="path">The path to the lock file.</param>
    public LockFile(string path)
    {
      _filePath = path;
    }


    /// <summary>
    /// Waits for the lock-file to be free (= non-existant) and thus creates it with the current process as its owner.
    /// </summary>
    public async Task Wait()
    {
      Stopwatch sw = null;
      while (File.Exists(_filePath))
      {
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        if (sw == null || sw.Elapsed > ProcessCheckFrequency)
        {
          sw?.Stop();
          if (!await DeleteFileIfOrphaned())
            sw = Stopwatch.StartNew();
        }
      }

      CreateFile();
    }

    /// <summary>
    /// Releases the lock-file.
    /// </summary>
    public async Task Release()
    {
      DeleteFile();
      await Task.CompletedTask;
    }


    private void DeleteFile()
    {
      if (File.Exists(_filePath)) File.Delete(_filePath);
    }

    /// <summary>
    /// Returns the process that currently owns the lock-file.
    /// </summary>
    /// <returns>The process that currently owns the lock-file.</returns>
    /// <exception cref="ProcessNotFoundException"></exception>
    public async Task<Process> GetLockedByProcess()
    {
      if (!File.Exists(_filePath)) return null;

      Process process = null;
      var pidText = String.Empty;
      try
      {
        int pid;
        using (var fs = File.OpenText(_filePath))
        {
          pidText = await fs.ReadLineAsync();
          pid = int.Parse(pidText);
        }

        process = Process.GetProcesses().FirstOrDefault(p => p.Id == pid);
      }
      catch
      {
        // ignore
      }

      if (process == null)
      {
        throw new ProcessNotFoundException(pidText);
      }

      return process;
    }

    private async Task<bool> DeleteFileIfOrphaned()
    {
      try
      {
        await GetLockedByProcess();
        return false;
      }
      catch (ProcessNotFoundException)
      {
        DeleteFile();
        return true;
      }
    }

    private void CreateFile()
    {
      var directoryPath = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directoryPath))
      {
        Directory.CreateDirectory(directoryPath);
      }

      using (var fs = File.CreateText(_filePath))
      {
        fs.WriteLine(Process.GetCurrentProcess().Id);
        fs.Close();
      }
    }

    /// <inheritdoc />
    public void Dispose()
    {
      DeleteFile();
    }
  }
}