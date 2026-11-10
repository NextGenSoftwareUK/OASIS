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
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task ActivatesAndDeactivatesAgainstDedicatedKubo(bool synchronous)
		{
			var endpoint = Environment.GetEnvironmentVariable("IPFSOASIS_TEST_API");
			Assert.False(string.IsNullOrWhiteSpace(endpoint), "Configure the dedicated disposable Kubo test repository.");
			var dna = new OASISDNA();
			dna.OASIS.StorageProviders = new StorageProviderSettings
			{
				IPFSOASIS = new IPFSOASISSettings { ConnectionString = endpoint }
			};
			var dnaPath = Path.Combine(Path.GetTempPath(), $"ipfsoasis-activation-{Guid.NewGuid():N}.json");
			var provider = new IPFSOASIS(dna, dnaPath);
			try
			{
				var activation = synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
				Assert.True(activation.Result && !activation.IsError, activation.Message);
				Assert.True(provider.IsProviderActivated);
			}
			finally
			{
				try
				{
					var deactivation = await provider.DeActivateProviderAsync();
					Assert.True(deactivation.Result && !deactivation.IsError, deactivation.Message);
					Assert.False(provider.IsProviderActivated);
				}
				finally { File.Delete(dnaPath); }
			}
		}

		[Fact]
		public async Task SaveLoadRoundTrip_IPFS()
		{
			var endpoint = Environment.GetEnvironmentVariable("IPFSOASIS_TEST_API");
			Assert.False(string.IsNullOrWhiteSpace(endpoint), "Configure the dedicated disposable Kubo test repository.");
			var dna = new OASISDNA();
			dna.OASIS.StorageProviders = new StorageProviderSettings
			{
				IPFSOASIS = new IPFSOASISSettings
				{
					ConnectionString = endpoint
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


