using System.Globalization;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaTestnetStorageTests
{
    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Funded testnet storage setting {name} is required; no skipped or mock evidence.");
        return value;
    }

    [TestMethod]
    public async Task FundedTestnetEncryptedCrudSurvivesProviderRestartAndDeletesOwnedRecords()
    {
        var key = Required("OASIS_HEDERA_TEST_PRIVATE_KEY");
        var encryptionKey = Required("OASIS_HEDERA_TEST_STORAGE_ENCRYPTION_KEY");
        var configuration = new HederaStorageConfiguration {
            ContractId = Required("OASIS_HEDERA_TEST_STORAGE_CONTRACT_ID"),
            OperatorAccountId = Required("OASIS_HEDERA_TEST_ACCOUNT_ID"),
            SigningKeyType = "der", SigningKeyResolver = () => key,
            EncryptionKeyResolver = () => Convert.FromBase64String(encryptionKey),
            GasLimit = long.Parse(Required("OASIS_HEDERA_TEST_STORAGE_GAS_LIMIT"), CultureInfo.InvariantCulture),
            MaxFeeTinybars = long.Parse(Required("OASIS_HEDERA_TEST_STORAGE_MAX_FEE_TINYBARS"), CultureInfo.InvariantCulture)
        };
        HashgraphOASIS Create() => new("https://testnet.mirrornode.hedera.com", "testnet", "296", storageConfiguration: configuration);
        using var storage = new ConfiguredHederaStorageBackend("testnet", null, configuration);
        var provider = Create();
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        var suffix = Guid.NewGuid().ToString("N");
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "owned-" + suffix,
            Email = suffix + "@oasis-test.invalid", Password = "test-only-" + suffix };
        var holon = new Holon { Id = Guid.NewGuid(), Name = "owned encrypted test " + suffix };
        var evidenceRoot = Path.GetFullPath(Required("OASIS_HEDERA_TEST_STORAGE_ROOT"));
        Directory.CreateDirectory(evidenceRoot);
        var intentPath = Path.Combine(evidenceRoot, "storage-pending.json");
        // CreateNew fences subsequent funded runs until an interrupted/failed run
        // has been reconciled. Never lose the ownership IDs of partial saves.
        using (var intent = new FileStream(intentPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(intent, new { network = "testnet", configuration.ContractId,
                configuration.OperatorAccountId, avatarId = avatar.Id, holonId = holon.Id,
                avatar.Username, avatar.Email, encryptionKeyLabel = "oasis-hedera-testnet-storage-fixture-v1" });
            intent.Flush(flushToDisk: true);
        }
        var avatarSaved = false;
        var holonSaved = false;
        Exception? primaryFailure = null;
        try
        {
            var saved = await provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);
            avatarSaved = true;
            var detail = await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Karma = 42 });
            Assert.IsFalse(detail.IsError, detail.Message);
            var savedHolon = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsFalse(savedHolon.IsError, savedHolon.Message);
            holonSaved = true;
            var restarted = Create();
            var loaded = await restarted.LoadAvatarByEmailAsync(avatar.Email);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(avatar.Password, loaded.Result.Password);
            var loadedDetail = await restarted.LoadAvatarDetailAsync(avatar.Id);
            Assert.IsFalse(loadedDetail.IsError, loadedDetail.Message);
            Assert.AreEqual(42L, Convert.ToInt64(loadedDetail.Result.Karma));
            var loadedHolon = await restarted.LoadHolonAsync(holon.Id, loadChildren: false);
            Assert.IsFalse(loadedHolon.IsError, loadedHolon.Message);
            Assert.AreEqual(holon.Name, loadedHolon.Result.Name);
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally
        {
            try
            {
            // Remove only records whose writes established success. An uncertain write
            // must be reconciled by its original transaction ID, never blindly retried.
            if (holonSaved)
            {
                var deleted = await provider.DeleteHolonAsync(holon.Id);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                Assert.IsNull(await storage.GetAsync($"oasis/holon/{holon.Id:N}"));
            }
            if (avatarSaved)
            {
                var deleted = await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                Assert.IsNull(await storage.GetAsync($"oasis/avatar/{avatar.Id:N}"));
                Assert.IsNull(await storage.GetAsync($"oasis/avatardetail/{avatar.Id:N}"));
            }
                if (primaryFailure == null)
                    File.Delete(intentPath);
            }
            catch (Exception cleanupFailure)
            {
                if (primaryFailure != null)
                    throw new AggregateException("Funded storage verification and cleanup both failed; retain ownership intent.", primaryFailure, cleanupFailure);
                throw;
            }
        }
    }
}
