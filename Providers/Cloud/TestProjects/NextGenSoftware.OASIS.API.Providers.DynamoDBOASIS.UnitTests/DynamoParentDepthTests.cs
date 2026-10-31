using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoParentDepthTests
{
    private sealed class Records:AmazonDynamoDBClient
    {
        public readonly Guid Parent=Guid.NewGuid();
        private readonly Holon child;
        private readonly Holon grandchild;
        public Records():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001"})
        {
            child=new Holon{Id=Guid.NewGuid(),ParentHolonId=Parent};
            grandchild=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id};child.Children.Add(grandchild);
        }
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken cancellationToken=default)
            =>Task.FromResult(new ScanResponse{HttpStatusCode=System.Net.HttpStatusCode.OK,Items=new[]{child,grandchild}.Select(row=>new Dictionary<string,AttributeValue>{["id"]=new(){S=row.Id.ToString()},["data_json"]=new(){S=OasisJson.Serialize(row)}}).ToList()});
    }

    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ParentSelectionCountsItsAlreadyTraversedEdge(bool fromProvider,bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        using var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var records=new Records();field.SetValue(provider,records);
        var result=synchronous?provider.LoadHolonsForParent(records.Parent,maxChildDepth:1,loadChildrenFromProvider:fromProvider)
            :await provider.LoadHolonsForParentAsync(records.Parent,maxChildDepth:1,loadChildrenFromProvider:fromProvider);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());
        Assert.AreEqual(0,result.Result.Single().Children.Count,"The selected child is depth1; maxDepth1 excludes its own children.");
    }
}
