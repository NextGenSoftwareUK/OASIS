using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

[Collection("Owned IPFS")]
public class IPFSNamedRoleInspectionTests
{
    [Fact][Trait("Category","IPFSDeleteFaultInspection")]
    public async Task OriginalPreAckDeleteFaultLeavesExactPublishedRecordsAndPinsUnchanged()
    {
        const string endpoint="http://127.0.0.1:18547";Assert.Equal(endpoint,Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC"));
        var path=Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH")!;var before=await File.ReadAllBytesAsync(path);
        Assert.Equal("5DF4188F9FF6B8CF6CD6CAB570361728156B52258D9254EA7CEF3076DC1394F3",Convert.ToHexString(SHA256.HashData(before)));
        Assert.Equal("D0EABD2E2B8E3E17EFD8555BCBAE4CFA7F3C257FAE397BE44D19E94FD413BC19",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Path.GetDirectoryName(path)!,"ipfs-native-current.trx")))));
        var dna=JsonConvert.DeserializeObject<OASISDNA>(System.Text.Encoding.UTF8.GetString(before))!;
        var client=new Ipfs.Http.IpfsClient(endpoint);async Task<string[]> Pins()=>(await client.Pin.ListAsync()).Select(cid=>cid.ToString()).OrderBy(value=>value,StringComparer.Ordinal).ToArray();var pins=await Pins();
        var prior=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;var provider=new IPFSOASIS(dna,path);
        try{
            Assert.False((await provider.ActivateProviderAsync()).IsError);var index=await provider.LoadLookupToJson();Assert.NotEmpty(index);
            var rows=new List<Holon>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var read=typeof(IPFSOASIS).GetMethod("ReadPrivateRecordAsync",flags)!;
            foreach(var entry in index){
                Assert.Contains(entry.Key,pins);var row=entry.Value;
                var type=row.RecordKind switch{"holon"=>typeof(Holon),"avatar"=>typeof(Avatar),"avatar-detail"=>typeof(AvatarDetail),_=>throw new InvalidDataException("Unexpected original record kind.")};
                var task=(Task)read.MakeGenericMethod(type).Invoke(provider,new object[]{entry.Key,row.RecordKind,row.Id})!;await task;
                var record=(NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase)task.GetType().GetProperty("Result")!.GetValue(task)!;Assert.Equal(row.Id,record.Id);
                if(row.RecordKind=="holon")rows.Add(Assert.IsAssignableFrom<Holon>(record));
            }
            Assert.Equal(6,rows.Count);
            var active=Assert.Single(rows.Where(row=>row.Name=="owned failed holon deletion"));Assert.True(active.IsActive);Assert.False(active.IsDeleted);Assert.Equal(DateTime.MinValue,active.DeletedDate);
            var deleted=Assert.Single(rows.Where(row=>row.Name=="owned holon deletion"));Assert.False(deleted.IsActive);Assert.True(deleted.IsDeleted);
            Assert.Equal(new[]{"owned failed holon deletion","owned holon deletion","owned-native-child","owned-native-grandchild","owned-partial-import","replicated-from-LocalFileOASIS"},rows.Select(row=>row.Name).OrderBy(value=>value,StringComparer.Ordinal));
            var root=rows.Single(row=>row.Name=="replicated-from-LocalFileOASIS");var child=rows.Single(row=>row.Name=="owned-native-child");Assert.Equal(root.Id,child.ParentHolonId);Assert.Equal(child.Id,rows.Single(row=>row.Name=="owned-native-grandchild").ParentHolonId);
            Assert.Equal(before,await File.ReadAllBytesAsync(path));Assert.Equal(pins,await Pins());
        }finally{await provider.DeActivateProviderAsync();OASISDNAManager.OASISDNA=prior;OASISDNAManager.OASISDNAPath=priorPath;}
    }

    [Fact][Trait("Category","IPFSNamedRoleInspection")]
    public async Task OriginalVersionZeroRoleFixtureIsReadOnlyVerified()
    {
        const string endpoint="http://127.0.0.1:18547";Assert.Equal(endpoint,Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC"));
        var path=Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH")!;var before=await File.ReadAllBytesAsync(path);
        var report=Path.Combine(Path.GetDirectoryName(path)!,"ipfs-native-current.trx");Assert.Equal("ED3C3820957168165FCA3889E8EC45744B1F433FFCE44C4963B41B7B2C95F3F6",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        var dna=JsonConvert.DeserializeObject<OASISDNA>(System.Text.Encoding.UTF8.GetString(before))!;
        var client=new Ipfs.Http.IpfsClient(endpoint);async Task<string[]> Pins()=>(await client.Pin.ListAsync()).Select(cid=>cid.ToString()).OrderBy(value=>value,StringComparer.Ordinal).ToArray();var pins=await Pins();
        var prior=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;var provider=new IPFSOASIS(dna,path);
        try{
            Assert.False((await provider.ActivateProviderAsync()).IsError);var index=await provider.LoadLookupToJson();Assert.Equal(2,index.Count);Assert.All(index.Values,row=>Assert.Equal("holon",row.RecordKind));
            var rows=new List<Holon>();foreach(var entry in index.Values){var loaded=await provider.LoadHolonAsync(entry.Id,loadChildren:false);Assert.False(loaded.IsError,loaded.Message);rows.Add((Holon)loaded.Result);}
            var root=Assert.Single(rows.Where(row=>row.Name=="native scalar update"));var child=Assert.Single(rows.Where(row=>row.Name=="native named child"));Assert.Equal(root.Id,child.ParentHolonId);Assert.Equal(0,root.Version);Assert.Equal(0,child.Version);
            var manifest=HolonChildRoleManifestJson.Deserialize(root.ChildRoleManifestJson);Assert.Equal(root.Id,manifest.ParentId);Assert.Equal(child.Id,Assert.Single(manifest.Roles.Single(role=>role.Name=="Components").ChildIds));Assert.Empty(manifest.Roles.Single(role=>role.Name=="Children").ChildIds);
            var old=root.PreviousVersionProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.IPFSOASIS];Assert.Contains(old,pins);
            var envelope=await provider.LoadStringToJson(old);var json=(string)typeof(IPFSOASIS).GetMethod("UnprotectRecord",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(provider,new object[]{"holon",root.Id,envelope})!;
            var archived=OasisJson.DeserializeStored<Holon>(json);Assert.Equal(root.Id,archived.Id);Assert.Equal(0,archived.Version);Assert.Equal("native named owner",archived.Name);Assert.Equal(root.ChildRoleManifestJson,archived.ChildRoleManifestJson);
            Assert.Equal(before,await File.ReadAllBytesAsync(path));Assert.Equal(pins,await Pins());
        }finally{await provider.DeActivateProviderAsync();OASISDNAManager.OASISDNA=prior;OASISDNAManager.OASISDNAPath=priorPath;}
    }
}
