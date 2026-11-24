using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace PcMonitor.Tests.Support;

/// <summary>
/// Denies the current user every kind of write on a folder and the files in it, then restores the ACL on dispose.
/// Simulates the dashboard's read-only account.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WriteDenied : IDisposable
{
    private const FileSystemRights Writes =
        FileSystemRights.WriteData | FileSystemRights.AppendData | FileSystemRights.WriteAttributes |
        FileSystemRights.WriteExtendedAttributes | FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles;

    private readonly DirectoryInfo _folder;
    private readonly FileSystemAccessRule _rule;

    public WriteDenied(string folder)
    {
        _folder = new DirectoryInfo(folder);
        _rule = new FileSystemAccessRule(
            WindowsIdentity.GetCurrent().User!,
            Writes,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Deny);

        var acl = _folder.GetAccessControl();
        acl.AddAccessRule(_rule);
        _folder.SetAccessControl(acl);
    }

    public void Dispose()
    {
        var acl = _folder.GetAccessControl();
        acl.RemoveAccessRule(_rule);
        _folder.SetAccessControl(acl);
    }
}
