using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public sealed class CosmosCredentialVersionTests
{
    public static IEnumerable<object[]> Cases(){for(var method=0;method<10;method++)foreach(var version in new[]{-1,1,int.MaxValue})yield return new object[]{method,version};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task CredentialVersionRefusalPrecedesAnyRepositoryRead(int method,int version)
    {
        IOASISStorageProvider provider=new AzureCosmosDBOASIS(new Uri("https://localhost:1"),"unused","owned",new()){IsProviderActivated=true};
        var result=method switch {
            0=>await provider.LoadAvatarByVerificationTokenAsync("owned",version),1=>provider.LoadAvatarByVerificationToken("owned",version),
            2=>await provider.LoadAvatarByResetTokenAsync("owned",version),3=>provider.LoadAvatarByResetToken("owned",version),
            4=>await provider.LoadAvatarByRefreshTokenAsync("owned",version),5=>provider.LoadAvatarByRefreshToken("owned",version),
            6=>await provider.LoadAvatarByPublicKeyAsync("owned",version),7=>provider.LoadAvatarByPublicKey("owned",version),
            8=>await provider.LoadAvatarByPrivateKeyAsync("owned",version),_=>provider.LoadAvatarByPrivateKey("owned",version)
        };
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode,result.Exception?.ToString());Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
    }
}
