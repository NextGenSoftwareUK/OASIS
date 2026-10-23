using System.Reflection;
using Google.Api.Gax.Grpc;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using ProtoTimestamp=Google.Protobuf.WellKnownTypes.Timestamp;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleReadCountTests
{
    private sealed class NearbySource(bool invalid):GoogleCloudOASIS(enableStorage:false,enableFirestore:false,enableBigQuery:false)
    {
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
            =>Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=new[]{new Holon{Id=Guid.NewGuid(),IsActive=true,MetaData=new(){{"Latitude","30"},{"Longitude","20"}}},new Holon{Id=Guid.NewGuid(),IsActive=true,MetaData=new(){{"Latitude",invalid?"invalid":"40"},{"Longitude","20"}}}},IsLoaded=true,LoadedCount=2});
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void NearbyRecountsFilteredRootsAndClearsCopiedCountOnFailure(int scenario)
    {
        IOASISNETProvider provider=new NearbySource(scenario==2);
        var result=provider.GetHolonsNearMe(scenario==1?-30000000:30000000,20000000,100,HolonType.All);
        Assert.AreEqual(scenario==2,result.IsError);Assert.AreEqual(scenario!=2,result.IsLoaded);Assert.AreEqual(scenario==0?1:0,result.LoadedCount);
        if(scenario==2)Assert.IsNull(result.Result);else Assert.AreEqual(result.LoadedCount,result.Result.Count());
    }

    private sealed class Reader<T>(T value):IAsyncStreamReader<T>
    {
        private bool _read;public T Current=>value;
        public Task<bool> MoveNext(CancellationToken token){var next=!_read;_read=true;return Task.FromResult(next);}
    }
    private sealed class QueryStream(RunQueryResponse response):FirestoreClient.RunQueryStream
    {
        public override AsyncServerStreamingCall<RunQueryResponse> GrpcCall {get;}=new(new Reader<RunQueryResponse>(response),Task.FromResult(new Metadata()),()=>Status.DefaultSuccess,()=>new Metadata(),()=>{});
    }
    private sealed class PointStream(BatchGetDocumentsResponse response):FirestoreClient.BatchGetDocumentsStream
    {
        public override AsyncServerStreamingCall<BatchGetDocumentsResponse> GrpcCall {get;}=new(new Reader<BatchGetDocumentsResponse>(response),Task.FromResult(new Metadata()),()=>Status.DefaultSuccess,()=>new Metadata(),()=>{});
    }
    private sealed class Client(int kind,bool empty=false):FirestoreClient
    {
        public readonly Guid Id=Guid.NewGuid(),Parent=Guid.NewGuid();public int Reads;
        public override FirestoreSettings Settings=>FirestoreSettings.GetDefault();
        private Document Record(string name)
        {
            var recordKind=kind<6?kind%3:kind<8?0:1;
            IHolonBase row=recordKind==0?new Avatar{Id=Id,Username="owned-read-count",Email="owned-count@example.invalid",Version=1}:
                recordKind==1?new AvatarDetail{Id=Id,Username="owned-read-count",Email="owned-count@example.invalid",Version=1}:new Holon{Id=Id,Version=1,ParentHolonId=Parent,CreatedByAvatarId=Id,MetaData=new(){{"owned-count","yes"}}};
            var stamp=ProtoTimestamp.FromDateTime(DateTime.UtcNow);
            var doc=new Document{Name=name,CreateTime=stamp,UpdateTime=stamp};
            doc.Fields.Add("id",new Value{StringValue=Id.ToString()});doc.Fields.Add("data_json",new Value{StringValue=OasisJson.Serialize(row)});return doc;
        }
        public override BatchGetDocumentsStream BatchGetDocuments(BatchGetDocumentsRequest request,CallSettings settings=null)
        {
            Reads++;return new PointStream(new(){Found=Record(request.Documents.Single()),ReadTime=ProtoTimestamp.FromDateTime(DateTime.UtcNow)});
        }
        public override RunQueryStream RunQuery(RunQueryRequest request,CallSettings settings=null)
        {
            Reads++;var collection=request.StructuredQuery.From.Single().CollectionId;
            return new QueryStream(new(){Document=empty?null:Record(request.Parent+"/"+collection+"/"+Id),ReadTime=ProtoTimestamp.FromDateTime(DateTime.UtcNow)});
        }
        public override Task<CommitResponse> CommitAsync(CommitRequest request,CallSettings settings=null)=>throw new InvalidOperationException("Read acknowledgement controls must not write.");
    }
    public static IEnumerable<object[]> Cases(){foreach(var kind in Enumerable.Range(0,10))foreach(var sync in new[]{false,true})yield return new object[]{kind,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task SuccessfulSdkReadReportsReturnedRootCount(int kind,bool sync)
    {
        var client=new Client(kind);var provider=new GoogleCloudOASIS("owned-read-count",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,FirestoreDb.Create("owned-read-count",client));
        void Check<T>(OASISResult<T> result){Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);Assert.IsFalse(result.IsSaved);}
        if(kind==0)Check(sync?provider.LoadAvatar(client.Id):await provider.LoadAvatarAsync(client.Id));
        else if(kind==1)Check(sync?provider.LoadAvatarDetail(client.Id):await provider.LoadAvatarDetailAsync(client.Id));
        else if(kind==2)Check(sync?provider.LoadHolon(client.Id,loadChildren:false):await provider.LoadHolonAsync(client.Id,loadChildren:false));
        else if(kind==3)Check(sync?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync());
        else if(kind==4)Check(sync?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync());
        else if(kind==5)Check(sync?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false));
        else if(kind==6)Check(sync?provider.LoadAvatarByUsername("owned-read-count"):await provider.LoadAvatarByUsernameAsync("owned-read-count"));
        else if(kind==7)Check(sync?provider.LoadAvatarByEmail("owned-count@example.invalid"):await provider.LoadAvatarByEmailAsync("owned-count@example.invalid"));
        else if(kind==8)Check(sync?provider.LoadAvatarDetailByUsername("owned-read-count"):await provider.LoadAvatarDetailByUsernameAsync("owned-read-count"));
        else Check(sync?provider.LoadAvatarDetailByEmail("owned-count@example.invalid"):await provider.LoadAvatarDetailByEmailAsync("owned-count@example.invalid"));
        Assert.AreEqual(1,client.Reads);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task FilteredCollectionsCountOnlyReturnedRoots(int scenario)
    {
        var client=new Client(5);var provider=new GoogleCloudOASIS("owned-read-count",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,FirestoreDb.Create("owned-read-count",client));
        var match=scenario%2==0;
        var result=scenario<2?await provider.LoadHolonsByMetaDataAsync("owned-count",match?"yes":"no",loadChildren:false):
            scenario<4?await provider.LoadHolonsForParentAsync(match?client.Parent:Guid.NewGuid(),loadChildren:false):
            await provider.ExportAllDataForAvatarByIdAsync(match?client.Id:Guid.NewGuid());
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(match?1:0,result.Result.Count());Assert.AreEqual(result.Result.Count(),result.LoadedCount);Assert.AreEqual(1,client.Reads);
    }

    [DataTestMethod][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task EmptySdkCollectionsAreSuccessfulZeroCounts(int kind)
    {
        var client=new Client(kind,true);var provider=new GoogleCloudOASIS("owned-read-count",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,FirestoreDb.Create("owned-read-count",client));
        void Check<T>(OASISResult<IEnumerable<T>> result){Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(0,result.Result.Count());Assert.AreEqual(0,result.LoadedCount);}
        if(kind==3)Check(await provider.LoadAllAvatarsAsync());else if(kind==4)Check(await provider.LoadAllAvatarDetailsAsync());else Check(await provider.LoadAllHolonsAsync(loadChildren:false));
        Assert.AreEqual(1,client.Reads);
    }
}
