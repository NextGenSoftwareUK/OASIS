using System.Reflection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

[Collection("NFT provider registry")]
public sealed class NFTMintQuantityTests
{
    [Fact]
    public async Task InvalidFirstEntryStopsBeforeChangingOrProcessingLaterEntry()
    {
        var manager = new NFTManager((IOASISStorageProvider)null!, Guid.NewGuid(), new NextGenSoftware.OASIS.API.DNA.OASISDNA());
        var later = new MintWeb3NFTRequest { NumberToMint = 0 };
        var request = new MintWeb4NFTRequest {
            NumberToMint = 1,
            OnChainProvider = new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SuiOASIS),
            OffChainProvider = new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.LocalFileOASIS),
            NFTStandardType = new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.NFTStandardType>(NextGenSoftware.OASIS.API.Core.Enums.NFTStandardType.ERC721),
            NFTOffChainMetaType = new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.NFTOffChainMetaType>(NextGenSoftware.OASIS.API.Core.Enums.NFTOffChainMetaType.OASIS),
            Web3NFTs = new List<IMintWeb3NFTRequest> { new MintWeb3NFTRequest { NumberToMint = 1 }, later },
        };
        var result = await manager.MintNftAsync(request);
        Assert.True(result.IsError);
        Assert.Contains("SuiObject", result.Message);
        Assert.Equal(0, later.NumberToMint);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    [InlineData(-1, true)]
    [InlineData(int.MinValue, false)]
    public async Task NonpositiveDefaultCountFailsBeforeRequestExpansion(int count, bool emptyList)
    {
        var manager = new NFTManager((IOASISStorageProvider)null!, Guid.NewGuid(), new NextGenSoftware.OASIS.API.DNA.OASISDNA());
        var request = new MintWeb4NFTRequest { NumberToMint = count, Web3NFTs = emptyList ? new List<IMintWeb3NFTRequest>() : null! };
        var originalList = request.Web3NFTs;
        var result = await manager.MintNftAsync(request);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.Contains("NumberToMint", result.Message);
        Assert.Same(originalList, request.Web3NFTs);
        Assert.Equal(count, request.NumberToMint);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(5, 0)]
    [InlineData(10, 0)]
    [InlineData(5, 2)]
    [InlineData(2, 5)]
    public void DefaultRequestExpansionHasExactCountAndPreservesExplicitEntries(int requested, int existing)
    {
        var entries = Enumerable.Range(0, existing).Select(_ => (IMintWeb3NFTRequest)new MintWeb3NFTRequest { NumberToMint = 7 }).ToList();
        var request = new MintWeb4NFTRequest { NumberToMint = requested, Web3NFTs = existing == 0 ? null! : entries.ToList() };
        var method = typeof(NFTManager).GetMethod("ExpandDefaultWeb3MintRequests", BindingFlags.Static | BindingFlags.NonPublic)!;
        method.Invoke(null, new object[] { request });
        Assert.Equal(Math.Max(requested, existing), request.Web3NFTs.Count);
        Assert.All(request.Web3NFTs.Skip(existing), entry => Assert.Equal(1, entry.NumberToMint));
        Assert.Equal(existing * 7 + Math.Max(0, requested - existing), request.Web3NFTs.Sum(entry => entry.NumberToMint!.Value));
        for (var index = 0; index < existing; index++)
        {
            Assert.Same(entries[index], request.Web3NFTs[index]);
            Assert.Equal(7, request.Web3NFTs[index].NumberToMint);
        }
        method.Invoke(null, new object[] { request });
        Assert.Equal(Math.Max(requested, existing), request.Web3NFTs.Count);
    }

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
