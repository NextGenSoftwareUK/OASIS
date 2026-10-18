using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class Web3CredentialValidationTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(false,true)] [DataRow(true,false)] [DataRow(true,true)]
    public async Task ActualWrappersValidateCredentialsBeforeActivation(bool arbitrum,bool sync)
    {
        Web3CoreOASISBaseProvider provider=arbitrum
            ? new ArbitrumOASIS.ArbitrumOASIS_Web3Core("http://127.0.0.1:1",new string('1',64),"0x"+new string('1',40),31337)
            : new PolygonOASIS.PolygonOASIS("http://127.0.0.1:1",new string('1',64),"0x"+new string('1',40),31337);
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
            if(input.Item2==1) Assert.IsInstanceOfType(result.Exception,typeof(NotSupportedException));
            else Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }
}
