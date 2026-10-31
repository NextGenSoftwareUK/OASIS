using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public sealed record CqlHostedQueryExample(string Id, string Label, JsonElement Request);

/// <summary>Public demo requests, never authorization or credentials. Captured server
/// configuration owns examples; each must still pass native capability planning.</summary>
public sealed class CqlHostedQueryExamples
{
    public const string ConfigurationKey = "CQL_HOSTED_QUERY_EXAMPLES_JSON";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<ProviderType, IReadOnlyList<CqlHostedQueryExample>> examples = new();

    public static CqlHostedQueryExamples FromConfiguration(IConfiguration configuration)
        => FromJson(configuration[ConfigurationKey]);

    public static CqlHostedQueryExamples FromJson(string json)
    {
        var result = new CqlHostedQueryExamples();
        if (json == null) return result;
        if (json.Length is < 2 or > 262144) throw new ArgumentException("Bounded public CQL example configuration required.");
        using var document = JsonDocument.Parse(json);
        Unique(document.RootElement);
        if (document.RootElement.ValueKind != JsonValueKind.Object || document.RootElement.EnumerateObject().Count() > 128)
            throw new ArgumentException("Examples require a bounded provider object.");
        foreach (var entry in document.RootElement.EnumerateObject())
        {
            if (!Enum.TryParse<ProviderType>(entry.Name, false, out var type) || type.ToString() != entry.Name
                || !CqlV1Compiler.SupportedReadProfile(type) || entry.Value.ValueKind != JsonValueKind.Array
                || entry.Value.GetArrayLength() is < 1 or > 8)
                throw new ArgumentException("Examples require an explicit supported source and1–8 requests.");
            var rows = new List<CqlHostedQueryExample>();
            foreach (var example in entry.Value.EnumerateArray())
            {
                if (example.ValueKind != JsonValueKind.Object || example.EnumerateObject().Count() != 3
                    || !example.TryGetProperty("id", out var id) || !example.TryGetProperty("label", out var label)
                    || !example.TryGetProperty("request", out var request))
                    throw new ArgumentException("Example fields must be id, label and request.");
                var key = id.GetString(); var title = label.GetString();
                if (string.IsNullOrWhiteSpace(key) || key.Length > 40 || key.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')
                    || string.IsNullOrWhiteSpace(title) || title.Length > 100 || title.Any(char.IsControl)
                    || rows.Any(row => row.Id == key) || request.GetRawText().Length > 32768)
                    throw new ArgumentException("Unique bounded public example identities required.");
                var typed = request.Deserialize<CqlV1Request>(Json);
                if (typed?.Provider != entry.Name || typed.Graph != null)
                    throw new ArgumentException("Public leaf examples cannot change source ownership or request graphs.");
                _ = CqlV1Compiler.Compile(typed, Guid.Parse("00000000-0000-0000-0000-000000000001"), type);
                rows.Add(new(key, title, JsonSerializer.SerializeToElement(typed, Json)));
            }
            result.examples.Add(type, rows.AsReadOnly());
        }
        return result;
    }

    public IReadOnlyList<CqlHostedQueryExample> For(ProviderType type, CosmicQueryCapabilities capabilities)
    {
        if (!examples.TryGetValue(type, out var rows)) return Array.Empty<CqlHostedQueryExample>();
        foreach (var example in rows)
        {
            var query = CqlV1Compiler.Compile(example.Request.Deserialize<CqlV1Request>(Json),
                Guid.Parse("00000000-0000-0000-0000-000000000001"), type);
            var plan = CosmicQueryPlanner.Plan(query, type, new[] { new CosmicQueryTarget(type, capabilities) });
            if (plan.IsError) throw new ArgumentException("CQL_DEMO_EXAMPLE_UNSUPPORTED");
        }
        return rows;
    }

    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new ArgumentException("Duplicate CQL example property.");
                Unique(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) Unique(item);
    }
}
