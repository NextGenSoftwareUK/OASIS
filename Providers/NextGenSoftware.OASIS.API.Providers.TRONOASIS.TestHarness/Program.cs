using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.TestHarness
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("NEXTGEN SOFTWARE TRONOASIS TEST HARNESS");
			Console.WriteLine("");

			var tron = new TRONOASIS();
			tron.ActivateProvider();

			// Test basic provider functionality
			Console.WriteLine("Testing TRON provider activation...");
			var activateResult = await tron.ActivateProviderAsync();
			Console.WriteLine($"Activation: {(activateResult.IsError ? activateResult.Message : "Success")}");

			// Test holon operations (currently not supported)
			var holon = new Holon
			{
				Id = Guid.NewGuid(),
				Name = "Test Holon",
				Description = "Created from TRON TestHarness",
				MetaData = new Dictionary<string, object> { { "env", "harness" }, { "component", "tron" } }
			};

			var saveHolon = await tron.SaveHolonAsync(holon);
			Console.WriteLine($"SaveHolon: {(saveHolon.IsError ? saveHolon.Message : saveHolon.Result?.Id.ToString())}");

			var allHolons = await tron.LoadAllHolonsAsync();
			Console.WriteLine($"LoadAllHolons: {(allHolons.IsError ? allHolons.Message : $"Count: {allHolons.Result?.Count() ?? 0}")}");

			// Test metadata search (currently not supported)
			var metaHolons = await tron.LoadHolonsByMetaDataAsync("env", "harness");
			Console.WriteLine($"LoadHolonsByMetaData: {(metaHolons.IsError ? metaHolons.Message : $"Count: {metaHolons.Result?.Count() ?? 0}")}");

			// Test export functionality (currently not supported)
			var exportAll = await tron.ExportAllAsync();
			Console.WriteLine($"ExportAll: {(exportAll.IsError ? exportAll.Message : $"Count: {exportAll.Result?.Count() ?? 0}")}");

			// Test network operations (not supported)
			var playersNearMe = tron.GetPlayersNearMe();
			Console.WriteLine($"GetPlayersNearMe: {(playersNearMe.IsError ? playersNearMe.Message : $"Count: {playersNearMe.Result?.Count() ?? 0}")}");

			tron.DeActivateProvider();
			Console.WriteLine("TRON TestHarness completed!");
		}
	}
}