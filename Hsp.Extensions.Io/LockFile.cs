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
    /// Specifies whether the lock-file is currently locked.
    /// </summary>
    public bool IsLocked => File.Exists(_filePath);

    public TimeSpan PollingFrequency { get; set; }


    /// <summary>
    /// Creates a new lock file.
    /// </summary>
    /// <param name="path">The path to the lock file.</param>
    public LockFile(string path)
    {
      _filePath = path;
      var rng = new Random();
      PollingFrequency = TimeSpan.FromMilliseconds(600 + rng.Next(0, 401) - 200);
    }


    /// <summary>
    /// Waits for the lock-file to be free (= non-existant) and optionlaly creates it with the current process as its owner.
    /// </summary>
    /// <param name="withLock">Whether to lock the file once it has been released.</param>
    public async Task Wait(bool withLock = true)
    {
      Stopwatch sw = null;
      while (IsLocked)
      {
        await Task.Delay(PollingFrequency);
        if (sw == null || sw.Elapsed > ProcessCheckFrequency)
        {
          sw?.Stop();
          if (!await DeleteFileIfOrphaned())
            sw = Stopwatch.StartNew();
        }
      }

      if (withLock)
        Lock();
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
      if (!IsLocked) return null;

      Process process = null;
      var pidText = string.Empty;
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

    /// <summary>
    /// Locks the lock-file with the current process as its owner.
    /// </summary>
    public void Lock()
    {
      if (IsLocked)
      {
        throw new InvalidOperationException("The lock-file is already locked.");
      }

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