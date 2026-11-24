using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

// Blockchain providers

// Storage / network / cloud providers

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

        // Every provider is built by OASISBootLoader from OASIS_DNA.Development.json, exactly as ONODE registers it,
        // so the harness exercises the shipped constructors and settings instead of hand-written arguments.
        private static readonly ProviderType[] ProviderTypes =
        {
            ProviderType.AptosOASIS,
            ProviderType.SuiOASIS,
            ProviderType.PolkadotOASIS,
            ProviderType.CosmosBlockChainOASIS,
            ProviderType.TRONOASIS,
            ProviderType.RadixOASIS,
            ProviderType.StarknetOASIS,
            ProviderType.MidenOASIS,
            ProviderType.AvalancheOASIS,
            ProviderType.TelosOASIS,
            ProviderType.BlockStackOASIS,
            ProviderType.EthereumOASIS,
            ProviderType.ArbitrumOASIS,
            ProviderType.FantomOASIS,
            ProviderType.BNBChainOASIS,
            ProviderType.OptimismOASIS,
            ProviderType.CardanoOASIS,
            ProviderType.NEAROASIS,
            ProviderType.HashgraphOASIS,
            ProviderType.ChainLinkOASIS,
            ProviderType.BitcoinOASIS,
            ProviderType.BaseOASIS,
            ProviderType.TONOASIS,
            ProviderType.ZkSyncOASIS,
            ProviderType.LineaOASIS,
            ProviderType.ScrollOASIS,
            ProviderType.XRPLOASIS,
            ProviderType.LocalFileOASIS,
            ProviderType.Neo4jOASIS,
            ProviderType.MongoDBOASIS,
            ProviderType.AzureCosmosDBOASIS,
            ProviderType.AWSOASIS,
            ProviderType.ThreeFoldOASIS,
            ProviderType.ActivityPubOASIS,
            ProviderType.IPFSOASIS,
        };

        private static List<IOASISStorageProvider> CreateProviders()
        {
            var providers = new List<IOASISStorageProvider>();
            var boot = OASISBootLoader.OASISBootLoader.BootOASIS("OASIS_DNA.Development.json", activateDefaultStorageProvider: false);
            if (boot.IsError)
            {
                Console.WriteLine($"[ERROR] OASIS boot failed: {boot.Message}");
                return providers;
            }

            foreach (var providerType in ProviderTypes)
            {
                var registered = OASISBootLoader.OASISBootLoader.RegisterProvider(providerType, activateProviderIfOASISProviderBootTypeIsHot: false);
                if (registered.IsError || registered.Result == null)
                    Console.WriteLine($"[SKIP] {providerType}: {registered.Message}");
                else
                    providers.Add(registered.Result);
            }

            return providers;
        }

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
            var searchParamsQuery = "test";
            var searchParams = new SearchParams { SearchOnlyForCurrentAvatar = false, SearchGroups = new List<ISearchGroupBase> { new SearchTextGroup { SearchQuery = searchParamsQuery, SearchAvatars = false, SearchHolons = true } } };

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


