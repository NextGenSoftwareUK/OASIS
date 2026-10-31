using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleResultForwardingTests
{
    private sealed class Controlled:OracleDBOASIS
    {
        public Controlled():base("UnusedOwnedTest=true"){}
        public OASISResult<IEnumerable<IHolon>> Saved=new();
        public OASISResult<IAvatar> Selected=new();
        public int Exports,SaveCalls;
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> rows,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false){SaveCalls++;return Task.FromResult(Saved);}
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string value,int version=0)=>Task.FromResult(Selected);
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string value,int version=0)=>Task.FromResult(Selected);
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id,int version=0){Exports++;return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=Array.Empty<IHolon>()});}
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ImportRetainsGraphAcknowledgementsAndPartialFailure(bool failure,bool synchronous)
    {
        var provider=new Controlled();var cause=new OraclePartialMutationException(new[]{Guid.NewGuid()},new IOException("owned lost acknowledgement"));
        provider.Saved=new(){Result=new[]{new Holon()},IsError=failure,IsSaved=!failure,SavedCount=failure?1:3,Exception=failure?cause:null,ErrorCode=failure?"owned-error":null,ErrorCount=failure?2:0};
        var result=synchronous?provider.Import(new[]{new Holon()}):await provider.ImportAsync(new[]{new Holon()});
        Assert.AreEqual(1,provider.SaveCalls,"Import must dispatch through the shared-interface graph owner.");
        Assert.AreEqual(!failure,result.Result);Assert.AreEqual(provider.Saved.IsSaved,result.IsSaved);Assert.AreEqual(provider.Saved.SavedCount,result.SavedCount);
        Assert.AreEqual(provider.Saved.ErrorCount,result.ErrorCount);Assert.AreEqual(provider.Saved.ErrorCode,result.ErrorCode);Assert.AreSame(provider.Saved.Exception,result.Exception);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ExportRetainsCompleteIdentityFailure(bool email,bool synchronous)
    {
        var provider=new Controlled();var cause=new InvalidDataException("owned corrupt identity");
        provider.Selected=new(){IsError=true,ErrorCount=3,Exception=cause,ErrorCode="owned-code",Message="owned-message"};
        var result=email?(synchronous?provider.ExportAllDataForAvatarByEmail("owned"):await provider.ExportAllDataForAvatarByEmailAsync("owned")):(synchronous?provider.ExportAllDataForAvatarByUsername("owned"):await provider.ExportAllDataForAvatarByUsernameAsync("owned"));
        Assert.IsTrue(result.IsError);Assert.AreSame(cause,result.Exception);Assert.AreEqual(3,result.ErrorCount);Assert.AreEqual("owned-code",result.ErrorCode);Assert.AreEqual("owned-message",result.Message);Assert.AreEqual(0,provider.Exports);
    }
}
