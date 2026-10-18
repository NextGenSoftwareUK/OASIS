using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronIdBoundProviderValidationTests
{
    [Theory]
    [InlineData("id",false)] [InlineData("id",true)]
    [InlineData("owner",false)] [InlineData("owner",true)]
    [InlineData("escrow",false)] [InlineData("escrow",true)]
    [InlineData("amount",false)] [InlineData("amount",true)]
    public async Task ConfiguredProtocolRejectsIncompleteIntentWithoutAggregateFallback(string field,bool synchronous)
    {
        var escrow=new TronNet.TronECKey(new string('1',64),TronNet.TronNetwork.MainNet).GetPublicAddress();
        var provider=new TRONOASIS(idBoundEscrow:new(escrow,new string('a',64)));
        var result=field=="amount"
            ? synchronous?provider.LockToken(new Core.Objects.Wallet.Requests.LockWeb3TokenRequest{
                LockId=Guid.NewGuid(),LockedByAvatarId=Guid.NewGuid(),TokenAddress=escrow,EscrowContractAddress=escrow,Amount=0})
                :await provider.LockTokenAsync(new Core.Objects.Wallet.Requests.LockWeb3TokenRequest{
                LockId=Guid.NewGuid(),LockedByAvatarId=Guid.NewGuid(),TokenAddress=escrow,EscrowContractAddress=escrow,Amount=0})
            :synchronous?provider.UnlockToken(new Core.Objects.Wallet.Requests.UnlockWeb3TokenRequest{
                LockId=field=="id"?Guid.Empty:Guid.NewGuid(),UnlockedByAvatarId=field=="owner"?Guid.Empty:Guid.NewGuid(),
                TokenAddress=escrow,EscrowContractAddress=field=="escrow"?"different":escrow})
                :await provider.UnlockTokenAsync(new Core.Objects.Wallet.Requests.UnlockWeb3TokenRequest{
                LockId=field=="id"?Guid.Empty:Guid.NewGuid(),UnlockedByAvatarId=field=="owner"?Guid.Empty:Guid.NewGuid(),
                TokenAddress=escrow,EscrowContractAddress=field=="escrow"?"different":escrow});
        Assert.True(result.IsError);Assert.IsType<ArgumentException>(result.Exception);
        Assert.Equal("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.Null(result.Result);Assert.False(provider.IsProviderActivated);
    }
}
