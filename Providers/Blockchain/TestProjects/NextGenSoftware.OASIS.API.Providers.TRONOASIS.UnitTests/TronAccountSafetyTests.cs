using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronAccountSafetyTests
{
    [Fact]
    public async Task PublicMnemonicMatchesIndependentEthersBip44Vector()
    {
        // Public test mnemonic, never a funded wallet. Verified by the cached ethers runner.
        var provider = new TRONOASIS();
        var restored = await provider.RestoreAccountAsync(
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about");
        Assert.False(restored.IsError);
        Assert.Equal("b5a4cea271ff424d7c31dc12a3e43e401df7a40d7412a15750f3f0b6b5449a28", restored.Result.PrivateKey);
        Assert.Equal("04ff21f8e64d3a3c0198edfbb7afdc79be959432e92e2f8a1984bb436a414b8edcec0345aad0c1bf7da04fd036dd7f9f617e30669224283d950fab9dd84831dc83", restored.Result.PublicKey);
        Assert.False(provider.IsProviderActivated);
    }

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
