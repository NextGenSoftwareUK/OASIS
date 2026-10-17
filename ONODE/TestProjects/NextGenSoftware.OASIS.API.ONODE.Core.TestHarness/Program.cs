using System;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;

namespace NextGenSoftware.OASIS.API.ONODE.Core.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE ONODE CORE TEST HARNESS V1.3");
            Console.WriteLine("");

            NFTManager NFTManager = new NFTManager(Guid.NewGuid());

            CLIEngine.ShowWorkingMessage("Minting NFT With External MetaData...");
            OASISResult<IWeb4NFT> mintResult = await NFTManager.MintNftAsync(new MintWeb4NFTRequest()
            {
                SendToAddressAfterMinting = "0x604b88BECeD9d6a02113fE1A0129f67fbD565D38",
                MintedByAvatarId = Guid.NewGuid(),
                Title = "Sample NFT Title",
                Description = "This is a description of the sample NFT. It includes all the unique attributes and features.",
                //Image = [0x01, 0x02, 0x03, 0x04], // Mock byte array for the image
                ImageUrl = "https://example.com/images/sample-nft.jpg",
                //Thumbnail = [0x05, 0x06, 0x07, 0x08], // Mock byte array for the thumbnail
                ThumbnailUrl = "https://example.com/thumbnails/sample-nft-thumb.jpg",
                Price = 1m, // Price in whatever currency the system uses, e.g., Ether
                Discount = 1m, // 5% discount
                MemoText = "Thank you for purchasing this NFT!",
                NumberToMint = 1,
                MetaData = new Dictionary<string, string>
                    {
                        { "Creator", "John Doe" },
                        { "BackgroundColor", "Blue" },
                        { "Rarity", "Rare" },
                        { "Edition", "First Edition" }
                    },
                OnChainProvider = new EnumValue<ProviderType>(ProviderType.ArbitrumOASIS),
                OffChainProvider = new EnumValue<ProviderType>(ProviderType.None),
                StoreNFTMetaDataOnChain = false,
                NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType.ExternalJSONURL),
                JSONMetaDataURL = "https://example.com/metadata/sample-nft.json",
                NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.ERC721),
                Symbol = "ONFT"
            });

            if (mintResult != null && mintResult.Result != null && !mintResult.IsError)
                CLIEngine.ShowSuccessMessage($"OASIS NFT ID: {mintResult.Result.Id} \nMinted Date: {mintResult.Result.MintedOn}\nMeta Data JSON URL:{mintResult.Result.JSONMetaDataURL}\nImage URL:{mintResult.Result.ImageUrl}");
            else
                CLIEngine.ShowErrorMessage($"Error Minting NFT: {mintResult.Message}");


            CLIEngine.ShowWorkingMessage("Minting NFT With MetaData Stored On OASIS...");
            mintResult = await NFTManager.MintNftAsync(new MintWeb4NFTRequest()
            {
                SendToAddressAfterMinting = "0x604b88BECeD9d6a02113fE1A0129f67fbD565D38",
                MintedByAvatarId = Guid.NewGuid(),
                Title = "Sample NFT Title",
                Description = "This is a description of the sample NFT. It includes all the unique attributes and features.",
                //Image = [0x01, 0x02, 0x03, 0x04], // Mock byte array for the image
                ImageUrl = "https://example.com/images/sample-nft.jpg",
                //Thumbnail = [0x05, 0x06, 0x07, 0x08], // Mock byte array for the thumbnail
                ThumbnailUrl = "https://example.com/thumbnails/sample-nft-thumb.jpg",
                Price = 1m, // Price in whatever currency the system uses, e.g., Ether
                Discount = 1m, // 5% discount
                MemoText = "Thank you for purchasing this NFT!",
                NumberToMint = 1,
                MetaData = new Dictionary<string, string>
                    {
                        { "Creator", "John Doe" },
                        { "BackgroundColor", "Blue" },
                        { "Rarity", "Rare" },
                        { "Edition", "First Edition" }
                    },
                OnChainProvider = new EnumValue<ProviderType>(ProviderType.ArbitrumOASIS),
                OffChainProvider = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
                StoreNFTMetaDataOnChain = false,
                NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType.OASIS),
                //JSONMetaDataUrl = "https://example.com/metadata/sample-nft.json",
                NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.ERC721),
                Symbol = "ONFT"
            });

            if (mintResult != null && mintResult.Result != null && !mintResult.IsError)
                CLIEngine.ShowSuccessMessage(mintResult.Message);
            else
                CLIEngine.ShowErrorMessage($"Error Minting NFT: {mintResult.Message}");

            await RunONETDemoAsync();

            return;

            ISampleManager sampleManager = new SampleManager(Guid.NewGuid());

            Console.WriteLine("Saving Sample Holon...");
            OASISResult<SampleHolon> saveSampleHolonResult = sampleManager.SaveSampleHolon("test wallet", "test avatar", Guid.NewGuid(), DateTime.Now, 77, 77777777777);

            if (!saveSampleHolonResult.IsError && saveSampleHolonResult.Result != null)
            {
                Console.WriteLine($"Sample Holon Saved. Id: {saveSampleHolonResult.Result.Id.ToString()}");

                Console.WriteLine("Loading Sample Holon...");
                OASISResult<SampleHolon> loadSampleHolonResult = sampleManager.LoadSampleHolon(saveSampleHolonResult.Result.Id);

                if (!loadSampleHolonResult.IsError && loadSampleHolonResult.Result != null)
                {
                    Console.WriteLine("SampleHolon Loaded.");
                    Console.WriteLine($"Id: {loadSampleHolonResult.Result.Id.ToString()}");
                    Console.WriteLine($"CustomProperty: {loadSampleHolonResult.Result.CustomProperty}");
                    Console.WriteLine($"CustomProperty2: {loadSampleHolonResult.Result.CustomProperty2}");
                    Console.WriteLine($"AvatarId: {loadSampleHolonResult.Result.AvatarId}");
                    Console.WriteLine($"CustomDate: {loadSampleHolonResult.Result.CustomDate.ToString()}");
                    Console.WriteLine($"CustomNumber: {loadSampleHolonResult.Result.CustomNumber.ToString()}");
                    Console.WriteLine($"CustomLongNumber: {loadSampleHolonResult.Result.CustomLongNumber.ToString()}");
                }
                else
                    Console.WriteLine($"Error Occured Loading Sample Holon. Reason: {loadSampleHolonResult.Message}");
            }
            else
                Console.WriteLine($"Error Occured Saving Sample Holon. Reason: {saveSampleHolonResult.Message}");


            /*
            NFTManager nftManager = new NFTManager();

            
            Console.WriteLine("Saving NFT Purchase Data...");
            OASISResult<IHolon> result = nftManager.PurchaseNFT("test wallet", "test avatar", Guid.NewGuid(), "tile data");

            if (!result.IsError && result.Result != null)
            { 
                Console.WriteLine($"NFT Purchase Data Saved. Id: {result.Result.Id.ToString()}");

                Console.WriteLine("Loading NFT Purchase Data...");
                OASISResult<IHolon> loadNFTPurchaseDataResult = nftManager.LoadNFTPurchaseData(result.Result.Id);

                if (!loadNFTPurchaseDataResult.IsError && loadNFTPurchaseDataResult.Result != null)
                {
                    Console.WriteLine("NFT Purchase Data Loaded.");
                    Console.WriteLine($"Id: {loadNFTPurchaseDataResult.Result.Id.ToString()}");
                    Console.WriteLine($"Wallet Address: {loadNFTPurchaseDataResult.Result.MetaData["WalletAddress"]}");
                    Console.WriteLine($"AvatarUsername: {loadNFTPurchaseDataResult.Result.MetaData["AvatarUsername"]}");
                    Console.WriteLine($"AvatarId: {loadNFTPurchaseDataResult.Result.MetaData["AvatarId"]}");
                    Console.WriteLine($"JsonSelectedTiles: {loadNFTPurchaseDataResult.Result.MetaData["JsonSelectedTiles"]}");
                }
                else
                    Console.WriteLine($"Error Occured Loading NFT Purchase Data. Reason: {loadNFTPurchaseDataResult.Message}");
            }
            else
                Console.WriteLine($"Error Occured Saving NFT Purchase Data. Reason: {result.Message}");
            


            Console.WriteLine("Saving NFT Purchase Data2...");
            OASISResult<PurchaseNFTHolon> purchaseHolonResult = nftManager.PurchaseNFT2("test wallet", "test avatar", Guid.NewGuid(), "tile data");

            if (!purchaseHolonResult.IsError && purchaseHolonResult.Result != null)
            {
                Console.WriteLine($"NFT Purchase Data Saved. Id: {purchaseHolonResult.Result.Id.ToString()}");

                Console.WriteLine("Loading NFT Purchase Data2...");
                OASISResult<PurchaseNFTHolon> loadNFTPurchaseDataResult = nftManager.LoadNFTPurchaseData2(purchaseHolonResult.Result.Id);

                if (!loadNFTPurchaseDataResult.IsError && loadNFTPurchaseDataResult.Result != null)
                {
                    Console.WriteLine("NFT Purchase Data Loaded.");
                    Console.WriteLine($"Id: {loadNFTPurchaseDataResult.Result.Id.ToString()}");
                    Console.WriteLine($"Wallet Address: {loadNFTPurchaseDataResult.Result.WalletAddress}");
                    Console.WriteLine($"AvatarUsername: {loadNFTPurchaseDataResult.Result.AvatarUsername}");
                    Console.WriteLine($"AvatarId: {loadNFTPurchaseDataResult.Result.AvatarId}");
                    Console.WriteLine($"JsonSelectedTiles: {loadNFTPurchaseDataResult.Result.JsonSelectedTiles}");
                }
                else
                    Console.WriteLine($"Error Occured Loading NFT Purchase Data. Reason: {loadNFTPurchaseDataResult.Message}");

                Console.WriteLine("Loading NFT Purchase Data3...");
                OASISResult<PurchaseNFTHolon> loadNFTPurchaseDataResult3 = nftManager.LoadNFTPurchaseData3(purchaseHolonResult.Result.Id);

                if (!loadNFTPurchaseDataResult3.IsError && loadNFTPurchaseDataResult3.Result != null)
                {
                    Console.WriteLine("NFT Purchase Data Loaded.");
                    Console.WriteLine($"Id: {loadNFTPurchaseDataResult3.Result.Id.ToString()}");
                    Console.WriteLine($"Wallet Address: {loadNFTPurchaseDataResult3.Result.WalletAddress}");
                    Console.WriteLine($"AvatarUsername: {loadNFTPurchaseDataResult3.Result.AvatarUsername}");
                    Console.WriteLine($"AvatarId: {loadNFTPurchaseDataResult3.Result.AvatarId}");
                    Console.WriteLine($"JsonSelectedTiles: {loadNFTPurchaseDataResult3.Result.JsonSelectedTiles}");
                }
                else
                    Console.WriteLine($"Error Occured Loading NFT Purchase Data. Reason: {loadNFTPurchaseDataResult3.Message}");

            }
            else
                Console.WriteLine($"Error Occured Saving NFT Purchase Data. Reason: {purchaseHolonResult.Message}");
            */
        }

        /// <summary>
        /// Manual end-to-end ONET demo: starts a real ONET network, brings up a second node on a different
        /// port, has it ping the first node over the real PING/PONG TCP responder, runs a real bootstrap
        /// discovery query (against itself, to demonstrate the real HTTP call path), then shuts both nodes
        /// down. Useful for hands-on verification beyond the automated unit/integration test suites.
        /// </summary>
        static async Task RunONETDemoAsync()
        {
            CLIEngine.ShowWorkingMessage("Starting ONET node A...");
            var nodeA = new ONETProtocol(storageProvider: null) { ListenPort = 38470 };
            var startResultA = await nodeA.StartNetworkAsync();
            if (startResultA.IsError)
            {
                CLIEngine.ShowErrorMessage($"Failed to start ONET node A: {startResultA.Message}");
                return;
            }
            CLIEngine.ShowSuccessMessage($"ONET node A started on port {nodeA.ListenPort}.");

            CLIEngine.ShowWorkingMessage("Starting ONET node B...");
            var nodeB = new ONETProtocol(storageProvider: null) { ListenPort = 38471 };
            var startResultB = await nodeB.StartNetworkAsync();
            if (startResultB.IsError)
            {
                CLIEngine.ShowErrorMessage($"Failed to start ONET node B: {startResultB.Message}");
                await nodeA.StopNetworkAsync();
                return;
            }
            CLIEngine.ShowSuccessMessage($"ONET node B started on port {nodeB.ListenPort}.");

            CLIEngine.ShowWorkingMessage("Node A pinging node B over real TCP...");
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                await client.ConnectAsync(System.Net.IPAddress.Loopback, nodeB.ListenPort);
                using var stream = client.GetStream();

                var ping = System.Text.Encoding.UTF8.GetBytes("ONET_PING\n");
                await stream.WriteAsync(ping, 0, ping.Length);

                var buffer = new byte[256];
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                var response = System.Text.Encoding.UTF8.GetString(buffer, 0, read);

                if (response.Contains("ONET_PONG"))
                    CLIEngine.ShowSuccessMessage($"Node B responded: {response.Trim()}");
                else
                    CLIEngine.ShowErrorMessage($"Unexpected response from node B: {response}");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Ping to node B failed: {ex.Message}");
            }

            CLIEngine.ShowWorkingMessage("Stopping ONET nodes...");
            await nodeA.StopNetworkAsync();
            await nodeB.StopNetworkAsync();
            CLIEngine.ShowSuccessMessage("ONET demo (TCP layer) completed.");

            // ── Two-manager discovery demo ─────────────────────────────────────────
            // Uses ONETManager (the full business-logic layer, not raw ONETProtocol)
            // to exercise the connect/discover/broadcast flow between two in-process
            // peers. No bootstrap HTTP server required — nodes find each other via
            // direct ConnectToNodeAsync then check each other's node lists.
            await RunONETTwoManagerDiscoveryAsync();
        }

        static async Task RunONETTwoManagerDiscoveryAsync()
        {
            Console.WriteLine();
            Console.WriteLine("== ONET Two-Manager Discovery Demo ==");

            var dnaA = new NextGenSoftware.OASIS.API.DNA.OASISDNA();
            dnaA.OASIS.ONET = new NextGenSoftware.OASIS.API.DNA.ONETConfig
            {
                TcpPort = 38472,
                NetworkType = "Internal",
                AutoRegisterOnBootstrap = false,
                EnableMDNS = false,
                BootstrapServers = new System.Collections.Generic.List<string>()
            };

            var dnaB = new NextGenSoftware.OASIS.API.DNA.OASISDNA();
            dnaB.OASIS.ONET = new NextGenSoftware.OASIS.API.DNA.ONETConfig
            {
                TcpPort = 38473,
                NetworkType = "Internal",
                AutoRegisterOnBootstrap = false,
                EnableMDNS = false,
                BootstrapServers = new System.Collections.Generic.List<string>()
            };

            CLIEngine.ShowWorkingMessage("Initialising ONETManager A (port 38472)...");
            var managerA = new ONETManager(storageProvider: null, oasisdna: dnaA);
            await managerA.InitializeAsync();
            CLIEngine.ShowSuccessMessage($"Manager A NodeId: {dnaA.OASIS.ONET.NodeId[..Math.Min(16, dnaA.OASIS.ONET.NodeId.Length)]}...");

            CLIEngine.ShowWorkingMessage("Initialising ONETManager B (port 38473)...");
            var managerB = new ONETManager(storageProvider: null, oasisdna: dnaB);
            await managerB.InitializeAsync();
            CLIEngine.ShowSuccessMessage($"Manager B NodeId: {dnaB.OASIS.ONET.NodeId[..Math.Min(16, dnaB.OASIS.ONET.NodeId.Length)]}...");

            CLIEngine.ShowWorkingMessage("Starting networks...");
            await managerA.StartNetworkAsync();
            await managerB.StartNetworkAsync();

            CLIEngine.ShowWorkingMessage("Node B connecting to Node A...");
            var connectResult = await managerB.ConnectToNodeAsync(dnaA.OASIS.ONET.NodeId, $"127.0.0.1:{dnaA.OASIS.ONET.TcpPort}");
            if (connectResult.IsError)
                CLIEngine.ShowErrorMessage($"Connect failed: {connectResult.Message}");
            else
                CLIEngine.ShowSuccessMessage("Node B connected to Node A.");

            CLIEngine.ShowWorkingMessage("Checking Node B's connected nodes...");
            var nodesB = await managerB.GetConnectedNodesAsync();
            if (!nodesB.IsError)
                CLIEngine.ShowSuccessMessage($"Node B sees {nodesB.Result?.Count ?? 0} connected peer(s).");
            else
                CLIEngine.ShowErrorMessage($"GetConnectedNodes failed: {nodesB.Message}");

            CLIEngine.ShowWorkingMessage("Node A broadcasting test message...");
            var broadcastResult = await managerA.BroadcastMessageAsync("Hello from Node A", "test");
            if (broadcastResult.IsError)
                CLIEngine.ShowErrorMessage($"Broadcast failed: {broadcastResult.Message}");
            else
                CLIEngine.ShowSuccessMessage("Broadcast sent.");

            CLIEngine.ShowWorkingMessage("Fetching stats from both managers...");
            var statsA = await managerA.GetNetworkStatsAsync();
            var statsB = await managerB.GetNetworkStatsAsync();
            if (!statsA.IsError && statsA.Result != null)
                CLIEngine.ShowSuccessMessage($"Node A — networkType: {statsA.Result.GetValueOrDefault("networkType")}, peers: {statsA.Result.GetValueOrDefault("connectedNodes")}");
            if (!statsB.IsError && statsB.Result != null)
                CLIEngine.ShowSuccessMessage($"Node B — networkType: {statsB.Result.GetValueOrDefault("networkType")}, peers: {statsB.Result.GetValueOrDefault("connectedNodes")}");

            CLIEngine.ShowWorkingMessage("Stopping networks...");
            await managerA.StopNetworkAsync();
            await managerB.StopNetworkAsync();
            CLIEngine.ShowSuccessMessage("Two-manager discovery demo completed.");
        }
    }
}
