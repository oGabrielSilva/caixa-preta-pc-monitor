using System.Globalization;

namespace PcMonitor.Domain.Diagnosis;

/// <summary>Builds finding parameters with culture-invariant formatting.</summary>
internal static class FindingParameters
{
    public static IReadOnlyDictionary<string, string> Of(params (string Name, object Value)[] parameters) =>
        parameters.ToDictionary(p => p.Name, p => Format(p.Value));

    public static string Volts(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    public static string Decimal(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static string Format(object value) => value switch
    {
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };
}
