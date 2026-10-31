using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using TronNet;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class TronKarmaRoutingTests
{
    [Fact]
    public async Task RoutedKarmaCommandsCommitOnceAndReplicateLedgerAndProjection()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Use-ReusableTronRuntime.ps1"))) directory = directory.Parent;
        directory.Should().NotBeNull();
        var root = Path.Combine(directory!.FullName, "TestResults/HyperDriveTron"); Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var key = new string('0', 63) + "1"; // Public local fixture only.
        var contract = (await File.ReadAllTextAsync(Path.Combine(directory.FullName, "TestResults/TronRuntime/local-store-address.txt"))).Trim();
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"),
            "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f", contractFeeLimitSun: 1000000000);
        using var backend = new TronPrivateStorageBackend(runtime.NativeTransfers, new TronECKey(key, TronNetwork.MainNet).GetPublicAddress(), contract,
            () => key, () => SHA256.HashData(Encoding.UTF8.GetBytes("public reusable local TRON encryption fixture")));
        var tron = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:9090", nativeTransfers: runtime.NativeTransfers, storageBackend: backend);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var dna = new OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
        var providers = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true, IsAutoFailOverEnabled = true, IsAutoLoadBalanceEnabled = false };
        providers.RegisterProvider(local); providers.RegisterProvider(tron);
        providers.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.TRONOASIS) });
        providers.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.TRONOASIS), new EnumValue<ProviderType>(ProviderType.LocalFileOASIS) });
        var constructor = typeof(KarmaManager).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(value => value.GetParameters().Length == 3);
        var karma = (KarmaManager)constructor.Invoke(new object[] { local, dna, providers });
        var instance = typeof(KarmaManager).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)!;
        var previous = instance.GetValue(null);
        var avatar = new Avatar { Id = Guid.Parse("fc3f2a29-5cdb-401c-8311-35685443125d"), Username = "tron-karma-owner", Email = "tron-karma-owner@example.invalid" };
        var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email };
        var settingsId = typeof(HolonManager).GetMethod("GetSettingsHolonId", BindingFlags.NonPublic | BindingFlags.Static)!;
        Guid SettingsId(Guid id, string category) => (Guid)settingsId.Invoke(null, new object[] { id, category })!;
        var ledgerId = SettingsId(avatar.Id, "karma");
        var owned = new List<Guid> { ledgerId, SettingsId(avatar.Id, "leaderboard") };
        var competition = typeof(KarmaManager).GetField("_competitionManager", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(karma)!;
        var categoryMethod = typeof(CompetitionManager).GetMethod("GetLeaderboardCategory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        foreach (var season in new[] { SeasonType.Daily, SeasonType.Weekly, SeasonType.Monthly, SeasonType.Quarterly, SeasonType.Yearly })
            owned.Add(SettingsId(Guid.Empty, (string)categoryMethod.Invoke(competition, new object[] { CompetitionType.Karma, season })!));
        var ownsRecords = false;
        try
        {
            (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await tron.ActivateProviderAsync()).IsError.Should().BeFalse();
            providers.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
            if (Environment.GetEnvironmentVariable("OASIS_TRON_KARMA_RECONCILE_OWNED_FIXTURE") == "1")
            {
                // Explicit recovery of this fixture's interrupted run only, never automatic overwrite.
                foreach (var provider in new IOASISStorageProvider[] { local, tron })
                foreach (var id in owned)
                {
                    var recovered = await provider.LoadHolonAsync(id, loadChildren: false);
                    if (recovered.Result == null) { (recovered.IsError && recovered.ErrorCode != OASISResultErrorCodes.RecordNotFound).Should().BeFalse(recovered.Message); continue; }
                    var record = recovered.Result;
                    record.Name.Should().StartWith("settings_avatar_");
                    (record.CreatedByAvatarId == avatar.Id || record.CreatedByAvatarId == Guid.Empty).Should().BeTrue();
                    if (record.CreatedByAvatarId == Guid.Empty)
                    {
                        record.Name.Should().StartWith("settings_avatar_" + Guid.Empty + "_leaderboard:Karma:");
                        if (record.MetaData.TryGetValue("entries", out var entries))
                            Newtonsoft.Json.JsonConvert.DeserializeObject<List<LeaderboardEntry>>(entries.ToString())!
                                .All(value => value.AvatarId == avatar.Id).Should().BeTrue("preserve all non-fixture leaderboard participants");
                    }
                    else if (record.MetaData.TryGetValue("history", out var history))
                        ((IEnumerable<Dictionary<string, object>>)history).All(value => value["avatarId"].ToString() == avatar.Id.ToString()).Should().BeTrue();
                    (await provider.DeleteHolonAsync(id)).IsError.Should().BeFalse();
                }
                foreach (var provider in new IOASISStorageProvider[] { local, tron })
                {
                    var prior = await provider.LoadAvatarDetailAsync(avatar.Id);
                    if (prior.Result != null) { prior.Result.Username.Should().Be(avatar.Username); (await provider.DeleteAvatarDetailAsync(avatar.Id, false)).IsError.Should().BeFalse(); }
                    var priorIdentity = await provider.LoadAvatarAsync(avatar.Id);
                    if (priorIdentity.Result != null) { priorIdentity.Result.Username.Should().Be(avatar.Username); (await provider.DeleteAvatarAsync(avatar.Id, false)).IsError.Should().BeFalse(); }
                }
            }
            foreach (var provider in new IOASISStorageProvider[] { local, tron })
            {
                foreach (var id in owned)
                {
                    var loaded = await provider.LoadHolonAsync(id, loadChildren: false);
                    loaded.Result.Should().BeNull("preserve pre-existing leaderboard/ledger records instead of overwriting them");
                    (loaded.IsError && loaded.ErrorCode != OASISResultErrorCodes.RecordNotFound).Should().BeFalse(loaded.Message);
                }
                var priorAvatar = await provider.LoadAvatarAsync(avatar.Id);
                priorAvatar.Result.Should().BeNull();
                (priorAvatar.IsError && priorAvatar.ErrorCode != OASISResultErrorCodes.RecordNotFound).Should().BeFalse(priorAvatar.Message);
                var priorDetail = await provider.LoadAvatarDetailAsync(avatar.Id);
                priorDetail.Result.Should().BeNull();
                (priorDetail.IsError && priorDetail.ErrorCode != OASISResultErrorCodes.RecordNotFound).Should().BeFalse(priorDetail.Message);
            }
            ownsRecords = true; instance.SetValue(null, karma);
            var router = new OASISHyperDrive(providers);
            (await router.RouteRequestAsync<IAvatar>(new StorageOperationRequest { Operation = "SaveAvatar", Payload = avatar, PreferredProvider = ProviderType.LocalFileOASIS })).IsError.Should().BeFalse();
            (await router.RouteRequestAsync<IAvatarDetail>(new StorageOperationRequest { Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = ProviderType.LocalFileOASIS })).IsError.Should().BeFalse();
            var positive = KarmaTypePositive.ContributingToTheOASISWithCode;
            var negative = KarmaTypeNegative.BeingSelfish;
            var added = await router.RouteRequestAsync<KarmaAkashicRecord>(new StorageOperationRequest { Operation = "AddKarmaToAvatar", Payload = detail,
                PositiveKarmaType = positive, KarmaSourceType = KarmaSourceType.Platform, KarmaSourceTitle = "public local fixture", PreferredProvider = ProviderType.TRONOASIS });
            added.IsError.Should().BeFalse(added.Message);
            added.IsWarning.Should().BeFalse(added.Message + " " + string.Join(";", added.InnerMessages ?? new List<string>()));
            var expected = KarmaManager.GetKarmaForType(positive);
            foreach (var provider in new IOASISStorageProvider[] { local, tron })
            {
                var projection = await provider.LoadAvatarDetailAsync(avatar.Id);
                projection.IsError.Should().BeFalse(provider.ProviderType.Value + ": " + projection.Message);
                projection.Result.Should().NotBeNull(provider.ProviderType.Value + ": " + projection.Message);
                projection.Result.Karma.Should().Be(expected);
                var ledger = (await provider.LoadHolonAsync(ledgerId, loadChildren: false)).Result;
                Convert.ToInt64(ledger.MetaData["totalKarma"]).Should().Be(expected);
                Convert.ToInt32(ledger.MetaData["karmaTransactions"]).Should().Be(1);
            }
            var removed = router.RouteRequest<KarmaAkashicRecord>(new StorageOperationRequest { Operation = "RemoveKarmaFromAvatar", Payload = detail,
                NegativeKarmaType = negative, KarmaSourceType = KarmaSourceType.Platform, KarmaSourceTitle = "public local fixture", PreferredProvider = ProviderType.TRONOASIS });
            removed.IsError.Should().BeFalse(removed.Message);
            removed.IsWarning.Should().BeFalse(removed.Message + " " + string.Join(";", removed.InnerMessages ?? new List<string>()));
            expected -= KarmaManager.GetKarmaForType(negative);
            foreach (var provider in new IOASISStorageProvider[] { local, tron })
            {
                (await provider.LoadAvatarDetailAsync(avatar.Id)).Result.Karma.Should().Be(expected);
                var ledger = (await provider.LoadHolonAsync(ledgerId, loadChildren: false)).Result;
                Convert.ToInt64(ledger.MetaData["totalKarma"]).Should().Be(expected);
                Convert.ToInt32(ledger.MetaData["karmaTransactions"]).Should().Be(2);
                foreach (var projected in new[] { provider.LoadAvatarDetailByUsername(avatar.Username),
                    await provider.LoadAvatarDetailByUsernameAsync(avatar.Username), provider.LoadAvatarDetailByEmail(avatar.Email),
                    await provider.LoadAvatarDetailByEmailAsync(avatar.Email) })
                {
                    projected.IsError.Should().BeFalse(projected.Message);
                    projected.Result.Karma.Should().Be(expected); projected.Result.KarmaAkashicRecords.Count.Should().Be(2);
                }
                foreach (var all in new[] { provider.LoadAllAvatarDetails(), await provider.LoadAllAvatarDetailsAsync() })
                { all.IsError.Should().BeFalse(all.Message); all.Result.Single(value => value.Id == avatar.Id).KarmaAkashicRecords.Count.Should().Be(2); }
            }
            (await karma.GetKarmaHistoryAsync(avatar.Id)).Result.Count.Should().Be(2);
            var syncAdd = router.RouteRequest<KarmaAkashicRecord>(new StorageOperationRequest { Operation = "AddKarmaToAvatar", Payload = detail,
                PositiveKarmaType = positive, KarmaSourceType = KarmaSourceType.Platform, PreferredProvider = ProviderType.TRONOASIS });
            syncAdd.IsError.Should().BeFalse(syncAdd.Message); syncAdd.IsWarning.Should().BeFalse(syncAdd.Message);
            var asyncRemove = await router.RouteRequestAsync<KarmaAkashicRecord>(new StorageOperationRequest { Operation = "RemoveKarmaFromAvatar", Payload = detail,
                NegativeKarmaType = negative, KarmaSourceType = KarmaSourceType.Platform, PreferredProvider = ProviderType.TRONOASIS });
            asyncRemove.IsError.Should().BeFalse(asyncRemove.Message); asyncRemove.IsWarning.Should().BeFalse(asyncRemove.Message);
            expected *= 2;
            foreach (var provider in new IOASISStorageProvider[] { local, tron })
            {
                var ledger = (await provider.LoadHolonAsync(ledgerId, loadChildren: false)).Result;
                Convert.ToInt64(ledger.MetaData["totalKarma"]).Should().Be(expected);
                Convert.ToInt32(ledger.MetaData["karmaTransactions"]).Should().Be(4);
                var projected = (await provider.LoadAvatarDetailAsync(avatar.Id)).Result;
                projected.Karma.Should().Be(expected); projected.KarmaAkashicRecords.Count.Should().Be(4);
                foreach (var id in owned.Skip(2))
                {
                    var board = await provider.LoadHolonAsync(id, loadChildren: false);
                    board.IsError.Should().BeFalse(board.Message);
                    var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LeaderboardEntry>>(board.Result.MetaData["entries"].ToString())!;
                    entries.Single(value => value.AvatarId == avatar.Id).Score.Should().Be(expected);
                }
                var stats = (await provider.LoadHolonAsync(owned[1], loadChildren: false)).Result;
                Convert.ToInt64(stats.MetaData["totalScore"]).Should().Be(expected);
            }
            (await karma.GetKarmaHistoryAsync(avatar.Id)).Result.Count.Should().Be(4);
        }
        finally
        {
            instance.SetValue(null, previous);
            if (ownsRecords) foreach (var provider in new IOASISStorageProvider[] { local, tron })
            {
                foreach (var id in owned) if ((await provider.LoadHolonAsync(id, loadChildren: false)).Result != null)
                    (await provider.DeleteHolonAsync(id)).IsError.Should().BeFalse();
                if ((await provider.LoadAvatarAsync(avatar.Id)).Result != null) (await provider.DeleteAvatarAsync(avatar.Id, false)).IsError.Should().BeFalse();
            }
            await local.DeActivateProviderAsync(); await tron.DeActivateProviderAsync();
        }
    }
}
