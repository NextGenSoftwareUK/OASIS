using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

public partial class GoogleAtomicGraphTests
{
    [DataTestMethod]
    [DataRow(ConditionalHolonOperation.Save,false)][DataRow(ConditionalHolonOperation.Save,true)]
    [DataRow(ConditionalHolonOperation.SoftDelete,false)][DataRow(ConditionalHolonOperation.SoftDelete,true)]
    [DataRow(ConditionalHolonOperation.HardDelete,false)][DataRow(ConditionalHolonOperation.HardDelete,true)]
    public async Task ConditionalResultCountsDistinctAcknowledgedNodesAndReplaysOriginalCounts(ConditionalHolonOperation operation,bool synchronous)
    {
        var client=new Client();var provider=Provider(client);var child=new Holon{Id=Guid.NewGuid()};
        var a=new Holon{Id=Guid.NewGuid(),Children=new List<IHolon>{child}};var b=new Holon{Id=Guid.NewGuid()};
        var create=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.Empty,ProviderType.GoogleCloudOASIS,
            new[]{a,b,child}.Select(row=>new HolonWriteCondition(row.Id,null))),ConditionalHolonOperation.Save,new[]{a,b});
        var initial=await provider.ExecuteConditionalHolonWriteAsync(create);Assert.IsFalse(initial.IsError,initial.Exception?.ToString());
        Assert.AreEqual(3,initial.SavedCount);Assert.AreEqual(0,initial.DeletedCount);Assert.IsTrue(initial.HasAnyHolonsChanged);
        var graph=HolonGraphWritePlan.Create(initial.Result,true,true,0,0);
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.Empty,ProviderType.GoogleCloudOASIS,
            graph.Nodes.Select(node=>new HolonWriteCondition(node.Source.Id,CosmicConcurrency.GetProviderRevisionId(node.Source,ProviderType.GoogleCloudOASIS)))),operation,initial.Result);
        var result=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Check(result);var commits=client.Commits;var replay=await provider.ExecuteConditionalHolonWriteAsync(request);Check(replay);Assert.AreEqual(commits,client.Commits);
        void Check(NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>> value){
            Assert.IsFalse(value.IsError,value.Exception?.ToString());Assert.AreEqual(operation==ConditionalHolonOperation.Save,value.IsSaved);
            Assert.AreEqual(operation!=ConditionalHolonOperation.Save,value.IsDeleted);
            Assert.AreEqual(operation==ConditionalHolonOperation.Save?3:0,value.SavedCount);
            Assert.AreEqual(operation==ConditionalHolonOperation.Save?0:3,value.DeletedCount);Assert.IsTrue(value.HasAnyHolonsChanged);
            foreach(var node in HolonGraphWritePlan.Create(value.Result,true,true,0,0).Nodes)Assert.IsFalse(node.Source.IsNewHolon);
        }
    }

    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task ConditionalActiveSaveOverTombstoneReturnsLifecycleConflictBeforeCommit(bool history,bool synchronous)
    {
        var client=new Client();var provider=Provider(client);provider.IsVersionControlEnabled=history;
        var initial=await provider.ExecuteConditionalHolonWriteAsync(Request(new Holon{Id=Guid.NewGuid(),IsActive=true},Guid.NewGuid()));
        Assert.IsFalse(initial.IsError,initial.Exception?.ToString());var row=(Holon)initial.Result.Single();
        var deleted=await provider.ExecuteConditionalHolonWriteAsync(Request(row,Guid.NewGuid(),CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS),ConditionalHolonOperation.SoftDelete));
        Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());row=(Holon)deleted.Result.Single();
        var before=client.Documents.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToByteArray());var commits=client.Commits;
        row.IsActive=true;row.DeletedDate=DateTime.MinValue;
        var request=Request(row,Guid.NewGuid(),CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS));
        var result=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsTrue(result.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
        Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);Assert.AreEqual(0,result.SavedCount);Assert.AreEqual(0,result.DeletedCount);
        Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(commits,client.Commits);
        foreach(var pair in before)CollectionAssert.AreEqual(pair.Value,client.Documents[pair.Key].ToByteArray());
    }
}
