using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class PostgreSqlResultForwardingTests
{
    private sealed class Provider:PostgreSQLOASIS
    {
        public Provider():base("UnsupportedConnectionKeyword=true"){}
        public OASISResult<IEnumerable<IHolon>> Saved=new();public OASISResult<IAvatar> Selected=new();
        public bool Continued;public int Exports;
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> rows,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false){Continued=continueOnError;return Task.FromResult(Saved);}
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string value,int version=0)=>Task.FromResult(Selected);
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string value,int version=0)=>Task.FromResult(Selected);
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id,int version=0){Exports++;return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=Array.Empty<IHolon>()});}
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ImportPreservesAcknowledgementsAndOriginalReconciliation(bool failure,bool synchronous)
    {
        var provider=new Provider();var id=Guid.NewGuid();var cause=new PostgreSqlGraphReconciliationException(new[]{id},new[]{id},new IOException("owned unknown acknowledgement"));
        provider.Saved=new OASISResult<IEnumerable<IHolon>>{Result=new[]{new Holon{Id=id}},IsError=failure,IsSaved=!failure,SavedCount=failure?1:3,Exception=failure?cause:null,ErrorCode=failure?"owned-error":null};
        var result=synchronous?provider.Import(new[]{new Holon()}):await provider.ImportAsync(new[]{new Holon()});
        Assert.IsFalse(provider.Continued,"Import's bool-only contract must use the existing atomic batch mode.");
        Assert.AreEqual(!failure,result.Result);Assert.AreEqual(!failure,result.IsSaved);Assert.AreEqual(provider.Saved.SavedCount,result.SavedCount);Assert.AreEqual(provider.Saved.ErrorCode,result.ErrorCode);Assert.AreSame(provider.Saved.Exception,result.Exception);
        if(failure)Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ExportIdentityFailureDoesNotBecomeAnAnonymousNotFound(bool email,bool synchronous)
    {
        var provider=new Provider();var cause=new InvalidDataException("owned corrupt identity record");
        provider.Selected=new OASISResult<IAvatar>{IsError=true,Exception=cause,ErrorCode="owned-code",Message="owned-message"};
        var result=email?(synchronous?provider.ExportAllDataForAvatarByEmail("owned"):await provider.ExportAllDataForAvatarByEmailAsync("owned")):(synchronous?provider.ExportAllDataForAvatarByUsername("owned"):await provider.ExportAllDataForAvatarByUsernameAsync("owned"));
        Assert.IsTrue(result.IsError);Assert.AreSame(cause,result.Exception);Assert.AreEqual("owned-code",result.ErrorCode);Assert.AreEqual(0,provider.Exports);
    }
}
