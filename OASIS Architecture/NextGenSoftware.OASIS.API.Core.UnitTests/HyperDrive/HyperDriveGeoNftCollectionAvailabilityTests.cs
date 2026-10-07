using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HyperDriveGeoNftCollectionAvailabilityTests
{
    [Fact]
    public void ExpiredCooldownBecomesLocallyAvailableWithoutChangingAuthoritativeCounters()
    {
        var id = Guid.NewGuid();
        var expiry = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var projection = new HyperDriveGeoNftCollectionAvailabilityProjection
        {
            AvatarId = Guid.NewGuid(),
            Items = new[] { new HyperDriveGeoNftCollectionAvailabilityItem
            {
                GeoNftId = id, CanCollect = false, Reason = "Waiting to respawn.",
                NextCollectAtUtc = expiry, PlayerCollectionCount = 2, GlobalCollectionCount = 7
            } }
        };

        var current = projection.GetAt(id, expiry);

        current.CanCollect.Should().BeTrue();
        current.Reason.Should().BeNull();
        current.NextCollectAtUtc.Should().BeNull();
        current.PlayerCollectionCount.Should().Be(2);
        current.GlobalCollectionCount.Should().Be(7);
    }

    [Fact]
    public void QuotaFailureCannotBeGuessedAwayByEdge()
    {
        var id = Guid.NewGuid();
        var now = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var item = new HyperDriveGeoNftCollectionAvailabilityItem
        {
            GeoNftId = id, CanCollect = false, Reason = "The global collection limit has been reached.",
            GlobalCollectionCount = 5
        };

        var current = item.At(now.AddYears(1));

        current.CanCollect.Should().BeFalse();
        current.Reason.Should().Be(item.Reason);
        current.GlobalCollectionCount.Should().Be(5);
    }

    [Fact]
    public void LookupRequiresUnambiguousIdentityAndUtcClock()
    {
        var id = Guid.NewGuid();
        var projection = new HyperDriveGeoNftCollectionAvailabilityProjection
        {
            Items = new[]
            {
                new HyperDriveGeoNftCollectionAvailabilityItem { GeoNftId = id },
                new HyperDriveGeoNftCollectionAvailabilityItem { GeoNftId = id }
            }
        };

        Action duplicate = () => projection.GetAt(id, DateTime.UtcNow);
        Action localClock = () => projection.GetAt(Guid.NewGuid(), DateTime.Now);

        duplicate.Should().Throw<InvalidOperationException>();
        localClock.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuthoritativeEvaluationCombinesEveryAvatarWithoutExposingTheirHistories()
    {
        Guid nftId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        DateTime now = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var nft = new Mock<IWeb4GeoSpatialNFT>();
        nft.SetupGet(x => x.Id).Returns(nftId);
        nft.SetupGet(x => x.PermSpawn).Returns(false);
        nft.SetupGet(x => x.AllowOtherPlayersToAlsoCollect).Returns(true);
        nft.SetupGet(x => x.GlobalSpawnQuantity).Returns(5);
        nft.SetupGet(x => x.PlayerSpawnQuantity).Returns(3);
        nft.SetupGet(x => x.RespawnDurationInSeconds).Returns(0);

        IAvatarDetail owner = Avatar(ownerId, nftId, 2, now.AddHours(-1));
        IAvatarDetail other = Avatar(Guid.NewGuid(), nftId, 1, now.AddHours(-2));

        var status = GeoNFTCollectionPolicy.EvaluateForAvatar(nft.Object, ownerId,
            new[] { owner, other }, now);

        status.CanCollect.Should().BeTrue();
        status.PlayerCollectionCount.Should().Be(2);
        status.GlobalCollectionCount.Should().Be(3);
    }

    private static IAvatarDetail Avatar(Guid avatarId, Guid nftId, long count, DateTime collectedUtc)
    {
        var avatar = new Mock<IAvatarDetail>();
        avatar.SetupGet(x => x.Id).Returns(avatarId);
        avatar.SetupGet(x => x.MetaData).Returns(new Dictionary<string, object>
        {
            ["GeoNFT.CollectionHistory.v1"] = JsonConvert.SerializeObject(new Dictionary<Guid, object>
            {
                [nftId] = new { Count = count, LastCollectedUtc = collectedUtc }
            })
        });
        return avatar.Object;
    }
}
