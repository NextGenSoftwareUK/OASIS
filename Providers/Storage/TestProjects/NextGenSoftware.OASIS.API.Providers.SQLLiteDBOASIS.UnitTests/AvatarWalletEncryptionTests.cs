using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass]
public sealed class AvatarWalletEncryptionTests
{
    [TestMethod]
    public async Task WalletManagerSecretsRemainEncryptedAfterSQLiteReopen()
    {
        var path = Path.GetTempFileName();
        try
        {
            await VerifyWalletEncryptionAsync(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ConnectionString);
        }
        finally
        {
            foreach (var file in new[] { path, path + "-wal", path + "-shm", path + "-journal" })
                File.Delete(file);
        }
    }

    private static async Task VerifyWalletEncryptionAsync(string connectionString)
    {
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                Security = new SecuritySettings
                {
                    OASISProviderPrivateKeys = new EncryptionSettings
                    {
                        Rijndael256EncryptionEnabled = true,
                        Rijndael256Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
                    }
                }
            }
        };
        var manager = new WalletManager(null, dna, new ProviderManager(null, dna));
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = "owned-encrypted-wallet",
            Email = "owned-encrypted-wallet@example.invalid",
            IsActive = true,
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)
        };
        const ProviderType walletType = ProviderType.SQLLiteDBOASIS;
        var created = manager.CreateWalletWithoutSaving(avatar.Id, "Owned wallet", "", walletType);
        Assert.IsFalse(created.IsError, "WalletManager must create a wallet with encrypted credentials.");
        var encrypted = created.Result;
        Assert.IsNotNull(encrypted);
        Assert.IsFalse(string.IsNullOrWhiteSpace(encrypted.PrivateKey));
        Assert.IsFalse(string.IsNullOrWhiteSpace(encrypted.SecretRecoveryPhrase));
        avatar.ProviderWallets[walletType] = new() { encrypted };

        var clear = manager.GetWalletThatPublicKeyBelongsTo(encrypted.PublicKey, walletType, avatar,
            showPrivateKey: true, showSecretWords: true);
        Assert.IsFalse(clear.IsError, "WalletManager must recover the generated credentials.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(clear.Result.PrivateKey));
        Assert.AreEqual(12, clear.Result.SecretRecoveryPhrase.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

        using (var writer = new SQLLiteDBOASIS(connectionString))
        {
            Assert.IsFalse((await writer.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await writer.SaveAvatarAsync(avatar)).IsError);
        }

        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT ProviderWalletsJson FROM Avatar";
            var raw = (string)(await command.ExecuteScalarAsync())!;
            var wallets = OasisJson.Deserialize<Dictionary<ProviderType, List<IProviderWallet>>>(raw);
            var persisted = wallets[walletType].Single();
            Assert.IsTrue(persisted.PrivateKey == encrypted.PrivateKey);
            Assert.IsTrue(persisted.SecretRecoveryPhrase == encrypted.SecretRecoveryPhrase);
            Assert.IsFalse(raw.Contains(clear.Result.PrivateKey, StringComparison.Ordinal));
            Assert.IsFalse(raw.Contains(clear.Result.SecretRecoveryPhrase, StringComparison.Ordinal));
        }

        using var reader = new SQLLiteDBOASIS(connectionString);
        Assert.IsFalse((await reader.ActivateProviderAsync()).IsError);
        var reloaded = await reader.LoadAvatarAsync(avatar.Id);
        Assert.IsFalse(reloaded.IsError);
        var stored = reloaded.Result.ProviderWallets[walletType].Single();
        var recovered = manager.GetWalletThatPublicKeyBelongsTo(stored.PublicKey, walletType, reloaded.Result,
            showPrivateKey: true, showSecretWords: true);
        Assert.IsFalse(recovered.IsError);
        Assert.IsTrue(recovered.Result.PrivateKey == clear.Result.PrivateKey);
        Assert.IsTrue(recovered.Result.SecretRecoveryPhrase == clear.Result.SecretRecoveryPhrase);
        Assert.IsTrue(stored.PrivateKey == encrypted.PrivateKey);
        Assert.IsTrue(stored.SecretRecoveryPhrase == encrypted.SecretRecoveryPhrase);
    }
}
