using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

internal static class CosmosCqlProjection
{
    internal static QueryDefinition Compile<T, TResult>(CosmicProjectionPlan<T, TResult> plan, int maximumResults) where T : IHolon
    {
        var admission = CosmicProjectionPlanner.Admit(plan, ProviderType.AzureCosmosDBOASIS,
            AzureCosmosDBOASIS.CqlCapabilities, AzureCosmosDBOASIS.CqlProjectionCapabilities);
        if (admission.IsError) throw new NotSupportedException(admission.Message);
        return CosmosCqlRowCommand.Compile(plan.SourcePlan, maximumResults, translator =>
            "VALUE {\"source\": {\"Id\": c[\"Id\"], \"id\": c[\"id\"], \"IsPublic\": c[\"IsPublic\"], " +
            "\"CreatedByAvatarId\": c[\"CreatedByAvatarId\"]}, \"cells\": [" +
            string.Join(", ", plan.Query.Columns.Select(column => translator.ProjectionCell(column.Path))) + "]}");
    }

    internal static IReadOnlyList<CosmicProjectionRow> Decode<T, TResult>(CosmicProjectionPlan<T, TResult> plan,
        IReadOnlyList<string> documents, int maximumResults, int maximumPayloadBytes, CancellationToken token) where T : IHolon
    {
        if (documents.Count > maximumResults) throw new InvalidDataException("Cosmos projection row budget exceeded.");
        if (plan.Query.Source.Limit.HasValue && documents.Count > plan.Query.Source.Limit.Value)
            throw new InvalidDataException("Cosmos projection exceeded its admitted source window.");
        var identities = new HashSet<Guid>();
        var rows = new List<CosmicProjectionRow>();
        long bytes = 0;
        var utf8 = new UTF8Encoding(false, true);
        foreach (var document in documents)
        {
            token.ThrowIfCancellationRequested();
            using var parsed = JsonDocument.Parse(document);
            var root = parsed.RootElement;
            var proof = root.GetProperty("source");
            var id = proof.GetProperty("Id").GetGuid();
            if (id == Guid.Empty || proof.GetProperty("id").GetString() != id.ToString("D") || !identities.Add(id))
                throw new InvalidDataException("Cosmos projection source identities are invalid or duplicate.");
            if (!Guid.TryParseExact(proof.GetProperty("CreatedByAvatarId").GetString(), "D", out var creator))
                throw new InvalidDataException("Cosmos projection creator proof is invalid.");
            var isPublic = proof.GetProperty("IsPublic").GetBoolean();
            if (plan.Query.Source.ReadAccess != null && !plan.Query.Source.ReadAccess.CanRead(id, isPublic, creator))
                throw new InvalidDataException("Cosmos projection violated native source visibility.");
            var cells = root.GetProperty("cells");
            if (cells.ValueKind != JsonValueKind.Array || cells.GetArrayLength() != plan.Query.Columns.Count)
                throw new InvalidDataException("Cosmos projection column acknowledgement differs from its plan.");
            var values = new object[cells.GetArrayLength()];
            bytes += 16;
            for (var index = 0; index < values.Length; index++)
            {
                token.ThrowIfCancellationRequested();
                var cell = cells[index];
                var state = cell.GetProperty("state").GetInt32();
                var declared = plan.Query.Columns[index].Path.ValueType;
                var type = Nullable.GetUnderlyingType(declared) ?? declared;
                if (state is < 0 or > 2) throw new InvalidDataException("Selected Cosmos scalar has the wrong canonical domain.");
                if (state is 0 or 1)
                {
                    if (state == 0 && cell.TryGetProperty("value", out _))
                        throw new InvalidDataException("Cosmos missing-state acknowledgement contains a value.");
                    if (declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                        throw new InvalidDataException("Selected non-nullable Cosmos scalar is missing or null.");
                    if (state == 1 && cell.GetProperty("value").ValueKind != JsonValueKind.Null)
                        throw new InvalidDataException("Cosmos null-state acknowledgement differs from its value.");
                    values[index] = null;
                }
                else
                {
                    var value = cell.GetProperty("value");
                    if (type == typeof(string) && value.ValueKind == JsonValueKind.String) values[index] = value.GetString();
                    else if (type == typeof(bool) && value.ValueKind is JsonValueKind.True or JsonValueKind.False) values[index] = value.GetBoolean();
                    else if (type == typeof(int) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer)) values[index] = integer;
                    else if (type == typeof(Guid) && value.ValueKind == JsonValueKind.String && Guid.TryParseExact(value.GetString(), "D", out var guid)) values[index] = guid;
                    else throw new InvalidDataException("Selected Cosmos scalar differs from its declared CLR domain.");
                }
                bytes += 16 + (values[index] is string text ? utf8.GetByteCount(text) : 16);
                if (bytes > maximumPayloadBytes) throw new InvalidDataException("Cosmos projection payload budget exceeded.");
            }
            rows.Add(new CosmicProjectionRow(values));
        }
        token.ThrowIfCancellationRequested();
        return rows.AsReadOnly();
    }
}
