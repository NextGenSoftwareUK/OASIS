using NextGenSoftware.OASIS.API.Providers.AptosOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.TestHarness
{
    public class AptosOASISTestHarness
    {
        private static AptosOASIS _aptosProvider;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== AptosOASIS Test Harness ===");
            Console.WriteLine("Testing Aptos blockchain integration with OASIS...\n");

            _aptosProvider = new AptosOASIS();

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
            
            Console.WriteLine($"Provider Name: {_aptosProvider.ProviderName}");
            Console.WriteLine($"Provider Type: {_aptosProvider.ProviderType}");
            Console.WriteLine($"Provider Category: {_aptosProvider.ProviderCategory}");
            Console.WriteLine($"Is Activated: {_aptosProvider.IsProviderActivated}");

            var activationResult = _aptosProvider.ActivateProvider();
            Console.WriteLine($"Activation Result: {(activationResult.IsError ? "Failed" : "Success")}");
            if (activationResult.IsError)
                Console.WriteLine($"Error: {activationResult.Message}");

            Console.WriteLine($"Is Activated After Activation: {_aptosProvider.IsProviderActivated}");
            Console.WriteLine();
        }

        private static async Task TestProviderInformation()
        {
            Console.WriteLine("--- Testing Provider Information ---");
            
            var version = _aptosProvider.GetProviderVersion();
            Console.WriteLine($"Provider Version: {version}");

            var providerType = _aptosProvider.GetProviderType();
            Console.WriteLine($"Provider Type: {providerType}");

            var category = _aptosProvider.GetProviderCategory();
            Console.WriteLine($"Provider Category: {category}");

            var description = _aptosProvider.ProviderDescription;
            Console.WriteLine($"Provider Description: {description}");
            Console.WriteLine();
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("--- Testing Avatar Operations ---");
            
            var testAvatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "AptosTestUser",
                Email = "aptostest@example.com",
                FirstName = "Aptos",
                LastName = "Tester",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"Creating Avatar: {testAvatar.Username} ({testAvatar.Email})");
            var saveResult = await _aptosProvider.SaveAvatarAsync(testAvatar);
            Console.WriteLine($"Save Avatar Result: {(saveResult.IsError ? "Failed" : "Success")}");
            if (saveResult.IsError)
                Console.WriteLine($"Error: {saveResult.Message}");

            Console.WriteLine($"Loading Avatar by ID: {testAvatar.Id}");
            var loadResult = await _aptosProvider.LoadAvatarAsync(testAvatar.Id);
            Console.WriteLine($"Load Avatar Result: {(loadResult.IsError ? "Failed" : "Success")}");
            if (loadResult.IsError)
                Console.WriteLine($"Error: {loadResult.Message}");

            Console.WriteLine($"Loading Avatar by Email: {testAvatar.Email}");
            var emailResult = await _aptosProvider.GetAvatarByEmailAsync(testAvatar.Email);
            Console.WriteLine($"Load by Email Result: {(emailResult.IsError ? "Failed" : "Success")}");
            if (emailResult.IsError)
                Console.WriteLine($"Error: {emailResult.Message}");

            Console.WriteLine($"Loading Avatar by Username: {testAvatar.Username}");
            var usernameResult = await _aptosProvider.GetAvatarByUsernameAsync(testAvatar.Username);
            Console.WriteLine($"Load by Username Result: {(usernameResult.IsError ? "Failed" : "Success")}");
            if (usernameResult.IsError)
                Console.WriteLine($"Error: {usernameResult.Message}");

            Console.WriteLine();
        }

        private static async Task TestHolonOperations()
        {
            Console.WriteLine("--- Testing Holon Operations ---");
            
            var testHolon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "AptosTestHolon",
                Description = "Test Holon for Aptos blockchain integration",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"Creating Holon: {testHolon.Name}");
            var saveResult = await _aptosProvider.SaveHolonAsync(testHolon);
            Console.WriteLine($"Save Holon Result: {(saveResult.IsError ? "Failed" : "Success")}");
            if (saveResult.IsError)
                Console.WriteLine($"Error: {saveResult.Message}");

            Console.WriteLine($"Loading Holon by ID: {testHolon.Id}");
            var loadResult = await _aptosProvider.LoadHolonAsync(testHolon.Id);
            Console.WriteLine($"Load Holon Result: {(loadResult.IsError ? "Failed" : "Success")}");
            if (loadResult.IsError)
                Console.WriteLine($"Error: {loadResult.Message}");

            Console.WriteLine();
        }

        private static async Task TestSearchOperations()
        {
            Console.WriteLine("--- Testing Search Operations ---");
            
            var searchParams = new SearchParams
            {
                SearchQuery = "test",
                SearchType = SearchType.Avatar
            };

            Console.WriteLine($"Searching Avatars with query: '{searchParams.SearchQuery}'");
            var avatarSearchResult = await _aptosProvider.SearchAvatarsAsync(searchParams);
            Console.WriteLine($"Avatar Search Result: {(avatarSearchResult.IsError ? "Failed" : "Success")}");
            if (avatarSearchResult.IsError)
                Console.WriteLine($"Error: {avatarSearchResult.Message}");
            else
                Console.WriteLine($"Found {avatarSearchResult.Result?.NumberOfResults ?? 0} avatars");

            searchParams.SearchType = SearchType.Holon;
            Console.WriteLine($"Searching Holons with query: '{searchParams.SearchQuery}'");
            var holonSearchResult = await _aptosProvider.SearchHolonsAsync(searchParams);
            Console.WriteLine($"Holon Search Result: {(holonSearchResult.IsError ? "Failed" : "Success")}");
            if (holonSearchResult.IsError)
                Console.WriteLine($"Error: {holonSearchResult.Message}");
            else
                Console.WriteLine($"Found {holonSearchResult.Result?.NumberOfResults ?? 0} holons");

            Console.WriteLine();
        }

        private static async Task TestProviderDeactivation()
        {
            Console.WriteLine("--- Testing Provider Deactivation ---");
            
            Console.WriteLine($"Is Activated Before Deactivation: {_aptosProvider.IsProviderActivated}");
            
            var deactivationResult = _aptosProvider.DeActivateProvider();
            Console.WriteLine($"Deactivation Result: {(deactivationResult.IsError ? "Failed" : "Success")}");
            if (deactivationResult.IsError)
                Console.WriteLine($"Error: {deactivationResult.Message}");

            Console.WriteLine($"Is Activated After Deactivation: {_aptosProvider.IsProviderActivated}");
            Console.WriteLine();
        }
    }
}
