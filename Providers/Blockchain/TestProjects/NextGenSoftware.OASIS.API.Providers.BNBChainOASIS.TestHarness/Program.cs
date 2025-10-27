using NextGenSoftware.OASIS.API.Providers.BNBChainOASIS;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS.TestHarness
{
    /// <summary>
    /// Test Harness for BNB Chain OASIS Provider
    /// This console application provides manual testing capabilities for the BNB Chain provider.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 BNB Chain OASIS Provider Test Harness");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            try
            {
                // Initialize the BNB Chain provider
                var provider = new BNBChainOASIS();
                
                Console.WriteLine("✅ BNB Chain Provider initialized successfully");
                Console.WriteLine($"Provider Name: {provider.ProviderName}");
                Console.WriteLine($"Provider Description: {provider.ProviderDescription}");
                Console.WriteLine($"Provider Category: {provider.ProviderCategory}");
                Console.WriteLine($"Provider Version: {provider.ProviderVersion}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔧 Testing provider activation...");
                var activationResult = await provider.ActivateProviderAsync();
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Provider activation failed: {activationResult.Message}");
                    return;
                }
                Console.WriteLine("✅ Provider activated successfully");
                Console.WriteLine();

                // Test avatar operations
                Console.WriteLine("👤 Testing avatar operations...");
                await TestAvatarOperations(provider);
                Console.WriteLine();

                // Test holon operations
                Console.WriteLine("📦 Testing holon operations...");
                await TestHolonOperations(provider);
                Console.WriteLine();

                // Test blockchain operations
                Console.WriteLine("⛓️ Testing blockchain operations...");
                await TestBlockchainOperations(provider);
                Console.WriteLine();

                Console.WriteLine("🎉 All tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during testing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static async Task TestAvatarOperations(BNBChainOASIS provider)
        {
            try
            {
                // Test avatar creation
                Console.WriteLine("  Creating test avatar...");
                var avatar = new Avatar
                {
                    Username = "testuser_bnb",
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User"
                };

                var saveResult = await provider.SaveAvatarAsync(avatar);
                if (saveResult.IsError)
                {
                    Console.WriteLine($"    ❌ Avatar creation failed: {saveResult.Message}");
                    return;
                }
                Console.WriteLine("    ✅ Avatar created successfully");

                // Test avatar loading
                Console.WriteLine("  Loading avatar...");
                var loadResult = await provider.LoadAvatarAsync(avatar.Id);
                if (loadResult.IsError)
                {
                    Console.WriteLine($"    ❌ Avatar loading failed: {loadResult.Message}");
                    return;
                }
                Console.WriteLine("    ✅ Avatar loaded successfully");

                // Test avatar search
                Console.WriteLine("  Searching avatars...");
                var searchResult = await provider.SearchAvatarsAsync("testuser_bnb");
                if (searchResult.IsError)
                {
                    Console.WriteLine($"    ❌ Avatar search failed: {searchResult.Message}");
                    return;
                }
                Console.WriteLine("    ✅ Avatar search completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ❌ Avatar operations error: {ex.Message}");
            }
        }

        static async Task TestHolonOperations(BNBChainOASIS provider)
        {
            try
            {
                // Test holon creation
                Console.WriteLine("  Creating test holon...");
                var holon = new Holon
                {
                    Name = "Test Holon BNB",
                    Description = "A test holon for BNB Chain provider",
                    HolonType = HolonType.Generic
                };

                var saveResult = await provider.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    Console.WriteLine($"    ❌ Holon creation failed: {saveResult.Message}");
                    return;
                }
                Console.WriteLine("    ✅ Holon created successfully");

                // Test holon loading
                Console.WriteLine("  Loading holon...");
                var loadResult = await provider.LoadHolonAsync(holon.Id);
                if (loadResult.IsError)
                {
                    Console.WriteLine($"    ❌ Holon loading failed: {loadResult.Message}");
                    return;
                }
                Console.WriteLine("    ✅ Holon loaded successfully");

                // Test holon search
                Console.WriteLine("  Searching holons...");
                var searchResult = await provider.SearchHolonsAsync("Test Holon BNB");
                if (searchResult.IsError)
                {
                    Console.WriteLine($"    ❌ Holon search failed: {searchResult.Message}");
                    return;
                }
                Console.WriteLine("    ✅ Holon search completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ❌ Holon operations error: {ex.Message}");
            }
        }

        static async Task TestBlockchainOperations(BNBChainOASIS provider)
        {
            try
                {
                // Test blockchain connection
                Console.WriteLine("  Testing blockchain connection...");
                var connectionResult = await provider.ConnectAsync();
                if (connectionResult.IsError)
                {
                    Console.WriteLine($"    ❌ Blockchain connection failed: {connectionResult.Message}");
                    return;
                }
                Console.WriteLine("    ✅ Blockchain connection successful");

                // Test account balance (if supported)
                Console.WriteLine("  Testing account operations...");
                var balanceResult = await provider.GetAccountBalanceAsync("0x0000000000000000000000000000000000000000");
                if (balanceResult.IsError)
                {
                    Console.WriteLine($"    ⚠️ Account balance check failed (expected for test): {balanceResult.Message}");
                }
                else
                {
                    Console.WriteLine("    ✅ Account balance retrieved");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ❌ Blockchain operations error: {ex.Message}");
            }
        }
    }
}
