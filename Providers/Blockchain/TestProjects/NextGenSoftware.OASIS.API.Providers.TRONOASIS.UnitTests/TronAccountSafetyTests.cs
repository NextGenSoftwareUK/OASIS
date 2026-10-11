using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronAccountSafetyTests
{
    [Fact]
    public async Task GeneratedMnemonicRestoresTheSameSigningKeyWithoutNodeActivation()
    {
        var provider = new TRONOASIS();
        var created = await provider.CreateAccountAsync();
        Assert.False(created.IsError);
        var restored = await provider.RestoreAccountAsync(created.Result.SeedPhrase);
        Assert.False(restored.IsError);
        Assert.Equal(created.Result.PrivateKey, restored.Result.PrivateKey);
        Assert.Equal(created.Result.PublicKey, restored.Result.PublicKey);
        Assert.Equal(130, restored.Result.PublicKey.Length);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon")]
    [InlineData("not a mnemonic")]
    [InlineData("1111111111111111111111111111111111111111111111111111111111111111")]
    public async Task InvalidMnemonicCannotBecomeHashDerivedCredentials(string phrase)
    {
        var provider = new TRONOASIS();
        var restored = await provider.RestoreAccountAsync(phrase);
        Assert.True(restored.IsError);
        Assert.Null(restored.Result.PrivateKey);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task CancelledCreationDoesNotGenerateCredentials()
    {
        var result = await new TRONOASIS().CreateAccountAsync(new CancellationToken(true));
        Assert.True(result.IsError);
        Assert.Null(result.Result.PrivateKey);
    }
}
