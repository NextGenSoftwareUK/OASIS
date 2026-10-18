using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeNearbyUsesCanonicalSdkScansAndV2Dispatch(bool avatars)
    {
        var records=new List<IHolonBase>();
        for(var i=0;i<4;i++)
        {
            IHolonBase row=avatars ? new Avatar {Username="nearby-"+i,FirstName="complete profile"}:new Holon();
            row.Id=Guid.NewGuid();row.Name="nearby";row.Description="complete record";row.IsActive=i!=2;
            row.HolonType=avatars ? HolonType.Avatar:HolonType.Park;row.MetaData=new() {{"proof","preserved"}};
            if(i!=3) {row.MetaData["Latitude"]=i==1 ? "30.002":"30.0005";row.MetaData["Longitude"]="20";}
            records.Add(row);
            if(avatars) {var saved=await _provider.SaveAvatarAsync((IAvatar)row);Assert.IsFalse(saved.IsError,saved.Message);}
            else {var saved=await _provider.SaveHolonAsync((IHolon)row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);}
        }
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
        var manager=new ProviderManager(null,dna);manager.RegisterProvider(_provider);var router=new OASISHyperDrive(manager);
        var request=new StorageOperationRequest {Operation=avatars ? "GetAvatarsNearMe":"GetHolonsNearMe",GeoLatitude=30000000,GeoLongitude=20000000,RadiusInMeters=100,HolonType=HolonType.All,PreferredProvider=ProviderType.AWSOASIS};
        var prior=System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("fr-FR");
            foreach(var synchronous in new[] {false,true})
            {
                var result=synchronous ? router.RouteRequest<IEnumerable<IHolonBase>>(request):await router.RouteRequestAsync<IEnumerable<IHolonBase>>(request);
                Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);var found=result.Result.Single();Assert.AreEqual(records[0].Id,found.Id);
                Assert.AreEqual("preserved",found.MetaData["proof"].ToString());
                if(avatars) Assert.AreEqual("complete profile",((IAvatar)found).FirstName);else Assert.AreEqual("complete record",found.Description);
            }
            records[0].MetaData.Remove("Longitude");
            if(avatars) Assert.IsFalse((await _provider.SaveAvatarAsync((IAvatar)records[0])).IsError);
            else Assert.IsFalse((await _provider.SaveHolonAsync((IHolon)records[0],saveChildren:false)).IsError);
            var corrupt=await router.RouteRequestAsync<IEnumerable<IHolonBase>>(request);
            Assert.IsTrue(corrupt.IsError);Assert.IsNull(corrupt.Result);Assert.IsFalse(corrupt.IsLoaded);Assert.IsInstanceOfType(corrupt.Exception,typeof(InvalidDataException));
        }
        finally {System.Globalization.CultureInfo.CurrentCulture=prior;}
    }
}
