using System;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

// Opt-in tamper-evident history. The seal is computed inside the same compare-and-swap that writes the version, from the
// exact observed document, so two simultaneous saves cannot fork the chain. With sealing off the only effect is that the
// seal fields are cleared (they are BsonIgnoreIfNull, so an unsealed document is identical to one written before this existed).
internal static class MongoHolonSeal
{
    internal const string SealField = "VersionSeal";
    internal const string PreviousSealField = "PreviousVersionSeal";
    internal const string KeyIdField = "VersionSealKeyId";

    // Fields that differ between replicas or change on every write without being version content.
    private static readonly HashSet<string> Excluded = new HashSet<string>(StringComparer.Ordinal)
    {
        "_id", SealField, PreviousSealField, KeyIdField, MongoNativeHolonHistory.Field, MongoCqlMembership.Field,
        "ProviderMetaData", "ProviderRevisionId", "ProviderUniqueStorageKey", "PreviousVersionProviderUniqueStorageKey",
        "Children", "AllChildren", "InstanceSavedOnProviderType"
    };

    /// <summary>Canonical text of one stored version: sorted keys, canonical extended JSON scalars, excluded fields removed.</summary>
    internal static string Canonical(BsonDocument document)
    {
        var sb = new StringBuilder();
        Write(sb, document, true);
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, BsonValue value, bool top)
    {
        if (value.IsBsonDocument)
        {
            sb.Append('{');
            var first = true;
            foreach (var element in value.AsBsonDocument.OrderBy(e => e.Name, StringComparer.Ordinal))
            {
                if (top && Excluded.Contains(element.Name)) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append(element.Name.Length).Append(':').Append(element.Name).Append('=');
                Write(sb, element.Value, false);
            }
            sb.Append('}');
        }
        else if (value.IsBsonArray)
        {
            sb.Append('[');
            var first = true;
            foreach (var item in value.AsBsonArray)
            {
                if (!first) sb.Append(',');
                first = false;
                Write(sb, item, false);
            }
            sb.Append(']');
        }
        else
            sb.Append(value.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson }));
    }

    internal static Guid Owner(string createdByAvatarId) => Guid.TryParse(createdByAvatarId, out var g) ? g : Guid.Empty;

    /// <summary>Clears any carried-over seal fields, then seals the replacement when the owner or app switch is on.</summary>
    internal static void Apply(BsonDocument replacement, BsonDocument observed, Guid ownerAvatarId)
    {
        replacement.Remove(SealField); replacement.Remove(PreviousSealField); replacement.Remove(KeyIdField);
        if (!HolonSealing.ShouldSeal(ownerAvatarId)) return;
        var key = HolonSealing.Policy?.SigningKey;
        if (key == null || !key.IsUsable) return;
        var previous = "";
        if (observed != null)
        {
            var observedVersion = observed.TryGetValue("Version", out var ov) && ov.IsNumeric ? ov.ToInt32() : -1;
            var newVersion = replacement.TryGetValue("Version", out var nv) && nv.IsNumeric ? nv.ToInt32() : -2;
            // A sealed save must be the exact successor of the version it observed. A stale or repeated version would fork the chain.
            if (newVersion != observedVersion + 1)
                throw new StorageLifecycleConflictException("Sealed holon history: this save is not the next version of the stored holon; reload and retry.");
            if (observed.TryGetValue(SealField, out var ps) && ps.IsString) previous = ps.AsString;
        }
        var seal = HolonSealing.ComputeSeal(key, previous, Canonical(replacement));
        if (previous.Length > 0) replacement[PreviousSealField] = previous;
        replacement[KeyIdField] = key.KeyId;
        replacement[SealField] = seal;
    }

    private static HolonSealEntry ToEntry(BsonDocument document)
    {
        string Str(string name) => document.TryGetValue(name, out var v) && v.IsString ? v.AsString : null;
        Guid Id(string name)
        {
            if (!document.TryGetValue(name, out var v)) return Guid.Empty;
            if (v.IsString && Guid.TryParse(v.AsString, out var g)) return g;
            if (v.IsBsonBinaryData) { try { return v.AsGuid; } catch (Exception) { return Guid.Empty; } }
            return Guid.Empty;
        }
        return new HolonSealEntry
        {
            Version = document.TryGetValue("Version", out var ver) && ver.IsNumeric ? ver.ToInt32() : 0,
            VersionId = Id("VersionId"),
            PreviousVersionId = Id("PreviousVersionId"),
            Seal = Str(SealField),
            PreviousSeal = Str(PreviousSealField),
            KeyId = Str(KeyIdField),
            Canonical = Canonical(document)
        };
    }

    /// <summary>Every stored version of one holon document: the archived snapshots (byte for byte) and the current one.</summary>
    internal static IReadOnlyList<HolonSealEntry> Entries(BsonDocument current)
    {
        var entries = new List<HolonSealEntry>();
        if (current.TryGetValue(MongoNativeHolonHistory.Field, out var archive) && archive.IsBsonDocument
            && archive.AsBsonDocument.TryGetValue("records", out var records) && records.IsBsonArray)
            foreach (var entry in records.AsBsonArray)
                if (entry.IsBsonDocument && entry.AsBsonDocument.TryGetValue("bson", out var snapshot) && snapshot.IsBsonDocument)
                    entries.Add(ToEntry(snapshot.AsBsonDocument));
        entries.Add(ToEntry(current));
        return entries;
    }
}
