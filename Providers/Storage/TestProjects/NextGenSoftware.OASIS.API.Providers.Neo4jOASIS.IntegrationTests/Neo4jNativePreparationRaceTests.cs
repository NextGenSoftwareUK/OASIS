using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow("claims")] [DataRow("preparation-first")] [DataRow("write-first")]
    public async Task NativeCosmicConditionalGraphReceiptsAndRacesPreparationFrontier(string order)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var firstGate=new Neo4jPreparationGateProxy.Gate();var secondGate=new Neo4jPreparationGateProxy.Gate();
        var first=new Provider(uri,"neo4j","oasis-local-test-only");
        var second=new Provider(uri,"neo4j","oasis-local-test-only");
        var driverField=typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!;
        driverField.SetValue(first,Neo4jPreparationGateProxy.Wrap((IDriver)driverField.GetValue(first)!,firstGate));
        driverField.SetValue(second,Neo4jPreparationGateProxy.Wrap((IDriver)driverField.GetValue(second)!,secondGate));
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        var fixture="owned-preparation-frontier-"+order;var operation=Guid.NewGuid();var actor=Guid.NewGuid();var id=Guid.NewGuid();var complete=false;
        var existing=await(await inspection.RunAsync("MATCH (f:CosmicPreparationFixture {Name:$fixture}) RETURN count(f) AS count",new{fixture})).SingleAsync();
        Assert.AreEqual(0L,existing["count"].As<long>(),"Reconcile retained failed fixture before rerunning.");
        await(await inspection.RunAsync("CREATE (f:CosmicPreparationFixture {Name:$fixture,OperationId:$operation,ActorId:$actor,HolonId:$id})",
            new{fixture,operation=operation.ToString("D"),actor=actor.ToString("D"),id=id.ToString("D")})).ConsumeAsync();
        HolonWritePreparation Candidate(string value)=>new(operation,actor,ProviderType.Neo4jOASIS,
            new[]{new ProtectedHolonPreparation(id,"oasis-preparation1:owned-"+value)});
        var leftCandidate=Candidate("left");var rightCandidate=Candidate("right");
        var write=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.Neo4jOASIS,
            new[]{new HolonWriteCondition(id,null)}),ConditionalHolonOperation.Save,
            new[]{new Holon{Id=id,Name="owned preparation frontier"}},saveChildren:false);
        Task<OASISResult<HolonWritePreparation>>? leftClaim=null,rightClaim=null;
        Task<OASISResult<IEnumerable<IHolon>>>? mutation=null;
        try
        {
            Assert.IsFalse((await first.ActivateProviderAsync()).IsError);Assert.IsFalse((await second.ActivateProviderAsync()).IsError);
            if(order=="write-first")mutation=first.ExecuteConditionalHolonWriteAsync(write);
            else leftClaim=first.ClaimHolonWritePreparationAsync(leftCandidate);
            if(order=="preparation-first")mutation=second.ExecuteConditionalHolonWriteAsync(write);
            else rightClaim=second.ClaimHolonWritePreparationAsync(rightCandidate);
            await Task.WhenAll(firstGate.Arrived.Task,secondGate.Arrived.Task).WaitAsync(TimeSpan.FromSeconds(30));
            firstGate.Release.TrySetResult();
            if(order=="claims")secondGate.Release.TrySetResult();
            else
            {
                if(leftClaim!=null)Assert.IsFalse((await leftClaim).IsError);
                else Assert.IsFalse((await mutation!).IsError);
                secondGate.Release.TrySetResult();
            }
            if(order=="claims")
            {
                var results=await Task.WhenAll(leftClaim!,rightClaim!);
                Assert.IsTrue(results.Any(result=>!result.IsError));
                foreach(var rejected in results.Where(result=>result.IsError))Assert.AreEqual("COSMIC_PREPARATION_CONCURRENT_CLAIM",rejected.ErrorCode);
                var replay=await first.ClaimHolonWritePreparationAsync(leftCandidate);Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
                foreach(var winner in results.Where(result=>!result.IsError))Assert.AreEqual(winner.Result.Records[0].Envelope,replay.Result.Records[0].Envelope);
                var native=await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r.PreparationJson AS json,r.ResultJson IS NULL AS absent",new{operation=operation.ToString("D")})).SingleAsync();
                Assert.IsTrue(native["absent"].As<bool>());Assert.AreEqual(ProtectedPreparationJson.Write(replay.Result),native["json"].As<string>());
            }
            else if(order=="preparation-first")
            {
                var committed=await mutation!;Assert.IsFalse(committed.IsError,committed.Message+committed.Exception);
                var replay=await second.ClaimHolonWritePreparationAsync(rightCandidate);Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
                Assert.AreEqual(leftCandidate.Records[0].Envelope,replay.Result.Records[0].Envelope);
                Assert.AreEqual(committed.Result.Single().VersionId,(await first.ExecuteConditionalHolonWriteAsync(write)).Result.Single().VersionId);
            }
            else
            {
                var rejected=await rightClaim!;Assert.IsTrue(rejected.IsError);Assert.IsNull(rejected.Result);
                Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",rejected.ErrorCode);
                var native=await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r.PreparationJson IS NULL AS absent",new{operation=operation.ToString("D")})).SingleAsync();
                Assert.IsTrue(native["absent"].As<bool>());
            }
            complete=true;
        }
        finally
        {
            firstGate.Release.TrySetResult();secondGate.Release.TrySetResult();
            var pending=new List<Task>();if(leftClaim!=null)pending.Add(leftClaim);if(rightClaim!=null)pending.Add(rightClaim);if(mutation!=null)pending.Add(mutation);
            await Task.WhenAll(pending);
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h",new{id=id.ToString("D")})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) DETACH DELETE r",new{operation=operation.ToString("D")})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (f:CosmicPreparationFixture {Name:$fixture}) DELETE f",new{fixture})).ConsumeAsync();
            }
            else Console.WriteLine("Preserved preparation frontier: "+fixture+" operation="+operation);
            await first.DeActivateProviderAsync();await second.DeActivateProviderAsync();
        }
    }
}
