using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronAvatarTokenTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TokenByIdRejectsMissingAvatarBeforeStorageActivation(bool synchronous)
    {
        var provider = new TRONOASIS();
        var recipient = Guid.NewGuid();
        var result = synchronous
            ? provider.SendTransactionById(Guid.Empty, recipient, 1m, "TRX")
            : await provider.SendTransactionByIdAsync(Guid.Empty, recipient, 1m, "TRX");
        Assert.True(result.IsError);
        Assert.IsType<ArgumentException>(result.Exception);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TokenByIdRejectsMissingTokenInsteadOfUsingConfiguredStorageContract(bool synchronous)
    {
        var provider = new TRONOASIS(contractAddress: "storage-is-not-the-requested-token");
        var sender = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var result = synchronous
            ? provider.SendTransactionById(sender, recipient, 1m, "")
            : await provider.SendTransactionByIdAsync(sender, recipient, 1m, "");
        Assert.True(result.IsError);
        Assert.IsType<ArgumentException>(result.Exception);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }
}
