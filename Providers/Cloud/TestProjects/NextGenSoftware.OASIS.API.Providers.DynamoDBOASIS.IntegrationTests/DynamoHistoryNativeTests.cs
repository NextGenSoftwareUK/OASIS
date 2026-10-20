using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public partial class DynamoHistoryNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeOversizedPutIsRejectedWithoutPublicationOrUnknownOutcome(bool aws)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;var journal=Path.Combine(work,$"oversized-put-{aws}.pending.json");Assert.IsFalse(File.Exists(journal));
        var row=new Holon{Id=Guid.NewGuid(),Name="owned-oversized-put",Description=new string('x',450*1024),Version=9,VersionId=Guid.NewGuid()};var versionId=row.VersionId;
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);var key=new Dictionary<string,AttributeValue>{{"id",new(){S=row.Id.ToString()}}};
            Assert.AreEqual(0,(await verifier.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=key,ConsistentRead=true})).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id=row.Id,table="OasisHolons",aws,state="before-oversized-sdk-put"}));file.Flush(true);}
            var rejected=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.IsSaved);Assert.IsNull(rejected.Result);Assert.AreEqual(1,client.Writes);
            Assert.IsInstanceOfType<AmazonDynamoDBException>(rejected.Exception);var cause=(AmazonDynamoDBException)rejected.Exception;
            Assert.AreEqual(HttpStatusCode.BadRequest,cause.StatusCode);Assert.AreEqual("ValidationException",cause.ErrorCode);Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(cause));
            Assert.AreEqual(9,row.Version);Assert.AreEqual(versionId,row.VersionId);Assert.AreEqual(0,row.ProviderUniqueStorageKey.Count);
            Assert.AreEqual(0,(await verifier.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=key,ConsistentRead=true})).Item.Count);File.Delete(journal);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
    private static AmazonDynamoDBConfig Configuration()=>new(){ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0};
    private static AmazonDynamoDBClient Client()=>new(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),
        Configuration());
    private sealed class FaultClient:AmazonDynamoDBClient
    {
        public string Mode="normal";public int Writes;public Func<Task>? BeforePut;
        public Func<Task>? AfterScan;
        public override async Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken cancellationToken=default)
        {
            var response=await base.ScanAsync(request,cancellationToken);
            if(AfterScan!=null){var callback=AfterScan;AfterScan=null;await callback();}
            return response;
        }
        public bool LoseTransactionReturn;public int Transactions;public string? TransactionToken;public Func<Task>? BeforeTransaction;
        public readonly IOException Lost=new("owned response dropped after actual SDK commit");
        public FaultClient():base(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),Configuration()){}
        public override async Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken cancellationToken=default)
        {
            Writes++;if(BeforePut!=null){var callback=BeforePut;BeforePut=null;await callback();}
            var actual=await base.PutItemAsync(request,cancellationToken);
            if(Mode=="lost")throw Lost;
            if(Mode=="missing-ack")actual.Attributes.Clear();
            return actual;
        }
        public override async Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default)
        {
            Transactions++;TransactionToken=request.ClientRequestToken;
            if(BeforeTransaction!=null){var callback=BeforeTransaction;BeforeTransaction=null;await callback();}
            var actual=await base.TransactWriteItemsAsync(request,token);
            if(LoseTransactionReturn)throw Lost;
            return actual;
        }
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RealAwsLifecycleLostReturnRetainsOriginalNativeIdempotencyToken(bool softDelete)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;var journal=Path.Combine(work,$"native-lifecycle-token-{softDelete}.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var name="owned-native-token-"+id.ToString("N");
        var provider=new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"){IsVersionControlEnabled=true};
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new FaultClient{LoseTransactionReturn=true};using var verifier=Client();field.SetValue(provider,client);
        var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"})Assert.AreEqual(0,(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,softDelete,state="before-owned-aws-lifecycle-lost-return"}));file.Flush(true);
            }
            await Write(provider,new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true},"OasisAvatars");
            await Write(provider,new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true},"OasisAvatarDetails");
            var result=await provider.DeleteAvatarAsync(id,softDelete);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
            var evidence=(DynamoDBMutationReconciliationException)result.Exception;Assert.AreSame(client.Lost,evidence.InnerException);
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(evidence));
            Assert.IsTrue(Guid.TryParseExact(client.TransactionToken,"N",out _));Assert.AreEqual(client.TransactionToken,evidence.ClientRequestToken);
            Assert.AreEqual(string.Empty,evidence.OriginalTransactionId);Assert.AreEqual(0,NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.OriginalTransactionIds(evidence).Count);
            foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"}){
                var actual=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true});Assert.AreEqual(HttpStatusCode.OK,actual.HttpStatusCode);
                if(!softDelete){Assert.AreEqual(0,actual.Item.Count);continue;}
                Assert.IsTrue(actual.Item["is_deleted"].BOOL);var json=actual.Item["data_json"].S;var document=JObject.Parse(json);
                Assert.AreEqual(2,document["Version"]!.Value<int>());Assert.AreEqual(name,document[EmbeddedRecordHistory.PropertyName]!["records"]![0]!["Username"]!.Value<string>());
                await RemoveOwned(verifier,table,id,json);
                Assert.AreEqual(0,(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item.Count);
            }
            Assert.AreEqual(1,client.Transactions,"Do not generate a new transaction/replay to resolve an uncertain return.");File.Delete(journal);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
    private static async Task<IHolonBase> Write(DynamoDBOASIS provider,IHolonBase row,string table)
    {
        var item=new Dictionary<string,AttributeValue>{{"id",new(){S=row.Id.ToString()}},{"data_json",new(){S=OasisJson.Serialize(row)}},{"is_deleted",new(){BOOL=row.IsDeleted}}};
        if(row is IAvatar avatar){item["username"]=new(){S=avatar.Username};item["email"]=new(){S=avatar.Email};}
        else if(row is IAvatarDetail detail){item["username"]=new(){S=detail.Username};item["email"]=new(){S=detail.Email};}
        else if(row is IHolon holon){item["parentHolonId"]=new(){S=holon.ParentHolonId.ToString()};item["holon_type"]=new(){N=((int)holon.HolonType).ToString()};}
        var task=(Task)typeof(DynamoDBOASIS).GetMethod("WriteRecordAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(row.GetType()).Invoke(provider,new object?[]{new PutItemRequest{TableName=table,Item=item},true,(Action)(()=>{}),null})!;
        await task;return (IHolonBase)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }
    private static async Task RemoveOwned(AmazonDynamoDBClient client,string table,Guid id,string expected)
    {
        var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
        var deleted=await client.DeleteItemAsync(new DeleteItemRequest{TableName=table,Key=key,ConditionExpression="data_json = :expected",ExpressionAttributeValues=new(){{":expected",new(){S=expected}}},ReturnValues=ReturnValue.ALL_OLD});
        Assert.AreEqual(HttpStatusCode.OK,deleted.HttpStatusCode);Assert.AreEqual(expected,deleted.Attributes["data_json"].S);
        Assert.AreEqual(0,(await client.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item.Count);
    }
    private static async Task SavePublic(DynamoDBOASIS provider,IHolonBase row)
    {
        if(row is IAvatar avatar){var saved=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreSame(row,saved.Result);}
        else if(row is IAvatarDetail detail){var saved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreSame(row,saved.Result);}
        else{var saved=await provider.SaveHolonAsync((IHolon)row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreSame(row,saved.Result);}
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task RealSdkCreateReplacementAndIndependentArchiveRead(int kind)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")??throw new InvalidOperationException("Use retained history runner.");
        var journal=Path.Combine(work,$"record-history-{kind}.pending.json");Assert.IsFalse(File.Exists(journal),"Reconcile original native fixture before another write.");
        var useAws=kind>=3;kind%=3;
        var id=Guid.NewGuid();var name="history-"+id.ToString("N");
        IHolonBase row=kind==0?new Avatar{Id=id,Username=name,Email=name+"@example.invalid"}:kind==1?new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid"}:new Holon{Id=id,Name=name};
        var owner=Guid.NewGuid();row.CreatedByAvatarId=owner;row.MetaData=new(){["owned_history_fixture"]="first"};
        var child=kind==2?new Holon{Id=Guid.NewGuid(),ParentHolonId=id,CreatedByAvatarId=owner,Name="independent-current-child",Version=5}:null;
        var table=kind==0?"OasisAvatars":kind==1?"OasisAvatarDetails":"OasisHolons";
        DynamoDBOASIS provider=useAws
            ? new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000")
            : new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=Client();using var verifier=Client();field.SetValue(provider,client);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
            Assert.AreEqual(0,(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{table,id,childId=child?.Id,endpoint="http://127.0.0.1:18000",state="before-native-history-create"}));file.Flush(true);
            }
            provider.IsVersionControlEnabled=true;
            void SetAdditionalCredentials(IAvatar avatar,string suffix){
                avatar.RefreshTokens=new(){new NextGenSoftware.OASIS.API.Core.Objects.RefreshToken{Token=name+suffix+"-refresh",Expires=DateTime.UtcNow.AddDays(1)}};
                avatar.ProviderWallets=new(){[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS]=new(){new NextGenSoftware.OASIS.API.Core.Objects.ProviderWallet{Id=Guid.NewGuid(),PublicKey=name+suffix+"-public",PrivateKey=name+suffix+"-private"}}};
            }
            async Task CheckAdditionalCredentials(string suffix,bool present){
                var results=new[]{await provider.LoadAvatarByRefreshTokenAsync(name+suffix+"-refresh"),provider.LoadAvatarByPublicKey(name+suffix+"-public"),await provider.LoadAvatarByPrivateKeyAsync(name+suffix+"-private")};
                foreach(var result in results){
                    if(present){Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(id,result.Result.Id);}
                    else{Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,result.ErrorCode);}
                }
            }
            if(row is Avatar initialCredentials){initialCredentials.VerificationToken=name+"-old-verification";initialCredentials.ResetToken=name+"-old-reset";SetAdditionalCredentials(initialCredentials,"-old");}
            await SavePublic(provider,row);Assert.AreEqual(1,row.Version);var firstVersionId=row.VersionId;
            if(row is Avatar){
                var verification=await provider.LoadAvatarByVerificationTokenAsync(name+"-old-verification");
                Assert.IsFalse(verification.IsError,verification.Exception?.ToString());Assert.AreEqual(id,verification.Result.Id);
                var reset=provider.LoadAvatarByResetToken(name+"-old-reset");
                Assert.IsFalse(reset.IsError,reset.Exception?.ToString());Assert.AreEqual(id,reset.Result.Id);
                await CheckAdditionalCredentials("-old",true);
            }
            var providerType=useAws?NextGenSoftware.OASIS.API.Core.Enums.ProviderType.AWSOASIS:NextGenSoftware.OASIS.API.Core.Enums.ProviderType.DynamoDBOASIS;
            Assert.AreEqual(id.ToString(),row.ProviderUniqueStorageKey[providerType]);
            provider.IsVersionControlEnabled=false;
            row.MetaData["owned_history_fixture"]="second";
            if(row is IAvatar renamedAvatar){renamedAvatar.Username=name+"-current";renamedAvatar.Email=name+"-current@example.invalid";renamedAvatar.VerificationToken=name+"-new-verification";renamedAvatar.ResetToken=name+"-new-reset";}
            else if(row is IAvatarDetail renamedDetail){renamedDetail.Username=name+"-current";renamedDetail.Email=name+"-current@example.invalid";}
            else if(row is IHolon movedRoot)movedRoot.ParentHolonId=Guid.NewGuid();
            if(row is IAvatar rotatingCredentials)SetAdditionalCredentials(rotatingCredentials,"-new");
            await SavePublic(provider,row);Assert.AreEqual(2,row.Version);Assert.AreEqual(firstVersionId,row.PreviousVersionId);
            if(child!=null)await Write(provider,child,table);
            if(kind==0){
                var selected=await provider.LoadAvatarAsync(id,1);Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual("first",selected.Result.MetaData["owned_history_fixture"].ToString());
                Assert.AreEqual(name+"-old-verification",selected.Result.VerificationToken);
                Assert.AreEqual(name+"-old-reset",selected.Result.ResetToken);
                Assert.AreEqual(name+"-old-refresh",selected.Result.RefreshTokens.Single().Token);
                Assert.AreEqual(name+"-old-public",selected.Result.ProviderWallets[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS].Single().PublicKey);
                Assert.AreEqual(name+"-old-private",selected.Result.ProviderWallets[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS].Single().PrivateKey);
                await CheckAdditionalCredentials("-old",false);await CheckAdditionalCredentials("-new",true);
                var oldVerification=provider.LoadAvatarByVerificationToken(name+"-old-verification");
                Assert.IsTrue(oldVerification.IsError);Assert.IsFalse(oldVerification.IsLoaded);Assert.IsNull(oldVerification.Result);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,oldVerification.ErrorCode);
                var oldReset=await provider.LoadAvatarByResetTokenAsync(name+"-old-reset");
                Assert.IsTrue(oldReset.IsError);Assert.IsFalse(oldReset.IsLoaded);Assert.IsNull(oldReset.Result);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,oldReset.ErrorCode);
                var currentVerification=provider.LoadAvatarByVerificationToken(name+"-new-verification");
                Assert.IsFalse(currentVerification.IsError,currentVerification.Exception?.ToString());Assert.AreEqual(id,currentVerification.Result.Id);
                var currentReset=await provider.LoadAvatarByResetTokenAsync(name+"-new-reset");
                Assert.IsFalse(currentReset.IsError,currentReset.Exception?.ToString());Assert.AreEqual(id,currentReset.Result.Id);
                Assert.AreEqual(id,(await provider.LoadAvatarByProviderKeyAsync(id.ToString(),1)).Result.Id);
                Assert.AreEqual(id,(await provider.LoadAvatarByUsernameAsync(name,1)).Result.Id);Assert.AreEqual(id,(await provider.LoadAvatarByEmailAsync(name+"@example.invalid",1)).Result.Id);
                Assert.IsTrue((await provider.LoadAllAvatarsAsync(1)).Result.Any(value=>value.Id==id&&value.Version==1));
                Assert.IsTrue((await provider.LoadAvatarAsync(id,99)).IsError);
                var credentials=await provider.LoadAvatarByVerificationTokenAsync("archived-token-must-not-authenticate",1);Assert.IsTrue(credentials.IsError);Assert.IsInstanceOfType(credentials.Exception,typeof(NotSupportedException));
            }else if(kind==1){
                var selected=await provider.LoadAvatarDetailAsync(id,1);Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual("first",selected.Result.MetaData["owned_history_fixture"].ToString());
                Assert.AreEqual(id,(await provider.LoadAvatarDetailByUsernameAsync(name,1)).Result.Id);Assert.AreEqual(id,(await provider.LoadAvatarDetailByEmailAsync(name+"@example.invalid",1)).Result.Id);
                Assert.IsTrue((await provider.LoadAllAvatarDetailsAsync(1)).Result.Any(value=>value.Id==id&&value.Version==1));Assert.IsTrue((await provider.LoadAvatarDetailAsync(id,99)).IsError);
            }else{
                var selected=await provider.LoadHolonAsync(id,loadChildren:false,version:1);Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual("first",selected.Result.MetaData["owned_history_fixture"].ToString());
                Assert.AreEqual(id,(await provider.LoadHolonAsync(id.ToString(),loadChildren:false,version:1)).Result.Id);
                Assert.IsTrue((await provider.LoadAllHolonsAsync(loadChildren:false,version:1)).Result.Any(value=>value.Id==id&&value.Version==1));
                Assert.IsTrue((await provider.LoadHolonsByMetaDataAsync("owned_history_fixture","first",loadChildren:false,version:1)).Result.Any(value=>value.Id==id));
                Assert.IsTrue((await provider.ExportAllAsync(1)).Result.Any(value=>value.Id==id&&value.Version==1));Assert.IsTrue((await provider.LoadHolonAsync(id,loadChildren:false,version:99)).IsError);
                var linked=await provider.LoadHolonAsync(id,loadChildrenFromProvider:true,version:1);Assert.IsFalse(linked.IsError,linked.Message);Assert.AreEqual(1,linked.Result.Version);Assert.AreEqual(5,linked.Result.Children.Single().Version);
                var parents=await provider.LoadHolonsForParentAsync(Guid.Empty,loadChildrenFromProvider:true,version:1);Assert.IsFalse(parents.IsError,parents.Message);Assert.AreEqual(5,parents.Result.Single(value=>value.Id==id).Children.Single().Version);
                var bounded=await provider.LoadHolonsForParentAsync(Guid.Empty,maxChildDepth:1,loadChildrenFromProvider:true,version:1);
                Assert.IsFalse(bounded.IsError,bounded.Exception?.ToString());Assert.AreEqual(0,bounded.Result.Single(value=>value.Id==id).Children.Count);
                var boundedSync=provider.LoadHolonsForParent(Guid.Empty.ToString(),maxChildDepth:1,loadChildrenFromProvider:true,version:1);
                Assert.IsFalse(boundedSync.IsError,boundedSync.Exception?.ToString());Assert.AreEqual(0,boundedSync.Result.Single(value=>value.Id==id).Children.Count);
                var search=await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){["owned_history_fixture"]="first"}},version:1);
                Assert.IsFalse(search.IsError,search.Message);Assert.AreEqual(1,search.Result.SearchResultHolons.Single(value=>value.Id==id).Version);
                var exported=await provider.ExportAllDataForAvatarByIdAsync(owner,1);Assert.IsFalse(exported.IsError,exported.Message);Assert.AreEqual(5,exported.Result.Single(value=>value.Id==id).Children.Single().Version);
            }
            var actual=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true});Assert.AreEqual(HttpStatusCode.OK,actual.HttpStatusCode);
            var document=JObject.Parse(actual.Item["data_json"].S);
            Assert.AreEqual(2,document["Version"]!.Value<int>());
            var archived=(JArray)document[EmbeddedRecordHistory.PropertyName]!["records"]!;Assert.AreEqual(1,archived.Count);
            Assert.AreEqual(id.ToString(),archived[0]!["Id"]!.Value<string>());Assert.AreEqual(1,archived[0]!["Version"]!.Value<int>());
            Assert.AreEqual("first",archived[0]!["MetaData"]!["owned_history_fixture"]!.Value<string>());Assert.AreEqual("second",document["MetaData"]!["owned_history_fixture"]!.Value<string>());
            if(child!=null){var childItem=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=child.Id.ToString()}}},ConsistentRead=true});await RemoveOwned(verifier,table,child.Id,childItem.Item["data_json"].S);}
            await RemoveOwned(verifier,table,id,actual.Item["data_json"].S);
            File.Delete(journal);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [TestMethod][TestCategory("Reconciliation")]
    public async Task ReconcileKnownHistoryReadFixture()
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;var journal=Path.Combine(work,"record-history-2.pending.json");
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
        var id=Guid.Parse("80bdb624-de8d-4ef8-92ca-395e9c7c4adb");var childId=Guid.Parse("9fd4e0cd-bea8-4f91-b882-d228756b269c");
        Assert.AreEqual(id,intent.RootElement.GetProperty("id").GetGuid());Assert.AreEqual(childId,intent.RootElement.GetProperty("childId").GetGuid());
        Assert.AreEqual("OasisHolons",intent.RootElement.GetProperty("table").GetString());Assert.AreEqual("before-native-history-create",intent.RootElement.GetProperty("state").GetString());
        using var verifier=Client();
        var root=(await verifier.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true})).Item;
        var child=(await verifier.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=childId.ToString()}}},ConsistentRead=true})).Item;
        var rootDoc=JObject.Parse(root["data_json"].S);var childDoc=JObject.Parse(child["data_json"].S);
        Assert.AreEqual(2,rootDoc["Version"]!.Value<int>());Assert.AreEqual(5,childDoc["Version"]!.Value<int>());
        Assert.AreEqual("second",rootDoc["MetaData"]!["owned_history_fixture"]!.Value<string>());Assert.AreEqual(id.ToString(),childDoc["ParentHolonId"]!.Value<string>());
        Assert.AreEqual("first",rootDoc[EmbeddedRecordHistory.PropertyName]!["records"]![0]!["MetaData"]!["owned_history_fixture"]!.Value<string>());
        var provider=new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        try{
            var historical=await provider.LoadHolonAsync(id,loadChildrenFromProvider:true,version:1);Assert.IsFalse(historical.IsError,historical.Message);Assert.AreEqual(1,historical.Result.Version);Assert.AreEqual(5,historical.Result.Children.Single().Version);
            var searched=await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){["owned_history_fixture"]="first"}},version:1);
            Assert.IsFalse(searched.IsError,searched.Message);Assert.AreEqual(1,searched.Result.SearchResultHolons.Single(value=>value.Id==id).Version);
            var exported=await provider.ExportAllDataForAvatarByIdAsync(Guid.Parse(rootDoc["CreatedByAvatarId"]!.Value<string>()!),1);Assert.IsFalse(exported.IsError,exported.Message);Assert.AreEqual(5,exported.Result.Single(value=>value.Id==id).Children.Single().Version);
            await RemoveOwned(verifier,"OasisHolons",childId,child["data_json"].S);await RemoveOwned(verifier,"OasisHolons",id,root["data_json"].S);
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task PublicOversizedSavePreservesCommittedRecordArchiveAndCallerVersion(int variant)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var journal=Path.Combine(work,$"public-capacity-{variant}.pending.json");Assert.IsFalse(File.Exists(journal));
        var kind=variant%3;var useAws=variant>=3;var id=Guid.NewGuid();var name="capacity-"+id.ToString("N");
        IHolonBase row=kind==0?new Avatar{Id=id,Username=name,Email=name+"@example.invalid"}:kind==1?new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid"}:new Holon{Id=id,Name=name};
        var table=kind==0?"OasisAvatars":kind==1?"OasisAvatarDetails":"OasisHolons";
        DynamoDBOASIS provider=useAws
            ? new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000")
            : new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);
        var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.AreEqual(0,(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,table,variant,state="before-native-public-capacity"}));file.Flush(true);
            }
            provider.IsVersionControlEnabled=true;
            row.MetaData=new(){["owned_history_fixture"]="first"};await SavePublic(provider,row);
            row.MetaData["owned_history_fixture"]="second";await SavePublic(provider,row);
            Assert.AreEqual(2,row.Version);var versionId=row.VersionId;var keys=JsonSerializer.Serialize(row.ProviderUniqueStorageKey);
            var before=(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item["data_json"].S;
            Assert.AreEqual(1,((JArray)JObject.Parse(before)[EmbeddedRecordHistory.PropertyName]!["records"]!).Count);
            client.Writes=0;row.MetaData["padding"]=new string('x',410*1024);
            if(row is IAvatar avatar)Check(await provider.SaveAvatarAsync(avatar));
            else if(row is IAvatarDetail detail)Check(await provider.SaveAvatarDetailAsync(detail));
            else Check(await provider.SaveHolonAsync((IHolon)row,saveChildren:false));
            Assert.AreEqual(1,client.Writes);Assert.AreEqual(2,row.Version);Assert.AreEqual(versionId,row.VersionId);
            Assert.AreEqual(keys,JsonSerializer.Serialize(row.ProviderUniqueStorageKey));
            var after=(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item["data_json"].S;
            Assert.AreEqual(before,after,"Rejected oversized public save must not trim or overwrite committed data/history.");
            await RemoveOwned(verifier,table,id,before);File.Delete(journal);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
        static void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<AmazonDynamoDBException>(result.Exception);
            var native=(AmazonDynamoDBException)result.Exception;
            Assert.AreEqual("ValidationException",native.ErrorCode);Assert.AreEqual(HttpStatusCode.BadRequest,native.StatusCode);
            Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(native));
        }
    }

    [TestMethod]
    public async Task RealAtomicAvatarSoftDeletePreservesBothArchivesAfterDisablingHistory()
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var journal=Path.Combine(work,"lifecycle-history.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var name="lifecycle-"+id.ToString("N");
        var provider=new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=Client();using var verifier=Client();field.SetValue(provider,client);
        var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"})
                Assert.AreEqual(0,(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,tables=new[]{"OasisAvatars","OasisAvatarDetails"},state="before-native-lifecycle-history"}));file.Flush(true);
            }
            await Write(provider,new Avatar{Id=id,Username=name,Email=name+"@example.invalid"},"OasisAvatars");
            await Write(provider,new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid"},"OasisAvatarDetails");
            provider.IsVersionControlEnabled=false;
            var deleted=await provider.DeleteAvatarAsync(id,softDelete:true);Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.Result);
            foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"}){
                var actual=(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item;
                var document=JObject.Parse(actual["data_json"].S);Assert.AreEqual(2,document["Version"]!.Value<int>());
                Assert.IsTrue(actual["is_deleted"].BOOL);Assert.AreEqual(1,document[EmbeddedRecordHistory.PropertyName]!["records"]![0]!["Version"]!.Value<int>());
                Assert.AreEqual(name,document[EmbeddedRecordHistory.PropertyName]!["records"]![0]!["Username"]!.Value<string>());
            }
            Assert.AreEqual(1,(await provider.LoadAvatarAsync(id,1)).Result.Version);
            Assert.AreEqual(1,(await provider.LoadAvatarDetailAsync(id,1)).Result.Version);
            foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"}){
                var actual=(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item;
                await RemoveOwned(verifier,table,id,actual["data_json"].S);
            }
            File.Delete(journal);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow("race")][DataRow("lost")][DataRow("missing-ack")][DataRow("capacity")]
    public async Task RealSdkConflictLostAckAndSizeRejectionPreserveCommittedHistory(string mode)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")??throw new InvalidOperationException("Use retained runner.");
        var journal=Path.Combine(work,"fault-history-"+mode+".pending.json");Assert.IsFalse(File.Exists(journal));
        var provider=new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        var rival=new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;var rivalOriginal=(AmazonDynamoDBClient)field.GetValue(rival)!;
        using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);field.SetValue(rival,verifier);
        const string table="OasisHolons";var row=new Holon{Id=Guid.NewGuid(),Name="owned-fault-fixture",MetaData=new(){["owned_history_fixture"]="first"}};
        var key=new Dictionary<string,AttributeValue>{{"id",new(){S=row.Id.ToString()}}};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.IsFalse((await rival.ActivateProviderAsync()).IsError);
            Assert.AreEqual(0,(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{table,id=row.Id,mode,state="before-native-fault-fixture"}));file.Flush(true);
            }
            await Write(provider,row,table);
            var before=(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item["data_json"].S;
            client.Writes=0;row.MetaData["owned_history_fixture"]="attempted";
            if(mode=="race"){
                client.BeforePut=async()=>{var competing=new Holon{Id=row.Id,Name=row.Name,MetaData=new(){["owned_history_fixture"]="concurrent"}};await Write(rival,competing,table);};
                await Assert.ThrowsExceptionAsync<ConditionalCheckFailedException>(()=>Write(provider,row,table));
            }else if(mode=="lost"){
                client.Mode="lost";var error=await Assert.ThrowsExceptionAsync<DynamoDBMutationReconciliationException>(()=>Write(provider,row,table));Assert.AreSame(client.Lost,error.InnerException);
            }else if(mode=="missing-ack"){
                client.Mode="missing-ack";var error=await Assert.ThrowsExceptionAsync<DynamoDBMutationReconciliationException>(()=>Write(provider,row,table));Assert.IsInstanceOfType(error.InnerException,typeof(InvalidDataException));
            }else{
                row.MetaData["padding"]=new string('x',410*1024);
                var error=await Assert.ThrowsExceptionAsync<AmazonDynamoDBException>(()=>Write(provider,row,table));
                Assert.AreEqual("ValidationException",error.ErrorCode);Assert.AreEqual(HttpStatusCode.BadRequest,error.StatusCode);
                Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(error));
            }
            Assert.AreEqual(1,client.Writes);Assert.AreEqual(0,row.Version);
            var actual=(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item;
            var document=JObject.Parse(actual["data_json"].S);Assert.AreEqual(mode=="capacity"?1:2,document["Version"]!.Value<int>());
            if(mode=="capacity")Assert.AreEqual(before,actual["data_json"].S,"Native size rejection must not alter or trim prior committed data/history.");
            else{
                var history=(JArray)document[EmbeddedRecordHistory.PropertyName]!["records"]!;Assert.AreEqual(1,history.Count);
                Assert.AreEqual("first",history[0]!["MetaData"]!["owned_history_fixture"]!.Value<string>());
                Assert.AreEqual(mode=="race"?"concurrent":"attempted",document["MetaData"]!["owned_history_fixture"]!.Value<string>());
            }
            var removed=await verifier.DeleteItemAsync(new DeleteItemRequest{TableName=table,Key=key,ConditionExpression="data_json = :expected",ExpressionAttributeValues=new(){{":expected",new(){S=actual["data_json"].S}}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,removed.HttpStatusCode);Assert.AreEqual(actual["data_json"].S,removed.Attributes["data_json"].S);
            Assert.AreEqual(0,(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item.Count);File.Delete(journal);
        }finally{field.SetValue(provider,original);field.SetValue(rival,rivalOriginal);await provider.DeActivateProviderAsync();await rival.DeActivateProviderAsync();}
    }
}
