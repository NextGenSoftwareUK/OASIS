using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeNearbyReadsEncryptedRecordsWithInvariantLocation(bool avatars)
    {
        var (provider,_) = await CreateActivatedProviderAsync();
        var records=new List<IHolonBase>();
        for(var i=0;i<4;i++)
        {
            IHolonBase record=avatars ? new Avatar {Username="nearby-"+i,Email="nearby"+i+"@example.invalid",FirstName="complete profile"}:new Holon();
            record.Id=Guid.NewGuid();record.Name="nearby "+i;record.Description="complete record";record.IsActive=i!=2;
            record.HolonType=avatars ? HolonType.Avatar:HolonType.Park;
            record.MetaData=new Dictionary<string,object> {{"proof","preserved"}};
            if(i!=3) {record.MetaData["Latitude"]=i==1 ? "30.002":"30.0005";record.MetaData["Longitude"]="20";}
            records.Add(record);
            if(avatars) {var saved=await provider.SaveAvatarAsync((IAvatar)record);Assert.IsFalse(saved.IsError,saved.Message);}
            else {var saved=await provider.SaveHolonAsync((IHolon)record,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);}
        }
        var net=(IOASISNETProvider)provider;var prior=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=new CultureInfo("fr-FR");
            if(avatars)
            {
                var near=net.GetAvatarsNearMe(30000000,20000000,100);
                Assert.IsFalse(near.IsError,near.Message);Assert.IsTrue(near.IsLoaded);
                var found=near.Result.Single();Assert.AreEqual(records[0].Id,found.Id);Assert.AreEqual("complete profile",found.FirstName);
                Assert.AreEqual("preserved",found.MetaData["proof"].ToString());
                Assert.AreEqual(0,net.GetAvatarsNearMe(30000000,20000000,0).Result.Count());
                records[0].MetaData["Latitude"]="NaN";
                var saved=await provider.SaveAvatarAsync((IAvatar)records[0]);Assert.IsFalse(saved.IsError,saved.Message);
                var corrupt=net.GetAvatarsNearMe(30000000,20000000,100);
                Assert.IsTrue(corrupt.IsError);Assert.IsFalse(corrupt.IsLoaded);Assert.IsNull(corrupt.Result);Assert.IsInstanceOfType(corrupt.Exception,typeof(InvalidDataException));
            }
            else
            {
                var near=net.GetHolonsNearMe(30000000,20000000,100,HolonType.Park);
                Assert.IsFalse(near.IsError,near.Message);Assert.IsTrue(near.IsLoaded);
                var found=near.Result.Single();Assert.AreEqual(records[0].Id,found.Id);Assert.AreEqual("complete record",found.Description);
                Assert.AreEqual("preserved",found.MetaData["proof"].ToString());
                Assert.AreEqual(0,net.GetHolonsNearMe(30000000,20000000,100,HolonType.Holon).Result.Count());
                Assert.AreEqual(0,net.GetHolonsNearMe(30000000,20000000,0,HolonType.All).Result.Count());
                records[0].MetaData.Remove("Longitude");
                var saved=await provider.SaveHolonAsync((IHolon)records[0],saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);
                var corrupt=net.GetHolonsNearMe(30000000,20000000,100,HolonType.All);
                Assert.IsTrue(corrupt.IsError);Assert.IsFalse(corrupt.IsLoaded);Assert.IsNull(corrupt.Result);Assert.IsInstanceOfType(corrupt.Exception,typeof(InvalidDataException));
            }
        }
        finally {CultureInfo.CurrentCulture=prior;await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(90000001L,0L,1)] [DataRow(0L,180000001L,1)] [DataRow(0L,0L,-1)]
    public void NearbyInvalidRequestFailsBeforeRpc(long latitude,long longitude,int radius)
    {
        var provider=new EthereumOASIS("http://127.0.0.1:1",PrivateKey,ChainId,"0x"+new string('1',40));
        var result=((IOASISNETProvider)provider).GetAvatarsNearMe(latitude,longitude,radius);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));Assert.IsFalse(provider.IsProviderActivated);
        var holons=((IOASISNETProvider)provider).GetHolonsNearMe(latitude,longitude,radius,HolonType.All);
        Assert.IsTrue(holons.IsError);Assert.IsInstanceOfType(holons.Exception,typeof(ArgumentOutOfRangeException));
    }

    [TestMethod]
    public void NearbyInvalidTypeFailsBeforeRpc()
    {
        var provider=new EthereumOASIS("http://127.0.0.1:1",PrivateKey,ChainId,"0x"+new string('1',40));
        var result=((IOASISNETProvider)provider).GetHolonsNearMe(0,0,100,(HolonType)int.MaxValue);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));Assert.IsFalse(provider.IsProviderActivated);
    }
}
