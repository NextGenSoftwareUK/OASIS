using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class SqliteMessageSummaryTests
{
    [Fact]
    public async Task CountsAllMessagesAndOnlyUnreadIncomingMessages()
    {
        var folder = Directory.CreateTempSubdirectory("oasis-message-summary-");
        try
        {
            using var provider = new SQLLiteDBOASIS($"Data Source={Path.Combine(folder.FullName, "messages.db")};Pooling=False");
            var activated = await provider.ActivateProviderAsync();
            Assert.False(activated.IsError, activated.Message);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            var runtime = new ProviderManager(null, dna)
                { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
            runtime.RegisterProvider(provider);

            var avatar = Guid.NewGuid();
            var peer = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var records = Enumerable.Range(0, 51)
                .Select(i => new Message { FromAvatarId = avatar, ToAvatarId = peer, Timestamp = now.AddSeconds(i) })
                .Append(new Message { FromAvatarId = peer, ToAvatarId = avatar, Timestamp = now.AddDays(-1) })
                .Append(new Message { FromAvatarId = avatar, ToAvatarId = avatar, Timestamp = now.AddDays(-2), IsRead = true })
                .Append(new Message { FromAvatarId = peer, ToAvatarId = stranger, Timestamp = now });

            foreach (var message in records)
            {
                message.Id = Guid.NewGuid();
                message.Content = "Owned message fixture";
                var saved = await provider.SaveHolonAsync(new Holon
                {
                    Id = message.Id,
                    IsActive = true,
                    MetaData = new Dictionary<string, object>
                    {
                        ["oasisEntityType"] = "message",
                        ["messageFromAvatarId"] = message.FromAvatarId.ToString(),
                        ["messageToAvatarId"] = message.ToAvatarId.ToString(),
                        ["messagePayload"] = JsonConvert.SerializeObject(message)
                    }
                }, saveChildren: false);
                Assert.False(saved.IsError, saved.Message);
            }

            var manager = new MessagingManager(provider, dna, runtime);
            var page = await manager.GetMessagesAsync(avatar);
            Assert.False(page.IsError, page.Message);
            Assert.Equal(50, page.Result.Count);
            Assert.DoesNotContain(page.Result, message => message.ToAvatarId == avatar);
            var summary = await manager.GetMessageSummaryAsync(avatar);
            Assert.False(summary.IsError, summary.Message);
            Assert.Equal(53, summary.Result.Count);
            Assert.Equal(1, summary.Result.UnreadCount);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}
