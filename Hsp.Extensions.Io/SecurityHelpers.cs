using System;
using System.Security.Principal;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// Helper functions regarding identity and security.
  /// </summary>
  public static class SecurityHelpers
  {
    /// <summary>
    /// Indicates whether the current process is running as a user that is a member of the local administrators group.
    /// </summary>
    public static bool IsAdministrator => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    /// <summary>
    /// Tests if the current process is running as a user that is a member of the local administrators group. If it is not,
    /// an exception is thrown.
    /// </summary>
    public static void AssertAdministrator()
    {
      if (!IsAdministrator)
        throw new InvalidOperationException("You must be a member of the local administrators group.");
    }
  }
}