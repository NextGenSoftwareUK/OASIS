using System.Reflection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public sealed class NFTDeliverySubmissionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderResultIsPreservedWithoutResubmission(bool failed)
    {
        var request = new SendWeb3NFTRequest { FromNFTTokenAddress = "0.0.123/7", Amount = 1 };
        var response = new Mock<IWeb3NFTTransactionResponse>();
        response.SetupGet(x => x.TransactionResult).Returns("original-transaction-id");
        var expected = new OASISResult<IWeb3NFTTransactionResponse>
            { IsError = failed, Result = response.Object, Message = "receipt pending" };
        var provider = new Mock<IOASISNFTProvider>(MockBehavior.Strict);
        provider.Setup(x => x.SendNFTAsync(request)).ReturnsAsync(expected);

        var actual = await Submit(provider.Object, request);

        Assert.Same(expected, actual);
        Assert.Equal("original-transaction-id", actual.Result.TransactionResult);
        provider.Verify(x => x.SendNFTAsync(request), Times.Once);
        provider.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingResultOrExceptionIsAnErrorWithoutResubmission(bool throws)
    {
        var request = new SendWeb3NFTRequest();
        var provider = new Mock<IOASISNFTProvider>(MockBehavior.Strict);
        if (throws)
            provider.Setup(x => x.SendNFTAsync(request)).ThrowsAsync(new TimeoutException("receipt timeout"));
        else
            provider.Setup(x => x.SendNFTAsync(request)).ReturnsAsync((OASISResult<IWeb3NFTTransactionResponse>)null!);

        var actual = await Submit(provider.Object, request);

        Assert.True(actual.IsError);
        Assert.Contains("Reconcile before retrying", actual.Message);
        Assert.True(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(actual.Exception));
        Assert.IsType<NFTDeliveryReconciliationException>(actual.Exception);
        if (throws) Assert.IsType<TimeoutException>(actual.Exception.InnerException);
        provider.Verify(x => x.SendNFTAsync(request), Times.Once);
        provider.VerifyNoOtherCalls();
    }

    private static Task<OASISResult<IWeb3NFTTransactionResponse>> Submit(IOASISNFTProvider provider, ISendWeb3NFTRequest request)
    {
        var method = typeof(NFTManager).GetMethod("SendMintedNFTOnceAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (Task<OASISResult<IWeb3NFTTransactionResponse>>)method.Invoke(null, new object[] { provider, request })!;
    }

    private sealed class OriginalEvidenceException : Exception, INativeMutationReconciliationRequired
    {
        public string OriginalTransactionId => "original-delivery";
    }

    [Fact]
    public async Task ThrownOriginalEvidenceIsPreservedExactlyWithoutAnotherSubmission()
    {
        var request=new SendWeb3NFTRequest();
        var evidence=new OriginalEvidenceException();
        var provider=new Mock<IOASISNFTProvider>(MockBehavior.Strict);
        provider.Setup(x=>x.SendNFTAsync(request)).ThrowsAsync(evidence);
        var result=await Submit(provider.Object,request);
        Assert.True(result.IsError);Assert.Same(evidence,result.Exception);
        Assert.Equal("original-delivery",((INativeMutationReconciliationRequired)result.Exception).OriginalTransactionId);
        provider.Verify(x=>x.SendNFTAsync(request),Times.Once());provider.VerifyNoOtherCalls();
    }
}
