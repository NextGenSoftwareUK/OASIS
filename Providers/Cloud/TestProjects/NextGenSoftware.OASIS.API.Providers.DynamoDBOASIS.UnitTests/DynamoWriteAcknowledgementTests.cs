using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoWriteAcknowledgementTests
{
    private sealed class MissingAcknowledgement(bool missing,bool success=false):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),
        new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0})
    {
        public int Calls;
        public PutItemRequest? Submitted;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
            =>Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=new()});
        public override Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken cancellationToken=default)
        {Calls++;Submitted=request;return Task.FromResult(missing?null!:new PutItemResponse{HttpStatusCode=success?HttpStatusCode.OK:HttpStatusCode.Accepted});}
    }

    [TestMethod]
    public async Task DetailStorageKeyIsPersistedAndPublishedAfterAcknowledgement()
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new MissingAcknowledgement(false,true);field.SetValue(provider,client);
        var detail=new AvatarDetail{ProviderUniqueStorageKey=null!};
        try
        {
            var saved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Message);
            Assert.IsNotNull(detail.ProviderUniqueStorageKey);
            Assert.AreEqual(detail.Id.ToString(),detail.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.DynamoDBOASIS]);
            var stored=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<AvatarDetail>(client.Submitted!.Item["data_json"].S);
            Assert.AreEqual(detail.Id.ToString(),stored.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.DynamoDBOASIS]);
        }
        finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(0,false,true)][DataRow(0,true,false)][DataRow(0,true,true)]
    [DataRow(1,false,false)][DataRow(1,false,true)][DataRow(1,true,false)][DataRow(1,true,true)]
    [DataRow(2,false,false)][DataRow(2,false,true)][DataRow(2,true,false)][DataRow(2,true,true)]
    public async Task SaveCannotPublishIdentityWithoutAuthoritativeNativeStatus(int kind,bool synchronous,bool missing)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var fault=new MissingAcknowledgement(missing);field.SetValue(provider,fault);
        provider.IsVersionControlEnabled=true;
        var versionId=Guid.NewGuid();var previousVersionId=Guid.NewGuid();
        var avatar=new Avatar{Version=7,VersionId=versionId,PreviousVersionId=previousVersionId};
        var detail=new AvatarDetail{Version=7,VersionId=versionId,PreviousVersionId=previousVersionId};
        var holon=new Holon{Version=7,VersionId=versionId,PreviousVersionId=previousVersionId};
        try
        {
            Exception error;bool failed,saved;
            if(kind==0){var r=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);failed=r.IsError;saved=r.IsSaved;error=r.Exception;}
            else if(kind==1){var r=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);failed=r.IsError;saved=r.IsSaved;error=r.Exception;}
            else{var r=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);failed=r.IsError;saved=r.IsSaved;error=r.Exception;}
            Assert.IsTrue(failed);Assert.IsFalse(saved);Assert.AreEqual(1,fault.Calls);
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(error));Assert.IsInstanceOfType<InvalidDataException>(error.InnerException);
            Assert.AreEqual(Guid.Empty,avatar.Id);Assert.AreEqual(Guid.Empty,detail.Id);Assert.AreEqual(Guid.Empty,holon.Id);
            foreach(var row in new NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase[]{avatar,detail,holon}){
                Assert.AreEqual(7,row.Version);Assert.AreEqual(versionId,row.VersionId);Assert.AreEqual(previousVersionId,row.PreviousVersionId);
            }
        }
        finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
