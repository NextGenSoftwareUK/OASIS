using System;
using System.Runtime.CompilerServices;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton;

internal static class MongoProviderConventions
{
    // CLR module initialization runs once, before this assembly's entity maps can be used.
    // Configure object MEMBERS rather than claiming ownership of the host's global serializer.
    [ModuleInitializer]
    internal static void Register()
    {
        ConventionRegistry.Register("OASIS.MongoDB.ObjectMembers",new ConventionPack {
            new ObjectSerializerAllowedTypesConvention(type=>ObjectSerializer.DefaultAllowedTypes(type)
                || type.FullName?.StartsWith("NextGenSoftware",StringComparison.Ordinal)==true
                || type.FullName?.StartsWith("System",StringComparison.Ordinal)==true)
        },type=>type.Namespace?.StartsWith("NextGenSoftware.",StringComparison.Ordinal)==true);
        ConventionRegistry.Register("OASIS.MongoDB.DictionaryRepresentation",new ConventionPack {
            new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays)
        },_=>true);
        ConventionRegistry.Register("OASIS.MongoDB.CoreRecordNavigation",new ConventionPack {
            new CoreRecordNavigationConvention()
        },type=>typeof(IAuditBase).IsAssignableFrom(type)
            && type.Namespace?.StartsWith("NextGenSoftware.OASIS.API.Core.",StringComparison.Ordinal)==true);
        // Preserve existing OASIS Guid-string and metadata BSON contracts before any
        // entity/member lookup can freeze driver defaults. Conflicting host policy stays an error.
        SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
        SerializerRegister.GetInstance().RegisterMetaDataDictionarySerializer();
    }

    private sealed class CoreRecordNavigationConvention:ConventionBase,IPostProcessingConvention
    {
        public void PostProcess(BsonClassMap map)
        {
            // Unmap before a lazy navigation getter can execute. The canonical record contract
            // stores its IDs, not hydrated objects. Ignore old embedded navigation/extension data
            // on these Core value classes, matching canonical JSON's unknown-member behavior.
            foreach(var member in map.DeclaredMemberMaps.Where(member=>StoredRecordJson.IsNavigationMember(member.MemberType)).ToArray())
                map.UnmapMember(member.MemberInfo);
            map.SetIgnoreExtraElements(true);
        }
    }
}
