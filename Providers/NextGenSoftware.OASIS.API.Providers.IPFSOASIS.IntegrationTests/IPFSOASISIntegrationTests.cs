using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IntegrationTests
{
	public class IPFSOASISIntegrationTests
	{
		[Fact]
		public async Task SaveLoadRoundTrip_IPFS()
		{
			var provider = new IPFSOASIS();
			provider.ActivateProvider();

			var holon = new Holon { Id = Guid.NewGuid(), Name = "Integration Holon" };
			var saved = await provider.SaveHolonAsync(holon);
			saved.IsError.Should().BeFalse();

			var all = await provider.LoadAllHolonsAsync();
			all.IsError.Should().BeFalse();
			all.Result.Any(h => h.Id == holon.Id).Should().BeTrue();
		}
	}
}


