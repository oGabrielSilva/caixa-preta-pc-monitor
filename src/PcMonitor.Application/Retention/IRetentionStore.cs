namespace PcMonitor.Application.Retention;

/// <summary>Storage operations that keep the database small.</summary>
public interface IRetentionStore
{
    /// <summary>Computes 1-minute aggregates for every complete minute before <paramref name="cutoff"/> not yet aggregated.</summary>
    /// <returns>Number of minutes aggregated.</returns>
    int AggregateMinutesBefore(DateTimeOffset cutoff);

    /// <summary>
    /// Deletes raw frames older than <paramref name="cutoff"/>, except those within <paramref name="incidentWindow"/>
    /// before the end of an incident's telemetry.
    /// </summary>
    /// <returns>Number of frames deleted.</returns>
    int DeleteFramesBefore(DateTimeOffset cutoff, TimeSpan incidentWindow);
}
