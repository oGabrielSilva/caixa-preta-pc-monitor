using Microsoft.Extensions.Time.Testing;
using PcMonitor.Application.History;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Application.Tests.History;

public sealed class QueryHistoryTests
{
    private const string Key = "/lpc/nct6799d/0/voltage/4";
    private static readonly DateTimeOffset _now = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(_now);
    private readonly StubHistoryReader _reader = new();

    private HistoryResult Run(HistoryRange range = HistoryRange.Day) => new QueryHistory(_reader, _time).Run(Key, range)!;

    [Theory]
    [InlineData(HistoryRange.Hour, 60, 1)]
    [InlineData(HistoryRange.Day, 1440, 15)]
    [InlineData(HistoryRange.Week, 10080, 60)]
    [InlineData(HistoryRange.TwoWeeks, 20160, 120)]
    public void Range_sets_span_and_bucket(HistoryRange range, int spanMinutes, int bucketMinutes) =>
        QueryHistory.Dimensions(range).ShouldBe((TimeSpan.FromMinutes(spanMinutes), TimeSpan.FromMinutes(bucketMinutes)));

    [Fact]
    public void Query_covers_the_span_ending_now()
    {
        var result = Run();

        (result.From, result.To, result.Bucket).ShouldBe((_now.AddDays(-1), _now, TimeSpan.FromMinutes(15)));
        _reader.Requested.ShouldBe((_now.AddDays(-1), _now));
    }

    [Fact]
    public void Buckets_take_min_of_min_max_of_max_and_mean_of_averages()
    {
        var start = new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);
        _reader.Minutes.Add(new(start, 11.9, 12.1, 12.2));
        _reader.Minutes.Add(new(start.AddMinutes(14), 12.0, 12.3, 12.4));

        Run().Buckets.ShouldHaveSingleItem().ShouldBe(new HistoryBucket(start, 11.9, 12.2, 12.4));
    }

    [Fact]
    public void Buckets_align_to_utc_multiples()
    {
        _reader.Minutes.Add(new(new DateTimeOffset(2026, 11, 24, 7, 7, 0, TimeSpan.FromHours(-3)), 1, 1, 1));

        Run().Buckets.ShouldHaveSingleItem().Start.ShouldBe(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Minutes_without_data_produce_no_bucket()
    {
        var start = new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);
        _reader.Minutes.Add(new(start, 1, 1, 1));
        _reader.Minutes.Add(new(start.AddHours(2), 2, 2, 2));

        Run().Buckets.Select(b => b.Start).ShouldBe([start, start.AddHours(2)]);
    }

    [Fact]
    public void Unknown_sensor_returns_null() =>
        new QueryHistory(_reader, _time).Run("/unknown", HistoryRange.Hour).ShouldBeNull();

    private sealed class StubHistoryReader : IHistoryReader
    {
        public List<MinuteAggregate> Minutes { get; } = [];

        public (DateTimeOffset From, DateTimeOffset To)? Requested { get; private set; }

        public IReadOnlyList<HistorySensor> Sensors() => [new(Key, "+12V", SensorKind.Voltage, "V", SensorRole.Rail12V)];

        IReadOnlyList<MinuteAggregate> IHistoryReader.Minutes(string stableKey, DateTimeOffset since, DateTimeOffset until)
        {
            Requested = (since, until);
            return Minutes;
        }
    }
}
