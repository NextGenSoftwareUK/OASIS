using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Ipfs.Http;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSCanonicalSelectorTests
{
    private sealed class Reader(Func<string> read):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {Assert.EndsWith("/cat",request.RequestUri!.AbsolutePath);return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(read())});}
    }
    public static IEnumerable<object[]> Cases(){foreach(var operation in Enumerable.Range(0,4))foreach(var sync in new[]{false,true})foreach(var drift in new[]{false,true})yield return new object[]{operation,sync,drift};}
    [Theory][MemberData(nameof(Cases))]
    public async Task IdentitySelectorsValidateCanonicalAvatarBeforeReturningData(int operation,bool sync,bool drift)
    {
        const string indexCid="QmPcSpfsigLFxjKLHvhKtBqiwtjmtHViHh9G4Hi8vdriSP",recordCid="Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdk";
        var provider=new IPFSOASIS(new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{StorageProviders=new StorageProviderSettings{
            IPFSOASIS=new IPFSOASISSettings{ConnectionString="http://127.0.0.1:1",LookUpIPFSAddress=indexCid}}}});
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var key=RandomNumberGenerator.GetBytes(32);
        typeof(IPFSOASIS).GetField("_storageKeyResolver",flags)!.SetValue(provider,(Func<byte[]>)(()=> (byte[])key.Clone()));
        typeof(IPFSOASIS).GetField("_ipfsClient",flags)!.SetValue(provider,new IpfsClient("http://127.0.0.1:1"));
        var email=operation%2==1;const string selected="owned-identity";
        var row=new Avatar{Id=Guid.NewGuid(),Username=drift&&!email?"different":selected,Email=drift&&email?"different":selected,Version=2};
        var index=JsonConvert.SerializeObject(new Dictionary<string,HolonResume>{{recordCid,new(){Id=row.Id,RecordKind="avatar",login=selected,email=selected,ProviderUniqueStorageKey=new(){{ProviderType.IPFSOASIS,recordCid}}}}});
        var indexEnvelope=(string)typeof(IPFSOASIS).GetMethod("ProtectIndex",flags)!.Invoke(provider,new object[]{index})!;
        var recordEnvelope=(string)typeof(IPFSOASIS).GetMethod("ProtectRecord",flags)!.Invoke(provider,new object[]{"avatar",row.Id,OasisJson.Serialize(row)})!;
        var reads=0;using var http=new HttpClient(new Reader(()=>++reads==1?indexEnvelope:recordEnvelope));
        var api=typeof(IpfsClient).GetField("api",BindingFlags.Static|BindingFlags.NonPublic)!;var original=api.GetValue(null);api.SetValue(null,http);
        try{
            void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
                Assert.Equal(drift,result.IsError);
                if(drift){Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.IsType<InvalidDataException>(result.Exception);}
                else Assert.True(result.IsLoaded);
            }
            if(operation==0)Check(sync?provider.LoadAvatarByUsername(selected):await provider.LoadAvatarByUsernameAsync(selected));
            else if(operation==1)Check(sync?provider.LoadAvatarByEmail(selected):await provider.LoadAvatarByEmailAsync(selected));
            else if(operation==2)Check(sync?provider.ExportAllDataForAvatarByUsername(selected):await provider.ExportAllDataForAvatarByUsernameAsync(selected));
            else Check(sync?provider.ExportAllDataForAvatarByEmail(selected):await provider.ExportAllDataForAvatarByEmailAsync(selected));
            Assert.InRange(reads,1,2);
        }finally{api.SetValue(null,original);CryptographicOperations.ZeroMemory(key);}
    }
}
