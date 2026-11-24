using System.Text.Json;
using PcMonitor.Domain.Diagnosis;

namespace PcMonitor.Dashboard.Tests.Api;

/// <summary>Checks the committed OpenAPI document, from which the frontend's TypeScript types are generated.</summary>
public sealed class OpenApiContractTests
{
    private static JsonElement Schemas()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "PcMonitor.Dashboard", "openapi", "PcMonitor.Dashboard.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("components").GetProperty("schemas").Clone();
    }

    [Fact]
    public void Finding_code_is_an_enum_of_the_catalogue()
    {
        var code = Schemas().GetProperty("FindingCode");

        code.GetProperty("type").GetString().ShouldBe("string");
        code.GetProperty("enum").EnumerateArray().Select(e => e.GetString()).ShouldBe(FindingCodes.All);
    }

    [Fact]
    public void Findings_and_observations_use_the_finding_code_schema()
    {
        var schemas = Schemas();

        schemas.GetProperty("FindingResponse").GetProperty("properties").GetProperty("code").GetProperty("$ref").GetString()
            .ShouldBe("#/components/schemas/FindingCode");
        schemas.GetProperty("IncidentSummaryResponse").GetProperty("properties").GetProperty("observations").GetProperty("items")
            .GetProperty("enum").EnumerateArray().Select(e => e.GetString()).ShouldBe(FindingCodes.All);
    }
}
