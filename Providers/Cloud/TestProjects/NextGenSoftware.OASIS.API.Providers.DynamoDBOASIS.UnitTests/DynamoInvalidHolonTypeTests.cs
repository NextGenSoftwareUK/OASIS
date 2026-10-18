using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoInvalidHolonTypeTests
{
    private sealed class CountingClient():AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18000"})
    {
        public int Reads;
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken cancellationToken=default)
        {Reads++;return Task.FromResult(new ScanResponse{Items=new(),HttpStatusCode=System.Net.HttpStatusCode.OK});}
    }

    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task InvalidTypeIsRejectedBeforeAnySdkRead(int selector,bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        using var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var counted=new CountingClient();field.SetValue(provider,counted);
        var id=Guid.NewGuid();var type=(HolonType)int.MaxValue;
        var result=selector switch
        {
            0=>synchronous?provider.LoadAllHolons(type,loadChildren:false):await provider.LoadAllHolonsAsync(type,loadChildren:false),
            1=>synchronous?provider.LoadHolonsForParent(id,type,loadChildren:false):await provider.LoadHolonsForParentAsync(id,type,loadChildren:false),
            _=>synchronous?provider.LoadHolonsForParent(id.ToString(),type,loadChildren:false):await provider.LoadHolonsForParentAsync(id.ToString(),type,loadChildren:false)
        };
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.AreEqual(0,counted.Reads);
    }
}
