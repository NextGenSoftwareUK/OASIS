using Ipfs;
using Neo4j.Driver;
using System.Drawing;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.Membranes;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class STARTests
    {
        string _privateKey = "";
        Planet _superWorld = null;
        Moon _jlaMoon = null;

        /// <summary>
        //        /// This is a good example of how to programatically interact with STAR including scripting etc...
        //        /// </summary>
        //        /// <param name="celestialBodyDNAFolder"></param>
        //        /// <param name="geneisFolder"></param>
        //        /// <returns></returns>
        public async Task RunCOSMICTests(OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, string celestialBodyDNAFolder, string geneisFolder)
        {
            CLIEngine.ShowWorkingMessage("BEGINNING STAR ODK/COSMIC TEST'S...");

            OASISResult<CoronalEjection> result = await STARCLI.Zomes.GenerateZomesAndHolonsAsync("Zomes And Holons Only", "Zomes And Holons Only Desc", OAPPType, OAPPTemplateId, OAPPTemplateVersion, celestialBodyDNAFolder, Path.Combine(geneisFolder, "ZomesAndHolons"), "NextGenSoftware.OASIS.OAPPS.ZomesAndHolonsOnly");

            //Passing in null for the ParentCelestialBody will default it to the default planet (Our World).
            result = await STARCLI.CelestialBodies.GenerateCelestialBodyAsync("The Justice League Academy", "Test Moon", null, OAPPType, OAPPTemplateId, OAPPTemplateVersion, GenesisType.Moon, celestialBodyDNAFolder, Path.Combine(geneisFolder, "JLA"), "NextGenSoftware.OASIS.OAPPS.JLA");

            // Currenly the JLA Moon and Our World Planet share the same Zome/Holon DNA (celestialBodyDNAFolder) but they can also have their own zomes/holons if they wish...
            // TODO: In future you will also be able to define the full CelestialBody DNA seperatley (cs/json) for each planet, moon, star etc where they can also define additional meta data for the moon/planet/star as well as their own zomes/holons like we have now, plus they can also refer to existing holons/zomes either in a folder (like we have now) or in STARNET Library using the GUID.
            // They will still be able to use a shared zomes/holons DNA folder as it is now if they wish or a combo of the two approaches...

            if (result != null && !result.IsError && result.Result != null && result.Result.CelestialBody != null)
            {
                _jlaMoon = (Moon)result.Result.CelestialBody;
                await STARCLI.CelestialBodies.LoadCelestialBodyAsync(_jlaMoon, "The Justice League Academy Moon");
                await STARCLI.Holons.LoadHolonAsync(_jlaMoon.Id.ToString(), "The Justice League Academy Moon");
            }

            //Passing in null for the ParentCelestialBody will default it to the default Star (Our Sun Sol).
            result = await STARCLI.CelestialBodies.GenerateCelestialBodyAsync("Our World", "Test Planet", null, OAPPType, OAPPTemplateId, OAPPTemplateVersion, GenesisType.Planet, celestialBodyDNAFolder, Path.Combine(geneisFolder, "Our World"), "NextGenSoftware.OASIS.OAPPS.OurWorld");

            if (result != null && !result.IsError && result.Result != null && result.Result.CelestialBody != null)
            {
                _superWorld = (Planet)result.Result.CelestialBody;

                result.Result.CelestialBody.OnHolonLoaded += CelestialBody_OnHolonLoaded;
                result.Result.CelestialBody.OnHolonSaved += CelestialBody_OnHolonSaved;
                result.Result.CelestialBody.OnZomeError += CelestialBody_OnZomeError;

                CLIEngine.ShowWorkingMessage("Loading Our World Zomes & Holons...");
                OASISResult<IEnumerable<IZome>> zomesResult = await result.Result.CelestialBody.LoadZomesAsync();

                bool finished = false;
                if (zomesResult != null && !zomesResult.IsError && zomesResult.Result != null)
                {
                    if (zomesResult.Result.Count() > 0)
                    {
                        CLIEngine.ShowSuccessMessage("Zomes & Holons Loaded Successfully.");
                        Console.WriteLine("");
                        STARCLI.Zomes.ShowZomesAndHolons(zomesResult.Result);
                    }
                    else
                        CLIEngine.ShowSuccessMessage("No Zomes Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"An Error Occured Loading Zomes/Holons. Reason: {zomesResult.Message}");


                //Set some custom properties (will save and load again below to check they persist).
                //TODO: Eventually you will be able to set these in the meta data when creating the celestial body.


                _superWorld.Age = 777777777777;
                _superWorld.Colour = Color.Blue;
                _superWorld.CurrentOrbitAngleOfParentStar = 44;
                _superWorld.Density = 44;
                _superWorld.DimensionLevel = DimensionLevel.Fourth;
                _superWorld.SubDimensionLevel = SubDimensionLevel.Second;
                _superWorld.DistanceFromParentStarInMetres = 77777777777777;
                _superWorld.EclipticLatitute = 33;
                _superWorld.EclipticLongitute = 44;
                _superWorld.EquatorialLatitute = 11;
                _superWorld.EquatorialLongitute = 22;
                _superWorld.GalacticLatitute = 23323232;
                _superWorld.GalacticLongitute = 43434323;
                _superWorld.GravitaionalPull = 7777;
                _superWorld.HorizontalLatitute = 452323;
                _superWorld.HorizontalLongitute = 4343422;
                _superWorld.Mass = 4343232323;
                _superWorld.NumberActiveAvatars = 77;
                _superWorld.NumberRegisteredAvatars = 444;
                _superWorld.Radius = 8888;
                _superWorld.OrbitPositionFromParentStar = 77;
                _superWorld.RotationPeriod = 878232;
                _superWorld.RotationSpeed = 77777;
                _superWorld.Size = 88888888;
                _superWorld.SpaceQuadrant = SpaceQuadrantType.Gamma;
                _superWorld.SpaceSector = 4;
                _superWorld.SuperGalacticLatitute = 7777;
                _superWorld.SuperGalacticLongitute = 7777;
                _superWorld.Temperature = 77;
                _superWorld.TiltAngle = 45;
                _superWorld.Weight = 77;

                //Example of adding a holon to Our World using AddHolonAsync
                CLIEngine.ShowWorkingMessage("Saving Test Holon To Our World...");
                OASISResult<Holon> ourWorldHolonResult = await _superWorld.AddHolonAsync(new Holon() { Name = "Our World Test Holon" }, STAR.BeamedInAvatar.Id);

                if (ourWorldHolonResult != null && !ourWorldHolonResult.IsError && ourWorldHolonResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Holon Saved Successfully.");
                    STARCLI.Holons.ShowHolonProperties(ourWorldHolonResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Saving Test Holon. Reason: {ourWorldHolonResult.Message}");


                //Example of adding a holon to a new zome using AddHolonAsync
                Zome zome = new Zome() { Name = "Our World Test Zome" };
                zome.Children.Add(new Holon() { Name = "Our World Test Zome Sub-Holon" });

                CLIEngine.ShowWorkingMessage("Saving Test Sub-Holon To Zome...");
                ourWorldHolonResult = await zome.AddHolonAsync(new Holon() { Name = "Our World Test Sub-Holon " }, STAR.BeamedInAvatar.Id);

                if (ourWorldHolonResult != null && !ourWorldHolonResult.IsError && ourWorldHolonResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Sub-Holon Saved Successfully.");
                    STARCLI.Holons.ShowHolonProperties(ourWorldHolonResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Saving Test Sub-Holon. Reason: {ourWorldHolonResult.Message}");


                //And then saving that new Zome to Our World using AddZomeAsync
                CLIEngine.ShowWorkingMessage("Saving Test Zome To Our World...");
                OASISResult<IZome> ourWorldZomeResult = await _superWorld.CelestialBodyCore.AddZomeAsync(zome);

                if (ourWorldZomeResult != null && !ourWorldZomeResult.IsError && ourWorldZomeResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Zome Saved Successfully.");
                    STARCLI.Holons.ShowHolonProperties(ourWorldZomeResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Saving Test Zome. Reason: {ourWorldZomeResult.Message}");


                //Example of adding zomes/holons in-memory and then saving all in one batch/atomic operation.
                CLIEngine.ShowWorkingMessage("Saving Test Zome 2 To Our World...");
                zome = new Zome() { Name = "Our World Test Zome 2" };
                zome.Children.Add(new Holon() { Name = "Our World Test Zome 2 Sub-Holon 2" });

                _superWorld.CelestialBodyCore.Zomes.Add(zome);

                CLIEngine.ShowWorkingMessage("Saving Our World...");
                OASISResult<ICelestialBody> ourWorldResult = await _superWorld.SaveAsync(); //Will also save the custom properties we set earlier (above).

                if (ourWorldResult != null && !ourWorldResult.IsError && ourWorldResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Zome 2 Saved Successfully.");
                    STARCLI.Holons.ShowHolonProperties(ourWorldResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Saving Test Zome 2. Reason: {ourWorldZomeResult.Message}");


                //Re-load the Our World planet to show the new zomes/holons added.
                await STARCLI.CelestialBodies.LoadCelestialBodyAsync(_superWorld, "Our World Planet");
                await STARCLI.Holons.LoadHolonAsync(_superWorld.Id.ToString(), "Our World Planet");


                CLIEngine.ShowWorkingMessage("Saving Generic Test Zome...");
                zome = new Zome() { Name = "Generic Test Zome 2" };
                OASISResult<IZome> zomeResult = await zome.SaveAsync();

                if (zomeResult != null && !zomeResult.IsError && zomeResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Zome 2 Saved Successfully.");
                    STARCLI.Holons.ShowHolonProperties(zomeResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Saving Test Zome 2. Reason: {zomeResult.Message}");



                //Example saving using Save on the Our World Core GlobalHolonData (shortcut to the Data API below), shows how ALL holons are connected through the cores...
                Holon newHolon = new Holon();
                newHolon.Name = "Test Data";
                newHolon.Description = "Test Desc";
                newHolon.HolonType = HolonType.Park;

                CLIEngine.ShowWorkingMessage("Saving Generic Test Holon...");
                OASISResult<IHolon> holonResult = await result.Result.CelestialBody.CelestialBodyCore.GlobalHolonData.SaveHolonAsync(newHolon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Holon Saved Successfully.");
                    STARCLI.Holons.ShowHolonProperties(holonResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Saving Test Holon. Reason: {holonResult.Message}");


                CLIEngine.ShowWorkingMessage("Loading Generic Test Holon...");
                OASISResult<IHolon> holonLoadResult = await newHolon.LoadAsync();

                if (!holonLoadResult.IsError && holonLoadResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Holon Loaded Successfully.");
                    STARCLI.Holons.ShowHolonProperties(holonLoadResult.Result);
                    //ShowHolonProperties(newHolon); //Can use either this line or the one above.
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Loading Test Holon. Reason: {holonLoadResult.Message}");

                newHolon = new Holon();
                newHolon.Name = "Test Data2";
                newHolon.Description = "Test Desc2";
                newHolon.HolonType = HolonType.Restaurant;


                //Example saving using Save direct on holon
                CLIEngine.ShowWorkingMessage("Saving Generic Test Holon 2...");
                holonResult = await newHolon.SaveAsync();

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Holon 2 Saved Successfully.");
                    STARCLI.Holons.ShowHolonProperties(holonResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Saving Test Holon 2. Reason: {holonResult.Message}");


                CLIEngine.ShowWorkingMessage("Loading Generic Test Holon 2...");
                holonLoadResult = await newHolon.LoadAsync();

                if (!holonLoadResult.IsError && holonLoadResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Holon 2 Loaded Successfully.");
                    //ShowHolonProperties(holonLoadResult.Result); //Can use either this line or the one below.
                    STARCLI.Holons.ShowHolonProperties(newHolon);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Loading Test Holon 2. Reason: {holonLoadResult.Message}");


                //Example saving using the Data API
                newHolon = new Holon();
                newHolon.Name = "Test Data3";
                newHolon.Description = "Test Desc3";
                newHolon.HolonType = HolonType.BusStation;

                CLIEngine.ShowWorkingMessage("Saving Generic Test Holon 3...");
                OASISResult<IHolon> holonResult2 = await STAR.OASISAPI.Data.SaveHolonAsync(newHolon, STAR.BeamedInAvatar.Id);

                if (!holonResult2.IsError && holonResult2.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Holon 3 Saved Successfully.");
                    STARCLI.Holons.ShowHolonProperties(holonResult2.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Saving Test Holon 3. Reason: {holonResult2.Message}");


                CLIEngine.ShowWorkingMessage("Loading Generic Test Holon 3...");
                holonLoadResult = await newHolon.LoadAsync();

                if (!holonLoadResult.IsError && holonLoadResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("Test Holon 3 Loaded Successfully.");
                    STARCLI.Holons.ShowHolonProperties(holonLoadResult.Result);
                    //ShowHolonProperties(newHolon); //Can use either this line or the one above.
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Loading Test Holon 3. Reason: {holonLoadResult.Message}");


                //await RunOASISAPTests(newHolon);


                // Build
                ICoronalEjection ejection = result.Result.CelestialBody.Flare();
                //OR
                //CoronalEjection ejection = Star.Flare(ourWorld);

                // Activate & Launch - Launch & activate the planet (OApp) by shining the star's light upon it...
                STAR.Shine(result.Result.CelestialBody);
                result.Result.CelestialBody.Shine();

                // Deactivate the planet (OApp)
                STAR.Dim(result.Result.CelestialBody);

                // Deploy the planet (OApp)
                //STAR.Seed(result.Result.CelestialBody.Id, ""); //TODO: Need to create test path for this.

                // Run Tests
                STAR.Twinkle(result.Result.CelestialBody);

                // Highlight the Planet (OApp) in the OApp Store (StarNET). *Admin Only*
                STAR.Radiate(result.Result.CelestialBody);

                // Show how much light the planet (OApp) is emitting into the solar system (StarNET/HoloNET)
                STAR.Emit(result.Result.CelestialBody);

                // Show stats of the Planet (OApp).
                STAR.Reflect(result.Result.CelestialBody);

                // Upgrade/update a Planet (OApp).
                STAR.Evolve(result.Result.CelestialBody);

                // Import/Export hApp, dApp & others.
                STAR.Mutate(result.Result.CelestialBody);

                // Send/Receive Love
                STAR.Love(result.Result.CelestialBody);

                // Show network stats/management/settings
                STAR.Burst(result.Result.CelestialBody);

                // Reserved For Future Use...
                STAR.Super(result.Result.CelestialBody);

                // Delete a planet (OApp).
                STAR.Dust(result.Result.CelestialBody);
            }
        }

        /// <summary>
        /// This is a good example of how to programatically interact with the OASIS API including scripting etc...
        /// </summary>
        /// <returns></returns>
        public async Task RunOASISAPTests()
        {
            // BEGIN OASIS API DEMO ***********************************************************************************
            CLIEngine.ShowWorkingMessage("BEGINNING OASIS API TEST'S...");

            CLIEngine.ShowWorkingMessage("Beginning Wallet/Key API Tests...");

            CLIEngine.ShowWorkingMessage("Linking Public Key to Solana Wallet...");
            OASISResult<Guid> keyLinkResult = STAR.OASISAPI.Keys.LinkProviderPublicKeyToAvatarByEmail(Guid.Empty, "davidellams@hotmail.com", ProviderType.SolanaOASIS, "TEST PUBLIC KEY");

            if (!keyLinkResult.IsError && keyLinkResult.Result != Guid.Empty)
                CLIEngine.ShowSuccessMessage($"Successfully linked public key to Solana Wallet. WalletID: {keyLinkResult.Result}");
            else
                CLIEngine.ShowErrorMessage($"Error occured linking key. Reason: {keyLinkResult.Message}");


            CLIEngine.ShowWorkingMessage("Linking Private Key to Solana Wallet...");
            keyLinkResult = STAR.OASISAPI.Keys.LinkProviderPrivateKeyToAvatarByEmail(keyLinkResult.Result, "davidellams@hotmail.com", ProviderType.SolanaOASIS, "TEST PRIVATE KEY");

            if (!keyLinkResult.IsError && keyLinkResult.Result != Guid.Empty)
                CLIEngine.ShowSuccessMessage($"Successfully linked private key to Solana Wallet. WalletID: {keyLinkResult.Result}");
            else
                CLIEngine.ShowErrorMessage($"Error occured linking key. Reason: {keyLinkResult.Message}");


            CLIEngine.ShowWorkingMessage("Generating KeyPair & Linking to EOS Wallet...");
            OASISResult<KeyPair> generateKeyPairResult = STAR.OASISAPI.Keys.GenerateKeyPairAndLinkProviderKeysToAvatarByEmail("davidellams@hotmail.com", ProviderType.EOSIOOASIS, true, true);

            if (!generateKeyPairResult.IsError && generateKeyPairResult.Result != null)
                CLIEngine.ShowSuccessMessage($"Successfully generated new keypair and linked to EOS Wallet. Public Key: {generateKeyPairResult.Result.PublicKey}, Private Key: {generateKeyPairResult.Result.PrivateKey}");
            else
                CLIEngine.ShowErrorMessage($"Error occured generating keypair. Reason: {generateKeyPairResult.Message}");

            CLIEngine.ShowWorkingMessage("Getting all Provider Public Keys For Avatar...");
            OASISResult<Dictionary<ProviderType, List<string>>> keysResult = STAR.OASISAPI.Keys.GetAllProviderPublicKeysForAvatarByEmail("davidellams@hotmail.com");

            if (!keysResult.IsError && keysResult.Result != null)
            {
                string message = "";
                foreach (ProviderType providerType in keysResult.Result.Keys)
                {
                    foreach (string key in keysResult.Result[providerType])
                        message = string.Concat(message, providerType.ToString(), ": ", key, "\n");
                }

                CLIEngine.ShowSuccessMessage($"Successfully retreived keys:\n{message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured getting keys. Reason: {keysResult.Message}");


            CLIEngine.ShowWorkingMessage("Getting all Provider Private Keys For Avatar...");
            keysResult = STAR.OASISAPI.Keys.GetAllProviderPrivateKeysForAvatarByUsername("davidellams@hotmail.com");

            if (!keysResult.IsError && keysResult.Result != null)
            {
                string message = "";
                foreach (ProviderType providerType in keysResult.Result.Keys)
                {
                    foreach (string key in keysResult.Result[providerType])
                        message = string.Concat(message, providerType.ToString(), ": ", key, "\n");
                }

                CLIEngine.ShowSuccessMessage($"Successfully retreived keys\n{message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured getting keys. Reason: {keysResult.Message}");


            CLIEngine.ShowWorkingMessage("Getting all Provider Unique Storage Keys For Avatar...");
            OASISResult<Dictionary<ProviderType, string>> uniqueKeysResult = STAR.OASISAPI.Keys.GetAllProviderUniqueStorageKeysForAvatarByEmail("davidellams@hotmail.com");

            if (!uniqueKeysResult.IsError && uniqueKeysResult.Result != null)
            {
                string message = "";
                foreach (ProviderType providerType in uniqueKeysResult.Result.Keys)
                    message = string.Concat(message, providerType.ToString(), ": ", uniqueKeysResult.Result[providerType], "\n");

                CLIEngine.ShowSuccessMessage($"Successfully retreived keys:\n{message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured getting keys. Reason: {uniqueKeysResult.Message}");


            CLIEngine.ShowSuccessMessage("Wallet/Key API Tests Complete.");

            // Console.WriteLine("Press Any Key To Continue...");
            // Console.ReadKey();

            //Set auto-replicate for all providers except IPFS and Neo4j.
            //EnableOrDisableAutoProviderList(ProviderManager.Instance.SetAutoReplicateForAllProviders, true, null, "Enabling Auto-Replication For All Providers...", "Auto-Replication Successfully Enabled For All Providers.", "Error Occured Enabling Auto-Replication For All Providers.");
            CLIEngine.ShowWorkingMessage("Enabling Auto-Replication For All Providers...");
            bool isSuccess = ProviderManager.Instance.SetAutoReplicateForAllProviders(true);
            ErrorHandling.HandleBooleansResponse(isSuccess, "Auto-Replication Successfully Enabled For All Providers.", "Error Occured Enabling Auto-Replication For All Providers.");

            CLIEngine.ShowWorkingMessage("Disabling Auto-Replication For IPFSOASIS & Neo4jOASIS Providers...");
            isSuccess = ProviderManager.Instance.SetAutoReplicationForProviders(false, new List<ProviderType>() { ProviderType.IPFSOASIS, ProviderType.Neo4jOASIS });
            //EnableOrDisableAutoProviderList(ProviderManager.Instance.SetAutoReplicationForProviders, false, new List<ProviderType>() { ProviderType.IPFSOASIS, ProviderType.Neo4jOASIS }, "Enabling Auto-Replication For All Providers...", "Auto-Replication Successfully Enabled For All Providers.", "Error Occured Enabling Auto-Replication For All Providers.");
            ErrorHandling.HandleBooleansResponse(isSuccess, "Auto-Replication Successfully Disabled For IPFSOASIS & Neo4jOASIS Providers.", "Error Occured Disabling Auto-Replication For IPFSOASIS & Neo4jOASIS Providers.");

            //Set auto-failover for all providers except Holochain.
            CLIEngine.ShowWorkingMessage("Enabling Auto-FailOver For All Providers...");
            isSuccess = ProviderManager.Instance.SetAutoFailOverForAllProviders(true);
            ErrorHandling.HandleBooleansResponse(isSuccess, "Auto-FailOver Successfully Enabled For All Providers.", "Error Occured Enabling Auto-FailOver For All Providers.");

            CLIEngine.ShowWorkingMessage("Disabling Auto-FailOver For HoloOASIS Provider...");
            isSuccess = ProviderManager.Instance.SetAutoFailOverForProviders(false, new List<ProviderType>() { ProviderType.HoloOASIS });
            ErrorHandling.HandleBooleansResponse(isSuccess, "Auto-FailOver Successfully Disabled For HoloOASIS.", "Error Occured Disabling Auto-FailOver For HoloOASIS Provider.");

            //Set auto-load balance for all providers except Ethereum.
            CLIEngine.ShowWorkingMessage("Enabling Auto-Load-Balancing For All Providers...");
            isSuccess = ProviderManager.Instance.SetAutoLoadBalanceForAllProviders(true);
            ErrorHandling.HandleBooleansResponse(isSuccess, "Auto-FailOver Successfully Disabled For HoloOASIS.", "Error Occured Disabling Auto-FailOver For HoloOASIS Provider.");

            CLIEngine.ShowWorkingMessage("Disabling Auto-Load-Balancing For EthereumOASIS Provider...");
            isSuccess = ProviderManager.Instance.SetAutoLoadBalanceForProviders(false, new List<ProviderType>() { ProviderType.EthereumOASIS });
            ErrorHandling.HandleBooleansResponse(isSuccess, "Auto-Load-Balancing Successfully Disabled For EthereumOASIS.", "Error Occured Disabling Auto-Load-Balancing For EthereumOASIS Provider.");

            // Set the default provider to MongoDB.
            // Set last param to false if you wish only the next call to use this provider.
            CLIEngine.ShowWorkingMessage("Setting Default Provider to MongoDBOASIS...");
            //HandleOASISResponse(ProviderManager.Instance.SetAndActivateCurrentStorageProvider(ProviderType.MongoDBOASIS, true), "Successfully Set Default Provider To MongoDBOASIS Provider.", "Error Occured Setting Default Provider To MongoDBOASIS.");
            ErrorHandling.HandleOASISResponse(await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(ProviderType.MongoDBOASIS, true), "Successfully Set Default Provider To MongoDBOASIS Provider.", "Error Occured Setting Default Provider To MongoDBOASIS.");

            //  Give HoloOASIS Store permission for the Name field(the field will only be stored on Holochain).
            CLIEngine.ShowWorkingMessage("Granting HoloOASIS Provider Store Permission For The Name Field...");
            STAR.OASISAPI.Avatars.Config.FieldToProviderMappings.Name.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });
            CLIEngine.ShowSuccessMessage("Permission Granted.");

            // Give all providers read/write access to the Karma field (will allow them to read and write to the field but it will only be stored on Holochain).
            // You could choose to store it on more than one provider if you wanted the extra redundancy (but not normally needed since Holochain has a lot of redundancy built in).
            CLIEngine.ShowWorkingMessage("Granting All Providers Read/Write Permission For The Karma Field...");
            STAR.OASISAPI.Avatars.Config.FieldToProviderMappings.Karma.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.All });
            CLIEngine.ShowSuccessMessage("Permission Granted.");

            //Give Ethereum read-only access to the DOB field.
            CLIEngine.ShowWorkingMessage("Granting EthereumOASIS Providers Read-Only Permission For The DOB Field...");
            STAR.OASISAPI.Avatars.Config.FieldToProviderMappings.DOB.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadOnly, Provider = ProviderType.EthereumOASIS });
            CLIEngine.ShowSuccessMessage("Permission Granted.");

            // All calls are load-balanced and have multiple redudancy/fail over for all supported OASIS Providers.
            CLIEngine.ShowWorkingMessage("Loading All Avatars Load Balanced Across All Providers...");
            OASISResult<IEnumerable<IAvatar>> avatarsResult = STAR.OASISAPI.Avatars.LoadAllAvatars(); // Load-balanced across all providers.

            if (!avatarsResult.IsError && avatarsResult.Result != null)
                CLIEngine.ShowSuccessMessage($"{avatarsResult.Result.Count()} Avatars Loaded.");
            else
                CLIEngine.ShowErrorMessage($"Error occured loading avatars. Reason: {avatarsResult.Message}");

            CLIEngine.ShowWorkingMessage("Loading All Avatars Only For The MongoDBOASIS Provider...");
            avatarsResult = STAR.OASISAPI.Avatars.LoadAllAvatars(false, true, true, ProviderType.MongoDBOASIS); // Only loads from MongoDB.

            if (!avatarsResult.IsError && avatarsResult.Result != null)
                CLIEngine.ShowSuccessMessage($"{avatarsResult.Result.Count()} Avatars Loaded.");
            else
                CLIEngine.ShowErrorMessage($"Error occured loading avatars. Reason: {avatarsResult.Message}");

            CLIEngine.ShowWorkingMessage("Loading Avatar Only For The HoloOASIS Provider...");
            OASISResult<IAvatar> avatarResult = STAR.OASISAPI.Avatars.LoadAvatar(STAR.BeamedInAvatar.Id, false, true, ProviderType.HoloOASIS); // Only loads from Holochain.

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                CLIEngine.ShowSuccessMessage("Avatar Loaded Successfully");
                CLIEngine.ShowSuccessMessage($"Avatar ID: {avatarResult.Result.Id}");
                CLIEngine.ShowSuccessMessage($"Avatar Name: {avatarResult.Result.FullName}");
                CLIEngine.ShowSuccessMessage($"Avatar Created Date: {avatarResult.Result.CreatedDate}");
                CLIEngine.ShowSuccessMessage($"Avatar Last Beamed In Date: {avatarResult.Result.LastBeamedIn}");
            }
            else
                CLIEngine.ShowErrorMessage("Error Loading Avatar.");


            Holon newHolon = new Holon();
            newHolon.Name = "Test Data";
            newHolon.Description = "Test Desc";
            newHolon.HolonType = HolonType.Park;

            CLIEngine.ShowWorkingMessage("Saving Test Holon (Load Balanced Across All Providers)...");
            ErrorHandling.HandleOASISResponse(STAR.OASISAPI.Data.SaveHolon(newHolon, STAR.BeamedInAvatar.Id), "Holon Saved Successfully.", "Error Saving Holon."); // Load-balanced across all providers.

            CLIEngine.ShowWorkingMessage("Saving Test Holon Only For The EthereumOASIS Provider...");
            ErrorHandling.HandleOASISResponse(STAR.OASISAPI.Data.SaveHolon(newHolon, STAR.BeamedInAvatar.Id, true, true, 0, true, false, ProviderType.EthereumOASIS), "Holon Saved Successfully.", "Error Saving Holon."); //  Only saves to Etherum.


            CLIEngine.ShowWorkingMessage("Creating & Drawing Route On Map Between 2 Test Holons (Load Balanced Across All Providers)...");
            ErrorHandling.HandleBooleansResponse(STAR.STARAPI.Map.CreateAndDrawRouteOnMapBetweenHolons(newHolon, newHolon), "Route Created Successfully.", "Error Creating Route."); // Load-balanced across all providers.

            CLIEngine.ShowWorkingMessage("Loading Test Holon (Load Balanced Across All Providers)...");
            OASISResult<IHolon> holonResult = STAR.OASISAPI.Data.LoadHolon(newHolon.Id); // Load-balanced across all providers.

            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
            {
                CLIEngine.ShowSuccessMessage("Holon Loaded Successfully.");
                CLIEngine.ShowSuccessMessage($"Id: {holonResult.Result.Id}");
                CLIEngine.ShowSuccessMessage($"Name: {holonResult.Result.Name}");
                CLIEngine.ShowSuccessMessage($"Description: {holonResult.Result.Description}");
            }
            else
                CLIEngine.ShowErrorMessage("Error Loading Holon");

            CLIEngine.ShowWorkingMessage("Loading Test Holon Only For IPFSOASIS Provider...");
            holonResult = STAR.OASISAPI.Data.LoadHolon(newHolon.Id, true, true, 0, true, false, HolonType.All, 0, ProviderType.IPFSOASIS); // Only loads from IPFS.

            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
            {
                CLIEngine.ShowSuccessMessage("Holon Loaded Successfully.");
                CLIEngine.ShowSuccessMessage($"Id: {holonResult.Result.Id}");
                CLIEngine.ShowSuccessMessage($"Name: {holonResult.Result.Name}");
                CLIEngine.ShowSuccessMessage($"Description: {holonResult.Result.Description}");
            }
            else
                CLIEngine.ShowErrorMessage("Error Loading Holon");

            CLIEngine.ShowWorkingMessage("Loading All Holons Of Type Moon Only For HoloOASIS Provider...");
            ErrorHandling.HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.Moon, true, true, 0, true, false, HolonType.All, 0, ProviderType.HoloOASIS)); // Loads all moon (OAPPs) from Holochain.

            CLIEngine.ShowWorkingMessage("Loading All Holons From The Current Default Provider (With Auto-FailOver)...");
            ErrorHandling.HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.All, true, true, 0, true, false, HolonType.All, 0, ProviderType.Default)); // Loads all holons from current default provider.

            CLIEngine.ShowWorkingMessage("Loading All Park Holons From All Providers (With Auto-Load-Balance & Auto-FailOver)...");
            ErrorHandling.HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.Park, true, true, 0, true, false, HolonType.All, 0, ProviderType.All)); // Loads all parks from all providers (load-balanced/fail over).

            //CLIEngine.ShowWorkingMessage("Loading All Park Holons From All Providers (With Auto-Load-Balance & Auto-FailOver)...");
            STAR.OASISAPI.Data.LoadAllHolons(HolonType.Park); // shorthand for above.

            CLIEngine.ShowWorkingMessage("Loading All Quest Holons From All Providers (With Auto-Load-Balance & Auto-FailOver)...");
            ErrorHandling.HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.Quest)); //  Loads all quests from all providers.

            CLIEngine.ShowWorkingMessage("Loading All Restaurant Holons From All Providers (With Auto-Load-Balance & Auto-FailOver)...");
            ErrorHandling.HandleHolonsOASISResponse(STAR.OASISAPI.Data.LoadAllHolons(HolonType.Restaurant)); //  Loads all resaurants from all providers.

            // Holochain Support

            try
            {
                CLIEngine.ShowWorkingMessage("Initiating Holochain Tests...");

                if (!STAR.OASISAPI.Providers.Holochain.IsProviderActivated)
                {
                    CLIEngine.ShowWorkingMessage("Activating Holochain Provider...");
                    STAR.OASISAPI.Providers.Holochain.ActivateProvider();
                    CLIEngine.ShowSuccessMessage("Holochain Provider Activated.");
                }

                CLIEngine.ShowWorkingMessage("Loading Avatar By Email...");
                OASISResult<IAvatar> avatarResultHolochain = STAR.OASISAPI.Providers.Holochain.LoadAvatarByEmail("davidellams@hotmail.com");

                if (!avatarResultHolochain.IsError && avatarResultHolochain.Result != null)
                    CLIEngine.ShowSuccessMessage($"Avatar Loaded Successfully. Id: {avatarResultHolochain.Result.Id}");

                CLIEngine.ShowWorkingMessage("Calling Test Zome Function on HoloNET Client...");
                //await STAR.OASISAPI.Providers.Holochain.HoloNETClient.CallZomeFunctionAsync(STAR.OASISAPI.Providers.Holochain.HoloNETClient.Config.AgentPubKey, "our_world_core", "load_holons", null);
                await STAR.OASISAPI.Providers.Holochain.HoloNETClientAppAgent.CallZomeFunctionAsync("our_world_core", "load_holons", null);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error occured during Holochain Tests: {ex.Message}");
            }

            CLIEngine.ShowSuccessMessage("Holochain Tests Completed.");

            // IPFS Support
            try
            {
                CLIEngine.ShowWorkingMessage("Initiating IPFS Tests...");

                if (!STAR.OASISAPI.Providers.IPFS.IsProviderActivated)
                {
                    CLIEngine.ShowWorkingMessage("Activating IPFS Provider...");
                    STAR.OASISAPI.Providers.IPFS.ActivateProvider();
                    CLIEngine.ShowSuccessMessage("IPFS Provider Activated.");
                }

                IFileSystemNode result = await STAR.OASISAPI.Providers.IPFS.IPFSClient.FileSystem.AddTextAsync("TEST");
                CLIEngine.ShowMessage($"Id of IPFS Write Test: {result.Id}");
                CLIEngine.ShowMessage($"Data Writen for IPFS Write Test: {result.DataBytes.Length} bytes");

                string ipfsResult = await STAR.OASISAPI.Providers.IPFS.IPFSClient.FileSystem.ReadAllTextAsync(result.Id);
                CLIEngine.ShowMessage($"IPFS Read Result: {ipfsResult}");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error occured during IPFS Tests: {ex.Message}");
            }

            CLIEngine.ShowSuccessMessage("IPFS Tests Completed.");

            // Ethereum Support
            try
            {
                CLIEngine.ShowWorkingMessage("Initiating Ethereum Tests...");

                if (!STAR.OASISAPI.Providers.Ethereum.IsProviderActivated)
                {
                    CLIEngine.ShowWorkingMessage("Activating Ethereum Provider...");
                    STAR.OASISAPI.Providers.Ethereum.ActivateProvider();
                    CLIEngine.ShowSuccessMessage("Ethereum Provider Activated.");
                }

                await STAR.OASISAPI.Providers.Ethereum.Web3Client.Client.SendRequestAsync(new Nethereum.JsonRpc.Client.RpcRequest("id", "test"));
                await STAR.OASISAPI.Providers.Ethereum.Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync("");
                //Contract contract = STAR.OASISAPI.Providers.Ethereum.Web3Client.Eth.GetContract("abi", "contractAddress");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error occured during Ethereum Tests: {ex.Message}");
            }

            CLIEngine.ShowSuccessMessage("Ethereum Tests Completed.");

            // EOSIO Support
            try
            {
                CLIEngine.ShowWorkingMessage("Initiating EOSIO Tests...");

                if (!STAR.OASISAPI.Providers.EOSIO.IsProviderActivated)
                {
                    CLIEngine.ShowWorkingMessage("Activating EOSIO Provider...");
                    STAR.OASISAPI.Providers.EOSIO.ActivateProvider();
                    CLIEngine.ShowSuccessMessage("EOSIO Provider Activated.");
                }

                STAR.OASISAPI.Providers.EOSIO.ChainAPI.GetTableRows("accounts", "accounts", "users", "true", 0, 0, 1, 3);
                STAR.OASISAPI.Providers.EOSIO.ChainAPI.GetBlock("block");
                STAR.OASISAPI.Providers.EOSIO.ChainAPI.GetAccount("test.account");
                STAR.OASISAPI.Providers.EOSIO.ChainAPI.GetCurrencyBalance("test.account", "", "");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error occured during EOSIO Tests: {ex.Message}");
            }

            CLIEngine.ShowSuccessMessage("EOSIO Tests Completed.");

            // Graph DB Support
            try
            {
                CLIEngine.ShowWorkingMessage("Initiating Neo4j (Graph DB) Tests...");

                if (!STAR.OASISAPI.Providers.Neo4j.IsProviderActivated)
                {
                    CLIEngine.ShowWorkingMessage("Activating Neo4j Provider...");
                    STAR.OASISAPI.Providers.Neo4j.ActivateProvider();
                    CLIEngine.ShowSuccessMessage("Neo4j Provider Activated.");
                }

                CLIEngine.ShowWorkingMessage("Executing Graph Cypher Test...");

                var session = STAR.OASISAPI.Providers.Neo4j.Driver.AsyncSession();

                await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"
                             MATCH (av:Avatar)                        
                             RETURN av.FirstName AS firstname,av.LastName AS lastname"
                    );

                    IEnumerable<IAvatar> objList = await cursor.ToListAsync(record => new Avatar
                    {
                        FirstName = record["firstname"].As<string>(),
                        LastName = record["lastname"].As<string>()
                    });
                });

                //await STAR.OASISAPI.Providers.Neo4j.Driver.Cypher.Merge("(a:Avatar { Id: avatar.Id })").OnCreate().Set("a = avatar").ExecuteWithoutResultsAsync(); //Insert/Update Avatar.
                //await STAR.OASISAPI.Providers.Neo4j.GraphClient.Cypher.Merge("(a:Avatar { Id: avatar.Id })").OnCreate().Set("a = avatar").ExecuteWithoutResultsAsync(); //Insert/Update Avatar.
                //Avatar newAvatar = STAR.OASISAPI.Providers.Neo4j.GraphClient.Cypher.Match("(p:Avatar {Username: {nameParam}})").WithParam("nameParam", "davidellams@hotmail.com").Return(p => p.As<Avatar>()).ResultsAsync.Result.Single(); //Load Avatar.
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error occured during Neo4j Tests: {ex.Message}");
            }

            CLIEngine.ShowSuccessMessage("Neo4j Tests Completed.");

            // Document/Object DB Support
            try
            {
                CLIEngine.ShowWorkingMessage("Initiating MongoDB Tests...");

                if (!STAR.OASISAPI.Providers.MongoDB.IsProviderActivated)
                {
                    CLIEngine.ShowWorkingMessage("Activating MongoDB Provider...");
                    STAR.OASISAPI.Providers.MongoDB.ActivateProvider();
                    CLIEngine.ShowSuccessMessage("MongoDB Provider Activated.");
                }

                CLIEngine.ShowWorkingMessage("Listing Collction Names...");
                STAR.OASISAPI.Providers.MongoDB.Database.MongoDB.ListCollectionNames();

                CLIEngine.ShowWorkingMessage("Getting Avatar Collection...");
                //IMongoCollection<IAvatar> collection = STAR.OASISAPI.Providers.MongoDB.Database.MongoDB.GetCollection<Avatar>("Avatar");
                STAR.OASISAPI.Providers.MongoDB.Database.MongoDB.GetCollection<Avatar>("Avatar");

                //if (collection != null)
                //    CLIEngine.ShowSuccessMessage($"{collection.Coi} avatars found.");

            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error occured during MongoDB Tests: {ex.Message}");
            }

            CLIEngine.ShowSuccessMessage("MongoDB Tests Completed.");

            // SEEDS Support
            //try
            //{
            //    CLIEngine.ShowWorkingMessage("Initiating SEEDS Tests...");

            //    if (!STAR.OASISAPI.Providers.SEEDS.IsProviderActivated)
            //    {
            //        CLIEngine.ShowWorkingMessage("Activating SEEDS Provider...");
            //        STAR.OASISAPI.Providers.SEEDS.ActivateProvider();
            //        CLIEngine.ShowSuccessMessage("SEEDS Provider Activated.");
            //    }

            //    CLIEngine.ShowWorkingMessage("Getting Balance for account davidsellams...");
            //    string balance = STAR.OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("davidsellams");
            //    CLIEngine.ShowSuccessMessage(string.Concat("Balance: ", balance));

            //    CLIEngine.ShowWorkingMessage("Getting Balance for account nextgenworld...");
            //    balance = STAR.OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("nextgenworld");
            //    CLIEngine.ShowSuccessMessage(string.Concat("Balance: ", balance));

            //    CLIEngine.ShowWorkingMessage("Getting Account for account davidsellams...");
            //    GetAccountResponseDto account = STAR.OASISAPI.Providers.SEEDS.TelosOASIS.GetTelosAccount("davidsellams");

            //    if (account != null)
            //    {
            //        CLIEngine.ShowSuccessMessage(string.Concat("Account.account_name: ", account.AccountName));
            //        CLIEngine.ShowSuccessMessage(string.Concat("Account.created: ", account.Created.ToString()));
            //    }
            //    else
            //        CLIEngine.ShowErrorMessage("Account not found.");

            //    CLIEngine.ShowWorkingMessage("Getting Account for account nextgenworld...");
            //    account = STAR.OASISAPI.Providers.SEEDS.TelosOASIS.GetTelosAccount("nextgenworld");

            //    if (account != null)
            //    {
            //        CLIEngine.ShowSuccessMessage(string.Concat("Account.account_name: ", account.AccountName));
            //        CLIEngine.ShowSuccessMessage(string.Concat("Account.created: ", account.Created.ToString()));
            //    }
            //    else
            //        CLIEngine.ShowErrorMessage("Account not found.");

            //    // Check that the Telos account name is linked to the avatar and link it if it is not (PayWithSeeds will fail if it is not linked when it tries to add the karma points).
            //    if (!STAR.BeamedInAvatar.ProviderUniqueStorageKey.ContainsKey(ProviderType.TelosOASIS))
            //    {
            //        CLIEngine.ShowWorkingMessage("Linking Telos Account to Avatar...");
            //        OASISResult<Guid> linkKeyResult = STAR.OASISAPI.Keys.LinkProviderPublicKeyToAvatarById(Guid.Empty, STAR.BeamedInAvatar.Id, ProviderType.TelosOASIS, "davidsellams");

            //        if (!linkKeyResult.IsError && linkKeyResult.Result != Guid.Empty)
            //            CLIEngine.ShowSuccessMessage($"Telos Account Successfully Linked to Avatar. WalletID: {linkKeyResult.Result}");
            //        else
            //            CLIEngine.ShowErrorMessage($"Error occured Whilst Linking Telos Account To Avatar. Reason: {linkKeyResult.Message}");
            //    }

            //    CLIEngine.ShowWorkingMessage("Sending SEEDS from nextgenworld to davidsellams...");
            //    OASISResult<string> payWithSeedsResult = STAR.OASISAPI.Providers.SEEDS.PayWithSeedsUsingTelosAccount("davidsellams", _privateKey, "nextgenworld", 1, KarmaSourceType.API, "test", "test", "test", "test memo");

            //    if (payWithSeedsResult.IsError)
            //        CLIEngine.ShowErrorMessage(string.Concat("Error Occured: ", payWithSeedsResult.Message));
            //    else
            //        CLIEngine.ShowSuccessMessage(string.Concat("SEEDS Sent. Transaction ID: ", payWithSeedsResult.Result));


            //    CLIEngine.ShowWorkingMessage("Getting Balance for account davidsellams...");
            //    balance = STAR.OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("davidsellams");
            //    CLIEngine.ShowSuccessMessage(string.Concat("Balance: ", balance));

            //    CLIEngine.ShowWorkingMessage("Getting Balance for account nextgenworld...");
            //    balance = STAR.OASISAPI.Providers.SEEDS.GetBalanceForTelosAccount("nextgenworld");
            //    CLIEngine.ShowSuccessMessage(string.Concat("Balance: ", balance));

            //    CLIEngine.ShowWorkingMessage("Getting Organsiations...");
            //    string orgs = STAR.OASISAPI.Providers.SEEDS.GetAllOrganisationsAsJSON();
            //    CLIEngine.ShowSuccessMessage(string.Concat("Organisations: ", orgs));

            //    //CLIEngine.ShowErrorMessage("Getting nextgenworld organsiation...");
            //    //string org = OASISAPI.Providers.SEEDS.GetOrganisation("nextgenworld");
            //    //CLIEngine.ShowErrorMessage(string.Concat("nextgenworld org: ", org));

            //    CLIEngine.ShowWorkingMessage("Generating QR Code for davidsellams...");
            //    string qrCode = STAR.OASISAPI.Providers.SEEDS.GenerateSignInQRCode("davidsellams");
            //    CLIEngine.ShowSuccessMessage(string.Concat("SEEDS Sign-In QRCode: ", qrCode));

            //    CLIEngine.ShowWorkingMessage("Sending invite to davidsellams...");
            //    OASISResult<SendInviteResult> sendInviteResult = STAR.OASISAPI.Providers.SEEDS.SendInviteToJoinSeedsUsingTelosAccount("davidsellams", _privateKey, "davidsellams", 1, 1, KarmaSourceType.API, "test", "test", "test");
            //    CLIEngine.ShowSuccessMessage(string.Concat("Success: ", sendInviteResult.IsError ? "false" : "true"));

            //    if (sendInviteResult.IsError)
            //        CLIEngine.ShowErrorMessage(string.Concat("Error Message: ", sendInviteResult.Message));
            //    else
            //    {
            //        CLIEngine.ShowSuccessMessage(string.Concat("Invite Sent To Join SEEDS. Invite Secret: ", sendInviteResult.Result.InviteSecret, ". Transction ID: ", sendInviteResult.Result.TransactionId));

            //        CLIEngine.ShowWorkingMessage("Accepting invite to davidsellams...");
            //        OASISResult<string> acceptInviteResult = STAR.OASISAPI.Providers.SEEDS.AcceptInviteToJoinSeedsUsingTelosAccount("davidsellams", sendInviteResult.Result.InviteSecret, KarmaSourceType.API, "test", "test", "test");
            //        CLIEngine.ShowSuccessMessage(string.Concat("Success: ", acceptInviteResult.IsError ? "false" : "true"));

            //        if (acceptInviteResult.IsError)
            //            CLIEngine.ShowErrorMessage(string.Concat("Error Message: ", acceptInviteResult.Message));
            //        else
            //            CLIEngine.ShowSuccessMessage(string.Concat("Invite Accepted To Join SEEDS. Transction ID: ", acceptInviteResult.Result));
            //    }
            //}
            //catch (Exception ex)
            //{
            //    CLIEngine.ShowErrorMessage($"Error occured during SEEDS Tests: {ex.Message}");
            //}

            //CLIEngine.ShowSuccessMessage("SEEDS Tests Completed.");


            // ThreeFold, AcivityPub, SOLID, Cross/Off Chain, Smart Contract Interoperability & lots more coming soon! :)

            CLIEngine.ShowSuccessMessage("OASIS API TESTS COMPLETE.");
            // END OASIS API DEMO ***********************************************************************************
        }

        private void CelestialBody_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Celestial Body Zome Error: {e.Result.Message}");
        }

        private void CelestialBody_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            CLIEngine.ShowSuccessMessage($"Celestial Body Holon Saved: {e.Result.Message} Name: {e.Result.Result.Name}, Id: {e.Result.Result.Id}, Type: {Enum.GetName(typeof(HolonType), e.Result.Result.HolonType)}");
        }

        private void CelestialBody_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            CLIEngine.ShowSuccessMessage($"Celestial Body Holon Loaded: {e.Result.Message}. Name: {e.Result.Result.Name}, Id: {e.Result.Result.Id}, Type: {Enum.GetName(typeof(HolonType), e.Result.Result.HolonType)}");
        }
    }
}