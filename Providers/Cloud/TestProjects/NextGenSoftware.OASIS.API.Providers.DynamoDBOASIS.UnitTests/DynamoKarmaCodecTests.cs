using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoKarmaCodecTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task HighLevelKarmaHistorySurvivesControlledManagerAndProviderRoundTrip(bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var client=new CodecClient();field.SetValue(provider,client);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var id=Guid.NewGuid();
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new Core.Holons.AvatarDetail {Id=id,XP=222})).IsError);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
            providers.RegisterProvider(provider);
            var constructor=typeof(KarmaManager).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,
                new[] {typeof(IOASISStorageProvider),typeof(OASISDNA),typeof(ProviderManager)},null)!;
            var manager=(KarmaManager)constructor.Invoke(new object[] {provider,dna,providers});
            var result=synchronous?manager.AddKarmaToAvatar(id,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","history control")
                :await manager.AddKarmaToAvatarAsync(id,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","history control");
            Assert.IsFalse(result.IsError,result.Message+" "+result.Exception);
            Assert.IsFalse(result.IsWarning,result.Message);Assert.IsNotNull(result.Result);
            var loaded=await provider.LoadAvatarDetailAsync(id);
            Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual(500L,loaded.Result.Karma);Assert.AreEqual(222L,loaded.Result.XP);
            Assert.AreEqual(1,loaded.Result.KarmaAkashicRecords.Count,client.Json("OasisAvatarDetails",id));
            Assert.AreEqual(id,loaded.Result.KarmaAkashicRecords[0].AvatarId);
            Assert.AreEqual(500L,loaded.Result.KarmaAkashicRecords[0].TotalKarma);
        }
        finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task AvatarDetailHistorySurvivesControlledProviderSave(bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var client=new CodecClient();field.SetValue(provider,client);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var detail=new Core.Holons.AvatarDetail {Id=Guid.NewGuid(),Karma=500};
            detail.KarmaAkashicRecords.Add(new Core.Objects.KarmaAkashicRecord {AvatarId=detail.Id,Karma=500,TotalKarma=500});
            var save=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(save.IsError,save.Exception?.ToString());
            var loaded=synchronous?provider.LoadAvatarDetail(detail.Id):await provider.LoadAvatarDetailAsync(detail.Id);
            Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());
            Assert.AreEqual(1,loaded.Result.KarmaAkashicRecords.Count,client.Json("OasisAvatarDetails",detail.Id));
            Assert.AreEqual(500L,loaded.Result.KarmaAkashicRecords[0].TotalKarma);
        }
        finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    // Controlled SDK boundary only: no service, network mutations or original
    // fixture replay. This narrows native failures without certifying SDK behavior.
    private sealed class CodecClient():AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),
        new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0,Timeout=TimeSpan.FromSeconds(1)})
    {
        private readonly Dictionary<(string,string),Dictionary<string,AttributeValue>> rows=new();
        public int Writes;
        public string Json(string table,Guid id)=>rows[(table,id.ToString())]["data_json"].S;
        private static Dictionary<string,AttributeValue> Copy(Dictionary<string,AttributeValue> source)
            =>Document.FromAttributeMap(source).ToAttributeMap();
        public override Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request,CancellationToken cancellationToken=default)
            =>Task.FromResult(new DescribeTableResponse {HttpStatusCode=HttpStatusCode.OK,Table=new TableDescription {
                TableName=request.TableName,TableStatus=TableStatus.ACTIVE,KeySchema=new() {new("id",Amazon.DynamoDBv2.KeyType.HASH)},
                AttributeDefinitions=new() {new("id",ScalarAttributeType.S)}}});
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
            =>Task.FromResult(new GetItemResponse {HttpStatusCode=HttpStatusCode.OK,
                Item=rows.TryGetValue((request.TableName,request.Key["id"].S),out var row)?Copy(row):new()});
        public override Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken cancellationToken=default)
        {
            var key=(request.TableName,request.Item["id"].S);rows.TryGetValue(key,out var old);
            if(old!=null&&request.ExpressionAttributeValues?.TryGetValue(":observed",out var expected)==true)
                Assert.AreEqual(old["data_json"].S,expected.S);
            rows[key]=Copy(request.Item);Writes++;
            return Task.FromResult(new PutItemResponse {HttpStatusCode=HttpStatusCode.OK,Attributes=old==null?new():Copy(old)});
        }
        public override Task<QueryResponse> QueryAsync(QueryRequest request,CancellationToken cancellationToken=default)
        {
            var parent=request.ExpressionAttributeValues.Values.Single().S;
            return Task.FromResult(new QueryResponse {HttpStatusCode=HttpStatusCode.OK,Items=rows.Where(pair=>pair.Key.Item1==request.TableName
                &&pair.Value.TryGetValue("parentHolonId",out var value)&&value.S==parent).Select(pair=>Copy(pair.Value)).ToList()});
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task SettingsMetadataWritePreservesPublicHeaderAndUnselectedChild(bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var client=new CodecClient();field.SetValue(provider,client);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var actor=Guid.NewGuid();const string category="owned-body-scope";
            var id=new Guid(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"settings:{actor}:{category}")));
            var child=new Core.Holons.Holon {Id=Guid.NewGuid(),ParentHolonId=id,Name="owned-unselected-child"};
            var root=new Core.Holons.Holon {Id=id,Name=$"settings_avatar_{actor}_{category}",IsPublic=true,
                CreatedByAvatarId=actor,MetaData=new() {{"value","before"}},Children=new List<IHolon> {child}};
            var seeded=await provider.SaveHolonAsync(root);
            Assert.IsFalse(seeded.IsError,seeded.Exception?.ToString());
            var childJson=client.Json("OasisHolons",child.Id);var before=client.Writes;
            var manifest=((Core.Holons.Holon)(await provider.LoadHolonAsync(id,loadChildren:false)).Result).ChildRoleManifestJson;
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
            providers.RegisterProvider(provider);
            var manager=new HolonManager(provider,dna,providers);
            var result=synchronous?manager.SaveSettings(actor,category,new() {{"value","after"}})
                :await manager.SaveSettingsAsync(actor,category,new() {{"value","after"}});
            Assert.IsFalse(result.IsError,result.Message+" "+result.Exception);Assert.IsTrue(result.Result);
            var reloaded=await provider.LoadHolonAsync(id,loadChildren:false);
            Assert.IsFalse(reloaded.IsError,reloaded.Message+" "+reloaded.Exception);Assert.IsNotNull(reloaded.Result);
            var loaded=reloaded.Result;
            Assert.IsTrue(loaded.IsPublic);Assert.AreEqual("after",loaded.MetaData["value"].ToString());
            Assert.AreEqual(manifest,((Core.Holons.Holon)loaded).ChildRoleManifestJson);
            Assert.AreEqual(childJson,client.Json("OasisHolons",child.Id));Assert.AreEqual(before+1,client.Writes);
        }
        finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task KarmaManagerUsesRealProviderCodecAcrossControlledSdkRoundTrips(bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var client=new CodecClient();field.SetValue(provider,client);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
            providers.RegisterProvider(provider);
            var constructor=typeof(KarmaManager).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,
                new[] {typeof(IOASISStorageProvider),typeof(OASISDNA),typeof(ProviderManager)},null)!;
            var manager=(KarmaManager)constructor.Invoke(new object[] {provider,dna,providers});var id=Guid.NewGuid();
            var result=synchronous?manager.AddKarma(id,500,KarmaSourceType.Game,"owned codec diagnostic")
                :await manager.AddKarmaAsync(id,500,KarmaSourceType.Game,"owned codec diagnostic");
            Assert.IsFalse(result.IsError,result.Message+" "+result.Exception);Assert.IsTrue(result.Result);
            var balance=synchronous?manager.GetKarma(id):await manager.GetKarmaAsync(id);
            Assert.IsFalse(balance.IsError,balance.Message+" "+balance.Exception);Assert.AreEqual(500L,balance.Result);
        }
        finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
