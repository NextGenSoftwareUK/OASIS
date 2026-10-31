using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharingConditionalFailureRetainsEvidenceAndNeverAcknowledgesSuccess(bool revoke)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var stored = new Holon { Id = id, CreatedByAvatarId = actor };
        CosmicConcurrency.SetProviderRevisionId(stored, Core.Enums.ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        HolonAccess.RecordShares(stored, revoke ? new[] { recipient } : Array.Empty<Guid>());
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true, ErrorCount = 3, ErrorCode = "CONTROLLED_WRITE_FAILURE",
                DetailedMessage = "Original conditional diagnostic.",
                MetaData = new Dictionary<string, string> { ["evidence"] = "original-conditional-attempt" }
            });
        var caller = new Mock<IAvatar>();
        caller.SetupGet(value => value.Id).Returns(actor);

        var result = revoke
            ? await HolonAccess.RevokeAsync(fixture.Manager, id, new[] { recipient }, caller.Object)
            : await HolonAccess.ShareAsync(fixture.Manager, id, new[] { recipient }, caller.Object);

        result.IsError.Should().BeTrue();
        result.IsSaved.Should().BeFalse();
        result.Result.Should().BeFalse();
        result.ErrorCount.Should().Be(3);
        result.ErrorCode.Should().Be("CONTROLLED_WRITE_FAILURE");
        result.DetailedMessage.Should().Be("Original conditional diagnostic.");
        result.MetaData.Should().ContainKey("evidence").WhoseValue.Should().Be("original-conditional-attempt");
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PinnedAuthorityFailurePreservesProviderEvidenceThroughSharing(bool share)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var failure = new OASISResult<IHolon>
        {
            IsError = true,
            ErrorCount = 3,
            ErrorCode = "PRIMARY_AUTHORITY_UNAVAILABLE",
            Message = "Primary authority rejected the observation.",
            DetailedMessage = "The original provider diagnostic.",
            Exception = new InvalidOperationException("controlled authority failure"),
            MetaData = new Dictionary<string, string> { ["evidence"] = "original-provider-attempt" }
        };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(failure);

        if (share)
        {
            var caller = new Mock<IAvatar>();
            caller.SetupGet(value => value.Id).Returns(actor);

            var result = await HolonAccess.ShareAsync(fixture.Manager, id, new[] { Guid.NewGuid() }, caller.Object);

            result.IsError.Should().BeTrue();
            result.ErrorCount.Should().Be(3);
            result.ErrorCode.Should().Be(failure.ErrorCode);
            result.DetailedMessage.Should().Be(failure.DetailedMessage);
            result.Exception.Should().BeSameAs(failure.Exception);
            result.MetaData.Should().ContainKey("evidence").WhoseValue.Should().Be("original-provider-attempt");
        }
        else
        {
            var result = await fixture.Manager.LoadHolonForWriteAsync(id, actor);

            result.IsError.Should().BeTrue();
            result.ErrorCount.Should().Be(3);
            result.ErrorCode.Should().Be(failure.ErrorCode);
            result.DetailedMessage.Should().Be(failure.DetailedMessage);
            result.Exception.Should().BeSameAs(failure.Exception);
            result.MetaData.Should().ContainKey("evidence").WhoseValue.Should().Be("original-provider-attempt");
        }

        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }
}
