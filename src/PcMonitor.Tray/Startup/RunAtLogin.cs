using Microsoft.Win32;

namespace PcMonitor.Tray.Startup;

/// <summary>The "start with Windows" entry of the current user (a value under a <c>Run</c> key).</summary>
internal sealed class RunAtLogin(string runKeyPath, string executable)
{
    public const string ValueName = "CaixaPreta";

    public const string CurrentUserRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKeyPath);
            return key?.GetValue(ValueName) is not null;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(runKeyPath);
            if (value)
            {
                key.SetValue(ValueName, $"\"{executable}\" --background");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
    }
}
