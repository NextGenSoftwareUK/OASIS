using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class AzureNearbyTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task OfficialBlobRecordsSupportInvariantNearbyQueries(bool avatars)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_AZURE_NATIVE_TEST"));
        var names=new[] {"oasis-nearby-holons","oasis-nearby-avatars","oasis-nearby-details"};
        var service=new BlobServiceClient("UseDevelopmentStorage=true");
        foreach(var name in names) Assert.IsFalse((await service.GetBlobContainerClient(name).ExistsAsync()).Value);
        var provider=new AzureStorageOASIS("UseDevelopmentStorage=true",names[0],names[1],names[2]);var verified=false;
        try
        {
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
            var records=new List<IHolonBase>();
            for(var i=0;i<4;i++)
            {
                IHolonBase record=avatars ? new Avatar {Username="nearby-"+i,FirstName="complete profile"}:new Holon();
                record.Id=Guid.NewGuid();record.Description="complete record";record.IsActive=i!=2;
                record.HolonType=avatars ? HolonType.Avatar:HolonType.Park;
                record.MetaData=new Dictionary<string,object> {{"proof","preserved"}};
                if(i!=3) {record.MetaData["Latitude"]=i==1 ? "30.002":"30.0005";record.MetaData["Longitude"]="20";}
                records.Add(record);
                if(avatars) {var saved=await provider.SaveAvatarAsync((IAvatar)record);Assert.IsFalse(saved.IsError,saved.Message);}
                else {var saved=await provider.SaveHolonAsync((IHolon)record,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);}
            }
            var prior=System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("fr-FR");
                if(avatars)
                {
                    var near=provider.GetAvatarsNearMe(30000000,20000000,100);
                    Assert.IsFalse(near.IsError,near.Message);Assert.IsTrue(near.IsLoaded);
                    var found=near.Result.Single();Assert.AreEqual(records[0].Id,found.Id);Assert.AreEqual("complete profile",found.FirstName);
                    Assert.AreEqual("preserved",found.MetaData["proof"].ToString());
                    Assert.AreEqual(0,provider.GetAvatarsNearMe(30000000,20000000,0).Result.Count());
                    records[0].MetaData["Latitude"]="NaN";
                    var saved=await provider.SaveAvatarAsync((IAvatar)records[0]);Assert.IsFalse(saved.IsError,saved.Message);
                    var corrupt=provider.GetAvatarsNearMe(30000000,20000000,100);
                    Assert.IsTrue(corrupt.IsError);Assert.IsNull(corrupt.Result);Assert.IsFalse(corrupt.IsLoaded);Assert.IsInstanceOfType(corrupt.Exception,typeof(InvalidDataException));
                }
                else
                {
                    var near=provider.GetHolonsNearMe(30000000,20000000,100,HolonType.Park);
                    Assert.IsFalse(near.IsError,near.Message);Assert.IsTrue(near.IsLoaded);
                    var found=near.Result.Single();Assert.AreEqual(records[0].Id,found.Id);Assert.AreEqual("complete record",found.Description);
                    Assert.AreEqual(0,provider.GetHolonsNearMe(30000000,20000000,100,HolonType.Holon).Result.Count());
                    Assert.AreEqual(0,provider.GetHolonsNearMe(30000000,20000000,0,HolonType.All).Result.Count());
                    records[0].MetaData.Remove("Longitude");
                    var saved=await provider.SaveHolonAsync((IHolon)records[0],saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);
                    var corrupt=provider.GetHolonsNearMe(30000000,20000000,100,HolonType.All);
                    Assert.IsTrue(corrupt.IsError);Assert.IsNull(corrupt.Result);Assert.IsFalse(corrupt.IsLoaded);Assert.IsInstanceOfType(corrupt.Exception,typeof(InvalidDataException));
                }
            }
            finally {System.Globalization.CultureInfo.CurrentCulture=prior;}
            verified=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(verified) foreach(var name in names) await service.GetBlobContainerClient(name).DeleteIfExistsAsync();
        }
    }
}
