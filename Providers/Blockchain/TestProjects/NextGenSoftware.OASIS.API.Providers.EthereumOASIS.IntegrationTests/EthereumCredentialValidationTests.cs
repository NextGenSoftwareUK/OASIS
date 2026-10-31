using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvalidAndUnsupportedCredentialRequestsNeverActivateRpc(bool sync)
    {
        var provider=new EthereumOASIS("http://127.0.0.1:1",new string('1',64),31337,"0x"+new string('1',40));
        var storage=(IOASISStorageProvider)provider;
        foreach(var input in new[] {(" ",0),("owned-synthetic",-1),("owned-synthetic",1)})
        foreach(var kind in new[] {0,1,2,3,4})
        {
            var result=kind switch {
                0=>sync ? storage.LoadAvatarByVerificationToken(input.Item1,input.Item2):await storage.LoadAvatarByVerificationTokenAsync(input.Item1,input.Item2),
                1=>sync ? storage.LoadAvatarByResetToken(input.Item1,input.Item2):await storage.LoadAvatarByResetTokenAsync(input.Item1,input.Item2),
                2=>sync ? storage.LoadAvatarByRefreshToken(input.Item1,input.Item2):await storage.LoadAvatarByRefreshTokenAsync(input.Item1,input.Item2),
                3=>sync ? storage.LoadAvatarByPublicKey(input.Item1,input.Item2):await storage.LoadAvatarByPublicKeyAsync(input.Item1,input.Item2),
                _=>sync ? storage.LoadAvatarByPrivateKey(input.Item1,input.Item2):await storage.LoadAvatarByPrivateKeyAsync(input.Item1,input.Item2)};
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
            if(input.Item2==1) Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));
            else Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }
}
