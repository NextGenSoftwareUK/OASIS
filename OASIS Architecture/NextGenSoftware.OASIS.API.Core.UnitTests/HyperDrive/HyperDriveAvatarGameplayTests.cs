using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HyperDriveAvatarGameplayTests
{
    [Fact]
    public void ConsumeInventoryUsesStableItemIdentityAndRejectsInsufficientQuantity()
    {
        var itemId = Guid.NewGuid();
        var detail = Detail(itemId, 2);

        var applied = HyperDriveAvatarGameplay.Apply(detail, new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.ConsumeInventory,
            InventoryItemId = itemId,
            Amount = 1
        });

        Assert.False(applied.IsError, applied.Message);
        Assert.Equal(1, Assert.Single(applied.Result.Inventory).Quantity);
        var rejected = HyperDriveAvatarGameplay.Apply(applied.Result, new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.ConsumeInventory,
            InventoryItemId = itemId,
            Amount = 2
        });
        Assert.True(rejected.IsError);
        Assert.Equal("AVATAR_GAMEPLAY_REJECTED", rejected.ErrorCode);
    }

    [Fact]
    public void TransferInventoryProjectsThePendingSourceViewWithoutInventingTargetState()
    {
        var itemId = Guid.NewGuid();
        var detail = Detail(itemId, 1);
        var applied = HyperDriveAvatarGameplay.Apply(detail, new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.TransferInventory,
            InventoryItemId = itemId,
            TargetAvatarId = Guid.NewGuid(),
            Amount = 1
        });

        Assert.False(applied.IsError, applied.Message);
        Assert.Empty(applied.Result.Inventory);
    }

    [Fact]
    public void TransferInventoryToClanProjectsOnlyTheRequestedQuantityFromThePendingSourceView()
    {
        var itemId = Guid.NewGuid();
        var detail = Detail(itemId, 3);
        var applied = HyperDriveAvatarGameplay.Apply(detail, new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.TransferInventoryToClan,
            InventoryItemId = itemId,
            TargetClanId = Guid.NewGuid(),
            DestinationInventoryItemId = Guid.NewGuid(),
            Amount = 2
        });

        Assert.False(applied.IsError, applied.Message);
        Assert.Equal(1, Assert.Single(applied.Result.Inventory).Quantity);
    }

    [Fact]
    public void TransferInventoryToClanRejectsInsufficientQuantityWithoutChangingTheSourceView()
    {
        var itemId = Guid.NewGuid();
        var detail = Detail(itemId, 1);
        var rejected = HyperDriveAvatarGameplay.Apply(detail, new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.TransferInventoryToClan,
            InventoryItemId = itemId,
            TargetClanId = Guid.NewGuid(),
            DestinationInventoryItemId = Guid.NewGuid(),
            Amount = 2
        });

        Assert.True(rejected.IsError);
        Assert.Equal("AVATAR_GAMEPLAY_REJECTED", rejected.ErrorCode);
        Assert.Equal(1, Assert.Single(detail.Inventory).Quantity);
    }

    [Fact]
    public void UpdateInventoryReplacesCompleteStateByStableIdentity()
    {
        var itemId = Guid.NewGuid();
        var detail = Detail(itemId, 1);
        var update = new HyperDriveInventoryItemProjection
        {
            Id = itemId, Name = "Master Key", Description = "Updated offline", Quantity = 3,
            GameSource = "Our World", ItemType = 1, ItemTypeName = "KeyItem", Rarity = "Epic",
            MaxQuantity = 5, Weight = 0.5f, IsStackable = true, IsUsable = true, IsTradeable = false,
            Value = 12.75m, ThumbnailUrl = "https://example.test/key-thumb.png",
            Properties = new Dictionary<string, string> { ["damage"] = "9" }
        };

        var applied = HyperDriveAvatarGameplay.Apply(detail, new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.UpdateInventory,
            InventoryItemId = itemId,
            InventoryUpdate = update
        });

        Assert.False(applied.IsError, applied.Message);
        var actual = Assert.Single(applied.Result.Inventory);
        Assert.Same(update, actual);
        Assert.Equal(12.75m, actual.Value);
        Assert.Equal("https://example.test/key-thumb.png", actual.ThumbnailUrl);
        Assert.Equal("9", actual.Properties["damage"]);
    }

    [Fact]
    public void UpdateInventoryRejectsMismatchedIdentity()
    {
        var itemId = Guid.NewGuid();
        var rejected = HyperDriveAvatarGameplay.Apply(Detail(itemId, 1), new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.UpdateInventory,
            InventoryItemId = itemId,
            InventoryUpdate = new HyperDriveInventoryItemProjection
            { Id = Guid.NewGuid(), Name = "Key", GameSource = "Our World", Quantity = 1 }
        });

        Assert.True(rejected.IsError);
        Assert.Equal("AVATAR_GAMEPLAY_REJECTED", rejected.ErrorCode);
    }

    [Fact]
    public void AddKarmaProjectsTotalAndImmutableHistoryDeterministically()
    {
        var occurred = new DateTime(2026, 10, 4, 12, 30, 0, DateTimeKind.Utc);
        var detail = Detail(Guid.NewGuid(), 1);
        detail.Karma = 40;

        var applied = HyperDriveAvatarGameplay.Apply(detail, new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.AddKarma,
            Amount = 100,
            KarmaSourceType = "Game",
            KarmaType = "OurWorldHelpOtherPlayer",
            KarmaSourceTitle = "Our World",
            KarmaSourceDescription = "Helped another player",
            KarmaOccurredAtUtc = occurred
        });

        Assert.False(applied.IsError, applied.Message);
        Assert.Equal(140, applied.Result.Karma);
        var history = Assert.Single(applied.Result.KarmaHistory);
        Assert.Equal(100, history.Amount);
        Assert.Equal(140, history.TotalKarma);
        Assert.Equal(occurred, history.Date);
        Assert.Equal("Our World", history.Source);
    }

    [Fact]
    public void DeductKarmaRejectsInsufficientBalanceWithoutChangingProjection()
    {
        var detail = Detail(Guid.NewGuid(), 1);
        detail.Karma = 3;

        var rejected = HyperDriveAvatarGameplay.Apply(detail, new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.DeductKarma,
            Amount = 4,
            KarmaSourceTitle = "Our World",
            KarmaOccurredAtUtc = DateTime.UtcNow
        });

        Assert.True(rejected.IsError);
        Assert.Equal(3, detail.Karma);
        Assert.Empty(detail.KarmaHistory);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void KarmaMutationRejectsNonPositiveAmount(int amount)
    {
        var rejected = HyperDriveAvatarGameplay.Apply(Detail(Guid.NewGuid(), 1),
            new HyperDriveAvatarGameplayCommand
            {
                Action = HyperDriveAvatarGameplayAction.AddKarma,
                Amount = amount,
                KarmaSourceTitle = "Our World",
                KarmaOccurredAtUtc = DateTime.UtcNow
            });

        Assert.True(rejected.IsError);
    }

    [Fact]
    public void PortableKarmaPolicyMatchesEveryCoreWeighting()
    {
        foreach (KarmaTypePositive type in Enum.GetValues(typeof(KarmaTypePositive)))
            Assert.Equal(KarmaManager.GetKarmaForType(type),
                HyperDriveKarmaPolicy.ResolveAmount(true, type.ToString()));
        foreach (KarmaTypeNegative type in Enum.GetValues(typeof(KarmaTypeNegative)))
            Assert.Equal(KarmaManager.GetKarmaForType(type),
                HyperDriveKarmaPolicy.ResolveAmount(false, type.ToString()));
    }

    [Fact]
    public void KarmaMutationRejectsUnknownPolicyVersionWithoutChangingProjection()
    {
        var detail = Detail(Guid.NewGuid(), 1);
        detail.Karma = 500;
        var result = HyperDriveAvatarGameplay.Apply(detail, new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.AddKarma,
            Amount = HyperDriveKarmaPolicy.ResolveAmount(true,
                KarmaTypePositive.OurWorldHelpOtherPlayer.ToString()),
            KarmaType = KarmaTypePositive.OurWorldHelpOtherPlayer.ToString(),
            KarmaPolicyVersion = "oasis.karma-policy.v999",
            KarmaSourceTitle = "Policy version test",
            KarmaOccurredAtUtc = DateTime.UtcNow
        });

        Assert.True(result.IsError);
        Assert.Equal("AVATAR_GAMEPLAY_REJECTED", result.ErrorCode);
        Assert.Contains("unsupported", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(500, detail.Karma);
        Assert.Empty(detail.KarmaHistory);
    }

    [Fact]
    public void KarmaPolicyVersionSurvivesPortableJsonRoundTrip()
    {
        var command = new HyperDriveAvatarGameplayCommand
        {
            Action = HyperDriveAvatarGameplayAction.AddKarma,
            KarmaPolicyVersion = HyperDriveKarmaPolicy.CurrentVersion
        };

        var restored = HyperDriveJson.Deserialize<HyperDriveAvatarGameplayCommand>(
            HyperDriveJson.Serialize(command));

        Assert.Equal(HyperDriveKarmaPolicy.CurrentVersion, restored.KarmaPolicyVersion);
    }

    [Fact]
    public void PreVersionedKarmaPayloadIsDeterministicallyAssignedVersionOne()
    {
        var restored = HyperDriveJson.Deserialize<HyperDriveAvatarGameplayCommand>(
            "{\"Action\":7,\"Amount\":100,\"KarmaType\":\"OurWorldHelpOtherPlayer\"}");

        Assert.Equal(HyperDriveKarmaPolicy.CurrentVersion, restored.KarmaPolicyVersion);
    }

    [Fact]
    public async System.Threading.Tasks.Task KarmaTransferFailsExplicitlyWithoutMutatingEitherAvatar()
    {
#pragma warning disable CS0618
        var dna = new OASISDNA();
        var result = await new KarmaManager(null, dna, new ProviderManager(null, dna)).TransferKarmaAsync(
            Guid.NewGuid(), Guid.NewGuid(), 10, "unsupported transfer");
#pragma warning restore CS0618

        Assert.True(result.IsError);
        Assert.False(result.Result);
        Assert.Equal("KARMA_TRANSFER_NOT_SUPPORTED", result.ErrorCode);
    }

    private static HyperDriveAvatarDetailProjection Detail(Guid itemId, int quantity) => new()
    {
        AvatarId = Guid.NewGuid(),
        Inventory = new[]
        {
            new HyperDriveInventoryItemProjection
            {
                Id = itemId, Name = "Key", GameSource = "Our World", Quantity = quantity
            }
        }
    };
}
