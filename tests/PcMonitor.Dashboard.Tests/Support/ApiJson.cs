using System.Text.Json;
using System.Text.Json.Serialization;

namespace PcMonitor.Dashboard.Tests.Support;

/// <summary>The JSON settings of the API (web defaults, enums as names), for reading responses in tests.</summary>
public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
}
