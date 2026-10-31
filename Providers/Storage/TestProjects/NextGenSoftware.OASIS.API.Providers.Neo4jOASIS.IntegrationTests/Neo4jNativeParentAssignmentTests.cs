using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false,false)] [DataRow(true,false,false)]
    [DataRow(false,true,false)] [DataRow(true,true,false)]
    [DataRow(false,false,true)] [DataRow(true,false,true)]
    [DataRow(false,true,true)] [DataRow(true,true,true)]
    public async Task NativeCosmicSelectedChildOwnsParentAssignment(bool synchronous,bool conditional,bool history)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only") {IsVersionControlEnabled=history};
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=driver.AsyncSession();
        var grand=new Holon {Id=Guid.NewGuid(),Name="owned parent assignment grandchild"};
        var child=new Holon {Id=Guid.NewGuid(),Name="owned parent assignment child",Children=new List<IHolon>{grand}};
        var first=new Holon {Id=Guid.NewGuid(),Name="owned parent assignment first",Children=new List<IHolon>{child}};
        var second=new Holon {Id=Guid.NewGuid(),Name="owned parent assignment second"};
        var ids=new[]{first.Id,second.Id,child.Id,grand.Id}.Select(id=>id.ToString()).ToArray();
        var operations=new List<string>();var complete=false;
        Guid Token(IHolon value)=>CosmicConcurrency.GetProviderRevisionId(value,ProviderType.Neo4jOASIS)!.Value;
        async Task<OASISResult<IHolon>> Write(IHolon value)
        {
            if(!conditional)return synchronous ? provider.SaveHolon(value,saveChildren:false)
                : await provider.SaveHolonAsync(value,saveChildren:false);
            var operation=Guid.NewGuid();operations.Add(operation.ToString());
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,Guid.Empty,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(value.Id,Token(value))}),ConditionalHolonOperation.Save,new[]{value},saveChildren:false);
            var result=synchronous ? provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
            return new OASISResult<IHolon>{IsError=result.IsError,Message=result.Message,Exception=result.Exception,
                Result=result.IsError ? null:result.Result.Single()};
        }
        async Task<string> Snapshot(Guid id)=>(await(await inspection.RunAsync(
            "MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",new{id=id.ToString()})).SingleAsync())["snapshot"].As<string>();
        async Task<long> HistoryCount()=>(await(await inspection.RunAsync(
            "MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids RETURN count(v) AS count",new{ids})).SingleAsync())["count"].As<long>();
        async Task<IHolon> Load(Guid id)
        {
            var result=await provider.LoadHolonAsync(id);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);return result.Result;
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var seeded=await provider.SaveHolonsAsync(new IHolon[]{first,second});
            Assert.IsFalse(seeded.IsError,seeded.Message+seeded.Exception);
            var firstSnapshot=await Snapshot(first.Id);var secondSnapshot=await Snapshot(second.Id);var grandSnapshot=await Snapshot(grand.Id);
            var selected=await Load(child.Id);var originalToken=Token(selected);
            selected.ParentHolonId=second.Id;
            var moved=await Write(selected);Assert.IsFalse(moved.IsError,moved.Message+moved.Exception);
            Assert.AreNotEqual(originalToken,Token(moved.Result));Assert.AreEqual(second.Id,moved.Result.ParentHolonId);
            Assert.AreEqual(0,(await Load(first.Id)).Children.Count);
            var secondLoaded=await Load(second.Id);
            Assert.AreEqual(child.Id,secondLoaded.Children.Single().Id);
            Assert.AreEqual(grand.Id,secondLoaded.Children.Single().Children.Single().Id);
            var byParent=await provider.LoadHolonsForParentAsync(second.Id);
            Assert.IsFalse(byParent.IsError,byParent.Message);Assert.AreEqual(child.Id,byParent.Result.Single().Id);
            Assert.AreEqual(firstSnapshot,await Snapshot(first.Id));Assert.AreEqual(secondSnapshot,await Snapshot(second.Id));
            Assert.AreEqual(grandSnapshot,await Snapshot(grand.Id));

            selected=moved.Result;selected.ParentHolonId=Guid.Empty;
            var unlinked=await Write(selected);Assert.IsFalse(unlinked.IsError,unlinked.Message+unlinked.Exception);
            Assert.AreEqual(0,(await Load(second.Id)).Children.Count);
            Assert.AreEqual(Guid.Empty,(await Load(child.Id)).ParentHolonId);
            Assert.AreEqual(grand.Id,(await Load(child.Id)).Children.Single().Id);

            selected=unlinked.Result;selected.ParentHolonId=first.Id;
            var restored=await Write(selected);Assert.IsFalse(restored.IsError,restored.Message+restored.Exception);
            // Saving an empty selected Children collection does not implicitly edit/delete an omitted child.
            var omitted=await provider.LoadHolonAsync(first.Id,loadChildren:false);
            var empty=await provider.SaveHolonAsync(omitted.Result,saveChildren:true);
            Assert.IsFalse(empty.IsError,empty.Message+empty.Exception);
            Assert.AreEqual(child.Id,(await Load(first.Id)).Children.Single().Id);

            // A cycle spanning an unselected descendant must roll back snapshot/history/receipt/edge changes.
            var before=await Snapshot(first.Id);var historyBefore=await HistoryCount();
            var root=await Load(first.Id);root.ParentHolonId=grand.Id;
            var rejected=await Write(root);Assert.IsTrue(rejected.IsError);
            Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(rejected.Exception));
            Assert.AreEqual(before,await Snapshot(first.Id));
            Assert.AreEqual(historyBefore,await HistoryCount());
            Assert.AreEqual(Guid.Empty,(await Load(first.Id)).ParentHolonId);
            Assert.AreEqual(child.Id,(await Load(first.Id)).Children.Single().Id);
            if(conditional)
            {
                var receipt=await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN count(r) AS count",
                    new{operation=operations.Last()})).SingleAsync();
                Assert.AreEqual(0L,receipt["count"].As<long>());
            }
            Assert.AreEqual(grandSnapshot,await Snapshot(grand.Id));
            complete=true;
        }
        finally
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {ids,operations,complete}));
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v",new{ids})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations DETACH DELETE r",new{operations})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();
        }
    }
}
