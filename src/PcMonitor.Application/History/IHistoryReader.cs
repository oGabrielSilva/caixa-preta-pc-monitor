namespace PcMonitor.Application.History;

/// <summary>Reads recorded data for the history view.</summary>
public interface IHistoryReader
{
    /// <summary>Every sensor that appears in a recorded segment, one per stable key.</summary>
    IReadOnlyList<HistorySensor> Sensors();

    /// <summary>One sensor by its stable key, or <c>null</c> when it was never recorded.</summary>
    HistorySensor? Sensor(string stableKey);

    /// <summary>
    /// One aggregate per recorded minute of the sensor in [<paramref name="since"/>, <paramref name="until"/>), oldest first,
    /// including recent minutes that retention has not aggregated yet.
    /// </summary>
    IReadOnlyList<MinuteAggregate> Minutes(string stableKey, DateTimeOffset since, DateTimeOffset until);
}
