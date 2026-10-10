using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IntegrationTests
{
	public class IPFSOASISIntegrationTests
	{
		[Fact]
		public async Task SaveLoadRoundTrip_IPFS()
		{
			var dna = new OASISDNA();
			dna.OASIS.StorageProviders = new StorageProviderSettings
			{
				IPFSOASIS = new IPFSOASISSettings
				{
					ConnectionString = Environment.GetEnvironmentVariable("IPFSOASIS_TEST_API") ?? "http://127.0.0.1:5001"
				}
			};
			var dnaPath = Path.Combine(Path.GetTempPath(), $"ipfsoasis-integration-{Guid.NewGuid():N}.json");
			var provider = new IPFSOASIS(dna, dnaPath);
			var activated = await provider.ActivateProviderAsync();
			activated.IsError.Should().BeFalse(activated.Message);
			provider.IsProviderActivated.Should().BeTrue();

			var holon = new Holon { Id = Guid.NewGuid(), Name = "Integration Holon", HolonType = HolonType.Holon };
			var saved = await provider.SaveHolonAsync(holon);
			saved.IsError.Should().BeFalse();

			var all = await provider.LoadAllHolonsAsync();
			all.IsError.Should().BeFalse();
			all.Result.Any(h => h.Id == holon.Id).Should().BeTrue();

			var loaded = await provider.LoadHolonAsync(holon.Id);
			loaded.IsError.Should().BeFalse(loaded.Message);
			loaded.Result.Should().NotBeNull();
			loaded.Result.Id.Should().Be(holon.Id);
		}
	}
}


