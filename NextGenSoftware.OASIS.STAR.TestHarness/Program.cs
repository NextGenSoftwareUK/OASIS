using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.OASISAPIManager;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.ParamObjects;

namespace NextGenSoftware.OASIS.STAR.TestHarness
{
    class Program
    {
        static Planet ourWorld;

        static async Task Main(string[] args)
        {
            var versionString = Assembly.GetEntryAssembly()
                                       .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                       .InformationalVersion
                                       .ToString();

            Console.WriteLine($"********************************************************************");
            Console.WriteLine($"NextGen Software STAR (Synergiser Transformer Aggregator Resolver) HDK/ODK TEST HARNESS v{versionString}");
            Console.WriteLine($"********************************************************************");
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  star beamin = Log in");
            Console.WriteLine("  star beamout = Log out");
            Console.WriteLine("  star light -dnaFolder -cSharpGeneisFolder -rustGenesisFolder = Creates a new Planet (OAPP) at the given folder genesis locations, from the given OAPP DNA.");
            Console.WriteLine("  star light -transmute -hAppDNA -cSharpGeneisFolder -rustGenesisFolder = Creates a new Planet (OAPP) at the given folder genesis locations, from the given hApp DNA.");
            Console.WriteLine("  star flare -planetName = Build a planet (OAPP).");
            Console.WriteLine("  star shine -planetName = Launch & activate a planet (OAPP) by shining the star's light upon it...");
            Console.WriteLine("  star dim -planetName = Deactivate a planet (OAPP).");
            Console.WriteLine("  star seed -planetName = Deploy a planet (OAPP).");
            Console.WriteLine("  star twinkle -planetName = Deactivate a planet (OAPP).");
            Console.WriteLine("  star dust -planetName = Delete a planet (OAPP).");
            Console.WriteLine("  star radiate -planetName = Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*");
            Console.WriteLine("  star emit -planetName = Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)");
            Console.WriteLine("  star reflect -planetName = Show stats of the Planet (OAPP).");
            Console.WriteLine("  star evolve -planetName = Upgrade/update a Planet (OAPP).");
            Console.WriteLine("  star mutate -planetName = Import/Export hApp, dApp & others.");
            Console.WriteLine("  star love -planetName = Send/Receive Love.");
            Console.WriteLine("  star burst = View network stats/management/settings.");
            Console.WriteLine("  star super - Reserved For Future Use...");
            Console.WriteLine($"********************************************************************");


            //string dnaFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\CelestialBodyDNA";
            //string cSharpGeneisFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\bin\Release\net5.0\Genesis\CSharp";
            //string rustGenesisFolder = @"C:\CODE\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.OASIS.STAR.TestHarness\bin\Release\net5.0\Genesis\Rust";

            string dnaFolder = "C:\\CODE\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.TestHarness\\CelestialBodyDNA";
            string cSharpGeneisFolder = "C:\\CODE\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.TestHarness\\bin\\Release\\net5.0\\Genesis\\CSharp";
            string rustGenesisFolder = "C:\\CODE\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.TestHarness\\bin\\Release\\net5.0\\Genesis\\Rust";


            // TODO: Not sure what events should expose on Star, StarCore and HoloNETClient?
            // I feel the events should at least be on the Star object, but then they need to be on the others to bubble them up (maybe could be hidden somehow?)
            SuperStar.OnZomeError += Star_OnZomeError;
            SuperStar.OnHolonLoaded += Star_OnHolonLoaded;
            SuperStar.OnHolonsLoaded += Star_OnHolonsLoaded;
            SuperStar.OnHolonSaved += Star_OnHolonSaved;
            SuperStar.OnInitialized += Star_OnInitialized;
            SuperStar.OnStarError += Star_OnStarError;

          //  Star.StarCore.OnZomeError += StarCore_OnZomeError;
          //  Star.StarCore.HoloNETClient.OnError += HoloNETClient_OnError;

            Console.WriteLine("Beaming In...");
            Console.WriteLine("");

            Avatar avatar = SuperStar.BeamIn("david@nextgensoftware.co.uk", "lettherebelight").Result;

            if (avatar != null)
            {
                Console.WriteLine(string.Concat("Successfully Beamed In! Welcome back ", avatar.Name, ". Have a nice day! :)"));
                Console.WriteLine(string.Concat("Karma: ", avatar.Karma));
                Console.WriteLine(string.Concat("Level: ", avatar.Level));
                Console.WriteLine("");

                // Create Planet (OAPP) by generating dynamic template/scaffolding code.
                Console.WriteLine("Generating Planet Our World...");
                CoronalEjection result = SuperStar.Light(GenesisType.Planet, "Our World", dnaFolder, cSharpGeneisFolder, rustGenesisFolder, "NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness.Genesis").Result;

                if (result.ErrorOccured)
                    Console.WriteLine(string.Concat("ERROR OCCURED: Error Message: ", result.Message));

                else
                {
                Console.WriteLine("Planet Our World Generated.");
                ourWorld = result.CelestialBody as Planet;

                ourWorld.OnHolonLoaded += OurWorld_OnHolonLoaded;
                ourWorld.OnHolonSaved += OurWorld_OnHolonSaved;
                ourWorld.OnZomeError += OurWorld_OnZomeError;

                ourWorld.LoadAll();
                //ourWorld.Zomes.Add()

                Holon newHolon = new Holon();
                newHolon.Name = "Test Data";
                newHolon.Description = "Test Desc";
                newHolon.HolonType = HolonType.Park;

                Console.WriteLine("Saving Holon...");

                // If you are using the generated code from Light above (highly recommended) you do not need to pass the HolonTypeName in, you only need to pass the holon in.
                //ourWorld.CelestialBodyCore.SaveHolonAsync("Test", newHolon);
                await ourWorld.CelestialBodyCore.SaveHolonAsync(newHolon);


                // BEGIN OASIS API DEMO ***********************************************************************************

                //Set auto-replicate for all providers except IPFS and Neo4j.
                ProviderManager.SetAutoReplicateForAllProviders(true);
                ProviderManager.SetAutoReplicationForProviders(false, new List<ProviderType>() { ProviderType.IPFSOASIS, ProviderType.Neo4jOASIS });

                //Set auto-failover for all providers except Holochain.
                ProviderManager.SetAutoFailOverForAllProviders(true);
                ProviderManager.SetAutoFailOverForProviders(false, new List<ProviderType>() { ProviderType.HoloOASIS });

                //Set auto-load balance for all providers except Ethereum.
                ProviderManager.SetAutoLoadBalanceForAllProviders(true);
                ProviderManager.SetAutoLoadBalanceForProviders(false, new List<ProviderType>() { ProviderType.EthereumOASIS });

                //  Set the default provider to MongoDB.
                ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.MongoDBOASIS, true); // Set last param to false if you wish only the next call to use this provider.

                //  Give HoloOASIS Store permission for the Name field(the field will only be stored on Holochain).
                OASISAPI.Avatar.Config.FieldToProviderMappings.Name.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

                // Give all providers read/write access to the Karma field (will allow them to read and write to the field but it will only be stored on Holochain).
                // You could choose to store it on more than one provider if you wanted the extra redundancy (but not normally needed since Holochain has a lot of redundancy built in).
                OASISAPI.Avatar.Config.FieldToProviderMappings.Karma.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.All });

                //Give Ethereum read-only access to the DOB field.
                OASISAPI.Avatar.Config.FieldToProviderMappings.DOB.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadOnly, Provider = ProviderType.EthereumOASIS });


                // All calls are load-balanced and have multiple redudancy/fail over for all supported OASIS Providers.
                OASISAPI.Avatar.LoadAllAvatars(); // Load-balanced across all providers.
                OASISAPI.Avatar.LoadAllAvatars(ProviderType.MongoDBOASIS); // Only loads from MongoDB.
                OASISAPI.Avatar.LoadAvatar(avatar.Id, ProviderType.HoloOASIS); // Only loads from Holochain.
                OASISAPI.Map.CreateAndDrawRouteOnMapBetweenHolons(newHolon, newHolon); // Load-balanced across all providers.

                OASISAPI.Data.LoadHolon(newHolon.Id); // Load-balanced across all providers.
                OASISAPI.Data.LoadHolon(newHolon.Id, HolonType.All, ProviderType.IPFSOASIS); // Only loads from IPFS.
                OASISAPI.Data.LoadAllHolons(HolonType.Moon, ProviderType.HoloOASIS); // Loads all moon (OAPPs) from Holochain.
                OASISAPI.Data.SaveHolon(newHolon); // Load-balanced across all providers.
                OASISAPI.Data.SaveHolon(newHolon, ProviderType.EthereumOASIS); //  Only saves to Etherum.

                OASISAPI.Data.LoadAllHolons(HolonType.All, ProviderType.Default); // Loads all parks from current default provider.
                OASISAPI.Data.LoadAllHolons(HolonType.Park, ProviderType.All); // Loads all parks from all providers (load-balanced/fail over).
                OASISAPI.Data.LoadAllHolons(HolonType.Park); // shorthand for above.
                OASISAPI.Data.LoadAllHolons(HolonType.Quest); //  Loads all quests from all providers.
                OASISAPI.Data.LoadAllHolons(HolonType.Restaurant); //  Loads all resaurants from all providers.

                // Holochain Support
                await OASISAPI.Providers.Holochain.HoloNETClient.CallZomeFunctionAsync(OASISAPI.Providers.Holochain.HoloNETClient.AgentPubKey, "our_world_core", "load_holons", null);

                // IPFS Support
                await OASISAPI.Providers.IPFS.IPFSEngine.FileSystem.ReadFileAsync("");
                await OASISAPI.Providers.IPFS.IPFSEngine.FileSystem.AddFileAsync("");
                await OASISAPI.Providers.IPFS.IPFSEngine.Swarm.PeersAsync();
                await OASISAPI.Providers.IPFS.IPFSEngine.KeyChainAsync();
                await OASISAPI.Providers.IPFS.IPFSEngine.Dns.ResolveAsync("test");
                await OASISAPI.Providers.IPFS.IPFSEngine.Dag.GetAsync(new Ipfs.Cid() { Hash = "" });
                await OASISAPI.Providers.IPFS.IPFSEngine.Dag.PutAsync(new Ipfs.Cid() { Hash = "" });

                // EOSIO Support
                OASISAPI.Providers.EOSIO.ChainAPI.GetTableRows("accounts", "accounts", "users", "true", 0, 0, 1, 3);
                OASISAPI.Providers.EOSIO.ChainAPI.GetBlock("block");
                OASISAPI.Providers.EOSIO.ChainAPI.GetAccount("test.account");
                OASISAPI.Providers.EOSIO.ChainAPI.GetCurrencyBalance("test.account", "", "");

                // Ethereum Support
                //  OASISAPI.Providers.Ethereum.Web3.


                // Graph DB Support
                await OASISAPI.Providers.Neo4j.GraphClient.Cypher.Merge("(a:Avatar { Id: avatar.Id })").OnCreate().Set("a = avatar").ExecuteWithoutResultsAsync(); //Insert/Update Avatar.
                Avatar newAvatar = OASISAPI.Providers.Neo4j.GraphClient.Cypher.Match("(p:Avatar {Username: {nameParam}})").WithParam("nameParam", "davidellams@hotmail.com").Return(p => p.As<Avatar>()).ResultsAsync.Result.Single(); //Load Avatar.

                // Document/Object DB Support
                OASISAPI.Providers.MongoDB.Database.MongoDB.ListCollectionNames();
                OASISAPI.Providers.MongoDB.Database.MongoDB.GetCollection<Avatar>("testCollection");

                // SEEDS Support
                string balance = OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("test.account");
                Account account = OASISAPI.Providers.SEEDS.EOSIOOASIS.GetEOSIOAccount("test.account");

                Console.WriteLine(string.Concat("Balance Before: ", balance));
                Console.WriteLine(string.Concat("Account.account_name: ", account.account_name));
                Console.WriteLine(string.Concat("Account.core_liquid_balance Before: ", account.core_liquid_balance));

                OASISAPI.Providers.SEEDS.PayWithSeedsUsingTelosAccount("test.account", "test.account2", 7, KarmaSourceType.API, "test", "test", "test", "test memo");
                balance = OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("test.account");
                account = OASISAPI.Providers.SEEDS.EOSIOOASIS.GetEOSIOAccount("test.account");

                Console.WriteLine(string.Concat("Balance After: ", balance));
                Console.WriteLine(string.Concat("Account.core_liquid_balance After: ", account.core_liquid_balance));

                string orgs = OASISAPI.Providers.SEEDS.GetAllOrganisationsAsJSON();
                Console.WriteLine(string.Concat("Organisations: ", orgs));

                string qrCode = OASISAPI.Providers.SEEDS.GenerateSignInQRCode("davidsellams");
                Console.WriteLine(string.Concat("SEEDS Sign-In QRCode: ", qrCode));

                SendInviteResult inviteResult = OASISAPI.Providers.SEEDS.SendInviteToJoinSeedsUsingTelosAccount("test.account", "test.account", 5, 5, KarmaSourceType.API, "test", "test", "test");
                Console.WriteLine(string.Concat("Invite Sent To Join SEEDS. Invite Secrert: ", inviteResult.InviteSecret, ". Transction ID: ", inviteResult.TransactionId));

                string transactionID = OASISAPI.Providers.SEEDS.AcceptInviteToJoinSeedsUsingTelosAccount("test.account2", inviteResult.InviteSecret, KarmaSourceType.API, "test", "test", "test");
                Console.WriteLine(string.Concat("Invite Accepted To Join SEEDS. Invite Secrert: ", inviteResult.InviteSecret, ". Transction ID: ", transactionID));

                // ThreeFold, AcivityPub, SOLID, Cross/Off Chain, Smart Contract Interoperability & lots more coming soon! :)

                // END OASIS API DEMO ***********************************************************************************


                // Build
                CoronalEjection ejection = ourWorld.Flare();
                //OR
                //CoronalEjection ejection = Star.Flare(ourWorld);

                // Activate & Launch - Launch & activate the planet (OAPP) by shining the star's light upon it...
                SuperStar.Shine(ourWorld);
                ourWorld.Shine();

                // Deactivate the planet (OAPP)
                SuperStar.Dim(ourWorld);

                // Deploy the planet (OAPP)
                SuperStar.Seed(ourWorld);

                // Run Tests
                SuperStar.Twinkle(ourWorld);

                // Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*
                SuperStar.Radiate(ourWorld);

                // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
                SuperStar.Emit(ourWorld);

                // Show stats of the Planet (OAPP).
                SuperStar.Reflect(ourWorld);

                // Upgrade/update a Planet (OAPP).
                SuperStar.Evolve(ourWorld);

                // Import/Export hApp, dApp & others.
                SuperStar.Mutate(ourWorld);

                // Send/Receive Love
                SuperStar.Love(ourWorld);

                // Show network stats/management/settings
                SuperStar.Burst(ourWorld);

                // Reserved For Future Use...
                SuperStar.Super(ourWorld);

                // Delete a planet (OAPP).
                SuperStar.Dust(ourWorld);
            }
        }
            else
                Console.WriteLine("Error Beaming In.");
        }

        private static void Star_OnStarError(object sender, StarErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("Star Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "EndPoint: ", e.EndPoint));
        }

        //private static void HoloNETClient_OnError(object sender, Client.Core.HoloNETErrorEventArgs e)
        //{
        //    Console.WriteLine(string.Concat("HoloNET Client Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "EndPoint: ", e.EndPoint));
        //}

        private static void StarCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("Star Core Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNETErrorDetails.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNETErrorDetails.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
        }

        private static void Star_OnInitialized(object sender, EventArgs e)
        {
            Console.WriteLine("Star Initialized.");
        }

        private static void Star_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            Console.WriteLine(string.Concat("Star Holons Saved. Holon Saved: ", e.Holon.Name));
        }

        private static void Star_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            Console.WriteLine(string.Concat("Star Holons Loaded. Holons Loaded: ", e.Holons.Count));
        }

        private static void Star_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            Console.WriteLine(string.Concat("Star Holons Loaded. Holon Name: ", e.Holon.Name));
        }

        private static void Star_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("Star Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNETErrorDetails.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNETErrorDetails.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
        }

        private static void OurWorld_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("Our World Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNETErrorDetails.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNETErrorDetails.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
        }

        private static void OurWorld_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            Console.WriteLine("Holon Saved");
            Console.WriteLine(string.Concat("Holon Id: ", e.Holon.Id));
            Console.WriteLine(string.Concat("Holon ProviderKey: ", e.Holon.ProviderKey));
            Console.WriteLine(string.Concat("Holon Name: ", e.Holon.Name));
            Console.WriteLine(string.Concat("Holon Type: ", e.Holon.HolonType));
            Console.WriteLine(string.Concat("Holon Description: ", e.Holon.Description));

            Console.WriteLine("Loading Holon...");
            //ourWorld.CelestialBodyCore.LoadHolonAsync(e.Holon.Name, e.Holon.ProviderKey);
            ourWorld.CelestialBodyCore.LoadHolonAsync(e.Holon.Id);
        }

        private static void OurWorld_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            Console.WriteLine("Holon Loaded");
            Console.WriteLine(string.Concat("Holon Id: ", e.Holon.Id));
            Console.WriteLine(string.Concat("Holon ProviderKey: ", e.Holon.ProviderKey));
            Console.WriteLine(string.Concat("Holon Name: ", e.Holon.Name));
            Console.WriteLine(string.Concat("Holon Type: ", e.Holon.HolonType));
            Console.WriteLine(string.Concat("Holon Description: ", e.Holon.Description));

            //Console.WriteLine(string.Concat("ourWorld.Zomes[0].Holons[0].ProviderKey: ", ourWorld.Zomes[0].Holons[0].ProviderKey));
            Console.WriteLine(string.Concat("ourWorld.Zomes[0].Holons[0].ProviderKey: ", ourWorld.CelestialBodyCore.Zomes[0].Holons[0].ProviderKey));
        }
    }
}
