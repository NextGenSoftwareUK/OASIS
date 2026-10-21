using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class EthereumCredentialHistoryTests
{
    private sealed class ObservedCollection : EthereumOASIS
    {
        public int RequestedVersion {get;private set;}=-1;
        public ObservedCollection():base("http://127.0.0.1:1",new string('1',64),31337,"0x"+new string('1',40)){}
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0)
        {
            RequestedVersion=version;
            return Task.FromResult(new OASISResult<IEnumerable<IAvatar>> {IsLoaded=true,Result=new[]{new Avatar {
                Id=Guid.Parse("0de2db3b-e361-45d0-ab84-341fe2a41a04"),Version=version,VerificationToken="owned-history"}}});
        }
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ArchivedCredentialsAreRejectedBeforeLoadingCollection(bool sync)
    {
        var provider=new ObservedCollection();
        foreach(var version in new[]{1,2,99})
        foreach(var kind in new[]{0,1,2,3,4}) {
            var storage=(IOASISStorageProvider)provider;
            var result=kind switch {
                0=>sync ? storage.LoadAvatarByVerificationToken("owned-history",version):await storage.LoadAvatarByVerificationTokenAsync("owned-history",version),
                1=>sync ? storage.LoadAvatarByResetToken("owned-history",version):await storage.LoadAvatarByResetTokenAsync("owned-history",version),
                2=>sync ? storage.LoadAvatarByRefreshToken("owned-history",version):await storage.LoadAvatarByRefreshTokenAsync("owned-history",version),
                3=>sync ? storage.LoadAvatarByPublicKey("owned-history",version):await storage.LoadAvatarByPublicKeyAsync("owned-history",version),
                _=>sync ? storage.LoadAvatarByPrivateKey("owned-history",version):await storage.LoadAvatarByPrivateKeyAsync("owned-history",version)};
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
            Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));
            Assert.AreEqual(-1,provider.RequestedVersion);
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }
}
