using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Ipfs.Http;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSAvatarSaveAcknowledgementTests
{
    private const string RecordCid="Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdk";
    private const string RootCid="QmPcSpfsigLFxjKLHvhKtBqiwtjmtHViHh9G4Hi8vdriSP";
    public static IEnumerable<object[]> Cases()
    {
        foreach(var detail in new[]{false,true})foreach(var sync in new[]{false,true})
        foreach(var failure in new[]{0,1,2})yield return new object[]{detail,sync,failure};
    }

    [Theory,MemberData(nameof(Cases))]
    public async Task SaveAcknowledgesOnlyACompletelyPublishedRecordAndReturnsOriginalFailure(bool detail,bool sync,int failure)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.NotNull(root);
        var folder=Path.Combine(root!.FullName,"TestResults","IPFSAvatarAcknowledgement");Directory.CreateDirectory(folder);
        var dnaPath=Path.Combine(folder,"owned-dna.json");Assert.False(File.Exists(dnaPath));
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {StorageProviders=new StorageProviderSettings {
            IPFSOASIS=new IPFSOASISSettings {ConnectionString="http://127.0.0.1:1"}}}};
        var provider=new IPFSOASIS(dna,dnaPath);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var key=RandomNumberGenerator.GetBytes(32);
        typeof(IPFSOASIS).GetField("_storageKeyResolver",flags)!.SetValue(provider,(Func<byte[]>)(()=> (byte[])key.Clone()));
        typeof(IPFSOASIS).GetField("_ipfsClient",flags)!.SetValue(provider,new IpfsClient("http://127.0.0.1:1"));
        var transport=new Transport(failure);using var http=new HttpClient(transport);
        var api=typeof(IpfsClient).GetField("api",BindingFlags.Static|BindingFlags.NonPublic)!;
        var prior=api.GetValue(null);api.SetValue(null,http);
        var priorDna=NextGenSoftware.OASIS.API.DNA.OASISDNAManager.OASISDNA;
        var priorPath=NextGenSoftware.OASIS.API.DNA.OASISDNAManager.OASISDNAPath;
        try
        {
            async Task Check<T>(Func<Task<OASISResult<T>>> save,T entity)
            {
                var result=await save();
                if(failure==0){
                    Assert.False(result.IsError,result.Message);Assert.True(result.IsSaved);Assert.Equal(1,result.SavedCount);
                    Assert.Same(entity,result.Result);Assert.Equal(RootCid,dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                    Assert.Equal(2,transport.Adds);Assert.True(File.Exists(dnaPath));
                }else{
                    Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Equal(0,result.SavedCount);Assert.Null(result.Result);
                    Assert.NotNull(result.Exception);Assert.Equal(failure,transport.Adds);
                    if(failure==2)Assert.Equal(RecordCid,Assert.IsType<IPFSRecordPublicationException>(result.Exception).OriginalTransactionId);
                }
            }
            if(detail){
                IAvatarDetail entity=new AvatarDetail {Id=Guid.NewGuid(),Version=1};
                await Check(()=>sync?Task.FromResult(provider.SaveAvatarDetail(entity)):provider.SaveAvatarDetailAsync(entity),entity);
            }else{
                IAvatar entity=new Avatar {Id=Guid.NewGuid(),Version=1,Username="ack-owner",Email="ack@example.invalid"};
                await Check(()=>sync?Task.FromResult(provider.SaveAvatar(entity)):provider.SaveAvatarAsync(entity),entity);
            }
        }
        finally{
            api.SetValue(null,prior);CryptographicOperations.ZeroMemory(key);
            NextGenSoftware.OASIS.API.DNA.OASISDNAManager.OASISDNA=priorDna;
            NextGenSoftware.OASIS.API.DNA.OASISDNAManager.OASISDNAPath=priorPath;
            if(File.Exists(dnaPath))File.Delete(dnaPath); // Exact owned mock-only configuration; no native CID/data.
        }
    }

    private sealed class Transport(int failure) : HttpMessageHandler
    {
        public int Adds;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Assert.EndsWith("/add",request.RequestUri!.AbsolutePath);Adds++;
            if(Adds==failure)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable){Content=new StringContent("controlled add rejection")});
            var cid=Adds==1?RecordCid:RootCid;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"Name\":\"\",\"Hash\":\""+cid+"\",\"Size\":\"32\"}\n")});
        }
    }
}
