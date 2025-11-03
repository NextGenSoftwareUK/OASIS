using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests
{
	public class TRONOASISIntegrationTests
	{
		[Fact]
		public async Task ProviderLifecycle_ShouldWork()
		{
			var provider = new TRONOASIS();
			
			// Activate
			var activateResult = await provider.ActivateProviderAsync();
			activateResult.IsError.Should().BeFalse();
			
			// Test operations (currently not supported)
			var holon = new Holon { Id = Guid.NewGuid(), Name = "Integration Holon" };
			var saveResult = await provider.SaveHolonAsync(holon);
			saveResult.IsError.Should().BeTrue(); // Expected - not supported yet
			saveResult.Message.Should().Contain("not supported");
			
			// Deactivate
			var deactivateResult = await provider.DeActivateProviderAsync();
			deactivateResult.IsError.Should().BeFalse();
		}

		[Fact]
		public async Task HttpClientIntegration_ShouldBeAvailable()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();
			
			// Test that HttpClient is available for future TRON API integration
			// This is a placeholder for when real TRON API calls are implemented
			var result = await provider.LoadAllHolonsAsync();
			result.IsError.Should().BeTrue(); // Expected - not implemented yet
			result.Message.Should().Contain("not supported");
		}
	}
}