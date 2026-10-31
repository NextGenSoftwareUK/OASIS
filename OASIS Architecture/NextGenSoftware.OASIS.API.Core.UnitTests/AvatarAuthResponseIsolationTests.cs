using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class AvatarAuthResponseIsolationTests
{
    [Theory]
    [InlineData("null-map")][InlineData("null-list")][InlineData("null-wallet")][InlineData("interface-wallet")]
    public void NullAndCustomWalletShapesRemainSafeAndDoNotMutateSource(string shape)
    {
        var dna=new OASISDNA();dna.OASIS.Security=new SecuritySettings();
        var manager=new AvatarManager(null,dna,new ProviderManager(null,dna));
        var mock=new Moq.Mock<IProviderWallet>();mock.SetupAllProperties();
        mock.Object.PrivateKey="source-key";mock.Object.SecretRecoveryPhrase="source-seed";mock.Object.WalletAddress="source-address";
        var source=new Avatar{ProviderWallets=shape=="null-map"?null!:new(){[ProviderType.TRONOASIS]=
            shape=="null-list"?null!:new(){shape=="null-wallet"?null!:mock.Object}}};
        var response=manager.HideAuthDetails(source);
        Assert.Equal("source-key",mock.Object.PrivateKey);Assert.Equal("source-seed",mock.Object.SecretRecoveryPhrase);
        if(shape=="null-map")Assert.Null(response.ProviderWallets);
        else if(shape=="null-list")Assert.Null(response.ProviderWallets[ProviderType.TRONOASIS]);
        else if(shape=="null-wallet")Assert.Null(Assert.Single(response.ProviderWallets[ProviderType.TRONOASIS]));
        else{
            var wallet=Assert.Single(response.ProviderWallets[ProviderType.TRONOASIS]);
            Assert.Null(wallet.PrivateKey);Assert.Null(wallet.SecretRecoveryPhrase);Assert.Equal("source-address",wallet.WalletAddress);
        }
    }
    [Fact]
    public void ConfiguredTokenHidingAlsoPreservesSource()
    {
        var dna=new OASISDNA();dna.OASIS.Security=new SecuritySettings{HideVerificationToken=true,HideRefreshTokens=true};
        var source=new Avatar{VerificationToken="source-verification",RefreshTokens=new(){new RefreshToken{Token="source-refresh"}}};
        var manager=new AvatarManager(null,dna,new ProviderManager(null,dna));
        var response=manager.HideAuthDetails(source);
        Assert.Null(response.VerificationToken);Assert.Null(response.RefreshTokens);
        Assert.Equal("source-verification",source.VerificationToken);Assert.Single(source.RefreshTokens);
    }
    public static IEnumerable<object[]> Flags()=>from password in new[]{false,true} from keys in new[]{false,true}
        from verification in new[]{false,true} from refresh in new[]{false,true} select new object[]{password,keys,verification,refresh};
    [Theory][MemberData(nameof(Flags))]
    public void RedactionChangesOnlyResponseAndNeverStoredCredentials(bool password,bool keys,bool verification,bool refresh)
    {
        var dna=new OASISDNA();dna.OASIS.Security=new SecuritySettings();
        var manager=new AvatarManager(null,dna,new ProviderManager(null,dna));
        var wallet=new ProviderWallet{Id=Guid.NewGuid(),AvatarId=Guid.NewGuid(),WalletAddress="public-address",PrivateKey="stored-ciphertext",
            SecretRecoveryPhrase="stored-seed",IsDefaultWallet=true,ProviderType=ProviderType.TRONOASIS,Version=7};
        var source=new Avatar{Id=wallet.AvatarId,Username="public-user",Password="stored-password",VerificationToken="stored-verification",
            RefreshTokens=new(){new RefreshToken{Token="stored-refresh"}},ProviderWallets=new(){[ProviderType.TRONOASIS]=new(){wallet}}};
        var tokens=source.RefreshTokens;var wallets=source.ProviderWallets;
        var result=manager.HideAuthDetails(source,password,keys,verification,refresh);
        Assert.Equal("stored-password",source.Password);Assert.Equal("stored-verification",source.VerificationToken);
        Assert.Same(tokens,source.RefreshTokens);Assert.Same(wallets,source.ProviderWallets);
        Assert.Equal("stored-ciphertext",wallet.PrivateKey);Assert.Equal("stored-seed",wallet.SecretRecoveryPhrase);
        Assert.Equal(password?null:"stored-password",result.Password);
        Assert.Equal(verification?null:"stored-verification",result.VerificationToken);
        Assert.Equal(refresh?null:tokens,result.RefreshTokens);
        var responseWallet=Assert.Single(result.ProviderWallets[ProviderType.TRONOASIS]);
        Assert.Equal(keys?null:"stored-ciphertext",responseWallet.PrivateKey);
        Assert.Equal(keys?null:"stored-seed",responseWallet.SecretRecoveryPhrase);
        Assert.Equal(wallet.Id,responseWallet.Id);Assert.Equal(7,responseWallet.Version);Assert.True(responseWallet.IsDefaultWallet);
        Assert.Equal("public-user",result.Username);Assert.Equal(source.Id,result.Id);
    }
}
