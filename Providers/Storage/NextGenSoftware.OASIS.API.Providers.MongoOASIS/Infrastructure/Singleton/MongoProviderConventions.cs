using System;
using System.Runtime.CompilerServices;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;

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
        // Preserve existing OASIS Guid-string and metadata BSON contracts before any
        // entity/member lookup can freeze driver defaults. Conflicting host policy stays an error.
        SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
        SerializerRegister.GetInstance().RegisterMetaDataDictionarySerializer();
    }
}
