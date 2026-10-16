using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalletEscrowCapabilityTests
{
    [Theory]
    [InlineData(false,false,"id")] [InlineData(true,false,"id")]
    [InlineData(false,false,"amount")] [InlineData(true,false,"amount")]
    [InlineData(false,false,"escrow")] [InlineData(true,false,"escrow")]
    [InlineData(false,true,"id")] [InlineData(true,true,"id")]
    [InlineData(false,true,"escrow")] [InlineData(true,true,"escrow")]
    public async Task UnsupportedExplicitEscrowFieldsNeverReachLegacyProvider(bool asynchronous,bool unlock,string field)
    {
        var provider=new Mock<IOASISBlockchainStorageProvider>();
        provider.SetupGet(p=>p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.HashgraphOASIS));
        var unsafeSuccess=new OASISResult<ITransactionResponse>(new TransactionResponse {TransactionResult="must-never-be-submitted"});
        provider.Setup(p=>p.LockToken(It.IsAny<ILockWeb3TokenRequest>())).Returns(unsafeSuccess);
        provider.Setup(p=>p.LockTokenAsync(It.IsAny<ILockWeb3TokenRequest>())).ReturnsAsync(unsafeSuccess);
        provider.Setup(p=>p.UnlockToken(It.IsAny<IUnlockWeb3TokenRequest>())).Returns(unsafeSuccess);
        provider.Setup(p=>p.UnlockTokenAsync(It.IsAny<IUnlockWeb3TokenRequest>())).ReturnsAsync(unsafeSuccess);
        var dna=new OASISDNA();var providers=new ProviderManager(null,dna);providers.RegisterProvider(provider.Object);
        var manager=new WalletManager(null,dna,providers);
        var lockRequest=new LockWeb4TokenRequest {ProviderType=new EnumValue<ProviderType>(ProviderType.HashgraphOASIS),
            LockId=field=="id" ? Guid.NewGuid():Guid.Empty,Amount=field=="amount" ? 1.25m:0,
            EscrowContractAddress=field=="escrow" ? "owned-escrow":null!};
        var unlockRequest=new UnlockWeb4TokenRequest {ProviderType=lockRequest.ProviderType,
            LockId=lockRequest.LockId,EscrowContractAddress=lockRequest.EscrowContractAddress};
        var result=unlock
            ? asynchronous ? await manager.UnlockTokenAsync(unlockRequest):manager.UnlockToken(unlockRequest)
            : asynchronous ? await manager.LockTokenAsync(lockRequest):manager.LockToken(lockRequest);
        Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Null(result.Result);
        Assert.Equal("OASIS_ESCROW_CAPABILITY_REQUIRED",result.ErrorCode);
        Assert.IsType<NotSupportedException>(result.Exception);Assert.False(NativeMutationReconciliation.IsRequired(result.Exception));
        provider.Verify(p=>p.LockToken(It.IsAny<ILockWeb3TokenRequest>()),Times.Never);
        provider.Verify(p=>p.LockTokenAsync(It.IsAny<ILockWeb3TokenRequest>()),Times.Never);
        provider.Verify(p=>p.UnlockToken(It.IsAny<IUnlockWeb3TokenRequest>()),Times.Never);
        provider.Verify(p=>p.UnlockTokenAsync(It.IsAny<IUnlockWeb3TokenRequest>()),Times.Never);
    }
}
