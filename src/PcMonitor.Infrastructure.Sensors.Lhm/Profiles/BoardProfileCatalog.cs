using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PcMonitor.Domain.Sensors;

namespace PcMonitor.Infrastructure.Sensors.Lhm.Profiles;

/// <summary>Board profiles embedded from <c>Profiles/*.json</c>.</summary>
public static class BoardProfileCatalog
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly Lazy<IReadOnlyList<BoardProfile>> _all = new(Load);

    public static IReadOnlyList<BoardProfile> All => _all.Value;

    /// <summary>The profile for a motherboard product name (SMBIOS), ignoring case and surrounding spaces.</summary>
    public static BoardProfile? FindFor(string? boardProduct) =>
        string.IsNullOrWhiteSpace(boardProduct)
            ? null
            : All.FirstOrDefault(p => string.Equals(p.BoardProduct, boardProduct.Trim(), StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<BoardProfile> Load()
    {
        var assembly = typeof(BoardProfileCatalog).Assembly;
        return [.. assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".json", StringComparison.Ordinal))
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                var file = JsonSerializer.Deserialize<ProfileFile>(stream, _json)
                    ?? throw new InvalidOperationException($"Board profile '{name}' is empty.");
                return file.ToProfile();
            })];
    }

    private sealed record ProfileFile(string BoardProduct, string[]? Exclude, ProfileSensorFile[]? Sensors)
    {
        public BoardProfile ToProfile() => new(
            BoardProduct,
            [.. (Exclude ?? []).Select(pattern => new Regex(pattern, RegexOptions.CultureInvariant))],
            [.. (Sensors ?? []).Select(s => new ProfileSensor(s.Key, s.Name, s.Role, s.Multiplier ?? 1))]);
    }

    private sealed record ProfileSensorFile(string Key, string Name, SensorRole Role, float? Multiplier);
}
