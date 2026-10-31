using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaIdentityLookupTests
{
    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(false,true)]
    [DataRow(true,false)]
    [DataRow(true,true)]
    public async Task SelectorsUseStoredCollectionsAndRetainActualFields(bool detail,bool synchronous)
    {
        var provider=new CollectionProvider();
        var id=Guid.NewGuid();
        provider.Avatars=new[] {new Avatar {Id=id,Email="real@example.invalid",Username="actual user",FirstName="stored"}};
        provider.Details=new[] {new AvatarDetail {Id=id,Email="real@example.invalid",Username="actual user",XP=57,Karma=29}};
        if(detail)
        {
            var byId=synchronous ? provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id);
            var byEmail=synchronous ? provider.LoadAvatarDetailByEmail("REAL@example.invalid"):await provider.LoadAvatarDetailByEmailAsync("REAL@example.invalid");
            var byName=synchronous ? provider.LoadAvatarDetailByUsername("ACTUAL USER"):await provider.LoadAvatarDetailByUsernameAsync("ACTUAL USER");
            foreach(var result in new[] {byId,byEmail,byName}) {Assert.IsFalse(result.IsError,result.Message); Assert.IsTrue(result.IsLoaded); Assert.AreSame(provider.Details[0],result.Result); Assert.AreEqual(57,result.Result.XP);}
        }
        else
        {
            var byId=synchronous ? provider.LoadAvatar(id):await provider.LoadAvatarAsync(id);
            var byEmail=synchronous ? provider.LoadAvatarByEmail("REAL@example.invalid"):await provider.LoadAvatarByEmailAsync("REAL@example.invalid");
            var byName=synchronous ? provider.LoadAvatarByUsername("ACTUAL USER"):await provider.LoadAvatarByUsernameAsync("ACTUAL USER");
            foreach(var result in new[] {byId,byEmail,byName}) {Assert.IsFalse(result.IsError,result.Message); Assert.IsTrue(result.IsLoaded); Assert.AreSame(provider.Avatars[0],result.Result); Assert.AreEqual("stored",result.Result.FirstName);}
        }
        Assert.AreEqual(3,provider.Reads);
    }

    [DataTestMethod]
    [DataRow("missing",false)]
    [DataRow("missing",true)]
    [DataRow("duplicate",false)]
    [DataRow("duplicate",true)]
    [DataRow("collection-error",false)]
    [DataRow("collection-error",true)]
    [DataRow("invalid",false)]
    [DataRow("invalid",true)]
    public async Task LookupFailuresNeverProduceFabricatedRecords(string scenario,bool synchronous)
    {
        var provider=new CollectionProvider();
        if(scenario=="duplicate") provider.Avatars=new[] {new Avatar {Email="match"},new Avatar {Email="MATCH"}};
        provider.Fail=scenario=="collection-error";
        var selector=scenario=="invalid" ? " ":"match";
        var result=synchronous ? provider.LoadAvatarByEmail(selector):await provider.LoadAvatarByEmailAsync(selector);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded); Assert.IsNull(result.Result);
        if(scenario=="duplicate") Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);
        if(scenario=="missing") Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
        if(scenario=="collection-error") {Assert.AreEqual("owned-collection-error",result.ErrorCode); Assert.AreSame(provider.Cause,result.Exception);}
        if(scenario=="invalid") Assert.AreEqual(0,provider.Reads);
    }

    private sealed class CollectionProvider:SolanaOASIS
    {
        public IAvatar[] Avatars=Array.Empty<IAvatar>();
        public IAvatarDetail[] Details=Array.Empty<IAvatarDetail>();
        public int Reads;
        public bool Fail;
        public Exception Cause=new InvalidOperationException("Owned collection failure");
        public CollectionProvider():this(new Account()) {}
        private CollectionProvider(Account account):base("http://127.0.0.1:8899",account.PrivateKey.Key,account.PublicKey.Key) {}
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0)
        {
            Reads++;
            return Task.FromResult(Fail ? new OASISResult<IEnumerable<IAvatar>> {IsError=true,ErrorCode="owned-collection-error",Exception=Cause,Message=Cause.Message}
                :new OASISResult<IEnumerable<IAvatar>> {Result=Avatars,IsLoaded=true});
        }
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version=0)
        {
            Reads++; return Task.FromResult(new OASISResult<IEnumerable<IAvatarDetail>> {Result=Details,IsLoaded=true});
        }
    }
}
