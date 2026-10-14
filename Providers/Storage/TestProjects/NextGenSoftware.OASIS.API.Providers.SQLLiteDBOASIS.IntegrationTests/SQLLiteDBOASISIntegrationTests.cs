using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public class SQLLiteDBOASISIntegrationTests : IDisposable
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task WalletSelectorsPreserveTerminalIdentityAmbiguity(bool synchronous, bool username)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var version in new[] { 3, 4 })
            (await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Username = "wallet-collision",
                Email = "wallet-collision@test.local", Version = version,
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) })).IsError.Should().BeFalse();
        var saved = username
            ? synchronous ? provider.SaveProviderWalletsForAvatarByUsername("wallet-collision", new()) : await provider.SaveProviderWalletsForAvatarByUsernameAsync("wallet-collision", new())
            : synchronous ? provider.SaveProviderWalletsForAvatarByEmail("wallet-collision@test.local", new()) : await provider.SaveProviderWalletsForAvatarByEmailAsync("wallet-collision@test.local", new());
        saved.IsError.Should().BeTrue();
        saved.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
        saved.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
        var loaded = username
            ? synchronous ? provider.LoadProviderWalletsForAvatarByUsername("wallet-collision") : await provider.LoadProviderWalletsForAvatarByUsernameAsync("wallet-collision")
            : synchronous ? provider.LoadProviderWalletsForAvatarByEmail("wallet-collision@test.local") : await provider.LoadProviderWalletsForAvatarByEmailAsync("wallet-collision@test.local");
        loaded.IsError.Should().BeTrue();
        loaded.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
        loaded.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
        using var control = new DataContext(TestConnectionString);
        control.AvatarWalletStores.Should().BeEmpty();
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("{\"SolanaOASIS\":null}")]
    [InlineData("{\"SolanaOASIS\":[null]}")]
    [InlineData("{\"UnknownProvider\":[]}")]
    [InlineData("{\"SolanaOASIS\":[{}]}")]
    public async Task CorruptStoredWalletPayloadCannotBecomeSuccessfulEmptyRead(string payload)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "corrupt-wallet-owner",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.AvatarWalletStores.Add(new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.AvatarWalletStore
            { AvatarId = avatar.Id.ToString(), WalletsJson = payload });
        control.SaveChanges();
        var loaded = await provider.LoadProviderWalletsForAvatarByIdAsync(avatar.Id);
        loaded.IsError.Should().BeTrue();
        loaded.Exception.Should().BeAssignableTo<Newtonsoft.Json.JsonException>();
        loaded.Result.Should().BeNull();
        control.AvatarWalletStores.Single().WalletsJson.Should().Be(payload);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("identity")]
    [InlineData("duplicate")]
    [InlineData("provider")]
    public async Task InvalidExplicitWalletIdentityIsRejectedBeforeStorage(string invalid)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "identity-wallet-owner",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        var wallet = new ProviderWallet { AvatarId = invalid == "owner" ? Guid.NewGuid() : avatar.Id,
            WalletId = invalid == "identity" ? Guid.Empty : Guid.NewGuid(),
            ProviderType = invalid == "provider" ? ProviderType.MongoDBOASIS : ProviderType.SolanaOASIS };
        var list = new List<IProviderWallet> { wallet };
        if (invalid == "duplicate") list.Add(wallet);
        var rejected = await provider.SaveProviderWalletsForAvatarByIdAsync(avatar.Id,
            new() { [ProviderType.SolanaOASIS] = list });
        rejected.IsError.Should().BeTrue();
        rejected.Result.Should().BeFalse();
        rejected.Exception.Should().BeOfType<ArgumentException>();
        using var control = new DataContext(TestConnectionString);
        control.AvatarWalletStores.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task InvalidBatchOwnerCannotPartiallyOverwriteValidOwner(bool synchronous, bool emptyOwner)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "valid-batch-owner",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        IProviderWallet Wallet(Guid owner, string address) => new ProviderWallet
            { WalletId = Guid.NewGuid(), AvatarId = owner, WalletAddress = address, ProviderType = ProviderType.SolanaOASIS };
        (await provider.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, new()
            { [ProviderType.SolanaOASIS] = new() { Wallet(avatar.Id, "original") } })).IsError.Should().BeFalse();
        var invalidOwner = emptyOwner ? Guid.Empty : Guid.NewGuid();
        var batch = new Dictionary<ProviderType, List<IProviderWallet>>
            { [ProviderType.SolanaOASIS] = new() { Wallet(avatar.Id, "rejected"), Wallet(invalidOwner, "invalid") } };
        var rejected = synchronous ? provider.SaveProviderWallets(batch) : await provider.SaveProviderWalletsAsync(batch);
        rejected.IsError.Should().BeTrue();
        rejected.Result.Should().BeFalse();
        rejected.Exception.Should().BeOfType<ArgumentException>();
        avatar.FirstName = "unrelated-success";
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        (await provider.LoadProviderWalletsForAvatarByIdAsync(avatar.Id)).Result[ProviderType.SolanaOASIS]
            .Should().ContainSingle().Which.WalletAddress.Should().Be("original");
        using var control = new DataContext(TestConnectionString);
        control.AvatarWalletStores.Count().Should().Be(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WalletBatchSecondOwnerRejectionRollsBackAllOwners(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatars = Enumerable.Range(0, 2).Select(index => new Avatar
        {
            Id = Guid.NewGuid(), Username = "batch-rollback-" + index,
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)
        }).ToArray();
        Dictionary<ProviderType, List<IProviderWallet>> Wallets(Avatar avatar, string address) => new()
        {
            [ProviderType.SolanaOASIS] = new() { new ProviderWallet { WalletId = Guid.NewGuid(), AvatarId = avatar.Id, WalletAddress = address, ProviderType = ProviderType.SolanaOASIS } }
        };
        foreach (var avatar in avatars)
        {
            (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            (await provider.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, Wallets(avatar, "original"))).IsError.Should().BeFalse();
        }
        using var control = new DataContext(TestConnectionString);
        // IDs are generated GUIDs; parameter-free trigger targets only this owned test row.
        control.Database.ExecuteSqlRaw($"CREATE TRIGGER reject_second_wallet BEFORE UPDATE ON AvatarWalletStore WHEN NEW.AvatarId = '{avatars[1].Id}' BEGIN SELECT RAISE(ABORT,'owned second wallet rejection'); END;");
        var batch = new Dictionary<ProviderType, List<IProviderWallet>>
        {
            [ProviderType.SolanaOASIS] = avatars.SelectMany(avatar => Wallets(avatar, "rejected")[ProviderType.SolanaOASIS]).ToList()
        };
        var rejected = synchronous ? provider.SaveProviderWallets(batch) : await provider.SaveProviderWalletsAsync(batch);
        rejected.IsError.Should().BeTrue();
        rejected.Result.Should().BeFalse();
        rejected.Exception.Should().BeOfType<DbUpdateException>().Which.InnerException.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
        control.Database.ExecuteSqlRaw("DROP TRIGGER reject_second_wallet;");
        avatars[0].FirstName = "unrelated-success";
        (await provider.SaveAvatarAsync(avatars[0])).IsError.Should().BeFalse();
        foreach (var avatar in avatars)
            (await provider.LoadProviderWalletsForAvatarByIdAsync(avatar.Id)).Result[ProviderType.SolanaOASIS]
                .Should().ContainSingle().Which.WalletAddress.Should().Be("original");
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task AvatarDeletionOwnsOnlyItsLocalWalletStore(bool synchronous, bool softDelete)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        foreach (var id in ids)
        {
            var avatar = new Avatar { Id = id, Username = id.ToString(), AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
            (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            (await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = id, Username = avatar.Username })).IsError.Should().BeFalse();
            (await provider.SaveProviderWalletsForAvatarByIdAsync(id, new()
            {
                [ProviderType.SolanaOASIS] = new() { new ProviderWallet { WalletId = Guid.NewGuid(), AvatarId = id, WalletAddress = id.ToString(), ProviderType = ProviderType.SolanaOASIS } }
            })).IsError.Should().BeFalse();
        }
        var batch = new Dictionary<ProviderType, List<IProviderWallet>>
        {
            [ProviderType.SolanaOASIS] = ids.Select(id => (IProviderWallet)new ProviderWallet
            {
                WalletId = Guid.NewGuid(), AvatarId = id, WalletAddress = "batch-" + id, ProviderType = ProviderType.SolanaOASIS
            }).ToList()
        };
        var batchSaved = synchronous ? provider.SaveProviderWallets(batch) : await provider.SaveProviderWalletsAsync(batch);
        batchSaved.IsError.Should().BeFalse(batchSaved.Message);
        foreach (var id in ids)
            (await provider.LoadProviderWalletsForAvatarByIdAsync(id)).Result[ProviderType.SolanaOASIS]
                .Should().ContainSingle().Which.WalletAddress.Should().Be("batch-" + id);
        var deleted = synchronous ? provider.DeleteAvatar(ids[0], softDelete) : await provider.DeleteAvatarAsync(ids[0], softDelete);
        deleted.IsError.Should().BeFalse(deleted.Message);
        deleted.Result.Should().BeTrue();
        using var control = new DataContext(TestConnectionString);
        control.AvatarWalletStores.Count(row => row.AvatarId == ids[0].ToString()).Should().Be(softDelete ? 1 : 0);
        control.AvatarWalletStores.Count(row => row.AvatarId == ids[1].ToString()).Should().Be(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectedWalletMutationDoesNotReplayDuringUnrelatedSave(bool existing)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "wallet-rejection", AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        Dictionary<ProviderType, List<IProviderWallet>> Wallets(string address) => new()
        {
            [ProviderType.SolanaOASIS] = new() { new ProviderWallet { WalletId = Guid.NewGuid(), AvatarId = avatar.Id, WalletAddress = address, ProviderType = ProviderType.SolanaOASIS } }
        };
        if (existing)
            (await provider.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, Wallets("original"))).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw(existing
            ? "CREATE TRIGGER reject_wallet BEFORE UPDATE ON AvatarWalletStore BEGIN SELECT RAISE(ABORT,'owned wallet rejection'); END;"
            : "CREATE TRIGGER reject_wallet BEFORE INSERT ON AvatarWalletStore BEGIN SELECT RAISE(ABORT,'owned wallet rejection'); END;");
        var rejected = await provider.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, Wallets("rejected"));
        rejected.IsError.Should().BeTrue();
        rejected.Result.Should().BeFalse();
        rejected.Exception.Should().BeOfType<DbUpdateException>().Which.InnerException.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
        control.Database.ExecuteSqlRaw("DROP TRIGGER reject_wallet;");
        avatar.FirstName = "unrelated-success";
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        var readback = await provider.LoadProviderWalletsForAvatarByIdAsync(avatar.Id);
        readback.IsError.Should().BeFalse();
        if (existing) readback.Result[ProviderType.SolanaOASIS].Should().ContainSingle().Which.WalletAddress.Should().Be("original");
        else readback.Result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("username")]
    [InlineData("email")]
    public async Task ProviderWalletSaveMustSurviveFreshConnectionReadback(string selector)
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "owned-wallet-avatar", Email = "owned-wallet@test.local",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        var wallet = new ProviderWallet { WalletId = Guid.NewGuid(), AvatarId = avatar.Id,
            ProviderType = ProviderType.SolanaOASIS, WalletAddress = "public-test-address", PublicKey = "public-test-key",
            PrivateKey = "opaque-test-ciphertext-not-a-key", SecretRecoveryPhrase = "opaque-test-ciphertext-not-a-phrase",
            Balance = 12.5, IsDefaultWallet = true, WalletAddressSegwitP2SH = "public-secondary-address" };
        wallet.Name = "owned-wallet-name";
        wallet.Description = "owned-wallet-description";
        wallet.CreatedByAvatarId = avatar.Id;
        wallet.CreatedDate = new DateTime(2026, 10, 14, 1, 2, 3, DateTimeKind.Utc);
        wallet.Version = 7;
        wallet.MetaData["public-test-label"] = "retained";
        var transaction = new NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response.WalletTransaction
        {
            TransactionId = Guid.NewGuid(), NativeTransactionId = "test-native-receipt",
            RawAmount = "12345678901234567890", AmountUnit = "lamport", Amount = 12.5,
            Description = "retained-history", CreatedDate = DateTime.UtcNow
        };
        wallet.Transactions = new() { transaction };
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            var wallets = new Dictionary<ProviderType, List<IProviderWallet>> { [ProviderType.SolanaOASIS] = new() { wallet } };
            var saved = selector switch
            {
                "id" => await provider.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, wallets),
                "username" => await provider.SaveProviderWalletsForAvatarByUsernameAsync(avatar.Username, wallets),
                "email" => await provider.SaveProviderWalletsForAvatarByEmailAsync(avatar.Email, wallets),
                _ => throw new ArgumentException(nameof(selector))
            };
            saved.IsError.Should().BeFalse(saved.Message);
            saved.Result.Should().BeTrue();
            // Public avatar replication/update does not own local credential records.
            avatar.ProviderWallets = new();
            avatar.FirstName = "public-updated";
            (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded = await reopened.LoadProviderWalletsForAvatarByIdAsync(avatar.Id);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Should().ContainKey(ProviderType.SolanaOASIS);
        var persistedWallet = loaded.Result[ProviderType.SolanaOASIS].Should().ContainSingle().Subject;
        persistedWallet.WalletId.Should().Be(wallet.WalletId);
        persistedWallet.AvatarId.Should().Be(avatar.Id);
        persistedWallet.PrivateKey.Should().Be(wallet.PrivateKey);
        persistedWallet.SecretRecoveryPhrase.Should().Be(wallet.SecretRecoveryPhrase);
        persistedWallet.WalletAddress.Should().Be(wallet.WalletAddress);
        persistedWallet.PublicKey.Should().Be(wallet.PublicKey);
        persistedWallet.Balance.Should().Be(wallet.Balance);
        persistedWallet.IsDefaultWallet.Should().BeTrue();
        persistedWallet.WalletAddressSegwitP2SH.Should().Be(wallet.WalletAddressSegwitP2SH);
        persistedWallet.Name.Should().Be(wallet.Name);
        persistedWallet.Description.Should().Be(wallet.Description);
        persistedWallet.CreatedByAvatarId.Should().Be(avatar.Id);
        persistedWallet.CreatedDate.Should().Be(wallet.CreatedDate);
        persistedWallet.Version.Should().Be(7);
        persistedWallet.MetaData["public-test-label"].ToString().Should().Be("retained");
        var persistedTransaction = persistedWallet.Transactions.Should().ContainSingle().Subject;
        persistedTransaction.TransactionId.Should().Be(transaction.TransactionId);
        persistedTransaction.NativeTransactionId.Should().Be(transaction.NativeTransactionId);
        persistedTransaction.RawAmount.Should().Be(transaction.RawAmount);
        persistedTransaction.AmountUnit.Should().Be(transaction.AmountUnit);
        persistedTransaction.CreatedDate.Should().Be(transaction.CreatedDate);
        var aggregate = await reopened.LoadProviderWalletsAsync();
        aggregate.IsError.Should().BeFalse(aggregate.Message);
        aggregate.Result[ProviderType.SolanaOASIS].Should().ContainSingle().Which.WalletId.Should().Be(wallet.WalletId);
        (await reopened.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, new())).IsError.Should().BeFalse();
        (await reopened.LoadProviderWalletsForAvatarByIdAsync(avatar.Id)).Result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ActivationFailureRetainsNativeCauseAndSearchDoesNotHideIt(bool asynchronous)
    {
        using var provider = new SQLLiteDBOASIS($"Data Source={_databasePath};Mode=ReadOnly;Pooling=False");
        var activated = asynchronous ? await provider.ActivateProviderAsync() : provider.ActivateProvider();
        activated.IsError.Should().BeTrue();
        activated.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
        provider.IsProviderActivated.Should().BeFalse();
        var searched = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams());
        searched.IsError.Should().BeTrue();
        searched.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
        File.Exists(_databasePath).Should().BeFalse();
    }

    private readonly string _databasePath;
    private readonly FileStream _lease;
    private string TestConnectionString => $"Data Source={_databasePath};Pooling=False";

    public SQLLiteDBOASISIntegrationTests()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) root = root.Parent;
        if (root == null) throw new InvalidOperationException("Existing task checkout required.");
        var directory = Path.Combine(root.FullName,"TestResults/SQLiteCurrent");
        Directory.CreateDirectory(directory);
        _databasePath = Path.Combine(directory,"integration-current.db");
        _lease = new FileStream(Path.Combine(directory,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        if (File.Exists(_databasePath)) { _lease.Dispose(); throw new InvalidOperationException("Existing fixture DB requires inspection; refusing overwrite."); }
    }

    public void Dispose()
    {
        try
        {
            foreach (var file in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
                if (File.Exists(file)) File.Delete(file);
        }
        finally { _lease.Dispose(); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarDeletionLookupFailureReturnsOriginalStorageError(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw("DROP TABLE Avatar;");
        var result = synchronous ? provider.DeleteAvatar(Guid.NewGuid(), false) : await provider.DeleteAvatarAsync(Guid.NewGuid(), false);
        result.IsError.Should().BeTrue();
        result.Result.Should().BeFalse();
        result.Exception.Should().NotBeNull();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task FailedAvatarSoftDeletionDoesNotReplayDuringLaterSave(bool synchronous, bool missingDetail)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "soft-rejection",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        if (!missingDetail)
            (await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Karma = 123 })).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        if (!missingDetail)
            control.Database.ExecuteSqlRaw("CREATE TRIGGER reject_soft_detail BEFORE UPDATE ON AvatarDetail BEGIN SELECT RAISE(ABORT, 'owned soft deletion rejection'); END;");
        var failed = synchronous ? provider.DeleteAvatar(avatar.Id, true) : await provider.DeleteAvatarAsync(avatar.Id, true);
        failed.IsError.Should().BeTrue();
        failed.Result.Should().BeFalse();
        if (!missingDetail)
        {
            failed.Exception.Should().NotBeNull();
            control.Database.ExecuteSqlRaw("DROP TRIGGER reject_soft_detail;");
        }
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "after-soft-rejection" })).IsError.Should().BeFalse();
        control.Avatars.AsNoTracking().Single(a => a.Id == avatar.Id.ToString()).DeletedDate.Should().Be(DateTime.MinValue);
        if (!missingDetail)
            control.AvatarDetails.AsNoTracking().Single(a => a.Id == avatar.Id.ToString()).DeletedDate.Should().Be(DateTime.MinValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarSoftDeletionMatchesDetailByIdentityAndPersists(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "soft-delete",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        (await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = avatar.Id, Username = "independent-detail-name", Karma = 123 })).IsError.Should().BeFalse();
        var deleted = synchronous ? provider.DeleteAvatar(avatar.Id, true) : await provider.DeleteAvatarAsync(avatar.Id, true);
        deleted.IsError.Should().BeFalse(deleted.Message);
        deleted.Result.Should().BeTrue();
        using var control = new DataContext(TestConnectionString);
        control.Avatars.Single(a => a.Id == avatar.Id.ToString()).DeletedDate.Should().NotBe(DateTime.MinValue);
        control.AvatarDetails.Single(a => a.Id == avatar.Id.ToString()).DeletedDate.Should().NotBe(DateTime.MinValue);
        control.AvatarDetails.Single(a => a.Id == avatar.Id.ToString()).Karma.Should().Be(123);
    }

    [Theory]
    [InlineData(true, "email")]
    [InlineData(false, "email")]
    [InlineData(true, "username")]
    [InlineData(false, "username")]
    [InlineData(true, "key")]
    [InlineData(false, "key")]
    public async Task AvatarDeletionSelectorsUsePersistedIdentity(bool synchronous, string selector)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "selector-delete", Email = "selector-delete@test.local",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        avatar.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "owned-avatar-delete-key";
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        (await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = avatar.Id, Username = "different-detail-username", Karma = 123 })).IsError.Should().BeFalse();
        OASISResult<bool> deleted;
        if (selector == "email") deleted = synchronous ? provider.DeleteAvatarByEmail(avatar.Email, false) : await provider.DeleteAvatarByEmailAsync(avatar.Email, false);
        else if (selector == "username") deleted = synchronous ? provider.DeleteAvatarByUsername(avatar.Username, false) : await provider.DeleteAvatarByUsernameAsync(avatar.Username, false);
        else deleted = synchronous ? provider.DeleteAvatar("owned-avatar-delete-key", false) : await provider.DeleteAvatarAsync("owned-avatar-delete-key", false);
        deleted.IsError.Should().BeFalse(deleted.Message);
        deleted.Result.Should().BeTrue();
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await reopened.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
        (await reopened.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectedAvatarDeleteCannotReplayDuringUnrelatedSave(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "delete-rejection", Email = "delete@test.local",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Karma = 123 };
        var item = new InventoryItem { Id = Guid.NewGuid(), Name = "preserved-after-rejection", Quantity = 2 };
        detail.Inventory.Add(item);
        detail.Stats.HP.Current = 37;
        (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.ProviderPublicKeyModel>().Add(
            new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.ProviderPublicKeyModel(ProviderType.MongoDBOASIS, "owned-public-key") { OwnerId = avatar.Id.ToString() });
        control.SaveChanges();
        control.Database.ExecuteSqlRaw("CREATE TRIGGER reject_avatar_delete BEFORE DELETE ON Avatar BEGIN SELECT RAISE(ABORT, 'owned avatar deletion rejection'); END;");
        var rejected = synchronous ? provider.DeleteAvatar(avatar.Id, false) : await provider.DeleteAvatarAsync(avatar.Id, false);
        rejected.IsError.Should().BeTrue();
        rejected.Exception.Should().NotBeNull();
        control.Database.ExecuteSqlRaw("DROP TRIGGER reject_avatar_delete;");
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "unrelated-after-rejected-delete" })).IsError.Should().BeFalse();
        control.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.ProviderPublicKeyModel>().AsNoTracking()
            .Count(k => k.OwnerId == avatar.Id.ToString()).Should().Be(1);
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await reopened.LoadAvatarAsync(avatar.Id)).Result.Should().NotBeNull();
        var preserved = (await reopened.LoadAvatarDetailAsync(avatar.Id)).Result;
        preserved.Karma.Should().Be(123);
        preserved.Stats.HP.Current.Should().Be(37);
        preserved.Inventory.Single().Id.Should().Be(item.Id);
        preserved.Inventory.Single().Quantity.Should().Be(2);
        var accepted = synchronous ? provider.DeleteAvatar(avatar.Id, false) : await provider.DeleteAvatarAsync(avatar.Id, false);
        accepted.IsError.Should().BeFalse(accepted.Message);
        accepted.Result.Should().BeTrue();
        (await reopened.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
        (await reopened.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
        control.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.ProviderPublicKeyModel>().AsNoTracking()
            .Count(k => k.OwnerId == avatar.Id.ToString()).Should().Be(0);
    }

    [Theory]
    [InlineData(true, "username")]
    [InlineData(false, "username")]
    [InlineData(true, "email")]
    [InlineData(false, "email")]
    [InlineData(true, "list")]
    [InlineData(false, "list")]
    public async Task DetailSelectorFirstReadsHydrateStatsWithoutTrackingFixup(bool synchronous, string selector)
    {
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Username = "selector-first", Email = "selector-first@test.local" };
        detail.Stats.HP.Current = 37;
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        IAvatarDetail loaded;
        if (selector == "username") loaded = (synchronous ? reopened.LoadAvatarDetailByUsername(detail.Username) : await reopened.LoadAvatarDetailByUsernameAsync(detail.Username)).Result;
        else if (selector == "email") loaded = (synchronous ? reopened.LoadAvatarDetailByEmail(detail.Email) : await reopened.LoadAvatarDetailByEmailAsync(detail.Email)).Result;
        else loaded = (synchronous ? reopened.LoadAllAvatarDetails() : await reopened.LoadAllAvatarDetailsAsync()).Result.Single();
        loaded.Stats.Should().NotBeNull();
        loaded.Stats.HP.Current.Should().Be(37);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CorruptUnrelatedDetailCannotPoisonIdentityLookup(bool synchronous)
    {
        var bad = new AvatarDetail { Id = Guid.NewGuid(), Username = "corrupt-detail" };
        var good = new AvatarDetail { Id = Guid.NewGuid(), Username = "valid-detail", Email = "valid-detail@test.local" };
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveAvatarDetailAsync(bad)).IsError.Should().BeFalse();
            (await provider.SaveAvatarDetailAsync(good)).IsError.Should().BeFalse();
        }
        using var control = new DataContext(TestConnectionString);
        control.Database.ExecuteSqlInterpolated($"UPDATE AvatarDetail SET MetaDataJson = {'{'} WHERE Id = {bad.Id.ToString()}");
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var byId = synchronous ? reopened.LoadAvatarDetail(good.Id) : await reopened.LoadAvatarDetailAsync(good.Id);
        var byName = synchronous ? reopened.LoadAvatarDetailByUsername(good.Username) : await reopened.LoadAvatarDetailByUsernameAsync(good.Username);
        var byEmail = synchronous ? reopened.LoadAvatarDetailByEmail(good.Email) : await reopened.LoadAvatarDetailByEmailAsync(good.Email);
        foreach (var loaded in new[] { byId, byName, byEmail })
        {
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Id.Should().Be(good.Id);
        }
        (await reopened.LoadAvatarDetailAsync(bad.Id)).IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CorruptUnrelatedAvatarCannotPoisonIdentityLookup(bool synchronous)
    {
        var bad = new Avatar { Id = Guid.NewGuid(), Username = "corrupt-avatar", AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        var good = new Avatar { Id = Guid.NewGuid(), Username = "valid-avatar", Email = "valid-avatar@test.local", AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveAvatarAsync(bad)).IsError.Should().BeFalse();
            (await provider.SaveAvatarAsync(good)).IsError.Should().BeFalse();
        }
        using var control = new DataContext(TestConnectionString);
        control.Database.ExecuteSqlInterpolated($"UPDATE Avatar SET MetaDataJson = {'{'} WHERE Id = {bad.Id.ToString()}");
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var byId = synchronous ? reopened.LoadAvatar(good.Id) : await reopened.LoadAvatarAsync(good.Id);
        var byName = synchronous ? reopened.LoadAvatarByUsername(good.Username) : await reopened.LoadAvatarByUsernameAsync(good.Username);
        var byEmail = synchronous ? reopened.LoadAvatarByEmail(good.Email) : await reopened.LoadAvatarByEmailAsync(good.Email);
        foreach (var loaded in new[] { byId, byName, byEmail })
        {
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Id.Should().Be(good.Id);
        }
        (await reopened.LoadAvatarAsync(bad.Id)).IsError.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CorruptUnrelatedHolonCannotPoisonGuidLookup(bool synchronous)
    {
        var bad = new Holon { Id = Guid.NewGuid(), Name = "owned-corrupt" };
        var good = new Holon { Id = Guid.NewGuid(), Name = "owned-valid" };
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(bad,saveChildren:false)).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(good,saveChildren:false)).IsError.Should().BeFalse();
        }
        using var control = new DataContext(TestConnectionString);
        control.Database.ExecuteSqlInterpolated($"UPDATE Holon SET MetaDataJson = {'{'} WHERE Id = {bad.Id.ToString()}");
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded = synchronous ? reopened.LoadHolon(good.Id,loadChildren:false) : await reopened.LoadHolonAsync(good.Id,loadChildren:false);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Name.Should().Be(good.Name);
        var corrupt = synchronous ? reopened.LoadHolon(bad.Id,loadChildren:false) : await reopened.LoadHolonAsync(bad.Id,loadChildren:false);
        corrupt.IsError.Should().BeTrue();
        corrupt.Result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarProviderKeySelectorMatchesOnlyLocalKeyAndRetainsForeignMap(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "key-avatar", Version = 3,
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        avatar.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "local-avatar-key";
        avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "foreign-avatar-key";
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        var loaded = synchronous ? provider.LoadAvatarByProviderKey("local-avatar-key") : await provider.LoadAvatarByProviderKeyAsync("local-avatar-key");
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Id.Should().Be(avatar.Id);
        loaded.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be("foreign-avatar-key");
        (await provider.LoadAvatarByProviderKeyAsync("foreign-avatar-key")).Result.Should().BeNull();
        provider.LoadAvatarByProviderKey("local-avatar-key",version:2).Result.Should().BeNull();
        var duplicate = new Avatar { Id = Guid.NewGuid(), Username = "conflicting-key-avatar",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        duplicate.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "local-avatar-key";
        var rejected = synchronous ? provider.SaveAvatar(duplicate) : await provider.SaveAvatarAsync(duplicate);
        rejected.IsError.Should().BeTrue();
        rejected.Exception.Should().NotBeNull();
        (await provider.LoadAvatarAsync(duplicate.Id)).Result.Should().BeNull();
        (await provider.LoadAvatarByProviderKeyAsync("local-avatar-key")).Result.Id.Should().Be(avatar.Id);
        var next = new Avatar { Id = Guid.NewGuid(), Username = "valid-after-key-rejection",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        var saved = synchronous ? provider.SaveAvatar(next) : await provider.SaveAvatarAsync(next);
        saved.IsError.Should().BeFalse(saved.Message);
        (await provider.LoadAvatarAsync(next.Id)).Result.Id.Should().Be(next.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DetailVersionUpdatesRemainQueryableAsCurrentAndByExactVersion(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Username = "versioned-detail", Email = "versioned-detail@test.local", Version = 3, Karma = 100 };
        var creator = Guid.NewGuid();
        var created = new DateTime(2025,1,2,3,4,5,DateTimeKind.Utc);
        detail.CreatedByAvatarId = creator;
        detail.CreatedDate = created;
        var originType = Enum.GetValues<OASISType>().First();
        detail.CreatedProviderType = new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
        detail.CreatedOASISType = new NextGenSoftware.Utilities.EnumValue<OASISType>(originType);
        (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        detail.Version = 4;
        detail.CreatedByAvatarId = Guid.Empty;
        detail.CreatedDate = default;
        detail.CreatedProviderType = null;
        detail.CreatedOASISType = null;
        detail.Karma = 200;
        var updated = synchronous ? provider.SaveAvatarDetail(detail) : await provider.SaveAvatarDetailAsync(detail);
        updated.IsError.Should().BeFalse(updated.Message);
        foreach (var loaded in new[] { await provider.LoadAvatarDetailAsync(detail.Id), provider.LoadAvatarDetail(detail.Id,version:4),
            await provider.LoadAvatarDetailByUsernameAsync(detail.Username), provider.LoadAvatarDetailByEmail(detail.Email) })
        {
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Version.Should().Be(4);
            loaded.Result.Karma.Should().Be(200);
            loaded.Result.CreatedByAvatarId.Should().Be(creator);
            loaded.Result.CreatedDate.Should().Be(created);
            loaded.Result.CreatedProviderType.Value.Should().Be(ProviderType.MongoDBOASIS);
            loaded.Result.CreatedOASISType.Value.Should().Be(originType);
        }
        (await provider.LoadAvatarDetailAsync(detail.Id,version:3)).Result.Should().BeNull();
        provider.LoadAvatarDetailByUsername(detail.Username,version:3).Result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectedDetailUpdateDoesNotReplayStatsOrKarmaDuringLaterSave(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Username = "rejected-detail", Karma = 100 };
        var item = new InventoryItem { Id = Guid.NewGuid(), Name = "rollback-seed", Quantity = 2 };
        item.MetaData["quest-source"] = "original-tree";
        detail.Inventory.Add(item);
        detail.Stats.HP.Current = 31;
        (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw("CREATE TRIGGER reject_detail_update BEFORE UPDATE ON AvatarDetail BEGIN SELECT RAISE(ABORT, 'owned detail rejection'); END;");
        detail.Karma = 999;
        detail.Stats.HP.Current = 88;
        item.Quantity = 9;
        item.MetaData["quest-source"] = "rejected-tree";
        var rejected = synchronous ? provider.SaveAvatarDetail(detail) : await provider.SaveAvatarDetailAsync(detail);
        rejected.IsError.Should().BeTrue();
        rejected.Exception.Should().NotBeNull();
        control.Database.ExecuteSqlRaw("DROP TRIGGER reject_detail_update;");
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid() }, saveChildren:false)).IsError.Should().BeFalse();
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded = await reopened.LoadAvatarDetailAsync(detail.Id);
        loaded.Result.Karma.Should().Be(100);
        loaded.Result.Stats.HP.Current.Should().Be(31);
        loaded.Result.Inventory.Single().Id.Should().Be(item.Id);
        loaded.Result.Inventory.Single().Quantity.Should().Be(2);
        loaded.Result.Inventory.Single().MetaData["quest-source"].ToString().Should().Be("original-tree");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExplicitMissingStatsReplacementRemovesOnlyOwnedRowAndAllowsRestoration(bool synchronous)
    {
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Username = "optional-stats" };
        var other = new AvatarDetail { Id = Guid.NewGuid(), Username = "other-stats" };
        detail.MetaData["shared-key"] = "first-avatar";
        other.MetaData["shared-key"] = "second-avatar";
        other.Stats.HP.Current = 27;
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
            (await provider.SaveAvatarDetailAsync(other)).IsError.Should().BeFalse();
            detail.Stats = null;
            var saved = synchronous ? provider.SaveAvatarDetail(detail) : await provider.SaveAvatarDetailAsync(detail);
            saved.IsError.Should().BeFalse(saved.Message);
            saved.Result.Should().NotBeNull();
            saved.Result.Id.Should().Be(detail.Id);
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await reopened.LoadAvatarDetailAsync(detail.Id)).Result.Stats.Should().BeNull();
        (await reopened.LoadAvatarDetailAsync(other.Id)).Result.Stats.HP.Current.Should().Be(27);
        (await reopened.LoadAvatarDetailAsync(detail.Id)).Result.MetaData["shared-key"].ToString().Should().Be("first-avatar");
        (await reopened.LoadAvatarDetailAsync(other.Id)).Result.MetaData["shared-key"].ToString().Should().Be("second-avatar");
        detail.Stats = new AvatarDetail().Stats;
        detail.Stats.HP.Current = 39;
        var restored = synchronous ? reopened.SaveAvatarDetail(detail) : await reopened.SaveAvatarDetailAsync(detail);
        restored.IsError.Should().BeFalse(restored.Message);
        (await reopened.LoadAvatarDetailAsync(detail.Id)).Result.Stats.HP.Current.Should().Be(39);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarDetailStatsSurviveFreshConnection(bool synchronous)
    {
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Username = "stats-detail", Email = "stats@test.local", Karma = 456, XP = 234 };
        detail.Stats.HP.Current = 37;
        detail.Stats.Mana.Current = 42;
        detail.Stats.Stamina.Current = 53;
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var saved = synchronous ? provider.SaveAvatarDetail(detail) : await provider.SaveAvatarDetailAsync(detail);
            saved.IsError.Should().BeFalse(saved.Message);
            detail.Stats.HP.Current = 38;
            var updated = synchronous ? provider.SaveAvatarDetail(detail) : await provider.SaveAvatarDetailAsync(detail);
            updated.IsError.Should().BeFalse(updated.Message);
            updated.Result.Should().NotBeNull();
            updated.Result.Stats.HP.Current.Should().Be(38);
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded = synchronous ? reopened.LoadAvatarDetail(detail.Id) : await reopened.LoadAvatarDetailAsync(detail.Id);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Karma.Should().Be(456);
        loaded.Result.XP.Should().Be(234);
        loaded.Result.Stats.HP.Current.Should().Be(38);
        loaded.Result.Stats.Mana.Current.Should().Be(42);
        loaded.Result.Stats.Stamina.Current.Should().Be(53);
        (await reopened.LoadAvatarDetailByUsernameAsync(detail.Username)).Result.Stats.HP.Current.Should().Be(38);
        reopened.LoadAvatarDetailByEmail(detail.Email).Result.Stats.Mana.Current.Should().Be(42);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectedAvatarUpdateDoesNotLeakIntoLaterSave(bool synchronous)
    {
        using var context = new DataContext(TestConnectionString);
        context.Database.EnsureCreated();
        var repository = new AvatarRepository(context);
        var original = new Avatar { Id = Guid.NewGuid(), Username = "held-avatar", FirstName = "original",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        repository.SaveAvatar(original).IsError.Should().BeFalse();
        context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_avatar_update BEFORE UPDATE ON Avatar BEGIN SELECT RAISE(ABORT, 'owned avatar rejection'); END;");
        original.FirstName = "rejected";
        var rejected = synchronous ? repository.SaveAvatar(original) : await repository.SaveAvatarAsync(original);
        rejected.IsError.Should().BeTrue();
        rejected.Exception.Should().NotBeNull();
        rejected.IsSaved.Should().BeFalse();
        context.Database.ExecuteSqlRaw("DROP TRIGGER reject_avatar_update;");
        var next = new Avatar { Id = Guid.NewGuid(), Username = "next-avatar",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        var saved = synchronous ? repository.SaveAvatar(next) : await repository.SaveAvatarAsync(next);
        saved.IsError.Should().BeFalse(saved.Message);
        repository.LoadAvatar(original.Id).Result.FirstName.Should().Be("original");
        repository.LoadAvatar(next.Id).Result.Id.Should().Be(next.Id);
        var accepted = synchronous ? repository.SaveAvatar(original) : await repository.SaveAvatarAsync(original);
        accepted.IsError.Should().BeFalse(accepted.Message);
        repository.LoadAvatar(original.Id).Result.FirstName.Should().Be("rejected");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CredentialRepositorySelectorRequiresMatchingStoredCredentialAndVersion(bool synchronous)
    {
        using var context = new DataContext(TestConnectionString);
        context.Database.EnsureCreated();
        var repository = new AvatarRepository(context);
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "credential-fixture", Password = "owned-stored-credential", Version = 3,
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        repository.SaveAvatar(avatar).IsError.Should().BeFalse();
        var loaded = synchronous ? repository.LoadAvatar(avatar.Username, avatar.Password) : await repository.LoadAvatarAsync(avatar.Username, avatar.Password);
        loaded.IsLoaded.Should().BeTrue();
        loaded.Result.Id.Should().Be(avatar.Id);
        var rejected = synchronous ? repository.LoadAvatar(avatar.Username, "wrong") : await repository.LoadAvatarAsync(avatar.Username, "wrong");
        rejected.Result.Should().BeNull();
        rejected.IsLoaded.Should().BeFalse();
        var mismatch = synchronous ? repository.LoadAvatar(avatar.Username, avatar.Password, 2) : await repository.LoadAvatarAsync(avatar.Username, avatar.Password, 2);
        mismatch.Result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarCurrentVersionAndExplicitSelectorsRespectPersistedVersion(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "versioned-avatar", Email = "versioned@test.local", Version = 3,
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        var saved = synchronous ? provider.SaveAvatar(avatar) : await provider.SaveAvatarAsync(avatar);
        saved.IsError.Should().BeFalse(saved.Message);
        saved.Result.Version.Should().Be(3);
        foreach (var loaded in new[] { await provider.LoadAvatarAsync(avatar.Id), await provider.LoadAvatarByUsernameAsync(avatar.Username), await provider.LoadAvatarByEmailAsync(avatar.Email) })
        {
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Version.Should().Be(3);
        }
        (await provider.LoadAvatarAsync(avatar.Id, version:3)).Result.Version.Should().Be(3);
        (await provider.LoadAvatarAsync(avatar.Id, version:2)).Result.Should().BeNull();
        provider.LoadAvatarByUsername(avatar.Username, version:2).Result.Should().BeNull();
        provider.LoadAvatarByEmail(avatar.Email, version:3).Result.Version.Should().Be(3);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task UpdatesPreserveCreationAuditForTrackedAndReopenedRecords(bool synchronous, bool reopen)
    {
        var id = Guid.NewGuid();
        var creator = Guid.NewGuid();
        var created = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(new Holon { Id = id, CreatedByAvatarId = creator, CreatedDate = created }, saveChildren:false)).IsError.Should().BeFalse();
            if (!reopen)
            {
                var replacement = new Holon { Id = id, Name = "updated" };
                var result = synchronous ? provider.SaveHolon(replacement, saveChildren:false) : await provider.SaveHolonAsync(replacement, saveChildren:false);
                result.IsError.Should().BeFalse(result.Message);
                result.Result.CreatedByAvatarId.Should().Be(creator);
            }
        }
        using var next = new SQLLiteDBOASIS(TestConnectionString);
        (await next.ActivateProviderAsync()).IsError.Should().BeFalse();
        if (reopen)
        {
            var replacement = new Holon { Id = id, Name = "updated" };
            var result = synchronous ? next.SaveHolon(replacement, saveChildren:false) : await next.SaveHolonAsync(replacement, saveChildren:false);
            result.IsError.Should().BeFalse(result.Message);
        }
        var loaded = await next.LoadHolonAsync(id, loadChildren:false);
        loaded.Result.CreatedByAvatarId.Should().Be(creator);
        loaded.Result.CreatedDate.Should().Be(created);
        loaded.Result.Name.Should().Be("updated");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuditIdentityAndDimensionFieldsSurviveDatabaseReopen(bool synchronous)
    {
        var original = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid(), ModifiedByAvatarId = Guid.NewGuid(), DeletedByAvatarId = Guid.NewGuid(),
            DimensionLevel = Enum.GetValues<DimensionLevel>().Last(), SubDimensionLevel = Enum.GetValues<SubDimensionLevel>().Last() };
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var saved = synchronous ? provider.SaveHolon(original, saveChildren:false) : await provider.SaveHolonAsync(original, saveChildren:false);
            saved.IsError.Should().BeFalse(saved.Message);
            saved.Result.CreatedByAvatarId.Should().Be(original.CreatedByAvatarId);
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var result = await reopened.LoadHolonAsync(original.Id, loadChildren:false);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.CreatedByAvatarId.Should().Be(original.CreatedByAvatarId);
        result.Result.ModifiedByAvatarId.Should().Be(original.ModifiedByAvatarId);
        result.Result.DeletedByAvatarId.Should().Be(original.DeletedByAvatarId);
        result.Result.DimensionLevel.Should().Be(original.DimensionLevel);
        result.Result.SubDimensionLevel.Should().Be(original.SubDimensionLevel);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task ParentQueryDatabaseFailureMustNotReportSuccess(bool synchronous, bool byKey)
    {
        using var context = new DataContext(TestConnectionString);
        context.Database.EnsureCreated();
        var repository = new HolonRepository(context);
        context.Database.ExecuteSqlRaw("DROP TABLE Holon;");
        var result = byKey
            ? (synchronous ? repository.LoadHolonsForParent("parent", loadChildren:false) : await repository.LoadHolonsForParentAsync("parent", loadChildren:false))
            : (synchronous ? repository.LoadHolonsForParent(Guid.NewGuid(), loadChildren:false) : await repository.LoadHolonsForParentAsync(Guid.NewGuid(), loadChildren:false));
        result.IsError.Should().BeTrue();
        result.IsLoaded.Should().BeFalse();
        result.Exception.Should().NotBeNull();
        result.Result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task GuidHolonLoadRetainsOriginalDatabaseFailure(bool synchronous)
    {
        using var context=new DataContext(TestConnectionString);context.Database.EnsureCreated();
        var repository=new HolonRepository(context);context.Database.ExecuteSqlRaw("DROP TABLE Holon;");
        var id=Guid.NewGuid();
        var result=synchronous?repository.LoadHolon(id,loadChildren:false):await repository.LoadHolonAsync(id,loadChildren:false);
        result.IsError.Should().BeTrue();result.IsLoaded.Should().BeFalse();result.Result.Should().BeNull();
        result.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
        result.Message.Should().Contain("no such table");
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task AllHolonLoadRetainsOriginalDatabaseFailure(bool synchronous)
    {
        using var context=new DataContext(TestConnectionString);context.Database.EnsureCreated();
        var repository=new HolonRepository(context);context.Database.ExecuteSqlRaw("DROP TABLE Holon;");
        var result=synchronous?repository.LoadAllHolons(loadChildren:false):await repository.LoadAllHolonsAsync(loadChildren:false);
        result.IsError.Should().BeTrue();result.IsLoaded.Should().BeFalse();result.Result.Should().BeNull();
        result.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
    }

    [Theory]
    [InlineData("Holon")] [InlineData("Avatar")]
    public async Task PublicSearchMustNotHideDatabaseFailure(string table)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))control.Database.ExecuteSqlRaw("DROP TABLE "+table+";");
        var result=await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{
            SearchGroups=new(){new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup{SearchQuery="owned-test"}}});
        result.IsError.Should().BeTrue();result.Result.Should().BeNull();
        result.Message.Should().Contain("search failed");
        result.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task AllAvatarLoadRetainsOriginalDatabaseFailure(bool synchronous)
    {
        using var context=new DataContext(TestConnectionString);context.Database.EnsureCreated();
        var repository=new AvatarRepository(context);context.Database.ExecuteSqlRaw("DROP TABLE Avatar;");
        var result=synchronous?repository.LoadAllAvatars():await repository.LoadAllAvatarsAsync();
        result.IsError.Should().BeTrue();result.IsLoaded.Should().BeFalse();result.Result.Should().BeNull();
        result.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
    }

    [Theory]
    [InlineData(true,false)] [InlineData(false,false)]
    [InlineData(true,true)] [InlineData(false,true)]
    public async Task PublicGuidAvatarLoadDistinguishesAbsenceFromDatabaseFailure(bool synchronous,bool brokenTable)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        if(brokenTable){using var control=new DataContext(TestConnectionString);control.Database.ExecuteSqlRaw("DROP TABLE Avatar;");}
        var id=Guid.NewGuid();var result=synchronous?provider.LoadAvatar(id):await provider.LoadAvatarAsync(id);
        result.IsLoaded.Should().BeFalse();result.Result.Should().BeNull();result.IsError.Should().Be(brokenTable);
        if(brokenTable)result.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
        else result.Exception.Should().BeNull();
    }

    [Theory]
    [InlineData(true,true,false)] [InlineData(false,true,false)]
    [InlineData(true,false,false)] [InlineData(false,false,false)]
    [InlineData(true,true,true)] [InlineData(false,true,true)]
    [InlineData(true,false,true)] [InlineData(false,false,true)]
    public async Task PublicAvatarIdentitySelectorsRetainAbsenceAndFailure(bool synchronous,bool email,bool brokenTable)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        if(brokenTable){using var control=new DataContext(TestConnectionString);control.Database.ExecuteSqlRaw("DROP TABLE Avatar;");}
        var result=email
            ? (synchronous?provider.LoadAvatarByEmail("missing@example.invalid"):await provider.LoadAvatarByEmailAsync("missing@example.invalid"))
            : (synchronous?provider.LoadAvatarByUsername("missing-owned-avatar"):await provider.LoadAvatarByUsernameAsync("missing-owned-avatar"));
        result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();result.IsError.Should().Be(brokenTable);
        if(brokenTable)result.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
        else result.Exception.Should().BeNull();
    }

    [Theory]
    [InlineData(true,0,false)] [InlineData(false,0,false)] [InlineData(true,1,false)] [InlineData(false,1,false)]
    [InlineData(true,2,false)] [InlineData(false,2,false)]
    [InlineData(true,0,true)] [InlineData(false,0,true)] [InlineData(true,1,true)] [InlineData(false,1,true)]
    [InlineData(true,2,true)] [InlineData(false,2,true)]
    public async Task PublicAvatarDetailSelectorsRetainAbsenceAndFailure(bool synchronous,int selector,bool brokenTable)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        if(brokenTable){using var control=new DataContext(TestConnectionString);control.Database.ExecuteSqlRaw("DROP TABLE AvatarDetail;");}
        var id=Guid.NewGuid();
        var result=selector switch {
            0=>synchronous?provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id),
            1=>synchronous?provider.LoadAvatarDetailByEmail("missing@example.invalid"):await provider.LoadAvatarDetailByEmailAsync("missing@example.invalid"),
            _=>synchronous?provider.LoadAvatarDetailByUsername("missing-owned-avatar"):await provider.LoadAvatarDetailByUsernameAsync("missing-owned-avatar")
        };
        result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();result.IsError.Should().Be(brokenTable);
        if(brokenTable)result.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
        else result.Exception.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ParentSelectorsIsolateChildrenAndRespectTypeAndVersion(bool synchronous)
    {
        using var context = new DataContext(TestConnectionString);
        context.Database.EnsureCreated();
        var repository = new HolonRepository(context);
        var parent = new Holon { Id = Guid.NewGuid() };
        parent.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "local-parent";
        parent.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "foreign-parent";
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = parent.Id, HolonType = HolonType.Quest, Version = 3 };
        foreach (var row in new[] { parent, child, new Holon { Id = Guid.NewGuid(), HolonType = HolonType.Quest, Version = 3 }, new Holon { Id = Guid.NewGuid(), ParentHolonId = parent.Id, Version = 2 } })
            repository.SaveHolon(row).IsError.Should().BeFalse();
        var byId = synchronous ? repository.LoadHolonsForParent(parent.Id, HolonType.Quest, loadChildren:false) : await repository.LoadHolonsForParentAsync(parent.Id, HolonType.Quest, loadChildren:false);
        byId.Result.Select(row => row.Id).Should().Equal(child.Id);
        var byKey = synchronous ? repository.LoadHolonsForParent("local-parent", HolonType.Quest, loadChildren:false, version:3) : await repository.LoadHolonsForParentAsync("local-parent", HolonType.Quest, loadChildren:false, version:3);
        byKey.Result.Select(row => row.Id).Should().Equal(child.Id);
        repository.LoadHolonsForParent("foreign-parent", loadChildren:false).Result.Should().BeEmpty();
        repository.LoadHolonsForParent(parent.Id, HolonType.Quest, loadChildren:false, version:2).Result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task RejectedHardDeleteRetainsErrorAndDoesNotLeakIntoLaterSave(bool synchronous, bool byKey)
    {
        using var context = new DataContext(TestConnectionString);
        context.Database.EnsureCreated();
        var repository = new HolonRepository(context);
        var id = Guid.NewGuid();
        repository.SaveHolon(new Holon { Id = id, IsActive = true }).IsError.Should().BeFalse();
        context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_delete BEFORE DELETE ON Holon BEGIN SELECT RAISE(ABORT, 'owned test rejection'); END;");
        var rejected = byKey
            ? (synchronous ? repository.DeleteHolon(id.ToString(), false) : await repository.DeleteHolonAsync(id.ToString(), false))
            : (synchronous ? repository.DeleteHolon(id, false) : await repository.DeleteHolonAsync(id, false));
        rejected.IsError.Should().BeTrue();
        rejected.Exception.Should().NotBeNull();
        rejected.IsDeleted.Should().BeFalse();
        context.Database.ExecuteSqlRaw("DROP TRIGGER reject_delete;");
        repository.SaveHolon(new Holon { Id = Guid.NewGuid() }).IsError.Should().BeFalse();
        repository.LoadHolon(id, loadChildren:false).Result.Id.Should().Be(id);
        var deleted = synchronous ? repository.DeleteHolon(id, false) : await repository.DeleteHolonAsync(id, false);
        deleted.IsDeleted.Should().BeTrue();
        repository.LoadHolon(id, loadChildren:false).Result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GuidDeletionReturnsDeletedRecordAndPersistsBeforeCompletion(bool synchronous)
    {
        var id = Guid.NewGuid();
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(new Holon { Id = id, IsActive = true }, saveChildren:false)).IsError.Should().BeFalse();
            var deleted = synchronous ? provider.DeleteHolon(id) : await provider.DeleteHolonAsync(id);
            deleted.IsError.Should().BeFalse(deleted.Message);
            deleted.IsDeleted.Should().BeTrue();
            deleted.DeletedCount.Should().Be(1);
            deleted.Result.Id.Should().Be(id);
            (await provider.LoadHolonAsync(id, loadChildren:false)).Result.Should().BeNull();
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded = await reopened.LoadHolonAsync(id, loadChildren:false);
        loaded.Result.Should().BeNull();
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(2, 0)]
    public async Task PublicTextSearchHonoursVersionForHolonsAndAvatars(int version, int expectedCount)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var storedVersion in new[] { 3, 4 })
        {
            var holon = new Holon { Id = Guid.NewGuid(), Version = storedVersion, Name = "owned-version-search" };
            var avatar = new Avatar { Id = Guid.NewGuid(), Version = storedVersion, Username = "owned-version-search-" + storedVersion,
                Email = "owned-version-search-" + storedVersion + "@test.invalid",
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
            (await provider.SaveHolonAsync(holon, saveChildren:false)).IsError.Should().BeFalse();
            var savedAvatar = await provider.SaveAvatarAsync(avatar);
            savedAvatar.IsError.Should().BeFalse(savedAvatar.Message);
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false,
            SearchGroups = new() { new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = "owned-version-search" } }
        }, loadChildren:false, version:version);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.SearchResultHolons.Should().HaveCount(expectedCount);
        result.Result.SearchResultAvatars.Should().HaveCount(expectedCount);
        if (version != 0 && expectedCount > 0)
        {
            result.Result.SearchResultHolons.Should().OnlyContain(h => h.Version == version);
            result.Result.SearchResultAvatars.Should().OnlyContain(a => a.Version == version);
        }
    }

    [Theory]
    [InlineData(HolonType.Quest, 1)]
    [InlineData(HolonType.Clan, 1)]
    [InlineData(HolonType.All, 2)]
    [InlineData(HolonType.Mission, 0)]
    public async Task PublicTextSearchHonoursHolonTypeRestriction(HolonType requestedType, int expected)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var type in new[] { HolonType.Quest, HolonType.Clan })
        {
            var saved = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), HolonType = type, Name = "shared-type-needle" }, saveChildren:false);
            saved.IsError.Should().BeFalse(saved.Message);
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = "shared-type-needle",
                    SearchHolons = true, HolonType = requestedType, HolonSearchParams = new SearchHolonParams { Name = true } }
            }
        });
        result.IsError.Should().BeFalse(result.Message);
        result.Result.SearchResultHolons.Should().HaveCount(expected);
        result.Result.SearchResultAvatars.Should().BeEmpty();
        if (requestedType != HolonType.All && expected > 0)
            result.Result.SearchResultHolons.Should().OnlyContain(h => h.HolonType == requestedType);
    }

    [Theory]
    [InlineData("id", true)]
    [InlineData("provider", true)]
    [InlineData("metadata", true)]
    [InlineData("metadata-disabled", false)]
    public async Task PublicAvatarIdentityAndMetadataSearchSurvivesFreshConnection(string selection, bool expected)
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "unrelated-avatar", Email = "unrelated@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
            MetaData = new Dictionary<string,object> { ["Region"] = "avatar-owned-needle" } };
        avatar.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "avatar-provider-needle";
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var saved = await provider.SaveAvatarAsync(avatar);
            saved.IsError.Should().BeFalse(saved.Message);
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var query = selection == "id" ? avatar.Id.ToString() : selection == "provider" ? "avatar-provider-needle" : "avatar-owned-needle";
        var result = await reopened.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup {
                    SearchQuery = query, SearchAvatars = true, SearchIds = selection == "id",
                    AvatarSearchParams = new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams {
                        ProviderUniqueStorageKey = selection == "provider", MetaData = selection == "metadata" }
                }
            }
        });
        result.IsError.Should().BeFalse(result.Message);
        if (expected) result.Result.SearchResultAvatars.Select(a => a.Id).Should().Equal(avatar.Id);
        else result.Result.SearchResultAvatars.Should().BeEmpty();
        result.Result.SearchResultHolons.Should().BeEmpty();
        result.Result.NumberOfResults.Should().Be(expected ? 1 : 0);
    }

    [Theory]
    [InlineData("id", true)]
    [InlineData("provider", true)]
    [InlineData("metadata", true)]
    [InlineData("metadata-wrong-key", false)]
    [InlineData("metadata-disabled", false)]
    public async Task PublicTextSearchHonoursIdentityProviderAndSelectedMetadata(string selection, bool expected)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var holon = new Holon { Id = Guid.NewGuid(), Name = "unrelated-name",
            MetaData = new Dictionary<string,object> { ["Region"] = "owned-needle" } };
        holon.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "owned-provider-needle";
        var saved = await provider.SaveHolonAsync(holon, saveChildren:false);
        saved.IsError.Should().BeFalse(saved.Message);
        var query = selection == "id" ? holon.Id.ToString() : selection == "provider" ? "owned-provider-needle" : "owned-needle";
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = query, SearchHolons = true,
                    SearchIds = selection == "id", SearchProviderKeys = selection == "provider",
                    HolonSearchParams = new SearchHolonParams { MetaData = selection.StartsWith("metadata") && selection != "metadata-disabled",
                        MetaDataKey = selection == "metadata-wrong-key" ? "Missing" : "Region" } }
            }
        });
        result.IsError.Should().BeFalse(result.Message);
        if (expected) result.Result.SearchResultHolons.Select(h => h.Id).Should().Equal(holon.Id);
        else result.Result.SearchResultHolons.Should().BeEmpty();
        result.Result.SearchResultAvatars.Should().BeEmpty();
    }

    [Fact]
    public async Task PublicTextSearchDoesNotMatchUnselectedFieldsOrTargets()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var selected = new Avatar { Id = Guid.NewGuid(), Username = "other", Email = "needle@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        var excluded = new Avatar { Id = Guid.NewGuid(), Username = "needle", Email = "other@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        foreach (var avatar in new[] { selected, excluded }) (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "needle" }, saveChildren:false)).IsError.Should().BeFalse();
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = "needle", SearchAvatars = true,
                    AvatarSearchParams = new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams { Email = true } }
            }
        });
        result.IsError.Should().BeFalse(result.Message);
        result.Result.SearchResultAvatars.Select(a => a.Id).Should().Equal(selected.Id);
        result.Result.SearchResultHolons.Should().BeEmpty();
    }

    [Fact]
    public async Task PublicSearchDoesNotPretendUnsupportedGroupsSucceeded()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchGroupBase()
            }
        });
        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Exception.Should().BeOfType<NotSupportedException>();
    }

    [Theory]
    [InlineData(true, false, true, 2, 0)]
    [InlineData(false, true, true, 0, 2)]
    [InlineData(true, true, false, 0, 0)]
    public async Task PublicDateSearchHonoursTargetsAndUnionsSelectedFields(bool holons, bool avatars, bool fields, int expectedHolons, int expectedAvatars)
    {
        var pivot = new DateTime(2026, 1, 2, 12, 30, 0, DateTimeKind.Utc);
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var matchCreated in new[] { true, false })
        {
            var created = matchCreated ? pivot : pivot.AddDays(1);
            var modified = matchCreated ? pivot.AddDays(1) : pivot;
            var savedHolon = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), CreatedDate = created, ModifiedDate = modified }, saveChildren:false);
            savedHolon.IsError.Should().BeFalse(savedHolon.Message);
            var savedAvatar = await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Username = "date-target-" + matchCreated,
                Email = matchCreated + "@test.invalid", CreatedDate = created, ModifiedDate = modified,
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) });
            savedAvatar.IsError.Should().BeFalse(savedAvatar.Message);
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchDateGroup {
                    Date = pivot, DateOperator = SearchOperatorType.EqualTo, SearchHolons = holons, SearchAvatars = avatars,
                    HolonSearchParams = new SearchHolonParams { CreatedDate = fields, ModifiedDate = fields },
                    AvatarSearchParams = new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams { CreatedDate = fields, ModifiedDate = fields }
                }
            }
        });
        result.IsError.Should().BeFalse(result.Message);
        result.Result.SearchResultHolons.Should().HaveCount(expectedHolons);
        result.Result.SearchResultAvatars.Should().HaveCount(expectedAvatars);
        result.Result.NumberOfResults.Should().Be(expectedHolons + expectedAvatars);
    }

    [Fact]
    public async Task PublicSearchRejectsInvalidDateOperatorOnEmptyStorage()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchGroups = new() { new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchDateGroup { DateOperator = (SearchOperatorType)999 } }
        });
        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Exception.Should().BeOfType<ArgumentException>();
    }

    [Theory]
    [InlineData(SearchOperatorType.LessThan, 1)]
    [InlineData(SearchOperatorType.LessThanOrEqualTo, 2)]
    [InlineData(SearchOperatorType.EqualTo, 1)]
    [InlineData(SearchOperatorType.NotEqualTo, 2)]
    [InlineData(SearchOperatorType.GreaterThan, 1)]
    [InlineData(SearchOperatorType.GreaterThanOrEqualTo, 2)]
    public async Task PublicDateSearchHonoursEveryComparisonAndSelectedAuditField(SearchOperatorType operation, int expected)
    {
        var pivot = new DateTime(2026, 1, 2, 12, 30, 0, DateTimeKind.Utc);
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var offset in new[] { -1, 0, 1 })
        {
            var savedHolon = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), CreatedDate = pivot.AddDays(offset),
                ModifiedDate = pivot.AddYears(1) }, saveChildren:false);
            savedHolon.IsError.Should().BeFalse(savedHolon.Message);
            var savedAvatar = await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Username = "date-" + offset,
                Email = offset + "@test.invalid", CreatedDate = pivot.AddYears(1), ModifiedDate = pivot.AddDays(offset),
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) });
            savedAvatar.IsError.Should().BeFalse(savedAvatar.Message);
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchDateGroup {
                    Date = pivot, DateOperator = operation, SearchHolons = true, SearchAvatars = true,
                    HolonSearchParams = new SearchHolonParams { CreatedDate = true },
                    AvatarSearchParams = new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams { ModifiedDate = true }
                }
            }
        }, loadChildren:false);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.SearchResultHolons.Should().HaveCount(expected);
        result.Result.SearchResultAvatars.Should().HaveCount(expected);
        result.Result.NumberOfResults.Should().Be(expected * 2);
    }

    [Theory]
    [InlineData(SearchParamGroupOperator.And, true, false, true, 1, 0)]
    [InlineData(SearchParamGroupOperator.And, false, true, true, 0, 1)]
    [InlineData(SearchParamGroupOperator.Or, true, false, true, 3, 2)]
    [InlineData(SearchParamGroupOperator.Or, false, true, true, 2, 3)]
    [InlineData(SearchParamGroupOperator.And, true, true, false, 0, 0)]
    [InlineData(SearchParamGroupOperator.Or, true, true, false, 2, 2)]
    public async Task PublicMixedSearchHonoursNumericTargetsAndFieldSelection(SearchParamGroupOperator operation,
        bool searchHolons, bool searchAvatars, bool selectVersion, int expectedHolons, int expectedAvatars)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var version in new[] { 2, 3, 4 })
        {
            var text = version == 3 ? "beta" : "alpha";
            var savedHolon = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Version = version, Name = text }, saveChildren:false);
            savedHolon.IsError.Should().BeFalse(savedHolon.Message);
            var savedAvatar = await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Version = version,
                Username = text + version, Email = version + "@test.invalid",
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) });
            savedAvatar.IsError.Should().BeFalse(savedAvatar.Message);
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = "alpha" },
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchNumberGroup {
                    Number = 3, NumberOperator = SearchOperatorType.GreaterThanOrEqualTo, PreviousSearchGroupOperator = operation,
                    SearchHolons = searchHolons, SearchAvatars = searchAvatars,
                    HolonSearchParams = new SearchHolonParams { Version = selectVersion },
                    AvatarSearchParams = new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams { Version = selectVersion }
                }
            }
        }, loadChildren:false);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.SearchResultHolons.Should().HaveCount(expectedHolons);
        result.Result.SearchResultAvatars.Should().HaveCount(expectedAvatars);
        result.Result.NumberOfResults.Should().Be(expectedHolons + expectedAvatars);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublicSearchRejectsInvalidOperatorsEvenOnEmptyStorage(bool numeric)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var groups = new List<NextGenSoftware.OASIS.API.Core.Objects.Search.ISearchGroupBase>();
        if (numeric) groups.Add(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchNumberGroup { NumberOperator = (SearchOperatorType)999 });
        else {
            groups.Add(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = "missing" });
            groups.Add(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { PreviousSearchGroupOperator = (SearchParamGroupOperator)999 });
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams { SearchGroups = groups });
        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Exception.Should().BeOfType<ArgumentException>();
    }

    [Theory]
    [InlineData(SearchOperatorType.LessThan, 1)]
    [InlineData(SearchOperatorType.LessThanOrEqualTo, 2)]
    [InlineData(SearchOperatorType.EqualTo, 1)]
    [InlineData(SearchOperatorType.NotEqualTo, 2)]
    [InlineData(SearchOperatorType.GreaterThan, 1)]
    [InlineData(SearchOperatorType.GreaterThanOrEqualTo, 2)]
    public async Task PublicNumericSearchHonoursEveryVersionComparison(SearchOperatorType operation, int expected)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var version in new[] { 2, 3, 4 })
        {
            var holon = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Version = version }, saveChildren:false);
            holon.IsError.Should().BeFalse(holon.Message);
            var avatar = await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Version = version,
                Username = "owned-numeric-" + version, Email = version + "@test.invalid",
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) });
            avatar.IsError.Should().BeFalse(avatar.Message);
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchNumberGroup {
                    Number = 3, NumberOperator = operation, SearchHolons = true, SearchAvatars = true,
                    HolonSearchParams = new SearchHolonParams { Version = true },
                    AvatarSearchParams = new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams { Version = true }
                }
            }
        }, loadChildren:false);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.SearchResultHolons.Should().HaveCount(expected);
        result.Result.SearchResultAvatars.Should().HaveCount(expected);
        result.Result.NumberOfResults.Should().Be(expected * 2);
    }

    [Theory]
    [InlineData(SearchParamGroupOperator.Or, 3)]
    [InlineData(SearchParamGroupOperator.And, 1)]
    public async Task PublicTextSearchCombinesGroupsAndReportsExactCount(SearchParamGroupOperator operation, int expected)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var text in new[] { "alpha", "beta", "alpha-beta", "neither" })
        {
            var savedHolon = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = text }, saveChildren:false);
            savedHolon.IsError.Should().BeFalse(savedHolon.Message);
            var savedAvatar = await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Username = text,
                Email = text + "@test.invalid", AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) });
            savedAvatar.IsError.Should().BeFalse(savedAvatar.Message);
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            SearchOnlyForCurrentAvatar = false, SearchGroups = new() {
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = "ALPHA" },
                new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = "beta", PreviousSearchGroupOperator = operation }
            }
        }, loadChildren:false);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.SearchResultHolons.Should().HaveCount(expected);
        result.Result.SearchResultAvatars.Should().HaveCount(expected);
        result.Result.NumberOfResults.Should().Be(expected * 2);
        result.Result.SearchResultHolons.Select(h => h.Id).Should().OnlyHaveUniqueItems();
        result.Result.SearchResultAvatars.Select(a => a.Id).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task PublicSearchHonoursParentScopeThroughNonMatchingAncestors(bool recursive, int expected)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = new Holon { Id = Guid.NewGuid(), Name = "owned-scope-match" };
        var direct = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "owned-scope-match" };
        var bridge = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "non-matching-bridge" };
        var grandchild = new Holon { Id = Guid.NewGuid(), ParentHolonId = bridge.Id, Name = "owned-scope-match" };
        // Deliberate cycle: the search root must never become its own descendant.
        root.ParentHolonId = grandchild.Id;
        var unrelated = new Holon { Id = Guid.NewGuid(), ParentHolonId = Guid.NewGuid(), Name = "owned-scope-match" };
        var deleted = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "owned-scope-match", DeletedDate = DateTime.UtcNow };
        var behindDeleted = new Holon { Id = Guid.NewGuid(), ParentHolonId = deleted.Id, Name = "owned-scope-match" };
        foreach (var holon in new[] { root, direct, bridge, grandchild, unrelated, deleted, behindDeleted })
        {
            var saved = await provider.SaveHolonAsync(holon, saveChildren:false);
            saved.IsError.Should().BeFalse(saved.Message);
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            ParentId = root.Id, Recursive = recursive, SearchOnlyForCurrentAvatar = false,
            SearchGroups = new() { new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = "owned-scope-match" } }
        }, loadChildren:false);
        result.IsError.Should().BeFalse(result.Message);
        var ids = result.Result.SearchResultHolons.Select(h => h.Id).ToList();
        ids.Should().HaveCount(expected);
        ids.Should().Contain(direct.Id);
        ids.Should().NotContain(new[] { root.Id, unrelated.Id, deleted.Id, behindDeleted.Id });
        if (recursive) ids.Should().Contain(grandchild.Id);
        else ids.Should().NotContain(grandchild.Id);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, null)]
    [InlineData(true, "")]
    [InlineData(false, "")]
    [InlineData(true, " ")]
    [InlineData(false, " ")]
    public async Task EmptyTokenSelectorsCannotReturnAnAvatarWithUnsetTokens(bool synchronous, string invalid)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Username = "unset-tokens", Email = "unset@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) })).IsError.Should().BeFalse();
        foreach (var result in new[] {
            synchronous ? provider.LoadAvatarByVerificationToken(invalid) : await provider.LoadAvatarByVerificationTokenAsync(invalid),
            synchronous ? provider.LoadAvatarByResetToken(invalid) : await provider.LoadAvatarByResetTokenAsync(invalid),
            synchronous ? provider.LoadAvatarByRefreshToken(invalid) : await provider.LoadAvatarByRefreshTokenAsync(invalid)
        }) {
            result.IsError.Should().BeTrue();
            result.IsLoaded.Should().BeFalse();
            result.Result.Should().BeNull();
            result.Exception.Should().BeAssignableTo<ArgumentException>();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AmbiguousTokenSelectorsDoNotChooseTheFirstAvatar(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var index in new[] { 1, 2 })
            (await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Username = "ambiguous-" + index,
                Email = index + "@ambiguous.invalid", VerificationToken = "same-verification", ResetToken = "same-reset",
                RefreshTokens = new() { new RefreshToken { Token = "same-refresh" } },
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) })).IsError.Should().BeFalse();
        foreach (var result in new[] {
            synchronous ? provider.LoadAvatarByVerificationToken("same-verification") : await provider.LoadAvatarByVerificationTokenAsync("same-verification"),
            synchronous ? provider.LoadAvatarByResetToken("same-reset") : await provider.LoadAvatarByResetTokenAsync("same-reset"),
            synchronous ? provider.LoadAvatarByRefreshToken("same-refresh") : await provider.LoadAvatarByRefreshTokenAsync("same-refresh")
        }) {
            result.IsError.Should().BeTrue();
            result.IsLoaded.Should().BeFalse();
            result.Result.Should().BeNull();
            result.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
            result.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UsernameAndEmailAmbiguityUsesTerminalClassification(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var version in new[] { 3, 4 })
            (await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Version = version, Username = "shared-owned-identity",
                Email = "shared@test.invalid", AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) })).IsError.Should().BeFalse();
        foreach (var result in new[] {
            synchronous ? provider.LoadAvatarByUsername("shared-owned-identity") : await provider.LoadAvatarByUsernameAsync("shared-owned-identity"),
            synchronous ? provider.LoadAvatarByEmail("shared@test.invalid") : await provider.LoadAvatarByEmailAsync("shared@test.invalid")
        }) {
            result.IsError.Should().BeTrue();
            result.Result.Should().BeNull();
            result.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
            result.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
        }
        foreach (var result in new[] {
            synchronous ? provider.LoadAvatarByUsername("shared-owned-identity",3) : await provider.LoadAvatarByUsernameAsync("shared-owned-identity",3),
            synchronous ? provider.LoadAvatarByEmail("shared@test.invalid",3) : await provider.LoadAvatarByEmailAsync("shared@test.invalid",3)
        }) {
            result.IsError.Should().BeFalse(result.Message);
            result.Result.Version.Should().Be(3);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GenericUsernameAndCredentialSelectorsRejectAmbiguity(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var version in new[] { 3, 4 })
            (await provider.SaveAvatarAsync(new Avatar { Id = Guid.NewGuid(), Version = version, Username = "duplicate-owned-login",
                Email = version + "@login.invalid", Password = "owned-fixture-password",
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) })).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString);
        var repository = new AvatarRepository(context);
        foreach (var result in new[] {
            synchronous ? repository.LoadAvatar("duplicate-owned-login") : await repository.LoadAvatarAsync("duplicate-owned-login"),
            synchronous ? repository.LoadAvatar("duplicate-owned-login","owned-fixture-password") : await repository.LoadAvatarAsync("duplicate-owned-login","owned-fixture-password")
        }) {
            result.IsError.Should().BeTrue();
            result.Result.Should().BeNull();
            result.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
            result.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
        }
        var valid = synchronous ? repository.LoadAvatar("duplicate-owned-login","owned-fixture-password",3)
            : await repository.LoadAvatarAsync("duplicate-owned-login","owned-fixture-password",3);
        valid.IsError.Should().BeFalse(valid.Message);
        valid.Result.Version.Should().Be(3);
        var wrong = synchronous ? repository.LoadAvatar("duplicate-owned-login","wrong-owned-password")
            : await repository.LoadAvatarAsync("duplicate-owned-login","wrong-owned-password");
        wrong.IsError.Should().BeFalse();
        wrong.Result.Should().BeNull();
        wrong.IsLoaded.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarDetailUsernameEmailAmbiguityIsClassifiedBeforeMapping(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var version in new[] { 3, 4 })
            (await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = Guid.NewGuid(), Version = version,
                Username = "shared-owned-detail", Email = "shared-detail@test.invalid", Karma = version })).IsError.Should().BeFalse();
        foreach (var result in new[] {
            synchronous ? provider.LoadAvatarDetailByUsername("shared-owned-detail") : await provider.LoadAvatarDetailByUsernameAsync("shared-owned-detail"),
            synchronous ? provider.LoadAvatarDetailByEmail("shared-detail@test.invalid") : await provider.LoadAvatarDetailByEmailAsync("shared-detail@test.invalid")
        }) {
            result.IsError.Should().BeTrue();
            result.Result.Should().BeNull();
            result.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
            result.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
        }
        var selected = synchronous ? provider.LoadAvatarDetailByUsername("shared-owned-detail",3)
            : await provider.LoadAvatarDetailByUsernameAsync("shared-owned-detail",3);
        selected.IsError.Should().BeFalse(selected.Message);
        selected.Result.Karma.Should().Be(3);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task AmbiguousUsernameEmailDeletionPreservesEveryAvatarAndDetail(bool synchronous, bool softDelete)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        foreach (var id in ids) {
            (await provider.SaveAvatarAsync(new Avatar { Id = id, Username = "shared-delete-owned", Email = "shared-delete@test.invalid",
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) })).IsError.Should().BeFalse();
            (await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = id, Username = "shared-delete-owned", Karma = 123 })).IsError.Should().BeFalse();
        }
        foreach (var result in new[] {
            synchronous ? provider.DeleteAvatarByUsername("shared-delete-owned",softDelete) : await provider.DeleteAvatarByUsernameAsync("shared-delete-owned",softDelete),
            synchronous ? provider.DeleteAvatarByEmail("shared-delete@test.invalid",softDelete) : await provider.DeleteAvatarByEmailAsync("shared-delete@test.invalid",softDelete)
        }) {
            result.IsError.Should().BeTrue();
            result.Result.Should().BeFalse();
            result.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
            result.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
        }
        foreach (var id in ids) {
            (await provider.LoadAvatarAsync(id)).Result.DeletedDate.Should().Be(DateTime.MinValue);
            var detail = (await provider.LoadAvatarDetailAsync(id)).Result;
            detail.DeletedDate.Should().Be(DateTime.MinValue);
            detail.Karma.Should().Be(123);
        }
    }

    [Theory]
    [InlineData(true, true, 0)]
    [InlineData(true, true, 1)]
    [InlineData(true, true, 2)]
    [InlineData(true, false, 0)]
    [InlineData(true, false, 1)]
    [InlineData(true, false, 2)]
    [InlineData(false, true, 0)]
    [InlineData(false, true, 1)]
    [InlineData(false, true, 2)]
    [InlineData(false, false, 0)]
    [InlineData(false, false, 1)]
    [InlineData(false, false, 2)]
    public async Task StandaloneDetailDeletionPreservesAvatarAndOtherOwnedGraphs(bool synchronous, bool softDelete, int selector)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var target = new AvatarDetail { Id = Guid.NewGuid(), Username = "owned-detail-target", Email = "target@detail.invalid", Karma = 123 };
        var other = new AvatarDetail { Id = Guid.NewGuid(), Username = "owned-detail-other", Email = "other@detail.invalid", Karma = 456 };
        foreach (var detail in new[] { target, other }) {
            detail.Stats.HP.Current = 37;
            detail.Inventory = new List<IInventoryItem> { new InventoryItem { Id = Guid.NewGuid(), Name = detail.Username, Quantity = 3 } };
            (await provider.SaveAvatarAsync(new Avatar { Id = detail.Id, Username = detail.Username, Email = detail.Email,
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) })).IsError.Should().BeFalse();
            (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        }
        IOASISStorageProvider store = provider;
        var deleted = selector switch {
            0 => synchronous ? store.DeleteAvatarDetail(target.Id, softDelete) : await store.DeleteAvatarDetailAsync(target.Id, softDelete),
            1 => synchronous ? store.DeleteAvatarDetailByUsername(target.Username, softDelete) : await store.DeleteAvatarDetailByUsernameAsync(target.Username, softDelete),
            _ => synchronous ? store.DeleteAvatarDetailByEmail(target.Email, softDelete) : await store.DeleteAvatarDetailByEmailAsync(target.Email, softDelete)
        };
        deleted.IsError.Should().BeFalse(deleted.Message);
        deleted.Result.Should().BeTrue();
        deleted.IsDeleted.Should().BeTrue();
        (await store.LoadAvatarAsync(target.Id)).Result.DeletedDate.Should().Be(DateTime.MinValue);
        var preserved = (await store.LoadAvatarDetailAsync(other.Id)).Result;
        preserved.Karma.Should().Be(456);
        preserved.Inventory.Single().Name.Should().Be(other.Username);
        preserved.Stats.HP.Current.Should().Be(37);
        var loadedTarget = (await store.LoadAvatarDetailAsync(target.Id)).Result;
        if (softDelete) { loadedTarget.IsActive.Should().BeFalse(); loadedTarget.DeletedDate.Should().NotBe(DateTime.MinValue); }
        else loadedTarget.Should().BeNull();
        using var context = new DataContext(TestConnectionString);
        context.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.AvatarStatsModel>().Count().Should().Be(softDelete ? 2 : 1);
        context.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.InventoryItemModel>().Count().Should().Be(softDelete ? 2 : 1);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task RejectedStandaloneDetailDeletionRollsBackWithoutDeferredReplay(bool synchronous, bool softDelete)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var target = new AvatarDetail { Id = Guid.NewGuid(), Username = "owned-rejected-detail", Karma = 123 };
        target.Inventory = new List<IInventoryItem> { new InventoryItem { Id = Guid.NewGuid(), Name = "owned-preserved-seed" } };
        (await provider.SaveAvatarDetailAsync(target)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString);
        if (softDelete) context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_detail_delete BEFORE UPDATE ON AvatarDetail BEGIN SELECT RAISE(ABORT, 'owned detail deletion rejection'); END;");
        else context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_detail_delete BEFORE DELETE ON AvatarDetail BEGIN SELECT RAISE(ABORT, 'owned detail deletion rejection'); END;");
        IOASISStorageProvider store = provider;
        var rejected = synchronous ? store.DeleteAvatarDetail(target.Id, softDelete) : await store.DeleteAvatarDetailAsync(target.Id, softDelete);
        rejected.IsError.Should().BeTrue();
        rejected.Result.Should().BeFalse();
        rejected.Exception.Should().NotBeNull();
        context.Database.ExecuteSqlRaw("DROP TRIGGER reject_detail_delete;");
        (await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = Guid.NewGuid(), Username = "unrelated-next-detail", Karma = 456 })).IsError.Should().BeFalse();
        var retained = (await provider.LoadAvatarDetailAsync(target.Id)).Result;
        retained.Karma.Should().Be(123);
        retained.DeletedDate.Should().Be(DateTime.MinValue);
        retained.Inventory.Single().Name.Should().Be("owned-preserved-seed");
        retained.Stats.Should().NotBeNull();
        context.AvatarDetails.AsNoTracking().Count().Should().Be(2);
    }

    [Fact]
    public async Task GenericAsyncUsernameLoaderFiltersBeforeMapping()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Version = 3, Username = "generic-owned", Email = "generic@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString);
        var repository = new AvatarRepository(context);
        (await repository.LoadAvatarAsync(avatar.Username, 3)).Result.Id.Should().Be(avatar.Id);
        (await repository.LoadAvatarAsync(avatar.Username, 2)).Result.Should().BeNull();
        (await repository.LoadAvatarAsync("absent-owned", 3)).IsLoaded.Should().BeFalse();
        context.Database.ExecuteSqlRaw("DROP TABLE Avatar;");
        (await repository.LoadAvatarAsync(avatar.Username, 3)).Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
    }

    [Fact]
    public async Task RefreshTokenLogicalIdsMayRepeatAcrossAvatarsWithoutStorageCollisions()
    {
        var avatars = new[] { Guid.NewGuid(), Guid.NewGuid() };
        using (var provider = new SQLLiteDBOASIS(TestConnectionString)) {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            foreach (var id in avatars) {
                var saved = await provider.SaveAvatarAsync(new Avatar { Id = id, Username = id.ToString(), Email = id + "@test.invalid",
                    AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
                    RefreshTokens = new() { new RefreshToken { Id = 7, Token = "owned-cross-token-" + id } } });
                saved.IsError.Should().BeFalse(saved.Message);
            }
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var id in avatars) {
            var loaded = await reopened.LoadAvatarByRefreshTokenAsync("owned-cross-token-" + id);
            loaded.Result.Id.Should().Be(id);
            loaded.Result.RefreshTokens.Single().Id.Should().Be(7);
        }
        using var context = new DataContext(TestConnectionString);
        context.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.RefreshTokenModel>().Select(t => t.StorageId).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectedRefreshTokenGraphDoesNotLeakIntoNextAvatarSave(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString);
        context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_owned_token BEFORE INSERT ON RefreshTokens BEGIN SELECT RAISE(ABORT, 'owned token rejection'); END;");
        var failed = new Avatar { Id = Guid.NewGuid(), Username = "rejected-token-avatar", Email = "rejected@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
            RefreshTokens = new() { new RefreshToken { Token = "rejected-owned-token" } } };
        var rejected = synchronous ? provider.SaveAvatar(failed) : await provider.SaveAvatarAsync(failed);
        rejected.IsError.Should().BeTrue();
        rejected.IsSaved.Should().BeFalse();
        rejected.Exception.Should().NotBeNull();
        context.Database.ExecuteSqlRaw("DROP TRIGGER reject_owned_token;");
        var good = new Avatar { Id = Guid.NewGuid(), Username = "next-token-avatar", Email = "next@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        var saved = synchronous ? provider.SaveAvatar(good) : await provider.SaveAvatarAsync(good);
        saved.IsError.Should().BeFalse(saved.Message);
        context.Avatars.AsNoTracking().Select(a => a.Id).Should().Equal(good.Id.ToString());
        context.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.RefreshTokenModel>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshTokenCollectionsPersistRotateAndDeleteWithTheirAvatar(bool synchronous)
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), Version = 3, Username = "collection-owned", Email = "collection@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
            RefreshTokens = new() { new RefreshToken { Id = 1, Token = "collection-first", Expires = DateTime.UtcNow.AddDays(1) },
                new RefreshToken { Id = 2, Token = "collection-second", CreatedByIp = "127.0.0.1" } } };
        using (var provider = new SQLLiteDBOASIS(TestConnectionString)) {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var saved = synchronous ? provider.SaveAvatar(avatar) : await provider.SaveAvatarAsync(avatar);
            saved.IsError.Should().BeFalse(saved.Message);
        }
        using (var provider = new SQLLiteDBOASIS(TestConnectionString)) {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded = synchronous ? provider.LoadAvatarByRefreshToken("collection-first",3) : await provider.LoadAvatarByRefreshTokenAsync("collection-first",3);
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Id.Should().Be(avatar.Id);
            loaded.Result.RefreshTokens.Should().HaveCount(2);
            loaded.Result.RefreshTokens.Select(t => t.Id).Should().OnlyHaveUniqueItems();
            loaded.Result.RefreshTokens.RemoveAll(t => t.Token == "collection-first");
            loaded.Result.RefreshTokens[0].RevokedByIp = "127.0.0.2";
            loaded.Result.RefreshTokens.Add(new RefreshToken { Id = 3, Token = "collection-third" });
            var saved = synchronous ? provider.SaveAvatar(loaded.Result) : await provider.SaveAvatarAsync(loaded.Result);
            saved.IsError.Should().BeFalse(saved.Message);
            saved.Result.RefreshTokens.Select(t => t.Token).Should().BeEquivalentTo("collection-second","collection-third");
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await reopened.LoadAvatarByRefreshTokenAsync("collection-first")).Result.Should().BeNull();
        var rotated = await reopened.LoadAvatarByRefreshTokenAsync("collection-third",3);
        rotated.Result.RefreshTokens.Single(t => t.Token == "collection-second").RevokedByIp.Should().Be("127.0.0.2");
        (await reopened.LoadAvatarByRefreshTokenAsync("collection-third",2)).Result.Should().BeNull();
        var deleted = synchronous ? reopened.DeleteAvatar(avatar.Id,false) : await reopened.DeleteAvatarAsync(avatar.Id,false);
        deleted.IsError.Should().BeFalse(deleted.Message);
        using var context = new DataContext(TestConnectionString);
        context.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.RefreshTokenModel>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarTokenSelectorsRespectVersionAndPreserveFailures(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Version = 3, Username = "token-owned", Email = "tokens@test.invalid",
            VerificationToken = "verification-owned", ResetToken = "reset-owned", RefreshToken = "refresh-owned",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        async Task<OASISResult<IAvatar>[]> Read(int version) => new[] {
            synchronous ? provider.LoadAvatarByVerificationToken(avatar.VerificationToken, version) : await provider.LoadAvatarByVerificationTokenAsync(avatar.VerificationToken, version),
            synchronous ? provider.LoadAvatarByResetToken(avatar.ResetToken, version) : await provider.LoadAvatarByResetTokenAsync(avatar.ResetToken, version),
            synchronous ? provider.LoadAvatarByRefreshToken(avatar.RefreshToken, version) : await provider.LoadAvatarByRefreshTokenAsync(avatar.RefreshToken, version)
        };
        foreach (var version in new[] { 0, 2, 3 })
        foreach (var result in await Read(version)) {
            result.IsError.Should().BeFalse(result.Message);
            result.IsLoaded.Should().Be(version != 2);
            if (version == 2) result.Result.Should().BeNull();
            else result.Result.Id.Should().Be(avatar.Id);
        }
        using var context = new DataContext(TestConnectionString);
        context.Database.ExecuteSqlRaw("DROP TABLE Avatar;");
        foreach (var result in await Read(3)) {
            result.IsError.Should().BeTrue();
            result.IsLoaded.Should().BeFalse();
            result.Exception.Should().BeOfType<Microsoft.Data.Sqlite.SqliteException>();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarActiveAndDerivedVerificationStateSurviveFreshConnection(bool synchronous)
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "owned-state-avatar", Email = "state@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User), IsActive = false,
            Verified = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc) };
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var saved = synchronous ? provider.SaveAvatar(avatar) : await provider.SaveAvatarAsync(avatar);
            saved.IsError.Should().BeFalse(saved.Message);
        }
        using (var reopened = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded = synchronous ? reopened.LoadAvatar(avatar.Id) : await reopened.LoadAvatarAsync(avatar.Id);
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.IsActive.Should().BeFalse();
            loaded.Result.IsVerified.Should().BeTrue();
            loaded.Result.Verified.Should().Be(avatar.Verified);
            loaded.Result.IsActive = true;
            loaded.Result.Verified = null;
            loaded.Result.PasswordReset = null;
            var saved = synchronous ? reopened.SaveAvatar(loaded.Result) : await reopened.SaveAvatarAsync(loaded.Result);
            saved.IsError.Should().BeFalse(saved.Message);
        }
        using var final = new SQLLiteDBOASIS(TestConnectionString);
        (await final.ActivateProviderAsync()).IsError.Should().BeFalse();
        var updated = await final.LoadAvatarAsync(avatar.Id);
        updated.Result.IsActive.Should().BeTrue();
        updated.Result.IsVerified.Should().BeFalse();
        using var context = new DataContext(TestConnectionString);
        context.Avatars.AsNoTracking().Single(a => a.Id == avatar.Id.ToString()).IsVerified.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarAuditFieldsSurviveSaveUpdateAndFreshConnection(bool synchronous)
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "owned-audit-avatar", Email = "audit@test.invalid",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
            CreatedByAvatarId = Guid.NewGuid(), ModifiedByAvatarId = Guid.NewGuid(), DeletedByAvatarId = Guid.NewGuid(),
            CreatedDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            ModifiedDate = new DateTime(2026, 2, 3, 4, 5, 6, DateTimeKind.Utc),
            DeletedDate = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc) };
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var saved = synchronous ? provider.SaveAvatar(avatar) : await provider.SaveAvatarAsync(avatar);
            saved.IsError.Should().BeFalse(saved.Message);
            avatar.ModifiedByAvatarId = Guid.NewGuid();
            avatar.ModifiedDate = avatar.ModifiedDate.AddDays(1);
            saved = synchronous ? provider.SaveAvatar(avatar) : await provider.SaveAvatarAsync(avatar);
            saved.IsError.Should().BeFalse(saved.Message);
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var result = synchronous ? reopened.LoadAvatar(avatar.Id) : await reopened.LoadAvatarAsync(avatar.Id);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.CreatedByAvatarId.Should().Be(avatar.CreatedByAvatarId);
        result.Result.ModifiedByAvatarId.Should().Be(avatar.ModifiedByAvatarId);
        result.Result.DeletedByAvatarId.Should().Be(avatar.DeletedByAvatarId);
        result.Result.CreatedDate.Should().Be(avatar.CreatedDate);
        result.Result.ModifiedDate.Should().Be(avatar.ModifiedDate);
        result.Result.DeletedDate.Should().Be(avatar.DeletedDate);
    }

    [Theory]
    [InlineData(MetaKeyValuePairMatchMode.All, true, 1, 1)]
    [InlineData(MetaKeyValuePairMatchMode.Any, true, 2, 1)]
    [InlineData(MetaKeyValuePairMatchMode.All, false, 2, 2)]
    [InlineData(MetaKeyValuePairMatchMode.Any, false, 3, 2)]
    public async Task PublicSearchHonoursOwnershipMetadataAndDeletedRecords(MetaKeyValuePairMatchMode mode,
        bool ownOnly, int holonCount, int avatarCount)
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var row in new[] { (owner, "match", "match", false), (owner, "match", "other", false),
            (other, "match", "match", false), (owner, "match", "match", true), (owner, "other", "other", false) })
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "owned-filter-search", CreatedByAvatarId = row.Item1,
                DeletedDate = row.Item4 ? DateTime.UtcNow : DateTime.MinValue,
                MetaData = new Dictionary<string,object> { ["Region"] = row.Item2, ["Category"] = row.Item3 } };
            var saved = await provider.SaveHolonAsync(holon, saveChildren:false);
            saved.IsError.Should().BeFalse(saved.Message);
        }
        foreach (var row in new[] { (owner, false), (other, false), (owner, true) })
        {
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "owned-filter-search-" + Guid.NewGuid(),
                Email = Guid.NewGuid() + "@test.invalid", CreatedByAvatarId = row.Item1,
                DeletedDate = row.Item2 ? DateTime.UtcNow : DateTime.MinValue,
                AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User) };
            var saved = await provider.SaveAvatarAsync(avatar);
            saved.IsError.Should().BeFalse(saved.Message);
        }
        var result = await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
            AvatarId = owner, SearchOnlyForCurrentAvatar = ownOnly, MetaKeyValuePairMatchMode = mode,
            FilterByMetaData = new() { ["Region"] = "match", ["Category"] = "match" },
            SearchGroups = new() { new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = "owned-filter-search" } }
        }, loadChildren:false);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.SearchResultHolons.Should().HaveCount(holonCount);
        result.Result.SearchResultAvatars.Should().HaveCount(avatarCount);
        result.Result.SearchResultHolons.Select(h => h.Id).Should().OnlyHaveUniqueItems();
        result.Result.SearchResultHolons.Should().OnlyContain(h => h.DeletedDate == DateTime.MinValue);
        result.Result.SearchResultAvatars.Should().OnlyContain(a => a.DeletedDate == DateTime.MinValue);
        if (ownOnly) {
            result.Result.SearchResultHolons.Should().OnlyContain(h => h.CreatedByAvatarId == owner);
            result.Result.SearchResultAvatars.Should().OnlyContain(a => a.CreatedByAvatarId == owner);
        }
    }

    [Fact]
    public async Task LatestAndExplicitVersionMetadataQueriesRespectCurrentVersionAndType()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var holon = new Holon { Id = Guid.NewGuid(), Version = 3, HolonType = HolonType.Quest,
            MetaData = new Dictionary<string,object> { ["Email"] = "query@example.invalid" } };
        (await provider.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
        provider.LoadHolon(holon.Id,loadChildren:false).Result.Should().NotBeNull();
        (await provider.LoadHolonAsync(holon.Id,loadChildren:false,version:2)).Result.Should().BeNull();
        (await provider.LoadAllHolonsAsync(loadChildren:false)).Result.Should().ContainSingle();
        (await provider.LoadHolonsByMetaDataAsync("Email","query@example.invalid",type:HolonType.Quest,loadChildren:false,version:3)).Result.Should().ContainSingle();
        (await provider.LoadHolonsByMetaDataAsync("Email","query@example.invalid",type:HolonType.Avatar,loadChildren:false,version:3)).Result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProviderKeySelectorsReturnOnlyTheirOwnedRecordAndRejectDuplicateKeys()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var first = new Holon { Id = Guid.NewGuid(), Version = 1 };
        first.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "first-local-key";
        first.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "foreign-provider-key";
        var second = new Holon { Id = Guid.NewGuid(), Version = 2 };
        second.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "second-local-key";
        (await provider.SaveHolonAsync(first,saveChildren:false)).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(second,saveChildren:false)).IsError.Should().BeFalse();
        provider.LoadHolon("second-local-key",loadChildren:false).Result.Id.Should().Be(second.Id);
        (await provider.LoadHolonAsync("first-local-key",loadChildren:false)).Result.Id.Should().Be(first.Id);
        (await provider.LoadHolonAsync("first-local-key",loadChildren:false,version:2)).Result.Should().BeNull();
        (await provider.LoadHolonAsync("foreign-provider-key",loadChildren:false)).Result.Should().BeNull();
        provider.LoadHolon("absent",loadChildren:false).Result.Should().BeNull();
        var duplicate = new Holon { Id = Guid.NewGuid() };
        duplicate.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "first-local-key";
        (await provider.SaveHolonAsync(duplicate,saveChildren:false)).IsError.Should().BeTrue();
    }

    [Fact]
    public async Task ProviderKeyDeletionUsesOnlyTheLocalKey()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var holon = new Holon { Id = Guid.NewGuid(), IsActive = true };
        holon.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "local-delete-key";
        holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "foreign-delete-key";
        (await provider.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
        provider.DeleteHolon("foreign-delete-key").IsError.Should().BeTrue();
        (await provider.LoadHolonAsync(holon.Id,loadChildren:false)).Result.IsActive.Should().BeTrue();
        (await provider.DeleteHolonAsync("local-delete-key")).IsError.Should().BeFalse();
        (await provider.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TrackedUpdatesAndRejectedWritesDoNotPoisonLaterSaves(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var original = new Holon { Id = Guid.NewGuid(), Name = "first", Version = 1 };
        original.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "owned-key";
        (await provider.SaveHolonAsync(original,saveChildren:false)).IsError.Should().BeFalse();
        (await provider.LoadHolonAsync(original.Id,loadChildren:false)).Result.Name.Should().Be("first");
        original.Name = "updated"; original.Version = 2;
        var update = synchronous ? provider.SaveHolon(original,saveChildren:false) : await provider.SaveHolonAsync(original,saveChildren:false);
        update.IsError.Should().BeFalse(update.Message);
        var rejected = new Holon { Id = Guid.NewGuid() };
        rejected.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "owned-key";
        var failure = synchronous ? provider.SaveHolon(rejected,saveChildren:false) : await provider.SaveHolonAsync(rejected,saveChildren:false);
        failure.IsError.Should().BeTrue(); failure.Exception.Should().NotBeNull();
        var next = new Holon { Id = Guid.NewGuid(), Name = "valid after failure" };
        var saved = synchronous ? provider.SaveHolon(next,saveChildren:false) : await provider.SaveHolonAsync(next,saveChildren:false);
        saved.IsError.Should().BeFalse(saved.Message);
        (await provider.LoadHolonAsync(original.Id,loadChildren:false)).Result.Name.Should().Be("updated");
        (await provider.LoadHolonAsync(rejected.Id,loadChildren:false)).Result.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedBulkWriteRollsBackAndDoesNotPoisonLaterWrites(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var held = new Holon { Id = Guid.NewGuid() };
        held.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "held-key";
        (await provider.SaveHolonAsync(held,saveChildren:false)).IsError.Should().BeFalse();
        var valid = new Holon { Id = Guid.NewGuid() };
        var conflicting = new Holon { Id = Guid.NewGuid() };
        conflicting.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "held-key";
        var batch = new IHolon[] { valid, conflicting };
        var rejected = synchronous ? provider.SaveHolons(batch,saveChildren:false) : await provider.SaveHolonsAsync(batch,saveChildren:false);
        rejected.IsError.Should().BeTrue(); rejected.Exception.Should().NotBeNull();
        (await provider.LoadHolonAsync(valid.Id,loadChildren:false)).Result.Should().BeNull();
        (await provider.LoadHolonAsync(conflicting.Id,loadChildren:false)).Result.Should().BeNull();
        var next = new IHolon[] { new Holon(), new Holon() };
        var saved = synchronous ? provider.SaveHolons(next,saveChildren:false) : await provider.SaveHolonsAsync(next,saveChildren:false);
        saved.IsError.Should().BeFalse(saved.Message);
        saved.Result.Should().HaveCount(2);
        saved.Result.Select(row => row.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
        foreach (var row in saved.Result)
            (await provider.LoadHolonAsync(row.Id,loadChildren:false)).Result.Id.Should().Be(row.Id);
    }

    [Fact]
    public async Task SQLLiteDBOASIS_Should_Initialize_Successfully()
    {
        // Arrange & Act
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var activation = await provider.ActivateProviderAsync();
        activation.IsError.Should().BeFalse(activation.Message);
        
        // Assert
        provider.Should().NotBeNull();
        provider.ProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SQLLiteDBOASIS_Should_Support_Avatar_Integration()
    {
        // Arrange
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var activation = await provider.ActivateProviderAsync();
        activation.IsError.Should().BeFalse(activation.Message);
        
        // Act & Assert
        provider.Should().NotBeNull();
        provider.ProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
        
        // Test avatar operations
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)
        };
        
        var saveResult = await provider.SaveAvatarAsync(avatar);
        saveResult.IsError.Should().BeFalse(saveResult.Message);
        saveResult.Result.Should().NotBeNull();

        var loaded = await provider.LoadAvatarByUsernameAsync(avatar.Username);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Should().NotBeNull();
        loaded.Result.Email.Should().Be(avatar.Email);

        (await provider.DeActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var afterReactivation = await provider.LoadAvatarByUsernameAsync(avatar.Username);
        afterReactivation.IsError.Should().BeFalse(afterReactivation.Message);
        afterReactivation.Result.Id.Should().Be(avatar.Id);
    }

    [Fact]
    public async Task SQLLiteDBOASIS_Should_Support_Holon_Integration()
    {
        // Arrange
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var activation = await provider.ActivateProviderAsync();
        activation.IsError.Should().BeFalse(activation.Message);
        
        // Act & Assert
        provider.Should().NotBeNull();
        provider.ProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
        
        // Test holon operations
        var holon = new Holon
        {
            Name = "Test Holon",
            Description = "Test Description"
        };
        
        var saveResult = await provider.SaveHolonAsync(holon);
        saveResult.IsError.Should().BeFalse(saveResult.Message);
        saveResult.Result.Should().NotBeNull();

        var loaded = await provider.LoadHolonAsync(saveResult.Result.Id);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Should().NotBeNull();
        loaded.Result.Name.Should().Be(holon.Name);
    }
}
