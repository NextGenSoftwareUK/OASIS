using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

internal static class CosmosCqlCount
{
    internal static QueryDefinition Compile<T, TResult>(CosmicScalarPlan<T, TResult> plan) where T : IHolon
    {
        var admission = CosmicScalarPlanner.Admit(plan, ProviderType.AzureCosmosDBOASIS,
            AzureCosmosDBOASIS.CqlCapabilities, AzureCosmosDBOASIS.CqlScalarCapabilities);
        if (admission.IsError) throw new NotSupportedException(admission.Message);
        var source = plan.Query.Source;
        var translator = new CosmosCqlPredicateSql();
        var predicate = source.Predicates.Count == 0 ? "true" : string.Join(" AND ", source.Predicates.Select(translator.Translate));
        // Cosmos rejects compositions of aggregates and other expressions.
        // Reduce the source once natively, then map its scalar cardinality to
        // the immutable window using exact integer algebra. Never fetch rows.
        return translator.BuildCommand("SELECT COUNT(1) AS count FROM c WHERE " + predicate);
    }

    internal static long ApplyWindow(long count, int offset, int? limit)
    {
        if (count < 0 || offset < 0 || limit < 0) throw new ArgumentOutOfRangeException(nameof(count));
        var remaining = count <= offset ? 0 : count - offset;
        return limit.HasValue ? Math.Min(remaining, limit.Value) : remaining;
    }

    internal static long Decode(IReadOnlyList<string> rows)
    {
        if (rows == null || rows.Count != 1)
            throw new InvalidDataException("Cosmos native count requires exactly one acknowledgement.");
        using var parsed = JsonDocument.Parse(rows[0]);
        var document = parsed.RootElement;
        if (document.ValueKind != JsonValueKind.Object || document.EnumerateObject().Count() != 1 ||
            !document.TryGetProperty("count", out var value) || value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt64(out var count))
            throw new InvalidDataException("Cosmos native count acknowledgement must contain one exact integer.");
        // Cosmos JSON numeric transport is binary64. Reject counts outside the
        // exact integer domain instead of claiming rounded LongCount precision.
        if (count < 0 || count > 9007199254740991L)
            throw new InvalidDataException("Cosmos native count exceeds its exact nonnegative integer domain.");
        return count;
    }
}
