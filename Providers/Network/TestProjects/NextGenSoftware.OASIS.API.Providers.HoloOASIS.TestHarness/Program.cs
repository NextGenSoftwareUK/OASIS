using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Holo OASIS Provider Test Harness");
            Console.WriteLine("====================================");
            Console.WriteLine();

            try
            {
                // Initialize Holo provider
                var holoProvider = new HoloOASIS();
                
                Console.WriteLine("✅ Holo Provider initialized successfully");
                Console.WriteLine($"Provider Name: {holoProvider.ProviderName}");
                Console.WriteLine($"Provider Description: {holoProvider.ProviderDescription}");
                Console.WriteLine($"Provider Type: {holoProvider.ProviderType}");
                Console.WriteLine($"Provider Category: {holoProvider.ProviderCategory}");
                Console.WriteLine();

                // Test provider activation
                Console.WriteLine("🔄 Activating Holo provider...");
                var activationResult = await holoProvider.ActivateProviderAsync();
                
                if (activationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to activate Holo provider: {activationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Holo provider activated successfully: {activationResult.Message}");
                Console.WriteLine();

                // Test provider deactivation
                Console.WriteLine("🔄 Deactivating Holo provider...");
                var deactivationResult = await holoProvider.DeActivateProviderAsync();
                
                if (deactivationResult.IsError)
                {
                    Console.WriteLine($"❌ Failed to deactivate Holo provider: {deactivationResult.Message}");
                    return;
                }
                
                Console.WriteLine($"✅ Holo provider deactivated successfully: {deactivationResult.Message}");
                Console.WriteLine();

                Console.WriteLine("Holo OASIS Provider Test Harness completed successfully!");
                Console.WriteLine();

                // ── ONET over HoloNET mode ────────────────────────────────────────────────
                // When NetworkType = "HoloNET" in OASISDNA, ONETManager uses the HoloOASIS
                // provider's HoloNETClientAppAgent as its P2P backend instead of the custom
                // Kademlia/mDNS/TCP stack. This requires a live Holochain conductor to be
                // running (holochain --config-path holochain-config.yaml). If no conductor
                // is reachable the test will log a warning and skip cleanly — it will not crash.
                await RunONETHoloNETModeAsync(holoProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Holo Provider Test Harness: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static async Task RunONETHoloNETModeAsync(HoloOASIS holoProvider)
        {
            Console.WriteLine("== ONET HoloNET Mode Test ==");
            Console.WriteLine();

            var dna = new OASISDNA();
            dna.OASIS.ONET = new ONETConfig
            {
                NetworkType = "HoloNET",
                NodeId = "",
                NodePublicKey = "",
                NodePrivateKey = "",
                BootstrapServers = new System.Collections.Generic.List<string>(),
                TcpPort = 38470,
                EnableMDNS = false,
                AutoRegisterOnBootstrap = false
            };

            Console.WriteLine("Constructing ONETManager in HoloNET mode (requires live conductor)...");

            ONETManager manager;
            try
            {
                manager = new ONETManager(holoProvider, dna, P2PNetworkType.HoloNET);
                Console.WriteLine("ONETManager constructed successfully.");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Constructor rejected non-HoloOASIS provider: {ex.Message}");
                Console.WriteLine("This is expected when the provider isn't a HoloOASIS instance.");
                return;
            }

            Console.WriteLine("Running InitializeAsync (generates NodeId, registers with bootstrap servers)...");
            try
            {
                await manager.InitializeAsync();
                Console.WriteLine($"NodeId generated: {dna.OASIS.ONET.NodeId}");
                Console.WriteLine($"PublicKey (first 20 chars): {dna.OASIS.ONET.NodePublicKey?[..Math.Min(20, dna.OASIS.ONET.NodePublicKey?.Length ?? 0)]}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InitializeAsync failed (conductor likely not running): {ex.Message}");
            }

            Console.WriteLine("Starting ONET network in HoloNET mode...");
            try
            {
                var startResult = await manager.StartNetworkAsync();
                if (startResult.IsError)
                    Console.WriteLine($"StartNetworkAsync returned error: {startResult.Message}");
                else
                    Console.WriteLine("StartNetworkAsync succeeded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StartNetworkAsync threw (conductor not running): {ex.Message}");
            }

            Console.WriteLine("Getting network stats...");
            try
            {
                var stats = await manager.GetNetworkStatsAsync();
                if (!stats.IsError && stats.Result != null)
                    foreach (var kv in stats.Result)
                        Console.WriteLine($"  {kv.Key}: {kv.Value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetNetworkStatsAsync threw: {ex.Message}");
            }

            Console.WriteLine("Stopping network...");
            try { await manager.StopNetworkAsync(); } catch { }
            Console.WriteLine("ONET HoloNET mode test complete.");
            Console.WriteLine();
        }
    }
}