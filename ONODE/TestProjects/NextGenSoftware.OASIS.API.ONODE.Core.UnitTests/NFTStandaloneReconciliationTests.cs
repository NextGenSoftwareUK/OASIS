using System.Reflection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

[Collection("NFT provider registry")]
public sealed class NFTStandaloneReconciliationTests
{
    private sealed class SubmittedException : Exception, INativeMutationReconciliationRequired
    {
        public string OriginalTransactionId => "original-submission";
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WaitOptionCannotReplayAReconciliationRequiredSend(bool asynchronous)
    {
        var provider=new Mock<IOASISStorageProvider>();
        provider.SetupGet(x=>x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SuiOASIS));
        provider.SetupGet(x=>x.IsProviderActivated).Returns(true);
        var nft=provider.As<IOASISNFTProvider>();
        var evidence=new SubmittedException();
        var failure=new OASISResult<IWeb3NFTTransactionResponse>{IsError=true,Exception=evidence,Message="receipt unresolved"};
        nft.Setup(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>())).Returns(failure);
        nft.Setup(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>())).ReturnsAsync(failure);
        var request=new SendWeb4NFTRequest{FromProvider=new EnumValue<ProviderType>(ProviderType.SuiOASIS),WaitTillNFTSent=true,
            AttemptToSendNFTEveryXSeconds=0,WaitForNFTToSendInSeconds=1};
        Action<NFTManager> check=manager=>{
            var result=asynchronous?manager.SendNFTAsync(Guid.NewGuid(),request).GetAwaiter().GetResult()
                :manager.SendNFT(Guid.NewGuid(),request);
            Assert.True(result.IsError);Assert.Same(evidence,result.Exception);
        };
        typeof(NFTProviderActivationTests).GetMethod("WithRegistry",BindingFlags.NonPublic|BindingFlags.Static)!
            .Invoke(null,new object[]{provider.Object,check});
        nft.Verify(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Never():Times.Once());
        nft.Verify(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Once():Times.Never());
    }
}
