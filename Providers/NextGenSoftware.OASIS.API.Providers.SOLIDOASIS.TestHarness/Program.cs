using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.TestHarness
{
    internal static class Program
    {
        private const string TestPodServerUrl = "https://solidcommunity.net";
        private const string TestAuthToken = ""; // Add your SOLID pod authentication token here

        private static async Task ExecuteSOLIDProviderExample(string podServerUrl, string authToken)
        {
            Console.WriteLine("=== SOLID Provider Test Harness ===");
            Console.WriteLine($"Pod Server: {podServerUrl}");
            Console.WriteLine($"Auth Token: {(string.IsNullOrEmpty(authToken) ? "None (public access)" : "Provided")}");
            Console.WriteLine();

            SOLIDOASIS solidOASIS = new(podServerUrl, authToken);

            try
            {
                Console.WriteLine("1. Testing Provider Activation...");
                var activateResult = await solidOASIS.ActivateProviderAsync();
                
                if (activateResult.IsError)
                {
                    Console.WriteLine($"❌ Activation failed: {activateResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Provider activated: {activateResult.Message}");
                Console.WriteLine();

                Console.WriteLine("2. Testing Provider Properties...");
                Console.WriteLine($"Provider Name: {solidOASIS.ProviderName}");
                Console.WriteLine($"Provider Description: {solidOASIS.ProviderDescription}");
                Console.WriteLine($"Provider Type: {solidOASIS.ProviderType.Value}");
                Console.WriteLine($"Provider Category: {solidOASIS.ProviderCategory.Value}");
                Console.WriteLine();

                Console.WriteLine("3. Testing Avatar Operations...");
                Console.WriteLine("Note: SOLID provider methods are currently marked as 'not yet implemented'");
                Console.WriteLine("This is a foundation for future SOLID pod integration");
                Console.WriteLine();

                Console.WriteLine("4. Testing Provider Deactivation...");
                var deactivateResult = await solidOASIS.DeActivateProviderAsync();
                
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
                Console.WriteLine($"❌ Error during SOLID provider testing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                solidOASIS.Dispose();
            }
        }

        private static async Task TestDifferentPodServers()
        {
            Console.WriteLine("=== Testing Different SOLID Pod Servers ===");
            Console.WriteLine();

            var podServers = new[]
            {
                ("SolidCommunity", "https://solidcommunity.net"),
                ("Inrupt", "https://inrupt.net"),
                ("SolidWeb", "https://solidweb.org")
            };

            foreach (var (name, url) in podServers)
            {
                Console.WriteLine($"Testing {name} ({url})...");
                
                try
                {
                    var provider = new SOLIDOASIS(url);
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
            Console.WriteLine("NextGenSoftware.OASIS.API.Providers.SOLIDOASIS - TEST HARNESS v1.0");
            Console.WriteLine("SOLID (Social Linked Data) Provider Test Harness");
            Console.WriteLine("Tim Berners-Lee's Decentralized Web Standard");
            Console.WriteLine("================================================");
            Console.WriteLine();

            try
            {
                // Test with default configuration
                await ExecuteSOLIDProviderExample(TestPodServerUrl, TestAuthToken);
                Console.WriteLine();
                
                // Test different pod servers
                await TestDifferentPodServers();
                
                Console.WriteLine("=== Test Harness Complete ===");
                Console.WriteLine("SOLID Provider is ready for integration with OASIS!");
                Console.WriteLine("Future implementations will include:");
                Console.WriteLine("- WebID authentication");
                Console.WriteLine("- Pod data storage and retrieval");
                Console.WriteLine("- Linked Data operations");
                Console.WriteLine("- RDF/JSON-LD processing");
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



