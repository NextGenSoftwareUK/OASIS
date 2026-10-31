using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoRuntimePolicyTests
{
    private sealed class NeverResolve : IHolonMetadataKeyResolver
    {
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
            => throw new InvalidOperationException("Serialization must not execute a runtime key resolver.");
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void CoreRuntimePoliciesAreExcludedBeforeBsonSerializerCreation(bool embedded)
    {
        var policy = new HolonMetadataEncryptionPolicy("runtime-only-context", "runtime-only-key", new NeverResolve());
        var old = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = "runtime-only-legacy-key" };
        var source = new Holon { Id = Guid.NewGuid(), MetadataEncryptionOverride = policy, DataEncryptionOverride = old,
            Children = new List<IHolon> { new Holon { Id = Guid.NewGuid(), Name = "stored child" } } };
        var document = embedded ? new NativeHolon { HolonId = Guid.NewGuid(), Children = new List<IHolon> { source } }.ToBsonDocument()
            : source.ToBsonDocument();
        var json = document.ToJson();
        var coreDocument = embedded ? document["Children"][0].AsBsonDocument : document;
        foreach (var member in new[] { "MetadataEncryptionOverride", "DataEncryptionOverride", "GlobalHolonData" })
            Assert.IsFalse(coreDocument.Contains(member), member + " must not enter Core BSON.");
        foreach (var runtime in new[] { "MetadataEncryptionOverride", "DataEncryptionOverride", "runtime-only-context", "runtime-only-key", "runtime-only-legacy-key" })
            Assert.IsFalse(json.Contains(runtime, StringComparison.Ordinal), runtime + " must not enter BSON.");
        var restored = embedded ? BsonSerializer.Deserialize<NativeHolon>(document).Children.Single() : BsonSerializer.Deserialize<Holon>(document);
        Assert.AreEqual(source.Id, restored.Id); Assert.AreEqual("stored child", restored.Children.Single().Name);
        Assert.IsNull(((Holon)restored).MetadataEncryptionOverride); Assert.IsNull(((Holon)restored).DataEncryptionOverride);
        Assert.AreSame(policy, source.MetadataEncryptionOverride); Assert.AreSame(old, source.DataEncryptionOverride);
    }
}
