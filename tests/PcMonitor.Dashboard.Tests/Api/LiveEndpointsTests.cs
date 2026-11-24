using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PcMonitor.Application.Live;
using PcMonitor.Dashboard.Tests.Support;
using PcMonitor.Domain.Monitoring;
using PcMonitor.Infrastructure.Persistence.Sqlite;
using PcMonitor.Infrastructure.Persistence.Sqlite.Live;

namespace PcMonitor.Dashboard.Tests.Api;

public sealed class LiveEndpointsTests
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private static JsonElement SensorOf(JsonElement live, string role) =>
        live.GetProperty("sensors").EnumerateArray().Single(s => s.GetProperty("role").GetString() == role);

    private static async Task<(HttpResponseMessage Response, SseReader Events)> OpenStream(HttpClient client, string query)
    {
        var response = await client.GetAsync(new Uri($"/api/live/stream{query}", UriKind.Relative), HttpCompletionOption.ResponseHeadersRead, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (response, new SseReader(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task Live_returns_the_last_ten_minutes_with_sensor_ranges()
    {
        using var dashboard = new SeededDashboard();
        var (_, segment) = dashboard.Seed.Recording(SeededDashboard.Now.AddSeconds(-899), 900);

        var live = await dashboard.Get("/api/live");

        live.GetProperty("state").GetString().ShouldBe("Recording");
        live.GetProperty("segmentId").GetInt64().ShouldBe(segment);
        live.GetProperty("session").GetProperty("boardProduct").GetString().ShouldBe("TUF GAMING A620M-PLUS WIFI");
        live.GetProperty("frames").GetArrayLength().ShouldBe(601);
        live.GetProperty("frames")[600].GetProperty("t").GetInt64().ShouldBe(SeededDashboard.Now.ToUnixTimeMilliseconds());
        SensorOf(live, "Rail12V").GetProperty("range").GetProperty("min").GetSingle().ShouldBe(11.4f);
        var cpu = SensorOf(live, "CpuTemperature");
        (cpu.GetProperty("limit").GetSingle(), cpu.GetProperty("critical").GetSingle(), cpu.GetProperty("unit").GetString()).ShouldBe((95f, 100f, "°C"));
    }

    [Fact]
    public async Task Missing_readings_are_null()
    {
        using var dashboard = new SeededDashboard();
        var session = dashboard.Seed.StartSession(SeededDashboard.Now);
        var segment = dashboard.Seed.StartSegment(session, SeededDashboard.Now);
        dashboard.Seed.Frames(segment, SeededDashboard.Now, 1, _ => [float.NaN, 6f, 20f, 12.19f, 5f, 3.3f, 2000f]);

        var live = await dashboard.Get("/api/live");

        live.GetProperty("frames")[0].GetProperty("values")[0].ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Live_without_sessions_is_no_data()
    {
        using var dashboard = new SeededDashboard();

        var live = await dashboard.Get("/api/live");

        live.GetProperty("state").GetString().ShouldBe("NoData");
        live.GetProperty("frames").GetArrayLength().ShouldBe(0);
        live.GetProperty("session").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Stream_sends_frames_written_after_the_snapshot()
    {
        using var dashboard = new SeededDashboard(realTime: true);
        var start = DateTimeOffset.UtcNow.AddSeconds(-5);
        var (_, segment) = dashboard.Seed.Recording(start, 5);
        var (response, events) = await OpenStream(dashboard.Client(), $"?segmentId={segment}&after={start.AddSeconds(4).ToUnixTimeMilliseconds()}");
        using (response)
        using (events)
        {
            dashboard.Seed.Frames(segment, start.AddSeconds(5), 1);

            var update = await events.Next("frames", _timeout);

            update.GetProperty("kind").GetString().ShouldBe("Frames");
            update.GetProperty("segmentId").GetInt64().ShouldBe(segment);
            update.GetProperty("frames").EnumerateArray().Select(f => f.GetProperty("t").GetInt64())
                .ShouldBe([start.AddSeconds(5).ToUnixTimeMilliseconds()]);
        }
    }

    [Fact]
    public async Task Stream_announces_a_new_segment()
    {
        using var dashboard = new SeededDashboard(realTime: true);
        var start = DateTimeOffset.UtcNow;
        var (session, segment) = dashboard.Seed.Recording(start, 1);
        var (response, events) = await OpenStream(dashboard.Client(), $"?segmentId={segment}&after={start.ToUnixTimeMilliseconds()}");
        using (response)
        using (events)
        {
            var next = dashboard.Seed.StartSegment(session, start.AddSeconds(1));

            (await events.Next("segment", _timeout)).GetProperty("segmentId").GetInt64().ShouldBe(next);
        }
    }

    [Fact]
    public async Task Closing_the_stream_stops_polling()
    {
        var reads = new CountingLiveReader();
        using var dashboard = new SeededDashboard(realTime: true, services => services.AddSingleton<ILiveReader>(provider =>
            reads.Wrap(new SqliteLiveReader(provider.GetRequiredService<SqliteDatabase>()))));
        dashboard.Seed.Recording(DateTimeOffset.UtcNow, 1);
        var (response, events) = await OpenStream(dashboard.Client(), string.Empty);
        await events.Next("segment", _timeout);

        events.Dispose();
        response.Dispose();
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var afterClose = reads.Count;
        await Task.Delay(TimeSpan.FromSeconds(2.5), TestContext.Current.CancellationToken);

        reads.Count.ShouldBe(afterClose);
    }

    [Fact]
    public async Task Stopping_the_dashboard_ends_open_streams()
    {
        using var dashboard = new SeededDashboard(realTime: true);
        dashboard.Seed.Recording(DateTimeOffset.UtcNow, 1);
        var (response, events) = await OpenStream(dashboard.Client(), string.Empty);
        using (response)
        using (events)
        {
            await events.Next("segment", _timeout);

            dashboard.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

            (await events.EndsWithin(TimeSpan.FromSeconds(5))).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Stream_is_503_without_a_database()
    {
        using var factory = DashboardFactory.WithoutDatabase();

        using var response = await factory.CreateClient().GetAsync(new Uri("/api/live/stream", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    private sealed class CountingLiveReader
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public ILiveReader Wrap(ILiveReader inner) => new Counting(inner, this);

        private sealed class Counting(ILiveReader inner, CountingLiveReader counter) : ILiveReader
        {
            public LiveSnapshot? Latest(TimeSpan window) => inner.Latest(window);

            public IReadOnlyList<Frame> FramesAfter(long segmentId, DateTimeOffset after) => inner.FramesAfter(segmentId, after);

            public LiveHead Head()
            {
                Interlocked.Increment(ref counter._count);
                return inner.Head();
            }
        }
    }
}
