using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jAvatarSnapshotTests
{
    [DataTestMethod]
    [DataRow("malformed")] [DataRow("typed")] [DataRow("type-name")]
    public void StoredSnapshotJsonErrorsAreTerminalIntegrityFailures(string kind)
    {
        var id=Guid.NewGuid();
        var json=kind=="malformed" ? "not-json":kind=="typed" ? $"{{\"Id\":\"{id}\",\"Version\":\"not-an-integer\"}}"
            :$"{{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"Id\":\"{id}\"}}";
        var error=Assert.ThrowsException<InvalidDataException>(()=>Provider.ReadAvatarSnapshot(json,id.ToString()));
        Assert.IsInstanceOfType(error.InnerException,typeof(Newtonsoft.Json.JsonException));
    }

    [TestMethod]
    public void PopulatedAvatarPreservesFieldsOutsideOldScalarProjectionWithoutMutatingSource()
    {
        var source = new Avatar { Id = Guid.NewGuid(), Username = "owned", Email = "owned@example.invalid",
            FirstName = "Full", LastName = "Record", Password = "test-only-already-hashed",
            Description = "preserve full record", IsActive = true, Version = 7, VersionId = Guid.NewGuid() };
        source.MetaData["biome"] = "woodland";
        source.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "preserved-other-provider-key";
        var staged = Provider.StageAvatar(source);
        var loaded = Provider.ReadAvatarSnapshot(OasisJson.Serialize(staged), source.Id.ToString());
        Assert.AreEqual(source.FirstName, loaded.FirstName); Assert.AreEqual(source.LastName, loaded.LastName);
        Assert.AreEqual(source.Password, loaded.Password); Assert.AreEqual(source.Description, loaded.Description);
        Assert.AreEqual(source.Version, loaded.Version); Assert.AreEqual(source.VersionId, loaded.VersionId);
        Assert.IsTrue(loaded.IsActive); Assert.AreEqual("woodland", loaded.MetaData["biome"]);
        Assert.AreEqual(source.Id.ToString(), loaded.ProviderUniqueStorageKey[ProviderType.Neo4jOASIS]);
        Assert.AreEqual("preserved-other-provider-key", loaded.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
        Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(ProviderType.Neo4jOASIS));
        Assert.AreNotSame(source, staged);
    }

    [TestMethod]
    public void NullClearingUsesCompleteReplacementSnapshot()
    {
        var source = new Avatar { Id = Guid.NewGuid(), Username = "owned", FirstName = "remove me" };
        var before = Provider.StageAvatar(source);
        source.FirstName = null;
        source.MetaData.Clear();
        var after = Provider.ReadAvatarSnapshot(OasisJson.Serialize(Provider.StageAvatar(source)), source.Id.ToString());
        Assert.AreEqual("remove me", before.FirstName);
        Assert.IsNull(after.FirstName);
        Assert.AreEqual(0, after.MetaData.Count);
    }

    [TestMethod]
    public void NewIdentityIsStagedUntilAcknowledgement()
    {
        var source = new Avatar { Id = Guid.Empty };
        var staged = Provider.StageAvatar(source);
        Assert.AreNotEqual(Guid.Empty, staged.Id);
        Assert.AreEqual(Guid.Empty, source.Id);
        Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(ProviderType.Neo4jOASIS));
    }

    [DataTestMethod]
    [DataRow(null)] [DataRow("")] [DataRow(" ")]
    public void MissingSnapshotCannotSilentlyFallBackToSparseAvatar(string json)
        => Assert.ThrowsException<InvalidDataException>(() => Provider.ReadAvatarSnapshot(json, Guid.NewGuid().ToString()));

    [TestMethod]
    public void WrongIndexedIdentityCannotReturnAnotherAvatar()
    {
        var source = new Avatar { Id = Guid.NewGuid() };
        Assert.ThrowsException<InvalidDataException>(() => Provider.ReadAvatarSnapshot(OasisJson.Serialize(source), Guid.NewGuid().ToString()));
        Assert.ThrowsException<InvalidDataException>(() => Provider.ReadAvatarSnapshot(OasisJson.Serialize(source), "invalid"));
    }
}
