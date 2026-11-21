using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class HolonStoragePolicyTests_Operations
{
    [Theory]
    [InlineData(HolonStorageMode.Materialized, true, false, true)]
    [InlineData(HolonStorageMode.Virtual, false, true, false)]
    [InlineData(HolonStorageMode.Hybrid, false, true, false)]
    [InlineData(HolonStorageMode.Hybrid, true, true, true)]
    public void NewPlacementOperationsHonorTheSelectedMode(HolonStorageMode mode, bool save,
        bool mappingsAllowed, bool materializationAllowed)
    {
        var policy = new HolonStoragePolicy(mode, HolonReadCacheMode.None, 0, 0, save);
        Assert.Equal(mappingsAllowed, !HolonStoragePolicyAdmission.PrepareVirtualMapping(policy).IsError);
        Assert.Equal(materializationAllowed, !HolonStoragePolicyAdmission.Materialize(policy).IsError);
    }

    [Fact]
    public void AnAbsentOverridePreservesExistingExplicitOperations()
    {
        Assert.True(HolonStoragePolicyAdmission.PrepareVirtualMapping(null).Result);
        Assert.True(HolonStoragePolicyAdmission.Materialize(null).Result);
    }

    [Fact]
    public void AnUnadmittedCacheIsNeverSimulatedByAnUncachedOperation()
    {
        var policy = new HolonStoragePolicy(HolonStorageMode.Hybrid, HolonReadCacheMode.Memory, 1024, 60, true);
        Assert.Equal("HOLON_STORAGE_POLICY_UNSUPPORTED", HolonStoragePolicyAdmission.PrepareVirtualMapping(policy).ErrorCode);
        Assert.Equal("HOLON_STORAGE_POLICY_UNSUPPORTED", HolonStoragePolicyAdmission.Materialize(policy).ErrorCode);
    }

    [Fact]
    public void ApplicationScopeSurvivesTheMappingDefinitionRoundTripWithoutChangingBindings()
    {
        var owner = Guid.NewGuid();
        var app = Guid.NewGuid();
        var fields = new Dictionary<string, string> { ["title"] = "name" };
        var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, owner, ProviderType.MongoDBOASIS,
            "products", "catalog", "owned-source", fields, app);
        var definition = mapping.ToDefinitionHolon();
        definition.Version = 1;
        var restored = VirtualHolonMapping.FromDefinitionHolon(owner, definition, "owned-source");
        Assert.Equal(app, restored.ApplicationId);
        Assert.Equal("name", restored.Fields["title"]);
        Assert.Equal(mapping.Id, restored.Id);
        Assert.Throws<ArgumentException>(() => VirtualHolonMapping.GetDefinitionApplicationId(Guid.NewGuid(), definition));
    }

    [Fact]
    public void LegacyMappingsRetainAvatarScopeAndMalformedApplicationBindingsAreErrors()
    {
        var owner = Guid.NewGuid();
        var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, owner, ProviderType.MongoDBOASIS,
            "products", "catalog", "owned-source", new Dictionary<string, string> { ["title"] = "name" });
        var definition = mapping.ToDefinitionHolon();
        definition.Version = 1;
        Assert.Null(VirtualHolonMapping.FromDefinitionHolon(owner, definition, "owned-source").ApplicationId);
        definition.MetaData["oasis.virtual.applicationId"] = Guid.Empty.ToString("D");
        Assert.Throws<ArgumentException>(() => VirtualHolonMapping.FromDefinitionHolon(owner, definition, "owned-source"));
    }
}
