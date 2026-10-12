using NextGenSoftware.OASIS.API.Core.Managers;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class WalletTokenRecipientContractTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("recipient", " ")]
    [InlineData("recipient", "other")]
    [InlineData(null, "recipient")]
    public void MissingOrConflictingResolvedAddressesReject(string? first,string? second)
        =>Assert.Throws<ArgumentException>(()=>WalletManager.ResolveTokenRecipientAddresses(new[] {first!,second!}));

    [Fact]
    public void NoSelectorRejects()
        =>Assert.Throws<ArgumentException>(()=>WalletManager.ResolveTokenRecipientAddresses(Array.Empty<string>()));

    [Fact]
    public void EveryResolvedSelectorMustIdentifySameWallet()
        =>Assert.Equal("recipient",WalletManager.ResolveTokenRecipientAddresses(new[] {"recipient","recipient","recipient"}));
}
