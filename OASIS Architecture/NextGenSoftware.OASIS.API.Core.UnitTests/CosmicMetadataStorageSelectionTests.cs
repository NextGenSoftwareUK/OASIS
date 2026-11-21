using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicMetadataStorageSelectionTests
{
    private sealed class Keys : IHolonMetadataKeyResolver
    {
        public List<HolonMetadataKeyMaterial> Leases = new();
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            var key = new HolonMetadataKeyMaterial(Enumerable.Repeat((byte)97, 32).ToArray());
            Leases.Add(key); return new(key);
        }
    }
    private static (HolonManager Manager, Holon Value, Keys Keys, HolonMetadataEncryptionPolicy Policy) Fixture()
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        var keys = new Keys();
        var policy = new HolonMetadataEncryptionPolicy("metadata-storage-selection", "key", keys, new[] { "Visible", "a/b~c" });
        manager.MetadataEncryptionPolicy = policy;
        var value = new Holon { Id = Guid.NewGuid(), Name = "plain scalar", MetaData = new() {
            ["Visible"] = "public", ["hidden"] = "private", ["a/b~c"] = "escaped public",
            ["visible"] = "case-sensitive private", ["branch"] = new Dictionary<string, object> { ["x"] = 9007199254740993L } } };
        return (manager, value, keys, policy);
    }

    [Theory]
    [InlineData("empty")][InlineData("queryable-only")][InlineData("null")]
    public void RemovedLastEncryptedKeyStillMapsToStoredMetadataWithoutEmittingCiphertext(string shape)
    {
        var f = Fixture();
        f.Value.MetaData = shape == "null" ? null! : shape == "empty" ? new() : new() { ["Visible"] = "public" };
        var profile = f.Manager.EncryptHolonMetaDataForSelection(f.Value);
        Assert.True(profile.UsesEnvelope); Assert.False(profile.RequiresMaterialization);
        Assert.Equal(new[] { "/MetaData", "/Name" }, profile.StoredPaths(new[] { "/Name", "/MetaData/removed-secret" }));
        Assert.Empty(f.Keys.Leases);
        if (f.Value.MetaData != null) Assert.False(f.Value.MetaData.ContainsKey(HolonSaveMetadataOwnership.EncryptionEnvelope));
    }

    [Fact]
    public void ActualEncryptionOwnerKeepsQueryableKeysAndReportsNoKeyMaterial()
    {
        var f = Fixture();
        var secret = f.Value.MetaData.Where(row => row.Key != "Visible" && row.Key != "a/b~c").ToDictionary(row => row.Key, row => row.Value);
        var expected = StoredRecordJson.Serialize(secret);
        var profile = f.Manager.EncryptHolonMetaDataForSelection(f.Value);
        Assert.True(profile.UsesEnvelope); Assert.False(profile.RequiresMaterialization);
        Assert.Equal(3, f.Value.MetaData.Count);
        Assert.Equal("public", f.Value.MetaData["Visible"]); Assert.Equal("escaped public", f.Value.MetaData["a/b~c"]);
        Assert.False(f.Value.MetaData.ContainsKey("hidden")); Assert.False(f.Value.MetaData.ContainsKey("visible"));
        var envelope = f.Value.MetaData[HolonSaveMetadataOwnership.EncryptionEnvelope].ToString()!;
        Assert.Equal(expected, HolonMetadataCipher.Decrypt(f.Value.Id, envelope, f.Policy));
        Assert.Equal(2, f.Keys.Leases.Count); Assert.All(f.Keys.Leases, lease => Assert.True(lease.IsDisposed));
        Assert.Contains("Visible", profile.QueryableKeys); Assert.DoesNotContain("hidden", profile.QueryableKeys);
    }

    [Theory]
    [InlineData("scalar")][InlineData("queryable")][InlineData("escaped")]
    [InlineData("case")][InlineData("nested")][InlineData("whole")][InlineData("mixed")]
    public void StoragePathsFollowTheActualOwnersOrdinalExemptionRule(string mode)
    {
        var f = Fixture(); var profile = f.Manager.EncryptHolonMetaDataForSelection(f.Value);
        var (logical, stored) = mode switch
        {
            "scalar" => (new[] { "/Name" }, new[] { "/Name" }),
            "queryable" => (new[] { "/MetaData/Visible" }, new[] { "/MetaData/Visible" }),
            "escaped" => (new[] { "/MetaData/a~1b~0c" }, new[] { "/MetaData/a~1b~0c" }),
            "case" => (new[] { "/MetaData/visible" }, new[] { "/MetaData" }),
            "nested" => (new[] { "/MetaData/branch/x" }, new[] { "/MetaData" }),
            "whole" => (new[] { "/MetaData" }, new[] { "/MetaData" }),
            _ => (new[] { "/Name", "/MetaData/hidden", "/MetaData/Visible" }, new[] { "/MetaData", "/Name" })
        };
        Assert.Equal(stored, profile.StoredPaths(logical));
    }

    [Fact]
    public void RecordRuntimeEncryptionOverrideUsesTheSameOwner()
    {
        var f = Fixture(); f.Manager.MetadataEncryptionPolicy = null!; f.Value.MetadataEncryptionOverride = f.Policy;
        var profile = f.Manager.EncryptHolonMetaDataForSelection(f.Value);
        Assert.True(profile.UsesEnvelope);
        Assert.Equal("/MetaData", Assert.Single(profile.StoredPaths(new[] { "/MetaData/hidden" })));
        Assert.True(f.Value.MetaData.ContainsKey(HolonSaveMetadataOwnership.EncryptionEnvelope));
        Assert.Single(f.Keys.Leases); Assert.True(f.Keys.Leases[0].IsDisposed);
    }

    [Fact]
    public void ExplicitDisabledLegacyOverridePreservesMetadataWithoutUsingGlobalKey()
    {
        var f = Fixture(); f.Value.DataEncryptionOverride = new EncryptionSettings { Rijndael256EncryptionEnabled = false, QuantumEncryptionEnabled = false };
        var before = StoredRecordJson.Serialize(f.Value.MetaData);
        var profile = f.Manager.EncryptHolonMetaDataForSelection(f.Value);
        Assert.False(profile.UsesEnvelope); Assert.Same(HolonMetadataStorageSelection.None, profile);
        Assert.Equal("/MetaData/hidden", Assert.Single(profile.StoredPaths(new[] { "/MetaData/hidden" })));
        Assert.Equal(before, StoredRecordJson.Serialize(f.Value.MetaData)); Assert.Empty(f.Keys.Leases);
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public void OpaqueLegacyInputCannotBeMistakenForMaterializedLogicalMetadata(bool quantum)
    {
        var f = Fixture(); f.Manager.MetadataEncryptionPolicy = null!;
        f.Value.DataEncryptionOverride = new EncryptionSettings { Rijndael256EncryptionEnabled = !quantum, QuantumEncryptionEnabled = quantum };
        f.Value.MetaData = new() { [HolonSaveMetadataOwnership.EncryptionEnvelope] = "legacy opaque input" };
        var before = StoredRecordJson.Serialize(f.Value.MetaData);
        var profile = f.Manager.EncryptHolonMetaDataForSelection(f.Value);
        Assert.True(profile.UsesEnvelope); Assert.True(profile.RequiresMaterialization);
        Assert.Throws<InvalidOperationException>(() => profile.StoredPaths(new[] { "/MetaData/hidden" }));
        Assert.Equal("/Name", Assert.Single(profile.StoredPaths(new[] { "/Name" })));
        Assert.Equal(before, StoredRecordJson.Serialize(f.Value.MetaData)); Assert.Empty(f.Keys.Leases);
    }

    [Fact]
    public void ConflictingRuntimePoliciesFailBeforeChangingMetadata()
    {
        var f = Fixture(); f.Value.MetadataEncryptionOverride = f.Policy; f.Value.DataEncryptionOverride = new EncryptionSettings();
        var before = StoredRecordJson.Serialize(f.Value.MetaData);
        Assert.Throws<InvalidOperationException>(() => f.Manager.EncryptHolonMetaDataForSelection(f.Value));
        Assert.Equal(before, StoredRecordJson.Serialize(f.Value.MetaData)); Assert.Empty(f.Keys.Leases);
    }

    [Fact]
    public void OwnerReportCannotHaveItsExemptionSetMutatedAfterEncryption()
    {
        var f = Fixture(); var profile = f.Manager.EncryptHolonMetaDataForSelection(f.Value);
        Assert.Throws<NotSupportedException>(() => ((ICollection<string>)profile.QueryableKeys).Add("hidden"));
        Assert.Equal("/MetaData", Assert.Single(profile.StoredPaths(new[] { "/MetaData/hidden" })));
    }
}
