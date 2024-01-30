using System;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// Represents an error that occurs when a process is not found.
  /// </summary>
  public class ProcessNotFoundException : Exception
  {
    /// <summary>
    /// Gets the PID of the process that was not found.
    /// </summary>
    public string PidString { get; }

    /// <summary>
    /// </summary>
    /// <param name="pidText"></param>
    public ProcessNotFoundException(string pidText)
      : base($"Process holding lock not found. ({pidText})")
    {
      PidString = pidText;
    }
  }
}