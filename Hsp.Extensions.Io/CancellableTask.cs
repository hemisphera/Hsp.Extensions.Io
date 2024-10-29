using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hsp.Extensions.Io
{
  public sealed class CancellableTask : IDisposable
  {
    public CancellationTokenSource TokenSource { get; }

    private readonly Task _task;


    public CancellableTask(Func<CancellationToken, Task> action, CancellationTokenSource? token = null)
    {
      TokenSource = token ?? new CancellationTokenSource();
      _task = action(TokenSource.Token);
    }

    public TaskAwaiter GetAwaiter()
    {
      return _task.GetAwaiter();
    }

    public void Dispose()
    {
      TokenSource.Cancel();
      TokenSource.Dispose();
      _task.Dispose();
    }
  }
}