using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Providers.MoralisOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MoralisDBOASIS.TestHarness
{
    public class MoralisDBOASISTestHarness
    {
        private static global::NextGenSoftware.OASIS.API.Providers.MoralisOASIS.MoralisOASIS _provider;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== MoralisDBOASIS Test Harness ===");
            Console.WriteLine("Testing MoralisDBOASIS integration with OASIS...\n");

            OASISDNA dna = OASISDNAManager.LoadDNA("OASIS_DNA.Development.json").Result;
            _provider = new global::NextGenSoftware.OASIS.API.Providers.MoralisOASIS.MoralisOASIS(dna.OASIS.StorageProviders.MoralisOASIS.ApiKey);

            try
            {
                await TestProviderActivation();
                await TestProviderInformation();
                await TestAvatarOperations();
                await TestHolonOperations();
                await TestSearchOperations();
                await TestProviderDeactivation();

                Console.WriteLine("\n=== All Tests Completed Successfully! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== Test Failed: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestProviderActivation()
        {
            Console.WriteLine("--- Testing Provider Activation ---");
            
            Console.WriteLine($"Provider Name: {_provider.ProviderName}");
            Console.WriteLine($"Provider Type: {_provider.ProviderType}");
            Console.WriteLine($"Provider Category: {_provider.ProviderCategory}");
            Console.WriteLine($"Is Activated: {_provider.IsProviderActivated}");

            var activationResult = _provider.ActivateProvider();
            Console.WriteLine($"Activation Result: {(activationResult.IsError ? "Failed" : "Success")}");
            if (activationResult.IsError)
                Console.WriteLine($"Error: {activationResult.Message}");

            Console.WriteLine($"Is Activated After Activation: {_provider.IsProviderActivated}");
            Console.WriteLine();
        }

        private static async Task TestProviderInformation()
        {
            Console.WriteLine("--- Testing Provider Information ---");
            
            var version = _provider.GetType().Assembly.GetName().Version;
            Console.WriteLine($"Provider Version: {version}");

            var providerType = _provider.ProviderType.Value;
            Console.WriteLine($"Provider Type: {providerType}");

            var category = _provider.ProviderCategory.Value;
            Console.WriteLine($"Provider Category: {category}");

            var description = _provider.ProviderDescription;
            Console.WriteLine($"Provider Description: {description}");
            Console.WriteLine();
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("--- Testing Avatar Operations ---");
            
            var testAvatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "MoralisDBOASISTestUser",
                Email = "moralisdboasistest@example.com",
                FirstName = "MoralisDBOASIS",
                LastName = "Tester",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"Creating Avatar: {testAvatar.Username} ({testAvatar.Email})");
            var saveResult = await _provider.SaveAvatarAsync(testAvatar);
            Console.WriteLine($"Save Avatar Result: {(saveResult.IsError ? "Failed" : "Success")}");
            if (saveResult.IsError)
                Console.WriteLine($"Error: {saveResult.Message}");

            Console.WriteLine($"Loading Avatar by ID: {testAvatar.Id}");
            var loadResult = await _provider.LoadAvatarAsync(testAvatar.Id);
            Console.WriteLine($"Load Avatar Result: {(loadResult.IsError ? "Failed" : "Success")}");
            if (loadResult.IsError)
                Console.WriteLine($"Error: {loadResult.Message}");

            Console.WriteLine();
        }

        private static async Task TestHolonOperations()
        {
            Console.WriteLine("--- Testing Holon Operations ---");
            
            var testHolon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "MoralisDBOASISTestHolon",
                Description = "Test Holon for MoralisDBOASIS integration",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"Creating Holon: {testHolon.Name}");
            var saveResult = await _provider.SaveHolonAsync(testHolon);
            Console.WriteLine($"Save Holon Result: {(saveResult.IsError ? "Failed" : "Success")}");
            if (saveResult.IsError)
                Console.WriteLine($"Error: {saveResult.Message}");

            Console.WriteLine($"Loading Holon by ID: {testHolon.Id}");
            var loadResult = await _provider.LoadHolonAsync(testHolon.Id);
            Console.WriteLine($"Load Holon Result: {(loadResult.IsError ? "Failed" : "Success")}");
            if (loadResult.IsError)
                Console.WriteLine($"Error: {loadResult.Message}");

            Console.WriteLine();
        }

        private static async Task TestSearchOperations()
        {
            Console.WriteLine("--- Testing Search Operations ---");
            
            var searchParamsQuery = "test";
            var searchParams = new SearchParams { SearchOnlyForCurrentAvatar = false, SearchGroups = new List<ISearchGroupBase> { new SearchTextGroup { SearchQuery = searchParamsQuery, SearchAvatars = true, SearchHolons = false } } };

            Console.WriteLine($"Searching Avatars with query: '{searchParamsQuery}'");
            var avatarSearchResult = await _provider.SearchAsync(searchParams);
            Console.WriteLine($"Avatar Search Result: {(avatarSearchResult.IsError ? "Failed" : "Success")}");
            if (avatarSearchResult.IsError)
                Console.WriteLine($"Error: {avatarSearchResult.Message}");
            else
                Console.WriteLine($"Found {avatarSearchResult.Result?.NumberOfResults ?? 0} avatars");

            Console.WriteLine();
        }

        private static async Task TestProviderDeactivation()
        {
            Console.WriteLine("--- Testing Provider Deactivation ---");
            
            Console.WriteLine($"Is Activated Before Deactivation: {_provider.IsProviderActivated}");
            
            var deactivationResult = _provider.DeActivateProvider();
            Console.WriteLine($"Deactivation Result: {(deactivationResult.IsError ? "Failed" : "Success")}");
            if (deactivationResult.IsError)
                Console.WriteLine($"Error: {deactivationResult.Message}");

            Console.WriteLine($"Is Activated After Deactivation: {_provider.IsProviderActivated}");
            Console.WriteLine();
        }
    }
}
