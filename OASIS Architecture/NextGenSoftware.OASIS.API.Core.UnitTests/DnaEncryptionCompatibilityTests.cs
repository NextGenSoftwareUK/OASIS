using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class DnaEncryptionCompatibilityTests
{
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
