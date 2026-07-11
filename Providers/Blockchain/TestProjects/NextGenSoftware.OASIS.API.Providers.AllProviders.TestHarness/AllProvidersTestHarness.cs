using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

// Blockchain providers
using NextGenSoftware.OASIS.API.Providers.AptosOASIS;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS;
using NextGenSoftware.OASIS.API.Providers.StarknetOASIS;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS;
using NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
using NextGenSoftware.OASIS.API.Providers.FantomOASIS;
using NextGenSoftware.OASIS.API.Providers.BNBChainOASIS;
using NextGenSoftware.OASIS.API.Providers.OptimismOASIS;
using NextGenSoftware.OASIS.API.Providers.CardanoOASIS;
using NextGenSoftware.OASIS.API.Providers.NEAROASIS;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;
using NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;
using NextGenSoftware.OASIS.API.Providers.BaseOASIS;
using NextGenSoftware.OASIS.API.Providers.TONOASIS;
using NextGenSoftware.OASIS.API.Providers.ZkSyncOASIS;
using NextGenSoftware.OASIS.API.Providers.LineaOASIS;
using NextGenSoftware.OASIS.API.Providers.ScrollOASIS;
using NextGenSoftware.OASIS.API.Providers.XRPLOASIS;

// Storage / network / cloud providers
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using NextGenSoftware.OASIS.API.Providers.AWSOASIS;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AllProviders.TestHarness
{
    public static class AllProvidersTestHarness
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== OASIS All-Providers Test Harness ===");
            Console.WriteLine("Running minimal CRUD + search smoke tests against each provider...\n");

            var providers = CreateProviders();

            foreach (var provider in providers)
            {
                Console.WriteLine($"\n===== {provider.ProviderName} ({provider.ProviderType.Value}) =====");
                try
                {
                    await RunProviderSmokeTestAsync(provider);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Unhandled exception in {provider.ProviderName}: {ex.Message}");
                }
            }

            Console.WriteLine("\n=== All provider smoke tests completed ===");
        }

        private static List<IOASISStorageProvider> CreateProviders() =>
            new()
            {
                // Blockchain
                new AptosOASIS(),
                new SuiOASIS(),
                new PolkadotOASIS(),
                new CosmosBlockChainOASIS(),
                new TRONOASIS(),
                new RadixOASIS(),
                new StarknetOASIS(),
                new MidenOASIS(),
                new AvalancheOASIS(),
                new TelosOASIS(),
                new BlockStackOASIS(),
                new EthereumOASIS(),
                new ArbitrumOASIS(),
                new FantomOASIS(),
                new BNBChainOASIS(),
                new OptimismOASIS(),
                new CardanoOASIS(),
                new NEAROASIS(),
                new HashgraphOASIS(),
                new ChainLinkOASIS(),
                new BitcoinOASIS(),
                new BaseOASIS("https://mainnet.base.org", "", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000"),
                new TONOASIS("https://ton-evm.example.com", "", "0x0000000000000000000000000000000000000000"),
                new ZkSyncOASIS("https://mainnet.era.zksync.io", "", "0x0000000000000000000000000000000000000000"),
                new LineaOASIS("https://rpc.linea.build", "", "0x0000000000000000000000000000000000000000"),
                new ScrollOASIS("https://rpc.scroll.io", "", "0x0000000000000000000000000000000000000000"),
                new XRPLOASIS("https://s1.ripple.com:51234", "rXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"),

                // Storage / Network / Cloud
                new LocalFileOASIS(),
                new Neo4jOASIS(),
                new MongoDBOASIS(),
                new AzureCosmosDBOASIS(),
                new AWSOASIS(),
                new ThreeFoldOASIS(),
                new ActivityPubOASIS(),
                new IPFSOASIS()
            };

        private static async Task RunProviderSmokeTestAsync(IOASISStorageProvider provider)
        {
            Console.WriteLine("[INFO] Activating provider...");
            var activate = provider.ActivateProvider();
            Console.WriteLine($"[INFO] Activate: error={activate.IsError}, msg={activate.Message}");

            var avatarId = Guid.NewGuid();
            var holonId = Guid.NewGuid();

            var avatar = new Avatar
            {
                Id = avatarId,
                Username = $"{provider.ProviderType.Value}_TestUser",
                Email = $"{provider.ProviderType.Value.ToString().ToLower()}_test@example.com",
                FirstName = "Test",
                LastName = "User",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"[INFO] Saving avatar {avatar.Username}...");
            var saveAvatar = await provider.SaveAvatarAsync(avatar);
            Console.WriteLine($"[INFO] SaveAvatar: error={saveAvatar.IsError}, msg={saveAvatar.Message}");

            Console.WriteLine("[INFO] Loading avatar by Id...");
            var loadAvatar = await provider.LoadAvatarAsync(avatarId);
            Console.WriteLine($"[INFO] LoadAvatar: error={loadAvatar.IsError}, msg={loadAvatar.Message}");

            var holon = new Holon
            {
                Id = holonId,
                Name = $"{provider.ProviderType.Value}_TestHolon",
                Description = $"Test holon for {provider.ProviderName}",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"[INFO] Saving holon {holon.Name}...");
            var saveHolon = await provider.SaveHolonAsync(holon);
            Console.WriteLine($"[INFO] SaveHolon: error={saveHolon.IsError}, msg={saveHolon.Message}");

            Console.WriteLine("[INFO] Loading holon by Id...");
            var loadHolon = await provider.LoadHolonAsync(holonId);
            Console.WriteLine($"[INFO] LoadHolon: error={loadHolon.IsError}, msg={loadHolon.Message}");

            // Search smoke test (if implemented)
            var searchParams = new SearchParams
            {
                SearchQuery = "test",
                SearchType = SearchType.Holon
            };

            Console.WriteLine("[INFO] Running search (if supported)...");
            try
            {
                var searchResult = await provider.SearchAsync(searchParams);
                Console.WriteLine($"[INFO] Search: error={searchResult.IsError}, msg={searchResult.Message}");
            }
            catch (NotImplementedException)
            {
                Console.WriteLine("[WARN] Search not implemented for this provider.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Exception during search: {ex.Message}");
            }

            Console.WriteLine("[INFO] Deactivating provider...");
            var deactivate = provider.DeActivateProvider();
            Console.WriteLine($"[INFO] DeActivate: error={deactivate.IsError}, msg={deactivate.Message}");
        }
    }
}


