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


