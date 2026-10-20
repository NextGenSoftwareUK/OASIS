using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed class SqliteConditionalReceiptInvalidException(Exception cause)
        : IOException("Stored SQLite conditional acknowledgement is invalid.", cause);

    private static string ReceiptDigest(JObject document)
        => ConditionalHolonPlan.ComputeFingerprint(new JObject(document.Properties()
            .Where(property => property.Name != "digest").Select(property => new JProperty(property.Name, property.Value.DeepClone()))));

    private static string WriteConditionalReceipt(IEnumerable<IHolon> roots, ConditionalHolonWriteRequest request, string fingerprint)
    {
        var document = new JObject {
            ["format"] = 2, ["fingerprintFormat"] = ConditionalFingerprintFormat(request),
            ["operationId"] = request.Write.OperationId, ["actor"] = request.Write.ActorId,
            ["authority"] = request.Write.Authority.ToString(), ["operation"] = request.Operation.ToString(),
            ["intent"] = fingerprint, ["graph"] = ConditionalHolonReceipt.Write(roots)
        };
        document["digest"] = ReceiptDigest(document);
        return document.ToString(Formatting.None);
    }

    private IEnumerable<IHolon> ReadConditionalReceipt(string json, ConditionalHolonWriteRequest request,
        PreparedGraph expectedGraph, string fingerprint)
    {
        try
        {
            var document = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (document.Count != 9 || document["format"]?.Type != JTokenType.Integer || document.Value<int>("format") != 2
                || document["fingerprintFormat"]?.Type != JTokenType.Integer || document.Value<int>("fingerprintFormat") != ConditionalFingerprintFormat(request)
                || document["digest"]?.Type != JTokenType.String || document.Value<string>("digest") != ReceiptDigest(document)
                || document["operationId"]?.Type != JTokenType.String || document.Value<string>("operationId") != request.Write.OperationId.ToString("D")
                || document["actor"]?.Type != JTokenType.String || document.Value<string>("actor") != request.Write.ActorId.ToString("D")
                || document["authority"]?.Type != JTokenType.String || document.Value<string>("authority") != request.Write.Authority.ToString()
                || document["operation"]?.Type != JTokenType.String || document.Value<string>("operation") != request.Operation.ToString()
                || document["intent"]?.Type != JTokenType.String || document.Value<string>("intent") != fingerprint
                || document["graph"]?.Type != JTokenType.String)
                throw new InvalidDataException("Receipt integrity or original operation binding is invalid.");
            // Reuse the canonical graph codec; it rejects missing/duplicate/unreachable
            // records and unsafe types before this owner validates the request binding.
            var roots = ConditionalHolonReceipt.Read(document.Value<string>("graph"));
            if (!roots.Select(root => root.Id).SequenceEqual(request.Roots.Select(root => root.Id)))
                throw new InvalidDataException("Receipt roots differ from the original request.");
            var graph = HolonGraphWritePlan.Create(roots, true, true, 0, 0);
            var conditions = request.Write.Conditions.ToDictionary(condition => condition.HolonId);
            var expectedNodes = expectedGraph.Nodes.ToDictionary(node => node.Snapshot.Id);
            if (!graph.Nodes.Select(node => node.Source.Id).ToHashSet().SetEquals(conditions.Keys))
                throw new InvalidDataException("Receipt selected records differ from the original request.");
            foreach (var node in graph.Nodes)
            {
                var row = node.Source; var expected = conditions[row.Id].ExpectedRevisionId;
                var revision = CosmicConcurrency.GetProviderRevisionId(row, ProviderType.SQLLiteDBOASIS);
                if (!node.SelectedChildIds.SequenceEqual(expectedGraph.ChildIds[row.Id])
                    || row.ParentHolonId != expectedNodes[row.Id].Snapshot.ParentHolonId
                    || row.Version < 1 || row.VersionId == Guid.Empty || !revision.HasValue || revision.Value == Guid.Empty
                    || revision.Value == row.VersionId || expected.HasValue && revision.Value == expected.Value
                    || !expected.HasValue && (row.Version != 1 || row.PreviousVersionId != Guid.Empty)
                    || request.Operation != ConditionalHolonOperation.Save && !row.IsDeleted)
                    throw new InvalidDataException("Receipt topology or committed revisions are invalid.");
            }
            // Delete consumes authoritative persisted values rather than caller edits;
            // its binding is the selected revisions, operation, actor and tombstone.
            if (request.Operation == ConditionalHolonOperation.Save
                && ConditionalFingerprint(request, PrepareGraph(roots, true, true, 0)) != ConditionalFingerprint(request, expectedGraph))
                throw new InvalidDataException("Receipt persistent values differ from the original submitted intent.");
            return roots;
        }
        catch (Exception cause) when (cause is JsonException or InvalidDataException or ArgumentException or InvalidCastException
            or FormatException or OverflowException or InvalidOperationException)
        {
            throw new SqliteConditionalReceiptInvalidException(cause);
        }
    }
}
