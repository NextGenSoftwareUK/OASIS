using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS;
using Solnet.Rpc;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Bridge.TestHarness;

/// <summary>
/// Interactive test harness for OASIS Universal Token Bridge
/// Tests cross-chain swaps between Solana (SOL) and Radix (XRD)
/// </summary>
class Program
{
    private static SolanaOASIS? _solanaProvider;
    private static RadixOASIS? _radixProvider;
    private static CrossChainBridgeManager? _bridgeManager;
    private static SolanaBridgeService? _solanaBridge;

    static async Task Main(string[] args)
    {
        Console.Clear();
        PrintHeader();

        // Initialize providers
        if (!await InitializeProvidersAsync())
        {
            Console.WriteLine("\n❌ Failed to initialize providers. Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Main menu loop
        bool running = true;
        while (running)
        {
            Console.Clear();
            PrintHeader();
            PrintMenu();

            var choice = Console.ReadKey(true).KeyChar;
            Console.WriteLine();

            switch (choice)
            {
                case '1':
                    await TestSolanaAccountCreation();
                    break;
                case '2':
                    await TestRadixAccountCreation();
                    break;
                case '3':
                    await CheckSolanaBalance();
                    break;
                case '4':
                    await CheckRadixBalance();
                    break;
                case '5':
                    await TestSolToXrdSwap();
                    break;
                case '6':
                    await TestXrdToSolSwap();
                    break;
                case '7':
                    await ViewConfiguration();
                    break;
                case '0':
                    running = false;
                    Console.WriteLine("👋 Goodbye!");
                    break;
                default:
                    Console.WriteLine("❌ Invalid option. Try again.");
                    break;
            }

            if (running)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    static void PrintHeader()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       OASIS UNIVERSAL TOKEN BRIDGE - TEST HARNESS             ║");
        Console.WriteLine("║                  SOL ↔ XRD Cross-Chain Swaps                   ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    static void PrintMenu()
    {
        Console.WriteLine("═══════════════════════ TEST MENU ═══════════════════════════\n");
        Console.WriteLine("  ACCOUNT MANAGEMENT:");
        Console.WriteLine("    [1] Create New Solana Account");
        Console.WriteLine("    [2] Create New Radix Account");
        Console.WriteLine();
        Console.WriteLine("  BALANCE CHECKS:");
        Console.WriteLine("    [3] Check Solana Balance");
        Console.WriteLine("    [4] Check Radix Balance");
        Console.WriteLine();
        Console.WriteLine("  BRIDGE OPERATIONS:");
        Console.WriteLine("    [5] Test SOL → XRD Swap");
        Console.WriteLine("    [6] Test XRD → SOL Swap");
        Console.WriteLine();
        Console.WriteLine("  UTILITIES:");
        Console.WriteLine("    [7] View Configuration");
        Console.WriteLine("    [0] Exit");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        Console.Write("Select an option: ");
    }

    static async Task<bool> InitializeProvidersAsync()
    {
        Console.WriteLine("🔧 Initializing providers...\n");

        try
        {
            // Initialize Solana Provider (Devnet)
            Console.WriteLine("📡 Connecting to Solana Devnet...");
            var solanaRpc = ClientFactory.GetClient(Cluster.DevNet);
            
            // Create a technical account for Solana (in production, load from secure config)
            var solanaMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
            var solanaWallet = new Wallet(solanaMnemonic);
            var solanaTechnicalAccount = solanaWallet.Account;

            _solanaBridge = new SolanaBridgeService(solanaTechnicalAccount, solanaRpc);
            
            // Note: We'll create a mock Radix provider since it has compilation issues
            Console.WriteLine("📡 Radix provider initialization (mocked for now)...");
            
            // Create bridge manager with Solana only for now
            Console.WriteLine("🌉 Initializing Cross-Chain Bridge Manager...");
            // _bridgeManager = new CrossChainBridgeManager(_solanaBridge, radixBridge);

            Console.WriteLine("\n✅ Solana provider initialized successfully!");
            Console.WriteLine("⚠️  Radix provider pending (has compilation issues)");
            Console.WriteLine("\n📝 Note: Using TESTNET - no real funds at risk!");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Initialization failed: {ex.Message}");
            Console.WriteLine($"   {ex.GetType().Name}");
            return false;
        }
    }

    static async Task TestSolanaAccountCreation()
    {
        Console.WriteLine("\n🔑 Creating New Solana Account...\n");

        try
        {
            var result = await _solanaBridge!.CreateAccountAsync();

            if (!result.IsError && result.Result != default)
            {
                var (publicKey, privateKey, seedPhrase) = result.Result;

                Console.WriteLine("✅ Solana Account Created Successfully!\n");
                Console.WriteLine($"📍 Public Key (Address):");
                Console.WriteLine($"   {publicKey}\n");
                Console.WriteLine($"🔐 Private Key (Base64):");
                Console.WriteLine($"   {privateKey.Substring(0, 40)}... (truncated)\n");
                Console.WriteLine($"🌱 Seed Phrase (SAVE THIS SECURELY!):");
                Console.WriteLine($"   {seedPhrase}\n");
                Console.WriteLine("⚠️  To use this account, fund it with devnet SOL from:");
                Console.WriteLine("   https://faucet.solana.com");
            }
            else
            {
                Console.WriteLine($"❌ Failed to create account: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    static async Task TestRadixAccountCreation()
    {
        Console.WriteLine("\n🔑 Creating New Radix Account...\n");
        Console.WriteLine("⚠️  Radix provider not fully initialized yet (compilation issues)");
        Console.WriteLine("   This will be available once RadixOASIS is fixed.");
        await Task.CompletedTask;
    }

    static async Task CheckSolanaBalance()
    {
        Console.WriteLine("\n💰 Check Solana Balance\n");
        Console.Write("Enter Solana address: ");
        string? address = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(address))
        {
            Console.WriteLine("❌ Invalid address");
            return;
        }

        try
        {
            Console.WriteLine("\n🔍 Checking balance...");
            var result = await _solanaBridge!.GetAccountBalanceAsync(address);

            if (!result.IsError)
            {
                Console.WriteLine($"\n✅ Balance: {result.Result:F9} SOL");
                
                if (result.Result == 0)
                {
                    Console.WriteLine("\n💡 Account has no balance. Fund it from:");
                    Console.WriteLine("   https://faucet.solana.com");
                }
            }
            else
            {
                Console.WriteLine($"❌ Failed to check balance: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    static async Task CheckRadixBalance()
    {
        Console.WriteLine("\n💰 Check Radix Balance\n");
        Console.WriteLine("⚠️  Radix provider not fully initialized yet");
        await Task.CompletedTask;
    }

    static async Task TestSolToXrdSwap()
    {
        Console.WriteLine("\n🌉 Test SOL → XRD Bridge Swap\n");
        Console.WriteLine("⚠️  Full bridge functionality pending RadixOASIS completion");
        Console.WriteLine("\n📋 What this will do when complete:");
        Console.WriteLine("   1. Validate your SOL balance");
        Console.WriteLine("   2. Get real-time SOL/XRD exchange rate");
        Console.WriteLine("   3. Withdraw SOL from your account");
        Console.WriteLine("   4. Deposit equivalent XRD to destination");
        Console.WriteLine("   5. Verify transaction or auto-rollback on failure");
        await Task.CompletedTask;
    }

    static async Task TestXrdToSolSwap()
    {
        Console.WriteLine("\n🌉 Test XRD → SOL Bridge Swap\n");
        Console.WriteLine("⚠️  Full bridge functionality pending RadixOASIS completion");
        await Task.CompletedTask;
    }

    static async Task ViewConfiguration()
    {
        Console.WriteLine("\n⚙️  Current Configuration\n");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("\n📡 NETWORK ENDPOINTS:");
        Console.WriteLine($"   Solana:  Devnet (https://api.devnet.solana.com)");
        Console.WriteLine($"   Radix:   StokNet (https://stokenet.radixdlt.com)");
        Console.WriteLine("\n🔧 PROVIDER STATUS:");
        Console.WriteLine($"   Solana Bridge:  ✅ Active");
        Console.WriteLine($"   Radix Bridge:   ⏳ Pending (compilation issues)");
        Console.WriteLine($"   Bridge Manager: ⏳ Awaiting both providers");
        Console.WriteLine("\n🔐 SECURITY:");
        Console.WriteLine($"   Technical Accounts: Generated per session");
        Console.WriteLine($"   Private Keys: In-memory only (not persisted)");
        Console.WriteLine($"   Network: TESTNET (no real funds)");
        Console.WriteLine("\n💾 BRIDGE ARCHITECTURE:");
        Console.WriteLine($"   Interface: IOASISBridge (universal)");
        Console.WriteLine($"   Manager: CrossChainBridgeManager");
        Console.WriteLine($"   Safety: Atomic operations with auto-rollback");
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
        await Task.CompletedTask;
    }
}


