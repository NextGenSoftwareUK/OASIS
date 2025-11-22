using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;

namespace BridgeDemo.Standalone;

/// <summary>
/// Standalone OASIS Universal Token Bridge Demo
/// Demonstrates core bridge functionality without full OASIS dependencies
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        PrintBanner();
        
        Console.WriteLine("🔧 Initializing Solana Devnet connection...\n");
        
        var rpcClient = ClientFactory.GetClient(Cluster.DevNet);
        
        // Test connection
        try
        {
            var health = await rpcClient.GetHealthAsync();
            Console.WriteLine($"✅ Connected to Solana Devnet");
            Console.WriteLine($"   Status: {health.Result}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection failed: {ex.Message}\n");
            return;
        }

        bool running = true;
        while (running)
        {
            PrintMenu();
            var choice = Console.ReadKey(true).KeyChar;
            Console.WriteLine();

            switch (choice)
            {
                case '1':
                    await CreateSolanaWallet();
                    break;
                case '2':
                    await CheckSolanaBalance(rpcClient);
                    break;
                case '3':
                    await ShowBridgeArchitecture();
                    break;
                case '4':
                    await SimulateBridgeSwap();
                    break;
                case '5':
                    await ShowFullOASISInfo();
                    break;
                case '0':
                    running = false;
                    Console.WriteLine("\n👋 Goodbye!");
                    break;
                default:
                    Console.WriteLine("❌ Invalid option\n");
                    break;
            }

            if (running)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
                PrintBanner();
            }
        }
    }

    static void PrintBanner()
    {
        Console.Clear();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         OASIS UNIVERSAL TOKEN BRIDGE - DEMO v1.0                ║");
        Console.WriteLine("║              Cross-Chain Atomic Swaps (SOL ↔ XRD)               ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    static void PrintMenu()
    {
        Console.WriteLine("══════════════════════════ DEMO MENU ══════════════════════════\n");
        Console.WriteLine("  LIVE DEMOS:");
        Console.WriteLine("    [1] 🔑 Create New Solana Wallet");
        Console.WriteLine("    [2] 💰 Check Solana Balance");
        Console.WriteLine();
        Console.WriteLine("  INFORMATION:");
        Console.WriteLine("    [3] 🏗️  View Bridge Architecture");
        Console.WriteLine("    [4] 🌉 Simulate Bridge Swap Flow");
        Console.WriteLine("    [5] 📚 Full OASIS Bridge Information");
        Console.WriteLine();
        Console.WriteLine("    [0] 🚪 Exit");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        Console.Write("Select an option: ");
    }

    static async Task CreateSolanaWallet()
    {
        Console.WriteLine("\n🔑 Creating New Solana Wallet...\n");

        try
        {
            // Generate new mnemonic (seed phrase)
            var mnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
            var wallet = new Wallet(mnemonic);

            Console.WriteLine("✅ Wallet Created Successfully!\n");
            Console.WriteLine($"📍 Public Key (Address):");
            Console.WriteLine($"   {wallet.Account.PublicKey}\n");
            Console.WriteLine($"🌱 Seed Phrase (SAVE THIS SECURELY!):");
            Console.WriteLine($"   {string.Join(" ", mnemonic.Words)}\n");
            Console.WriteLine($"🔐 Private Key (Base64):");
            Console.WriteLine($"   {Convert.ToBase64String(wallet.Account.PrivateKey).Substring(0, 40)}... (truncated)\n");
            Console.WriteLine("⚠️  To use this wallet:");
            Console.WriteLine("   1. Save the seed phrase securely");
            Console.WriteLine("   2. Fund it with devnet SOL from: https://faucet.solana.com");
            Console.WriteLine($"   3. Use your public key: {wallet.Account.PublicKey}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating wallet: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    static async Task CheckSolanaBalance(IRpcClient rpcClient)
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
            var response = await rpcClient.GetBalanceAsync(address);

            if (response.WasSuccessful && response.Result?.Value != null)
            {
                decimal balance = response.Result.Value / 1_000_000_000m; // Convert lamports to SOL
                Console.WriteLine($"\n✅ Balance: {balance:F9} SOL");

                if (balance == 0)
                {
                    Console.WriteLine("\n💡 Account has no balance. Get devnet SOL from:");
                    Console.WriteLine("   https://faucet.solana.com");
                }
                else
                {
                    Console.WriteLine($"\n💵 Equivalent lamports: {response.Result.Value:N0}");
                }
            }
            else
            {
                Console.WriteLine($"❌ Failed to check balance");
                if (response.Reason != null)
                {
                    Console.WriteLine($"   Reason: {response.Reason}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    static async Task ShowBridgeArchitecture()
    {
        Console.WriteLine("\n🏗️  OASIS UNIVERSAL TOKEN BRIDGE ARCHITECTURE\n");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("📦 CORE COMPONENTS:");
        Console.WriteLine();
        Console.WriteLine("   1. IOASISBridge Interface (Universal)");
        Console.WriteLine("      └─ Works with ANY blockchain");
        Console.WriteLine("      └─ 6 core methods:");
        Console.WriteLine("         • GetAccountBalanceAsync");
        Console.WriteLine("         • CreateAccountAsync");
        Console.WriteLine("         • RestoreAccountAsync");
        Console.WriteLine("         • WithdrawAsync");
        Console.WriteLine("         • DepositAsync");
        Console.WriteLine("         • GetTransactionStatusAsync");
        Console.WriteLine();
        Console.WriteLine("   2. Provider Implementations:");
        Console.WriteLine("      ✅ SolanaOASIS → SolanaBridgeService");
        Console.WriteLine("      ⏳ RadixOASIS → RadixBridgeService (pending)");
        Console.WriteLine("      ❌ EthereumOASIS → (6-8 hours to add)");
        Console.WriteLine("      ❌ PolygonOASIS → (6-8 hours to add)");
        Console.WriteLine("      ... (easy to extend to any chain)");
        Console.WriteLine();
        Console.WriteLine("   3. CrossChainBridgeManager:");
        Console.WriteLine("      └─ Orchestrates atomic swaps");
        Console.WriteLine("      └─ Automatic rollback on failure");
        Console.WriteLine("      └─ Exchange rate integration");
        Console.WriteLine("      └─ Multi-chain coordination");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("📂 FILE LOCATIONS:");
        Console.WriteLine();
        Console.WriteLine("Core Bridge:");
        Console.WriteLine("  /OASIS Architecture/NextGenSoftware.OASIS.API.Core/");
        Console.WriteLine("  └─ Managers/Bridge/");
        Console.WriteLine("     ├─ Interfaces/IOASISBridge.cs");
        Console.WriteLine("     ├─ CrossChainBridgeManager.cs");
        Console.WriteLine("     ├─ DTOs/ (Request/Response models)");
        Console.WriteLine("     └─ Services/ (Exchange rates)");
        Console.WriteLine();
        Console.WriteLine("Solana Implementation:");
        Console.WriteLine("  /Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/");
        Console.WriteLine("  └─ Infrastructure/Services/Solana/SolanaBridgeService.cs");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        await Task.CompletedTask;
    }

    static async Task SimulateBridgeSwap()
    {
        Console.WriteLine("\n🌉 BRIDGE SWAP SIMULATION (SOL → XRD)\n");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("This demonstrates the atomic swap flow:\n");

        var steps = new[]
        {
            ("1️⃣  User initiates swap", "Amount: 1.5 SOL → XRD"),
            ("2️⃣  Validate request", "Check amount > 0, valid addresses"),
            ("3️⃣  Get exchange rate", "SOL/XRD rate from CoinGecko/KuCoin"),
            ("4️⃣  Calculate converted amount", "1.5 SOL × rate = X XRD"),
            ("5️⃣  Check source balance", "Verify user has >= 1.5 SOL"),
            ("6️⃣  WITHDRAW: SOL → Technical Account", "Transfer 1.5 SOL from user"),
            ("✓", "If SUCCESS → Continue"),
            ("✗", "If FAIL → Return error, stop"),
            ("7️⃣  DEPOSIT: XRD → User", "Transfer X XRD to destination"),
            ("✓", "If SUCCESS → Continue"),
            ("✗", "If FAIL → ROLLBACK: Return 1.5 SOL"),
            ("8️⃣  Verify deposit transaction", "Confirm XRD received"),
            ("✓", "If SUCCESS → Complete!"),
            ("✗", "If FAIL → ROLLBACK: Return 1.5 SOL"),
            ("9️⃣  Return success", "Provide transaction hashes"),
        };

        foreach (var (step, description) in steps)
        {
            Console.WriteLine($"{step,-40} {description}");
            await Task.Delay(300); // Animated display
        }

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("🔒 SAFETY FEATURES:");
        Console.WriteLine("   • Atomic operations (all or nothing)");
        Console.WriteLine("   • Automatic rollback on any failure");
        Console.WriteLine("   • Transaction verification before completion");
        Console.WriteLine("   • No partial swaps possible");
        Console.WriteLine("   • Funds always protected");
    }

    static async Task ShowFullOASISInfo()
    {
        Console.WriteLine("\n📚 FULL OASIS UNIVERSAL TOKEN BRIDGE INFORMATION\n");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("🎯 PROJECT STATUS:");
        Console.WriteLine("   Core Infrastructure: ✅ 100% Complete (~800 lines, 8 files)");
        Console.WriteLine("   Solana Bridge:      ✅ 100% Complete (~330 lines)");
        Console.WriteLine("   Radix Bridge:       ⏳ 40% Complete (compilation issues)");
        Console.WriteLine("   Bridge Manager:     ✅ 100% Complete (~370 lines)");
        Console.WriteLine("   Documentation:      ✅ 100% Complete (5 files)");
        Console.WriteLine();
        Console.WriteLine("   Overall Progress:   📊 70% Complete");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("🔗 SUPPORTED CHAINS (Current & Planned):");
        Console.WriteLine();
        Console.WriteLine("   Currently Implemented:");
        Console.WriteLine("   ✅ Solana (SOL) - Full bridge support");
        Console.WriteLine();
        Console.WriteLine("   In Progress:");
        Console.WriteLine("   ⏳ Radix (XRD) - 40% complete, needs SDK fixes");
        Console.WriteLine();
        Console.WriteLine("   Easy to Add (EVM Chains - 6-8 hours each):");
        Console.WriteLine("   ❌ Ethereum (ETH)");
        Console.WriteLine("   ❌ Polygon (MATIC)");
        Console.WriteLine("   ❌ Arbitrum");
        Console.WriteLine("   ❌ Avalanche (AVAX)");
        Console.WriteLine("   ❌ Base");
        Console.WriteLine("   ❌ Optimism");
        Console.WriteLine("   ❌ BNB Chain");
        Console.WriteLine("   ❌ Fantom");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("📖 DOCUMENTATION:");
        Console.WriteLine("   • BRIDGE_MIGRATION_CONTEXT_FOR_AI.md");
        Console.WriteLine("   • BRIDGE_MIGRATION_COMPLETE_SUMMARY.md");
        Console.WriteLine("   • ADDING_BRIDGE_SUPPORT_TO_PROVIDERS.md");
        Console.WriteLine("   • BRIDGE_FILES_REFERENCE.md");
        Console.WriteLine("   • BRIDGE_MIGRATION_STATUS.md");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("🔮 NEXT STEPS:");
        Console.WriteLine("   1. Fix RadixOASIS compilation issues");
        Console.WriteLine("   2. Integrate real-time exchange rate API");
        Console.WriteLine("   3. Test SOL ↔ XRD swaps on testnet");
        Console.WriteLine("   4. Add Ethereum bridge support");
        Console.WriteLine("   5. Add database persistence (optional)");
        Console.WriteLine("   6. Deploy to mainnet");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        await Task.CompletedTask;
    }
}

