using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.TestHarness
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("NEXTGEN SOFTWARE IPFSOASIS TEST HARNESS");
			Console.WriteLine("");

			var ipfs = new IPFSOASIS();
			ipfs.ActivateProvider();

			var holon = new Holon
			{
				Id = Guid.NewGuid(),
				Name = "Test Holon",
				Description = "Created from IPFS TestHarness",
				MetaData = new Dictionary<string, object> { { "env", "harness" }, { "component", "ipfs" } }
			};

			var saveHolon = await ipfs.SaveHolonAsync(holon);
			Console.WriteLine($"SaveHolon: {(saveHolon.IsError ? saveHolon.Message : saveHolon.Result?.Id.ToString())}");

			var allHolons = await ipfs.LoadAllHolonsAsync();
			Console.WriteLine($"LoadAllHolons Count: {allHolons.Result?.Count() ?? 0}");

			var metaHolons = await ipfs.LoadHolonsByMetaDataAsync("env", "harness");
			Console.WriteLine($"LoadHolonsByMetaData Count: {metaHolons.Result?.Count() ?? 0}");

			var exportAll = await ipfs.ExportAllAsync();
			Console.WriteLine($"ExportAll Count: {exportAll.Result?.Count() ?? 0}");

			ipfs.DeActivateProvider();
		}
	}
}
