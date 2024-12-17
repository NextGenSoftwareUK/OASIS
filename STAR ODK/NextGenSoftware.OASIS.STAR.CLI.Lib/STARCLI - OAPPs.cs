using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static partial class STARCLI
    {
        //Used for the tests:
        private static Planet _superWorld;
        private static Moon _jlaMoon;
        private static string _privateKey = "";

        public static async Task LightWizardAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<CoronalEjection> lightResult = null;

            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage("Welcome to the OASIS Omniverse/MagicVerse Light Wizard!");
            CLIEngine.ShowDivider();
            Console.WriteLine();
            CLIEngine.ShowMessage("This wizard will allow you create an OAPP (Moon, Planet, Star & More) which will appear in the MagicVerse within the OASIS Omniverse.", false);
            CLIEngine.ShowMessage("The OAPP will also optionally appear within the AR geo-location Our World/AR World platform/game in your desired geo-location.");
            CLIEngine.ShowMessage("The OAPP will also optionally appear within the One World (Open World MMORPG) game/platform. VR support is also provided.");
            CLIEngine.ShowMessage("The OAPP can have as many interfaces/lenses (way to visualize/represent the data of your OAPP) as you like, for example you can also have a 2D web view as well as a 3D view, Metaverse/Omniverse view, etc.");
            CLIEngine.ShowMessage("Each OAPP is composed of zomes (re-usable/composable modules containing collections of holons) & holons (generic/composable re-usable OASIS Data Objects). This means the zomes and holons can be shared and re-used with other OAPPs within the STARNET Library. Different zomes and holons can be plugged together to form unique combinations for new OAPPs saving lots of time!");
            CLIEngine.ShowMessage("Each OAPP is built/generated on top of a powerful easy to use ORM called (WEB5) COSMIC (The Worlds ORM because it aggregrates all of the worlds data into a simple to use ORM) which allows very easy data management across all of web2 and web3 making data interoperability and interchange very simple and makes silos a thing of the past!");
            CLIEngine.ShowMessage("COSMIC is built on top of the powerful WEB4 OASIS API so each OAPP also has easy to use API's for manging keys, wallets, data, nfts, geo-nfts, providers, avatars, karma & much more!");
            CLIEngine.ShowMessage("A OAPP can be anything you want such as a website, game, app, service, api, protocol or anything else that a template exists for!");
            CLIEngine.ShowMessage("Data can be shared between OAPP's but you are always in full control of your data, you own your data and you can choose exactly who and how that data is shared. You have full data sovereignty.");
            CLIEngine.ShowMessage("Due to your OAPP being built on the OASIS API you also benefit from many other advanced features such as auto-replication, auto-failover and auto-load balancing so if one node goes down in your local area it will automatically find the next fastest one in your area irrespective of network.");
            CLIEngine.ShowMessage("The more users your OAPP has the larger that celestial body (moon, planet or star) will appear within The MagicVerse. The higher the karma score of the owner (can be a individual or company/organisation) of the OAPP becomes the closer that celestial bodies orbit will be to it's parent so if it's a moon it will get closer and closer to the planet and if it's a planet it will get closer and closer to it's star.");
            CLIEngine.ShowDivider();

            string OAPPName = CLIEngine.GetValidInput("What is the name of the OAPP?");

            if (OAPPName == "exit")
                return;

            string OAPPDesc = CLIEngine.GetValidInput("What is the description of the OAPP?");

            if (OAPPDesc == "exit")
                return;

            object value = CLIEngine.GetValidInputForEnum("What type of OAPP do you wish to create?", typeof(OAPPType));
            long ourWorldLat = 0;
            long ourWorldLong = 0;
            long oneWorlddLat = 0;
            long oneWorldLong = 0;
            string ourWorld3dObjectPath = "";
            byte[] ourWorld3dObject = null;
            Uri ourWorld3dObjectURI = null;
            string ourWorld2dSpritePath = "";
            byte[] ourWorld2dSprite = null;
            Uri ourWorld2dSpriteURI = null;

            string oneWorld3dObjectPath = "";
            byte[] oneWorld3dObject = null;
            Uri oneWorld3dObjectURI = null;
            string oneWorld2dSpritePath = "";
            byte[] oneWorld2dSprite = null;
            Uri oneWorld2dSpriteURI = null;

            if (value != null)
            {
                if (value.ToString() == "exit")
                    return;

                OAPPType OAPPType = (OAPPType)value;

                //TODO: I think star bang was going to be used to create non OAPP Celestial bodies or spaces outside of the magic verse.
                //if (CLIEngine.GetConfirmation("Do you wish the OAPP to be part of the MagicVerse within the OASIS Omniverse (will optionally appear in Our World/AR World)? If you say yes then new avatars will only be able to create moons that orbit Our World until you reach karma level 33 where you will then be able to create planets, when you reach level 77 you can create stars. If you select no then you can create whatever you like outside of the MagicVerse but it will still be within the OASIS Omniverse."))
                //{

                //}


                if (CLIEngine.GetConfirmation("Do you wish for your OAPP to appear in the AR geo-location Our World/AR World game/platform? (recommeneded)"))
                {
                    Console.WriteLine("");
                    ourWorldLat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your OAPP to appear in Our World/AR World?");

                    if (ourWorldLat == -1)
                        return;
                    
                    ourWorldLong = CLIEngine.GetValidInputForLong("What is the long geo-location you wish for your OAPP to appear in Our World/AR World?");

                    if (ourWorldLong == -1)
                        return;

                    if (CLIEngine.GetConfirmation("Would you rather use a 3D object or a 2D sprite/image to represent your OAPP? Press Y for 3D or N for 2D."))
                    {
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation("Would you like to upload a local 3D object from your device or input a URI to an online object? (Press Y for local or N for online)"))
                        {
                            Console.WriteLine("");
                            ourWorld3dObjectPath = CLIEngine.GetValidFile("What is the full path to the local 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                            if (ourWorld3dObjectPath == "exit")
                                return;
                            
                            ourWorld3dObject = File.ReadAllBytes(ourWorld3dObjectPath);

                        }
                        else
                        {
                            Console.WriteLine("");
                            ourWorld3dObjectURI = await CLIEngine.GetValidURIAsync("What is the URI to the 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                            if (ourWorld3dObjectURI == null)
                                return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation("Would you like to upload a local 2D sprite/image from your device or input a URI to an online sprite/image? (Press Y for local or N for online)"))
                        {
                            Console.WriteLine("");
                            ourWorld2dSpritePath = CLIEngine.GetValidFile("What is the full path to the local 2d sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");

                            if (ourWorld2dSpritePath == "exit")
                                return;

                            ourWorld2dSprite = File.ReadAllBytes(ourWorld2dSpritePath);
                        }
                        else
                        {
                            Console.WriteLine("");
                            ourWorld2dSpriteURI = await CLIEngine.GetValidURIAsync("What is the URI to the 2D sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");

                            if (ourWorld3dObjectURI == null)
                                return;
                        }
                    }
                }
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation("Do you wish for your OAPP to appear in the Open World MMORPG One World game/platform? (recommeneded)"))
                {
                    Console.WriteLine("");
                    oneWorlddLat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your OAPP to appear in One World?");

                    if (oneWorlddLat == -1)
                        return;
                    
                    oneWorldLong = CLIEngine.GetValidInputForLong("What is the long geo-location you wish for your OAPP to appear in One World?");

                    if ( oneWorldLong == -1)
                        return;

                    if (CLIEngine.GetConfirmation("Would you rather use a 3D object or a 2D sprite/image to represent your OAPP within One World? Press Y for 3D or N for 2D."))
                    {
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation("Would you like to upload a local 3D object from your device or input a URI to an online object? (Press Y for local or N for online)"))
                        {
                            Console.WriteLine("");
                            oneWorld3dObjectPath = CLIEngine.GetValidFile("What is the full path to the local 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");
                            
                            if (oneWorld3dObjectPath == "exit")
                                return;
                            
                            oneWorld3dObject = File.ReadAllBytes(oneWorld3dObjectPath);
                        }
                        else
                        {
                            Console.WriteLine("");
                            oneWorld3dObjectURI = await CLIEngine.GetValidURIAsync("What is the URI to the 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                            if (oneWorld3dObjectURI == null)
                                return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation("Would you like to upload a local 2D sprite/image from your device or input a URI to an online sprite/image? (Press Y for local or N for online)"))
                        {
                            Console.WriteLine("");
                            oneWorld2dSpritePath = CLIEngine.GetValidFile("What is the full path to the local 2d sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");
                            
                            if (oneWorld2dSpritePath == "exit")
                                return;
                            
                            oneWorld2dSprite = File.ReadAllBytes(oneWorld2dSpritePath);
                        }
                        else
                        {
                            Console.WriteLine("");
                            oneWorld2dSpriteURI = await CLIEngine.GetValidURIAsync("What is the URI to the 2D sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");

                            if (oneWorld2dSpriteURI == null)
                                return;
                        }
                    }
                }
                else
                    Console.WriteLine("");

                value = CLIEngine.GetValidInputForEnum("What type of GenesisType do you wish to create? (New avatars will only be able to create moons that orbit Our World until you reach karma level 33 where you will then be able to create planets, when you reach level 77 you can create stars & beyond 77 you can create Galaxies and even entire Universes in your jounrey to become fully God realised!.)", typeof(GenesisType));

                if (value != null)
                {
                    if (value.ToString() == "exit")
                        return;

                    GenesisType genesisType = (GenesisType)value;
                    string dnaFolder = "";

                    //if (CLIEngine.GetConfirmation("Do you wish to create the CelestialBody/Zomes/Holons DNA now? (Enter 'n' if you already have a folder containing the DNA)."))
                    //{
                    //    //string zomeName = CLIEngine.GetValidInput("What is the name of the Zome (collection of Holons)?");
                    //    //string holonName = CLIEngine.GetValidInput("What is the name of the Holon (OASIS Data Object)?");
                    //    //string propName = CLIEngine.GetValidInput("What is the name of the Field/Property?");
                    //    //object propType = CLIEngine.GetValidInputForEnum("What is the type of the Field/Property?", typeof(HolonPropType));

                    //    //TODO:Come back to this.
                    //}
                    //else
                    dnaFolder = CLIEngine.GetValidFolder("What is the path to the CelestialBody/Zomes/Holons DNA?", false);

                    if (dnaFolder == "exit")
                        return;

                    if (Directory.Exists(dnaFolder) && Directory.GetFiles(dnaFolder).Length > 0)
                    {
                        string genesisFolder = CLIEngine.GetValidFolder("What is the path to the GenesisFolder (where the OAPP will be generated)?");
                        if (genesisFolder == "exit") return;
                        
                        string genesisNamespace = OAPPName;

                        if (!CLIEngine.GetConfirmation("Do you wish to use the OAPP Name for the Genesis Namespace (the OAPP namespace)? (Recommended)"))
                        {
                            Console.WriteLine();
                            genesisNamespace = CLIEngine.GetValidInput("What is the Genesis Namespace (the OAPP namespace)?");
                            if (genesisNamespace == "exit") return;
                        }
                        else
                            Console.WriteLine();

                        Guid parentId = Guid.Empty;

                        //bool multipleHolonInstances = CLIEngine.GetConfirmation("Do you want holons to create multiple instances of themselves?");

                        if (CLIEngine.GetConfirmation("Does this OAPP belong to another CelestialBody? (e.g. if it's a moon, what planet does it orbit or if it's a planet what star does it orbit? Only possible for avatars over level 33. Pressing N will add the OAPP (Moon) to the default planet (Our World))"))
                        {
                            if (STAR.BeamedInAvatarDetail.Level > 33)
                            {
                                Console.WriteLine("");
                                parentId = CLIEngine.GetValidInputForGuid("What is the Id (GUID) of the parent CelestialBody?");
                                if (parentId == Guid.Empty) return;

                                CLIEngine.ShowWorkingMessage("Generating OAPP...");
                                lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, genesisType, dnaFolder, genesisFolder, genesisNamespace, parentId, providerType);
                            }
                            else
                            {
                                Console.WriteLine("");
                                CLIEngine.ShowErrorMessage($"You are only level {STAR.BeamedInAvatarDetail.Level}. You need to be at least level 33 to be able to change the parent celestialbody. Using the default of Our World.");
                                Console.WriteLine("");
                                CLIEngine.ShowWorkingMessage("Generating OAPP...");
                                lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, genesisType, dnaFolder, genesisFolder, genesisNamespace, providerType);
                            }
                        }
                        else
                        {
                            Console.WriteLine("");
                            CLIEngine.ShowWorkingMessage("Generating OAPP...");
                            lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, genesisType, dnaFolder, genesisFolder, genesisNamespace, providerType);
                        }

                        if (lightResult != null)
                        {
                            if (!lightResult.IsError && lightResult.Result != null)
                            {
                                CLIEngine.ShowSuccessMessage($"OAPP Successfully Generated. ({lightResult.Message})");
                                ShowOAPP(lightResult.Result.OAPPDNA, lightResult.Result.CelestialBody.CelestialBodyCore.Zomes);
                                Console.WriteLine("");

                                if (CLIEngine.GetConfirmation("Do you wish to open the OAPP now?"))
                                    Process.Start("explorer.exe", Path.Combine(genesisFolder, string.Concat(OAPPName, " OAPP"), string.Concat(genesisNamespace, ".csproj")));

                                Console.WriteLine("");

                                if (CLIEngine.GetConfirmation("Do you wish to open the OAPP folder now?"))
                                    Process.Start("explorer.exe", Path.Combine(genesisFolder, string.Concat(OAPPName, " OAPP")));

                                Console.WriteLine("");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"Error Occured: {lightResult.Message}");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"The DnaFolder {dnaFolder} Is Not Valid. It Does Mot Contain Any Files!");
                }
            }
        }

        public static async Task<OASISResult<CoronalEjection>> GenerateCelestialBodyAsync(string OAPPName, string OAPPDesc, ICelestialBody parentCelestialBody, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", ProviderType providerType = ProviderType.Default)
        {
            // Create (OApp) by generating dynamic template/scaffolding code.
            string message = $"Generating {Enum.GetName(typeof(GenesisType), genesisType)} '{OAPPName}' (OApp)";

            if (genesisType == GenesisType.Moon && parentCelestialBody != null)
                message = $"{message} For Planet '{parentCelestialBody.Name}'";

            message = $"{message} ...";

            CLIEngine.ShowWorkingMessage(message);

            //Allows the celestialBodyDNAFolder, genesisFolder & genesisNameSpace params to be passed in overridng what is in the STARDNA.json file.
            OASISResult<CoronalEjection> lightResult = STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, parentCelestialBody, providerType).Result;

            //Will use settings in the STARDNA.json file.
            //OASISResult<CoronalEjection> lightResult = STAR.LightAsync(OAPPType, genesisType, name, parentCelestialBody).Result;

            if (lightResult.IsError)
                CLIEngine.ShowErrorMessage(string.Concat(" ERROR OCCURED. Error Message: ", lightResult.Message));
            else
            {
                CLIEngine.ShowSuccessMessage($"{Enum.GetName(typeof(GenesisType), genesisType)} Generated.");

                Console.WriteLine("");
                Console.WriteLine(string.Concat(" Id: ", lightResult.Result.CelestialBody.Id));
                Console.WriteLine(string.Concat(" CreatedByAvatarId: ", lightResult.Result.CelestialBody.CreatedByAvatarId));
                Console.WriteLine(string.Concat(" CreatedDate: ", lightResult.Result.CelestialBody.CreatedDate));
                Console.WriteLine("");
                ShowZomesAndHolons(lightResult.Result.CelestialBody.CelestialBodyCore.Zomes, string.Concat($" {Enum.GetName(typeof(GenesisType), genesisType)} contains ", lightResult.Result.CelestialBody.CelestialBodyCore.Zomes.Count(), " Zome(s): "));
            }

            return lightResult;
        }

        public static async Task<OASISResult<CoronalEjection>> GenerateZomesAndHolonsAsync(string OAPPName, string OAPPDesc, OAPPType OAPPType, string zomesAndHolonsyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", ProviderType providerType = ProviderType.Default)
        {
            // Create (OApp) by generating dynamic template/scaffolding code.
            CLIEngine.ShowWorkingMessage($"Generating Zomes & Holons...");

            //OASISResult<CoronalEjection> lightResult = STAR.LightAsync(oAPPName, OAPPType, zomesAndHolonsyDNAFolder, genesisFolder, genesisNameSpace).Result;
            OASISResult<CoronalEjection> lightResult = STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, zomesAndHolonsyDNAFolder, genesisFolder, genesisNameSpace).Result;

            //Will use settings in the STARDNA.json file.
            //OASISResult<CoronalEjection> lightResult = STAR.LightAsync(oAPPName, OAPPType).Result;

            if (lightResult.IsError)
                CLIEngine.ShowErrorMessage(string.Concat(" ERROR OCCURED. Error Message: ", lightResult.Message));
            else
            {
                int iNoHolons = 0;
                foreach (IZome zome in lightResult.Result.Zomes)
                    iNoHolons += zome.Children.Count();

                CLIEngine.ShowSuccessMessage($"{lightResult.Result.Zomes.Count()} Zomes & {iNoHolons} Holons Generated.");

                Console.WriteLine("");
                ShowZomesAndHolons(lightResult.Result.Zomes);
            }

            return lightResult;
        }

        public static async Task PublishOAPPAsync(string oappPath = "", bool publishDotNot = false, ProviderType providerType = ProviderType.Default)
        //public static async Task PublishOAPPAsync(string oappPath = "", ProviderType providerType = ProviderType.Default)
        {
            bool generateOAPPSource = false;
            bool uploadOAPPSource = false;
            bool generateOAPP = true;
            //bool uploadOAPP = true;
            bool uploadOAPPToCloud = false;
            bool generateOAPPSelfContained = false;
            //bool uploadOAPPSelfContained = false;
            bool uploadOAPPSelfContainedToCloud = false;
            bool generateOAPPSelfContainedFull = false;
            //bool uploadOAPPSelfContainedFull = false;
            bool uploadOAPPSelfContainedFullToCloud = false;
            bool makeOAPPSourcePublic = false;
            ProviderType OAPPBinaryProviderType = providerType; //ProviderType.IPFSOASIS;
            ProviderType OAPPSelfContainedBinaryProviderType = ProviderType.IPFSOASIS; //ProviderType.IPFSOASIS;
            ProviderType OAPPSelfContainedFullBinaryProviderType = ProviderType.IPFSOASIS; //ProviderType.IPFSOASIS;
            string launchTarget = "";
            string publishPath = "";
            string launchTargetQuestion = "";
           // bool publishDotNot = false;

            if (string.IsNullOrEmpty(oappPath))
            {
                string OAPPPathQuestion = "What is the full path to the (dotnet) published output for the OAPP you wish to publish?";
                launchTargetQuestion = "What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the OAPP? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";

                if (!CLIEngine.GetConfirmation("Have you already published the OAPP within Visual Studio (VS), Visual Studio Code (VSCode) or using the dotnet command? (If your OAPP is using a non dotnet template you can answer 'N')."))
                {
                    OAPPPathQuestion = "What is the full path to the OAPP you wish to publish?";
                    publishDotNot = true;
                    Console.WriteLine();
                    CLIEngine.ShowMessage("No worries, we will do that for you (if it's a dotnet OAPP)! ;-)");
                }
                else
                    Console.WriteLine();

                oappPath = CLIEngine.GetValidFolder(OAPPPathQuestion, false);
            }

            OASISResult<IOAPPDNA> OAPPDNAResult = await STAR.OASISAPI.OAPPs.ReadOAPPDNAAsync(oappPath);

            if (OAPPDNAResult != null && OAPPDNAResult.Result != null && !OAPPDNAResult.IsError)
            {
                switch (OAPPDNAResult.Result.OAPPType)
                {
                    case OAPPType.Console:
                    case OAPPType.WPF:
                    case OAPPType.WinForms:
                        launchTarget = $"{OAPPDNAResult.Result.OAPPName}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
                        //launchTarget = $"bin\\Release\\net8.0\\{OAPPDNAResult.Result.OAPPName}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
                        break;

                    case OAPPType.Blazor:
                    case OAPPType.MAUI:
                    case OAPPType.WebMVC:
                        //launchTarget = $"bin\\Release\\net8.0\\index.html"; 
                        launchTarget = $"index.html";
                        break;
                }

                if (!string.IsNullOrEmpty(launchTarget))
                {
                    if (!CLIEngine.GetConfirmation($"{launchTargetQuestion} Do you wish to use the following default launch target: {launchTarget}?"))
                        launchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", oappPath);
                    else
                        launchTarget = Path.Combine(oappPath, launchTarget);
                }
                else
                    launchTarget = CLIEngine.GetValidFile(launchTargetQuestion, oappPath);

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate the standard .oapp file? (Recommended). This file contains only the built & published OAPP source code. NOTE: You will need to make sure the target machine that runs this OAPP has both the appropriate OASIS & STAR ODK Runtimes installed along with the appropriate .NET Runtime."))
                {
                    generateOAPP = true;

                    if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to STARNET?"))
                    {
                        if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                            uploadOAPPToCloud = true;

                        object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                        if (OAPPBinaryProviderTypeObject != null)
                        {
                            if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                return;
                            else
                                OAPPBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                        }
                    }
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained .oapp file? This file contains the built & published OAPP source code along with the OASIS & STAR ODK Runtimes. NOTE: You will need to make sure the target machine that runs this OAPP has the appropriate .NET runtime installed. The file will be a minimum of 250 MB."))
                {
                    generateOAPPSelfContained = true;

                    if (CLIEngine.GetConfirmation("Do you wish to upload/publish the self-contained .oapp file to STARNET?"))
                    {
                        if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                            uploadOAPPToCloud = true;

                        object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                        if (OAPPBinaryProviderTypeObject != null)
                        {
                            if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                return;
                            else
                                OAPPBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                        }
                    }
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained (full) .oapp file? This file contains the built & published OAPP source code along with the OASIS, STAR ODK & .NET Runtimes. NOTE: The file will be a minimum of 500 MB."))
                {
                    generateOAPPSelfContained = true;

                    if (CLIEngine.GetConfirmation("Do you wish to upload/publish the self-contained (full) .oapp file to STARNET?"))
                    {
                        if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                            uploadOAPPToCloud = true;

                        object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                        if (OAPPBinaryProviderTypeObject != null)
                        {
                            if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                return;
                            else
                                OAPPBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                        }
                    }
                }

                if (!uploadOAPPToCloud && OAPPBinaryProviderType == ProviderType.None)
                    CLIEngine.ShowMessage("Since you did not select to upload to the cloud or OASIS storage the oapp will not be published to STARNET.");

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate a .oappsource file? This file will contain only the source files with no dependencies such as the OASIS or STAR runtimes (around 203MB). These will automatically be restored via nuget when the OAPP is built and/or published. You can optionally choose to upload this .oappsource file to STARNET from which others can download and then build to install and run your OAPP. NOTE: The full .oapp file is pre-built and published and is around 250MB minimum. You can optionally choose to also upload this file to STARNET (but you MUST upload either the .oappsource and make public or the full .oapp file if you want people to to be able to download and install your OAPP.) The advantage of the full .oapp file is that the OAPP is pre-built with all dependencies and so is guaranteed to install and run without any issues. It can also verify the launch target exists in the pre-built OAPP. If an OAPP is installed from the smaller .oappsource file (if you choose to upload and make public) there may be problems with restoring all dependencies etc so there are pros and cons to both approaches with the oapp taking longer to publish/upload and download over the .oappsource (as well as taking up more storage space) but has the advantage of being fully self contained and guaranteed to install & run fine."))
                {
                    generateOAPPSource = true;

                    if (CLIEngine.GetConfirmation("Do you wish to upload the .oappsource file to STARNET? The next question will ask if you wish to make this public. You may choose to upload and keep private as an extra backup for your code for example."))
                    {
                        uploadOAPPSource = true;

                        if (CLIEngine.GetConfirmation("Do you wish to make the .oappsource public? People will be able to view your code so only do this if you are happy with this. NOTE: If you select 'N' to this question then people will not be able to download, build, publish and install your OAPP from your .oappsource file. You will need to upload the full pre-built & published .oapp file below if you want people to be able to download and install your OAPP from STARNET. If you wish people to be able to download and install from your .oappsource file then select 'Y' to this question and the next."))
                            makeOAPPSourcePublic = true;
                    }
                }

                Console.WriteLine("");
                bool registerOnSTARNET = CLIEngine.GetConfirmation("Do you wish to publish to STARNET? If you select 'Y' to this question then your OAPP will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .oapp install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");

                if (registerOnSTARNET)
                {
                    Console.WriteLine("");
                    bool uploadOAPP = true;

                    if (makeOAPPSourcePublic)
                    {
                        if (!(CLIEngine.GetConfirmation("You have selected to generate, upload and make public your .oappsource file from which people can download, build, publish & install your OAPP (as long as the dependencies are restored fine and the launch target is found). You can also choose to upload the full .oapp file to STARNET giving people the option of which download and install process they prefer as well as an extra layer of redundancy. Do you wish to upload the .oapp file now?")))
                            uploadOAPP = false;
                    }
                    
                    if (uploadOAPP)
                    {
                        CLIEngine.ShowMessage("Do you wish to publish/upload the.oapp file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

                        //if (CLIEngine.GetConfirmation("Do you wish to upload to the cloud?"))
                        //    uploadToCloud = true;
                        
                        //if (CLIEngine.GetConfirmation("Do you wish to upload to an OASIS Provider? Make sure you select a provider that can handle large files such as IPFSOASIS, HoloOASIS etc. Also remember the OASIS Hyperdrive will only be able to auto-replicate to other providers that also support large files and are free or cost effective. By default it will NOT auto-replicate large files, you will need to manually configure this in your OASIS Profile settings."))
                        //{
                        //    object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to publish the OAPP to? (The default is IPFSOASIS)", typeof(ProviderType));

                        //    if (largeProviderTypeObject != null)
                        //        largeFileProviderType = (ProviderType)largeProviderTypeObject;
                        //}
                    }
                }
                else
                    Console.WriteLine("");

                if (Path.IsPathRooted(STAR.STARDNA.DefaultPublishedOAPPsPath))
                    publishPath = STAR.STARDNA.DefaultPublishedOAPPsPath;
                else
                    publishPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultPublishedOAPPsPath);

                //Console.WriteLine("");
                if (!CLIEngine.GetConfirmation($"Do you wish to publish the OAPP to the default publish folder defined in the STARDNA as DefaultPublishedOAPPsPath : {publishPath}?"))
                {
                    if (CLIEngine.GetConfirmation($"Do you wish to publish the OAPP to: {Path.Combine(oappPath, "Published")}?"))
                        publishPath = Path.Combine(oappPath, "Published");
                    else
                        publishPath = CLIEngine.GetValidFolder("Where do you wish to publish the OAPP?", true);
                }

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage("Publishing OAPP...");

                STAR.OASISAPI.OAPPs.OnOAPPPublishStatusChanged += OAPPs_OnOAPPPublishStatusChanged;
                STAR.OASISAPI.OAPPs.OnOAPPUploadStatusChanged += OAPPs_OnOAPPUploadStatusChanged;
                OASISResult<IOAPPDNA> publishResult = await STAR.OASISAPI.OAPPs.PublishOAPPAsync(oappPath, launchTarget, STAR.BeamedInAvatar.Id, publishDotNot, publishPath, registerOnSTARNET, generateOAPPSource, uploadOAPPSource, makeOAPPSourcePublic, generateOAPP, generateOAPPSelfContained, generateOAPPSelfContainedFull, uploadOAPPToCloud, uploadOAPPSelfContainedToCloud, uploadOAPPSelfContainedFullToCloud, providerType, OAPPBinaryProviderType, OAPPSelfContainedBinaryProviderType, OAPPSelfContainedFullBinaryProviderType);
                STAR.OASISAPI.OAPPs.OnOAPPUploadStatusChanged -= OAPPs_OnOAPPUploadStatusChanged;
                STAR.OASISAPI.OAPPs.OnOAPPPublishStatusChanged -= OAPPs_OnOAPPPublishStatusChanged;

                if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Successfully Published.");
                    ShowOAPP(publishResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to install the OAPP now?"))
                        await InstallOAPPAsync(publishResult.Result.OAPPId.ToString());

                    Console.WriteLine("");
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured publishing the OAPP. Reason: {publishResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage("The OAPPDNA.json file could not be found! Please ensure it is in the folder you specified.");
        }

        public static async Task UnPublishOAPPAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = await LoadOAPPAsync(idOrName, "unpublish", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<IOAPPDNA> unpublishResult = await STAR.OASISAPI.OAPPs.UnPublishOAPPAsync(result.Result, providerType);

                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Successfully Unpublished.");
                    ShowOAPP(unpublishResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the OAPP. Reason: {unpublishResult.Message}");
            }
        }

        //TODO: Make all methods use idOrName instead of just id...
        public static async Task EditOAPPAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> loadResult = await LoadOAPPAsync(idOrName, "edit", providerType);

            if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
            {
                ShowOAPP(loadResult.Result);

                //TODO: Comeback to this.
                loadResult.Result.Name = CLIEngine.GetValidInput("What is the name of the OAPP?");
                loadResult.Result.Description = CLIEngine.GetValidInput("What is the description of the OAPP?");

                OASISResult<IOAPP> result = await STAR.OASISAPI.OAPPs.SaveOAPPAsync(loadResult.Result, providerType);
                CLIEngine.ShowWorkingMessage("Saving OAPP...");

                if (result != null && !result.IsError && result.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Successfully Updated.");
                    ShowOAPP(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured updating the OAPP. Reason: {result.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP. Reason: {loadResult.Message}");
        }

        public static async Task DeleteOAPPAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = await LoadOAPPAsync(idOrName, "delete", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                ShowOAPP(result.Result);

                if (CLIEngine.GetConfirmation("Are you sure you wish to delete the OAPP?"))
                {
                    CLIEngine.ShowWorkingMessage("Deleting OAPP...");
                    result = await STAR.OASISAPI.OAPPs.DeleteOAPPAsync(result.Result, providerType);

                    if (result != null && !result.IsError && result.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage("OAPP Successfully Deleted.");
                        ShowOAPP(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured deleting the OAPP. Reason: {result.Message}");
                }
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP. Reason: {result.Message}");
        }

        public static async Task InstallOAPPAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPP> installResult = null;
            string installPath = "";

            if (Path.IsPathRooted(STAR.STARDNA.DefaultInstalledOAPPsPath))
                installPath = STAR.STARDNA.DefaultInstalledOAPPsPath;
            else
                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultInstalledOAPPsPath);

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to install the OAPP to the default install folder defined in the STARDNA as DefaultInstalledOAPPsPath : {installPath}?"))
                installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the OAPP?", true);

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                ProviderType largeFileProviderType = ProviderType.IPFSOASIS;
                object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to install the OAPP from? (The default is IPFSOASIS)", typeof(ProviderType));

                if (largeProviderTypeObject != null)
                {
                    largeFileProviderType = (ProviderType)largeProviderTypeObject;
                    OASISResult<IOAPP> result = await LoadOAPPAsync(idOrName, "install", largeFileProviderType, false);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        STAR.OASISAPI.OAPPs.OnOAPPDownloadStatusChanged += OAPPs_OnOAPPDownloadStatusChanged;
                        STAR.OASISAPI.OAPPs.OnOAPPInstallStatusChanged += OAPPs_OnOAPPInstallStatusChanged;
                        CLIEngine.ShowWorkingMessage("Installing OAPP...");
                        installResult = await STAR.OASISAPI.OAPPs.InstallOAPPAsync(STAR.BeamedInAvatar.Id, result.Result, installPath, true, providerType);
                        STAR.OASISAPI.OAPPs.OnOAPPDownloadStatusChanged -= OAPPs_OnOAPPDownloadStatusChanged;
                        STAR.OASISAPI.OAPPs.OnOAPPInstallStatusChanged -= OAPPs_OnOAPPInstallStatusChanged;
                    }

                    //installResult = await STAR.OASISAPI.OAPPs.InstallOAPPAsync(STAR.BeamedInAvatar.Id, id, installPath, providerType);
                }
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to install the OAPP from a local .oapp file or from STARNET? Press 'Y' for local .oapp or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oapp file?");

                    CLIEngine.ShowWorkingMessage("Installing OAPP...");
                    installResult = await STAR.OASISAPI.OAPPs.InstallOAPPAsync(STAR.BeamedInAvatar.Id, oappPath, installPath, true, providerType);
                }
                else
                    await LaunchSTARNETAsync(true);
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Successfully Installed.");
                    ShowInstalledOAPP(installResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to launch the OAPP now?"))
                    {
                        string oappTarget = Path.Combine(installPath, installResult.Result.OAPPDNA.LaunchTarget);
                        //Process.Start("explorer.exe", Path.Combine(installPath, installResult.Result.OAPPDNA.LaunchTarget));
                        Process.Start("dotnet.exe", Path.Combine(installPath, installResult.Result.OAPPDNA.LaunchTarget));
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing OAPP. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing OAPP. Reason: Unknown error occured!");
        }

        public static void InstallOAPP(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInstalledOAPP> installResult = null;
            string installPath = "";

            if (Path.IsPathRooted(STAR.STARDNA.DefaultInstalledOAPPsPath))
                installPath = STAR.STARDNA.DefaultInstalledOAPPsPath;
            else
                installPath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.DefaultInstalledOAPPsPath);

            if (!CLIEngine.GetConfirmation($"Do you wish to install the OAPP to the default install folder defined in the STARDNA as DefaultInstalledOAPPsPath : {installPath}?"))
                installPath = CLIEngine.GetValidFolder("What is the full path to where you wish to install the OAPP?", true);

            if (!string.IsNullOrEmpty(idOrName))
            {
                OASISResult<IOAPP> result = LoadOAPP(idOrName, "install", providerType);

                if (result != null && result.Result != null && !result.IsError)
                    installResult = STAR.OASISAPI.OAPPs.InstallOAPP(STAR.BeamedInAvatar.Id, result.Result, installPath, true, providerType);

                //installResult = await STAR.OASISAPI.OAPPs.InstallOAPPAsync(STAR.BeamedInAvatar.Id, id, installPath, providerType);
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to install the OAPP from a local .oapp file or from STARNET? Press 'Y' for local .oapp or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile("What is the full path to the .oapp file?");

                    CLIEngine.ShowWorkingMessage("Installing OAPP...");
                    installResult = STAR.OASISAPI.OAPPs.InstallOAPP(STAR.BeamedInAvatar.Id, oappPath, installPath, true, providerType);
                }
                else
                    LaunchSTARNETAsync(true);
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage("OAPP Successfully Installed.");
                    ShowInstalledOAPP(installResult.Result);

                    if (CLIEngine.GetConfirmation("Do you wish to launch the OAPP now?"))
                    {
                        string oappTarget = Path.Combine(installPath, installResult.Result.OAPPDNA.LaunchTarget);
                        //Process.Start("explorer.exe", Path.Combine(installPath, installResult.Result.OAPPDNA.LaunchTarget));
                        Process.Start("dotnet.exe", Path.Combine(installPath, installResult.Result.OAPPDNA.LaunchTarget));
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing OAPP. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing OAPP. Reason: Unknown error occured!");
        }

        public static async Task UnInstallOAPPAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = await LoadOAPPAsync(idOrName, "uninstall", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<IOAPPDNA> uninstallResult = await STAR.OASISAPI.OAPPs.UnInstallOAPPAsync(result.Result.OAPPDNA, STAR.BeamedInAvatar.Id, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage("OAPP Successfully Uninstalled.");
                        ShowOAPP(uninstallResult.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing OAPP. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling OAPP. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP. Reason: {result.Message}");
        }

        public static async Task LaunchSTARNETAsync(bool installOAPP = true, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowMessage("Welcome to STARNET!");
            await ListAllOAPPsAsync();

            if (installOAPP)
            {
                //Guid OAPPID = CLIEngine.GetValidInputForGuid("What is the GUID/ID of the OAPP you wish to install?");

                ProviderType largeFileProviderType = ProviderType.IPFSOASIS;
                object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to install the OAPP from? (The default is IPFSOASIS)", typeof(ProviderType));

                if (largeProviderTypeObject != null)
                {
                    largeFileProviderType = (ProviderType)largeProviderTypeObject;
                    OASISResult<IOAPP> result = await LoadOAPPAsync("", "install", largeFileProviderType);

                    if (result != null && result.Result != null && !result.IsError)
                        await InstallOAPPAsync(result.Result.Id.ToString());
                }
            }

            //TODO: Soon this will be like a sub-menu listing the STARNET commands (install, uninstall, list, publish, unpublish etc) and change the cursor to STARNET: rather than STAR:. They can then type exit to go back to the main STAR menu.
        }

        public static void LaunchSTARNET(bool installOAPP = true, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowMessage("Welcome to STARNET!");
            ListAllOAPPs();

            if (installOAPP)
            {
                //Guid OAPPID = CLIEngine.GetValidInputForGuid("What is the GUID/ID of the OAPP you wish to install?");

                OASISResult<IOAPP> result = LoadOAPP("", "install", providerType);

                if (result != null && result.Result != null && !result.IsError)
                    InstallOAPP(result.Result.Id.ToString());
            }

            //TODO: Soon this will be like a sub-menu listing the STARNET commands (install, uninstall, list, publish, unpublish etc) and change the cursor to STARNET: rather than STAR:. They can then type exit to go back to the main STAR menu.
        }

        public static async Task ListAllOAPPsAsync(ProviderType providerType = ProviderType.Default)
        {
            ListOAPPs(await STAR.OASISAPI.OAPPs.ListAllOAPPsAsync(providerType));
        }

        public static void ListAllOAPPs(ProviderType providerType = ProviderType.Default)
        {
            ListOAPPs(STAR.OASISAPI.OAPPs.ListAllOAPPs(providerType));
        }

        public static async Task ListOAPPsCreatedByBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            if (STAR.BeamedInAvatar != null)
                ListOAPPs(await STAR.OASISAPI.OAPPs.ListOAPPsCreatedByAvatarAsync(STAR.BeamedInAvatar.AvatarId));
            else
                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
        }

        public static async Task<OASISResult<IEnumerable<IInstalledOAPP>>> ListOAPPsInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInstalledOAPP>> result = new OASISResult<IEnumerable<IInstalledOAPP>>();

            if (STAR.BeamedInAvatar != null)
            {
                result = await STAR.OASISAPI.OAPPs.ListInstalledOAPPsAsync(STAR.BeamedInAvatar.AvatarId);
                ListInstalledOAPPs(result);
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");
                //CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public static async Task SearchOAPPsAsync(ProviderType providerType = ProviderType.Default)
        {
            
        }

        public static async Task LoadCelestialBodyAsync<T>(T celestialBody, string name, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            CLIEngine.ShowWorkingMessage($"Loading {name}...");
            OASISResult<T> celestialBodyResult = await celestialBody.LoadAsync<T>();

            if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
            {
                CLIEngine.ShowSuccessMessage($"{name} Loaded Successfully.");
                ShowHolonProperties(celestialBodyResult.Result);
                Console.WriteLine("");
                ShowZomesAndHolons(celestialBodyResult.Result.CelestialBodyCore.Zomes, string.Concat(" ", name, " Contains ", celestialBodyResult.Result.CelestialBodyCore.Zomes.Count(), " Zome(s)", celestialBodyResult.Result.CelestialBodyCore.Zomes.Count > 0 ? ":" : ""));
            }
        }

        public static async Task LoadHolonAsync(Guid id, string name, ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage($"Loading Holon {name}...");
            OASISResult<IHolon> holonResult = await STAR.OASISAPI.Data.LoadHolonAsync(id);

            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
            {
                CLIEngine.ShowSuccessMessage($"{name} Loaded Successfully.");
                ShowHolonProperties(holonResult.Result);
            }
        }

        public static async Task ShowOAPPAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = await LoadOAPPAsync(idOrName, "view", providerType);

            if (result != null && !result.IsError && result.Result != null)
                ShowOAPP(result.Result);
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the OAPP. Reason: {result.Message}");
        }

        //TODO: Once OAPP has been changed to OAPPDNA in OAPPManager this method will be redundant so can just use the other ShowOAPP method below (removes redundant code and redundant storage).
        public static void ShowOAPP(IOAPP oapp)
        {
            ShowOAPP(oapp.OAPPDNA, Mapper.Convert<IHolon, IZome>(oapp.Children).ToList());
        }

        public static void ShowOAPP(IOAPPDNA oapp, List<IZome> zomes = null)
        {
            //CLIEngine.ShowMessage(string.Concat($"Id: ", oapp.OAPPId != Guid.Empty ? oapp.OAPPId : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Name: ", !string.IsNullOrEmpty(oapp.OAPPName) ? oapp.OAPPName : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(oapp.Description) ? oapp.Description : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Type: ", Enum.GetName(typeof(OAPPType), oapp.OAPPType)));
            //CLIEngine.ShowMessage(string.Concat($"Genesis Type: ", Enum.GetName(typeof(GenesisType), oapp.GenesisType)));
            //CLIEngine.ShowMessage(string.Concat($"Celestial Body Id: ", oapp.CelestialBodyId != Guid.Empty ? oapp.CelestialBodyId : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Celestial Body Name: ", !string.IsNullOrEmpty(oapp.CelestialBodyName) ? oapp.CelestialBodyName : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Celestial Body Type: ", Enum.GetName(typeof(HolonType), oapp.CelestialBodyType)));
            //CLIEngine.ShowMessage(string.Concat($"Created On: ", oapp.CreatedOn != DateTime.MinValue ? oapp.CreatedOn.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Created By: ", oapp.CreatedByAvatarId != Guid.Empty ? string.Concat(oapp.CreatedByAvatarUsername, " (", oapp.CreatedByAvatarId.ToString(), ")") : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Published On: ", oapp.PublishedOn != DateTime.MinValue ? oapp.PublishedOn.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Published By: ", oapp.PublishedByAvatarId != Guid.Empty ? string.Concat(oapp.PublishedByAvatarUsername, " (", oapp.PublishedByAvatarId.ToString(), ")") : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published Path: ", !string.IsNullOrEmpty(oapp.OAPPPublishedPath) ? oapp.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Filesize: ", oapp.OAPPFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published Path: ", !string.IsNullOrEmpty(oapp.OAPPWithSTARAndOASISRunTimePublishedPath) ? oapp.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Filesize: ", oapp.OAPPWithSTARAndOASISRunTimeFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published Path: ", !string.IsNullOrEmpty(oapp.OAPPWithSTARAndOASISRunTimeAndDotNetPublishedPath) ? oapp.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Filesize: ", oapp.OAPPWithSTARAndOASISRunTimeAndDotNetFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published On STARNET: ", oapp.OAPPPublishedOnSTARNET ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published To Cloud: ", oapp.OAPPPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oapp.OAPPPublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To Cloud: ", oapp.OAPPWithSTARAndOASISRunTimePublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oapp.OAPPWithSTARAndOASISRunTimePublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To Cloud: ", oapp.OAPPWithSTARAndOASISRunTimeAndDotNetPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oapp.OAPPWithSTARAndOASISRunTimeAndDotNetPublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Published Path: ", !string.IsNullOrEmpty(oapp.OAPPSourcePublishedPath) ? oapp.OAPPSourcePublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Filesize: ", oapp.OAPPSourceFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Published On STARNET: ", oapp.OAPPSourcePublishedOnSTARNET ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Public On STARNET: ", oapp.OAPPSourcePublicOnSTARNET ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"Launch Target: ", !string.IsNullOrEmpty(oapp.LaunchTarget) ? oapp.LaunchTarget : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Version: ", oapp.Version));
            //CLIEngine.ShowMessage(string.Concat($"STAR ODK Version: ", oapp.STARODKVersion));
            //CLIEngine.ShowMessage(string.Concat($"OASIS Version: ", oapp.OASISVersion));
            //CLIEngine.ShowMessage(string.Concat($"COSMIC Version: ", oapp.COSMICVersion));

            //CLIEngine.ShowMessage("");
            //CLIEngine.ShowMessage("");
            //CLIEngine.ShowMessage(string.Concat($"Id:                                                   ", oapp.OAPPId != Guid.Empty ? oapp.OAPPId : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Name:                                                 ", !string.IsNullOrEmpty(oapp.OAPPName) ? oapp.OAPPName : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Description:                                          ", !string.IsNullOrEmpty(oapp.Description) ? oapp.Description : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Type:                                            ", Enum.GetName(typeof(OAPPType), oapp.OAPPType)));
            //CLIEngine.ShowMessage(string.Concat($"Genesis Type:                                         ", Enum.GetName(typeof(GenesisType), oapp.GenesisType)));
            //CLIEngine.ShowMessage(string.Concat($"Celestial Body Id:                                    ", oapp.CelestialBodyId != Guid.Empty ? oapp.CelestialBodyId : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Celestial Body Name:                                  ", !string.IsNullOrEmpty(oapp.CelestialBodyName) ? oapp.CelestialBodyName : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Celestial Body Type:                                  ", Enum.GetName(typeof(HolonType), oapp.CelestialBodyType)));
            //CLIEngine.ShowMessage(string.Concat($"Created On:                                           ", oapp.CreatedOn != DateTime.MinValue ? oapp.CreatedOn.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Created By:                                           ", oapp.CreatedByAvatarId != Guid.Empty ? string.Concat(oapp.CreatedByAvatarUsername, " (", oapp.CreatedByAvatarId.ToString(), ")") : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Published On:                                         ", oapp.PublishedOn != DateTime.MinValue ? oapp.PublishedOn.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Published By:                                         ", oapp.PublishedByAvatarId != Guid.Empty ? string.Concat(oapp.PublishedByAvatarUsername, " (", oapp.PublishedByAvatarId.ToString(), ")") : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published Path:                                  ", !string.IsNullOrEmpty(oapp.OAPPPublishedPath) ? oapp.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Filesize:                                        ", oapp.OAPPFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published Path:                   ", !string.IsNullOrEmpty(oapp.OAPPWithSTARAndOASISRunTimePublishedPath) ? oapp.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Filesize:                         ", oapp.OAPPWithSTARAndOASISRunTimeFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published Path:              ", !string.IsNullOrEmpty(oapp.OAPPWithSTARAndOASISRunTimeAndDotNetPublishedPath) ? oapp.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Filesize:                    ", oapp.OAPPWithSTARAndOASISRunTimeAndDotNetFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published On STARNET:                            ", oapp.OAPPPublishedOnSTARNET ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published To Cloud:                              ", oapp.OAPPPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published To OASIS Provider:                     ", Enum.GetName(typeof(ProviderType), oapp.OAPPPublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To Cloud:               ", oapp.OAPPWithSTARAndOASISRunTimePublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To OASIS Provider:      ", Enum.GetName(typeof(ProviderType), oapp.OAPPWithSTARAndOASISRunTimePublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To Cloud:          ", oapp.OAPPWithSTARAndOASISRunTimeAndDotNetPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oapp.OAPPWithSTARAndOASISRunTimeAndDotNetPublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Published Path:                           ", !string.IsNullOrEmpty(oapp.OAPPSourcePublishedPath) ? oapp.OAPPSourcePublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Filesize:                                 ", oapp.OAPPSourceFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Published On STARNET:                     ", oapp.OAPPSourcePublishedOnSTARNET ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Public On STARNET:                        ", oapp.OAPPSourcePublicOnSTARNET ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"Launch Target:                                        ", !string.IsNullOrEmpty(oapp.LaunchTarget) ? oapp.LaunchTarget : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Version:                                              ", oapp.Version));
            //CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:                                     ", oapp.STARODKVersion));
            //CLIEngine.ShowMessage(string.Concat($"OASIS Version:                                        ", oapp.OASISVersion));
            //CLIEngine.ShowMessage(string.Concat($"COSMIC Version:                                       ", oapp.COSMICVersion));
            //CLIEngine.ShowMessage(string.Concat($".NET Version:                                         ", oapp.DotNetVersion));

            CLIEngine.ShowMessage(string.Concat($"Id:                                                   ", oapp.OAPPId != Guid.Empty ? oapp.OAPPId : "None"));
            CLIEngine.ShowMessage(string.Concat($"Name:                                                 ", !string.IsNullOrEmpty(oapp.OAPPName) ? oapp.OAPPName : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description:                                          ", !string.IsNullOrEmpty(oapp.Description) ? oapp.Description : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Type:                                            ", Enum.GetName(typeof(OAPPType), oapp.OAPPType)));
            CLIEngine.ShowMessage(string.Concat($"Genesis Type:                                         ", Enum.GetName(typeof(GenesisType), oapp.GenesisType)));
            CLIEngine.ShowMessage(string.Concat($"Celestial Body Id:                                    ", oapp.CelestialBodyId != Guid.Empty ? oapp.CelestialBodyId : "None"));
            CLIEngine.ShowMessage(string.Concat($"Celestial Body Name:                                  ", !string.IsNullOrEmpty(oapp.CelestialBodyName) ? oapp.CelestialBodyName : "None"));
            CLIEngine.ShowMessage(string.Concat($"Celestial Body Type:                                  ", Enum.GetName(typeof(HolonType), oapp.CelestialBodyType)));
            CLIEngine.ShowMessage(string.Concat($"Created On:                                           ", oapp.CreatedOn != DateTime.MinValue ? oapp.CreatedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created By:                                           ", oapp.CreatedByAvatarId != Guid.Empty ? string.Concat(oapp.CreatedByAvatarUsername, " (", oapp.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published On:                                         ", oapp.PublishedOn != DateTime.MinValue ? oapp.PublishedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published By:                                         ", oapp.PublishedByAvatarId != Guid.Empty ? string.Concat(oapp.PublishedByAvatarUsername, " (", oapp.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published Path:                                  ", !string.IsNullOrEmpty(oapp.OAPPPublishedPath) ? oapp.OAPPPublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Filesize:                                        ", oapp.OAPPFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published Path:                   ", !string.IsNullOrEmpty(oapp.OAPPSelfContainedPublishedPath) ? oapp.OAPPPublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Filesize:                         ", oapp.OAPPSelfContainedFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published Path:              ", !string.IsNullOrEmpty(oapp.OAPPSelfContainedFullPublishedPath) ? oapp.OAPPPublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Filesize:                    ", oapp.OAPPSelfContainedFullFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published On STARNET:                            ", oapp.OAPPPublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published To Cloud:                              ", oapp.OAPPPublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published To OASIS Provider:                     ", Enum.GetName(typeof(ProviderType), oapp.OAPPPublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To Cloud:               ", oapp.OAPPSelfContainedPublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To OASIS Provider:      ", Enum.GetName(typeof(ProviderType), oapp.OAPPSelfContainedPublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To Cloud:          ", oapp.OAPPSelfContainedFullPublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oapp.OAPPSelfContainedFullPublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Published Path:                           ", !string.IsNullOrEmpty(oapp.OAPPSourcePublishedPath) ? oapp.OAPPSourcePublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Filesize:                                 ", oapp.OAPPSourceFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Published On STARNET:                     ", oapp.OAPPSourcePublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Public On STARNET:                        ", oapp.OAPPSourcePublicOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"Launch Target:                                        ", !string.IsNullOrEmpty(oapp.LaunchTarget) ? oapp.LaunchTarget : "None"));
            CLIEngine.ShowMessage(string.Concat($"Version:                                              ", oapp.Version));
            CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:                                     ", oapp.STARODKVersion));
            CLIEngine.ShowMessage(string.Concat($"OASIS Version:                                        ", oapp.OASISVersion));
            CLIEngine.ShowMessage(string.Concat($"COSMIC Version:                                       ", oapp.COSMICVersion));
            CLIEngine.ShowMessage(string.Concat($".NET Version:                                         ", oapp.DotNetVersion));


            if (zomes != null && zomes.Count > 0)
            {
                Console.WriteLine("");
                ShowZomesAndHolons(zomes);
            }

            //TODO: Come back to this.
            //if (oapp.CelestialBody != null && oapp.CelestialBody.CelestialBodyCore != null && oapp.CelestialBody.CelestialBodyCore.Zomes != null)
            //    ShowZomesAndHolons(oapp.CelestialBody.CelestialBodyCore.Zomes);

                //else if (oapp.Zomes != null)
                //    ShowZomesAndHolons(oapp.Zomes);

            CLIEngine.ShowDivider();
        }

        public static void ShowInstalledOAPP(IInstalledOAPP oapp)
        {
            ShowOAPP(oapp.OAPPDNA);
            CLIEngine.ShowMessage(string.Concat($"Installed On:                                         ", oapp.InstalledOn != DateTime.MinValue ? oapp.InstalledOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed By:                                         ", oapp.InstalledBy != Guid.Empty ? string.Concat(oapp.InstalledByAvatarUsername, " (", oapp.InstalledBy.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Installed Path:                                       ", oapp.InstalledPath));


            //CLIEngine.ShowMessage(string.Concat($"Id: ", oapp.OAPPDNA.OAPPId != Guid.Empty ? oapp.OAPPDNA.OAPPId : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Name: ", !string.IsNullOrEmpty(oapp.OAPPDNA.OAPPName) ? oapp.OAPPDNA.OAPPName : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(oapp.OAPPDNA.Description) ? oapp.OAPPDNA.Description : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Type: ", Enum.GetName(typeof(OAPPType), oapp.OAPPDNA.OAPPType)));
            //CLIEngine.ShowMessage(string.Concat($"Genesis Type: ", Enum.GetName(typeof(GenesisType), oapp.OAPPDNA.GenesisType)));
            //CLIEngine.ShowMessage(string.Concat($"Celestial Body Id: ", oapp.OAPPDNA.CelestialBodyId != Guid.Empty ? oapp.OAPPDNA.CelestialBodyId : "None"));

            //if (oapp.OAPPDNA.CelestialBodyId != Guid.Empty)
            //{
            //    CLIEngine.ShowMessage(string.Concat($"Celestial Body Name: ", oapp.OAPPDNA.CelestialBodyName != null ? oapp.OAPPDNA.CelestialBodyName : "None"));
            //    CLIEngine.ShowMessage(string.Concat($"Celestial Body Type: ", Enum.GetName(typeof(HolonType), oapp.OAPPDNA.CelestialBodyType)));
            //}

            //CLIEngine.ShowMessage(string.Concat($"Created On: ", oapp.OAPPDNA.CreatedOn != DateTime.MinValue ? oapp.OAPPDNA.CreatedOn.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Created By: ", oapp.CreatedByAvatarId != Guid.Empty ? string.Concat(oapp.OAPPDNA.CreatedByAvatarUsername, " (", oapp.CreatedByAvatarId.ToString(), ")") : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Published On: ", oapp.OAPPDNA.PublishedOn != DateTime.MinValue ? oapp.OAPPDNA.PublishedOn.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Published By: ", oapp.OAPPDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(oapp.OAPPDNA.PublishedByAvatarUsername, " (", oapp.OAPPDNA.PublishedByAvatarId.ToString(), ")") : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published Path: ", !string.IsNullOrEmpty(oapp.OAPPDNA.OAPPPublishedPath) ? oapp.OAPPDNA.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Filesize: ", oapp.OAPPDNA.OAPPFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP (With STAR And OASIS Runtimes) Published Path: ", !string.IsNullOrEmpty(oapp.OAPPWithSTARAndOASISRunTimePublishedPath) ? oapp.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP (With STAR And OASIS Runtimes) Filesize: ", oapp.OAPPDNA.OAPPWithSTARAndOASISRunTimeFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP (With STAR And OASIS Runtimes And DotNet) Published Path: ", !string.IsNullOrEmpty(oapp.OAPPWithSTARAndOASISRunTimeAndDotNetPublishedPath) ? oapp.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP (With STAR And OASIS Runtimes And DotNet) Filesize: ", oapp.OAPPDNA.OAPPWithSTARAndOASISRunTimeAndDotNetFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published On STARNET: ", oapp.OAPPDNA.OAPPPublishedOnSTARNET ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published To Cloud: ", oapp.OAPPDNA.OAPPPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oapp.OAPPPublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP (With STAR And OASIS Runtimes) Published To Cloud: ", oapp.OAPPWithSTARAndOASISRunTimePublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP (With STAR And OASIS Runtimes) Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oapp.OAPPWithSTARAndOASISRunTimePublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP (With STAR And OASIS Runtimes And DotNet) Published To Cloud: ", oapp.OAPPWithSTARAndOASISRunTimeAndDotNetPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP (With STAR And OASIS Runtimes And DotNet) Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oapp.OAPPWithSTARAndOASISRunTimeAndDotNetPublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Published Path: ", !string.IsNullOrEmpty(oapp.OAPPSourcePublishedPath) ? oapp.OAPPSourcePublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Filesize: ", oapp.OAPPSourceFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Published On STARNET: ", oapp.OAPPSourcePublishedOnSTARNET ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Source Public On STARNET: ", oapp.OAPPSourcePublicOnSTARNET ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"Launch Target: ", !string.IsNullOrEmpty(oapp.OAPPDNA.LaunchTarget) ? oapp.OAPPDNA.LaunchTarget : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Installed On: ", oapp.InstalledOn != DateTime.MinValue ? oapp.InstalledOn.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Installed By: ", oapp.InstalledBy != Guid.Empty ? string.Concat(oapp.InstalledByAvatarUsername, " (", oapp.InstalledBy.ToString(), ")") : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Installed Path: ", oapp.InstalledPath));
            //CLIEngine.ShowMessage(string.Concat($"Version: ", !string.IsNullOrEmpty(oapp.OAPPDNA.Version) ? oapp.OAPPDNA.Version : "None"));
            //CLIEngine.ShowMessage(string.Concat($"STAR ODK Version: ", oapp.OAPPDNA.STARODKVersion));
            //CLIEngine.ShowMessage(string.Concat($"OASIS Version: ", oapp.OAPPDNA.OASISVersion));
            //CLIEngine.ShowMessage(string.Concat($"COSMIC Version: ", oapp.OAPPDNA.COSMICVersion));




            //CLIEngine.ShowMessage($"Zomes: ");

            //if (oapp.CelestialBody != null)
            //    ShowZomesAndHolons(oapp.CelestialBody.CelestialBodyCore.Zomes);
            //else
            //    ShowHolons(oapp.Children);

            CLIEngine.ShowDivider();
        }

        private static void ListOAPPs(OASISResult<IEnumerable<IOAPP>> oapps)
        {
            if (oapps != null && !oapps.IsError)
            {
                if (oapps.Result != null && oapps.Result.Count() > 0)
                {
                    Console.WriteLine();

                    if (oapps.Result.Count() == 1)
                        CLIEngine.ShowMessage($"{oapps.Result.Count()} OAPP Found:");
                    else
                        CLIEngine.ShowMessage($"{oapps.Result.Count()} OAPP's Found:");

                    CLIEngine.ShowDivider();

                    foreach (IOAPP oapp in oapps.Result)
                        ShowOAPP(oapp);

                    ShowOAPPListFooter();
                }
                else
                    CLIEngine.ShowWarningMessage("No OAPP's Found.");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured loading OAPP's. Reason: {oapps.Message}");
        }

        private static void ListInstalledOAPPs(OASISResult<IEnumerable<IInstalledOAPP>> oapps)
        {
            if (oapps != null && !oapps.IsError)
            {
                if (oapps.Result != null && oapps.Result.Count() > 0)
                {
                    Console.WriteLine();

                    if (oapps.Result.Count() == 1)
                        CLIEngine.ShowMessage($"{oapps.Result.Count()} OAPP Found:");
                    else
                        CLIEngine.ShowMessage($"{oapps.Result.Count()} OAPP's Found:");

                    CLIEngine.ShowDivider();

                    foreach (IInstalledOAPP oapp in oapps.Result)
                        ShowInstalledOAPP(oapp);

                    ShowOAPPListFooter();
                }
                else
                    CLIEngine.ShowWarningMessage("No OAPP's Found.");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured loading OAPP's. Reason: {oapps.Message}");
        }

        private static void ShowOAPPListFooter()
        {
            CLIEngine.ShowMessage("");
            CLIEngine.ShowMessage("NOTE: 'OAPP' contains only the OAPP itself, the target machine will need to have both the OASIS & STAR Runtimes installed along with the correct .NET Runtime.");
            CLIEngine.ShowMessage("      'OAPP Self Contained' contains the OAPP along with the OASIS & STAR Runtimes meaning it will run on any machine as long as the correct .NET Runtime is already installed.");
            CLIEngine.ShowMessage("      'OAPP Self Contained Full' contains the OAPP along with the OASIS & STAR Runtimes And the .NET Runtime meaning it will run on any machine with no additional dependencies.");
        }

        private static async Task<OASISResult<IOAPP>> LoadOAPPAsync(string idOrName, string operationName, ProviderType providerType = ProviderType.Default, bool addSpace = true)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            Guid id = Guid.Empty;

            if (string.IsNullOrEmpty(idOrName))
                idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name to the OAPP you wish to {operationName}?");

            if (addSpace)
                Console.WriteLine("");

            CLIEngine.ShowWorkingMessage("Loading OAPP...");

            if (Guid.TryParse(idOrName, out id))
                result = await STAR.OASISAPI.OAPPs.LoadOAPPAsync(id, providerType);
            else
            {
                OASISResult<IEnumerable<IOAPP>> allOAPPsResult = await STAR.OASISAPI.OAPPs.ListAllOAPPsAsync();

                if (allOAPPsResult != null && allOAPPsResult.Result != null && !allOAPPsResult.IsError)
                {
                    result.Result = allOAPPsResult.Result.FirstOrDefault(x => x.Name == idOrName); //TODO: In future will use Where instead so user can select which OAPP they want... (if more than one matches the given name).

                    if (result.Result == null)
                    {
                        result.IsError = true;
                        result.Message = "No OAPP Was Found!";
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.OAPPs.ListAllOAPPsAsync. Reason: {allOAPPsResult.Message}");
            }

            return result;
        }

        private static OASISResult<IOAPP> LoadOAPP(string idOrName, string operationName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            Guid id = Guid.Empty;

            if (string.IsNullOrEmpty(idOrName))
                idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name to the OAPP you wish to {operationName}?");

            CLIEngine.ShowWorkingMessage("Loading OAPP...");

            if (Guid.TryParse(idOrName, out id))
                result = STAR.OASISAPI.OAPPs.LoadOAPP(id, providerType);
            else
            {
                OASISResult<IEnumerable<IOAPP>> allOAPPsResult = STAR.OASISAPI.OAPPs.ListAllOAPPs();

                if (allOAPPsResult != null && allOAPPsResult.Result != null && !allOAPPsResult.IsError)
                {
                    result.Result = allOAPPsResult.Result.FirstOrDefault(x => x.Name == idOrName); //TODO: In future will use Where instead so user can select which OAPP they want... (if more than one matches the given name).

                    if (result.Result == null)
                    {
                        result.IsError = true;
                        result.Message = "No OAPP Was Found!";
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.OAPPs.ListAllOAPPsAsync. Reason: {allOAPPsResult.Message}");
            }

            return result;
        }

        private static void OAPPs_OnOAPPPublishStatusChanged(object sender, API.ONODE.Core.Events.OAPPPublishStatusEventArgs e)
        {
            switch (e.Status)
            {
                case OAPPPublishStatus.DotNetPublishing:
                    CLIEngine.ShowWorkingMessage($"Dotnet Publishing...");
                    break;

                case OAPPPublishStatus.Uploading:
                    CLIEngine.ShowMessage("Uploading...");
                    Console.WriteLine("");
                    //CLIEngine.ShowWorkingMessage("Uploading... 0%");
                    //CLIEngine.BeginWorkingMessage("Uploading... 0%");
                    //CLIEngine.ShowProgressBar(0);
                    break;

                case OAPPPublishStatus.Published:
                    CLIEngine.ShowSuccessMessage("OAPP Published Successfully");
                    break;

                case OAPPPublishStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(OAPPPublishStatus), e.Status)}...");
                    break;
            }  
        }

        private static void OAPPs_OnOAPPUploadStatusChanged(object sender, API.ONODE.Core.Events.OAPPUploadProgressEventArgs e)
        {
            //CLIEngine.ShowProgressBar(e.Progress, true);
            //CLIEngine.ShowProgressBar(e.Progress);
            //CLIEngine.UpdateWorkingMessageWithPercent(e.Progress);
            //CLIEngine.UpdateWorkingMessage($"Uploading... {e.Progress}%"); //was this one.
            CLIEngine.ShowProgressBar((double)e.Progress / (double)100);
        }

        private static void OAPPs_OnOAPPInstallStatusChanged(object sender, API.ONODE.Core.Events.OAPPInstallStatusEventArgs e)
        {
            switch (e.Status)
            {
                case OAPPInstallStatus.Downloading:
                    CLIEngine.BeginWorkingMessage("Downloading...");
                    //CLIEngine.ShowProgressBar(0);
                    break;

                case OAPPInstallStatus.Installed:
                    CLIEngine.ShowSuccessMessage("OAPP Installed Successfully");
                    break;

                case OAPPInstallStatus.Error:
                    CLIEngine.ShowErrorMessage(e.ErrorMessage);
                    break;

                default:
                    CLIEngine.ShowWorkingMessage($"{Enum.GetName(typeof(OAPPInstallStatus), e.Status)}...");
                    break;
            }
        }

        private static void OAPPs_OnOAPPDownloadStatusChanged(object sender, API.ONODE.Core.Events.OAPPDownloadProgressEventArgs e)
        {
            //CLIEngine.ShowProgressBar(e.Progress, true);
            //LIEngine.ShowProgressBar(e.Progress);
            CLIEngine.UpdateWorkingMessage($"Downloading... {e.Progress}%");
        }
    }
}

