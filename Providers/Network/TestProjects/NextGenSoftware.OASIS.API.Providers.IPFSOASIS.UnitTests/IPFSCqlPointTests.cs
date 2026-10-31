using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Ipfs.Http;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSCqlPointTests
{
    [Theory]
    [InlineData("public")] [InlineData("owner")] [InlineData("denied")]
    [InlineData("deleted")] [InlineData("missing")] [InlineData("invalid")]
    [InlineData("cancelled")] [InlineData("inactive")] [InlineData("unsupported")]
    [InlineData("empty-window")] [InlineData("bad-root")] [InlineData("bad-record")]
    [InlineData("oversized")]
    [InlineData("keyset")] [InlineData("keyset-window")] [InlineData("keyset-corrupt")] [InlineData("keyset-budget")]
    public async Task PointProfileAuthenticatesTheCurrentSourceAndEnforcesAccessBeforePublication(string scenario)
    {
        var checkout=new DirectoryInfo(AppContext.BaseDirectory);
        while(checkout!=null&&!Directory.Exists(Path.Combine(checkout.FullName,"Scripts","TestHosts")))checkout=checkout.Parent;
        Assert.NotNull(checkout);
        var folder=Path.Combine(checkout!.FullName,"TestResults","IPFSCqlPoint");Directory.CreateDirectory(folder);
        var path=Path.Combine(folder,"owned-dna.json");Assert.False(File.Exists(path));
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {StorageProviders=new StorageProviderSettings {
            IPFSOASIS=new IPFSOASISSettings {ConnectionString="http://127.0.0.1:1"}}}};
        var provider=new IPFSOASIS(dna,path) { EnableCqlCidKeySets=scenario.StartsWith("keyset") };
        var key=RandomNumberGenerator.GetBytes(32);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(IPFSOASIS).GetField("_storageKeyResolver",flags)!.SetValue(provider,(Func<byte[]>)(()=> (byte[])key.Clone()));
        typeof(IPFSOASIS).GetField("_ipfsClient",flags)!.SetValue(provider,new IpfsClient("http://127.0.0.1:1"));
        var transport=new Transport();using var http=new HttpClient(transport);
        var api=typeof(IpfsClient).GetField("api",BindingFlags.Static|BindingFlags.NonPublic)!;var original=api.GetValue(null);
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        api.SetValue(null,http);
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var owner=Guid.NewGuid();
            var root=new Holon {Id=Guid.NewGuid(),CreatedByAvatarId=owner,IsPublic=scenario=="public"};
            var child=new Holon {Id=Guid.NewGuid()};root.Children.Add(child);
            if(scenario=="deleted")root.DeletedDate=DateTime.UtcNow;
            var saved=await provider.SaveHolonAsync(root);Assert.False(saved.IsError,saved.Message);
            var cid=root.ProviderUniqueStorageKey[ProviderType.IPFSOASIS];
            if(scenario.StartsWith("keyset"))
            {
                var records=new List<Holon>{root};
                // Three encrypted native records; the query selects two of them, not the
                // entire catalogue. Read access excludes the first private record.
                for(var index=0;index<2;index++)
                {
                    var other=new Holon{Id=Guid.NewGuid(),IsPublic=true};
                    Assert.False((await provider.SaveHolonAsync(other,saveChildren:false)).IsError);
                    records.Add(other);
                }
                var keys=records.Select(row=>row.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]).ToArray();
                var selected=new[]{keys[0],keys[2]};
                var batch=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.IPFSOASIS).Result)
                    .Where(row=>selected.Contains(CosmicQueryMetadata.Value<string>(row,IPFSOASIS.CqlRecordCidKey))).Result;
                if(scenario!="keyset-budget")batch=batch.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
                if(scenario=="keyset-window")batch=batch.Skip(1).Result;
                if(scenario=="keyset-corrupt"){
                    transport.Corrupt=keys[2];batch=batch.Take(0).Result;
                }
                var batchPlan=CosmicQueryPlanner.Plan(batch,ProviderType.IPFSOASIS,
                    new[]{new CosmicQueryTarget(ProviderType.IPFSOASIS,provider.QueryCapabilities)});
                Assert.False(batchPlan.IsError,batchPlan.Message);
                var reads=transport.Cats;var writes=transport.Adds;
                var batchResult=await provider.ExecuteCosmicQueryAsync(batchPlan.Result,maximumResults:1);
                Assert.Equal(3,transport.Cats-reads);Assert.Equal(writes,transport.Adds);
                if(scenario is "keyset-corrupt" or "keyset-budget"){
                    Assert.True(batchResult.IsError);Assert.Null(batchResult.Result);
                    Assert.Equal(scenario=="keyset-budget"?"CQL_QUERY_LIMIT":"CQL_QUERY_FAILED",batchResult.ErrorCode);
                }else{
                    Assert.False(batchResult.IsError,batchResult.Exception?.ToString());
                    if(scenario=="keyset-window")Assert.Empty(batchResult.Result);
                    else Assert.Equal(records[2].Id,Assert.Single(batchResult.Result).Id);
                }
                return;
            }
            var recordKey=scenario=="missing"?"Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdh":scenario=="invalid"?"not-a-cid":cid;
            var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.IPFSOASIS).Result)
                .Where(h=>CosmicQueryMetadata.Value<string>(h,IPFSOASIS.CqlRecordCidKey)==recordKey).Result;
            query=query.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(scenario=="owner"?owner:Guid.NewGuid()).Result).Result;
            if(scenario=="unsupported")query=query.OrderBy(h=>h.Name).Result;
            if(scenario=="empty-window")query=query.Take(0).Result;
            var plan=CosmicQueryPlanner.Plan(query,ProviderType.IPFSOASIS,
                new[]{new CosmicQueryTarget(ProviderType.IPFSOASIS,provider.QueryCapabilities)});
            if(scenario=="unsupported"){
                Assert.True(plan.IsError);Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",plan.ErrorCode);return;
            }
            Assert.False(plan.IsError,plan.Message);
            if(scenario=="inactive")await provider.DeActivateProviderAsync();
            if(scenario=="bad-root")transport.Corrupt=dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
            if(scenario=="bad-record")transport.Corrupt=cid;
            if(scenario=="oversized")transport.Oversized=true;
            var before=transport.Cats;
            using var cancel=new CancellationTokenSource();if(scenario=="cancelled")cancel.Cancel();
            var result=await provider.ExecuteCosmicQueryAsync(plan.Result,cancellationToken:cancel.Token);
            if(scenario is "invalid" or "cancelled" or "inactive" or "bad-root" or "bad-record" or "oversized"){
                Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsLoaded);
                Assert.Equal(scenario=="invalid"?"CQL_INVALID_SOURCE_KEY":scenario=="cancelled"?"CQL_QUERY_CANCELLED":scenario=="inactive"?"CQL_PROVIDER_INACTIVE":"CQL_QUERY_FAILED",result.ErrorCode);
            }else{
                Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsLoaded);
                if(scenario is "public" or "owner"){
                    var row=Assert.Single(result.Result);Assert.Equal(root.Id,row.Id);Assert.Empty(row.Children);
                    Assert.Equal(cid,row.MetaData[IPFSOASIS.CqlRecordCidKey]);
                    Assert.Equal(cid,row.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
                }else Assert.Empty(result.Result);
            }
            Assert.Equal(scenario is "invalid" or "cancelled" or "inactive" or "empty-window"?0:scenario is "missing" or "bad-root" or "oversized"?1:2,transport.Cats-before);
            Assert.Equal(4,transport.Adds); // Query never writes or scans unrelated record blobs.
            if(scenario=="public"){
                var executed=await CosmicQueryExecutor.ExecuteAsync(plan.Result,new ICosmicQueryStorageProvider[]{provider});
                Assert.False(executed.IsError,executed.Exception?.ToString());
                var nativeRow=Assert.Single(Assert.Single(executed.Result.Providers).Rows);
                Assert.Equal(root.Id,nativeRow.Id);
                Assert.Equal(cid,nativeRow.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]);
            }

        }
        finally{
            api.SetValue(null,original);CryptographicOperations.ZeroMemory(key);OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;
            if(File.Exists(path))File.Delete(path); // Exact owned mock configuration; no native state.
        }
    }

    private sealed class Transport : HttpMessageHandler
    {
        public int Adds;
        public int Cats;public string? Corrupt;public bool Oversized;
        private readonly Dictionary<string,string> stored=new();
        private static readonly string[] Cids={"Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdk","QmPcSpfsigLFxjKLHvhKtBqiwtjmtHViHh9G4Hi8vdriSP",
            "QmRqPMYJEEsSvcDNTGsuBHp5s5EZsrVDJbs26BYGkRti9D","QmXuu1N91m5dR2oh3FzLZWpPYgNgPQuCxp7EMHGtpAJD9D",
            "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdj", "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdi",
            "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdh", "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdg",
            "Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdf"};
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri!.AbsolutePath.EndsWith("/version"))return new(HttpStatusCode.OK){Content=new StringContent("{\"Version\":\"0.43.1\"}")};
            if(request.RequestUri!.AbsolutePath.EndsWith("/cat")){
                Cats++;
                var argument=request.RequestUri.Query.TrimStart('?').Split('&').Single(item=>item.StartsWith("arg="));
                var cid=Uri.UnescapeDataString(argument.Substring(4));
                return new(HttpStatusCode.OK){Content=new StringContent(Oversized?new string('x',4*1024*1024+1):Corrupt==cid?"corrupted-envelope":stored[cid])};
            }
            Assert.EndsWith("/add",request.RequestUri.AbsolutePath);
            var parts=Assert.IsType<MultipartFormDataContent>(request.Content);
            var payload=await Assert.Single(parts).ReadAsStringAsync(token);
            var selected=Cids[Adds++];stored[selected]=payload;
            return new(HttpStatusCode.OK){Content=new StringContent("{\"Name\":\"\",\"Hash\":\""+selected+"\",\"Size\":\"32\"}\n")};
        }
    }
}
