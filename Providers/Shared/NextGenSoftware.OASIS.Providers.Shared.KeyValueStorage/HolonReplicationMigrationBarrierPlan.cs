using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

internal sealed class HolonMigrationBarrierPlanException(string code, string message) : InvalidOperationException(message)
{ internal string Code { get; } = code; }

/// <summary>Immutable, redacted pre-mutation recovery evidence. It contains exact
/// hash preimages for the reviewed preview and original receipt summaries, not
/// business values, credentials or native fence owner tokens. Restore requires the
/// independently authorized original context; a checksum is not a permission grant.</summary>
public sealed class HolonReplicationMigrationBarrierPlan
{
    public const int MaximumUtf8Bytes = 16 * 1024 * 1024;
    public HolonReplicationMigrationAuthorizationContext Context { get; }
    public string CanonicalJson { get; }
    public string Fingerprint { get; }
    public IReadOnlyList<HolonMigrationFenceRequest> Scopes { get; }
    private HolonReplicationMigrationBarrierPlan(HolonReplicationMigrationAuthorizationContext context,
        string canonical, IEnumerable<Guid> ids)
    {
        Context = context; CanonicalJson = canonical; Fingerprint = Hash(canonical);
        Scopes = Array.AsReadOnly(new[] { context.SourceStorage, context.OriginalTargetStorage, context.ReplacementRoute.TargetStorage }
            .GroupBy(s => s.BindingFingerprint, StringComparer.Ordinal).Select(g => g.First())
            .OrderBy(s => s.BindingFingerprint, StringComparer.Ordinal).Select(s => new HolonMigrationFenceRequest(
                context.MigrationId, context.OperatorId, s, context.PreviewDigest, context.ContextDigest, ids)).ToArray());
    }
    public static HolonReplicationMigrationBarrierPlan Create(HolonReplicationMigrationPreview preview,
        HolonReplicationMigrationAuthorizationContext context)
    {
        ArgumentNullException.ThrowIfNull(preview); ArgumentNullException.ThrowIfNull(context);
        if (preview.AuthorizationContext(context.MigrationId, context.OperatorId, context.AuthorizationPolicyBinding).ContextDigest != context.ContextDigest)
            throw new HolonMigrationBarrierPlanException("COSMIC_MIGRATION_BARRIER_SCOPE_MISMATCH", "The context must describe this exact reviewed preview.");
        return ReadCanonical(Encode(context.ContextDigest, preview.DigestMaterial,
            preview.Receipts.Select(r => r.DigestMaterial).ToArray()), context);
    }
    private static string Encode(string context, string preview, string[] receipts)
        => JsonSerializer.Serialize(new { format = "cosmic-migration-barrier-plan-v1", contextDigest = context,
            previewMaterial = preview, receiptMaterials = receipts });
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(new UTF8Encoding(false, true).GetBytes(text)));
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
    private static JsonDocument Parse(string json)
    {
        var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
        try { Unique(document.RootElement); return document; }
        catch { document.Dispose(); throw; }
    }
    private static void Unique(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            { Require(names.Add(property.Name), "Duplicate recovery evidence property."); Unique(property.Value); }
        }
        else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) Unique(item);
    }
    /// <summary>Verify original hash preimages against the independently supplied
    /// approved context. Never substitute a fresh preview after a partial acquire.</summary>
    public static HolonReplicationMigrationBarrierPlan ReadCanonical(string json,
        HolonReplicationMigrationAuthorizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Require(json != null && Encoding.UTF8.GetByteCount(json) <= MaximumUtf8Bytes, "Recovery evidence exceeds its explicit bound.");
        using var envelope = Parse(json); var root = envelope.RootElement;
        Require(root.ValueKind == JsonValueKind.Object && root.EnumerateObject().Count() == 4
            && root.GetProperty("format").GetString() == "cosmic-migration-barrier-plan-v1"
            && root.GetProperty("contextDigest").GetString() == context.ContextDigest, "Recovery envelope or administrator scope differs.");
        var material = root.GetProperty("previewMaterial").GetString();
        Require(material != null && Hash(material) == context.PreviewDigest, "Original preview hash does not match the approved context.");
        using var preview = Parse(material); var p = preview.RootElement;
        Require(p.GetProperty("Format").GetString() == "cosmic-lineage-migration-preview-v3"
            && p.GetProperty("source").GetString() == context.SourceStorage.BindingFingerprint
            && p.GetProperty("target").GetString() == context.OriginalTargetStorage.BindingFingerprint
            && p.GetProperty("OriginalSourceId").GetString() == context.OriginalSourceId
            && p.GetProperty("OriginalTargetId").GetString() == context.OriginalTargetId
            && p.GetProperty("replacementSource").GetString() == context.ReplacementRoute.SourceId
            && p.GetProperty("replacementTarget").GetString() == context.ReplacementRoute.TargetId
            && p.GetProperty("replacementPolicy").GetString() == context.ReplacementRoute.PolicyBinding,
            "Original preview route or physical store differs.");
        foreach (var finding in p.GetProperty("Findings").EnumerateArray())
            if ((HolonReplicationMigrationFinding)finding.GetInt32() is not
                (HolonReplicationMigrationFinding.NativeWriteAndDeliveryFencesRequired or
                 HolonReplicationMigrationFinding.ReplacementTargetEvidenceUnverified or HolonReplicationMigrationFinding.DeliveryInFlight))
                throw new HolonMigrationBarrierPlanException("COSMIC_MIGRATION_BARRIER_PREVIEW_UNVERIFIED", "Original graph, transport and historical receiver proof are required.");
        var summaries = root.GetProperty("receiptMaterials").EnumerateArray().Select(r => r.GetString()).ToArray();
        var hashes = p.GetProperty("receipts").EnumerateArray().Select(r => r.GetString()).ToArray();
        Require(summaries.Length > 0 && summaries.Length <= 4096 && summaries.Length == hashes.Length,
            "Every original receipt summary is required.");
        var ids = new HashSet<Guid>();
        for (var i = 0; i < summaries.Length; i++)
        {
            Require(summaries[i] != null && Hash(summaries[i]) == hashes[i], "Original receipt summary hash differs.");
            using var summary = Parse(summaries[i]); var r = summary.RootElement;
            Require(r.GetProperty("Format").GetString() == "cosmic-receipt-preflight-v1"
                && r.GetProperty("SourceStorageFingerprint").GetString() == context.SourceStorage.BindingFingerprint
                && r.GetProperty("TargetStorageFingerprint").GetString() == context.OriginalTargetStorage.BindingFingerprint
                && r.GetProperty("OriginalSourceId").GetString() == context.OriginalSourceId, "Original receipt physical scope differs.");
            var records = r.GetProperty("Records").EnumerateArray().ToArray();
            Require(records.Length > 0 && records.Length == r.GetProperty("OriginalSelectedRecordCount").GetInt32(), "Original graph summary is incomplete.");
            var selected = new HashSet<Guid>();
            foreach (var record in records)
            {
                Require(record.GetProperty("Kind").GetInt32() == (int)HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt,
                    "Original target evidence does not match the receipt.");
                var id = record.GetProperty("Original").GetProperty("HolonId").GetGuid();
                Require(id != Guid.Empty && selected.Add(id), "Original record identity is empty or repeated."); ids.Add(id);
            }
        }
        Require(ids.Count <= 4096, "The complete record scope exceeds its explicit bound.");
        Require(Encode(context.ContextDigest, material, summaries) == json, "Recovery envelope is not canonical.");
        return new(context, json, ids.OrderBy(id => id.ToString("D"), StringComparer.Ordinal));
    }
}
