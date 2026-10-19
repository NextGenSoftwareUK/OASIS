using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace ConsumerModels
{
    public sealed class AvatarInput:Avatar { }
    public sealed class DetailInput:AvatarDetail { }
    public sealed class HolonInput:Holon { }
}

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests
{
    [TestClass]
    public class DynamoInputProjectionTests
    {
        private sealed class CaptureClient:AmazonDynamoDBClient
        {
            public PutItemRequest? Submitted;
            public List<PutItemRequest> Requests=new();
            public CaptureClient():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0}) { }
            public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
                =>Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=new()});
            public override Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken cancellationToken=default)
            {Submitted=request;Requests.Add(request);return Task.FromResult(new PutItemResponse{HttpStatusCode=HttpStatusCode.OK});}
        }

        [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
        public async Task TrustedConsumerSubclassProjectsIntoCanonicalStorageDto(int kind,bool synchronous)
        {
            var provider=new DynamoDBOASIS("owned","owned","us-east-1");
            var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
            var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
            using var client=new CaptureClient();field.SetValue(provider,client);
            provider.IsVersionControlEnabled=true;
            var avatar=new ConsumerModels.AvatarInput{Username="consumer-avatar"};
            var detail=new ConsumerModels.DetailInput{Username="consumer-detail",XP=321};
            var holon=new ConsumerModels.HolonInput{Name="consumer-holon"};
            if(kind==3)holon.Children.Add(new ConsumerModels.HolonInput{Name="consumer-child"});
            try
            {
                if(kind==0){var r=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);Assert.IsFalse(r.IsError,r.Exception?.ToString());}
                else if(kind==1){var r=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(r.IsError,r.Exception?.ToString());}
                else{var r=synchronous?provider.SaveHolon(holon,saveChildren:kind==3):await provider.SaveHolonAsync(holon,saveChildren:kind==3);Assert.IsFalse(r.IsError,r.Exception?.ToString());}
                Assert.IsNotNull(client.Submitted);var json=kind==3?client.Requests.Single(r=>r.Item["id"].S==holon.Id.ToString()).Item["data_json"].S:client.Submitted.Item["data_json"].S;
                Assert.IsFalse(json.Contains("ConsumerModels",StringComparison.Ordinal));
                if(kind==0)Assert.AreEqual(avatar.Id,OasisJson.DeserializeStored<Avatar>(json).Id);
                else if(kind==1)Assert.AreEqual(321L,OasisJson.DeserializeStored<AvatarDetail>(json).XP);
                else Assert.AreEqual("consumer-holon",OasisJson.DeserializeStored<Holon>(json).Name);
                if(kind==3)
                {
                    Assert.AreEqual(1,holon.Version);Assert.AreNotEqual(Guid.Empty,holon.VersionId);
                    Assert.AreEqual(1,holon.Children.Single().Version);Assert.AreNotEqual(Guid.Empty,holon.Children.Single().VersionId);
                    Assert.AreEqual(2,client.Requests.Count);
                    var child=OasisJson.DeserializeStored<Holon>(client.Requests.Single(r=>r.Item["id"].S!=holon.Id.ToString()).Item["data_json"].S);
                    Assert.AreEqual(holon.Id,child.ParentHolonId);Assert.AreEqual("consumer-child",child.Name);
                }
            }
            finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
        }
    }
}
