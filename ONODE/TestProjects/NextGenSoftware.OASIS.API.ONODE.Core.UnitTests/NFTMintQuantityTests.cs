using System.Reflection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public sealed class NFTMintQuantityTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    public async Task EachProviderSubmissionMintsOneAndRestoresTheRequest(int? count, int amount)
    {
        var request = new MintWeb3NFTRequest { NumberToMint = count, Amount = amount };
        var response = new OASISResult<IWeb3NFTTransactionResponse>();
        var provider = new Mock<IOASISNFTProvider>(MockBehavior.Strict);
        provider.Setup(x => x.MintNFTAsync(request)).Returns((IMintWeb3NFTRequest actual) =>
        {
            Assert.Equal(1, actual.NumberToMint);
            Assert.Equal(1m, actual.Amount);
            return Task.FromResult(response);
        });

        Assert.Same(response, await Submit(provider.Object, request));
        Assert.Equal(count, request.NumberToMint);
        Assert.Equal(amount, request.Amount);
        provider.Verify(x => x.MintNFTAsync(request), Times.Once);
        provider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExceptionRestoresQuantityWithoutRetrying()
    {
        var request = new MintWeb3NFTRequest { NumberToMint = 7, Amount = 7 };
        var provider = new Mock<IOASISNFTProvider>(MockBehavior.Strict);
        provider.Setup(x => x.MintNFTAsync(request)).ThrowsAsync(new TimeoutException("uncertain receipt"));

        await Assert.ThrowsAsync<TimeoutException>(() => Submit(provider.Object, request));

        Assert.Equal(7, request.NumberToMint);
        Assert.Equal(7m, request.Amount);
        provider.Verify(x => x.MintNFTAsync(request), Times.Once);
        provider.VerifyNoOtherCalls();
    }

    private static Task<OASISResult<IWeb3NFTTransactionResponse>> Submit(IOASISNFTProvider provider, IMintWeb3NFTRequest request)
    {
        var method = typeof(NFTManager).GetMethod("MintSingleNFTAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (Task<OASISResult<IWeb3NFTTransactionResponse>>)method.Invoke(null, new object[] { provider, request })!;
    }
}
