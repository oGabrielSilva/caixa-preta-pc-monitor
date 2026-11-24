using System.ComponentModel.DataAnnotations;
using PcMonitor.Infrastructure.Persistence.Sqlite;

namespace PcMonitor.Collector.Configuration;

/// <summary>Collector settings bound from the <c>Collector</c> configuration section.</summary>
internal sealed class CollectorOptions : IValidatableObject
{
    public const string SectionName = "Collector";

    private const int StallIntervals = 5;
    private const int GapIntervals = 3;

    /// <summary>Database file. Environment variables such as <c>%ProgramData%</c> are expanded.</summary>
    [Required]
    public string DatabasePath { get; set; } = SqliteDatabase.DefaultPath;

    /// <summary>Time between two frames.</summary>
    [Range(typeof(TimeSpan), "00:00:00.200", "00:01:00")]
    public TimeSpan SampleInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>A sampling cycle slower than this terminates the process so the service restarts.</summary>
    public TimeSpan StallTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Budget for opening the hardware and the database before the first cycle (slow at boot).</summary>
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>How long raw frames are kept.</summary>
    [Range(typeof(TimeSpan), "1.00:00:00", "3650.00:00:00")]
    public TimeSpan RawRetention { get; set; } = TimeSpan.FromDays(14);

    /// <summary>How often aggregation and deletion run.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan RetentionInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Silence longer than this, with no power event, starts a new segment.</summary>
    public TimeSpan GapThreshold => SampleInterval * GapIntervals;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StallTimeout < SampleInterval * StallIntervals)
        {
            yield return new ValidationResult(
                $"{nameof(StallTimeout)} must be at least {StallIntervals} × {nameof(SampleInterval)}.", [nameof(StallTimeout)]);
        }

        if (StartupTimeout < StallTimeout)
        {
            yield return new ValidationResult(
                $"{nameof(StartupTimeout)} must be at least {nameof(StallTimeout)}.", [nameof(StartupTimeout)]);
        }
    }
}
