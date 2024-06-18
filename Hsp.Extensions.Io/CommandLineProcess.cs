using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// An instance of a process spawned by CommandLineRunner.
  /// </summary>
  public sealed class CommandLineProcess : IDisposable
  {
    private readonly TimeSpan _timeout;

    private readonly CancellationTokenSource _cts;

    private readonly Process _process;

    private readonly List<string> _outputLines = new List<string>();

    private readonly List<string> _errorLines = new List<string>();

    private readonly Action<string> _outputCallback;

    private readonly Action<string> _errorCallback;

    /// <summary>
    /// Returns the process ID of the spawned process.
    /// </summary>
    public int ProcessId => _process.Id;


    /// <summary>
    /// Contains the lines that the process has written to stdout.
    /// </summary>
    public IReadOnlyList<string> OutputLines
    {
      get
      {
        lock (_outputLines) return _outputLines.AsReadOnly();
      }
    }

    /// <summary>
    /// Contains the lines that the process has written to stderr.
    /// </summary>
    public IReadOnlyList<string> ErrorLines
    {
      get
      {
        lock (_errorLines) return _errorLines.AsReadOnly();
      }
    }

    /// <summary>
    /// Indicates the exit code of the process, if it has already exited.
    /// If the process is still running, this property returns null.
    /// </summary>
    public int? ExitCode => _process.HasExited ? _process.ExitCode : (int?)null;


    private CommandLineProcess(
      Process proc, TimeSpan timeout,
      Action<string> outputCallback,
      Action<string> errorCallback)
    {
      _process = proc;
      _timeout = timeout;
      _cts = new CancellationTokenSource();

      _outputCallback = outputCallback;
      _errorCallback = errorCallback;

      _process.OutputDataReceived += Proc_OutputDataReceived;
      _process.ErrorDataReceived += Proc_ErrorDataReceived;
    }


    internal static CommandLineProcess Create(
      string filename, string args, TimeSpan timeout, string workingDir,
      Action<string> outputCallback,
      Action<string> errorCallback)
    {
      var proc = new Process
      {
        StartInfo =
        {
          StandardOutputEncoding = Encoding.Default,
          StandardErrorEncoding = Encoding.Default,
          FileName = filename,
          Arguments = args,
          WindowStyle = ProcessWindowStyle.Hidden,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          RedirectStandardInput = true,
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };
      if (!String.IsNullOrEmpty(workingDir))
        proc.StartInfo.WorkingDirectory = workingDir;

      var instance = new CommandLineProcess(proc, timeout, outputCallback, errorCallback);

      proc.Start();
      proc.BeginOutputReadLine();
      proc.BeginErrorReadLine();
      var unused = instance.StartWatchdog();

      return instance;
    }

    /// <summary>
    /// Asynchronously waits for the process to exit.
    /// </summary>
    /// <param name="token">A cancellation token.</param>
    /// <returns>The current instance.</returns>
    public Task<CommandLineProcess> Wait(CancellationToken token = default)
    {
      return Wait(ec => true, token);
    }

    /// <summary>
    /// Asynchronously waits for the process to exit and throws an exception if the exit code is not the expected one.
    /// </summary>
    /// <param name="exitCodeIsValid">A predicate that determines whether the exit code is valid.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>The current instance.</returns>
    public async Task<CommandLineProcess> Wait(Predicate<int> exitCodeIsValid, CancellationToken token = default)
    {
      while (!_process.HasExited)
        await Task.Delay(500, token);
      var ec = ExitCode ?? -1; // exit code will never be null when _process has exited
      if (!exitCodeIsValid(ec))
        throw new InvalidOperationException($"The process has exited with an invalid exit code ({ec})");
      return this;
    }

    /// <summary>
    /// Asynchronously waits for the process to exit and throws an exception if the exit code is not the expected one.
    /// </summary>
    /// <param name="expectedExitCode">The expected exit code.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>The current instance.</returns>
    public Task<CommandLineProcess> Wait(int expectedExitCode, CancellationToken token = default)
    {
      return Wait(ec => ec == expectedExitCode, token);
    }

    private void Proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
      if (String.IsNullOrEmpty(e?.Data)) return;

      lock (_errorLines)
        _errorLines.Add(e.Data);
      _errorCallback?.Invoke(e.Data);
    }

    private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
      if (String.IsNullOrEmpty(e?.Data)) return;

      lock (_outputLines)
        _outputLines.Add(e.Data);
      _outputCallback?.Invoke(e.Data);
    }

    /// <summary>
    /// Aborts the process with the given message.
    /// </summary>
    /// <param name="message">A message that will be written to the error log.</param>
    public void Abort(string message)
    {
      _cts.Cancel();
      _process.Kill();

      lock (_errorLines)
        _errorLines.Add(message);
    }

    private async Task StartWatchdog()
    {
      var token = _cts.Token;
      var start = DateTime.Now;
      while (!_process.HasExited)
      {
        await Task.Delay(500, token);
        token.ThrowIfCancellationRequested();
        if (DateTime.Now.Subtract(start) > _timeout)
          Abort("The operation has timed out.");
      }
    }


    /// <summary>
    /// Generates and throws an exception, if the process has written anything to stderr
    /// </summary>
    public void ThrowError()
    {
      if (ErrorLines.Count > 0)
        throw new Exception(String.Join(Environment.NewLine, ErrorLines));
    }

    /// <summary>
    /// Returns the underlying Process object.
    /// </summary>
    /// <returns>The underlying Process object.</returns>
    public Process GetProcess()
    {
      return _process;
    }

    /// <inheritdoc />
    public void Dispose()
    {
      _cts?.Dispose();
      _process?.Dispose();
    }
  }
}