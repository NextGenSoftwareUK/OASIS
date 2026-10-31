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
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task OwnedSaveAuthorizesOnlyTheNodesSelectedByChildParameters(int mode)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, CreatedByAvatarId = actor };
        var excluded = new Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id, Name = "excluded foreign grandchild" };
        root.Children.Add(child);
        child.Children.Add(excluded);
        foreach (var record in new[] { root, child, excluded })
        {
            CosmicConcurrency.SetProviderRevisionId(record, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            var stored = new Holon { Id = record.Id, CreatedByAvatarId = record.Id == excluded.Id ? Guid.NewGuid() : actor };
            fixture.Provider.Setup(provider => provider.LoadHolonAsync(record.Id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        }
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = "OWNED_SCOPE_CONTROL" });

        var result = await fixture.Manager.SaveOwnedHolonAsync(root, actor,
            new CosmicWriteOptions(Guid.NewGuid()), saveChildren: mode != 0,
            recursive: mode != 2, maxChildDepth: mode == 1 ? 1 : 0);

        result.ErrorCode.Should().Be("OWNED_SCOPE_CONTROL");
        var expected = mode == 0 ? new[] { root.Id } : new[] { root.Id, child.Id };
        submitted!.Write.Conditions.Select(condition => condition.HolonId).Should().BeEquivalentTo(expected);
        fixture.Provider.Invocations.Where(call => call.Method.Name == "LoadHolonAsync")
            .Select(call => (Guid)call.Arguments[0]).Should().BeEquivalentTo(expected);
        excluded.Name.Should().Be("excluded foreign grandchild");
    }
}
