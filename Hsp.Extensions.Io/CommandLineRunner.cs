using System;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// A class that spawns instances of CommandLineProcess.
  /// </summary>
  public class CommandLineRunner
  {
    /// <summary>
    /// Specifies the working directory for process that are spawned by this instance.
    /// </summary>
    public string? WorkingDir { get; set; }

    /// <summary>
    /// Specifies the default executable filename for the processes spawned by this instance.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// Specifies an output callback that will be used for all processes spawned by this instance.
    /// </summary>
    public Action<string>? OutputCallback { get; set; }

    /// <summary>
    /// Specifies an error callback that will be used for all processes spawned by this instance.
    /// </summary>
    public Action<string>? ErrorCallback { get; set; }

    /// <summary>
    /// Specifies the maximum time a process has to complete execution. After this time has elapsed, the process is aborted
    /// </summary>
    public TimeSpan TimeoutPeriod { get; set; } = TimeSpan.FromMinutes(2);


    /// <summary>
    /// Executes the the default executable with no arguments.
    /// </summary>
    /// <returns>The spawned process.</returns>
    public CommandLineProcess Execute()
    {
      return Execute(null, string.Empty);
    }

    /// <summary>
    /// Executes the the given executable with the given arguments.
    /// </summary>
    /// <param name="filename">The filename to execute. If this is null or empty, the default filename will be used.</param>
    /// <param name="args">The parameters to pass to the process</param>
    /// <returns>The spawned process.</returns>
    public CommandLineProcess Execute(string? filename, ArgBuilder args)
    {
      return Execute(filename, args.ToArgString());
    }

    /// <summary>
    /// Executes the the default executable with the given arguments.
    /// </summary>
    /// <param name="args">The parameters to pass to the process</param>
    /// <returns>The spawned process.</returns>
    public CommandLineProcess Execute(ArgBuilder args)
    {
      return Execute(null, args);
    }

    /// <summary>
    /// Executes the the given executable with the given arguments.
    /// </summary>
    /// <param name="filename">The filename to execute. If this is null or empty, the default filename will be used.</param>
    /// <param name="args">The parameters to pass to the process</param>
    /// <returns>The spawned process.</returns>
    public CommandLineProcess Execute(string? filename, string args)
    {
      var actualFilename = string.IsNullOrEmpty(filename) ? Filename : filename;
      if (actualFilename == null || string.IsNullOrEmpty(actualFilename)) throw new ArgumentNullException(nameof(filename));
      return CommandLineProcess.Create(actualFilename, args, TimeoutPeriod, WorkingDir, OutputCallback, ErrorCallback);
    }

    /// <summary>
    /// Executes the the default executable with the given arguments.
    /// </summary>
    /// <param name="args">The parameters to pass to the process</param>
    /// <returns>The spawned process.</returns>
    public CommandLineProcess Execute(string args)
    {
      return Execute(null, args);
    }
  }
}