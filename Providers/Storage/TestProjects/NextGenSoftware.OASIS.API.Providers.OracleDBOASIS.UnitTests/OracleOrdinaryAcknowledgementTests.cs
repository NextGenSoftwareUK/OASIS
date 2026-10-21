using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleOrdinaryAcknowledgementTests
{
    private sealed class Provider(int mode):OracleDBOASIS("Unused owned controlled connection")
    {
        public int Calls;
        protected override Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write)
        {
            Calls++;write.Snapshot.Version=1;write.Snapshot.VersionId=Guid.NewGuid();
            if(mode!=0&&Calls==mode)write.Source.Name="newer caller edit";
            return Task.FromResult(new OASISResult<IHolon>{Result=write.Snapshot,IsSaved=mode!=0,SavedCount=mode==0?0:1});
        }
    }
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task GraphRequiresAcknowledgementAndCountsKnownCommitBeforeCallerPublication(int mode,bool synchronous)
    {
        var rows=Enumerable.Range(0,3).Select(index=>new Holon{Id=Guid.NewGuid(),Name="owned source "+index}).ToArray();
        var provider=new Provider(mode);
        var result=synchronous?provider.SaveHolons(rows,saveChildren:false):await provider.SaveHolonsAsync(rows,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(mode==0?0:mode,result.SavedCount);
        Assert.AreEqual(mode==2?1:0,result.Result?.Count()??0);Assert.AreEqual(mode==2?2:1,provider.Calls);
        if(mode!=0){Assert.AreEqual("newer caller edit",rows[mode-1].Name);Assert.AreEqual(0,rows[mode-1].Version);}
        Assert.AreEqual(0,rows[2].Version);
    }
}
