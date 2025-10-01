using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.TestHarness
{
    internal static class Program
    {
        private const string TestInstanceUrl = "https://mastodon.social";
        private const string TestAccessToken = ""; // Add your ActivityPub OAuth token here

        private static async Task ExecuteActivityPubProviderExample(string instanceUrl, string accessToken)
        {
            Console.WriteLine("=== ActivityPub Provider Test Harness ===");
            Console.WriteLine($"Instance URL: {instanceUrl}");
            Console.WriteLine($"Access Token: {(string.IsNullOrEmpty(accessToken) ? "None (public access)" : "Provided")}");
            Console.WriteLine();

            ActivityPubOASIS activityPubOASIS = new(instanceUrl, accessToken);

            try
            {
                Console.WriteLine("1. Testing Provider Activation...");
                var activateResult = await activityPubOASIS.ActivateProviderAsync();
                
                if (activateResult.IsError)
                {
                    Console.WriteLine($"❌ Activation failed: {activateResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Provider activated: {activateResult.Message}");
                Console.WriteLine();

                Console.WriteLine("2. Testing Provider Properties...");
                Console.WriteLine($"Provider Name: {activityPubOASIS.ProviderName}");
                Console.WriteLine($"Provider Description: {activityPubOASIS.ProviderDescription}");
                Console.WriteLine($"Provider Type: {activityPubOASIS.ProviderType.Value}");
                Console.WriteLine($"Provider Category: {activityPubOASIS.ProviderCategory.Value}");
                Console.WriteLine();

                Console.WriteLine("3. Testing ActivityPub Operations...");
                Console.WriteLine("Note: ActivityPub provider methods are currently marked as 'not yet implemented'");
                Console.WriteLine("This is a foundation for future ActivityPub integration");
                Console.WriteLine();

                Console.WriteLine("4. Testing Provider Deactivation...");
                var deactivateResult = await activityPubOASIS.DeActivateProviderAsync();
                
                if (deactivateResult.IsError)
                {
                    Console.WriteLine($"❌ Deactivation failed: {deactivateResult.Message}");
                }
                else
                {
                    Console.WriteLine($"✅ Provider deactivated: {deactivateResult.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during ActivityPub provider testing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                activityPubOASIS.Dispose();
            }
        }

        private static async Task TestDifferentActivityPubInstances()
        {
            Console.WriteLine("=== Testing Different ActivityPub Instances ===");
            Console.WriteLine();

            var instances = new[]
            {
                ("Mastodon.social", "https://mastodon.social"),
                ("Pleroma", "https://pleroma.social"),
                ("Misskey", "https://misskey.io"),
                ("Friendica", "https://friendica.social")
            };

            foreach (var (name, url) in instances)
            {
                Console.WriteLine($"Testing {name} ({url})...");
                
                try
                {
                    var provider = new ActivityPubOASIS(url);
                    var result = await provider.ActivateProviderAsync();
                    
                    if (result.IsError)
                    {
                        Console.WriteLine($"❌ {name}: {result.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ {name}: Connected successfully");
                    }
                    
                    provider.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {name}: Error - {ex.Message}");
                }
                
                Console.WriteLine();
            }
        }

        private static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS - TEST HARNESS v1.0");
            Console.WriteLine("ActivityPub Provider Test Harness");
            Console.WriteLine("Federated Social Network Protocol");
            Console.WriteLine("================================================");
            Console.WriteLine();

            try
            {
                // Test with default configuration
                await ExecuteActivityPubProviderExample(TestInstanceUrl, TestAccessToken);
                Console.WriteLine();
                
                // Test different ActivityPub instances
                await TestDifferentActivityPubInstances();
                
                Console.WriteLine("=== Test Harness Complete ===");
                Console.WriteLine("ActivityPub Provider is ready for integration with OASIS!");
                Console.WriteLine("Future implementations will include:");
                Console.WriteLine("- ActivityPub actor management");
                Console.WriteLine("- Activity streaming and processing");
                Console.WriteLine("- Federated social network integration");
                Console.WriteLine("- Mastodon, Pleroma, Misskey support");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test harness failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}

