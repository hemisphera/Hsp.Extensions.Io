using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// Represents a cancellable task.
  /// </summary>
  public sealed class CancellableTask : IDisposable
  {
    /// <summary>
    /// The token source used to cancel the task.
    /// </summary>
    public CancellationTokenSource TokenSource { get; }

    private readonly Task _task;


    /// <summary>
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    /// <param name="token">An optional cancellation token source. If nont is given, a new one will be created.</param>
    public CancellableTask(Func<CancellationToken, Task> action, CancellationTokenSource? token = null)
    {
      TokenSource = token ?? new CancellationTokenSource();
      _task = action(TokenSource.Token);
    }

    /// <summary>
    /// Returns the awaiter of the task.
    /// </summary>
    public TaskAwaiter GetAwaiter()
    {
      return _task.GetAwaiter();
    }

    /// <inheritdoc />
    public void Dispose()
    {
      TokenSource.Cancel();
      TokenSource.Dispose();
      _task.Dispose();
    }
  }
}