using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests
{
	public class IPFSOASISTests
	{
		[Fact]
		public async Task SaveAndLoadHolon_Works()
		{
			var provider = new IPFSOASIS();
			provider.ActivateProvider();

			var holon = new Holon
			{
				Id = Guid.NewGuid(),
				Name = "Unit Test Holon",
				MetaData = new Dictionary<string, object> { { "k", "v" } }
			};

			var save = await provider.SaveHolonAsync(holon);
			save.IsError.Should().BeFalse();

			var loadAll = await provider.LoadAllHolonsAsync();
			loadAll.IsError.Should().BeFalse();
			loadAll.Result.Any(h => h.Id == holon.Id).Should().BeTrue();
		}

		[Fact]
		public async Task LoadHolonsByMetaData_Filters()
		{
			var provider = new IPFSOASIS();
			provider.ActivateProvider();

			var holon = new Holon { Id = Guid.NewGuid(), Name = "Meta Holon", MetaData = new Dictionary<string, object> { { "env", "test" } } };
			await provider.SaveHolonAsync(holon);

			var filtered = await provider.LoadHolonsByMetaDataAsync("env", "test");
			filtered.IsError.Should().BeFalse();
			filtered.Result.Any(h => h.Id == holon.Id).Should().BeTrue();
		}

		[Fact]
		public async Task ImportExport_Works()
		{
			var provider = new IPFSOASIS();
			provider.ActivateProvider();

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
		}
	}
}


