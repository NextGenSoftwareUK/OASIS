using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotNearbyTests
{
    private sealed class Provider():PolkadotOASIS("http://127.0.0.1:1","fixture","",null,null)
    {
        public int Activations,Reads;
        public OASISResult<bool> Activation=new(true);
        public OASISResult<IEnumerable<IAvatar>> Avatars=new(Array.Empty<IAvatar>());
        public OASISResult<IEnumerable<IHolon>> Holons=new(Array.Empty<IHolon>());
        public override Task<OASISResult<bool>> ActivateProviderAsync(){Activations++;return Task.FromResult(Activation);}
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0){Reads++;return Task.FromResult(Avatars);}
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,
            bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {Assert.IsFalse(loadChildren);Reads++;return Task.FromResult(Holons);}
    }
    [DataTestMethod]
    [DataRow(90000001L,0L,1)][DataRow(0L,180000001L,1)][DataRow(0L,0L,-1)]
    public void InvalidCoordinatesOrRadiusFailBeforeActivation(long latitude,long longitude,int radius)
    {
        using var provider=new Provider();var network=(IOASISNETProvider)provider;
        Assert.IsTrue(network.GetAvatarsNearMe(latitude,longitude,radius).IsError);
        Assert.IsTrue(network.GetHolonsNearMe(latitude,longitude,radius,HolonType.All).IsError);
        Assert.AreEqual(0,provider.Activations);Assert.AreEqual(0,provider.Reads);
    }
    [TestMethod]
    public void UndefinedHolonTypeFailsBeforeActivation()
    {
        using var provider=new Provider();
        Assert.IsTrue(((IOASISNETProvider)provider).GetHolonsNearMe(0,0,100,(HolonType)int.MaxValue).IsError);
        Assert.AreEqual(0,provider.Activations);Assert.AreEqual(0,provider.Reads);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void OriginalLocatedRecordsAreSelectedAndInactiveDeletedOrUnlocatedRecordsExcluded(bool avatars)
    {
        using var provider=new Provider();
        var near=Located(avatars,0,180);var far=Located(avatars,10,0);
        var inactive=Located(avatars,0,180);inactive.IsActive=false;
        var deleted=Located(avatars,0,180);deleted.DeletedDate=DateTime.UtcNow;
        IHolonBase unlocated=avatars?new Avatar{IsActive=true}:new Holon{IsActive=true};
        var records=new[]{near,far,inactive,deleted,unlocated};
        if(avatars)provider.Avatars=new(records.Cast<IAvatar>());
        else provider.Holons=new(records.Cast<IHolon>());
        var selected=avatars?((IOASISNETProvider)provider).GetAvatarsNearMe(0,-180000000,0).Result.Cast<IHolonBase>().ToArray():
            ((IOASISNETProvider)provider).GetHolonsNearMe(0,-180000000,0,HolonType.All).Result.Cast<IHolonBase>().ToArray();
        Assert.AreEqual(1,selected.Length);Assert.AreSame(near,selected[0]);
        Assert.AreEqual(1,provider.Activations);Assert.AreEqual(1,provider.Reads);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void UpstreamReadErrorPreservesWholeOriginalEnvelope(bool avatars)
    {
        using var provider=new Provider();var cause=new IOException("owned lookup failure");
        if(avatars){provider.Avatars=new(){IsError=true,ErrorCount=3,Exception=cause};provider.Avatars.MetaData["owner"]="fixture";
            Assert.AreSame(provider.Avatars,((IOASISNETProvider)provider).GetAvatarsNearMe(0,0,100));}
        else{provider.Holons=new(){IsError=true,ErrorCount=3,Exception=cause};provider.Holons.MetaData["owner"]="fixture";
            Assert.AreSame(provider.Holons,((IOASISNETProvider)provider).GetHolonsNearMe(0,0,100,HolonType.All));}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void ActivationFailurePreservesCountsAndEvidenceWithoutReading(bool avatars)
    {
        using var provider=new Provider();var cause=new IOException("owned activation failure");
        provider.Activation=new(){IsError=true,ErrorCount=3,Exception=cause};provider.Activation.MetaData["owner"]="fixture";
        if(avatars){var result=((IOASISNETProvider)provider).GetAvatarsNearMe(0,0,100);Assert.AreEqual(3,result.ErrorCount);Assert.AreSame(cause,result.Exception);Assert.AreEqual("fixture",result.MetaData["owner"]);}
        else{var result=((IOASISNETProvider)provider).GetHolonsNearMe(0,0,100,HolonType.All);Assert.AreEqual(3,result.ErrorCount);Assert.AreSame(cause,result.Exception);Assert.AreEqual("fixture",result.MetaData["owner"]);}
        Assert.AreEqual(0,provider.Reads);
    }
    private static IHolonBase Located(bool avatar,double latitude,double longitude)
    {
        IHolonBase record=avatar?new Avatar{IsActive=true}:new Holon{IsActive=true};
        record.MetaData["Latitude"]=latitude;record.MetaData["Longitude"]=longitude;return record;
    }
    [TestMethod]
    public void RequestedTypeIsAppliedBeforeUnrelatedMalformedLocation()
    {
        using var provider=new Provider();
        var selected=(IHolon)Located(false,0,0);selected.HolonType=HolonType.Planet;
        var unrelated=(IHolon)Located(false,0,0);unrelated.HolonType=HolonType.Star;
        unrelated.MetaData["Latitude"]="invalid";
        provider.Holons=new(new[]{selected,unrelated});
        var result=((IOASISNETProvider)provider).GetHolonsNearMe(0,0,0,HolonType.Planet);
        Assert.IsFalse(result.IsError);Assert.IsTrue(result.IsLoaded);
        CollectionAssert.AreEqual(new[]{selected},result.Result.ToArray());
    }
    [TestMethod]
    public void MalformedSelectedLocationIsAnErrorRatherThanAnEmptySuccess()
    {
        using var provider=new Provider();var invalid=(IAvatar)Located(true,0,0);
        invalid.MetaData["Longitude"]="not-a-coordinate";provider.Avatars=new(new[]{invalid});
        var result=((IOASISNETProvider)provider).GetAvatarsNearMe(0,0,100);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
        Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
    }
}
