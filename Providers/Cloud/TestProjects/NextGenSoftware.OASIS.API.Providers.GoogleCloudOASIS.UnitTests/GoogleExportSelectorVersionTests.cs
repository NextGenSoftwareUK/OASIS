using System.Reflection;
using Google.Api.Gax.Grpc;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using ProtoTime=Google.Protobuf.WellKnownTypes.Timestamp;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleExportSelectorVersionTests
{
    private sealed class Reader<T>(IEnumerable<T> values):IAsyncStreamReader<T>
    {
        private readonly IEnumerator<T> _values=values.GetEnumerator();public T Current=>_values.Current;
        public Task<bool> MoveNext(CancellationToken token)=>Task.FromResult(_values.MoveNext());
    }
    private sealed class Stream(IEnumerable<RunQueryResponse> values):FirestoreClient.RunQueryStream
    {
        public override AsyncServerStreamingCall<RunQueryResponse> GrpcCall{get;}=new(new Reader<RunQueryResponse>(values),Task.FromResult(new Metadata()),()=>Status.DefaultSuccess,()=>new Metadata(),()=>{});
    }
    private sealed class Client:FirestoreClient
    {
        internal bool CollectionRead,Repeated;
        internal int Reads;internal readonly Guid Owner=Guid.NewGuid(),Own=Guid.NewGuid(),Foreign=Guid.NewGuid(),OtherOwner=Guid.NewGuid();
        public override FirestoreSettings Settings=>FirestoreSettings.GetDefault();
        public override RunQueryStream RunQuery(RunQueryRequest request,CallSettings settings=null)
        {
            Assert.IsTrue(++Reads<20);var collection=request.StructuredQuery.From.Single().CollectionId;
            IHolonBase[] rows=collection=="avatars"?new IHolonBase[]{new Avatar{Id=Owner,Username="export-owner",Email="export@example.invalid",Version=1}}
                :new IHolonBase[]{new Holon{Id=Own,CreatedByAvatarId=Owner,Version=3,Name="own-v3"},new Holon{Id=Foreign,CreatedByAvatarId=OtherOwner,Version=3,Name="foreign-v3"}};
            if(CollectionRead){
                rows=collection=="avatars"?new IHolonBase[]{new Avatar{Id=Owner,IsActive=true,Version=1}}
                    :collection=="avatarDetails"?new IHolonBase[]{new AvatarDetail{Id=Owner,IsActive=true,Version=1,XP=321}}
                    :new IHolonBase[]{new Holon{Id=Own,IsActive=true,Version=1}};
                if(Repeated)rows=rows.Concat(rows.Take(1)).ToArray();
            }
            return new Stream(rows.Select(row=>{
                var document=new Document{Name=request.Parent+"/"+collection+"/"+row.Id,CreateTime=ProtoTime.FromDateTime(DateTime.UtcNow),UpdateTime=ProtoTime.FromDateTime(DateTime.UtcNow)};
                document.Fields.Add("id",new Value{StringValue=row.Id.ToString()});document.Fields.Add("data_json",new Value{StringValue=OasisJson.Serialize(row)});
                return new RunQueryResponse{Document=document,ReadTime=ProtoTime.FromDateTime(DateTime.UtcNow)};
            }));
        }
        public override Task<CommitResponse> CommitAsync(CommitRequest request,CallSettings settings=null)=>throw new InvalidOperationException("Read-only export must not write.");
    }

    public static IEnumerable<object[]> CollectionCases()
    {
        foreach(var kind in new[]{"avatar","detail","holon"})foreach(var sync in new[]{false,true})
        foreach(var version in new[]{0,1,3})foreach(var repeated in new[]{false,true})yield return new object[]{kind,sync,version,repeated};
    }

    [DataTestMethod][DynamicData(nameof(CollectionCases),DynamicDataSourceType.Method)]
    public async Task CollectionRejectsRepeatedIdentityBeforeVersionFiltering(string kind,bool sync,int version,bool repeated)
    {
        var client=new Client{CollectionRead=true,Repeated=repeated};var provider=Provider(client);
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<T>> result) where T:IHolonBase
        {
            if(repeated){Assert.IsTrue(result.IsError,"Repeated native identity cannot become a successful or empty selected collection.");Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);}
            else{Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsNotNull(result.Result);Assert.AreEqual(version==3?0:1,result.Result.Count());Assert.AreEqual(result.Result.Count(),result.LoadedCount);}
        }
        if(kind=="avatar")Check(sync?provider.LoadAllAvatars(version):await provider.LoadAllAvatarsAsync(version));
        else if(kind=="detail")Check(sync?provider.LoadAllAvatarDetails(version):await provider.LoadAllAvatarDetailsAsync(version));
        else Check(sync?provider.LoadAllHolons(loadChildren:false,version:version):await provider.LoadAllHolonsAsync(loadChildren:false,version:version));
        Assert.AreEqual(1,client.Reads);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task HolonExportVersionDoesNotSelectAvatarHistory(bool sync,bool email)
    {
        var client=new Client();var provider=Provider(client);
        var result=email
            ?sync?provider.ExportAllDataForAvatarByEmail("export@example.invalid",3):await provider.ExportAllDataForAvatarByEmailAsync("export@example.invalid",3)
            :sync?provider.ExportAllDataForAvatarByUsername("export-owner",3):await provider.ExportAllDataForAvatarByUsernameAsync("export-owner",3);
        Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
        Assert.AreEqual(client.Own,result.Result.Single().Id);Assert.AreEqual(3,result.Result.Single().Version);Assert.AreEqual("own-v3",result.Result.Single().Name);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NegativeVersionDoesNotReadIdentity(bool sync,bool email)
    {
        var client=new Client();var provider=Provider(client);
        var result=email
            ?sync?provider.ExportAllDataForAvatarByEmail("export@example.invalid",-1):await provider.ExportAllDataForAvatarByEmailAsync("export@example.invalid",-1)
            :sync?provider.ExportAllDataForAvatarByUsername("export-owner",-1):await provider.ExportAllDataForAvatarByUsernameAsync("export-owner",-1);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.AreEqual(0,client.Reads);
    }
    private static GoogleCloudOASIS Provider(Client client)
    {
        var provider=new GoogleCloudOASIS("owned-export",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,FirestoreDb.Create("owned-export",client));return provider;
    }
}
