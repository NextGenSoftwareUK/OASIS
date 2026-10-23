using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    // These fields have one relational owner. The extension stores only the remaining
    // persistent Holon values; it cannot override a column or store runtime navigation.
    private static readonly HashSet<string> HolonColumnFields=new(StringComparer.Ordinal){
        nameof(IHolon.Id),nameof(IHolon.ParentHolonId),nameof(IHolon.Name),nameof(IHolon.Description),nameof(IHolon.HolonType),
        nameof(IHolon.Version),nameof(IHolon.VersionId),nameof(IHolon.PreviousVersionId),nameof(IHolon.IsActive),nameof(IHolon.IsPublic),
        nameof(IHolon.CustomHolonType),nameof(IHolon.CreatedDate),nameof(IHolon.CreatedByAvatarId),nameof(IHolon.ModifiedDate),
        nameof(IHolon.ModifiedByAvatarId),nameof(IHolon.DeletedDate),nameof(IHolon.DeletedByAvatarId),nameof(IHolon.CreatedProviderType),
        nameof(IHolon.CreatedOASISType),nameof(IHolon.DimensionLevel),nameof(IHolon.SubDimensionLevel),nameof(IHolon.MetaData),
        nameof(IHolon.ProviderUniqueStorageKey),nameof(IHolon.InstanceSavedOnProviderType)};

    private static string CaptureHolonPersistentValues(IHolon source)
    {
        var record=StoredRecordJson.Project<Holon>(source);
        // Splitting native columns must not reinterpret metadata strings or narrow numbers.
        var values=OasisJson.ParseStoredObject(StoredRecordJson.Serialize(record));
        foreach(var field in HolonColumnFields)values.Remove(field);
        // The native column owns this provider's physical observation.
        if(values[nameof(IHolon.ProviderMetaData)] is JObject providers
            && providers[ProviderType.SQLLiteDBOASIS.ToString()] is JObject local)
            local.Remove(CosmicConcurrency.ProviderRevisionMetadataKey);
        return values.ToString(Formatting.None);
    }

    private static IHolon RestoreHolonPersistentValues(HolonModel model,Holon mapped)
    {
        try
        {
            var values=OasisJson.ParseStoredObject(model.PersistentValueJson ?? throw new InvalidDataException("Stored holon extension is missing."));
            if(values.Property(nameof(Holon.ChildRoleManifestJson)) is { } relationship)
            {
                if(relationship.Value.Type!=JTokenType.String)
                    throw new InvalidDataException("Stored relationship state must be canonical manifest JSON.");
                var manifest=HolonChildRoleManifestJson.Deserialize((string)relationship.Value);
                if(manifest.ParentId!=Guid.Parse(model.Id))throw new InvalidDataException("Stored relationship parent differs from record identity.");
            }
            if(values.Properties().Any(property=>HolonColumnFields.Contains(property.Name) || property.Name.StartsWith("$",StringComparison.Ordinal)))
                throw new InvalidDataException("Stored holon extension overlaps relational or serializer-owned fields.");
            if(values[nameof(IHolon.ProviderMetaData)] is JObject providers
                && providers[ProviderType.SQLLiteDBOASIS.ToString()] is JObject local
                && local.Property(CosmicConcurrency.ProviderRevisionMetadataKey)!=null)
                throw new InvalidDataException("Stored holon extension contains a duplicate native revision observation.");
            var document=OasisJson.ParseStoredObject(StoredRecordJson.Serialize(mapped));
            foreach(var property in values.Properties())document[property.Name]=property.Value.DeepClone();
            var restored=StoredRecordJson.DeserializeStored<Holon>(document.ToString(Formatting.None))
                ?? throw new InvalidDataException("Stored holon extension has no canonical value.");
            var roundTrip=OasisJson.ParseStoredObject(StoredRecordJson.Serialize(restored));
            if(values.Properties().Any(property=>roundTrip.Property(property.Name)==null))
                throw new InvalidDataException("Stored holon extension contains unknown or nonpersistent fields.");
            // Keep the relational codecs and physical observation authoritative.
            restored.MetaData=mapped.MetaData;restored.ProviderUniqueStorageKey=mapped.ProviderUniqueStorageKey;
            CosmicConcurrency.SetProviderRevisionId(restored,ProviderType.SQLLiteDBOASIS,model.ProviderRevisionId);
            restored.InstanceSavedOnProviderType=new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
            restored.IsNewHolon=false;
            return restored;
        }
        catch(JsonException cause){throw new InvalidDataException("Stored holon extension is invalid JSON.",cause);}
    }
}
