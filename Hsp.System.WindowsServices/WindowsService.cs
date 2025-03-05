using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Hsp.Extensions.Io;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using TimeoutException = System.ServiceProcess.TimeoutException;

namespace Hsp.System.WindowsServices
{
  public class WindowsService : IDisposable
  {
    private static async Task RunSc(ILogger? logger, params string[] args)
    {
      SecurityHelpers.AssertAdministrator();
      var cr = new CommandLineRunner();
      var proc = cr.Execute("sc.exe", string.Join(" ", args));
      if (logger != null)
      {
        proc.OutputLines.ForEach(a => logger.LogInformation(a));
        proc.ErrorLines.ForEach(a => logger.LogError(a));
      }

      await proc.Wait(0);
    }

    public static WindowsService Open(string name, bool ignoreCase = true)
    {
      var comp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
      var controller = ServiceController.GetServices().FirstOrDefault(a => a.ServiceName.Equals(name, comp));
      if (controller == null) throw new KeyNotFoundException($"Service '{name}' does not exist.");
      return new WindowsService(controller);
    }

    public static async Task<WindowsService> Create(string name, ServiceImage image, ILogger? logger = null)
    {
      await RunSc(logger, "create", name, "binPath= tmp.exe");
      var instance = Open(name);
      instance.Image = image;
      return instance;
    }

    public static Task Delete(WindowsService service, ILogger? logger = null)
    {
      return Delete(service.Name, logger);
    }

    public static async Task Delete(string name, ILogger? logger = null)
    {
      await RunSc(logger, "delete", name);
    }

    public static IEnumerable<WindowsService> Enumerate(Predicate<WindowsService>? filter)
    {
      var services = ServiceController.GetServices();
      return services
        .Select(a => new WindowsService(a))
        .Where(a => filter == null || filter(a));
    }


    private readonly ServiceController _controller;

    private CancellableTask? _worker;


    public string Name => _controller.ServiceName;

    public ServiceControllerStatus Status
    {
      get
      {
        _controller.Refresh();
        return _controller.Status;
      }
    }

    public bool IsInPendingStatus
    {
      get
      {
        _controller.Refresh();
        return _controller.Status == ServiceControllerStatus.StartPending ||
               _controller.Status == ServiceControllerStatus.StopPending ||
               _controller.Status == ServiceControllerStatus.PausePending ||
               _controller.Status == ServiceControllerStatus.ContinuePending;
      }
    }

    /// <summary>
    /// Specifies the full path (including arguments) to the image the service is for.
    /// </summary>
    public ServiceImage? Image
    {
      get
      {
        var imagePath = GetRegistryKeyValue("ImagePath") as string;
        return imagePath.IsNullOrEmpty() ? null : ServiceImage.FromBinPath(imagePath);
      }
      set => SetRegistryKeyValue("ImagePath", value == null ? string.Empty : value.ToString(), RegistryValueKind.String);
    }

    /// <summary>
    /// Specifies whether the service uses delayed auto-start.
    /// </summary>
    public bool? DelayedAutoStart
    {
      get => GetRegistryKeyValue("DelayedAutostart") as bool?;
      set => SetRegistryKeyValue("DelayedAutostart", value, RegistryValueKind.DWord);
    }

    /// <summary>
    /// Specifies the display name of the service.
    /// </summary>
    public string? Displayname
    {
      get => GetRegistryKeyValue("DisplayName") as string;
      set => SetRegistryKeyValue("DisplayName", value, RegistryValueKind.String);
    }

    /// <summary>
    /// Specifies the list of dependencies this service has.
    /// </summary>
    public string[]? Dependencies
    {
      get => GetRegistryKeyValue("DependOnService") as string[];
      set => SetRegistryKeyValue("DependOnService", value, RegistryValueKind.MultiString);
    }


    private WindowsService(ServiceController controller)
    {
      _controller = controller;
    }


    private object? GetRegistryKeyValue(string name)
    {
      using (var key = GetRegistryKey())
      {
        return key.GetValue(name);
      }
    }

    private void SetRegistryKeyValue(string name, object? value, RegistryValueKind kind)
    {
      using (var key = GetRegistryKey(true))
      {
        var valueExists = key.GetValue(name) != null;
        if (valueExists) key.DeleteValue(name);
        if (value != null)
          key.SetValue(name, value, kind);
      }
    }

    private RegistryKey GetRegistryKey(bool writeMode = false)
    {
      var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
      key = key.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + Name, writeMode);
      if (key == null)
        throw new InvalidOperationException("The specified service could not be found on this machine.");
      return key;
    }


    public Task StopService(CancellationToken ct = default)
    {
      return StopService(null, ct);
    }

    public async Task StopService(TimeSpan? waitTimeout, CancellationToken ct = default)
    {
      _controller.Refresh();
      if (_controller.Status == ServiceControllerStatus.Stopped) return;
      if (_controller.Status != ServiceControllerStatus.Running && _controller.Status != ServiceControllerStatus.StopPending)
        throw new ServiceStatusException(Name, ServiceControllerStatus.Running);
      if (_controller.Status == ServiceControllerStatus.Running)
        _controller.Stop();
      if (waitTimeout != null)
        await WaitForStatus(ServiceControllerStatus.Stopped, waitTimeout.Value, ct);
    }

    public Task StartService(CancellationToken ct = default)
    {
      return StartService(null, ct);
    }

    public async Task StartService(TimeSpan? waitTimeout, CancellationToken ct = default)
    {
      _controller.Refresh();
      if (_controller.Status == ServiceControllerStatus.Running) return;
      if (_controller.Status != ServiceControllerStatus.Stopped && _controller.Status != ServiceControllerStatus.StartPending)
        throw new ServiceStatusException(Name, ServiceControllerStatus.Stopped);
      if (_controller.Status == ServiceControllerStatus.Stopped)
        _controller.Start();
      if (waitTimeout != null)
        await WaitForStatus(ServiceControllerStatus.Running, waitTimeout.Value, ct);
    }

    public async Task WaitForNonPendingStatus(TimeSpan waitTimeout, CancellationToken ct = default)
    {
      if (!IsInPendingStatus) return;

      var delay = TimeSpan.FromMilliseconds(500);
      var st = DateTime.Now;
      do
      {
        ct.ThrowIfCancellationRequested();
        if (IsInPendingStatus)
          await Task.Delay(delay, ct);
        if (DateTime.Now.Subtract(st) > waitTimeout)
          throw new TimeoutException(
            $"The given timeout '{waitTimeout.TotalMilliseconds}'ms has elapsed while waiting for service '{Name}' to exit pending status.");
      } while (IsInPendingStatus);
    }

    public async Task WaitForStatus(ServiceControllerStatus reqStatus, TimeSpan waitTimeout, CancellationToken ct = default)
    {
      ServiceControllerStatus serviceStatus;
      var delay = TimeSpan.FromMilliseconds(500);
      var st = DateTime.Now;
      do
      {
        ct.ThrowIfCancellationRequested();
        _controller.Refresh();
        serviceStatus = _controller.Status;
        if (_controller.Status != reqStatus)
          await Task.Delay(delay, ct);
        if (DateTime.Now.Subtract(st) > waitTimeout)
          throw new TimeoutException(
            $"The given timeout '{waitTimeout.TotalMilliseconds}'ms has elapsed while waiting for service '{Name}' to reach status '{reqStatus}'.");
      } while (serviceStatus != reqStatus);
    }

    public void StartEventLogStream(ILogger logger, TimeSpan? frequency = null)
    {
      var delay = frequency ?? TimeSpan.FromSeconds(1);
      _worker?.Dispose();
      _worker = new CancellableTask(async ct =>
      {
        var lastEntry = DateTime.Now;
        while (!ct.IsCancellationRequested)
        {
          var eventLog = new EventLog("Application");
          await Task.Delay(delay, ct);
          var newEntries = eventLog.Entries.OfType<EventLogEntry>()
            .Where(e => e.Source == Name)
            .Where(e => e.TimeGenerated >= lastEntry)
            .ToArray();
          lastEntry = DateTime.Now;
          if (newEntries.Length == 0) continue;

          foreach (var entry in newEntries.Where(t => t.EntryType == EventLogEntryType.Information))
            logger.LogInformation(entry.Message);
          foreach (var entry in newEntries.Where(t => t.EntryType == EventLogEntryType.Warning))
            logger.LogWarning(entry.Message);
          foreach (var entry in newEntries.Where(t => t.EntryType == EventLogEntryType.Error))
            logger.LogError(entry.Message);
        }
      });
    }

    public void StopEventLogStream()
    {
      _worker?.Dispose();
    }

    public void Dispose()
    {
      _controller.Dispose();
      _worker?.Dispose();
    }
  }
}