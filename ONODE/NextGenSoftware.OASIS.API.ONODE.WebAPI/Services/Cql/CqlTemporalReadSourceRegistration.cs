using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.TemporalOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlTemporalReadSourceRegistration
{
    public static void AddTemporalReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var source = configuration["CQL_TEMPORAL_QUERY_SOURCE_JSON"];
        var apiKey = configuration["CQL_TEMPORAL_QUERY_API_KEY"];
        if (source == null && apiKey == null) return;
        if (source == null || Encoding.UTF8.GetByteCount(source) > 4096)
            throw new InvalidOperationException("A bounded Temporal teaching catalogue is required.");
        try
        {
            using var document = JsonDocument.Parse(source, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            RequireFields(root, "owner", "namespace", "runs", "probeRunId");
            var owner = ReadGuid(root, "owner");
            var workflowNamespace = root.GetProperty("namespace").GetString();
            var runs = root.GetProperty("runs").EnumerateArray().Take(9).Select(value =>
            {
                RequireFields(value, "workflowId", "runId");
                return new TemporalTeachingRun(value.GetProperty("workflowId").GetString(), ReadGuid(value, "runId"));
            }).ToArray();
            if (runs.Select(run => run.RunId).Distinct().Count() != runs.Length)
                throw new ArgumentException("Unique exact Temporal run IDs are required.");
            var probeRunId = ReadGuid(root, "probeRunId");
            var probe = runs.SingleOrDefault(run => run.RunId == probeRunId);
            var profile = TemporalTeachingQuerySource.ForCloud(workflowNamespace, apiKey, owner, runs, probe);
            services.AddSingleton(new CqlReadSourceProfile(profile));
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException)
        {
            throw new InvalidOperationException("Use a reviewed Temporal Cloud catalogue and a separate protected reader key.", error);
        }
    }

    private static void RequireFields(JsonElement value, params string[] expected)
    {
        var fields = value.EnumerateObject().Select(property => property.Name).ToArray();
        if (fields.Length != expected.Length || !fields.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw new ArgumentException("Unexpected Temporal teaching catalogue fields.");
    }

    private static Guid ReadGuid(JsonElement value, string name)
    {
        var text = value.GetProperty(name).GetString();
        if (!Guid.TryParseExact(text, "D", out var id) || id == Guid.Empty || id.ToString("D") != text)
            throw new ArgumentException("A canonical Temporal teaching identity is required.");
        return id;
    }
}
