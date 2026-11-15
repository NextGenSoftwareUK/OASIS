using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class SqliteGiftSummaryTests
{
    [Fact]
    public async Task CountsDurableInboxAndUnopenedGifts()
    {
        var folder = Directory.CreateTempSubdirectory("oasis-gift-summary-");
        try
        {
            using var provider = new SQLLiteDBOASIS($"Data Source={Path.Combine(folder.FullName, "gifts.db")};Pooling=False");
            var activated = await provider.ActivateProviderAsync();
            Assert.False(activated.IsError, activated.Message);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            var runtime = new ProviderManager(null, dna)
                { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
            runtime.RegisterProvider(provider);

            var member = Guid.NewGuid();
            var other = Guid.NewGuid();
            var gifts = new GiftsManager(provider, dna, runtime);

            async Task SeedGiftAsync(Guid from, Guid to, bool opened, bool deleted = false)
            {
                var gift = new Gift { Id = Guid.NewGuid(), FromAvatarId = from, ToAvatarId = to,
                    IsOpened = opened, SentAt = DateTime.UtcNow, Message = "Owned gift fixture" };
                var saved = await provider.SaveHolonAsync(new Holon
                {
                    Id = gift.Id, IsActive = true, IsDeleted = deleted,
                    MetaData = new Dictionary<string, object>
                    {
                        ["oasisEntityType"] = "gift",
                        ["giftFromAvatarId"] = from.ToString(),
                        ["giftToAvatarId"] = to.ToString(),
                        ["giftPayload"] = JsonConvert.SerializeObject(gift)
                    }
                }, saveChildren: false);
                Assert.False(saved.IsError, saved.Message);
            }

            async Task VerifyAsync(int total, int unopened)
            {
                var summary = await gifts.GetGiftSummaryAsync(member);
                Assert.False(summary.IsError, summary.Message);
                Assert.Equal(new GiftSummary(total, unopened), summary.Result);
                var inbox = await gifts.GetAllGiftsAsync(member);
                Assert.False(inbox.IsError, inbox.Message);
                Assert.Equal(inbox.Result.Count, summary.Result.Count);
                Assert.Equal(inbox.Result.Count(gift => !gift.IsOpened), summary.Result.UnopenedCount);
            }

            await SeedGiftAsync(member, other, opened: false);
            await VerifyAsync(0, 0);
            await SeedGiftAsync(other, member, opened: true);
            await VerifyAsync(1, 0);
            await SeedGiftAsync(other, member, opened: false);
            await SeedGiftAsync(other, member, opened: false, deleted: true);
            await VerifyAsync(2, 1);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}
