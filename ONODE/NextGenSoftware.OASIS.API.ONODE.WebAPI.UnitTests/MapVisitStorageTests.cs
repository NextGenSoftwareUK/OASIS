using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class MapVisitStorageTests
{
    [Fact]
    public async Task NativeMapVisitSurvivesNewManagersAndKeepsAvatarHistoryPrivate()
    {
        // VisitLocation also updates the existing competition service. Run only in the isolated CI worker.
        if (Environment.GetEnvironmentVariable("OASIS_NATIVE_MAP_VISITS") != "owned-ci")
            return;

        var folder = Directory.CreateTempSubdirectory("oasis-map-visits-");
        try
        {
            var connection = $"Data Source={Path.Combine(folder.FullName, "visits.db")};Pooling=False";
            var avatar = Guid.NewGuid();
            var otherAvatar = Guid.NewGuid();
            var location = Guid.NewGuid();
            var dna = new OASISDNA
            {
                OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                {
                    HyperDriveMode = HyperDriveModes.V2,
                    StorageProviders = new StorageProviderSettings()
                }
            };

            using (var writer = new SQLLiteDBOASIS(connection))
            {
                try
                {
                    var activation = await writer.ActivateProviderAsync();
                    Assert.False(activation.IsError, activation.Message);
                    var manager = ForAvatar(writer, avatar, dna);
                    var empty = await manager.GetMapStatsAsync(avatar);
                    Assert.False(empty.IsError, empty.Message);
                    Assert.Equal(0, Assert.IsType<int>(empty.Result["totalVisits"]));

                    var visit = await manager.VisitLocationAsync(avatar, location, "Exploration");
                    Assert.False(visit.IsError, visit.Message);
                    Assert.True(visit.Result);
                }
                finally
                {
                    UnregisterOwnedProvider(writer);
                }
            }

            using var reader = new SQLLiteDBOASIS(connection);
            try
            {
                var readActivation = await reader.ActivateProviderAsync();
                Assert.False(readActivation.IsError, readActivation.Message);
                var reopened = ForAvatar(reader, avatar, dna);
                var history = await reopened.GetVisitHistoryAsync(avatar);
                Assert.False(history.IsError, history.Message);
                var stored = Assert.Single(history.Result);
                Assert.Equal(location, stored.LocationId);
                Assert.Equal(avatar, stored.AvatarId);
                Assert.Equal("Exploration", stored.Purpose);
                Assert.NotEqual(default(DateTime), stored.VisitedAt);

                var stats = await reopened.GetMapStatsAsync(avatar);
                Assert.False(stats.IsError, stats.Message);
                Assert.Equal(1, Assert.IsType<int>(stats.Result["totalVisits"]));
                var unrelated = await ForAvatar(reader, otherAvatar, dna).GetVisitHistoryAsync(otherAvatar);
                Assert.False(unrelated.IsError, unrelated.Message);
                Assert.Empty(unrelated.Result);
            }
            finally
            {
                UnregisterOwnedProvider(reader);
            }
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    private static void UnregisterOwnedProvider(SQLLiteDBOASIS provider)
    {
        var shared = ProviderManager.Instance;
        if (ReferenceEquals(shared.GetStorageProvider(ProviderType.SQLLiteDBOASIS), provider))
            shared.UnRegisterProvider(provider);
    }

    private static MapManager ForAvatar(SQLLiteDBOASIS provider, Guid avatar, OASISDNA dna)
    {
        var providers = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
        providers.RegisterProvider(provider);
        return new MapManager(provider, avatar, dna) { Data = new HolonManager(provider, dna, providers) };
    }
}
