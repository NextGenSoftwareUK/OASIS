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
    public static IEnumerable<object[]> NativeOutcomes()
    {
        foreach(var asynchronous in new[]{false,true})
        foreach(var wait in new[]{false,true})
        foreach(var outcome in new[]{"success","warning","error","submitted","rejected"})
            yield return new object[]{asynchronous,wait,outcome};
    }

    [Theory]
    [MemberData(nameof(NativeOutcomes))]
    public void StandaloneSendRetainsNativeOutcomeAndNeverResubmits(bool asynchronous,bool wait,string outcome)
    {
        var provider=new Mock<IOASISStorageProvider>();
        provider.SetupGet(x=>x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SuiOASIS));
        provider.SetupGet(x=>x.IsProviderActivated).Returns(true);
        var nft=provider.As<IOASISNFTProvider>();
        var native=new OASISResult<IWeb3NFTTransactionResponse>{
            Result=outcome=="rejected"?null:new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse{TransactionResult="original-native-id"},
            IsError=outcome is "error" or "rejected",IsWarning=outcome=="warning",WarningCount=outcome=="warning"?3:0,
            IsSaved=outcome!="rejected",SavedCount=outcome=="rejected"?0:1,ResultsCount=1,
            ErrorCode="native-code",Message="native-"+outcome,DetailedMessage="original-native-detailed-message",
            Exception=outcome=="submitted"?new SubmittedException():null
        };
        native.MetaData["native-evidence"]="retained";
        native.InnerMessages.Add("original-native-detail");
        int calls=0;
        OASISResult<IWeb3NFTTransactionResponse> Submit()
        {
            if(++calls>1) throw new InvalidOperationException("A second submission is not permitted.");
            return native;
        }
        nft.Setup(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>())).Returns(Submit);
        nft.Setup(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>())).ReturnsAsync(Submit);
        var request=new SendWeb4NFTRequest{FromProvider=new EnumValue<ProviderType>(ProviderType.SuiOASIS),
            ToProvider=new EnumValue<ProviderType>(ProviderType.SuiOASIS),WaitTillNFTSent=wait,
            AttemptToSendNFTEveryXSeconds=0,WaitForNFTToSendInSeconds=60};
        Action<NFTManager> check=manager=>{
            var result=asynchronous?manager.SendNFTAsync(Guid.NewGuid(),request).GetAwaiter().GetResult():manager.SendNFT(Guid.NewGuid(),request);
            Assert.Equal(1,calls);
            Assert.Equal(native.IsError||outcome=="submitted",result.IsError);
            Assert.Equal(native.Result?.TransactionResult,result.Result?.SendTransactionResult);
            Assert.Equal(native.Message,result.Message);Assert.Equal(native.ErrorCode,result.ErrorCode);
            Assert.Equal(native.DetailedMessage,result.DetailedMessage);
            Assert.Same(native.Exception,result.Exception);
            Assert.Equal(native.IsWarning,result.IsWarning);Assert.Equal(native.WarningCount,result.WarningCount);
            Assert.Equal(native.IsSaved,result.IsSaved);Assert.Equal(native.SavedCount,result.SavedCount);
            Assert.Equal(native.ResultsCount,result.ResultsCount);
            Assert.Equal("retained",result.MetaData["native-evidence"]);
            Assert.Contains("original-native-detail",result.InnerMessages);
            Assert.Equal(outcome is "error" or "rejected",native.IsError);
        };
        typeof(NFTProviderActivationTests).GetMethod("WithRegistry",BindingFlags.NonPublic|BindingFlags.Static)!
            .Invoke(null,new object[]{provider.Object,check});
        nft.Verify(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Never():Times.Once());
        nft.Verify(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Once():Times.Never());
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(true,true)]
    public void ConfirmedSendReturnsIdentityWithOrWithoutOptionalNftMetadata(bool asynchronous,bool metadata)
    {
        var provider=new Mock<IOASISStorageProvider>();
        provider.SetupGet(x=>x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SuiOASIS));
        provider.SetupGet(x=>x.IsProviderActivated).Returns(true);
        var nft=provider.As<IOASISNFTProvider>();
        var response=new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse {
            TransactionResult="confirmed-send",Web3NFT=metadata?new NextGenSoftware.OASIS.API.Core.Objects.NFT.Web3NFT():null };
        var success=new OASISResult<IWeb3NFTTransactionResponse>{Result=response};
        nft.Setup(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>())).Returns(success);
        nft.Setup(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>())).ReturnsAsync(success);
        var request=new SendWeb4NFTRequest { FromProvider=new EnumValue<ProviderType>(ProviderType.SuiOASIS),
            ToProvider=new EnumValue<ProviderType>(ProviderType.SuiOASIS) };
        Action<NFTManager> check=manager=>{
            var result=asynchronous?manager.SendNFTAsync(Guid.NewGuid(),request).GetAwaiter().GetResult():manager.SendNFT(Guid.NewGuid(),request);
            Assert.False(result.IsError,result.Message);Assert.Equal("confirmed-send",result.Result.SendTransactionResult);
            if(metadata)Assert.Equal("confirmed-send",response.Web3NFT.SendNFTTransactionHash);
            else Assert.Null(response.Web3NFT);
        };
        typeof(NFTProviderActivationTests).GetMethod("WithRegistry",BindingFlags.NonPublic|BindingFlags.Static)!
            .Invoke(null,new object[]{provider.Object,check});
        nft.Verify(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Never():Times.Once());
        nft.Verify(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Once():Times.Never());
    }

    private sealed class SubmittedException : Exception, INativeMutationReconciliationRequired
    {
        public string OriginalTransactionId => "original-submission";
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PresentationFailureCannotDiscardAnObservedReceipt(bool asynchronous)
    {
        var provider=new Mock<IOASISStorageProvider>();
        provider.SetupGet(x=>x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SuiOASIS));
        provider.SetupGet(x=>x.IsProviderActivated).Returns(true);
        var nft=provider.As<IOASISNFTProvider>();
        var presentationError=new InvalidOperationException("Injected optional metadata presentation failure.");
        var metadata=new Mock<NextGenSoftware.OASIS.API.Core.Interfaces.NFT.IWeb3NFT>();
        metadata.SetupSet(x=>x.SendNFTTransactionHash=It.IsAny<string>()).Throws(presentationError);
        var native=new OASISResult<IWeb3NFTTransactionResponse>{IsSaved=true,SavedCount=1,
            Result=new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse{
                TransactionResult="observed-before-formatting",Web3NFT=metadata.Object}};
        native.MetaData["native-evidence"]="retained";
        nft.Setup(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>())).Returns(native);
        nft.Setup(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>())).ReturnsAsync(native);
        var request=new SendWeb4NFTRequest{FromProvider=new EnumValue<ProviderType>(ProviderType.SuiOASIS),
            ToProvider=new EnumValue<ProviderType>(ProviderType.SuiOASIS)};
        Action<NFTManager> check=manager=>{
            var result=asynchronous?manager.SendNFTAsync(Guid.NewGuid(),request).GetAwaiter().GetResult():manager.SendNFT(Guid.NewGuid(),request);
            Assert.True(result.IsError);Assert.Same(presentationError,result.Exception);
            Assert.Equal("observed-before-formatting",result.Result.SendTransactionResult);
            Assert.True(result.IsSaved);Assert.Equal(1,result.SavedCount);
            Assert.Equal("retained",result.MetaData["native-evidence"]);
            Assert.False(native.IsError);
        };
        typeof(NFTProviderActivationTests).GetMethod("WithRegistry",BindingFlags.NonPublic|BindingFlags.Static)!
            .Invoke(null,new object[]{provider.Object,check});
        nft.Verify(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Never():Times.Once());
        nft.Verify(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Once():Times.Never());
    }

    [Theory]
    [InlineData(false,"missing-response")] [InlineData(true,"missing-response")]
    [InlineData(false,"missing-result")] [InlineData(true,"missing-result")]
    [InlineData(false,"missing-id")] [InlineData(true,"missing-id")]
    public void MissingDeliveryIdentityCannotEnableWaitLoopReplay(bool asynchronous,string mode)
    {
        var provider=new Mock<IOASISStorageProvider>();
        provider.SetupGet(x=>x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SuiOASIS));
        provider.SetupGet(x=>x.IsProviderActivated).Returns(true);
        var nft=provider.As<IOASISNFTProvider>();
        OASISResult<IWeb3NFTTransactionResponse>? response=mode=="missing-response"?null:new() {
            Result=mode=="missing-result"?null:new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse() };
        if(response!=null)
        {
            response.IsSaved=true;response.SavedCount=1;response.ResultsCount=1;
            response.ErrorCode="native-missing-identity";response.MetaData["native-evidence"]="retained";
        }
        nft.Setup(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>())).Returns(response!);
        nft.Setup(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>())).ReturnsAsync(response!);
        var request=new SendWeb4NFTRequest{FromProvider=new EnumValue<ProviderType>(ProviderType.SuiOASIS),WaitTillNFTSent=true,
            AttemptToSendNFTEveryXSeconds=0,WaitForNFTToSendInSeconds=1};
        Action<NFTManager> check=manager=>{
            var result=asynchronous?manager.SendNFTAsync(Guid.NewGuid(),request).GetAwaiter().GetResult():manager.SendNFT(Guid.NewGuid(),request);
            Assert.True(result.IsError);Assert.Null(result.Result);
            var error=Assert.IsType<NFTDeliveryReconciliationException>(result.Exception);
            Assert.Null(error.OriginalTransactionId);
            if(response!=null)
            {
                Assert.True(result.IsSaved);Assert.Equal(1,result.SavedCount);Assert.Equal(1,result.ResultsCount);
                Assert.Equal(response.ErrorCode,result.ErrorCode);Assert.Equal("retained",result.MetaData["native-evidence"]);
                Assert.False(response.IsError);
            }
        };
        typeof(NFTProviderActivationTests).GetMethod("WithRegistry",BindingFlags.NonPublic|BindingFlags.Static)!
            .Invoke(null,new object[]{provider.Object,check});
        nft.Verify(x=>x.SendNFT(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Never():Times.Once());
        nft.Verify(x=>x.SendNFTAsync(It.IsAny<ISendWeb3NFTRequest>()),asynchronous?Times.Once():Times.Never());
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
