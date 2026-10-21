using System.Net;
using System.Reflection;
using Ipfs.Http;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

// The bundled SDK owns one static HttpClient. Serialize with all existing owned-IPFS tests
// and restore its exact original handle in finally; never contact a daemon here.
[Collection("Owned IPFS")]
public class IPFSSearchSnapshotTests
{
    private sealed class ReadHandler(Func<HttpRequestMessage,string> read):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(read(request))});
    }

    [Theory]
    [InlineData(0,false)] [InlineData(0,true)]
    [InlineData(1,false)] [InlineData(1,true)]
    [InlineData(2,false)] [InlineData(2,true)]
    [InlineData(3,false)] [InlineData(3,true)]
    [InlineData(4,false)] [InlineData(4,true)]
    public async Task SearchCapturesScopeBeforeSdkIndexRead(int scenario,bool synchronous)
    {
        const string indexCid="QmPcSpfsigLFxjKLHvhKtBqiwtjmtHViHh9G4Hi8vdriSP";
        const string recordCid="Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdk";
        var settings=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{StorageProviders=new StorageProviderSettings{
            IPFSOASIS=new IPFSOASISSettings{ConnectionString="http://127.0.0.1:1",LookUpIPFSAddress=indexCid}}}};
        var provider=new IPFSOASIS(settings);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var key=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        typeof(IPFSOASIS).GetField("_storageKeyResolver",flags)!.SetValue(provider,(Func<byte[]>)(()=> (byte[])key.Clone()));
        typeof(IPFSOASIS).GetField("_ipfsClient",flags)!.SetValue(provider,new IpfsClient("http://127.0.0.1:1"));
        var row=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=Guid.NewGuid(),ParentHolonId=Guid.NewGuid(),MetaData=new(){{"zone","north"}}};
        var group=new SearchTextGroup{SearchHolons=true,SearchQuery="oak",HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=row.CreatedByAvatarId,ParentId=row.ParentHolonId,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new(){group}};
        var index=JsonConvert.SerializeObject(new Dictionary<string,HolonResume>{{recordCid,new HolonResume{Id=row.Id,RecordKind="holon",ProviderUniqueStorageKey=new(){{ProviderType.IPFSOASIS,recordCid}}}}});
        var indexEnvelope=(string)typeof(IPFSOASIS).GetMethod("ProtectIndex",flags)!.Invoke(provider,new object[]{index})!;
        var recordEnvelope=(string)typeof(IPFSOASIS).GetMethod("ProtectRecord",flags)!.Invoke(provider,new object[]{"holon",row.Id,OasisJson.Serialize(row)})!;
        var reads=0;
        using var http=new HttpClient(new ReadHandler(request=>{
            Assert.EndsWith("/cat",request.RequestUri!.AbsolutePath);
            if(++reads==1)
            {
                switch(scenario){case 0:query.AvatarId=Guid.NewGuid();break;case 1:group.SearchQuery="pine";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=Guid.NewGuid();break;}
                return indexEnvelope;
            }
            return recordEnvelope;
        }));
        var api=typeof(IpfsClient).GetField("api",BindingFlags.Static|BindingFlags.NonPublic)!;
        var original=api.GetValue(null);api.SetValue(null,http);
        try
        {
            var result=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
            Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsLoaded);
            Assert.Equal(row.Id,Assert.Single(result.Result.SearchResultHolons).Id);Assert.Equal(1,result.Result.NumberOfResults);
            Assert.Equal(2,reads);
        }
        finally{api.SetValue(null,original);System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);}
    }
}
