namespace PcMonitor.Application.Monitoring;

/// <summary>Reads hardware sensors.</summary>
public interface ISensorSource
{
    /// <summary>Prepares the hardware and describes the sensors that <see cref="Read"/> will return.</summary>
    SensorSourceInfo Open();

    /// <summary>Current values in catalog order; <see cref="float.NaN"/> for a sensor without a value.</summary>
    IReadOnlyList<float> Read();
}
