using System.Reflection;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoPartialGraphAdmissionTests
{
    private sealed class AcknowledgementProbe():AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),
        new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0})
    {
        private readonly Dictionary<string,Dictionary<string,AttributeValue>> rows=new();
        public bool FailChild;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
            =>Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=rows.GetValueOrDefault(request.Key["id"].S)??new()});
        public override Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken cancellationToken=default)
        {
            if(FailChild && request.Item["parentHolonId"].S!=Guid.Empty.ToString())
                throw new InvalidOperationException("Synthetic child dispatch failure after root acknowledgement.");
            var key=request.Item["id"].S;var old=rows.GetValueOrDefault(key);rows[key]=request.Item;
            return Task.FromResult(new PutItemResponse{HttpStatusCode=HttpStatusCode.OK,Attributes=old??new()});
        }
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task PartialNativeAcknowledgementMustNotPublishCompleteMembership(bool synchronous)
    {
        var root=new Holon{Id=Guid.NewGuid()};root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id});
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new AcknowledgementProbe{FailChild=true};field.SetValue(provider,probe);
        try
        {
            var result=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNotNull(root.ChildRoleManifestJson);
            Assert.ThrowsException<InvalidOperationException>(()=>HolonGraphWritePlan.CreateForSave(new[]{root},true,true,0,0),
                "A root acknowledgement followed by child failure cannot publish complete graph observation.");
        }
        finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task CompleteNativeAcknowledgementPublishesSelectedMembership(bool synchronous)
    {
        var root=new Holon{Id=Guid.NewGuid()};root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id});
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new AcknowledgementProbe();field.SetValue(provider,probe);
        try
        {
            var result=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsNotNull(root.ChildRoleManifestJson);
            Assert.AreEqual(2,HolonGraphWritePlan.CreateForSave(new[]{root},true,true,0,0).Nodes.Count,
                "Complete successful publication must retain observation for the next ordinary graph save.");
        }
        finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    private sealed class DispatchProbe():AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),
        new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0})
    {
        public int Reads;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
        {Reads++;throw new InvalidOperationException("The native dispatch boundary was reached.");}
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task KnownPartialGraphMustRejectBeforeNativeRead(bool persisted,bool acceptChanges,bool synchronous)
    {
        var root=new Holon{Id=Guid.NewGuid()};
        if(persisted)root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root.Id,
            new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}));
        HolonChangeTracking.MarkIncompleteChildSelection(root);if(acceptChanges)root.AcceptChanges();
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new DispatchProbe();field.SetValue(provider,probe);
        try
        {
            var result=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<InvalidOperationException>(result.Exception);
            Assert.AreEqual(0,probe.Reads,"Partial caller graphs must reject before native observation, not after a storage failure.");
            Assert.AreEqual(0,root.Version);Assert.IsFalse(result.IsSaved);
            var scalar=synchronous?provider.SaveHolon(root,saveChildren:false):await provider.SaveHolonAsync(root,saveChildren:false);
            Assert.IsTrue(scalar.IsError);Assert.AreEqual(1,probe.Reads,"Root-only admission remains allowed; the synthetic probe deliberately stops the native read.");
        }
        finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
