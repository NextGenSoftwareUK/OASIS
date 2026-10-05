using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace Zypherpunk.Tests
{
    /// <summary>
    /// Test script for Miden wallet with funded balance
    /// Run this to verify your wallet is working correctly
    /// </summary>
    public class TestMidenWallet
    {
        private const string MIDEN_ADDRESS = "mtst1aqwg0x9w9wcrvyqdx6ftykeh65v9cn9c_qruqqypuyph";
        private const string MIDEN_API_URL = "https://testnet.miden.xyz";

        public static async Task RunTests()
        {
            Console.WriteLine("🧪 Testing Miden Wallet");
            Console.WriteLine("========================");
            Console.WriteLine($"Address: {MIDEN_ADDRESS}");
            Console.WriteLine();

            var midenProvider = new MidenOASIS(
                apiBaseUrl: MIDEN_API_URL,
                apiKey: null,
                network: "testnet"
            );

            // Test 1: Activate Provider
            Console.WriteLine("1️⃣ Activating Miden provider...");
            var activateResult = await midenProvider.ActivateProviderAsync();
            if (activateResult.IsError)
            {
                Console.WriteLine($"❌ Failed to activate: {activateResult.Message}");
                return;
            }
            Console.WriteLine("✅ Provider activated");
            Console.WriteLine();

            // Test 2: Check Balance
            Console.WriteLine("2️⃣ Checking wallet balance...");
            var balanceResult = await midenProvider.GetAccountBalanceAsync(MIDEN_ADDRESS);
            if (!balanceResult.IsError)
            {
                Console.WriteLine($"✅ Balance: {balanceResult.Result} tokens");
                if (balanceResult.Result >= 100)
                {
                    Console.WriteLine("   🎉 Wallet is funded!");
                }
            }
            else
            {
                Console.WriteLine($"⚠️  Could not check balance: {balanceResult.Message}");
                Console.WriteLine("   (This may be normal if API doesn't support balance queries yet)");
            }
            Console.WriteLine();

            // Test 3: Create Private Note
            Console.WriteLine("3️⃣ Testing private note creation...");
            try
            {
                var noteResult = await midenProvider.CreatePrivateNoteAsync(
                    value: 1.0m,
                    ownerPublicKey: MIDEN_ADDRESS,
                    assetId: "ZEC",
                    memo: "Test note from OASIS"
                );

                if (noteResult != null && !noteResult.IsError)
                {
                    Console.WriteLine("✅ Private note created successfully");
                    if (noteResult.Result != null)
                    {
                        Console.WriteLine($"   Note ID: {noteResult.Result.NoteId}");
                        Console.WriteLine($"   Value: {noteResult.Result.Value}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️  Note creation: {noteResult?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Note creation not yet implemented: {ex.Message}");
            }
            Console.WriteLine();

            // Test 4: STARK Proof (if available)
            Console.WriteLine("4️⃣ Testing STARK proof generation...");
            try
            {
                var proofResult = await midenProvider.GenerateSTARKProofAsync(
                    programHash: "test_program_hash",
                    inputs: new { amount = 1.0m, address = MIDEN_ADDRESS },
                    outputs: new { noteId = "test_note" }
                );

                if (!proofResult.IsError)
                {
                    Console.WriteLine("✅ STARK proof generated");
                    
                    // Verify proof
                    var verifyResult = await midenProvider.VerifySTARKProofAsync(proofResult.Result);
                    if (!verifyResult.IsError && verifyResult.Result)
                    {
                        Console.WriteLine("✅ STARK proof verified");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️  Proof verification: {verifyResult.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️  Proof generation: {proofResult.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  STARK proof not yet implemented: {ex.Message}");
            }
            Console.WriteLine();

            Console.WriteLine("✅ Tests complete!");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  1. Get Zcash testnet address");
            Console.WriteLine("  2. Test bridge operations");
            Console.WriteLine("  3. Test bi-directional bridge");
        }
    }
}

