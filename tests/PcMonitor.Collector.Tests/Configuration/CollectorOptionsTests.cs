using System.ComponentModel.DataAnnotations;
using PcMonitor.Collector.Configuration;

namespace PcMonitor.Collector.Tests.Configuration;

public sealed class CollectorOptionsTests
{
    private static bool IsValid(CollectorOptions options) =>
        Validator.TryValidateObject(options, new ValidationContext(options), null, validateAllProperties: true);

    [Fact]
    public void Defaults_are_valid_and_match_the_plan()
    {
        var options = new CollectorOptions();

        IsValid(options).ShouldBeTrue();
        options.SampleInterval.ShouldBe(TimeSpan.FromSeconds(1));
        options.StallTimeout.ShouldBe(TimeSpan.FromSeconds(10));
        options.RawRetention.ShouldBe(TimeSpan.FromDays(14));
        options.RetentionInterval.ShouldBe(TimeSpan.FromHours(1));
        options.DatabasePath.ShouldEndWith(@"PcMonitor\pcmonitor.db");
    }

    [Fact]
    public void Options_reject_a_stall_timeout_shorter_than_five_intervals() =>
        IsValid(new CollectorOptions { SampleInterval = TimeSpan.FromSeconds(1), StallTimeout = TimeSpan.FromSeconds(4) })
            .ShouldBeFalse();

    [Theory]
    [InlineData(100)]
    [InlineData(61_000)]
    public void Options_reject_a_sample_interval_outside_200ms_to_60s(int milliseconds) =>
        IsValid(new CollectorOptions { SampleInterval = TimeSpan.FromMilliseconds(milliseconds), StallTimeout = TimeSpan.FromMinutes(10) })
            .ShouldBeFalse();

    [Fact]
    public void Gap_threshold_is_three_intervals() =>
        new CollectorOptions { SampleInterval = TimeSpan.FromSeconds(2) }.GapThreshold.ShouldBe(TimeSpan.FromSeconds(6));
}
