using System.Text.Json;
using System.Text.Json.Serialization;

namespace PcMonitor.Dashboard.Api.Incidents;

/// <summary>
/// A diagnosis code (see <c>docs/architecture/diagnosis.md</c>). Serialized as a plain string; the OpenAPI document
/// lists every value (<see cref="OpenApi.FindingCodeSchemaTransformer"/>) so the frontend gets a union type.
/// </summary>
[JsonConverter(typeof(FindingCodeConverter))]
public readonly record struct FindingCode(string Value)
{
    public override string ToString() => Value;

    private sealed class FindingCodeConverter : JsonConverter<FindingCode>
    {
        public override FindingCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString() ?? string.Empty);

        public override void Write(Utf8JsonWriter writer, FindingCode value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }
}
