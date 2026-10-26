using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.ProviderTestSupport;
// Wrapper controls only, not simulated SDK, storage or billing evidence.
internal static class ImportResultEnvelopeAssertions
{
    private sealed class UnknownOutcome:IOException,INativeMutationReconciliationRequired
    {public string OriginalTransactionId=>"synthetic-original-submission";}
    private sealed class Unenumerable:IEnumerable<IHolon>
    {
        public IEnumerator<IHolon> GetEnumerator()=>throw new InvalidOperationException("Import must not enumerate an already produced save payload.");
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    internal static OASISResult<IEnumerable<IHolon>> Source(int mode)=>new()
    {
        IsError=mode is 0 or 3,IsSaved=mode is 1 or 3 or 4,SavedCount=mode==2||mode==4?0:3,Result=mode==2?null:new Unenumerable(),
        Message="original save message",DetailedMessage="original native outcome detail",ErrorCode=mode is 0 or 3?"NATIVE_RECONCILIATION_REQUIRED":null,
        Exception=mode is 0 or 3?new UnknownOutcome():null,
        ErrorCount=mode is 0 or 3?1:0,IsWarning=true,WarningCount=2,HasAnyHolonsChanged=true,
        MetaData=new Dictionary<string,string>{{"original-request","synthetic-test-request"}},InnerMessages=new List<string>{"original evidence retained"}
    };
    internal static async Task Verify(IOASISStorageProvider provider,OASISResult<IEnumerable<IHolon>> source,bool synchronous)
    {
        var input=new IHolon[]{new Holon()};var result=synchronous?provider.Import(input):await provider.ImportAsync(input);
        Assert.AreEqual(!source.IsError&&source.IsSaved,result.Result,"Import requires a confirmed save.");
        Assert.AreEqual(source.IsError,result.IsError);Assert.AreEqual(source.IsSaved,result.IsSaved);Assert.AreEqual(source.SavedCount,result.SavedCount);
        Assert.AreEqual(source.ErrorCode,result.ErrorCode);Assert.AreSame(source.Exception,result.Exception);Assert.AreEqual(source.DetailedMessage,result.DetailedMessage);
        Assert.AreEqual(source.ErrorCount,result.ErrorCount);Assert.AreEqual(source.IsWarning,result.IsWarning);Assert.AreEqual(source.WarningCount,result.WarningCount);
        Assert.AreEqual(source.HasAnyHolonsChanged,result.HasAnyHolonsChanged);Assert.AreSame(source.MetaData,result.MetaData);Assert.AreSame(source.InnerMessages,result.InnerMessages);
    }
}
