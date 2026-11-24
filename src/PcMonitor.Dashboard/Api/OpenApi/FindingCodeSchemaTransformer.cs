using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using PcMonitor.Dashboard.Api.Incidents;
using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Dashboard.Api.OpenApi;

/// <summary>
/// Publishes <see cref="FindingCode"/> as a string enum of <see cref="FindingCodes.All"/>. Lists of codes lose their
/// item schema with a custom converter, so the enum is put back on them.
/// </summary>
internal sealed class FindingCodeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(FindingCode))
        {
            schema.Type = JsonSchemaType.String;
            schema.Properties?.Clear();
            schema.Required?.Clear();
            schema.Enum = Codes();
        }
        else if (typeof(IEnumerable<FindingCode>).IsAssignableFrom(context.JsonTypeInfo.Type))
        {
            // A reference cannot be created at this stage; the same enum inline gives the same TypeScript union.
            schema.Items = new OpenApiSchema { Type = JsonSchemaType.String, Enum = Codes() };
        }

        return Task.CompletedTask;
    }

    private static List<JsonNode> Codes() => [.. FindingCodes.All.Select(code => (JsonNode)JsonValue.Create(code))];
}
