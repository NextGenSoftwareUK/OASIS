using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleGraphAcknowledgementIdentityTests
{
    private sealed class Controlled(Guid claimed,bool thrown):GoogleCloudOASIS("owned-identity",enableStorage:false,enableFirestore:false,enableBigQuery:false)
    {
        public int Calls;
        protected override Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write)
        {
            Calls++;
            Exception cause=new GoogleFirestorePartialMutationException(new[]{claimed,claimed,Guid.Empty},new IOException("owned post-ack failure"));
            if(thrown)throw cause;
            return Task.FromResult(new OASISResult<IHolon>{IsError=true,Exception=cause,SavedCount=99});
        }
    }

    [DataTestMethod]
    [DataRow("current",false,false)][DataRow("future",false,false)][DataRow("foreign",false,false)]
    [DataRow("current",true,false)][DataRow("future",true,false)][DataRow("foreign",true,false)]
    [DataRow("current",false,true)][DataRow("future",false,true)][DataRow("foreign",false,true)]
    [DataRow("current",true,true)][DataRow("future",true,true)][DataRow("foreign",true,true)]
    public async Task GraphCountsOnlyAcknowledgementsBoundToDispatchedIdentity(string identity,bool thrown,bool synchronous)
    {
        var rows=Enumerable.Range(0,3).Select(_=>new Holon{Id=Guid.NewGuid()}).ToArray();
        var claimed=identity=="current"?rows[0].Id:identity=="future"?rows[2].Id:Guid.NewGuid();
        var provider=new Controlled(claimed,thrown);
        var result=synchronous?provider.SaveHolons(rows,continueOnError:true):await provider.SaveHolonsAsync(rows,continueOnError:true);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(1,provider.Calls,"Never dispatch another write after an uncertain outcome.");
        Assert.AreEqual(identity=="current"?1:0,result.SavedCount,"A foreign or undispatched identity cannot establish this operation's acknowledgement count.");
        foreach(var row in rows)Assert.AreEqual(0,row.Version);
    }
}
