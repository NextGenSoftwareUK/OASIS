using System;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using EOSNewYork.EOSCore.Response.API;
using Nethereum.Contracts;
using Console = System.Console;
//using Spectre.Console;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.Membranes;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.Zomes;

namespace NextGenSoftware.OASIS.STAR.TestHarness
{
    class Program
    {
        private const string dnaFolder = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.TestHarness\\CelestialBodyDNA";
        private const string cSharpGeneisFolder = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.TestHarness\\bin\\Release\\net6.0\\Genesis\\CSharp";
        private const string rustGenesisFolder = "C:\\Users\\david\\source\\repos\\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\\NextGenSoftware.OASIS.STAR.TestHarness\\bin\\Release\\net6.0\\Genesis\\Rust";
        private static Planet _superWorld;
        private static Moon _jlaMoon;
        private static Spinner _spinner = new Spinner();
        private static string _privateKey = ""; //Set to privatekey when testing BUT remember to remove again before checking in code! Better to use avatar methods so private key is retreived from avatar and then no need to pass them in.
        
        static async Task Main(string[] args)
        {
            try
            {
                ShowHeader();
                ShowMessage("", false);

                // TODO: Not sure what events should expose on Star, StarCore and HoloNETClient?
                // I feel the events should at least be on the Star object, but then they need to be on the others to bubble them up (maybe could be hidden somehow?)
                STAR.OnCelestialSpaceLoaded += STAR_OnCelestialSpaceLoaded;
                STAR.OnCelestialSpaceSaved += STAR_OnCelestialSpaceSaved;
                STAR.OnCelestialSpaceError += STAR_OnCelestialSpaceError;
                STAR.OnCelestialSpacesLoaded += STAR_OnCelestialSpacesLoaded;
                STAR.OnCelestialSpacesSaved += STAR_OnCelestialSpacesSaved;
                STAR.OnCelestialSpacesError += STAR_OnCelestialSpacesError;
                STAR.OnCelestialBodyLoaded += STAR_OnCelestialBodyLoaded;
                STAR.OnCelestialBodySaved += STAR_OnCelestialBodySaved;
                STAR.OnCelestialBodyError += STAR_OnCelestialBodyError;
                STAR.OnCelestialBodiesLoaded += STAR_OnCelestialBodiesLoaded;
                STAR.OnCelestialBodiesSaved += STAR_OnCelestialBodiesSaved;
                STAR.OnCelestialBodiesError += STAR_OnCelestialBodiesError;
                STAR.OnZomeLoaded += STAR_OnZomeLoaded;
                STAR.OnZomeSaved += STAR_OnZomeSaved;
                STAR.OnZomeError += STAR_OnZomeError;
                STAR.OnZomesLoaded += STAR_OnZomesLoaded;
                STAR.OnZomesSaved += STAR_OnZomesSaved;
                STAR.OnZomesError += STAR_OnZomesError;
                STAR.OnHolonLoaded += STAR_OnHolonLoaded;
                STAR.OnHolonSaved += STAR_OnHolonSaved;
                STAR.OnHolonError += STAR_OnHolonError;
                STAR.OnHolonsLoaded += STAR_OnHolonsLoaded;
                STAR.OnHolonsSaved += STAR_OnHolonsSaved;
                STAR.OnHolonsError += STAR_OnHolonsError;
                STAR.OnStarIgnited += STAR_OnStarIgnited;
                STAR.OnStarError += STAR_OnStarError;
                STAR.OnStarStatusChanged += STAR_OnStarStatusChanged;
                STAR.OnOASISBooted += STAR_OnOASISBooted;
                STAR.OnOASISBootError += STAR_OnOASISBootError;

                OASISResult<IOmiverse> result = STAR.IgniteStar();

                if (result.IsError)
                    ShowErrorMessage(string.Concat("Error Igniting STAR. Error Message: ", result.Message));
                else
                {
                    //Console.ForegroundColor = ConsoleColor.Yellow;

                    if (!GetConfirmation("Do you have an existing avatar? "))
                        CreateAvatar();
                    else
                        ShowMessage("", false);

                    LoginAvatar();

                    ShowMessage("", false);
                    Colorful.Console.WriteAscii(" READY PLAYER ONE?", Color.Green);
                    ShowMessage("", false);

                    //TODO: TEMP - REMOVE AFTER TESTING! :)
                    await Test(dnaFolder, cSharpGeneisFolder, rustGenesisFolder);

                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                ShowErrorMessage(string.Concat("An unknown error has occured. Error Details: ", ex.ToString()));
                //AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            }
        }

        private static async Task Test(string dnaFolder, string cSharpGeneisFolder, string rustGenesisFolder)
        {
            //Passing in null for the ParentCelestialBody will default it to the default planet (Our World).
            OASISResult<CoronalEjection> result = await GenerateCelestialBody("The Justice League Accademy", null, GenesisType.Moon, dnaFolder, cSharpGeneisFolder, rustGenesisFolder, "NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness.Genesis");
            
            if (result != null && !result.IsError && result.Result != null && result.Result.CelestialBody != null)
                _jlaMoon = (Moon)result.Result.CelestialBody;

            //Passing in null for the ParentCelestialBody will default it to the default Star (Our Sun Sol).
            result = await GenerateCelestialBody("Super World", null, GenesisType.Planet, dnaFolder, cSharpGeneisFolder, rustGenesisFolder, "NextGenSoftware.Holochain.HoloNET.HDK.Core.TestHarness.Genesis");

            if (result != null && !result.IsError && result.Result != null && result.Result.CelestialBody != null)
            {
                _superWorld = (Planet)result.Result.CelestialBody;

                result.Result.CelestialBody.OnHolonLoaded += CelestialBody_OnHolonLoaded;
                result.Result.CelestialBody.OnHolonSaved += CelestialBody_OnHolonSaved;
                result.Result.CelestialBody.OnZomeError += CelestialBody_OnZomeError;

                ShowWorkingMessage("Loading Zomes & Holons...");
                OASISResult<IEnumerable<IZome>> zomesResult = await result.Result.CelestialBody.LoadZomesAsync();

                bool finished = false;
                if (zomesResult != null && !zomesResult.IsError && zomesResult.Result != null)
                {
                    if (zomesResult.Result.Count() > 0)
                    {
                        ShowSuccessMessage("Zomes & Holons Loaded Successfully.");
                        ShowZomesAndHolons(zomesResult.Result);
                    }
                    else
                        ShowSuccessMessage("No Zomes Found.");

                    finished = true;
                }
                else
                {
                    ShowErrorMessage($"An Error Occured Loading Zomes/Holons. Reason: {zomesResult.Message}");
                    finished = true;
                }

                //_spinner.Stop();

                while (!finished) { }

                Holon newHolon = new Holon();
                newHolon.Name = "Test Data";
                newHolon.Description = "Test Desc";
                newHolon.HolonType = HolonType.Park;

                ShowWorkingMessage("Saving Test Holon...");
                OASISResult<IHolon> holonResult =  await result.Result.CelestialBody.CelestialBodyCore.SaveHolonAsync(newHolon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    ShowSuccessMessage("Test Holon Saved Successfully.");
                    ShowSuccessMessage($"Id: {newHolon.Id}");
                    ShowSuccessMessage($"Created By Avatar Id: {newHolon.CreatedByAvatarId}");
                    ShowSuccessMessage($"Created Date: {newHolon.CreatedDate}");
                }
                else
                    ShowErrorMessage($"Error Saving Test Holon. Reason: {holonResult.Message}");

                
                await InitiateOASISAPTests(newHolon);
                

                // Build
                CoronalEjection ejection = result.Result.CelestialBody.Flare();
                //OR
                //CoronalEjection ejection = Star.Flare(ourWorld);

                // Activate & Launch - Launch & activate the planet (OAPP) by shining the star's light upon it...
                STAR.Shine(result.Result.CelestialBody);
                result.Result.CelestialBody.Shine();

                // Deactivate the planet (OAPP)
                STAR.Dim(result.Result.CelestialBody);

                // Deploy the planet (OAPP)
                STAR.Seed(result.Result.CelestialBody);

                // Run Tests
                STAR.Twinkle(result.Result.CelestialBody);

                // Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*
                STAR.Radiate(result.Result.CelestialBody);

                // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
                STAR.Emit(result.Result.CelestialBody);

                // Show stats of the Planet (OAPP).
                STAR.Reflect(result.Result.CelestialBody);

                // Upgrade/update a Planet (OAPP).
                STAR.Evolve(result.Result.CelestialBody);

                // Import/Export hApp, dApp & others.
                STAR.Mutate(result.Result.CelestialBody);

                // Send/Receive Love
                STAR.Love(result.Result.CelestialBody);

                // Show network stats/management/settings
                STAR.Burst(result.Result.CelestialBody);

                // Reserved For Future Use...
                STAR.Super(result.Result.CelestialBody);

                // Delete a planet (OAPP).
                STAR.Dust(result.Result.CelestialBody);
            }
        }

        private static async Task InitiateOASISAPTests(IHolon newHolon)
        {
            // BEGIN OASIS API DEMO ***********************************************************************************
            ShowMessage("BEGINNING OASIS API TEST'S...");

            //Set auto-replicate for all providers except IPFS and Neo4j.
            //EnableOrDisableAutoProviderList(ProviderManager.SetAutoReplicateForAllProviders, true, null, "Enabling Auto-Replication For All Providers...", "Auto-Replication Successfully Enabled For All Providers.", "Error Occured Enabling Auto-Replication For All Providers.");
            ShowWorkingMessage("Enabling Auto-Replication For All Providers...");
            bool isSuccess = ProviderManager.SetAutoReplicateForAllProviders(true);
            HandleBooleansResponse(isSuccess, "Auto-Replication Successfully Enabled For All Providers.", "Error Occured Enabling Auto-Replication For All Providers.");

            ShowWorkingMessage("Disabling Auto-Replication For IPFSOASIS & Neo4jOASIS Providers...");
            isSuccess = ProviderManager.SetAutoReplicationForProviders(false, new List<ProviderType>() { ProviderType.IPFSOASIS, ProviderType.Neo4jOASIS });
            //EnableOrDisableAutoProviderList(ProviderManager.SetAutoReplicationForProviders, false, new List<ProviderType>() { ProviderType.IPFSOASIS, ProviderType.Neo4jOASIS }, "Enabling Auto-Replication For All Providers...", "Auto-Replication Successfully Enabled For All Providers.", "Error Occured Enabling Auto-Replication For All Providers.");
            HandleBooleansResponse(isSuccess, "Auto-Replication Successfully Disabled For IPFSOASIS & Neo4jOASIS Providers.", "Error Occured Disabling Auto-Replication For IPFSOASIS & Neo4jOASIS Providers.");

            //Set auto-failover for all providers except Holochain.
            ShowWorkingMessage("Enabling Auto-FailOver For All Providers...");
            isSuccess = ProviderManager.SetAutoFailOverForAllProviders(true);
            HandleBooleansResponse(isSuccess, "Auto-FailOver Successfully Enabled For All Providers.", "Error Occured Enabling Auto-FailOver For All Providers.");

            ShowWorkingMessage("Disabling Auto-FailOver For HoloOASIS Provider...");
            isSuccess = ProviderManager.SetAutoFailOverForProviders(false, new List<ProviderType>() { ProviderType.HoloOASIS });
            HandleBooleansResponse(isSuccess, "Auto-FailOver Successfully Disabled For HoloOASIS.", "Error Occured Disabling Auto-FailOver For HoloOASIS Provider.");

            //Set auto-load balance for all providers except Ethereum.
            ShowWorkingMessage("Enabling Auto-Load-Balancing For All Providers...");
            isSuccess = ProviderManager.SetAutoLoadBalanceForAllProviders(true);
            HandleBooleansResponse(isSuccess, "Auto-FailOver Successfully Disabled For HoloOASIS.", "Error Occured Disabling Auto-FailOver For HoloOASIS Provider.");

            ShowWorkingMessage("Disabling Auto-Load-Balancing For EthereumOASIS Provider...");
            isSuccess = ProviderManager.SetAutoLoadBalanceForProviders(false, new List<ProviderType>() { ProviderType.EthereumOASIS });
            HandleBooleansResponse(isSuccess, "Auto-Load-Balancing Successfully Disabled For EthereumOASIS.", "Error Occured Disabling Auto-Load-Balancing For EthereumOASIS Provider.");

            // Set the default provider to MongoDB.
            // Set last param to false if you wish only the next call to use this provider.
            ShowWorkingMessage("Setting Default Provider to MongoDBOASIS...");
            HandleOASISResponse(ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.MongoDBOASIS, true), "Successfully Set Default Provider To MongoDBOASIS Provider.", "Error Occured Setting Default Provider To MongoDBOASIS.");

            //  Give HoloOASIS Store permission for the Name field(the field will only be stored on Holochain).
            ShowWorkingMessage("Granting HoloOASIS Provider Store Permission For The Name Field...");
            STAR.OASISAPI.Avatar.Config.FieldToProviderMappings.Name.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });
            ShowSuccessMessage("Permission Granted.");

            // Give all providers read/write access to the Karma field (will allow them to read and write to the field but it will only be stored on Holochain).
            // You could choose to store it on more than one provider if you wanted the extra redundancy (but not normally needed since Holochain has a lot of redundancy built in).
            ShowWorkingMessage("Granting All Providers Read/Write Permission For The Karma Field...");
            STAR.OASISAPI.Avatar.Config.FieldToProviderMappings.Karma.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.All });
            ShowSuccessMessage("Permission Granted.");

            //Give Ethereum read-only access to the DOB field.
            ShowWorkingMessage("Granting EthereumOASIS Providers Read-Only Permission For The DOB Field...");
            STAR.OASISAPI.Avatar.Config.FieldToProviderMappings.DOB.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadOnly, Provider = ProviderType.EthereumOASIS });
            ShowSuccessMessage("Permission Granted.");

            // All calls are load-balanced and have multiple redudancy/fail over for all supported OASIS Providers.
            ShowWorkingMessage("Loading All Avatars Load Balanced Across All Providers...");
            IEnumerable<IAvatar> avatars = STAR.OASISAPI.Avatar.LoadAllAvatars(); // Load-balanced across all providers.
            ShowSuccessMessage($"{avatars.Count()} Avatars Loaded.");

            ShowWorkingMessage("Loading All Avatars Only For The MongoDBOASIS Provider...");
            avatars = STAR.OASISAPI.Avatar.LoadAllAvatars(ProviderType.MongoDBOASIS); // Only loads from MongoDB.
            ShowSuccessMessage($"{avatars.Count()} Avatars Loaded.");

            ShowWorkingMessage("Loading Avatar Only For The HoloOASIS Provider...");
            OASISResult<IAvatar> avatarResult = STAR.OASISAPI.Avatar.LoadAvatar(STAR.LoggedInAvatar.Id, ProviderType.HoloOASIS); // Only loads from Holochain.

            if (!avatarResult.IsError && avatarResult.Result != null) 
            {
                ShowSuccessMessage("Avatar Loaded Successfully");
                ShowSuccessMessage($"Avatar ID: {avatarResult.Result.Id}");
                ShowSuccessMessage($"Avatar Name: {avatarResult.Result.FullName}");
                ShowSuccessMessage($"Avatar Created Date: {avatarResult.Result.CreatedDate}");
                ShowSuccessMessage($"Avatar Last Beamed In Date: {avatarResult.Result.LastBeamedIn}");
            }
            else
                ShowErrorMessage("Error Loading Avatar.");

            ShowWorkingMessage("Creating & Drawing Route On Map Between 2 Test Holons (Load Balanced Across All Providers)...");
            HandleBooleansResponse(STAR.OASISAPI.Map.CreateAndDrawRouteOnMapBetweenHolons(newHolon, newHolon), "Route Created Successfully.", "Error Creating Route."); // Load-balanced across all providers.

            ShowWorkingMessage("Loading Test Holon (Load Balanced Across All Providers)...");
            OASISResult<IHolon> holonResult = STAR.OASISAPI.Data.LoadHolon(newHolon.Id); // Load-balanced across all providers.

            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
            {
                ShowSuccessMessage("Holon Loaded Successfully.");
                ShowSuccessMessage($"Id: {holonResult.Result.Id}");
                ShowSuccessMessage($"Name: {holonResult.Result.Name}");
                ShowSuccessMessage($"Description: {holonResult.Result.Description}");
            }
            else
                ShowErrorMessage("Error Loading Holon");

            ShowWorkingMessage("Loading Test Holon Only For IPFSOASIS Provider...");
            holonResult = STAR.OASISAPI.Data.LoadHolon(newHolon.Id, true, true, 0, true, 0, ProviderType.IPFSOASIS); // Only loads from IPFS.

            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
            {
                ShowSuccessMessage("Holon Loaded Successfully.");
                ShowSuccessMessage($"Id: {holonResult.Result.Id}");
                ShowSuccessMessage($"Name: {holonResult.Result.Name}");
                ShowSuccessMessage($"Description: {holonResult.Result.Description}");
            }
            else
                ShowErrorMessage("Error Loading Holon");

            ShowWorkingMessage("Loading All Holons Of Type Moon Only For HoloOASIS Provider...");
            HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.Moon, true, true, 0, true, 0, ProviderType.HoloOASIS)); // Loads all moon (OAPPs) from Holochain.

            ShowWorkingMessage("Saving Test Holon (Load Balanced Across All Providers)...");
            HandleOASISResponse(STAR.OASISAPI.Data.SaveHolon(newHolon), "Holon Saved Successfully.", "Error Saving Holon."); // Load-balanced across all providers.

            ShowWorkingMessage("Saving Test Holon Only For The EthereumOASIS Provider...");
            HandleOASISResponse(STAR.OASISAPI.Data.SaveHolon(newHolon, true, true, 0, true, ProviderType.EthereumOASIS), "Holon Saved Successfully.", "Error Saving Holon."); //  Only saves to Etherum.

            ShowWorkingMessage("Loading All Holons From The Current Default Provider (With Auto-FailOver)...");
            HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.All, true, true, 0, true, 0, ProviderType.Default)); // Loads all holons from current default provider.

            ShowWorkingMessage("Loading All Park Holons From All Providers (With Auto-Load-Balance & Auto-FailOver)...");
            HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.Park, true, true, 0, true, 0, ProviderType.All)); // Loads all parks from all providers (load-balanced/fail over).

            //ShowWorkingMessage("Loading All Park Holons From All Providers (With Auto-Load-Balance & Auto-FailOver)...");
            STAR.OASISAPI.Data.LoadAllHolons(HolonType.Park); // shorthand for above.

            ShowWorkingMessage("Loading All Quest Holons From All Providers (With Auto-Load-Balance & Auto-FailOver)...");
            HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.Quest)); //  Loads all quests from all providers.

            ShowWorkingMessage("Loading All Restaurant Holons From All Providers (With Auto-Load-Balance & Auto-FailOver)...");
            HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.Restaurant)); //  Loads all resaurants from all providers.

            // Holochain Support
            //TODO: Sort this out soon! ;-)
            ShowWorkingMessage("Loading All Restaurant Holons From All Providers (With Auto-Load-Balance & Auto-FailOver)...");
            await STAR.OASISAPI.Providers.Holochain.HoloNETClient.CallZomeFunctionAsync(STAR.OASISAPI.Providers.Holochain.HoloNETClient.Config.AgentPubKey, "our_world_core", "load_holons", null);

            // IPFS Support
            ShowWorkingMessage("Initiating IPFS Tests...");
            await STAR.OASISAPI.Providers.IPFS.IPFSEngine.FileSystem.ReadFileAsync("");
            await STAR.OASISAPI.Providers.IPFS.IPFSEngine.FileSystem.AddFileAsync("");
            await STAR.OASISAPI.Providers.IPFS.IPFSEngine.Swarm.PeersAsync();
            await STAR.OASISAPI.Providers.IPFS.IPFSEngine.KeyChainAsync();
            await STAR.OASISAPI.Providers.IPFS.IPFSEngine.Dns.ResolveAsync("test");
            await STAR.OASISAPI.Providers.IPFS.IPFSEngine.Dag.GetAsync(new Ipfs.Cid() { Hash = "" });
            await STAR.OASISAPI.Providers.IPFS.IPFSEngine.Dag.PutAsync(new Ipfs.Cid() { Hash = "" });

            // Ethereum Support
            ShowWorkingMessage("Initiating Ethereum Tests...");
            await STAR.OASISAPI.Providers.Ethereum.Web3.Client.SendRequestAsync(new Nethereum.JsonRpc.Client.RpcRequest("id", "test"));
            await STAR.OASISAPI.Providers.Ethereum.Web3.Eth.Blocks.GetBlockNumber.SendRequestAsync("");
            Contract contract = STAR.OASISAPI.Providers.Ethereum.Web3.Eth.GetContract("abi", "contractAddress");

            // EOSIO Support
            ShowWorkingMessage("Initiating EOSIO Tests...");
            STAR.OASISAPI.Providers.EOSIO.ChainAPI.GetTableRows("accounts", "accounts", "users", "true", 0, 0, 1, 3);
            STAR.OASISAPI.Providers.EOSIO.ChainAPI.GetBlock("block");
            STAR.OASISAPI.Providers.EOSIO.ChainAPI.GetAccount("test.account");
            STAR.OASISAPI.Providers.EOSIO.ChainAPI.GetCurrencyBalance("test.account", "", "");

            // Graph DB Support
            ShowWorkingMessage("Initiating Neo4j Tests...");
            ShowWorkingMessage("Executing Graph Cypher Test...");
            await STAR.OASISAPI.Providers.Neo4j.GraphClient.Cypher.Merge("(a:Avatar { Id: avatar.Id })").OnCreate().Set("a = avatar").ExecuteWithoutResultsAsync(); //Insert/Update Avatar.
            Avatar newAvatar = STAR.OASISAPI.Providers.Neo4j.GraphClient.Cypher.Match("(p:Avatar {Username: {nameParam}})").WithParam("nameParam", "davidellams@hotmail.com").Return(p => p.As<Avatar>()).ResultsAsync.Result.Single(); //Load Avatar.

            // Document/Object DB Support
            ShowWorkingMessage("Initiating MongoDB Tests...");
            STAR.OASISAPI.Providers.MongoDB.Database.MongoDB.ListCollectionNames();
            STAR.OASISAPI.Providers.MongoDB.Database.MongoDB.GetCollection<Avatar>("Avatar");

            // SEEDS Support
            ShowWorkingMessage("Initiating SEEDS Tests...");
            ShowWorkingMessage("Getting Balance for account davidsellams...");
            string balance = STAR.OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("davidsellams");
            ShowSuccessMessage(string.Concat("Balance: ", balance));

            ShowWorkingMessage("Getting Balance for account nextgenworld...");
            balance = STAR.OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("nextgenworld");
            ShowSuccessMessage(string.Concat("Balance: ", balance));

            ShowWorkingMessage("Getting Account for account davidsellams...");
            Account account = STAR.OASISAPI.Providers.SEEDS.TelosOASIS.GetTelosAccount("davidsellams");
            ShowSuccessMessage(string.Concat("Account.account_name: ", account.account_name));
            ShowSuccessMessage(string.Concat("Account.created: ", account.created_datetime.ToString()));

            ShowWorkingMessage("Getting Account for account nextgenworld...");
            account = STAR.OASISAPI.Providers.SEEDS.TelosOASIS.GetTelosAccount("nextgenworld");
            ShowSuccessMessage(string.Concat("Account.account_name: ", account.account_name));
            ShowSuccessMessage(string.Concat("Account.created: ", account.created_datetime.ToString()));

            // Check that the Telos account name is linked to the avatar and link it if it is not (PayWithSeeds will fail if it is not linked when it tries to add the karma points).
            if (!STAR.LoggedInAvatar.ProviderUniqueStorageKey.ContainsKey(ProviderType.TelosOASIS))
            {
                ShowWorkingMessage("Linking Telos Account to Avatar...");
                OASISResult<bool> result = STAR.OASISAPI.Keys.LinkProviderPublicKeyToAvatar(STAR.LoggedInAvatar.Id, ProviderType.TelosOASIS, "davidsellams");

                if (!result.IsError && result.Result)
                    ShowSuccessMessage("Telos Account Successfully Linked to Avatar.");
                else
                    ShowErrorMessage("Error occured Whilst Linking Telos Account To Avatar.");
            }

            ShowWorkingMessage("Sending SEEDS from nextgenworld to davidsellams...");
            OASISResult<string> payWithSeedsResult = STAR.OASISAPI.Providers.SEEDS.PayWithSeedsUsingTelosAccount("davidsellams", _privateKey, "nextgenworld", 1, KarmaSourceType.API, "test", "test", "test", "test memo");
            ShowSuccessMessage(string.Concat("Success: ", payWithSeedsResult.IsError ? "false" : "true"));

            if (payWithSeedsResult.IsError)
                ShowErrorMessage(string.Concat("Error Message: ", payWithSeedsResult.Message));

            ShowSuccessMessage(string.Concat("Result: ", payWithSeedsResult.Result));

            ShowWorkingMessage("Getting Balance for account davidsellams...");
            balance = STAR.OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("davidsellams");
            ShowSuccessMessage(string.Concat("Balance: ", balance));

            ShowWorkingMessage("Getting Balance for account nextgenworld...");
            balance = STAR.OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("nextgenworld");
            ShowSuccessMessage(string.Concat("Balance: ", balance));

            ShowWorkingMessage("Getting Organsiations...");
            string orgs = STAR.OASISAPI.Providers.SEEDS.GetAllOrganisationsAsJSON();
            ShowSuccessMessage(string.Concat("Organisations: ", orgs));

            //ShowMessage("Getting nextgenworld organsiation...");
            //string org = OASISAPI.Providers.SEEDS.GetOrganisation("nextgenworld");
            //ShowMessage(string.Concat("nextgenworld org: ", org));

            ShowWorkingMessage("Generating QR Code for davidsellams...");
            string qrCode = STAR.OASISAPI.Providers.SEEDS.GenerateSignInQRCode("davidsellams");
            ShowSuccessMessage(string.Concat("SEEDS Sign-In QRCode: ", qrCode));

            ShowWorkingMessage("Sending invite to davidsellams...");
            OASISResult<SendInviteResult> sendInviteResult = STAR.OASISAPI.Providers.SEEDS.SendInviteToJoinSeedsUsingTelosAccount("davidsellams", _privateKey, "davidsellams", 1, 1, KarmaSourceType.API, "test", "test", "test");
            ShowSuccessMessage(string.Concat("Success: ", sendInviteResult.IsError ? "false" : "true"));

            if (sendInviteResult.IsError)
                ShowErrorMessage(string.Concat("Error Message: ", sendInviteResult.Message));
            else
            {
                ShowSuccessMessage(string.Concat("Invite Sent To Join SEEDS. Invite Secret: ", sendInviteResult.Result.InviteSecret, ". Transction ID: ", sendInviteResult.Result.TransactionId));

                ShowWorkingMessage("Accepting invite to davidsellams...");
                OASISResult<string> acceptInviteResult = STAR.OASISAPI.Providers.SEEDS.AcceptInviteToJoinSeedsUsingTelosAccount("davidsellams", sendInviteResult.Result.InviteSecret, KarmaSourceType.API, "test", "test", "test");
                ShowSuccessMessage(string.Concat("Success: ", acceptInviteResult.IsError ? "false" : "true"));

                if (acceptInviteResult.IsError)
                    ShowErrorMessage(string.Concat("Error Message: ", acceptInviteResult.Message));
                else
                    ShowSuccessMessage(string.Concat("Invite Accepted To Join SEEDS. Transction ID: ", acceptInviteResult.Result));
            }
            // ThreeFold, AcivityPub, SOLID, Cross/Off Chain, Smart Contract Interoperability & lots more coming soon! :)

            ShowMessage("OASIS API TESTS COMPLETE.");
            // END OASIS API DEMO ***********************************************************************************
        }


        private static void ShowAvatars(IEnumerable<IAvatar> avatars)
        {
            foreach (IAvatar avatar in avatars)
                ShowAvatar(avatar);
        }

        private static void ShowAvatar(IAvatar avatar)
        {
            if (avatar != null)
            {
                ShowSuccessMessage("Avatar Loaded Successfully");
                ShowSuccessMessage($"Avatar ID: {avatar.Id}");
                ShowSuccessMessage($"Avatar Name: {avatar.FullName}");
                ShowSuccessMessage($"Avatar Username: {avatar.Username}");
                ShowSuccessMessage($"Avatar Type: {avatar.AvatarType.Name}");
                ShowSuccessMessage($"Avatar Created Date: {avatar.CreatedDate}");
                ShowSuccessMessage($"Avatar Modifed Date: {avatar.ModifiedDate}");
                ShowSuccessMessage($"Avatar Last Beamed In Date: {avatar.LastBeamedIn}");
                ShowSuccessMessage($"Avatar Last Beamed Out Date: {avatar.LastBeamedOut}");
                ShowSuccessMessage(String.Concat("Avatar Is Active: ", avatar.IsActive ? "True" : "False"));
                ShowSuccessMessage(String.Concat("Avatar Is Beamed In: ", avatar.IsBeamedIn ? "True" : "False"));
                ShowSuccessMessage(String.Concat("Avatar Is Verified: ", avatar.IsVerified ? "True" : "False"));
                ShowSuccessMessage($"Avatar Version: {avatar.Version}");
            }
            else
                ShowErrorMessage("Error Loading Avatar.");
        }

        private static void EnableOrDisableAutoProviderList(Func<bool, List<ProviderType>, bool> funct, bool isEnabled, List<ProviderType> providerTypes, string workingMessage, string successMessage, string errorMessage)
        {
            ShowWorkingMessage(workingMessage);

            if (funct(isEnabled, providerTypes))
                ShowSuccessMessage(successMessage);
            else
                ShowErrorMessage(errorMessage);
        }

        private static void HandleBooleansResponse(bool isSuccess, string successMessage, string errorMessage)
        {
            if (isSuccess)
                ShowSuccessMessage(successMessage);
            else
                ShowErrorMessage(errorMessage);
        }

        private static void HandleOASISResponse<T>(OASISResult<T> result, string successMessage, string errorMessage)
        {
            if (!result.IsError && result.Result != null)
                ShowSuccessMessage(successMessage);
            else
                ShowErrorMessage($"{errorMessage}Reason: {result.Message}");
        }

        private static void HandleHolonsOASISResponse(OASISResult<IEnumerable<IHolon>> result)
        {
            if (!result.IsError && result.Result != null)
            {
                ShowSuccessMessage($"{result.Result.Count()} Holon(s) Loaded:");
                ShowHolons(result.Result, " ");
            }
            else
                ShowErrorMessage($"Error Loading Holons. Reason: {result.Message}");
        }

        private static async Task<OASISResult<CoronalEjection>> GenerateCelestialBody(string name, ICelestialBody parentCelestialBody, GenesisType genesisType, string dnaFolder, string cSharpGeneisFolder, string rustGenesisFolder, string genesisNameSpace)
        {
            // Create (OAPP) by generating dynamic template/scaffolding code.
            string message = $"Generating {Enum.GetName(typeof(GenesisType), genesisType)} '{name}' (OAPP)";

            if (genesisType == GenesisType.Moon && parentCelestialBody != null)
                message = $"{message} For Planet '{parentCelestialBody.Name}'";

            message = $"{message} ...";

            ShowWorkingMessage(message);
            OASISResult<CoronalEjection> lightResult = STAR.LightAsync(genesisType, name, parentCelestialBody, dnaFolder, cSharpGeneisFolder, rustGenesisFolder, genesisNameSpace).Result;

            if (lightResult.IsError)
                ShowErrorMessage(string.Concat(" ERROR OCCURED. Error Message: ", lightResult.Message));
            else
            {
                ShowSuccessMessage($"{Enum.GetName(typeof(GenesisType), genesisType)} Generated.");

                Console.WriteLine("");
                Console.WriteLine(string.Concat(" Id: ", lightResult.Result.CelestialBody.Id));
                Console.WriteLine(string.Concat(" CreatedByAvatarId: ", lightResult.Result.CelestialBody.CreatedByAvatarId));
                Console.WriteLine(string.Concat(" CreatedDate: ", lightResult.Result.CelestialBody.CreatedDate)) ;
                Console.WriteLine("");
                ShowZomesAndHolons(lightResult.Result.CelestialBody.CelestialBodyCore.Zomes, string.Concat($" {Enum.GetName(typeof(GenesisType), genesisType)} contains ", lightResult.Result.CelestialBody.CelestialBodyCore.Zomes.Count(), " Zome(s): "));
            }

            return lightResult;
        }

        private static void ShowZomesAndHolons(IEnumerable<IZome> zomes, string customHeader = null)
        {
            if (string.IsNullOrEmpty(customHeader))
                Console.WriteLine($"{zomes.Count()} Zome(s) Found:");
            else
                Console.WriteLine(customHeader);

            foreach (IZome zome in zomes)
            {
                Console.WriteLine(string.Concat("  Zome Name: ", zome.Name, " Zome Id: ", zome.Id, " containing ", zome.Holons.Count(), " holon(s):"));
                ShowHolons(zome.Holons, " ");
            }
        }

        private static void ShowHolons(IEnumerable<IHolon> holons, string customHeader = null)
        {
            if (string.IsNullOrEmpty(customHeader))
                Console.WriteLine($"{holons.Count()} Holons(s) Found:");
            else
                Console.WriteLine(customHeader);

            foreach (IHolon holon in holons)
            {
                Console.WriteLine("");
                Console.WriteLine(string.Concat("   Holon Name: ", holon.Name, " Holon Id: ", holon.Id, " containing ", holon.Nodes.Count(), " node(s): "));

                foreach (INode node in holon.Nodes)
                {
                    Console.WriteLine("");
                    Console.WriteLine(string.Concat("    Node Name: ", node.NodeName, " Node Id: ", node.Id, " Node Type: ", Enum.GetName(node.NodeType)));
                }
            }
        }

        private static void CelestialBody_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
           
        }

        private static void CelestialBody_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
           
        }

        private static void CelestialBody_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            
        }

        private static void ShowColoursAvailable()
        {
            ShowMessage("", false);
            ConsoleColor oldColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" Sorry, that colour is not valid. Please try again. The colour needs to be one of the following: ");

            string[] values = Enum.GetNames(typeof(ConsoleColor));

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != "Black")
                {
                    PrintColour(values[i]);

                    if (i < values.Length - 2)
                        Console.Write(", ");

                    else if (i == values.Length - 2)
                        Console.Write(" or ");
                }
            }

            ShowMessage("", false);
            Console.ForegroundColor = oldColour;
        }

        private static void PrintColour(string colour)
        {
            Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colour);
            Console.Write(colour);
        }

        private static void ShowSuccessMessage(string message, bool lineSpace = true)
        {
            if (_spinner.IsActive)
            {
                _spinner.Stop();
                Console.WriteLine("");
            }

            ConsoleColor existingColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            if (lineSpace)
                Console.WriteLine("");

            Console.WriteLine(string.Concat(" ", message));
            Console.ForegroundColor = existingColour;

            //if (SuperStar.LoggedInAvatar != null)
            //    Console.ForegroundColor = SuperStar.LoggedInAvatar.STARCLIColour;
            //else
            //    Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private static void ShowMessage(string message, bool lineSpace = true, bool noLineBreaks = false)
        {
            if (lineSpace)
                Console.WriteLine(" ");
            
            if (noLineBreaks)
                Console.Write(string.Concat(" ", message));
            else
                Console.WriteLine(string.Concat(" ", message));
        }

        private static void ShowWorkingMessage(string message, bool lineSpace = true)
        {
            if (_spinner.IsActive)
            {
                _spinner.Stop();
                Console.WriteLine("");
            }

            if (lineSpace)
                Console.WriteLine(" ");

            Console.Write(string.Concat(" ", message));
            _spinner.Start();
        }

        private static void ShowErrorMessage(string message, bool lineSpace = true)
        {
            if (_spinner.IsActive)
            {
                _spinner.Stop();
                Console.WriteLine("");
            }

            ConsoleColor existingColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            
            if (lineSpace)
                Console.WriteLine("");

            Console.WriteLine(string.Concat(" ", message));
            Console.ForegroundColor = existingColour;

            //if (SuperStar.LoggedInAvatar != null)
            //    Console.ForegroundColor = SuperStar.LoggedInAvatar.STARCLIColour;
            //else
            //    Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private static string GetValidTitle(string message)
        {
            string title = GetValidInput(message).ToUpper();
            //string[] validTitles = new string[5] { "Mr", "Mrs", "Ms", "Miss", "Dr" };
            string validTitles = "MR,MRS,MS,MISS,DR";

            bool titleValid = false;
            while (!titleValid)
            {
                if (!validTitles.Contains(title))
                {
                    ShowErrorMessage("Title invalid. Please try again.");
                    title = GetValidInput(message).ToUpper();
                }
                else
                    titleValid = true;
            }

            return ExtensionMethods.ExtensionMethods.ToPascalCase(title);
        }

        private static string GetValidInput(string message)
        {
            string input = "";
            while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
            {
                ShowMessage(string.Concat("", message), true, true);
                input = Console.ReadLine();
            }

            return input;
        }

        private static bool GetConfirmation(string message)
        {
            bool validKey = false;
            bool confirm = false;

            while (!validKey)
            {
                //ShowMessage("", false);
                ShowMessage(message, true, true);
                ConsoleKey key = Console.ReadKey().Key;

                if (key == ConsoleKey.Y)
                {
                    confirm = true;
                    validKey = true;
                }

                if (key == ConsoleKey.N)
                {
                    confirm = false;
                    validKey = true;
                }
            }

            return confirm;
        }

        private static string GetValidEmail(string message, bool checkIfEmailAlreadyInUse)
        {
            bool emailValid = false;
            string email = "";

            while (!emailValid)
            {
                ShowMessage(string.Concat("", message), true, true);
                email = Console.ReadLine();

                if (!ValidationHelper.IsValidEmail(email))
                    ShowErrorMessage("That email is not valid. Please try again.");

                else if (checkIfEmailAlreadyInUse)
                {
                    ShowWorkingMessage("Checking if email already in use...");

                    if (STAR.OASISAPI.Avatar.CheckIfEmailIsAlreadyInUse(email))
                        ShowErrorMessage("Sorry, that email is already in use, please use another one.");
                    else
                    {
                        emailValid = true;
                        _spinner.Stop();
                        ShowMessage("", false);
                    }
                }
                else
                    emailValid = true;
            }

            return email;
        }

        private static string GetValidPassword()
        {
            string password = "";
            string password2 = "";
            ShowMessage("", false);

            while ((string.IsNullOrEmpty(password) && string.IsNullOrEmpty(password2)) || password != password2)
            {
                password = ReadPassword("What is the password you wish to use? ");
                password2 = ReadPassword("Please confirm password: ");

                if (password != password2)
                    ShowErrorMessage("The passwords do not match. Please try again.");
            }

            return password;
        }

        private static string ReadPassword(string message)
        {
            string password = "";
            ConsoleKey key;

            while (string.IsNullOrEmpty(password) && string.IsNullOrWhiteSpace(password))
            {
                ShowMessage(string.Concat("", message), true, true);

                do
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    key = keyInfo.Key;

                    if (key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        Console.Write("\b \b");
                        password = password[0..^1];
                    }
                    else if (!char.IsControl(keyInfo.KeyChar))
                    {
                        Console.Write("*");
                        password += keyInfo.KeyChar;
                    }
                } while (key != ConsoleKey.Enter);

                ShowMessage("", false);
            }

            return password;
        }

        private static void GetValidColour(ref ConsoleColor favColour, ref ConsoleColor cliColour)
        {
            bool colourSet = false;
            while (!colourSet)
            {
                ShowMessage("What is your favourite colour? ", true, true);
                string colour = Console.ReadLine();
                colour = ExtensionMethods.ExtensionMethods.ToPascalCase(colour);
                object colourObj = null;

                if (Enum.TryParse(typeof(ConsoleColor), colour, out colourObj))
                {
                    favColour = (ConsoleColor)colourObj;
                    Console.ForegroundColor = favColour;
                    ShowMessage("Do you prefer to use your favourite colour? :) ", true, true);

                    if (Console.ReadKey().Key != ConsoleKey.Y)
                    {
                        Console.WriteLine("");

                        while (!colourSet)
                        {
                            //Defaults
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            _spinner.Colour = ConsoleColor.Green;

                            ShowMessage("Which colour would you prefer? ", true, true);
                            colour = Console.ReadLine();
                            colour = ExtensionMethods.ExtensionMethods.ToPascalCase(colour);
                            colourObj = null;

                            if (Enum.TryParse(typeof(ConsoleColor), colour, out colourObj))
                            {
                                cliColour = (ConsoleColor)colourObj;
                                Console.ForegroundColor = cliColour;
                                _spinner.Colour = cliColour;

                               // ShowMessage("", false);
                                ShowMessage("This colour ok? ", true, true);

                                if (Console.ReadKey().Key == ConsoleKey.Y)
                                    colourSet = true;
                                else
                                    ShowMessage("", false);
                            }
                            else
                                ShowColoursAvailable();
                        }
                    }
                    else
                        colourSet = true;
                }
                else
                    ShowColoursAvailable();
            }
        }

        private static void CreateAvatar()
        {
            ConsoleColor favColour = ConsoleColor.Green;
            ConsoleColor cliColour = ConsoleColor.Green;

            ShowMessage("");
            ShowMessage("Please create an avatar below:", false);

            string title = GetValidTitle("What is your title? ");
            string firstName = GetValidInput("What is your first name? ");
            ShowMessage(string.Concat("Nice to meet you ", firstName, ". :)"));
            string lastName = GetValidInput(string.Concat("What is your last name ", firstName, "? "));
            string email = GetValidEmail("What is your email address? ", true);
            GetValidColour(ref favColour, ref cliColour);
            string password = GetValidPassword();
            ShowWorkingMessage("Creating Avatar...");

            OASISResult<IAvatar> createAvatarResult = STAR.CreateAvatar(title, firstName, lastName, email, password, cliColour, favColour);
            ShowMessage("");

            if (createAvatarResult.IsError)
                ShowErrorMessage(string.Concat("Error creating avatar. Error message: ", createAvatarResult.Message));
            else
                ShowSuccessMessage("Successfully Created Avatar. Please Check Your Email To Verify Your Account Before Logging In.");
        }

        private static void ShowHeader()
        {
            var versionString = Assembly.GetEntryAssembly()
                                           .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                           .InformationalVersion
                                           .ToString();

            // Console.SetWindowSize(300, Console.WindowHeight);
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("************************************************************************************************");
            Console.Write(" NextGen Software");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" STAR");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($" (Synergiser Transformer Aggregator Resolver) HDK/ODK TEST HARNESS v{versionString} ");
            Console.WriteLine("");
            Console.WriteLine("************************************************************************************************");
            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("                  ,O,");
            Console.WriteLine("                 ,OOO,");
            Console.WriteLine("           'oooooOOOOOooooo'");
            Console.WriteLine("             `OOOOOOOOOOO`");
            Console.WriteLine("               `OOOOOOO`");
            Console.WriteLine("               OOOO'OOOO");
            Console.WriteLine("              OOO'   'OOO");
            Console.WriteLine("             O'         'O");

            /*
            Image Picture = Image.FromFile("images/star6b.jpg");
            Console.SetBufferSize((Picture.Width * 0x2), (Picture.Height * 0x2));
            //Console.SetBufferSize((Picture.Width), (Picture.Height));
            Console.WindowWidth = 100; //180
            //Console.WindowHeight = 61;

            FrameDimension Dimension = new FrameDimension(Picture.FrameDimensionsList[0x0]);
            int FrameCount = Picture.GetFrameCount(Dimension);
            int Left = Console.WindowLeft, Top = Console.WindowTop;
            char[] Chars = { '#', '#', '@', '%', '=', '+', '*', ':', '-', '.', ' ' };
            Picture.SelectActiveFrame(Dimension, 0x0);
            for (int i = 0x0; i < Picture.Height; i++)
            {
                for (int x = 0x0; x < Picture.Width; x++)
                {
                    Color Color = ((Bitmap)Picture).GetPixel(x, i);
                    int Gray = (Color.R + Color.G + Color.B) / 0x3;
                    int Index = (Gray * (Chars.Length - 0x1)) / 0xFF;
                    Console.Write(Chars[Index]);
                }
                Console.Write('\n');
                Thread.Sleep(50);
            }
            //Console.SetCursorPosition(Left, Top);
            */

            // Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
            Colorful.Console.WriteAscii(" STAR", Color.Yellow);

            // var font = FigletFont.Load("fonts/wow.flf");
            // Figlet figlet = new Figlet(font);
            //Colorful.Console.WriteLine(figlet.ToAscii("STAR"), Color.FromArgb(67, 144, 198));
            // Colorful.Console.WriteLine(figlet.ToAscii("STAR"), Color.Yellow);

            ShowCommands();

            Console.WriteLine("");
            Console.Write(" Welcome to");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" STAR");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" (The");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" ♥");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" Of The OASIS)");
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private static void ShowCommands()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n Usage:");
            Console.WriteLine("   star ignite = Ignite SuperStar & Boot The OASIS");
            Console.WriteLine("   star extinguish = Extinguish SuperStar & Shutdown The OASIS");
            Console.WriteLine("   star beamin = Log in");
            Console.WriteLine("   star beamout = Log out");
            Console.WriteLine("   star light -dnaFolder -cSharpGeneisFolder -rustGenesisFolder = Creates a new Planet (OAPP) at the given folder genesis locations, from the given OAPP DNA.");
            Console.WriteLine("   star light -transmute -hAppDNA -cSharpGeneisFolder -rustGenesisFolder = Creates a new Planet (OAPP) at the given folder genesis locations, from the given hApp DNA.");
            Console.WriteLine("   star flare -planetName = Build a planet (OAPP).");
            Console.WriteLine("   star shine -planetName = Launch & activate a planet (OAPP) by shining the star's light upon it...");
            Console.WriteLine("   star dim -planetName = Deactivate a planet (OAPP).");
            Console.WriteLine("   star seed -planetName = Deploy a planet (OAPP).");
            Console.WriteLine("   star twinkle -planetName = Deactivate a planet (OAPP).");
            Console.WriteLine("   star dust -planetName = Delete a planet (OAPP).");
            Console.WriteLine("   star radiate -planetName = Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*");
            Console.WriteLine("   star emit -planetName = Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)");
            Console.WriteLine("   star reflect -planetName = Show stats of the Planet (OAPP).");
            Console.WriteLine("   star evolve -planetName = Upgrade/update a Planet (OAPP).");
            Console.WriteLine("   star mutate -planetName = Import/Export hApp, dApp & others.");
            Console.WriteLine("   star love -planetName = Send/Receive Love.");
            Console.WriteLine("   star burst = View network stats/management/settings.");
            Console.WriteLine("   star super - Reserved For Future Use...");
            Console.WriteLine("************************************************************************************************");
        }

        private static void LoginAvatar()
        {
            OASISResult<IAvatar> beamInResult = null;

            while (beamInResult == null || (beamInResult != null && beamInResult.IsError))
            {
                //TODO: TEMP - PUT BACK IN WHEN GOING LIVE!
                /*
                ShowMessage("Please login below:");
                string username = GetValidEmail("Username/Email? ", false);
                string password = ReadPassword("Password? ");
                ShowWorkingMessage("Beaming In...");
                beamInResult = SuperStar.BeamIn(username, password);
                */

                ShowWorkingMessage("Beaming In...");
                //beamInResult = STAR.BeamIn("davidellams@hotmail.com", "my-super-secret-password");
                beamInResult = STAR.BeamIn("davidellams@hotmail.com", "test!");
                //beamInResult = STAR.BeamIn("davidellams@gmail.com", "test!");

                ShowMessage("");

                if (beamInResult.IsError)
                {
                    ShowErrorMessage(string.Concat("Error logging in. Error Message: ", beamInResult.Message));

                    if (beamInResult.Message == "Avatar has not been verified. Please check your email.")
                    {
                        ShowErrorMessage("Then either click the link in the email to activate your avatar or enter the validation token contained in the email below:", false);

                        bool validToken = false;
                        while (!validToken)
                        {
                            string token = GetValidInput("Enter validation token: ");
                            ShowWorkingMessage("Verifying Token...");
                            OASISResult<bool> verifyEmailResult = STAR.OASISAPI.Avatar.VerifyEmail(token);

                            if (verifyEmailResult.IsError)
                                ShowErrorMessage(verifyEmailResult.Message);
                            else
                            {
                                ShowSuccessMessage("Verification successful, you can now login");
                                validToken = true;
                            }
                        }
                    }
                }

                else if (STAR.LoggedInAvatar == null)
                    ShowErrorMessage("Error Beaming In. Username/Password may be incorrect.");
            }

            ShowSuccessMessage(string.Concat("Successfully Beamed In! Welcome back ", STAR.LoggedInAvatar.FullName, ". Have a nice day! :)"));
            ShowAvatarStats();
        }

        private static void ShowAvatarStats()
        {
            ShowMessage("", false);
            Console.WriteLine(string.Concat(" Karma: ", STAR.LoggedInAvatarDetail.Karma));
            Console.WriteLine(string.Concat(" Level: ", STAR.LoggedInAvatarDetail.Level));
            Console.WriteLine(string.Concat(" XP: ", STAR.LoggedInAvatarDetail.XP));

            Console.WriteLine("");
            Console.WriteLine(" Chakras:");
            Console.WriteLine(string.Concat(" Crown XP: ", STAR.LoggedInAvatarDetail.Chakras.Crown.XP));
            Console.WriteLine(string.Concat(" Crown Level: ", STAR.LoggedInAvatarDetail.Chakras.Crown.Level));
            Console.WriteLine(string.Concat(" ThirdEye XP: ", STAR.LoggedInAvatarDetail.Chakras.ThirdEye.XP));
            Console.WriteLine(string.Concat(" ThirdEye Level: ", STAR.LoggedInAvatarDetail.Chakras.ThirdEye.Level));
            Console.WriteLine(string.Concat(" Throat XP: ", STAR.LoggedInAvatarDetail.Chakras.Throat.XP));
            Console.WriteLine(string.Concat(" Throat Level: ", STAR.LoggedInAvatarDetail.Chakras.Throat.Level));
            Console.WriteLine(string.Concat(" Heart XP: ", STAR.LoggedInAvatarDetail.Chakras.Heart.XP));
            Console.WriteLine(string.Concat(" Heart Level: ", STAR.LoggedInAvatarDetail.Chakras.Heart.Level));
            Console.WriteLine(string.Concat(" SoloarPlexus XP: ", STAR.LoggedInAvatarDetail.Chakras.SoloarPlexus.XP));
            Console.WriteLine(string.Concat(" SoloarPlexus Level: ", STAR.LoggedInAvatarDetail.Chakras.SoloarPlexus.Level));
            Console.WriteLine(string.Concat(" Sacral XP: ", STAR.LoggedInAvatarDetail.Chakras.Sacral.XP));
            Console.WriteLine(string.Concat(" Sacral Level: ", STAR.LoggedInAvatarDetail.Chakras.Sacral.Level));

            Console.WriteLine(string.Concat(" Root SanskritName: ", STAR.LoggedInAvatarDetail.Chakras.Root.SanskritName));
            Console.WriteLine(string.Concat(" Root XP: ", STAR.LoggedInAvatarDetail.Chakras.Root.XP));
            Console.WriteLine(string.Concat(" Root Level: ", STAR.LoggedInAvatarDetail.Chakras.Root.Level));
            Console.WriteLine(string.Concat(" Root Progress: ", STAR.LoggedInAvatarDetail.Chakras.Root.Progress));
           // Console.WriteLine(string.Concat(" Root Color: ", SuperSTAR.LoggedInAvatar.Chakras.Root.Color.Name));
            Console.WriteLine(string.Concat(" Root Element: ", STAR.LoggedInAvatarDetail.Chakras.Root.Element.Name));
            Console.WriteLine(string.Concat(" Root YogaPose: ", STAR.LoggedInAvatarDetail.Chakras.Root.YogaPose.Name));
            Console.WriteLine(string.Concat(" Root WhatItControls: ", STAR.LoggedInAvatarDetail.Chakras.Root.WhatItControls));
            Console.WriteLine(string.Concat(" Root WhenItDevelops: ", STAR.LoggedInAvatarDetail.Chakras.Root.WhenItDevelops));
            Console.WriteLine(string.Concat(" Root Crystal Name: ", STAR.LoggedInAvatarDetail.Chakras.Root.Crystal.Name.Name));
            Console.WriteLine(string.Concat(" Root Crystal AmplifyicationLevel: ", STAR.LoggedInAvatarDetail.Chakras.Root.Crystal.AmplifyicationLevel));
            Console.WriteLine(string.Concat(" Root Crystal CleansingLevel: ", STAR.LoggedInAvatarDetail.Chakras.Root.Crystal.CleansingLevel));
            Console.WriteLine(string.Concat(" Root Crystal EnergisingLevel: ", STAR.LoggedInAvatarDetail.Chakras.Root.Crystal.EnergisingLevel));
            Console.WriteLine(string.Concat(" Root Crystal GroundingLevel: ", STAR.LoggedInAvatarDetail.Chakras.Root.Crystal.GroundingLevel));
            Console.WriteLine(string.Concat(" Root Crystal ProtectionLevel: ", STAR.LoggedInAvatarDetail.Chakras.Root.Crystal.ProtectionLevel));

            Console.WriteLine("");
            Console.WriteLine(" Aurua:");
            Console.WriteLine(string.Concat(" Brightness: ", STAR.LoggedInAvatarDetail.Aura.Brightness));
            Console.WriteLine(string.Concat(" Size: ", STAR.LoggedInAvatarDetail.Aura.Size));
            Console.WriteLine(string.Concat(" Level: ", STAR.LoggedInAvatarDetail.Aura.Level));
            Console.WriteLine(string.Concat(" Value: ", STAR.LoggedInAvatarDetail.Aura.Value));
            Console.WriteLine(string.Concat(" Progress: ", STAR.LoggedInAvatarDetail.Aura.Progress));
            Console.WriteLine(string.Concat(" ColourRed: ", STAR.LoggedInAvatarDetail.Aura.ColourRed));
            Console.WriteLine(string.Concat(" ColourGreen: ", STAR.LoggedInAvatarDetail.Aura.ColourGreen));
            Console.WriteLine(string.Concat(" ColourBlue: ", STAR.LoggedInAvatarDetail.Aura.ColourBlue));

            Console.WriteLine("");
            Console.WriteLine(" Attributes:");
            Console.WriteLine(string.Concat(" Strength: ", STAR.LoggedInAvatarDetail.Attributes.Strength));
            Console.WriteLine(string.Concat(" Speed: ", STAR.LoggedInAvatarDetail.Attributes.Speed));
            Console.WriteLine(string.Concat(" Dexterity: ", STAR.LoggedInAvatarDetail.Attributes.Dexterity));
            Console.WriteLine(string.Concat(" Intelligence: ", STAR.LoggedInAvatarDetail.Attributes.Intelligence));
            Console.WriteLine(string.Concat(" Magic: ", STAR.LoggedInAvatarDetail.Attributes.Magic));
            Console.WriteLine(string.Concat(" Wisdom: ", STAR.LoggedInAvatarDetail.Attributes.Wisdom));
            Console.WriteLine(string.Concat(" Toughness: ", STAR.LoggedInAvatarDetail.Attributes.Toughness));
            Console.WriteLine(string.Concat(" Vitality: ", STAR.LoggedInAvatarDetail.Attributes.Vitality));
            Console.WriteLine(string.Concat(" Endurance: ", STAR.LoggedInAvatarDetail.Attributes.Endurance));

            Console.WriteLine("");
            Console.WriteLine(" Stats:");
            Console.WriteLine(string.Concat(" HP: ", STAR.LoggedInAvatarDetail.Stats.HP.Current, "/", STAR.LoggedInAvatarDetail.Stats.HP.Max));
            Console.WriteLine(string.Concat(" Mana: ", STAR.LoggedInAvatarDetail.Stats.Mana.Current, "/", STAR.LoggedInAvatarDetail.Stats.Mana.Max));
            Console.WriteLine(string.Concat(" Energy: ", STAR.LoggedInAvatarDetail.Stats.Energy.Current, "/", STAR.LoggedInAvatarDetail.Stats.Energy.Max));
            Console.WriteLine(string.Concat(" Staminia: ", STAR.LoggedInAvatarDetail.Stats.Staminia.Current, "/", STAR.LoggedInAvatarDetail.Stats.Staminia.Max));

            Console.WriteLine("");
            Console.WriteLine(" Super Powers:");
            Console.WriteLine(string.Concat(" Flight: ", STAR.LoggedInAvatarDetail.SuperPowers.Flight));
            Console.WriteLine(string.Concat(" Astral Projection: ", STAR.LoggedInAvatarDetail.SuperPowers.AstralProjection));
            Console.WriteLine(string.Concat(" Bio-Locatation: ", STAR.LoggedInAvatarDetail.SuperPowers.BioLocatation));
            Console.WriteLine(string.Concat(" Heat Vision: ", STAR.LoggedInAvatarDetail.SuperPowers.HeatVision));
            Console.WriteLine(string.Concat(" Invulerability: ", STAR.LoggedInAvatarDetail.SuperPowers.Invulerability));
            Console.WriteLine(string.Concat(" Remote Viewing: ", STAR.LoggedInAvatarDetail.SuperPowers.RemoteViewing));
            Console.WriteLine(string.Concat(" Super Speed: ", STAR.LoggedInAvatarDetail.SuperPowers.SuperSpeed));
            Console.WriteLine(string.Concat(" Super Strength: ", STAR.LoggedInAvatarDetail.SuperPowers.SuperStrength));
            Console.WriteLine(string.Concat(" Telekineseis: ", STAR.LoggedInAvatarDetail.SuperPowers.Telekineseis));
            Console.WriteLine(string.Concat(" XRay Vision: ", STAR.LoggedInAvatarDetail.SuperPowers.XRayVision));

            Console.WriteLine("");
            Console.WriteLine(" Skills:");
            Console.WriteLine(string.Concat(" Computers: ", STAR.LoggedInAvatarDetail.Skills.Computers));
            Console.WriteLine(string.Concat(" Engineering: ", STAR.LoggedInAvatarDetail.Skills.Engineering));
            Console.WriteLine(string.Concat(" Farming: ", STAR.LoggedInAvatarDetail.Skills.Farming));
            Console.WriteLine(string.Concat(" FireStarting: ", STAR.LoggedInAvatarDetail.Skills.FireStarting));
            Console.WriteLine(string.Concat(" Fishing: ", STAR.LoggedInAvatarDetail.Skills.Fishing));
            Console.WriteLine(string.Concat(" Languages: ", STAR.LoggedInAvatarDetail.Skills.Languages));
            Console.WriteLine(string.Concat(" Meditation: ", STAR.LoggedInAvatarDetail.Skills.Meditation));
            Console.WriteLine(string.Concat(" MelleeCombat: ", STAR.LoggedInAvatarDetail.Skills.MelleeCombat));
            Console.WriteLine(string.Concat(" Mindfulness: ", STAR.LoggedInAvatarDetail.Skills.Mindfulness));
            Console.WriteLine(string.Concat(" Negotiating: ", STAR.LoggedInAvatarDetail.Skills.Negotiating));
            Console.WriteLine(string.Concat(" RangeCombat: ", STAR.LoggedInAvatarDetail.Skills.RangeCombat));
            Console.WriteLine(string.Concat(" Research: ", STAR.LoggedInAvatarDetail.Skills.Research));
            Console.WriteLine(string.Concat(" Science: ", STAR.LoggedInAvatarDetail.Skills.Science));
            Console.WriteLine(string.Concat(" SpellCasting: ", STAR.LoggedInAvatarDetail.Skills.SpellCasting));
            Console.WriteLine(string.Concat(" Translating: ", STAR.LoggedInAvatarDetail.Skills.Translating));
            Console.WriteLine(string.Concat(" Yoga: ", STAR.LoggedInAvatarDetail.Skills.Yoga));

            Console.WriteLine("");
            Console.WriteLine(" Gifts:");

            foreach (AvatarGift gift in STAR.LoggedInAvatarDetail.Gifts)
                Console.WriteLine(string.Concat(" ", Enum.GetName(gift.GiftType), " earnt on ", gift.GiftEarnt.ToString()));

            Console.WriteLine("");
            Console.WriteLine(" Spells:");

            foreach (Spell spell in STAR.LoggedInAvatarDetail.Spells)
                Console.WriteLine(string.Concat(" ", spell.Name));

            Console.WriteLine("");
            Console.WriteLine(" Inventory:");

            foreach (InventoryItem inventoryItem in STAR.LoggedInAvatarDetail.Inventory)
                Console.WriteLine(string.Concat(" ", inventoryItem.Name));

            Console.WriteLine("");
            Console.WriteLine(" Achievements:");

            foreach (Achievement achievement in STAR.LoggedInAvatarDetail.Achievements)
                Console.WriteLine(string.Concat(" ", achievement.Name));

            Console.WriteLine("");
            Console.WriteLine(" Gene Keys:");

            foreach (GeneKey geneKey in STAR.LoggedInAvatarDetail.GeneKeys)
                Console.WriteLine(string.Concat(" ", geneKey.Name));

            Console.WriteLine("");
            Console.WriteLine(" Human Design:");
            Console.WriteLine(string.Concat(" Type: ", STAR.LoggedInAvatarDetail.HumanDesign.Type));
        }

        private static void STAR_OnInitialized(object sender, System.EventArgs e)
        {
            ShowSuccessMessage(" STAR Initialized.");
        }

        private static void STAR_OnOASISBootError(object sender, OASISBootErrorEventArgs e)
        {
            //ShowErrorMessage(string.Concat("OASIS Boot Error. Reason: ", e.ErrorReason));
            ShowErrorMessage(e.ErrorReason);
        }

        private static void STAR_OnOASISBooted(object sender, EventArgs.OASISBootedEventArgs e)
        {
           // ShowSuccessMessage(string.Concat("OASIS BOOTED.", e.Message));
        }

        private static void STAR_OnStarError(object sender, EventArgs.StarErrorEventArgs e)
        {
             ShowErrorMessage(string.Concat("Error Igniting SuperStar. Reason: ", e.Reason));
        }

        private static void STAR_OnStarIgnited(object sender, System.EventArgs e)
        {
            //ShowSuccessMessage("STAR IGNITED");
        }

        private static void STAR_OnStarStatusChanged(object sender, EventArgs.StarStatusChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                switch (e.MessageType)
                {
                    case Enums.StarStatusMessageType.Processing:
                        ShowWorkingMessage(e.Message);
                        break;

                    case Enums.StarStatusMessageType.Success:
                        ShowSuccessMessage(e.Message);
                        break;

                    case Enums.StarStatusMessageType.Error:
                        ShowErrorMessage(e.Message);
                        break;
                }
            }
            else
            {
                switch (e.Status)
                {
                    case Enums.StarStatus.BootingOASIS:
                        ShowWorkingMessage("BOOTING OASIS...");
                        break;

                    case Enums.StarStatus.OASISBooted:
                        ShowSuccessMessage("OASIS BOOTED");
                        break;

                    case Enums.StarStatus.Igniting:
                        ShowWorkingMessage("IGNITING STAR...");
                        break;

                    case Enums.StarStatus.Ingited:
                        ShowSuccessMessage("STAR IGNITED");
                        break;

                        //case Enums.SuperStarStatus.Error:
                        //  ShowErrorMessage("SuperStar Error");
                }
            }
        }

        private static void STAR_OnCelestialSpacesLoaded(object sender, CelestialSpacesLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"CelesitalSpaces Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialSpacesSaved(object sender, CelestialSpacesSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"CelesitalSpaces Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialSpacesError(object sender, CelestialSpacesErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving CelestialSpaces. Reason: {e.Reason}");
        }

        private static void STAR_OnCelestialSpaceLoaded(object sender, CelestialSpaceLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"CelesitalSpace Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialSpaceSaved(object sender, CelestialSpaceSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"CelesitalSpace Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialSpaceError(object sender, CelestialSpaceErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving CelestialSpace. Reason: {e.Reason}");
        }

        private static void STAR_OnCelestialBodyLoaded(object sender, CelestialBodyLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"CelesitalBody Loaded Successfully. {detailedMessage}");
        }
        private static void STAR_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"CelesitalBody Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving CelestialBody. Reason: {e.Reason}");
        }

        private static void STAR_OnCelestialBodiesLoaded(object sender, CelestialBodiesLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"CelesitalBodies Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialBodiesSaved(object sender, CelestialBodiesSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"CelesitalBodies Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialBodiesError(object sender, CelestialBodiesErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving CelestialBodies. Reason: {e.Reason}");
        }

        private static void STAR_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"Zome Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"Zome Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving Zome. Reason: {e.Reason}");
            //Console.WriteLine(string.Concat("Star Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNETErrorDetails.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNETErrorDetails.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
            //ShowErrorMessage(string.Concat(" STAR Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void STAR_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"Zome Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"Zome Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving Zomes. Reason: {e.Reason}");
        }

        private static void STAR_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            ShowSuccessMessage(string.Concat(" STAR Holons Loaded. Holon Name: ", e.Result.Result.Name));
        }

        private static void STAR_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            if (e.Result.IsError)
                ShowErrorMessage(e.Result.Message);
            else
                ShowSuccessMessage(string.Concat(" STAR Holons Saved. Holon Saved: ", e.Result.Result.Name));
        }

        private static void STAR_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving Holon. Reason: {e.Reason}");
        }

        private static void STAR_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            ShowSuccessMessage(string.Concat(" STAR Holons Loaded. Holons Loaded: ", e.Result.Result.Count()));
        }

        private static void STAR_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            ShowSuccessMessage($"Holons Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving Holons. Reason: {e.Reason}");
        }

        private static void StarCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving Zome For StarCore. Reason: {e.Reason}");
            //Console.WriteLine(string.Concat("Star Core Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNETErrorDetails.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNETErrorDetails.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
            //ShowErrorMessage(string.Concat(" Star Core Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void OurWorld_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            ShowErrorMessage($"Error occured loading/saving Zome For Planet Our World. Reason: {e.Reason}");
            //Console.WriteLine(string.Concat("Our World Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNETErrorDetails.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNETErrorDetails.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
            //ShowErrorMessage(string.Concat(" Our World Error Occured. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void OurWorld_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            Console.WriteLine(" Holon Loaded");
            Console.WriteLine(string.Concat(" Holon Id: ", e.Result.Result.Id));
            Console.WriteLine(string.Concat(" Holon ProviderUniqueStorageKey: ", e.Result.Result.ProviderUniqueStorageKey));
            Console.WriteLine(string.Concat(" Holon Name: ", e.Result.Result.Name));
            Console.WriteLine(string.Concat(" Holon Type: ", e.Result.Result.HolonType));
            Console.WriteLine(string.Concat(" Holon Description: ", e.Result.Result.Description));

            //Console.WriteLine(string.Concat("ourWorld.Zomes[0].Holons[0].ProviderUniqueStorageKey: ", ourWorld.Zomes[0].Holons[0].ProviderUniqueStorageKey));
            Console.WriteLine(string.Concat(" ourWorld.Zomes[0].Holons[0].ProviderUniqueStorageKey: ", _superWorld.CelestialBodyCore.Zomes[0].Holons[0].ProviderUniqueStorageKey));
        }

        private static void OurWorld_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            if (e.Result.IsError)
                ShowErrorMessage(e.Result.Message);
            else
            {
                Console.WriteLine(" Holon Saved");
                Console.WriteLine(string.Concat(" Holon Id: ", e.Result.Result.Id));
                Console.WriteLine(string.Concat(" Holon ProviderUniqueStorageKey: ", e.Result.Result.ProviderUniqueStorageKey));
                Console.WriteLine(string.Concat(" Holon Name: ", e.Result.Result.Name));
                Console.WriteLine(string.Concat("Holon Type: ", e.Result.Result.HolonType));
                Console.WriteLine(string.Concat(" Holon Description: ", e.Result.Result.Description));

                Console.WriteLine(" Loading Holon...");
                //ourWorld.CelestialBodyCore.LoadHolonAsync(e.Holon.Name, e.Holon.ProviderUniqueStorageKey);
                _superWorld.CelestialBodyCore.LoadHolonAsync(e.Result.Result.Id);
            }
        }
    }
}
