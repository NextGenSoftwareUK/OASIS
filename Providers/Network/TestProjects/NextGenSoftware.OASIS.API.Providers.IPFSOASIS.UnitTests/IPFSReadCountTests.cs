using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Ipfs.Http;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSReadCountTests
{
    private sealed class Reader(Func<string> read):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {Assert.EndsWith("/cat",request.RequestUri!.AbsolutePath);return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(read())});}
    }
    public static IEnumerable<object[]> Cases()
    {foreach(var operation in Enumerable.Range(0,18))foreach(var sync in new[]{false,true})foreach(var present in new[]{false,true})yield return new object[]{operation,sync,present};}

    [Theory,MemberData(nameof(Cases))]
    public async Task ReadsCountReturnedRootsAndKeepAbsenceDistinct(int operation,bool sync,bool present)
    {
        const string indexCid="QmPcSpfsigLFxjKLHvhKtBqiwtjmtHViHh9G4Hi8vdriSP",recordCid="Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdk";
        var provider=new IPFSOASIS(new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{StorageProviders=new StorageProviderSettings{
            IPFSOASIS=new IPFSOASISSettings{ConnectionString="http://127.0.0.1:1",LookUpIPFSAddress=indexCid}}}});
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var key=RandomNumberGenerator.GetBytes(32);
        typeof(IPFSOASIS).GetField("_storageKeyResolver",flags)!.SetValue(provider,(Func<byte[]>)(()=> (byte[])key.Clone()));
        typeof(IPFSOASIS).GetField("_ipfsClient",flags)!.SetValue(provider,new IpfsClient("http://127.0.0.1:1"));
        var id=Guid.NewGuid();var parent=Guid.NewGuid();var owner=Guid.NewGuid();
        var kind=operation<5?"avatar":operation<9?"avatar-detail":"holon";
        IHolonBase row=kind=="avatar"?new Avatar{Id=id,Username="owned",Email="owned@example.invalid"}
            :kind=="avatar-detail"?new AvatarDetail{Id=id,Username="owned",Email="owned@example.invalid"}
            :new Holon{Id=id,Name="oak",ParentHolonId=parent,CreatedByAvatarId=owner,MetaData=new(){{"zone","north"}}};
        row.ProviderUniqueStorageKey=new(){{ProviderType.IPFSOASIS,recordCid}};
        var index=JsonConvert.SerializeObject(present?new Dictionary<string,HolonResume>{{recordCid,new(){Id=id,RecordKind=kind,login="owned",email="owned@example.invalid",ProviderUniqueStorageKey=new(){{ProviderType.IPFSOASIS,recordCid}}}}}:new());
        var indexEnvelope=(string)typeof(IPFSOASIS).GetMethod("ProtectIndex",flags)!.Invoke(provider,new object[]{index})!;
        var recordEnvelope=(string)typeof(IPFSOASIS).GetMethod("ProtectRecord",flags)!.Invoke(provider,new object[]{kind,id,OasisJson.Serialize(row)})!;
        var reads=0;using var http=new HttpClient(new Reader(()=>++reads==1?indexEnvelope:recordEnvelope));
        var api=typeof(IpfsClient).GetField("api",BindingFlags.Static|BindingFlags.NonPublic)!;var original=api.GetValue(null);api.SetValue(null,http);
        try{
            void Check<T>(OASISResult<T> result,bool point=false){
                Assert.Equal(!present&&point,result.IsError);Assert.Equal(present||!point,result.IsLoaded);
                Assert.Equal(present?1:0,result.LoadedCount);if(!present&&point)Assert.Null(result.Result);
            }
            switch(operation){
                case 0:Check(sync?provider.LoadAvatar(id):await provider.LoadAvatarAsync(id),true);break;
                case 1:Check(sync?provider.LoadAvatarByUsername("owned"):await provider.LoadAvatarByUsernameAsync("owned"),true);break;
                case 2:Check(sync?provider.LoadAvatarByEmail("owned@example.invalid"):await provider.LoadAvatarByEmailAsync("owned@example.invalid"),true);break;
                case 3:Check(sync?provider.LoadAvatarByProviderKey(recordCid):await provider.LoadAvatarByProviderKeyAsync(recordCid),true);break;
                case 4:Check(sync?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync());break;
                case 5:Check(sync?provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id),true);break;
                case 6:Check(sync?provider.LoadAvatarDetailByUsername("owned"):await provider.LoadAvatarDetailByUsernameAsync("owned"),true);break;
                case 7:Check(sync?provider.LoadAvatarDetailByEmail("owned@example.invalid"):await provider.LoadAvatarDetailByEmailAsync("owned@example.invalid"),true);break;
                case 8:Check(sync?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync());break;
                case 9:Check(sync?provider.LoadHolon(id,loadChildren:false):await provider.LoadHolonAsync(id,loadChildren:false),true);break;
                case 10:Check(sync?provider.LoadHolon(recordCid,loadChildren:false):await provider.LoadHolonAsync(recordCid,loadChildren:false),true);break;
                case 11:Check(sync?provider.LoadAllHolons(HolonType.All,loadChildren:false):await provider.LoadAllHolonsAsync(HolonType.All,loadChildren:false));break;
                case 12:Check(sync?provider.LoadHolonsForParent(parent,loadChildren:false):await provider.LoadHolonsForParentAsync(parent,loadChildren:false));break;
                case 13:Check(sync?provider.LoadHolonsByMetaData("zone","north",loadChildren:false):await provider.LoadHolonsByMetaDataAsync("zone","north",loadChildren:false));break;
                case 14:Check(sync?provider.LoadHolonsByMetaData(new(){{"zone","north"}},MetaKeyValuePairMatchMode.Any,loadChildren:false):await provider.LoadHolonsByMetaDataAsync(new(){{"zone","north"}},MetaKeyValuePairMatchMode.Any,loadChildren:false));break;
                case 15:Check(sync?provider.ExportAll():await provider.ExportAllAsync());break;
                case 16:Check(sync?provider.ExportAllDataForAvatarById(owner):await provider.ExportAllDataForAvatarByIdAsync(owner));break;
                default:Check(sync?provider.Search(new SearchParams{SearchOnlyForCurrentAvatar=false},loadChildren:false):await provider.SearchAsync(new SearchParams{SearchOnlyForCurrentAvatar=false},loadChildren:false));break;
            }
            Assert.InRange(reads,1,2);
        }finally{api.SetValue(null,original);CryptographicOperations.ZeroMemory(key);}
    }
}
