using System.Numerics;
using System.Reflection;
using System.Text;
using Google.Protobuf;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNftMintSafetyTests
{
    [Theory]
    [InlineData(Core.Enums.ProviderType.EthereumOASIS,false)] [InlineData(Core.Enums.ProviderType.EthereumOASIS,true)]
    [InlineData(Core.Enums.ProviderType.SolanaOASIS,false)] [InlineData(Core.Enums.ProviderType.SolanaOASIS,true)]
    [InlineData(Core.Enums.ProviderType.None,false)] [InlineData(Core.Enums.ProviderType.None,true)]
    public async Task MintRejectsContradictoryExplicitChainBeforeWalletLookup(Core.Enums.ProviderType chain,bool synchronous)
    {
        var provider=new TRONOASIS();
        var request=new Core.Objects.NFT.Requests.MintWeb3NFTRequest {Amount=1,NFTStandardType=Core.Enums.NFTStandardType.TRC721,OnChainProvider=chain};
        var result=synchronous?provider.MintNFT(request):await provider.MintNFTAsync(request);
        Assert.True(result.IsError);Assert.Null(result.Result);
        Assert.Equal("OnChainProvider",Assert.IsType<ArgumentException>(result.Exception).ParamName);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData(Core.Enums.ProviderType.TRONOASIS,false)] [InlineData(Core.Enums.ProviderType.TRONOASIS,true)]
    [InlineData(Core.Enums.ProviderType.Default,false)] [InlineData(Core.Enums.ProviderType.Default,true)]
    public async Task NativeAndDefaultChainContinueToExistingMinterValidation(Core.Enums.ProviderType chain,bool synchronous)
    {
        var provider=new TRONOASIS();
        var request=new Core.Objects.NFT.Requests.MintWeb3NFTRequest {Amount=1,NFTStandardType=Core.Enums.NFTStandardType.TRC721,OnChainProvider=chain};
        var result=synchronous?provider.MintNFT(request):await provider.MintNFTAsync(request);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Contains("MintedByAvatarId",result.Message);
        Assert.False(provider.IsProviderActivated);
    }

    public static IEnumerable<object[]> IncompatibleStandards()=>from standard in new[]{0,1,2,3,4,5,999}
        from synchronous in new[]{false,true} select new object[]{standard,synchronous};
    [Theory][MemberData(nameof(IncompatibleStandards))]
    public async Task MintRejectsIncompatibleExplicitStandardBeforeWalletLookup(int standard,bool synchronous)
    {
        var provider=new TRONOASIS();
        var request=new Core.Objects.NFT.Requests.MintWeb3NFTRequest {NFTStandardType=(Core.Enums.NFTStandardType)standard};
        var result=synchronous?provider.MintNFT(request):await provider.MintNFTAsync(request);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(provider.IsProviderActivated);
        Assert.Equal("NFTStandardType",Assert.IsType<ArgumentException>(result.Exception).ParamName);
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task OmittedAndNativeStandardContinueToExistingMinterValidation(bool explicitStandard,bool synchronous)
    {
        var provider=new TRONOASIS();
        var request=new Core.Objects.NFT.Requests.MintWeb3NFTRequest {
            Amount=1,NFTStandardType=explicitStandard?Core.Enums.NFTStandardType.TRC721:null};
        var result=synchronous?provider.MintNFT(request):await provider.MintNFTAsync(request);
        Assert.True(result.IsError);Assert.Null(result.Result);
        Assert.Contains("MintedByAvatarId",result.Message);Assert.False(provider.IsProviderActivated);
    }

    private static byte[] Encode(BigInteger id, string uri)
    {
        var address = new byte[21];
        address[0] = 0x41;
        address[20] = 1;
        return (byte[])typeof(TronNativeTransferClient).GetMethod("EncodeNftMint", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { ByteString.CopyFrom(address), id, uri })!;
    }

    [Theory]
    [InlineData("0")]
    [InlineData("9007199254740993")]
    public void MintPreservesExplicitIdentityRecipientAndUnicodeUri(string token)
    {
        var id = BigInteger.Parse(token);
        var uri = "ipfs://test/🌳/metadata.json";
        var data = Encode(id, uri);
        Assert.Equal(new Nethereum.Util.Sha3Keccack().CalculateHash("mint(address,uint256,string)")[..8].ToUpperInvariant(),
            Convert.ToHexString(data.AsSpan(0, 4)));
        Assert.Equal(BigInteger.One, new BigInteger(data.AsSpan(4, 32), true, true));
        Assert.Equal(id, new BigInteger(data.AsSpan(36, 32), true, true));
        Assert.Equal(new BigInteger(96), new BigInteger(data.AsSpan(68, 32), true, true));
        var bytes = Encoding.UTF8.GetBytes(uri);
        Assert.Equal(new BigInteger(bytes.Length), new BigInteger(data.AsSpan(100, 32), true, true));
        Assert.Equal(bytes, data.AsSpan(132, bytes.Length).ToArray());
        Assert.All(data.Skip(132 + bytes.Length), b => Assert.Equal((byte)0, b));
    }

    [Fact]
    public void InvalidMintInputsCannotBeEncoded()
    {
        Assert.IsType<ArgumentOutOfRangeException>(Assert.Throws<TargetInvocationException>(() => Encode(-1, "ipfs://test")).InnerException);
        Assert.IsType<ArgumentOutOfRangeException>(Assert.Throws<TargetInvocationException>(() => Encode(BigInteger.One << 256, "ipfs://test")).InnerException);
        Assert.IsType<ArgumentException>(Assert.Throws<TargetInvocationException>(() => Encode(0, "")).InnerException);
        Assert.IsType<EncoderFallbackException>(Assert.Throws<TargetInvocationException>(() => Encode(0, "\uD800")).InnerException);
        Assert.IsType<ArgumentException>(Assert.Throws<TargetInvocationException>(() => Encode(0, new string('a', 8193))).InnerException);
    }

    [Fact]
    public async Task MissingMintRequestReturnsErrorBeforeActivation()
    {
        var provider = new TRONOASIS();
        var result = await provider.MintNFTAsync(null!);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }
}
