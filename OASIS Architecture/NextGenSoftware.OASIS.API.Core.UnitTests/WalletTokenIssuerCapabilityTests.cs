using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
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

public sealed class WalletTokenIssuerCapabilityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ProviderWithoutIssuanceIsRejectedBeforeAnything(bool asynchronous)
    {
        var provider=new Mock<IOASISBlockchainStorageProvider>();
        provider.SetupGet(p=>p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.HashgraphOASIS));
        var manager=Manager(provider.Object);
        var request=Request(ProviderType.HashgraphOASIS);
        var result=asynchronous ? await manager.CreateTokenAsync(request):manager.CreateToken(request);
        Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Null(result.Result);
        Assert.Equal("OASIS_TOKEN_ISSUER_CAPABILITY_REQUIRED",result.ErrorCode);
        Assert.IsType<NotSupportedException>(result.Exception);
        provider.VerifyGet(p=>p.ProviderType,Times.AtLeastOnce);provider.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task IssuerReceivesAnExactProviderRequest(bool asynchronous)
    {
        var issuer=new Mock<IOASISTokenIssuerProvider>();
        issuer.SetupGet(p=>p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SolanaOASIS));
        ICreateWeb3TokenRequest? seen=null;
        var created=new OASISResult<ICreateTokenResponse>(new CreateTokenResponse {TokenAddress="owned-mint",TransactionResult="owned-signature"});
        issuer.Setup(p=>p.CreateTokenAsync(It.IsAny<ICreateWeb3TokenRequest>())).Callback<ICreateWeb3TokenRequest>(r=>seen=r).ReturnsAsync(created);
        issuer.Setup(p=>p.CreateToken(It.IsAny<ICreateWeb3TokenRequest>())).Callback<ICreateWeb3TokenRequest>(r=>seen=r).Returns(created);
        var request=Request(ProviderType.SolanaOASIS);
        var result=asynchronous ? await Manager(issuer.Object).CreateTokenAsync(request):Manager(issuer.Object).CreateToken(request);
        Assert.Same(created,result);
        Assert.NotNull(seen);Assert.IsNotAssignableFrom<ICreateWeb4TokenRequest>(seen);
        Assert.Equal(request.CreatedByAvatarId,seen!.CreatedByAvatarId);Assert.Equal(request.AuthorityWalletAddress,seen.AuthorityWalletAddress);
        Assert.Equal(request.Decimals,seen.Decimals);Assert.Equal(request.InitialSupply,seen.InitialSupply);Assert.Equal(request.MemoText,seen.MemoText);
    }

    [Theory]
    [InlineData("avatar")] [InlineData("authority")] [InlineData("decimals")] [InlineData("supply")] [InlineData("provider")]
    public async Task InvalidRequestsNeverReachTheIssuer(string invalid)
    {
        var issuer=new Mock<IOASISTokenIssuerProvider>();
        issuer.SetupGet(p=>p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SolanaOASIS));
        var request=Request(ProviderType.SolanaOASIS);
        if(invalid=="avatar") request.CreatedByAvatarId=Guid.Empty;
        if(invalid=="authority") request.AuthorityWalletAddress=" ";
        if(invalid=="decimals") request.Decimals=19;
        if(invalid=="supply") request.InitialSupply=-1;
        if(invalid=="provider") request.ProviderType=null!;
        var result=await Manager(issuer.Object).CreateTokenAsync(request);
        Assert.True(result.IsError);Assert.Null(result.Result);
        issuer.Verify(p=>p.CreateTokenAsync(It.IsAny<ICreateWeb3TokenRequest>()),Times.Never);
    }

    private static WalletManager Manager(IOASISProvider provider)
    {
        var dna=new OASISDNA();var providers=new ProviderManager(null,dna);providers.RegisterProvider(provider);
        return new WalletManager(null,dna,providers);
    }

    private static CreateWeb4TokenRequest Request(ProviderType type)=>new()
    {
        ProviderType=new EnumValue<ProviderType>(type),CreatedByAvatarId=Guid.NewGuid(),AuthorityWalletAddress="owned-authority",
        Decimals=6,InitialSupply=1000m,MemoText="owned memo"
    };
}
