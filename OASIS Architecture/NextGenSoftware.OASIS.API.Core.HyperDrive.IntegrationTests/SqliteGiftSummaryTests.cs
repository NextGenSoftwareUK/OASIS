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
    public Task SendsReceivesAndOpensGiftsAgainstNativeSqlite() => WithNativeGiftsAsync(async (provider, gifts) =>
    {
        var sender = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        foreach (var id in new[] { sender, recipient })
        {
            var account = await provider.SaveAvatarAsync(new Avatar
            {
                Id = id, Username = "gift-" + id.ToString("N"), Email = id.ToString("N") + "@example.test",
                IsActive = true, CreatedDate = DateTime.UtcNow
            });
            Assert.False(account.IsError, account.Message);
            Assert.Equal(id, account.Result.Id);
        }
        Assert.True((await gifts.SendGiftAsync(Guid.Empty, recipient, GiftType.Custom)).IsError);
        Assert.True((await gifts.SendGiftAsync(sender, Guid.Empty, GiftType.Custom)).IsError);
        Assert.True((await gifts.SendGiftAsync(sender, recipient, (GiftType)int.MaxValue)).IsError);
        var missingRecipient = Guid.NewGuid();
        Assert.True((await gifts.SendGiftAsync(sender, missingRecipient, GiftType.Custom)).IsError);
        var missingInbox = await gifts.GetGiftSummaryAsync(missingRecipient);
        Assert.False(missingInbox.IsError, missingInbox.Message);
        Assert.Equal(new GiftSummary(0, 0), missingInbox.Result);
        Assert.True((await gifts.ReceiveGiftAsync(Guid.Empty, Guid.NewGuid())).IsError);
        Assert.True((await gifts.OpenGiftAsync(recipient, Guid.Empty)).IsError);
        var sent = await gifts.SendGiftAsync(sender, recipient, GiftType.Custom, "Native gift lifecycle");
        Assert.False(sent.IsError, sent.Message);
        Assert.NotEqual(Guid.Empty, sent.Result.Id);
        var giftId = sent.Result.Id;

        var pending = await gifts.GetGiftSummaryAsync(recipient);
        Assert.False(pending.IsError, pending.Message);
        Assert.Equal(new GiftSummary(1, 1), pending.Result);
        Assert.True((await gifts.OpenGiftAsync(recipient, giftId)).IsError);
        Assert.True((await gifts.ReceiveGiftAsync(sender, giftId)).IsError);

        var received = await gifts.ReceiveGiftAsync(recipient, giftId);
        Assert.False(received.IsError, received.Message);
        Assert.True(received.Result);
        var opened = await gifts.OpenGiftAsync(recipient, giftId);
        Assert.False(opened.IsError, opened.Message);
        Assert.True(opened.Result);

        var summary = await gifts.GetGiftSummaryAsync(recipient);
        Assert.False(summary.IsError, summary.Message);
        Assert.Equal(new GiftSummary(1, 0), summary.Result);
        var persisted = await provider.LoadHolonAsync(giftId, loadChildren: false);
        Assert.False(persisted.IsError, persisted.Message);
        var gift = JsonConvert.DeserializeObject<Gift>(persisted.Result.MetaData["giftPayload"].ToString());
        Assert.Equal(giftId, gift.Id);
        Assert.Equal(recipient, gift.ToAvatarId);
        Assert.True(gift.IsReceived);
        Assert.True(gift.IsOpened);

        var stats = await gifts.GetGiftStatsAsync(recipient);
        Assert.False(stats.IsError, stats.Message);
        Assert.Equal(0, Assert.IsType<int>(stats.Result["sentGifts"]));
        Assert.Equal(1, Assert.IsType<int>(stats.Result["receivedGifts"]));
        Assert.Equal(1, Assert.IsType<int>(stats.Result["openedGifts"]));

        var history = await gifts.GetGiftHistoryAsync(recipient);
        Assert.False(history.IsError, history.Message);
        Assert.Contains(history.Result, entry => entry.GiftId == giftId &&
            entry.TransactionType == GiftTransactionType.Opened);
    });

    private static async Task WithNativeGiftsAsync(Func<SQLLiteDBOASIS, GiftsManager, Task> verify)
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

            var gifts = new GiftsManager(provider, dna, runtime);
            await verify(provider, gifts);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [Fact]
    public Task CountsDurableInboxAndUnopenedGifts() => WithNativeGiftsAsync(async (provider, gifts) =>
    {
        var member = Guid.NewGuid();
        var other = Guid.NewGuid();
        async Task SeedGiftAsync(Guid from, Guid to, bool opened, bool deleted = false)
        {
            var gift = new Gift { Id = Guid.NewGuid(), FromAvatarId = from, ToAvatarId = to,
                IsOpened = opened, SentAt = DateTime.UtcNow, Message = "Owned gift fixture" };
            var saved = await provider.SaveHolonAsync(new Holon
            {
                Id = gift.Id, IsActive = !deleted,
                DeletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue,
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
    });
}
