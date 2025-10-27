using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests
{
	public class TRONOASISTests
	{
		[Fact]
		public async Task ActivateProvider_ShouldSucceed()
		{
			var provider = new TRONOASIS();
			var result = await provider.ActivateProviderAsync();
			
			result.IsError.Should().BeFalse();
			result.Result.Should().BeTrue();
		}

		[Fact]
		public async Task SaveHolon_ShouldReturnNotSupported()
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
			result.Message.Should().Contain("not supported");
		}

		[Fact]
		public async Task LoadAllHolons_ShouldReturnNotSupported()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = await provider.LoadAllHolonsAsync();
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not supported");
			result.Result.Should().NotBeNull();
			result.Result.Should().BeEmpty();
		}

		[Fact]
		public async Task LoadHolonsByMetaData_ShouldReturnNotSupported()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = await provider.LoadHolonsByMetaDataAsync("env", "test");
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not supported");
			result.Result.Should().NotBeNull();
			result.Result.Should().BeEmpty();
		}

		[Fact]
		public async Task Import_ShouldReturnNotSupported()
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
			result.Message.Should().Contain("not supported");
			result.Result.Should().BeFalse();
		}

		[Fact]
		public void GetPlayersNearMe_ShouldReturnNotSupported()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = provider.GetPlayersNearMe();
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not supported");
			result.Result.Should().NotBeNull();
			result.Result.Should().BeEmpty();
		}

		[Fact]
		public void GetHolonsNearMe_ShouldReturnNotSupported()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = provider.GetHolonsNearMe(HolonType.Holon);
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not supported");
			result.Result.Should().NotBeNull();
			result.Result.Should().BeEmpty();
		}

		[Fact]
		public void SendTransaction_ShouldReturnNotImplemented()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = provider.SendTransaction(null);
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not implemented");
		}

		[Fact]
		public void MintNFT_ShouldReturnNotImplemented()
		{
			var provider = new TRONOASIS();
			provider.ActivateProvider();

			var result = provider.MintNFT(null);
			result.IsError.Should().BeTrue();
			result.Message.Should().Contain("not implemented");
		}
	}
}