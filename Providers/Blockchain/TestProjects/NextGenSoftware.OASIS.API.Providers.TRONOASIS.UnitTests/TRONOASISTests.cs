using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests
{
	public class TRONOASISTests
	{
		[Fact]
		public async Task ActivateProvider_WithoutNodeConfiguration_ShouldFail()
		{
			var provider = new TRONOASIS();
			var result = await provider.ActivateProviderAsync();
			
			result.IsError.Should().BeTrue();
			result.Result.Should().BeFalse();
		}

		[Fact]
		public async Task SaveHolon_WithoutEncryptedStorage_ShouldFail()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var holon = new Holon
			{
				Id = Guid.NewGuid(),
				Name = "Unit Test Holon",
				MetaData = new Dictionary<string, object> { { "k", "v" } }
			};

			var result = await provider.SaveHolonAsync(holon);
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not configured");
		}

		[Fact]
		public async Task LoadAllHolons_WithoutEncryptedStorage_ShouldFail()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = await provider.LoadAllHolonsAsync();
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not configured");
			result.Result.Should().BeNull();
		}

		[Fact]
		public async Task LoadHolonsByMetaData_WithoutEncryptedStorage_ShouldFail()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = await provider.LoadHolonsByMetaDataAsync("env", "test");
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not configured");
			result.Result.Should().BeNull();
		}

		[Fact]
		public async Task Import_WithoutEncryptedStorage_ShouldFail()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var holons = new List<IHolon>
			{
				new Holon { Id = Guid.NewGuid(), Name = "A" },
				new Holon { Id = Guid.NewGuid(), Name = "B" }
			};

			var result = await provider.ImportAsync(holons);
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not configured");
			result.Result.Should().BeFalse();
		}

		[Fact]
		public void GetAvatarsNearMe_WithoutConfiguration_ShouldFail()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = provider.GetAvatarsNearMe(0, 0, 100);
			result.IsError.Should().BeTrue();
			result.Result.Should().BeNull();
		}

		[Fact]
		public void GetHolonsNearMe_WithoutConfiguration_ShouldFail()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = provider.GetHolonsNearMe(0, 0, 100, HolonType.Holon);
			result.IsError.Should().BeTrue();
			result.Result.Should().BeNull();
		}

		[Fact]
		public void SendTransaction_WithoutConfiguration_ShouldFail()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = provider.SendTransaction("sender", "recipient", 1m, "test");
			result.IsError.Should().BeTrue();
			result.Result.Should().BeNull();
		}

		[Fact]
		public void MintNFT_ShouldRejectMissingRequestBeforeActivation()
		{
			var provider = new TRONOASIS();
			var result = provider.MintNFT(null);
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("transaction");
			result.Result.Should().BeNull();
			provider.IsProviderActivated.Should().BeFalse();
		}
	}
}
