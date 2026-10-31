using System.Net;
using System.Reflection;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoDuplicatePageTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach(var route in Enumerable.Range(0,4))foreach(var mode in Enumerable.Range(0,3))foreach(var synchronous in new[]{false,true})
            yield return new object[]{route,mode,synchronous};
    }

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task DuplicateIdentitiesRejectTheWholeRead(int route,int mode,bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var probe=new Pages(mode);field.SetValue(provider,probe);
        try
        {
            void Check<T>(OASISResult<IEnumerable<T>> result)
            {
                Assert.AreEqual(mode!=2,result.IsError,result.Message);
                if(mode==2){Assert.IsTrue(result.IsLoaded);Assert.AreEqual(2,result.Result.Count());Assert.AreEqual(2,result.LoadedCount);}
                else{Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));}
            }
            if(route==0)Check(synchronous?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync());
            else if(route==1)Check(synchronous?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync());
            else if(route==2)Check(synchronous?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false));
            else
            {
                var method=typeof(DynamoDBOASIS).GetMethod("ReadQueryItemsAsync",BindingFlags.Static|BindingFlags.NonPublic)!;
                Func<QueryRequest,Task<QueryResponse>> read=request=>probe.QueryAsync(request);
                var pending=(Task<List<Dictionary<string,AttributeValue>>>)method.Invoke(null,new object[]{read,new QueryRequest{TableName="OasisHolons",ConsistentRead=true}})!;
                if(mode==2)Assert.AreEqual(2,(synchronous?pending.GetAwaiter().GetResult():await pending).Count);
                else if(synchronous)Assert.ThrowsException<InvalidDataException>(()=>pending.GetAwaiter().GetResult());
                else await Assert.ThrowsExceptionAsync<InvalidDataException>(async()=>await pending);
            }
            Assert.AreEqual(mode==0?1:2,probe.Reads,"Reject the first repeated identity; do not retry or silently deduplicate.");
        }
        finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    private sealed class Pages(int mode):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{RegionEndpoint=RegionEndpoint.USEast1,MaxErrorRetry=0})
    {
        private readonly Guid first=Guid.NewGuid(),second=Guid.NewGuid();public int Reads;
        private (List<Dictionary<string,AttributeValue>> Items,Dictionary<string,AttributeValue> Cursor) Page(string table)
        {
            Reads++;if(Reads>2)throw new InvalidOperationException("Unbounded page dispatch.");
            var id=mode==2&&Reads==2?second:first;
            Core.Interfaces.IHolonBase record=table=="OasisAvatars"?new Core.Holons.Avatar{Id=id}:table=="OasisAvatarDetails"?new Core.Holons.AvatarDetail{Id=id}:new Core.Holons.Holon{Id=id};
            var item=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString("D")}},{"data_json",new(){S=OasisJson.Serialize(record)}}};
            return(mode==0?new(){item,item}:new(){item},mode!=0&&Reads==1?new(){{"id",new(){S="next"}}}:new());
        }
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken token=default)
        {
            var page=Page(request.TableName);return Task.FromResult(new ScanResponse{HttpStatusCode=HttpStatusCode.OK,Items=page.Items,LastEvaluatedKey=page.Cursor});
        }
        public override Task<QueryResponse> QueryAsync(QueryRequest request,CancellationToken token=default)
        {
            var page=Page(request.TableName);return Task.FromResult(new QueryResponse{HttpStatusCode=HttpStatusCode.OK,Items=page.Items,LastEvaluatedKey=page.Cursor});
        }
    }
}
