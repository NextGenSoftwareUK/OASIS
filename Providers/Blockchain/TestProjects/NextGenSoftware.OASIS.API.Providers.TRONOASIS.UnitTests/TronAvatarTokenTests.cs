using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronAvatarTokenTests
{
    [Theory]
    [InlineData("id", true)]
    [InlineData("id", false)]
    [InlineData("username", true)]
    [InlineData("username", false)]
    [InlineData("email", true)]
    [InlineData("email", false)]
    public async Task NativeAvatarRoutesRejectInvalidSunAmountBeforeWalletOrStorage(string selector, bool synchronous)
    {
        var provider = new TRONOASIS();
        var from = Guid.NewGuid(); var to = Guid.NewGuid();
        const decimal amount = 0.0000001m;
        var result = selector switch
        {
            "id" => synchronous ? provider.SendTransactionById(from, to, amount)
                : await provider.SendTransactionByIdAsync(from, to, amount),
            "username" => synchronous ? provider.SendTransactionByUsername("sender", "recipient", amount)
                : await provider.SendTransactionByUsernameAsync("sender", "recipient", amount),
            _ => synchronous ? provider.SendTransactionByEmail("sender@example.org", "recipient@example.org", amount)
                : await provider.SendTransactionByEmailAsync("sender@example.org", "recipient@example.org", amount)
        };
        Assert.True(result.IsError);
        Assert.IsType<ArgumentOutOfRangeException>(result.Exception);
        Assert.Contains("SUN units", result.Message);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }

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
