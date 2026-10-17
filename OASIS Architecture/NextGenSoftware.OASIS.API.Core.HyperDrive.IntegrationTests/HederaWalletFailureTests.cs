using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class HederaWalletFailureTests
{
    [Theory]
    [InlineData(false, "id", false)]
    [InlineData(true, "id", false)]
    [InlineData(false, "username", false)]
    [InlineData(true, "username", false)]
    [InlineData(false, "email", false)]
    [InlineData(true, "email", false)]
    [InlineData(false, "username", true)]
    [InlineData(true, "username", true)]
    [InlineData(false, "email", true)]
    [InlineData(true, "email", true)]
    [InlineData(false,"escrow-lock",false)] [InlineData(true,"escrow-lock",false)]
    [InlineData(false,"escrow-release",false)] [InlineData(true,"escrow-release",false)]
    [InlineData(false,"escrow-associate",false)] [InlineData(true,"escrow-associate",false)]
    public async Task CorruptStoredSigningWalletRetainsOriginalFailureBeforeSdkUse(bool synchronous, string selector, bool sui)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var root = Path.Combine(directory!.FullName, "TestResults", "HederaCurrent", "wallet-failure-files");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "fixture.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var sender = Guid.Parse("478e4ef2-b442-459f-93e0-fec53c7de2f1");
        var recipient = Guid.Parse("478e4ef2-b442-459f-93e0-fec53c7de2f2");
        var path = Path.Combine(root, "wallets_" + sender + ".json");
        Assert.False(File.Exists(path), "Preserve existing fixture before retry.");
        const string corruptFixture = "{synthetic-invalid-wallet-json";
        var local = new LocalFileOASIS(filePath: root + Path.DirectorySeparatorChar, storageDirectory: root);
        Assert.False((await local.ActivateProviderAsync()).IsError);
        var created = new List<Guid>();
        try
        {
            if (selector is "username" or "email") foreach (var pair in new[] { (sender, "hedera-failure-sender"), (recipient, "hedera-failure-recipient") }) {
                Assert.Null((await local.LoadAvatarAsync(pair.Item1)).Result);
                var saved = await local.SaveAvatarAsync(new Core.Holons.Avatar { Id = pair.Item1, Username = pair.Item2,
                    Email = pair.Item2 + "@example.invalid", IsActive = true });
                Assert.False(saved.IsError, saved.Message); created.Add(pair.Item1);
            }
            var dna = new OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
            dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
            var providers = new ProviderManager(null, dna); providers.RegisterProvider(local);
            Assert.False(providers.SetAndActivateCurrentStorageProvider(local).IsError);
            var constructor = typeof(WalletManager).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single(c => c.GetParameters().Length == 4);
            var wallets = (WalletManager)constructor.Invoke(new object?[] { local, dna, providers, null });
            await File.WriteAllTextAsync(path, corruptFixture);
            var walletProvider = sui ? ProviderType.SuiOASIS : ProviderType.HashgraphOASIS;
            var expected = selector == "username"
                ? await wallets.GetAvatarDefaultWalletByUsernameAsync("hedera-failure-sender", showPrivateKeys: true, providerType: walletProvider)
                : selector == "email"
                ? await wallets.GetAvatarDefaultWalletByEmailAsync("hedera-failure-sender@example.invalid", walletProvider, showPrivateKeys: true)
                : await wallets.GetAvatarDefaultWalletByIdAsync(sender, ProviderType.HashgraphOASIS, showPrivateKeys: true);
            Assert.True(expected.IsError); Assert.NotNull(expected.Exception);
            var hedera = new HashgraphOASIS(rpcEndpoint:"http://127.0.0.1:17689",walletManager: wallets,
                tokenEscrowConfiguration:selector.StartsWith("escrow-",StringComparison.Ordinal)?new(){
                    Protocol=HederaTokenEscrowProtocol.IdBoundV1,ContractId="0.0.2001",RuntimeCodeHash="0x"+new string('1',64),GasLimit=400000,MaxFeeTinybars=100000000}:null);
            using var suiProvider = new SuiOASIS { WalletManager = wallets };
            async Task<NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse>> Escrow(){
                var id=Guid.Parse("5f31afde-1229-4e99-8088-a0034d33a557");
                if(selector=="escrow-associate")return synchronous?hedera.AssociateTokenWithEscrow(sender,"0.0.3001","0.0.2001")
                    :await hedera.AssociateTokenWithEscrowAsync(sender,"0.0.3001","0.0.2001");
                if(selector=="escrow-lock"){
                    var request=new Core.Objects.Wallet.Requests.LockWeb3TokenRequest{LockedByAvatarId=sender,LockId=id,Amount=1.23m,TokenAddress="0.0.3001",EscrowContractAddress="0.0.2001"};
                    return synchronous?hedera.LockToken(request):await hedera.LockTokenAsync(request);
                }
                var release=new Core.Objects.Wallet.Requests.UnlockWeb3TokenRequest{UnlockedByAvatarId=sender,LockId=id,TokenAddress="0.0.3001",EscrowContractAddress="0.0.2001"};
                return synchronous?hedera.UnlockToken(release):await hedera.UnlockTokenAsync(release);
            }
            var result = selector.StartsWith("escrow-",StringComparison.Ordinal)?await Escrow():sui
                ? selector == "username"
                    ? synchronous ? suiProvider.SendTransactionByUsername("hedera-failure-sender", "hedera-failure-recipient", 1m)
                        : await suiProvider.SendTransactionByUsernameAsync("hedera-failure-sender", "hedera-failure-recipient", 1m)
                    : synchronous ? suiProvider.SendTransactionByEmail("hedera-failure-sender@example.invalid", "hedera-failure-recipient@example.invalid", 1m)
                        : await suiProvider.SendTransactionByEmailAsync("hedera-failure-sender@example.invalid", "hedera-failure-recipient@example.invalid", 1m)
                : selector == "username"
                ? synchronous ? hedera.SendTransactionByUsername("hedera-failure-sender", "hedera-failure-recipient", 1m)
                    : await hedera.SendTransactionByUsernameAsync("hedera-failure-sender", "hedera-failure-recipient", 1m)
                : selector == "email"
                ? synchronous ? hedera.SendTransactionByEmail("hedera-failure-sender@example.invalid", "hedera-failure-recipient@example.invalid", 1m)
                    : await hedera.SendTransactionByEmailAsync("hedera-failure-sender@example.invalid", "hedera-failure-recipient@example.invalid", 1m)
                : synchronous ? hedera.SendTransactionById(sender, recipient, 1m)
                    : await hedera.SendTransactionByIdAsync(sender, recipient, 1m);
            Assert.True(result.IsError); Assert.Null(result.Result);
            Assert.NotNull(result.Exception); Assert.IsType(expected.Exception.GetType(), result.Exception);
            Assert.Equal(expected.ErrorCode, result.ErrorCode); Assert.Equal(expected.DetailedMessage, result.DetailedMessage);
            Assert.Equal(expected.Message, result.Message); Assert.False(hedera.IsProviderActivated);
            Assert.False(suiProvider.IsProviderActivated);
        }
        finally
        {
            try {
                if (File.Exists(path)) {
                    Assert.Equal(corruptFixture, await File.ReadAllTextAsync(path));
                    File.Delete(path);
                }
                foreach (var id in created) Assert.False((await local.DeleteAvatarAsync(id, softDelete: false)).IsError);
            } finally { await local.DeActivateProviderAsync(); }
        }
    }
}
