using System;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public enum HolonChildRoleWriteKind { SourceSave, SourceDelete, CommittedReplica }

/// <summary>Resolves role ownership before native body, acknowledgement and receipt capture.
/// The caller supplies a persisted observation protected by its native write transaction.</summary>
public static class HolonChildRolePersistence
{
    public static string Resolve(PlannedHolonWrite write, HolonChildRoleWriteKind kind,
        bool persistedRecordExists, string persistedManifestJson)
    {
        ArgumentNullException.ThrowIfNull(write);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        string manifest;
        if (kind == HolonChildRoleWriteKind.CommittedReplica)
            manifest = write.Snapshot.ChildRoleManifestJson;
        else if (kind == HolonChildRoleWriteKind.SourceSave && write.SelectsChildren)
        {
            if (write.RelationshipManifest == null)
                throw new InvalidDataException("A selected child membership requires its captured role manifest.");
            manifest = HolonChildRoleManifestJson.Serialize(write.RelationshipManifest);
        }
        else
            manifest = persistedRecordExists ? persistedManifestJson : write.Snapshot.ChildRoleManifestJson;
        if (manifest != null && HolonChildRoleManifestJson.Deserialize(manifest).ParentId != write.Snapshot.Id)
            throw new InvalidDataException("The persisted child-role observation belongs to another owner.");
        return manifest;
    }
}
