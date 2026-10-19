using System.Reflection;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoVersionTests
{
    private sealed class ReadProbe : AmazonDynamoDBClient
    {
        public int Reads;
        public ReadProbe():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true}) { }
        private Task<T> Unexpected<T>() {Reads++;return Task.FromException<T>(new IOException("Unexpected latest-record SDK read."));}
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)=>Unexpected<GetItemResponse>();
        public override Task<QueryResponse> QueryAsync(QueryRequest request,CancellationToken cancellationToken=default)=>Unexpected<QueryResponse>();
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken cancellationToken=default)=>Unexpected<ScanResponse>();
    }

    [DataTestMethod]
    [DataRow(false,-1)] [DataRow(true,-1)]
    public async Task ExplicitAvatarVersionsNeverSilentlyReadLatest(bool synchronous,int version)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var probe=new ReadProbe();field.SetValue(provider,probe);
        try
        {
            void Check<T>(OASISResult<T> result)
            {
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
                Assert.IsInstanceOfType(result.Exception,version<0 ? typeof(ArgumentOutOfRangeException):typeof(NotSupportedException));
            }
            var id=Guid.NewGuid();
            Check(synchronous ? provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version));
            Check(synchronous ? provider.LoadAvatarByProviderKey(id.ToString(),version):await provider.LoadAvatarByProviderKeyAsync(id.ToString(),version));
            Check(synchronous ? provider.LoadAvatarByUsername("owned",version):await provider.LoadAvatarByUsernameAsync("owned",version));
            Check(synchronous ? provider.LoadAvatarByEmail("owned@example.invalid",version):await provider.LoadAvatarByEmailAsync("owned@example.invalid",version));
            Check(synchronous ? provider.LoadAllAvatars(version):await provider.LoadAllAvatarsAsync(version));
            Check(synchronous ? provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version));
            Check(synchronous ? provider.LoadAvatarDetailByUsername("owned",version):await provider.LoadAvatarDetailByUsernameAsync("owned",version));
            Check(synchronous ? provider.LoadAvatarDetailByEmail("owned@example.invalid",version):await provider.LoadAvatarDetailByEmailAsync("owned@example.invalid",version));
            Check(synchronous ? provider.LoadAllAvatarDetails(version):await provider.LoadAllAvatarDetailsAsync(version));
            Assert.AreEqual(0,probe.Reads);
        }
        finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
