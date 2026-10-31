using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class DnaEncryptionCompatibilityTests
{
    [Fact]
    public void PrivateLedgerKeyReferencesAreConfigurableWithoutPersistingKeyMaterial()
    {
        Assert.Equal("OASIS_COSMOS_STORAGE_ENCRYPTION_KEY",new CosmosBlockChainOASISProviderSettings().StorageEncryptionKeyEnvironmentVariable);
        Assert.Equal("OASIS_SUI_STORAGE_ENCRYPTION_KEY",new SuiOASISProviderSettings().StorageEncryptionKeyEnvironmentVariable);
        var cosmos=System.Text.Json.JsonSerializer.Deserialize<CosmosBlockChainOASISProviderSettings>("{\"StorageEncryptionKeyEnvironmentVariable\":\"CUSTOM_COSMOS_KEY\"}")!;
        var sui=System.Text.Json.JsonSerializer.Deserialize<SuiOASISProviderSettings>("{\"StorageEncryptionKeyEnvironmentVariable\":\"CUSTOM_SUI_KEY\"}")!;
        Assert.Equal("CUSTOM_COSMOS_KEY",cosmos.StorageEncryptionKeyEnvironmentVariable);
        Assert.Equal("CUSTOM_SUI_KEY",sui.StorageEncryptionKeyEnvironmentVariable);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EnabledEncryptionCannotSilentlySkipMissingKeys(bool quantum)
    {
        var settings=new EncryptionSettings { BCryptEncryptionEnabled=true,
            Rijndael256EncryptionEnabled=!quantum,QuantumEncryptionEnabled=quantum };
        Assert.Throws<InvalidOperationException>(()=>PasswordEncryptionHelper.EncryptValue("private",settings));
        Assert.Throws<InvalidOperationException>(()=>PasswordEncryptionHelper.HashPassword("password",settings));
        if(quantum)Assert.Throws<InvalidOperationException>(()=>PasswordEncryptionHelper.WrapQuantumLayer("encrypted-inner",settings));
    }
    [Fact]
    public void DecryptionCannotReturnCiphertextOnMissingKeyOrAuthenticationFailure()
    {
        var settings=new EncryptionSettings { QuantumEncryptionEnabled=true,QuantumEncryptionKey="public-test-key" };
        var encrypted=PasswordEncryptionHelper.EncryptValue("private",settings);
        Assert.Throws<InvalidOperationException>(()=>PasswordEncryptionHelper.DecryptValue(encrypted,new EncryptionSettings()));
        Assert.Throws<InvalidOperationException>(()=>PasswordEncryptionHelper.UnwrapQuantumLayer(encrypted,new EncryptionSettings()));
        var wrong=new EncryptionSettings { QuantumEncryptionEnabled=true,QuantumEncryptionKey="wrong-public-test-key" };
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(()=>PasswordEncryptionHelper.DecryptValue(encrypted,wrong));
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(()=>PasswordEncryptionHelper.UnwrapQuantumLayer(encrypted,wrong));
    }
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public void PasswordLayersVerifyOriginalWithoutReversiblePasswordStorage(bool rijndael,bool quantum)
    {
        var settings=new EncryptionSettings { BCryptEncryptionEnabled=true,
            Rijndael256EncryptionEnabled=rijndael,Rijndael256Key="public-unit-test-cbc-key",
            QuantumEncryptionEnabled=quantum,QuantumEncryptionKey="public-unit-test-gcm-key" };
        var stored=PasswordEncryptionHelper.HashPassword("test-password-only",settings);
        Assert.NotEqual("test-password-only",stored);
        Assert.True(PasswordEncryptionHelper.VerifyPassword("test-password-only",stored,settings));
        Assert.False(PasswordEncryptionHelper.VerifyPassword("wrong-password",stored,settings));
        Assert.StartsWith(quantum?"qpq:":rijndael?"r256:":"$2",stored);
    }

    [Theory]
    [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public void ReversibleDnaDataLayersRoundtripWithoutBcrypt(bool rijndael,bool quantum)
    {
        var settings=new EncryptionSettings { BCryptEncryptionEnabled=false,
            Rijndael256EncryptionEnabled=rijndael,Rijndael256Key="public-unit-test-cbc-key",
            QuantumEncryptionEnabled=quantum,QuantumEncryptionKey="public-unit-test-gcm-key" };
        const string value="{\"private\":\"tree 🌳\"}";
        var stored=PasswordEncryptionHelper.EncryptValue(value,settings);
        Assert.NotEqual(value,stored);
        Assert.Equal(value,PasswordEncryptionHelper.DecryptValue(stored,settings));
        Assert.StartsWith(quantum?"qpq:":"r256:",stored);
    }
}
