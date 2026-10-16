using Ipfs;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using SqliteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using Xunit;
using Xunit.Abstractions;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public sealed class IPFSFixtureReconciliationTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category","IPFSNetworkDiagnostic")]
    public async Task ClosedEndpointCannotReturnStoredContent()
    {
        var client=new Ipfs.Http.IpfsClient("http://127.0.0.1:18549");
        var error=await Record.ExceptionAsync(async()=>
        {
            var text=await client.FileSystem.ReadAllTextAsync((Cid)"Qmc1XJWAyg3FyUwbzG9SpL1sQiwxnyUHPyxbt5vYL5Rzdk");
            output.WriteLine($"Closed endpoint returned text length={text?.Length}");
        });
        output.WriteLine(error?.ToString() ?? "No network exception");
        var network=Assert.IsType<System.Net.Http.HttpRequestException>(error);
        Assert.IsType<System.Net.Sockets.SocketException>(network.InnerException);
    }
    [Fact]
    [Trait("Category","IPFSFixtureReconciliation")]
    public async Task InspectAndReleaseOnlyAcknowledgedSyntheticPolicyFixture()
    {
        const string endpoint="http://127.0.0.1:18547";
        Assert.Equal(endpoint,Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC"));
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null && !File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Run-ReusableIpfsTests.ps1"))) directory=directory.Parent;
        Assert.NotNull(directory);
        var work=Path.Combine(directory!.FullName,"TestResults","IPFSCurrent");
        var path=Path.Combine(work,"owned-dna.json");var dbPath=Path.Combine(work,"v2-routing.db");
        Assert.Equal(path,Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH"));
        Assert.True(File.Exists(path));Assert.True(File.Exists(dbPath));
        var dna=JsonConvert.DeserializeObject<OASISDNA>(await File.ReadAllTextAsync(path))!;
        Assert.Equal(endpoint,dna.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
        var root=dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
        Assert.False(string.IsNullOrEmpty(root));
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        var ipfs=new IPFSOASIS(dna,path);
        using var sqlite=new SqliteProvider($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
        var verified=false;var cids=new HashSet<string> {root};
        try
        {
            Assert.False((await ipfs.ActivateProviderAsync()).IsError);
            Assert.False((await sqlite.ActivateProviderAsync()).IsError);
            var index=await ipfs.LoadLookupToJson();Assert.Equal(4,index.Count);
            Assert.Single(index.Values.Where(row=>row.RecordKind=="avatar"));
            Assert.Single(index.Values.Where(row=>row.RecordKind=="avatar-detail"));
            Assert.Equal(2,index.Values.Count(row=>row.RecordKind=="holon"));
            var avatarId=index.Values.Single(row=>row.RecordKind=="avatar").Id;
            Assert.Equal(avatarId,index.Values.Single(row=>row.RecordKind=="avatar-detail").Id);
            foreach(var store in new NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider[] {ipfs,sqlite})
            {
                var avatars=await store.LoadAllAvatarsAsync();Assert.False(avatars.IsError,avatars.Message);
                var avatar=Assert.Single(avatars.Result);Assert.Equal(avatarId,avatar.Id);
                Assert.Equal("owned-policy-avatar",avatar.Username);Assert.Equal("policy@example.invalid",avatar.Email);
                var details=await store.LoadAllAvatarDetailsAsync();Assert.False(details.IsError,details.Message);
                var detail=Assert.Single(details.Result);Assert.Equal(avatarId,detail.Id);
                Assert.Equal(321,detail.XP);Assert.Equal(29,detail.Karma);
                var item=Assert.Single(detail.Inventory);Assert.Equal("owned policy seed",item.Name);Assert.Equal(4,item.Quantity);
                var holons=await store.LoadAllHolonsAsync(loadChildren:false);Assert.False(holons.IsError,holons.Message);
                Assert.Equal(2,holons.Result.Count());
                Assert.Equal("complete replicated holon",holons.Result.Single(row=>row.Id==avatarId).Name);
                Assert.Equal(store==ipfs ? "native-ipfs-marker":"native-sqlite-marker",holons.Result.Single(row=>row.Id!=avatarId).Name);
            }
            foreach(var entry in index) {cids.Add(entry.Key);output.WriteLine($"Acknowledged synthetic record: {entry.Value.RecordKind}/{entry.Value.Id:D} CID={entry.Key}");}
            output.WriteLine($"Acknowledged synthetic index: {root}; database bytes={new FileInfo(dbPath).Length}");
            verified=true;
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();sqlite.Dispose();await ipfs.DeActivateProviderAsync();
            OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;
        }
        if(verified)
        {
            var client=new Ipfs.Http.IpfsClient(endpoint);
            foreach(var cid in cids) Assert.Contains((await client.Pin.RemoveAsync((Cid)cid)).Select(value=>value.ToString()),value=>value==cid);
            File.Delete(path);File.Delete(dbPath);
            output.WriteLine("Released only the five acknowledged pins and exact synthetic DNA/SQLite files; unknown earlier pins retained.");
        }
    }
}
