using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitNewChildIdentityStillRequiresParentAddChildren(bool includeParent)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var parent = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid(), Name = "parent" };
        parent.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(builder =>
            builder.SetGrant(HolonPermissionSet.Key(actor), HolonPermission.Write)).Encode().Result;
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(parent.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(parent) { IsLoaded = true });
        var suppliedId = Guid.NewGuid();
        var child = new Holon { Id = suppliedId, ParentHolonId = parent.Id, Name = "new explicit identity" };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(suppliedId, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound });
        var edit = (Holon)HolonChangeTracking.Clone(parent);
        HolonChangeTracking.AcceptChanges(edit, false);
        edit.Description = "allowed parent scalar edit";
        var roots = includeParent ? new IHolon[] { edit, child } : new IHolon[] { child };
        var options = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?>
            { [parent.Id] = Guid.NewGuid(), [suppliedId] = null });

        var result = await fixture.Manager.SaveOwnedHolonsAsync(roots, actor, options,
            saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("COSMIC_ADD_CHILDREN_FORBIDDEN",
            "authoritative absence makes a caller-assigned identity a new child too");
        child.Id.Should().Be(suppliedId);
        child.Version.Should().Be(0);
        fixture.Provider.Invocations.Should().NotContain(call => call.Method.Name.StartsWith("Save")
            || call.Method.Name.StartsWith("ExecuteConditional") || call.Method.Name.StartsWith("ExecuteReplicating"));
    }

    [Theory]
    [InlineData("error")]
    [InlineData("warning")]
    [InlineData("wrong-identity")]
    [InlineData("null-response")]
    public async Task UnverifiedExternalParentCannotAuthorizeChildCreation(string failure)
    {
        var fixture = Fixture(true, conditional: true);
        var parentId = Guid.NewGuid();
        var parent = new Holon { Id = parentId, CreatedByAvatarId = Guid.NewGuid() };
        OASISResult<IHolon> observed = failure switch
        {
            "error" => new OASISResult<IHolon> { IsError = true, ErrorCode = "CONTROLLED_PARENT_READ_FAILURE" },
            "warning" => new OASISResult<IHolon>(parent) { IsWarning = true },
            "wrong-identity" => new OASISResult<IHolon>(new Holon { Id = Guid.NewGuid() }),
            "null-response" => null,
            _ => throw new ArgumentException(nameof(failure))
        };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(parentId, false, true, 0, true, false, 0))
            .ReturnsAsync(observed);
        var child = new Holon { ParentHolonId = parentId, Name = "must remain unsaved" };

        var result = await fixture.Manager.SaveOwnedHolonAsync(child, Guid.NewGuid(),
            new CosmicWriteOptions(Guid.NewGuid()), providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("COSMIC_WRITE_OWNERSHIP_LOOKUP_FAILED",
            "an unverified parent lookup cannot stand in for permission to attach a child");
        child.Id.Should().BeEmpty();
        child.Version.Should().Be(0);
        fixture.Provider.Invocations.Should().NotContain(call => call.Method.Name.StartsWith("Save")
            || call.Method.Name.StartsWith("ExecuteConditional") || call.Method.Name.StartsWith("ExecuteReplicating"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExistingParentInOwnedBatchStillRequiresAddChildren(bool nested, bool selected)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var parent = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid(), Name = "parent" };
        parent.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(builder =>
            builder.SetGrant(HolonPermissionSet.Key(actor), HolonPermission.Write)).Encode().Result;
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(parent.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(parent) { IsLoaded = true });
        var edit = (Holon)HolonChangeTracking.Clone(parent);
        HolonChangeTracking.AcceptChanges(edit, false);
        edit.Description = "allowed scalar edit";
        var child = new Holon { ParentHolonId = parent.Id, Name = "must not be created" };
        if (nested) edit.Children.Add(child);
        var roots = nested ? new IHolon[] { edit } : new IHolon[] { edit, child };
        var options = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [parent.Id] = Guid.NewGuid() });

        var result = selected
            ? await fixture.Manager.SaveOwnedHolonsAsync(roots, actor, options,
                new Dictionary<Guid, IReadOnlyList<string>> { [parent.Id] = new[] { "/Description" } },
                providerType: ProviderType.SQLLiteDBOASIS)
            : await fixture.Manager.SaveOwnedHolonsAsync(roots, actor, options, providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("COSMIC_ADD_CHILDREN_FORBIDDEN",
            "including a stored parent in a graph must not grant permission to attach children");
        child.Id.Should().BeEmpty();
        child.Version.Should().Be(0);
        parent.Description.Should().NotBe("allowed scalar edit");
        edit.Description.Should().Be("allowed scalar edit");
        fixture.Provider.Invocations.Should().NotContain(call => call.Method.Name.StartsWith("Save")
            || call.Method.Name.StartsWith("ExecuteConditional") || call.Method.Name.StartsWith("ExecuteReplicating"));
    }
}
