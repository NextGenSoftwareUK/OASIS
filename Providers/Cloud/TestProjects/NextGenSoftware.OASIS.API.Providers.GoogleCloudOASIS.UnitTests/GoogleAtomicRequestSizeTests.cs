using Google.Cloud.Firestore.V1;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

public partial class GoogleAtomicGraphTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task DeletedFieldMasksCannotBeRejectedByInventedPerFieldRequestPadding(bool sync,bool nativeRejection)
    {
        var client=new Client();var provider=Provider(client);var roots=new List<Holon>();
        for(var index=0;index<7;index++){
            var seed=await provider.ExecuteConditionalHolonWriteAsync(Request(new Holon{Id=Guid.NewGuid(),Name="owned-request-mask"},Guid.NewGuid()));
            Assert.IsFalse(seed.IsError,seed.Exception?.ToString());var row=(Holon)seed.Result.Single();roots.Add(row);
            var document=client.Documents.Single(pair=>pair.Key.EndsWith("/holons/"+row.Id,StringComparison.Ordinal)).Value;
            for(var field=0;field<20000;field++)document.Fields.Add("obsolete"+field.ToString("D5"),new Value{StringValue=""});
            Assert.IsTrue(document.CalculateSize()<1024*1024,"Bounded controlled native document, not a hosted index-size acceptance claim.");
        }
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.Empty,ProviderType.GoogleCloudOASIS,
            roots.Select(row=>new HolonWriteCondition(row.Id,CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS)))),ConditionalHolonOperation.Save,roots,false);
        client.Commits=0;client.Submitted=null;
        var before=client.Documents.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToByteString());
        if(nativeRejection)client.CommitFailure=new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument,"controlled native aggregate/index rejection"));
        var result=sync?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.AreEqual(nativeRejection,result.IsError,result.Exception?.ToString());Assert.AreEqual(1,client.Commits);Assert.AreEqual(nativeRejection?0:7,result.SavedCount);
        if(nativeRejection){Assert.AreSame(client.CommitFailure,result.Exception);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.Exception is INativeMutationReconciliationRequired);Assert.AreEqual(before.Count,client.Documents.Count);foreach(var pair in before)Assert.AreEqual(pair.Value,client.Documents[pair.Key].ToByteString());}
        Assert.IsNotNull(client.Submitted);Assert.AreEqual(9,client.Submitted.Writes.Count);
        Assert.IsTrue(client.Submitted.CalculateSize()<10*1024*1024,"The actual SDK protobuf request is below the wire limit, not a native index/cost guarantee.");
        Assert.AreEqual(140000,client.Submitted.Writes.Sum(write=>write.UpdateMask?.FieldPaths.Count(path=>path.StartsWith("obsolete",StringComparison.Ordinal))??0));
    }
}
