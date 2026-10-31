using Ipfs;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public class IPFSSourceSdkCompatibilityTests
{
    [Theory]
    [InlineData("base16", "f0001feff")]
    [InlineData("BASE16", "F0001FEFF")]
    [InlineData("base32", "baaa757y")]
    public void SourceBuiltRegistryPreservesCanonicalMultibaseVectors(string algorithm, string expected)
    {
        byte[] bytes = { 0, 1, 254, 255 };
        Assert.Equal(expected, MultiBase.Encode(bytes, algorithm));
        Assert.Equal(bytes, MultiBase.Decode(expected));
    }

    [Fact]
    public void NativeKuboCidV0AndV1ShareTheSameContentIdentity()
    {
        const string original = "QmWjVxGF3RrAGEJSMdJyPnPSwVG6iGt6eUW21DxuU8PMqz";
        // Independent `kubo cid format -v 1 -b base32` output, not this codec's own result.
        const string converted = "bafybeid4w72cifyk5gd5sp22yeiwva6rckzyo6j7iqb33cwvnguc6g5dfe";
        Cid v0 = original;
        Cid v1 = converted;
        Assert.Equal(original, v0.ToString());
        Assert.Equal(converted, v1.ToString());
        Assert.Equal(v0.Hash, v1.Hash);
        Assert.Equal(v0.ContentType, v1.ContentType);
        Assert.Equal(0, v0.Version);
        Assert.Equal(1, v1.Version);
    }
}
