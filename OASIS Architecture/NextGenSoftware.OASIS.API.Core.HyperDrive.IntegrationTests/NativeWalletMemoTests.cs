using System.Text;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class NativeWalletMemoTests
{
    [Theory]
    [InlineData("")] [InlineData("native memo 🌳")]
    public void TextAndOriginalBytesRoundTripExactly(string text)
    {
        var memo=NativeWalletMemo.FromText(text);
        Assert.Equal(text,memo.Text);Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes(text)),memo.Base64);
        Assert.Equal(text,NativeWalletMemo.FromBase64(memo.Base64).Text);
    }
    [Fact]
    public void BinaryMemoPreservesOriginalWithoutInventingReplacementText()
    {
        var memo=NativeWalletMemo.FromBytes(new byte[]{0xff,0x00,0xfe});
        Assert.Null(memo.Text);Assert.Equal("/wD+",memo.Base64);
        Assert.Null(NativeWalletMemo.FromBase64(memo.Base64).Text);
    }
    [Theory]
    [InlineData(" AQID")] [InlineData("invalid*")]
    public void InvalidBase64IsNotAnAbsentMemo(string value)
        =>Assert.Throws<FormatException>(()=>NativeWalletMemo.FromBase64(value));
    [Fact]
    public void AbsenceAndEmptyMemoRemainDistinct()
    {
        Assert.Null(NativeWalletMemo.FromText(null!).Base64);Assert.Null(NativeWalletMemo.FromBase64(null!).Text);
        Assert.Equal("",NativeWalletMemo.FromBytes(Array.Empty<byte>()).Base64);
    }
    [Fact]
    public void InvalidTextCannotBecomeReplacementBytes()
        =>Assert.Throws<EncoderFallbackException>(()=>NativeWalletMemo.FromText("\ud800"));
}
