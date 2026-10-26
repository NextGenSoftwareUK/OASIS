using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jPreparationReadTests
{
    [DataTestMethod][DataRow("stored")][DataRow("absent")][DataRow("receipt-only")]
    [DataRow("foreign")][DataRow("corrupt")][DataRow("incomplete")][DataRow("duplicate")]
    public async Task LookupDoesNotClaimOrMutateAndNeverReturnsForeignEnvelopes(string mode)
    {
        var original=new HolonWritePreparation(Guid.NewGuid(),Guid.NewGuid(),ProviderType.Neo4jOASIS,
            new[]{new ProtectedHolonPreparation(Guid.NewGuid(),"oasis-preparation1:opaque")});
        var properties=new Dictionary<string,object>{["PreparationActorId"]=original.ActorId.ToString("D"),
            ["PreparationFormat"]=1L,["PreparationCreatedAtUtc"]=DateTime.UtcNow.ToString("O"),
            ["PreparationJson"]=mode=="corrupt"?"invalid":ProtectedPreparationJson.Write(original)};
        if(mode=="receipt-only"){properties.Clear();properties["ResultJson"]="not-consumed-by-preparation-read";}
        if(mode=="incomplete")properties.Remove("PreparationJson");
        var node=DispatchProxy.Create<Neo4j.Driver.INode,Neo4jNativeIdentityTests.NodeProbe>();
        ((Neo4jNativeIdentityTests.NodeProbe)node).Values=properties;
        var record=DispatchProxy.Create<IRecord,Neo4jConditionalReceiptCleanupTests.RecordProbe>();
        ((Neo4jConditionalReceiptCleanupTests.RecordProbe)record).Node=node;
        var session=DispatchProxy.Create<IAsyncSession,Neo4jConditionalRecoveryTests.SessionProbe>();
        var probe=(Neo4jConditionalRecoveryTests.SessionProbe)session;
        probe.Record=record;probe.Count=mode=="absent"?0:mode=="duplicate"?2:1;
        var driver=DispatchProxy.Create<IDriver,Neo4jLifecycleTests.DriverProbe>();
        var provider=new Neo4jOASIS("bolt://owned.invalid","owned","owned",()=>driver);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        ((Neo4jLifecycleTests.DriverProbe)driver).SessionFactory=()=>session;
        try
        {
        var result=await provider.ReadHolonWritePreparationAsync(original.OperationId,mode=="foreign"?Guid.NewGuid():original.ActorId);
        Assert.AreEqual(1,probe.Queries);
        Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
        var rejected=mode is "foreign" or "corrupt" or "incomplete" or "duplicate";
        Assert.AreEqual(rejected,result.IsError,result.Message+result.Exception);
        if(mode=="stored")Assert.AreEqual(original.Records[0].Envelope,result.Result.Records[0].Envelope);
        else Assert.IsNull(result.Result);
        if(mode=="foreign")Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH",result.ErrorCode);
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
