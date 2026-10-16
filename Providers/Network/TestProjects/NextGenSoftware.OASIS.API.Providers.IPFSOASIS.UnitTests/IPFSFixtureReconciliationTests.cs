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
    [Trait("Category","IPFSRootVersionReconciliation")]
    public async Task ReleaseExactRootVersionAssertionFixture()
    {
        const string endpoint="http://127.0.0.1:18547";
        Assert.Equal(endpoint,Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC"));
        var path=Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH")!;
        Assert.Equal("owned-dna.json",Path.GetFileName(path));
        var dna=JsonConvert.DeserializeObject<OASISDNA>(await File.ReadAllTextAsync(path))!;
        const string root="QmPcSpfsigLFxjKLHvhKtBqiwtjmtHViHh9G4Hi8vdriSP";
        Assert.Equal(root,dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
        Assert.Equal(endpoint,dna.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(path)!,"v2-routing.db")));
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(path)!,"tombstone-routing.db")));
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        var provider=new IPFSOASIS(dna,path);var cids=new HashSet<string> {root};
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var index=await provider.LoadLookupToJson();Assert.Equal(6,index.Count);
            var avatar=Assert.Single((await provider.LoadAllAvatarsAsync()).Result);
            Assert.Equal("owned-native-avatar",avatar.Username);Assert.Equal("owned@example.invalid",avatar.Email);Assert.Equal(2,avatar.Version);
            var detail=Assert.Single((await provider.LoadAllAvatarDetailsAsync()).Result);
            Assert.Equal(avatar.Id,detail.Id);Assert.Equal(654,detail.XP);Assert.Equal(29,detail.Karma);
            Assert.Equal("owned oak seed",Assert.Single(detail.Inventory).Name);
            var records=(await provider.LoadAllHolonsAsync(NextGenSoftware.OASIS.API.Core.Enums.HolonType.All,loadChildren:false)).Result.ToList();
            Assert.Equal(4,records.Count);
            Assert.Single(records.Where(row=>row.Name=="owned-partial-import"));
            var parent=Assert.Single(records.Where(row=>row.Name=="owned-native-holon-updated"));Assert.Equal(avatar.Id,parent.Id);Assert.Equal(2,parent.Version);
            var child=Assert.Single(records.Where(row=>row.Name=="owned-native-child"));Assert.Equal(parent.Id,child.ParentHolonId);
            var grand=Assert.Single(records.Where(row=>row.Name=="owned-native-grandchild"));Assert.Equal(child.Id,grand.ParentHolonId);
            var historical=await provider.LoadHolonAsync(parent.Id,loadChildren:false,version:1);
            Assert.Equal("owned-native-holon",historical.Result.Name);
            foreach(var entry in index) {cids.Add(entry.Key);output.WriteLine($"Verified root-version fixture {entry.Value.RecordKind}/{entry.Value.Id:D} CID={entry.Key}");}
        }
        finally {await provider.DeActivateProviderAsync();OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;}
        var client=new Ipfs.Http.IpfsClient(endpoint);
        foreach(var cid in cids) Assert.Contains((await client.Pin.RemoveAsync((Cid)cid)).Select(value=>value.ToString()),value=>value==cid);
        File.Delete(path);
        output.WriteLine("Released only seven verified current fixture pins and exact synthetic DNA; older/unknown pins retained.");
    }
    [Fact]
    [Trait("Category","IPFSNativeFixtureReconciliation")]
    public async Task ReleaseVerifiedInterruptedVersionFixture()
    {
        const string endpoint="http://127.0.0.1:18547";
        Assert.Equal(endpoint,Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC"));
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null && !File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Run-ReusableIpfsTests.ps1"))) directory=directory.Parent;
        Assert.NotNull(directory);
        var path=Path.Combine(directory!.FullName,"TestResults","IPFSCurrent","owned-dna.json");
        Assert.Equal(path,Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH"));
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(path)!,"v2-routing.db")));
        var dna=JsonConvert.DeserializeObject<OASISDNA>(await File.ReadAllTextAsync(path))!;
        Assert.Equal(endpoint,dna.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
        var root=dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
        var provider=new IPFSOASIS(dna,path);
        var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
        var cids=new HashSet<string> {root};var verified=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var index=await provider.LoadLookupToJson();
            foreach(var entry in index)
                output.WriteLine($"Retained index {entry.Value.RecordKind}/{entry.Value.Id:D} login={entry.Value.login} CID={entry.Key}");
            Assert.Equal(12,index.Count);
            var avatars=await provider.LoadAllAvatarsAsync();Assert.False(avatars.IsError,avatars.Message);
            Assert.Equal(4,avatars.Result.Count());
            var avatar=Assert.Single(avatars.Result.Where(row=>row.Username=="owned-native-avatar"));Assert.Equal("owned@example.invalid",avatar.Email);
            Assert.Equal(Guid.Parse("86b07556-6125-480a-b3e9-ac4740e55984"),avatar.Id);
            Assert.False(avatar.IsActive);Assert.Equal(2,avatar.Version);
            Assert.Equal(2,avatars.Result.Count(row=>row.Username=="conflict-"+row.Id));
            Assert.Single(avatars.Result.Where(row=>row.Username=="partial-native-"+row.Id && row.IsActive));
            var details=await provider.LoadAllAvatarDetailsAsync();Assert.False(details.IsError,details.Message);
            Assert.Equal(4,details.Result.Count());
            Assert.Equal(avatars.Result.Select(row=>row.Id).OrderBy(id=>id),details.Result.Select(row=>row.Id).OrderBy(id=>id));
            var detail=Assert.Single(details.Result.Where(row=>row.Id==avatar.Id));Assert.Equal(654,detail.XP);Assert.Equal(29,detail.Karma);
            Assert.Equal(avatar.DeletedDate,detail.DeletedDate);Assert.False(detail.IsActive);
            Assert.Equal(2,details.Result.Count(row=>row.Username=="conflict-"+row.Id && row.XP==246 && row.IsActive));
            Assert.Single(details.Result.Where(row=>row.Username=="partial-native-"+row.Id && row.XP==123 && row.IsActive));
            var item=Assert.Single(detail.Inventory);Assert.Equal("owned oak seed",item.Name);Assert.Equal(5,item.Quantity);
            var rows=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>();
            foreach(var entry in index.Values.Where(row=>row.RecordKind=="holon"))
            {
                var loaded=await provider.LoadHolonAsync(entry.Id,loadChildren:false);Assert.False(loaded.IsError,loaded.Message);rows.Add(loaded.Result);
            }
            Assert.Equal(4,rows.Count);
            Assert.Equal(new[]{"owned-native-child","owned-native-grandchild","owned-partial-import","replicated-from-LocalFileOASIS"},rows.Select(row=>row.Name).OrderBy(value=>value,StringComparer.Ordinal));
            var parent=rows.Single(row=>row.Name=="replicated-from-LocalFileOASIS");Assert.Equal(avatar.Id,parent.Id);
            var child=rows.Single(row=>row.Name=="owned-native-child");Assert.Equal(parent.Id,child.ParentHolonId);
            Assert.Equal(child.Id,rows.Single(row=>row.Name=="owned-native-grandchild").ParentHolonId);
            foreach(var entry in index){cids.Add(entry.Key);output.WriteLine($"Verified native fixture {entry.Value.RecordKind}/{entry.Value.Id:D} CID={entry.Key}");}
            var sqlitePath=Path.Combine(Path.GetDirectoryName(path)!,"tombstone-routing.db");
            using(var sqlite=new SqliteProvider($"Data Source={sqlitePath};Mode=ReadOnly;Pooling=False"))
            {
                Assert.False((await sqlite.ActivateProviderAsync()).IsError);
                var localAvatar=Assert.Single((await sqlite.LoadAllAvatarsAsync()).Result);
                var localDetail=Assert.Single((await sqlite.LoadAllAvatarDetailsAsync()).Result);
                Assert.Equal(avatar.Id,localAvatar.Id);Assert.Equal(avatar.Id,localDetail.Id);
                Assert.Equal("owned-sqlite-primary",localAvatar.Username);Assert.Equal(999,localDetail.XP);Assert.Equal(77,localDetail.Karma);
                Assert.Equal(avatar.DeletedDate,localAvatar.DeletedDate);Assert.Equal(avatar.DeletedDate,localDetail.DeletedDate);
                Assert.Empty((await sqlite.LoadAllHolonsAsync()).Result);
                await sqlite.DeActivateProviderAsync();
            }
            output.WriteLine($"Verified native fixture root={root}; exact synthetic tombstone database={new FileInfo(sqlitePath).Length} bytes");verified=true;
        }
        finally {await provider.DeActivateProviderAsync();OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;}
        if(verified)
        {
            var client=new Ipfs.Http.IpfsClient(endpoint);
            foreach(var cid in cids) Assert.Contains((await client.Pin.RemoveAsync((Cid)cid)).Select(value=>value.ToString()),value=>value==cid);
            File.Delete(path);
            File.Delete(Path.Combine(Path.GetDirectoryName(path)!,"tombstone-routing.db"));
            output.WriteLine("Released thirteen verified current pins and exact synthetic DNA/tombstone SQLite; unknown earlier pins/local files retained.");
        }
    }
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
