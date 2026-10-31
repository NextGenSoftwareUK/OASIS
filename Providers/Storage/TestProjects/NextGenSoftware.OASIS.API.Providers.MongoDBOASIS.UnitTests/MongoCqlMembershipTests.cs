using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton;
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoCqlMembershipTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AbsentAndDeclaredEmptyMembershipRemainDistinct(bool declared)
    {
        var owner = Guid.NewGuid();
        var manifest = declared ? Manifest(owner, Array.Empty<Guid>()) : null;
        var projection = MongoCqlMembership.Build(owner, manifest!);

        Assert.AreEqual(1, projection["schema"].AsInt32);
        Assert.AreEqual(owner.ToString("D"), projection["owner"].AsString);
        Assert.AreEqual(declared, projection["declared"].AsBoolean);
        Assert.AreEqual(0, projection["members"].AsBsonArray.Count);
        Assert.AreEqual<BsonValue>(manifest == null ? BsonNull.Value : new BsonString(manifest), projection["source"]);
    }

    [TestMethod]
    public void NamedRolesAreIncludedAndTheExactCanonicalObservationIsRetained()
    {
        var owner = Guid.NewGuid();
        var children = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var manifest = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1, owner,
            new[] { new HolonChildRole("Children", children.Take(1)), new HolonChildRole("Extras", children.Skip(1)) }));
        var projection = MongoCqlMembership.Build(owner, manifest);

        CollectionAssert.AreEqual(children.Select(id => id.ToString("D")).ToArray(),
            projection["members"].AsBsonArray.Select(id => id.AsString).ToArray());
        Assert.AreEqual(manifest, projection["source"].AsString);
    }

    [TestMethod]
    public void ForeignOwnerManifestCannotBecomeProviderQueryState()
        => Assert.ThrowsException<BsonSerializationException>(() =>
            MongoCqlMembership.Build(Guid.NewGuid(), Manifest(Guid.NewGuid(), new[] { Guid.NewGuid() })));

    [DataTestMethod]
    [DataRow("")]
    [DataRow("{bad-json")]
    public void InvalidDeclaredManifestFailsSerialization(string manifest)
        => Assert.ThrowsException<BsonSerializationException>(() => MongoCqlMembership.Build(Guid.NewGuid(), manifest));

    [TestMethod]
    public void FinalWriterOwnsProjectionAndUnknownFieldRetentionCannotRestoreStaleMembership()
    {
        SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
        SerializerRegister.GetInstance().RegisterMetaDataDictionarySerializer();
        var owner = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var member = Guid.NewGuid();
        var record = new NativeHolon { HolonId = owner, ProviderRevisionId = revision,
            ChildRoleManifestJson = Manifest(owner, new[] { member }) };
        // This collection supplies only its serializer; the test sends no commands.
        var collection = new MongoClient("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1")
            .GetDatabase("membership-unit-only").GetCollection<NativeHolon>("Holon");
        var replacement = MongoNativeRecordWriter.SerializePayload(collection, record);
        var observed = new BsonDocument { { MongoCqlMembership.Field, new BsonDocument("members", new BsonArray()) },
            { "preservedUnknown", "kept" } };
        MongoNativeRecordWriter.RetainUnknownFields<NativeHolon>(replacement, observed);

        var projection = replacement[MongoCqlMembership.Field].AsBsonDocument;
        Assert.AreEqual(member.ToString("D"), projection["members"].AsBsonArray.Single().AsString);
        Assert.AreEqual("kept", replacement["preservedUnknown"].AsString);
        Assert.AreEqual(revision, record.ProviderRevisionId);
        Assert.AreEqual(record.ChildRoleManifestJson, projection["source"].AsString);
        Assert.AreEqual(owner, BsonSerializer.Deserialize<NativeHolon>(replacement).HolonId);
    }

    private static string Manifest(Guid owner, IEnumerable<Guid> members)
        => HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1, owner,
            new[] { new HolonChildRole("Children", members) }));
}
