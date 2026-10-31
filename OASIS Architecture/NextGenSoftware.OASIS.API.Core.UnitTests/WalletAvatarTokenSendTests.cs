using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Rijndael256;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class WalletAvatarTokenSendTests
{
    [Theory]
    [InlineData("")]
    [InlineData("sender")]
    [InlineData("another-owner")]
    public async Task PublicAsyncSendUsesDetachedAvatarSignerAndRejectsDifferentSender(string requestedSender)
    {
        var avatarId = Guid.NewGuid();
        var encrypted = Rijndael.Encrypt("test-only-signing-key", new string('k', 32), KeySize.Aes256);
        var wallet = new ProviderWallet { Id = Guid.NewGuid(), AvatarId = avatarId, IsDefaultWallet = true,
            ProviderType = ProviderType.SuiOASIS, WalletAddress = "sender", PublicKey = "test-only-public", PrivateKey = encrypted };
        var storage = new Mock<IOASISStorageProvider>(); storage.SetupAllProperties();
        var local = storage.As<IOASISLocalStorageProvider>();
        storage.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        storage.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        storage.Object.ProviderName = "token-signer-test"; storage.Object.IsProviderActivated = true;
        storage.Setup(p => p.ActivateProvider()).Returns(new OASISResult<bool>(true));
        local.Setup(p => p.LoadProviderWalletsForAvatarByIdAsync(avatarId)).ReturnsAsync(new OASISResult<Dictionary<ProviderType,List<IProviderWallet>>>(
            new() { [ProviderType.SuiOASIS] = new() { wallet } }));
        var chain = new Mock<IOASISBlockchainStorageProvider>();
        chain.SetupGet(p => p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SuiOASIS));
        var submissions = 0;
        chain.Setup(p => p.SendTokenAsync(It.IsAny<ISendWeb3TokenRequest>())).Returns((ISendWeb3TokenRequest projected) =>
        {
            submissions++; Assert.Equal("test-only-signing-key", projected.OwnerPrivateKey);
            Assert.Equal("test-only-public", projected.OwnerPublicKey); Assert.Equal("sender", projected.FromWalletAddress);
            Assert.Equal("recipient", projected.ToWalletAddress); Assert.Equal(1m, projected.Amount);
            return Task.FromResult(new OASISResult<ITransactionResponse>(new TransactionResponse { TransactionResult = "verified-fixture-digest" }));
        });
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        dna.OASIS.Security = new SecuritySettings { OASISProviderPrivateKeys = new EncryptionSettings
            { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('k',32) } };
        dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
        var providers = new ProviderManager(null, dna); providers.RegisterProvider(storage.Object); providers.RegisterProvider(chain.Object);
        Assert.False(providers.SetAndActivateCurrentStorageProvider(storage.Object).IsError);
        var manager = new WalletManager(null, dna, providers);
        var request = new SendWeb4TokenRequest { FromProvider = new EnumValue<ProviderType>(ProviderType.SuiOASIS),
            ToProvider = new EnumValue<ProviderType>(ProviderType.SuiOASIS), FromWalletAddress = requestedSender,
            ToWalletAddress = "recipient", FromTokenAddress = "0x2::sui::SUI", Amount = 1 };
        var result = await manager.SendTokenAsync(avatarId, request);
        Assert.Equal(requestedSender == "another-owner", result.IsError);
        Assert.Equal(requestedSender == "another-owner" ? 0 : 1, submissions);
        Assert.Equal(encrypted, wallet.PrivateKey); Assert.Null(request.OwnerPrivateKey);
        Assert.Equal(requestedSender, request.FromWalletAddress);
        if (!result.IsError) Assert.Equal("verified-fixture-digest", result.Result.SendTransactionResult);
    }
}
