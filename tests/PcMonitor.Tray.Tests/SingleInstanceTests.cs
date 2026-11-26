using PcMonitor.Tray.Startup;

namespace PcMonitor.Tray.Tests;

public sealed class SingleInstanceTests
{
    [Fact]
    public async Task A_second_instance_asks_the_first_to_show_its_window()
    {
        var name = $"CaixaPreta.Tests.{Guid.NewGuid():N}";
        var shown = new TaskCompletionSource();
        using var first = SingleInstance.TryAcquire(name).ShouldNotBeNull();
        first.Listen(() => shown.TrySetResult());

        SingleInstance.TryAcquire(name).ShouldBeNull();
        SingleInstance.SignalExisting(name);

        await shown.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void The_name_is_free_again_after_the_first_instance_exits()
    {
        var name = $"CaixaPreta.Tests.{Guid.NewGuid():N}";
        SingleInstance.TryAcquire(name)!.Dispose();

        using var again = SingleInstance.TryAcquire(name);

        again.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_signal_sent_while_the_first_instance_is_still_starting_is_delivered_once_it_listens()
    {
        var name = $"CaixaPreta.Tests.{Guid.NewGuid():N}";
        var shown = new TaskCompletionSource();
        using var first = SingleInstance.TryAcquire(name).ShouldNotBeNull();

        var signal = Task.Run(() => SingleInstance.SignalExisting(name), TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);
        first.Listen(() => shown.TrySetResult());

        await shown.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await signal;
    }

    [Fact]
    public void Each_Windows_session_has_its_own_pipe()
    {
        SingleInstance.PipeName("CaixaPreta.Tray", sessionId: 1).ShouldNotBe(SingleInstance.PipeName("CaixaPreta.Tray", sessionId: 2));
    }
}
