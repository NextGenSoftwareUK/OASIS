using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoCallerPublicationTests
{
    private sealed class ChangingClient(Action change,bool duringRead=false):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),
        new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0})
    {
        public int Writes;
        public PutItemRequest? Submitted;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default)
        {if(duringRead)change();return Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=new()});}
        public override async Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken token=default)
        {Writes++;Submitted=request;await Task.Yield();if(!duringRead)change();return new PutItemResponse{HttpStatusCode=HttpStatusCode.OK};}
    }

    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(1,false,false)][DataRow(2,false,false)]
    [DataRow(0,true,false)][DataRow(1,true,false)][DataRow(2,true,false)]
    [DataRow(0,false,true)][DataRow(1,false,true)][DataRow(2,false,true)]
    [DataRow(0,true,true)][DataRow(1,true,true)][DataRow(2,true,true)]
    public async Task ObservedCallerVersionEditCannotBeOverwrittenOrSubmittedStale(int kind,bool synchronous,bool duringRead)
    {
        IHolonBase source=kind==0?new Avatar():kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon();
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new ChangingClient(()=>source.Version=999,duringRead);field.SetValue(provider,client);
        try{
            Exception? failure;
            if(kind==0){var result=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
            else if(kind==1){var result=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
            else{var result=synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
            Assert.AreEqual(999,source.Version);Assert.AreEqual(duringRead?0:1,client.Writes);
            if(duringRead)Assert.IsInstanceOfType<InvalidOperationException>(failure);
            else{var partial=failure as DynamoDBPartialMutationException;Assert.IsNotNull(partial);Assert.AreEqual(Guid.Parse(client.Submitted!.Item["id"].S),partial.AcknowledgedRecordIds.Single());}
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(0,false)] [DataRow(1,false)] [DataRow(2,false)]
    [DataRow(0,true)] [DataRow(1,true)] [DataRow(2,true)]
    public async Task AcknowledgedSaveDoesNotOverwriteChangedCallerId(int kind,bool synchronous)
    {
        IHolonBase source=kind==0?new Avatar():kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon();
        var replacement=Guid.NewGuid();
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new ChangingClient(()=>source.Id=replacement);field.SetValue(provider,client);
        try{
            Exception? failure;
            if(kind==0){var result=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
            else if(kind==1){var result=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
            else{var result=synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
            Assert.AreEqual(replacement,source.Id);Assert.AreEqual(1,client.Writes);
            var partial=failure as DynamoDBPartialMutationException;Assert.IsNotNull(partial);
            Assert.AreEqual(Guid.Parse(client.Submitted!.Item["id"].S),partial.AcknowledgedRecordIds.Single());
            Assert.AreNotEqual(replacement,partial.AcknowledgedRecordIds.Single());
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task BatchPreservesChangedIdentityAndDoesNotWriteLaterChangedNode(bool synchronous,bool later)
    {
        var first=new Holon();var second=new Holon();var replacement=Guid.NewGuid();
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new ChangingClient(()=>(later?second:first).Id=replacement);field.SetValue(provider,client);
        try{
            var result=synchronous?provider.SaveHolons(new[]{first,second}):await provider.SaveHolonsAsync(new[]{first,second});
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,client.Writes);
            Assert.AreEqual(replacement,(later?second:first).Id);
            var partial=result.Exception as DynamoDBPartialMutationException;Assert.IsNotNull(partial);
            Assert.AreEqual(Guid.Parse(client.Submitted!.Item["id"].S),partial.AcknowledgedRecordIds.Single());
            Assert.AreNotEqual(replacement,partial.AcknowledgedRecordIds.Single());
            if(later)Assert.AreEqual(first.Id,partial.AcknowledgedRecordIds.Single());
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
