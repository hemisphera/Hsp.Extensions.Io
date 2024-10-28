using System;
using System.ServiceProcess;

namespace Hsp.System.WindowsServices
{
  /// <summary>
  /// A service status exception.
  /// </summary>
  [Serializable]
  public class ServiceStatusException : Exception
  {
    private static string GetMessage(string serviceName, ServiceControllerStatus status)
    {
      return $"Service '{serviceName}' must be be in status '{status}'.";
    }

    /// <summary>
    /// The name of the service.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// The requested status.
    /// </summary>
    public ServiceControllerStatus RequestedStatus { get; }


    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="status">The requested status.</param>
    public ServiceStatusException(string serviceName, ServiceControllerStatus status)
      : base(GetMessage(serviceName, status))
    {
      ServiceName = serviceName;
      RequestedStatus = status;
    }
  }
}