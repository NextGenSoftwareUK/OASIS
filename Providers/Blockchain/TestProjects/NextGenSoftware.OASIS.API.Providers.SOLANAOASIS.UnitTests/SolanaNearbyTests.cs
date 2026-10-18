using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaNearbyTests
{
    [DataTestMethod]
    [DataRow(90000001L,0L,1)] [DataRow(0L,180000001L,1)] [DataRow(0L,0L,-1)]
    public void InvalidInputCannotActivateOrRead(long latitude,long longitude,int radius)
    {
        var provider=new Probe(false);var net=(IOASISNETProvider)provider;
        Assert.IsInstanceOfType(net.GetAvatarsNearMe(latitude,longitude,radius).Exception,typeof(ArgumentOutOfRangeException));
        Assert.IsInstanceOfType(net.GetHolonsNearMe(latitude,longitude,radius,HolonType.All).Exception,typeof(ArgumentOutOfRangeException));
        Assert.AreEqual(0,provider.Activations);Assert.AreEqual(0,provider.Reads);
    }

    [TestMethod]
    public void InvalidTypeCannotActivateOrRead()
    {
        var provider=new Probe(false);
        Assert.IsInstanceOfType(((IOASISNETProvider)provider).GetHolonsNearMe(0,0,100,(HolonType)int.MaxValue).Exception,typeof(ArgumentOutOfRangeException));
        Assert.AreEqual(0,provider.Activations);Assert.AreEqual(0,provider.Reads);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void AdapterPreservesRecordsAndRejectsMalformedCoordinates(bool avatars)
    {
        var provider=new Probe(true);var net=(IOASISNETProvider)provider;
        Dictionary<string,object> Location(string latitude)=>new() {{"Latitude",latitude},{"Longitude","20"},{"proof","preserved"}};
        provider.Avatars=new[] {new Avatar {Id=Guid.NewGuid(),FirstName="complete",IsActive=true,MetaData=Location("30.0005")},new Avatar {IsActive=true,MetaData=Location("30.002")},new Avatar {IsActive=false,MetaData=Location("30.0005")},new Avatar {IsActive=true}};
        provider.Holons=provider.Avatars.Select(a=>(IHolon)new Holon {Id=a.Id,Description="complete",HolonType=HolonType.Park,IsActive=a.IsActive,MetaData=a.MetaData}).ToArray();
        var prior=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=new CultureInfo("fr-FR");
            if(avatars)
            {
                var result=net.GetAvatarsNearMe(30000000,20000000,100);
                Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreSame(provider.Avatars[0],result.Result.Single());
                Assert.AreEqual(0,net.GetAvatarsNearMe(30000000,20000000,0).Result.Count());
                provider.Avatars[0].MetaData["Latitude"]="NaN";
                var invalid=net.GetAvatarsNearMe(30000000,20000000,100);
                Assert.IsInstanceOfType(invalid.Exception,typeof(InvalidDataException));Assert.IsNull(invalid.Result);Assert.IsFalse(invalid.IsLoaded);
            }
            else
            {
                var result=net.GetHolonsNearMe(30000000,20000000,100,HolonType.Park);
                Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreSame(provider.Holons[0],result.Result.Single());
                Assert.AreEqual(0,net.GetHolonsNearMe(30000000,20000000,100,HolonType.Holon).Result.Count());
                Assert.AreEqual(0,net.GetHolonsNearMe(30000000,20000000,0,HolonType.All).Result.Count());
                provider.Holons[0].MetaData.Remove("Longitude");
                var invalid=net.GetHolonsNearMe(30000000,20000000,100,HolonType.All);
                Assert.IsInstanceOfType(invalid.Exception,typeof(InvalidDataException));Assert.IsNull(invalid.Result);Assert.IsFalse(invalid.IsLoaded);
            }
        }
        finally {CultureInfo.CurrentCulture=prior;}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void AdapterReturnsOriginalCollectionFailure(bool avatars)
    {
        var provider=new Probe(true) {Cause=new IOException("original collection failure")};var net=(IOASISNETProvider)provider;
        var cause=avatars ? net.GetAvatarsNearMe(0,0,1).Exception:net.GetHolonsNearMe(0,0,1,HolonType.All).Exception;
        Assert.AreSame(provider.Cause,cause);
    }

    private sealed class Probe:SolanaOASIS
    {
        public int Activations,Reads;
        public Exception? Cause;
        public IAvatar[] Avatars=Array.Empty<IAvatar>();
        public IHolon[] Holons=Array.Empty<IHolon>();
        public Probe(bool active):this(new Account(),active) {}
        private Probe(Account account,bool active):base("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key) {IsProviderActivated=active;}
        public override Task<OASISResult<bool>> ActivateProviderAsync() {Activations++;throw new InvalidOperationException("Unexpected activation");}
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version=0) {Reads++;return new() {Result=Avatars,IsError=Cause!=null,Exception=Cause};}
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0) {Reads++;return new() {Result=Holons,IsError=Cause!=null,Exception=Cause};}
    }
}
