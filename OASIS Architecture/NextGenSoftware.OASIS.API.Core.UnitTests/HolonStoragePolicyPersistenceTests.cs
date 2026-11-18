using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class HolonStoragePolicyTests_Persistence
{
    [Fact]
    public void CanonicalRepresentationRoundTripsEveryDeclaredMode()
    {
        foreach (var mode in new[] { HolonStorageMode.Materialized, HolonStorageMode.Virtual, HolonStorageMode.Hybrid })
        {
            var original = new HolonStoragePolicy(mode, HolonReadCacheMode.None, 0, 0, mode != HolonStorageMode.Virtual);
            var actual = HolonStoragePolicyCodec.Deserialize(HolonStoragePolicyCodec.Serialize(original));
            Assert.Equal(mode, actual.Mode);
            Assert.Equal(original.MaterializeOnExplicitSave, actual.MaterializeOnExplicitSave);
        }
        var cache = new HolonStoragePolicy(HolonStorageMode.Hybrid, HolonReadCacheMode.Memory, 1073741824, 86400, false);
        var restored = HolonStoragePolicyCodec.Deserialize(HolonStoragePolicyCodec.Serialize(cache));
        Assert.Equal(cache.MaximumCacheBytes, restored.MaximumCacheBytes);
        Assert.Equal(cache.MaximumCacheAgeSeconds, restored.MaximumCacheAgeSeconds);
        Assert.Equal(cache.ReadCache, restored.ReadCache);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"schemaVersion\":1,\"mode\":\"Virtual\",\"readCache\":\"None\",\"maximumCacheBytes\":0,\"maximumCacheAgeSeconds\":0,\"materializeOnExplicitSave\":false,\"extra\":1}")]
    [InlineData("{\"schemaVersion\":1,\"mode\":\"Virtual\",\"mode\":\"Hybrid\",\"readCache\":\"None\",\"maximumCacheBytes\":0,\"maximumCacheAgeSeconds\":0,\"materializeOnExplicitSave\":false}")]
    [InlineData("{\"schemaVersion\":2,\"mode\":\"Virtual\",\"readCache\":\"None\",\"maximumCacheBytes\":0,\"maximumCacheAgeSeconds\":0,\"materializeOnExplicitSave\":false}")]
    [InlineData("{\"schemaVersion\":1,\"mode\":\"1\",\"readCache\":\"None\",\"maximumCacheBytes\":0,\"maximumCacheAgeSeconds\":0,\"materializeOnExplicitSave\":false}")]
    [InlineData("{\"schemaVersion\":1,\"mode\":\"virtual\",\"readCache\":\"None\",\"maximumCacheBytes\":0,\"maximumCacheAgeSeconds\":0,\"materializeOnExplicitSave\":false}")]
    [InlineData("{\"schemaVersion\":1,\"mode\":\"Virtual\",\"readCache\":\"None\",\"maximumCacheBytes\":\"0\",\"maximumCacheAgeSeconds\":0,\"materializeOnExplicitSave\":false}")]
    [InlineData("{\"schemaVersion\":1,\"mode\":\"Virtual\",\"readCache\":\"None\",\"maximumCacheBytes\":0,\"maximumCacheAgeSeconds\":0,\"materializeOnExplicitSave\":\"false\"}")]
    public void InvalidPoliciesCannotBecomeDefaults(string json)
        => Assert.Throws<FormatException>(() => HolonStoragePolicyCodec.Deserialize(json));


    [Fact]
    public void PersistedJsonSettingsObjectsPreserveTheirPolicyAndUnselectedValues()
    {
        var full = new HolonStoragePolicy(HolonStorageMode.Materialized, HolonReadCacheMode.None, 0, 0, true);
        object preferences = System.Text.Json.JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["holonStoragePolicies"] = new Dictionary<string, object> { ["avatar"] = HolonStoragePolicyCodec.Serialize(full) },
            ["limit"] = long.MaxValue,
            ["fraction"] = 12345.678901234567890123456789m
        });
        var metadata = new Dictionary<string, object> { ["preferences"] = preferences };
        var avatar = new Avatar { Id = Guid.NewGuid(), MetaData = metadata };
        var read = SettingsManager.ReadHolonStoragePolicy(avatar);
        Assert.False(read.IsError, read.Message);
        Assert.Equal(HolonStorageMode.Materialized, read.Result.EffectivePolicy.Mode);
        Assert.Same(metadata, avatar.MetaData);
        Assert.Same(preferences, metadata["preferences"]);
        var preserved = Assert.IsType<System.Text.Json.JsonElement>(metadata["preferences"]);
        Assert.Equal(long.MaxValue, preserved.GetProperty("limit").GetInt64());
        Assert.Equal(12345.678901234567890123456789m, preserved.GetProperty("fraction").GetDecimal());
    }

    [Fact]
    public void AnAbsentPolicyDoesNotInventOrWriteSettings()
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), MetaData = new Dictionary<string, object>() };
        var result = SettingsManager.ReadHolonStoragePolicy(avatar);
        Assert.False(result.IsError, result.Message);
        Assert.Null(result.Result.EffectivePolicy);
        Assert.Empty(avatar.MetaData);
    }

    [Fact]
    public void ApplicationSelectionInheritsOnlyThisAvatarsSavedDefaultAndPreservesMetadata()
    {
        var app = Guid.NewGuid();
        var full = new HolonStoragePolicy(HolonStorageMode.Materialized, HolonReadCacheMode.None, 0, 0, true);
        var mapped = new HolonStoragePolicy(HolonStorageMode.Virtual, HolonReadCacheMode.None, 0, 0, false);
        var entries = new Dictionary<string, object> {
            ["avatar"] = HolonStoragePolicyCodec.Serialize(full),
            ["app:" + app.ToString("D")] = HolonStoragePolicyCodec.Serialize(mapped)
        };
        var preferences = new Dictionary<string, object> {
            ["holonStoragePolicies"] = entries, ["other"] = "preserved"
        };
        var metadata = new Dictionary<string, object> { ["preferences"] = preferences, ["unrelated"] = 7 };
        var avatar = new Avatar { Id = Guid.NewGuid(), MetaData = metadata };
        var selected = SettingsManager.ReadHolonStoragePolicy(avatar, app);
        Assert.False(selected.IsError, selected.Message);
        Assert.Equal(HolonStorageMode.Virtual, selected.Result.EffectivePolicy.Mode);
        var inherited = SettingsManager.ReadHolonStoragePolicy(avatar, Guid.NewGuid());
        Assert.False(inherited.IsError, inherited.Message);
        Assert.Equal(HolonStorageMode.Materialized, inherited.Result.EffectivePolicy.Mode);
        Assert.Same(metadata, avatar.MetaData);
        Assert.Same(preferences, avatar.MetaData["preferences"]);
        Assert.Same(entries, preferences["holonStoragePolicies"]);
        Assert.Equal("preserved", preferences["other"]);
        Assert.Equal(7, metadata["unrelated"]);
    }

    [Fact]
    public void CorruptPreferenceContainersAreErrorsAndAreNeverCleared()
    {
        foreach (var stored in new object[] { "not an object", 7, new Dictionary<string, object> { ["holonStoragePolicies"] = "broken" } })
        {
            var metadata = new Dictionary<string, object> { ["preferences"] = stored };
            var avatar = new Avatar { Id = Guid.NewGuid(), MetaData = metadata };
            var read = SettingsManager.ReadHolonStoragePolicy(avatar);
            Assert.True(read.IsError);
            Assert.Equal("HOLON_STORAGE_POLICY_INVALID", read.ErrorCode);
            Assert.Null(read.Result);
            Assert.Same(stored, metadata["preferences"]);
        }
    }

    [Fact]
    public void MalformedSavedPolicyAndEmptyScopesFailClosed()
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), MetaData = new Dictionary<string, object> {
            ["preferences"] = new Dictionary<string, object> {
                ["holonStoragePolicies"] = new Dictionary<string, object> { ["avatar"] = "{}" }
            }
        }};
        Assert.True(SettingsManager.ReadHolonStoragePolicy(avatar).IsError);
        Assert.True(SettingsManager.ReadHolonStoragePolicy(avatar, Guid.Empty).IsError);
        Assert.True(SettingsManager.ReadHolonStoragePolicy(null).IsError);
    }
}
