using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Ipfs;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public class IPFSNativeStorageTests
{
    [Fact]
    public async Task RealOfflineDaemonRetainsEncryptedSameGuidRecordsAndIndex()
    {
        const string endpoint="http://127.0.0.1:18547";
        if(Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC")!=endpoint) throw new InvalidOperationException("Owned reusable IPFS runner required.");
        var path=Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH") ?? throw new InvalidOperationException("Owned fixture path required.");
        if(Path.GetFileName(path)!="owned-dna.json" || File.Exists(path)) throw new InvalidOperationException("Existing or unexpected fixture requires inspection.");
        var resolve=typeof(OASISDNAManager).GetMethod("ResolveEffectiveOASISDNAPath",BindingFlags.Static|BindingFlags.NonPublic)!;
        Assert.Equal(Path.GetFullPath(path),(string)resolve.Invoke(null,new object[] {path})!);
        var priorDna=OASISDNAManager.OASISDNA; var priorPath=OASISDNAManager.OASISDNAPath;
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {StorageProviders=new StorageProviderSettings {
            IPFSOASIS=new IPFSOASISSettings {ConnectionString=endpoint}}}};
        var provider=new IPFSOASIS(dna,path);
        var ownedCids=new HashSet<string>(StringComparer.Ordinal);
        var verified=false;
        try
        {
            var activated=await provider.ActivateProviderAsync(); Assert.False(activated.IsError,activated.Message);
            var id=Guid.NewGuid();
            var avatar=new Avatar {Id=id,Username="owned-native-avatar",Email="owned@example.invalid",Password="synthetic-not-a-user-secret"};
            var detail=new AvatarDetail {Id=id,Username=avatar.Username,Email=avatar.Email,XP=321,Karma=29};
            var holon=new Holon {Id=id,Name="owned-native-holon",HolonType=HolonType.Holon};
            await provider.SaveAvatarToFile(avatar);
            ownedCids.Add(avatar.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            await provider.SaveAvatarDetailToFile(detail);
            ownedCids.Add(detail.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            await provider.SaveHolonToFile(holon);
            ownedCids.Add(holon.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            var profile=await provider.LoadAvatarTemplateAsync(row=>row.Id==id);
            var stats=await provider.LoadAvatarDetailTemplateAsync(row=>row.Id==id);
            var stored=await provider.LoadHolonTemplateAsync(row=>row.Id==id);
            Assert.Equal(avatar.Email,profile.Result.Email); Assert.Equal(avatar.Password,profile.Result.Password);
            Assert.Equal(detail.XP,stats.Result.XP); Assert.Equal(detail.Karma,stats.Result.Karma); Assert.Equal(holon.Name,stored.Result.Name);
            Assert.Single((await provider.LoadAllAvatarsAsync()).Result); Assert.Single((await provider.LoadAllAvatarDetailsAsync()).Result);
            Assert.Single((await provider.LoadAllHolonsAsync(HolonType.All)).Result);
            var child=new Holon {Id=Guid.NewGuid(),ParentHolonId=id,Name="owned-native-child",HolonType=HolonType.Holon};
            await provider.SaveHolonToFile(child);
            ownedCids.Add(child.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            var parentKey=holon.ProviderUniqueStorageKey[ProviderType.IPFSOASIS];
            Assert.Equal(child.Id,Assert.Single((await provider.LoadHolonsForParentAsync(parentKey,loadChildren:false)).Result).Id);
            Assert.Equal(child.Id,Assert.Single(provider.LoadHolonsForParent(parentKey,loadChildren:false).Result).Id);
            Assert.Empty((await provider.LoadHolonsForParentAsync(child.ProviderUniqueStorageKey[ProviderType.IPFSOASIS],loadChildren:false)).Result);
            Assert.Throws<NotSupportedException>(()=>provider.LoadHolonsForParent(parentKey,version:1));
            var grandchild=new Holon {Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="owned-native-grandchild",HolonType=HolonType.Holon};
            await provider.SaveHolonToFile(grandchild);
            ownedCids.Add(grandchild.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]); ownedCids.Add(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
            var graph=Assert.Single((await provider.LoadHolonsForParentAsync(parentKey,loadChildrenFromProvider:true)).Result);
            Assert.Equal(grandchild.Id,Assert.Single(graph.Children).Id);
            Assert.Empty(Assert.Single((await provider.LoadHolonsForParentAsync(parentKey,loadChildren:false,loadChildrenFromProvider:true)).Result).Children);
            Assert.Empty(Assert.Single((await provider.LoadHolonsForParentAsync(parentKey,maxChildDepth:1,curentChildDepth:1,loadChildrenFromProvider:true)).Result).Children);
            Assert.Throws<ArgumentOutOfRangeException>(()=>provider.LoadHolonsForParent(parentKey,maxChildDepth:-1));
            var loadedRoot=await provider.LoadHolonAsync(id,loadChildrenFromProvider:true);
            Assert.False(loadedRoot.IsError,loadedRoot.Message);
            Assert.Equal(grandchild.Id,Assert.Single(Assert.Single(loadedRoot.Result.Children).Children).Id);
            var shallow=provider.LoadHolon(parentKey,recursive:false,loadChildrenFromProvider:true);
            Assert.False(shallow.IsError,shallow.Message);
            Assert.Empty(Assert.Single(shallow.Result.Children).Children);
            Assert.Empty(provider.LoadHolon(id,loadChildren:false,loadChildrenFromProvider:true).Result.Children);
            var historical=provider.LoadHolon(parentKey,version:1);
            Assert.True(historical.IsError); Assert.IsType<NotSupportedException>(historical.Exception);
            var missing=await provider.LoadHolonAsync(Guid.NewGuid(),loadChildren:false);
            Assert.True(missing.IsError); Assert.Null(missing.Result);
            foreach(var cid in new[] {avatar.ProviderUniqueStorageKey[ProviderType.IPFSOASIS],detail.ProviderUniqueStorageKey[ProviderType.IPFSOASIS],holon.ProviderUniqueStorageKey[ProviderType.IPFSOASIS],dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress})
            {
                var raw=await provider.IPFSClient.FileSystem.ReadAllTextAsync((Cid)cid);
                Assert.StartsWith("oasis-aes256gcm-gzip-v2:",raw); Assert.DoesNotContain(avatar.Email,raw); Assert.DoesNotContain(avatar.Password,raw); Assert.DoesNotContain(id.ToString(),raw);
            }
            await provider.DeActivateProviderAsync();
            var version=await new Ipfs.Http.IpfsClient(endpoint).VersionAsync(); Assert.NotEmpty(version);
            verified=true;
        }
        finally
        {
            var cleaned=false;
            try
            {
                if(verified)
                {
                    var client=new Ipfs.Http.IpfsClient(endpoint);
                    foreach(var cid in ownedCids) Assert.Contains((await client.Pin.RemoveAsync((Cid)cid)).Select(value=>value.ToString()),value=>value==cid);
                    cleaned=true;
                }
            }
            finally
            {
                await provider.DeActivateProviderAsync();
                OASISDNAManager.OASISDNA=priorDna; OASISDNAManager.OASISDNAPath=priorPath;
                if(cleaned && File.Exists(path)) File.Delete(path);
            }
        }
    }
}
