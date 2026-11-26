using PcMonitor.Tray.Startup;

namespace PcMonitor.Tray.Tests;

public sealed class SingleInstanceTests
{
    [Fact]
    public async Task A_second_instance_asks_the_first_to_show_its_window()
    {
        var name = $"CaixaPreta.Tests.{Guid.NewGuid():N}";
        var shown = new TaskCompletionSource();
        using var first = SingleInstance.TryAcquire(name, () => shown.TrySetResult()).ShouldNotBeNull();

        SingleInstance.TryAcquire(name, () => { }).ShouldBeNull();
        SingleInstance.SignalExisting(name);

        await shown.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void The_name_is_free_again_after_the_first_instance_exits()
    {
        var name = $"CaixaPreta.Tests.{Guid.NewGuid():N}";
        SingleInstance.TryAcquire(name, () => { })!.Dispose();

        using var again = SingleInstance.TryAcquire(name, () => { });

        again.ShouldNotBeNull();
    }
}
