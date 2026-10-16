using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;
using System.IO;
using Ipfs;
using Ipfs.Http;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests
{
    [CollectionDefinition("Owned IPFS",DisableParallelization=true)]
    public class OwnedIpfsCollection { }

    [Collection("Owned IPFS")]
    [Trait("Category","NativeIPFS")]
	public class IPFSOASISTests
	{
        [Fact]
        public async Task NearbyReadsEncryptedCanonicalLocationsUnderNonEnglishCulture()
        {
            foreach(var avatars in new[] {false,true})
            await WithOwnedProvider(async provider => {
                var records=new List<IHolonBase>();
                for(var i=0;i<4;i++)
                {
                    IHolonBase record=avatars ? new Avatar {Username="nearby-"+i,FirstName="complete profile"}:new Holon();
                    record.Id=Guid.NewGuid();record.Description="complete record";record.IsActive=i!=2;
                    record.HolonType=avatars ? NextGenSoftware.OASIS.API.Core.Enums.HolonType.Avatar:NextGenSoftware.OASIS.API.Core.Enums.HolonType.Park;
                    record.MetaData=new Dictionary<string,object> {{"proof","preserved"}};
                    if(i!=3) {record.MetaData["Latitude"]=i==1 ? "30.002":"30.0005";record.MetaData["Longitude"]="20";}
                    records.Add(record);
                    if(avatars) {var saved=await provider.SaveAvatarAsync((IAvatar)record);Assert.False(saved.IsError,saved.Message);}
                    else {var saved=await provider.SaveHolonAsync((IHolon)record,saveChildren:false);Assert.False(saved.IsError,saved.Message);}
                }
                var net=(IOASISNETProvider)provider;var prior=System.Globalization.CultureInfo.CurrentCulture;
                try
                {
                    System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("fr-FR");
                    if(avatars)
                    {
                        var near=net.GetAvatarsNearMe(30000000,20000000,100);
                        Assert.False(near.IsError,near.Message);Assert.True(near.IsLoaded);
                        var found=Assert.Single(near.Result);Assert.Equal(records[0].Id,found.Id);Assert.Equal("complete profile",found.FirstName);
                        Assert.Equal("preserved",found.MetaData["proof"].ToString());
                        Assert.Empty(net.GetAvatarsNearMe(30000000,20000000,0).Result);
                        records[0].MetaData["Latitude"]="NaN";
                        var saved=await provider.SaveAvatarAsync((IAvatar)records[0]);Assert.False(saved.IsError,saved.Message);
                        var corrupt=net.GetAvatarsNearMe(30000000,20000000,100);
                        Assert.True(corrupt.IsError);Assert.False(corrupt.IsLoaded);Assert.Null(corrupt.Result);Assert.IsType<InvalidDataException>(corrupt.Exception);
                    }
                    else
                    {
                        var near=net.GetHolonsNearMe(30000000,20000000,100,NextGenSoftware.OASIS.API.Core.Enums.HolonType.Park);
                        Assert.False(near.IsError,near.Message);Assert.True(near.IsLoaded);
                        var found=Assert.Single(near.Result);Assert.Equal(records[0].Id,found.Id);Assert.Equal("complete record",found.Description);
                        Assert.Equal("preserved",found.MetaData["proof"].ToString());
                        Assert.Empty(net.GetHolonsNearMe(30000000,20000000,100,NextGenSoftware.OASIS.API.Core.Enums.HolonType.Holon).Result);
                        Assert.Empty(net.GetHolonsNearMe(30000000,20000000,0,NextGenSoftware.OASIS.API.Core.Enums.HolonType.All).Result);
                        records[0].MetaData.Remove("Longitude");
                        var saved=await provider.SaveHolonAsync((IHolon)records[0],saveChildren:false);Assert.False(saved.IsError,saved.Message);
                        var corrupt=net.GetHolonsNearMe(30000000,20000000,100,NextGenSoftware.OASIS.API.Core.Enums.HolonType.All);
                        Assert.True(corrupt.IsError);Assert.False(corrupt.IsLoaded);Assert.Null(corrupt.Result);Assert.IsType<InvalidDataException>(corrupt.Exception);
                    }
                }
                finally {System.Globalization.CultureInfo.CurrentCulture=prior;}
            });
        }

        [Fact]
        public async Task GraphChildFailuresRetainReceiptsAndRespectContinuation()
        {
            foreach(var synchronous in new[] {false,true})
            foreach(var continueOnError in new[] {false,true})
            foreach(var acknowledgedFailure in new[] {false,true})
                await WithOwnedProvider(async provider => {
                    var root=new Holon {Id=Guid.NewGuid(),Name="fault root"};
                    var child=new Holon {Id=Guid.NewGuid(),Name="fault child"};
                    var grandchild=new Holon {Id=Guid.NewGuid(),Name="must skip descendant"};
                    var sibling=new Holon {Id=Guid.NewGuid(),Name="independent sibling"};
                    root.Children=new List<IHolon>{child,sibling};child.Children=new List<IHolon>{grandchild};
                    var resolver=typeof(IPFSOASIS).GetField("_storageKeyResolver",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                    var prior=resolver.GetValue(provider);var calls=0;
                    var cause=new InvalidOperationException("owned graph key fault");
                    resolver.SetValue(provider,(Func<byte[]>)(()=> {
                        calls++;
                        // Root record/index=1/2; child index read=3, record=4, publication=5.
                        if(calls==(acknowledgedFailure ? 5:4)) throw cause;
                        return Convert.FromBase64String(Environment.GetEnvironmentVariable("OASIS_IPFS_STORAGE_ENCRYPTION_KEY")!);
                    }));
                    NextGenSoftware.OASIS.Common.OASISResult<IHolon> result;
                    try {result=synchronous ? provider.SaveHolon(root,continueOnError:continueOnError)
                        :await provider.SaveHolonAsync(root,continueOnError:continueOnError);}
                    finally {resolver.SetValue(provider,prior);}
                    Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Null(result.Result);
                    var evidence=Assert.IsType<IPFSRecordPublicationException>(result.Exception);
                    var expectedSibling=continueOnError && !acknowledgedFailure;
                    Assert.Equal(acknowledgedFailure || expectedSibling ? 2:1,evidence.AcknowledgedRecordCids.Count);
                    Assert.Equal(root.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.IPFSOASIS],evidence.RecordCid);
                    Assert.Empty(child.ProviderUniqueStorageKey);Assert.Empty(grandchild.ProviderUniqueStorageKey);
                    Assert.NotNull((await provider.LoadHolonAsync(root.Id,loadChildren:false)).Result);
                    Assert.Null((await provider.LoadHolonAsync(child.Id,loadChildren:false)).Result);
                    Assert.Null((await provider.LoadHolonAsync(grandchild.Id,loadChildren:false)).Result);
                    Assert.Equal(expectedSibling,(await provider.LoadHolonAsync(sibling.Id,loadChildren:false)).Result!=null);
                    if(acknowledgedFailure)
                    {
                        var publication=Assert.IsType<IPFSRecordPublicationException>(evidence.InnerException);
                        Assert.Same(cause,publication.InnerException);
                        Assert.StartsWith("oasis-aes256gcm-gzip-v2:",await provider.IPFSClient.FileSystem.ReadAllTextAsync((Cid)publication.RecordCid));
                    }
                    else Assert.Same(cause,evidence.InnerException);
                });
        }

        [Fact]
        public async Task SelectedGraphOptionsControlActualIndexedWrites()
        {
            await WithOwnedProvider(async provider => {
                foreach(var synchronous in new[] {false,true})
                foreach(var bulk in new[] {false,true})
                foreach(var option in new (bool Children,bool Recursive,int Maximum,int Initial,int Expected)[] {(false,true,0,0,1),
                    (true,false,0,0,2),(true,true,1,0,2),(true,true,2,0,3),(true,true,1,1,1)})
                {
                    if(!bulk && option.Initial!=0) continue;
                    var root=new Holon {Id=Guid.NewGuid(),Name="graph root"};
                    var child=new Holon {Id=Guid.NewGuid(),Name="graph child"};
                    var grandchild=new Holon {Id=Guid.NewGuid(),Name="graph grandchild"};
                    root.Children=new List<IHolon>{child};child.Children=new List<IHolon>{grandchild};
                    if(bulk)
                    {
                        var saved=synchronous ? provider.SaveHolons(new[] {root},option.Children,option.Recursive,option.Maximum,option.Initial)
                            :await provider.SaveHolonsAsync(new[] {root},option.Children,option.Recursive,option.Maximum,option.Initial);
                        Assert.False(saved.IsError,saved.Message);Assert.True(saved.IsSaved);
                    }
                    else
                    {
                        var saved=synchronous ? provider.SaveHolon(root,option.Children,option.Recursive,option.Maximum)
                            :await provider.SaveHolonAsync(root,option.Children,option.Recursive,option.Maximum);
                        Assert.False(saved.IsError,saved.Message);Assert.True(saved.IsSaved);
                    }
                    var rows=new[] {root,child,grandchild};
                    for(var index=0;index<rows.Length;index++)
                    {
                        var read=await provider.LoadHolonAsync(rows[index].Id,loadChildren:false);
                        if(index>=option.Expected) Assert.Null(read.Result);
                        else
                        {
                            Assert.False(read.IsError,read.Message);Assert.Equal(rows[index].Name,read.Result.Name);
                            Assert.Equal(index==0 ? Guid.Empty:rows[index-1].Id,read.Result.ParentHolonId);
                        }
                    }
                }
            });
        }

        private static async Task WithOwnedProvider(Func<IPFSOASIS,Task> action)
        {
            const string endpoint="http://127.0.0.1:18547";
            if(Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_RPC")!=endpoint)
                throw new InvalidOperationException("Owned reusable IPFS runner required; no default daemon is permitted.");
            var path=Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH") ?? throw new InvalidOperationException("Owned DNA path required.");
            if(Path.GetFileName(path)!="owned-dna.json" || File.Exists(path)) throw new InvalidOperationException("Existing or unexpected fixture requires inspection.");
            var priorDna=OASISDNAManager.OASISDNA;var priorPath=OASISDNAManager.OASISDNAPath;
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {StorageProviders=new StorageProviderSettings {
                IPFSOASIS=new IPFSOASISSettings {ConnectionString=endpoint}}}};
            var provider=new IPFSOASIS(dna,path);var client=new IpfsClient(endpoint);
            var priorPins=(await client.Pin.ListAsync()).Select(cid=>cid.ToString()).ToHashSet(StringComparer.Ordinal);
            var verified=false;
            try
            {
                var activated=await provider.ActivateProviderAsync();Assert.False(activated.IsError,activated.Message);
                await action(provider);verified=true;
            }
            finally
            {
                try
                {
                    // This collection owns the daemon exclusively; preserve every pre-existing pin.
                    if(verified)
                    {
                        var created=(await client.Pin.ListAsync()).Where(cid=>!priorPins.Contains(cid.ToString())).ToArray();
                        foreach(var cid in created) await client.Pin.RemoveAsync(cid);
                        if(File.Exists(path)) File.Delete(path);
                    }
                }
                finally
                {
                    await provider.DeActivateProviderAsync();
                    OASISDNAManager.OASISDNA=priorDna;OASISDNAManager.OASISDNAPath=priorPath;
                }
            }
        }

		[Fact]
		public async Task SaveAndLoadHolon_Works()
		{
            await WithOwnedProvider(async provider => {

			var holon = new Holon
			{
				Id = Guid.NewGuid(),
				Name = "Unit Test Holon",
                HolonType = NextGenSoftware.OASIS.API.Core.Enums.HolonType.Holon,
				MetaData = new Dictionary<string, object> { { "k", "v" } }
			};

			var save = await provider.SaveHolonAsync(holon);
			save.IsError.Should().BeFalse();

			var loadAll = await provider.LoadAllHolonsAsync();
			loadAll.IsError.Should().BeFalse();
			loadAll.Result.Any(h => h.Id == holon.Id).Should().BeTrue();
            });
		}

		[Fact]
		public async Task LoadHolonsByMetaData_Filters()
		{
            await WithOwnedProvider(async provider => {

			var holon = new Holon { Id = Guid.NewGuid(), Name = "Meta Holon", MetaData = new Dictionary<string, object> { { "env", "test" } } };
			await provider.SaveHolonAsync(holon);

			var filtered = await provider.LoadHolonsByMetaDataAsync("env", "test");
			filtered.IsError.Should().BeFalse();
			filtered.Result.Any(h => h.Id == holon.Id).Should().BeTrue();
            });
		}

		[Fact]
		public async Task ImportExport_Works()
		{
            await WithOwnedProvider(async provider => {

			var holons = new List<IHolon>
			{
				new Holon { Id = Guid.NewGuid(), Name = "A" },
				new Holon { Id = Guid.NewGuid(), Name = "B" }
			};

			var import = await provider.ImportAsync(holons);
			import.IsError.Should().BeFalse();

			var exported = await provider.ExportAllAsync();
			exported.IsError.Should().BeFalse();
			exported.Result.Should().NotBeNull();
			exported.Result.Count().Should().BeGreaterOrEqualTo(2);
            exported.Result.Select(row=>row.Id).Should().Contain(holons.Select(row=>row.Id));
            });
		}
	}
}


