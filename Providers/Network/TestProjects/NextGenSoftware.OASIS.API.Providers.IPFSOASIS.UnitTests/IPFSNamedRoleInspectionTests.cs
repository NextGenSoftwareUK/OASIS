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
