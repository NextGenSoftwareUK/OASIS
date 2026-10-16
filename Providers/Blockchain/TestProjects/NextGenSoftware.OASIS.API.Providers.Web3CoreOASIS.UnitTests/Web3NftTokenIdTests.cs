using System.Globalization;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using PolygonProvider = NextGenSoftware.OASIS.API.Providers.PolygonOASIS.PolygonOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class Web3NftTokenIdTests
{
    [DataTestMethod]
    [DataRow("0")]
    [DataRow("1")]
    [DataRow("9223372036854775808")]
    [DataRow("115792089237316195423570985008687907853269984665640564039457584007913129639935")]
    public async Task NativeTokenIdsRetainFullUint256Precision(string value)
    {
        Assert.IsTrue(Web3NftTokenId.TryParse(value, out var actual));
        Assert.AreEqual(BigInteger.Parse(value, CultureInfo.InvariantCulture), actual);
        var provider = new PolygonProvider("http://127.0.0.1:18546", "", "0x0000000000000000000000000000000000000000");
        var result = await provider.SendNFTAsync(new SendWeb3NFTRequest { TokenId = value, Amount = 1 });
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("Provider is not activated. Please activate provider and try again.", result.Message);
    }

    [DataTestMethod]
    [DataRow(null)] [DataRow("")] [DataRow(" ")] [DataRow(" 1")]
    [DataRow("+1")] [DataRow("-1")] [DataRow("1.5")] [DataRow("0x01")]
    [DataRow("d08fad81-0170-4af1-928f-e822e45fe47a")]
    [DataRow("115792089237316195423570985008687907853269984665640564039457584007913129639936")]
    public async Task InvalidTokenIdsFailAtPublicBoundaryBeforeActivation(string value)
    {
        Assert.IsFalse(Web3NftTokenId.TryParse(value, out _));
        var provider = new PolygonProvider("http://127.0.0.1:18546", "", "0x0000000000000000000000000000000000000000");
        var result = await provider.SendNFTAsync(new SendWeb3NFTRequest { TokenId = value, Amount = 1 });
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("NFT token ID must be a decimal uint256 value.", result.Message);
        Assert.IsNull(result.Result);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task NullAndFractionalRequestsCannotThrowOrSilentlyTruncate()
    {
        var provider = new PolygonProvider("http://127.0.0.1:18546", "", "0x0000000000000000000000000000000000000000");
        var missing = await provider.SendNFTAsync(null!);
        Assert.IsTrue(missing.IsError);
        Assert.AreEqual("NFT transfer request is required.", missing.Message);
        foreach (var amount in new[] { -1m, 0m, 0.5m, 1.5m })
        {
            var result = await provider.SendNFTAsync(new SendWeb3NFTRequest { TokenId = "1", Amount = amount });
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("NFT transfer amount must be a positive whole number.", result.Message);
            Assert.IsNull(result.Result);
        }
    }
}
