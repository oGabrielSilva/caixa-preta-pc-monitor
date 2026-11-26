using Microsoft.Win32;
using PcMonitor.Tray.Startup;

namespace PcMonitor.Tray.Tests;

public sealed class RunAtLoginTests : IDisposable
{
    private readonly string _key = $@"Software\PcMonitorTests\{Guid.NewGuid():N}";

    public void Dispose() => Registry.CurrentUser.DeleteSubKeyTree(_key, throwOnMissingSubKey: false);

    [Fact]
    public void Run_at_login_adds_and_removes_the_entry()
    {
        var runAtLogin = new RunAtLogin(_key, @"C:\Program Files\Caixa-Preta\CaixaPreta.exe");

        runAtLogin.IsEnabled.ShouldBeFalse();
        runAtLogin.IsEnabled = true;
        runAtLogin.IsEnabled.ShouldBeTrue();
        Registry.CurrentUser.OpenSubKey(_key)!.GetValue(RunAtLogin.ValueName).ShouldBe("\"C:\\Program Files\\Caixa-Preta\\CaixaPreta.exe\" --background");
        runAtLogin.IsEnabled = false;
        runAtLogin.IsEnabled.ShouldBeFalse();
    }
}
