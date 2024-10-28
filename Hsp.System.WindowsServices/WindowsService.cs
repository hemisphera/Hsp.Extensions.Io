using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Hsp.Extensions.Io;
using Microsoft.Win32;

namespace Hsp.System.WindowsServices
{
  public class WindowsService
  {
    private static async Task RunSc(params string[] args)
    {
      var cr = new CommandLineRunner();
      var proc = cr.Execute("sc.exe", string.Join(" ", args));
      await proc.Wait(0);
    }

    public static WindowsService Open(string name)
    {
      return new WindowsService(name);
    }

    public static async Task<WindowsService> Create(string name, ServiceImage image)
    {
      await RunSc("create", name, "binPath= tmp.exe");
      var instance = Open(name);
      instance.Image = image;
      return instance;
    }


    public static Task Delete(WindowsService service)
    {
      return Delete(service.Name);
    }

    public static async Task Delete(string name)
    {
      await RunSc("delete", name);
    }

    private readonly ServiceController _controller;


    public string Name { get; }

    public ServiceControllerStatus Status
    {
      get
      {
        _controller.Refresh();
        return _controller.Status;
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


    private WindowsService(string name)
    {
      Name = name;
      var exists = ServiceController.GetServices().Any(a => a.ServiceName.Equals(name, StringComparison.OrdinalIgnoreCase));
      if (!exists) throw new Exception($"Service '{name}' does not exist.");
      _controller = new ServiceController(name);
    }


    private object GetRegistryKeyValue(string name)
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
      await WaitForStatus(ServiceControllerStatus.Stopped, waitTimeout, ct);
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
      await WaitForStatus(ServiceControllerStatus.Running, waitTimeout, ct);
    }

    private async Task WaitForStatus(ServiceControllerStatus reqStatus, TimeSpan? waitTimeout, CancellationToken ct = default)
    {
      if (waitTimeout == null) return;

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
          throw new global::System.TimeoutException(
            $"The given timeout '{waitTimeout.Value.TotalMilliseconds}'ms has elapsed while waiting for service '{Name}' to reach status '{reqStatus}'.");
      } while (serviceStatus != reqStatus);
    }
  }
}