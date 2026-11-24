using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Request;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.TestHarness
{
    /// <summary>
    /// CargoOASIS exposes Cargo NFT platform API handlers (there is no IOASISStorageProvider class),
    /// so this harness exercises the read-only query handlers against the live Cargo API.
    /// </summary>
    public class CargoOASISTestHarness
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== CargoOASIS Test Harness ===");
            var http = new HttpHandler();

            string address = args.Length > 0 ? args[0] : "0x0000000000000000000000000000000000000000";

            Console.WriteLine("--- GetContracts ---");
            var contracts = await new GetContractsHandler(http).Handle(new GetContractsRequestHandler { Page = "1", Limit = "5" });
            Console.WriteLine(contracts.IsError ? $"Error: {contracts.Message}" : $"Total contracts: {contracts.Result?.Data?.Total}");

            Console.WriteLine($"--- GetAllUserCollectibles ({address}) ---");
            var collectibles = await new GetAllUserCollectiblesHandler(http).Handle(new GetAllUserCollectiblesRequestModel { Address = address, Page = "1", Limit = "5" });
            Console.WriteLine(collectibles.IsError ? $"Error: {collectibles.Message}" : "Collectibles loaded.");
        }
    }
}
