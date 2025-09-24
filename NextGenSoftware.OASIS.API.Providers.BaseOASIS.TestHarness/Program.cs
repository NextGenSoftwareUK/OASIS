using System;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.BaseOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("BASE OASIS TEST HARNESS V1.0");
            Console.WriteLine("==============================");
            Console.WriteLine("");

            try
            {
                // Configuration for Base blockchain
                // Mainnet: Chain ID 8453, Testnet: Chain ID 84532
                string hostUri = "https://mainnet.base.org"; // Change to https://sepolia.base.org for testnet
                string chainPrivateKey = "YOUR_PRIVATE_KEY_HERE"; // Replace with actual private key
                BigInteger chainId = 8453; // 8453 for mainnet, 84532 for testnet
                string contractAddress = "YOUR_CONTRACT_ADDRESS_HERE"; // Replace with deployed contract address

                // Initialize provider
                var provider = new BaseOASIS(hostUri, chainPrivateKey, chainId, contractAddress);

                // Test provider activation
                Console.WriteLine("Testing provider activation...");
                var activationResult = provider.ActivateProvider();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Activation failed: {activationResult.Message}");
                    if (activationResult.Exception != null)
                    {
                        Console.WriteLine($"Exception: {activationResult.Exception}");
                    }
                    return;
                }
                
                Console.WriteLine("✅ Provider activated successfully");
                Console.WriteLine($"Provider Name: {provider.ProviderName}");
                Console.WriteLine($"Provider Description: {provider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {provider.ProviderType.Value}");
                Console.WriteLine($"Provider Category: {provider.ProviderCategory.Value}");
                Console.WriteLine("");

                // Test basic operations
                TestBasicOperations(provider);

                // Test NFT operations
                TestNFTOperations(provider);

                // Test transaction operations
                TestTransactionOperations(provider);

                // Test cleanup
                Console.WriteLine("Testing provider deactivation...");
                var deactivationResult = provider.DeActivateProvider();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Deactivation failed: {deactivationResult.Message}");
                }
                else
                {
                    Console.WriteLine("✅ Provider deactivated successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test harness error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("");
            Console.WriteLine("Test harness completed. Press any key to exit...");
            Console.ReadKey();
        }

        static void TestBasicOperations(BaseOASIS provider)
        {
            Console.WriteLine("--- Testing Basic Operations ---");
            
            try
            {
                // Test avatar operations would go here
                // Note: These require actual wallet setup and contract deployment
                Console.WriteLine("✅ Basic operations test completed (placeholder)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Basic operations test failed: {ex.Message}");
            }
            
            Console.WriteLine("");
        }

        static void TestNFTOperations(BaseOASIS provider)
        {
            Console.WriteLine("--- Testing NFT Operations ---");
            
            try
            {
                // Test NFT operations would go here
                // Note: These require actual wallet setup and contract deployment
                Console.WriteLine("✅ NFT operations test completed (placeholder)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ NFT operations test failed: {ex.Message}");
            }
            
            Console.WriteLine("");
        }

        static void TestTransactionOperations(BaseOASIS provider)
        {
            Console.WriteLine("--- Testing Transaction Operations ---");
            
            try
            {
                // Test transaction operations would go here
                // Note: These require actual wallet setup and sufficient balance
                Console.WriteLine("✅ Transaction operations test completed (placeholder)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Transaction operations test failed: {ex.Message}");
            }
            
            Console.WriteLine("");
        }
    }
}
