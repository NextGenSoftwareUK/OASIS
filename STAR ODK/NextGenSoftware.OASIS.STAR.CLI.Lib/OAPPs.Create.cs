using System.Diagnostics;
using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.Interfaces;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.DNA;
using Ipfs.CoreApi;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class OAPPs
    {
        public override async Task<OASISResult<OAPP>> CreateAsync(ISTARNETCreateOptions<OAPP, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            if (createOptions?.CustomCreateParams != null
                && createOptions.CustomCreateParams.TryGetValue(StarCliNonInteractiveCreateKeys.LightRequestJsonPath, out object lightJsonObj)
                && lightJsonObj is string lightJsonPath
                && !string.IsNullOrWhiteSpace(lightJsonPath))
            {
                OASISResult<CoronalEjection> lightCor = await LightFromJsonFileAsync(lightJsonPath.Trim(), providerType);
                return MapCoronalToOapp(lightCor);
            }

            if (createOptions?.CustomCreateParams != null
                && createOptions.CustomCreateParams.TryGetValue(StarCliNonInteractiveCreateKeys.Scripted, out object scriptedFlag)
                && scriptedFlag is bool sb && sb)
                return await base.CreateAsync(createOptions, null, showHeaderAndInro, addDependencies, providerType);

            if (CLIEngine.NonInteractive)
            {
                OASISResult<OAPP> blocked = new OASISResult<OAPP>();
                OASISErrorHandling.HandleError(ref blocked,
                    "Non-interactive OAPP create requires one of: (1) `oapp light <LightRequest.json>` / `happ light <file>` (or alias `oapp create light <file>`) — full STAR.LightAsync + STARNET registration from JSON; (2) scripted argv `create <name> <description> <OAPPType> [parentFolder]`; or (3) top-level `light <LightRequest.json>` / `light json <file>` / `light <full positional args>`. The interactive Light wizard (`light wiz` / prompts) is not available in -n.");
                return blocked;
            }

            OASISResult<CoronalEjection> result = await LightWizardAsync(createOptions, holonSubType, showHeaderAndInro, providerType);

            return new OASISResult<OAPP>
            {
                IsError = result.IsError,
                Message = result.Message,
                Result = result.Result != null && result.Result.OAPP != null ? (OAPP)result.Result.OAPP : null
            };
        }

        public async Task<OASISResult<CoronalEjection>> LightWizardAsync(ISTARNETCreateOptions<OAPP, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, ProviderType providerType = ProviderType.Default)
        {
            //OASISResult<OAPP> result = new OASISResult<OAPP>();
            OASISResult<CoronalEjection> lightResult = new OASISResult<CoronalEjection>();
            string errorMessage = "Error occured in STAR.CLI.Lib.OAPPs.LightWizardAsync. Reason: ";
            object enumValue = null;
            OAPPType OAPPType = OAPPType.OAPPTemplate;
            OAPPTemplateType OAPPTemplateType = OAPPTemplateType.Console;
            IInstalledOAPPTemplate installedOAPPTemplate = null;
            InstalledCelestialBodyMetaDataDNA celestialBodyMetaDataDNA = null;
            long ourWorldLat = 0;
            long ourWorldLong = 0;
            long oneWorldLat = 0;
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
            string cbMetaDataGeneratedPath = "";
            Dictionary<string, IList<INode>> nodes = new Dictionary<string, IList<INode>>();

            ShowHeader();

            string OAPPName = CLIEngine.GetValidInput("What is the name of the OAPP?");

            if (OAPPName == "exit")
            {
                lightResult.Message = "User Exited";
                return lightResult;
            }

            //OAPPName = OAPPName.Replace(" ", "_");

            string OAPPDesc = CLIEngine.GetValidInput("What is the description of the OAPP?");

            if (OAPPDesc == "exit")
            {
                lightResult.Message = "User Exited";
                return lightResult;
            }

            if (CLIEngine.GetConfirmation("Do you want to create the OAPP from an OAPP Template or do you want to generate the code only? Select 'Y' for OAPPTemplate or 'N' for Generated Code Only."))
            {
                //Console.WriteLine("");
                //enumValue = CLIEngine.GetValidInputForEnum("What type of OAPP Template do you wish to use?", typeof(OAPPTemplateType));

                //if (enumValue != null)
                //{
                //    if (enumValue.ToString() == "exit")
                //    {
                //        lightResult.Message = "User Exited";
                //        return lightResult;
                //    }
                //    else
                //    {
                        //OAPPTemplateType = (OAPPTemplateType)enumValue;
                        bool templateInstalled = false;

                        do
                        {
                            OASISResult<InstalledOAPPTemplate> findResult = await STARCLI.OAPPTemplates.FindForProviderAndInstallIfNotInstalledAsync("use", providerType: providerType);

                            if (findResult != null && findResult.Result != null && !findResult.IsError)
                            {
                                templateInstalled = true;
                                installedOAPPTemplate = findResult.Result;
                                OAPPTemplateType = (OAPPTemplateType)Enum.Parse(typeof(OAPPTemplateType), installedOAPPTemplate.STARNETDNA.STARNETCategory.ToString());
                            }
                            else
                            {
                                //CLIEngine.ShowErrorMessage($"Error occured finding OAPP Template. Reason: {findResult.Message}");

                                if (findResult.Message == "User Exited")
                                {
                                    lightResult.Message = "User Exited";
                                    return lightResult;
                                }
                            }
                        }
                        while (!templateInstalled);
                    //}
                //}
            }
            else
                OAPPType = OAPPType.GeneratedCodeOnly;

            //TODO: I think star bang was going to be used to create non OAPP Celestial bodies or spaces outside of the magic verse.
            //if (CLIEngine.GetConfirmation("Do you wish the OAPP to be part of the MagicVerse within the OASIS Omniverse (will optionally appear in Our World/AR World)? If you say yes then new avatars will only be able to create moons that orbit Our World until you reach karma level 33 where you will then be able to create planets, when you reach level 77 you can create stars. If you select no then you can create whatever you like outside of the MagicVerse but it will still be within the OASIS Omniverse."))
            //{

            //}



            if (CLIEngine.GetConfirmation("Do you wish for your OAPP to appear in the AR geo-location Our World/AR World game/platform? (recommended)"))
            {
                Console.WriteLine("");
                ourWorldLat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your OAPP to appear in Our World/AR World?");

                if (ourWorldLat == -1)
                {
                    lightResult.Message = "User Exited";
                    return lightResult;
                }

                ourWorldLong = CLIEngine.GetValidInputForLong("What is the long geo-location you wish for your OAPP to appear in Our World/AR World?");

                if (ourWorldLong == -1)
                {
                    lightResult.Message = "User Exited";
                    return lightResult;
                }

                if (CLIEngine.GetConfirmation("Would you rather use a 3D object or a 2D sprite/image to represent your OAPP? Press Y for 3D or N for 2D."))
                {
                    Console.WriteLine("");

                    if (CLIEngine.GetConfirmation("Would you like to upload a local 3D object from your device or input a URI to an online object? (Press Y for local or N for online)"))
                    {
                        Console.WriteLine("");
                        ourWorld3dObjectPath = CLIEngine.GetValidFile("What is the full path to the local 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                        if (ourWorld3dObjectPath == "exit")
                        {
                            lightResult.Message = "User Exited";
                            return lightResult;
                        }

                        ourWorld3dObject = File.ReadAllBytes(ourWorld3dObjectPath);

                    }
                    else
                    {
                        Console.WriteLine("");
                        ourWorld3dObjectURI = await CLIEngine.GetValidURIAsync("What is the URI to the 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                        if (ourWorld3dObjectURI == null)
                        {
                            lightResult.Message = "User Exited";
                            return lightResult;
                        }
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
                        {
                            lightResult.Message = "User Exited";
                            return lightResult;
                        }

                        ourWorld2dSprite = File.ReadAllBytes(ourWorld2dSpritePath);
                    }
                    else
                    {
                        Console.WriteLine("");
                        ourWorld2dSpriteURI = await CLIEngine.GetValidURIAsync("What is the URI to the 2D sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");

                        if (ourWorld2dSpriteURI == null)
                        {
                            lightResult.Message = "User Exited";
                            return lightResult;
                        }
                    }
                }
            }
            else
                Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Do you wish for your OAPP to appear in the Open World MMORPG One World game/platform? (recommended)"))
            {
                Console.WriteLine("");
                oneWorldLat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your OAPP to appear in One World?");

                if (oneWorldLat == -1)
                {
                    lightResult.Message = "User Exited";
                    return lightResult;
                }

                oneWorldLong = CLIEngine.GetValidInputForLong("What is the long geo-location you wish for your OAPP to appear in One World?");

                if (oneWorldLong == -1)
                {
                    lightResult.Message = "User Exited";
                    return lightResult;
                }

                if (CLIEngine.GetConfirmation("Would you rather use a 3D object or a 2D sprite/image to represent your OAPP within One World? Press Y for 3D or N for 2D."))
                {
                    Console.WriteLine("");

                    if (CLIEngine.GetConfirmation("Would you like to upload a local 3D object from your device or input a URI to an online object? (Press Y for local or N for online)"))
                    {
                        Console.WriteLine("");
                        oneWorld3dObjectPath = CLIEngine.GetValidFile("What is the full path to the local 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                        if (oneWorld3dObjectPath == "exit")
                        {
                            lightResult.Message = "User Exited";
                            return lightResult;
                        }

                        oneWorld3dObject = File.ReadAllBytes(oneWorld3dObjectPath);
                    }
                    else
                    {
                        Console.WriteLine("");
                        oneWorld3dObjectURI = await CLIEngine.GetValidURIAsync("What is the URI to the 3D object? (Press Enter if you wish to skip and use a default 3D object instead. You can always change this later.)");

                        if (oneWorld3dObjectURI == null)
                        {
                            lightResult.Message = "User Exited";
                            return lightResult;
                        }
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
                        {
                            lightResult.Message = "User Exited";
                            return lightResult;
                        }

                        oneWorld2dSprite = File.ReadAllBytes(oneWorld2dSpritePath);
                    }
                    else
                    {
                        Console.WriteLine("");
                        oneWorld2dSpriteURI = await CLIEngine.GetValidURIAsync("What is the URI to the 2D sprite/image? (Press Enter if you wish to skip and use the default image instead. You can always change this later.)");

                        if (oneWorld2dSpriteURI == null)
                        {
                            lightResult.Message = "User Exited";
                            return lightResult;
                        }
                    }
                }
            }
            else
                Console.WriteLine("");

            enumValue = CLIEngine.GetValidInputForEnum("What type of GenesisType do you wish to create? (New avatars will only be able to create moons that orbit Our World until you reach karma level 33 where you will then be able to create planets, when you reach level 77 you can create stars & beyond 77 you can create Galaxies and even entire Universes in your journey to become fully God realised!.)", typeof(GenesisType));

            if (enumValue != null)
            {
                if (enumValue.ToString() == "exit")
                {
                    lightResult.Message = "User Exited";
                    return lightResult;
                }

                GenesisType genesisType = (GenesisType)enumValue;
                string dnaFolder = "";

                List<IZome> zomes = new List<IZome>();
                List<IHolon> holons = new List<IHolon>();
                bool addMoreZomes = true;
                bool addMoreHolons = true;
                bool addMoreProps = true;
                bool validDNA = false;

                do
                {
                    if (CLIEngine.GetConfirmation("Do you wish to create the CelestialBody/Zomes/Holons DNA now? (Enter 'N' if you already have a folder containing the DNA or wish to use/install one from STARNET)."))
                    {
                        do
                        {
                            Console.WriteLine("");
                            string zomeName = CLIEngine.GetValidInput("What is the name of the Zome (collection of Holons)?");

                            IZome zome = new Zome();
                            zome.Name = zomeName;

                            addMoreHolons = true;
                            do
                            {
                                IHolon holon = new Holon();
                                holon.Name = CLIEngine.GetValidInput("What is the name of the Holon (OASIS Data Object)?");
                                addMoreProps = true;

                                do
                                {
                                    string propName = CLIEngine.GetValidInput("What is the name of the Field/Property?");
                                    
                                    CLIEngine.ShowMessage("NodeType KEY: String (Text) = 0, Int (Small Number) = 1, Bool (Yes/No) = 2, DateTime = 3, Long (Big Number) = 4, Double (Big Decimal Number) = 5, ByteArray (Data) = 6, Float (Small Decimal Number) = 7, Object = 8 (Generic Data/Object), Unknown = 9");
                                    object propType = CLIEngine.GetValidInputForEnum("What is the type of the Field/Property?", typeof(NodeType)); //typeof(HolonPropType));

                                    if (propType != null)
                                    {
                                        if (propType.ToString() == "exit")
                                        {
                                            lightResult.Message = "User Exited";
                                            return lightResult;
                                        }
                                        NodeType holonPropType = (NodeType)propType;

                                        if (holon.Nodes == null)
                                            holon.Nodes = new List<INode>();

                                        holon.Nodes.Add(new Node
                                        {
                                            NodeName = propName,
                                            NodeType = holonPropType
                                        });
                                    }
                                    else
                                        CLIEngine.ShowErrorMessage("Invalid Field/Property Type! Please try again.");

                                    //Console.WriteLine("");
                                    addMoreProps = CLIEngine.GetConfirmation("Do you wish to add more fields/properties to the Holon?");
                                    Console.WriteLine("");

                                } while (addMoreProps);

                                nodes[holon.Name] = holon.Nodes;
                                zome.Children.Add(holon);

                                addMoreHolons = CLIEngine.GetConfirmation("Do you wish to add more Holon's to the Zome?");
                                Console.WriteLine("");

                            } while (addMoreHolons);

                            zomes.Add(zome);
                            addMoreZomes = CLIEngine.GetConfirmation("Do you wish to add more Zome's to the Celestial Body/OAPP?");

                        } while (addMoreZomes);

                        string OAPPMetaDataDNAFolder = STAR.STARDNA.OAPPMetaDataDNAFolder;

                        if (!Path.IsPathRooted(STAR.STARDNA.OAPPMetaDataDNAFolder))
                            OAPPMetaDataDNAFolder = Path.Combine(STAR.STARDNA.STARBasePath, STAR.STARDNA.OAPPMetaDataDNAFolder);

                        Console.WriteLine("");
                        (lightResult, OAPPMetaDataDNAFolder) = GetValidFolder(lightResult, OAPPMetaDataDNAFolder, "CelestialBody/Zomes/Holons MetaData DNA", "OAPPMetaDataDNAFolder", false);

                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage("Generating CelestialBody/Zomes/Holons MetaData DNA...");
                        OASISResult<IGenerateMetaDataDNAResult> generateResult = STAR.GenerateMetaDataDNA(zomes, OAPPName, OAPPMetaDataDNAFolder, providerType);

                        if (generateResult != null && generateResult.Result != null && !generateResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("MetaData DNA Successfully Generated.");
                            validDNA = true;
                            (lightResult, CelestialBodyMetaDataDNA cbDNA) = await CreateMetaDataOnSTARNETAsync(lightResult, generateResult.Result, genesisType, errorMessage, providerType);

                            if (cbDNA != null)
                            {
                                celestialBodyMetaDataDNA = new InstalledCelestialBodyMetaDataDNA() { STARNETDNA = cbDNA.STARNETDNA };

                                //Check if the user chose to install (after creating and publishing).
                                CLIEngine.SupressConsoleLogging = true;
                                OASISResult<InstalledCelestialBodyMetaDataDNA> installedResult = await STAR.STARAPI.CelestialBodiesMetaDataDNA.LoadInstalledAsync(STAR.BeamedInAvatar.Id, cbDNA.STARNETDNA.Id, cbDNA.STARNETDNA.Version, providerType);
                                CLIEngine.SupressConsoleLogging = false;

                                if (installedResult != null && !installedResult.IsError && installedResult.Result != null)
                                    celestialBodyMetaDataDNA = installedResult.Result;
                            }

                            cbMetaDataGeneratedPath = Path.Combine(OAPPMetaDataDNAFolder, OAPPName, "CelestialBodyDNA");
                            dnaFolder = cbMetaDataGeneratedPath;
                        }
                        else
                            OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} An error occured in STAR.GenerateMetaDataDNAAsync. Reason: {generateResult.Message}");
                    }
                    else
                    {
                        Console.WriteLine("");
                        if (CLIEngine.GetConfirmation("Do you wish to use/install a CelestialBody MetaData DNA (contains Zome & Holon MetaData DNA) from STARNET? (Enter 'N' if you already have a folder containing the DNA)."))
                        {
                            Console.WriteLine("");
                            OASISResult<InstalledCelestialBodyMetaDataDNA> findResult = await STARCLI.CelestialBodiesMetaDataDNA.FindForProviderAndInstallIfNotInstalledAsync("use", providerType: providerType);

                            if (findResult != null && findResult.Result != null && !findResult.IsError)
                            {
                                OASISResult<Dictionary<string, IList<INode>>> nodesResult = ValidateCelestialBodyDataDNA(findResult.Result.InstalledPath);

                                if (nodesResult != null && nodesResult.Result != null && !nodesResult.IsError && nodesResult.Result.Count > 0)
                                {
                                    nodes = nodesResult.Result;
                                    validDNA = true;
                                    dnaFolder = findResult.Result.InstalledPath;
                                    celestialBodyMetaDataDNA = findResult.Result;
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage($"Error occured finding CelestialBody MetaData DNA. Reason: {findResult.Message}");
                        }
                        else
                        {
                            Console.WriteLine("");
                            dnaFolder = CLIEngine.GetValidFolder("What is the path to the CelestialBody MetaData DNA (which needs to contain Zome MetaData DNA & Holon MetaData DNA)?", false);

                            if (dnaFolder == "exit")
                            {
                                lightResult.Message = "User Exited";
                                return lightResult;
                            }

                            if (Directory.Exists(dnaFolder) && Directory.GetFiles(dnaFolder).Length > 0)
                            {
                                OASISResult<Dictionary<string, IList<INode>>> nodesResult = ValidateCelestialBodyDataDNA(dnaFolder);

                                if (nodesResult != null && nodesResult.Result != null && !nodesResult.IsError && nodesResult.Result.Count > 0)
                                {
                                    nodes = nodesResult.Result;
                                    cbMetaDataGeneratedPath = dnaFolder;
                                    validDNA = true;
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage($"The DnaFolder {dnaFolder} is not valid, it does not contain any files! Please try again!");
                        }
                    }
                } while (!validDNA);

                OASISResult<List<MetaHolonTag>> metaHolonTagMappingsResult = MapCustomHolonMetaTagsToTemplate(installedOAPPTemplate, nodes);
                OASISResult<Dictionary<string, string>> metaTagMappingsResult = MapCustomMetaTagsToTemplate(installedOAPPTemplate);

                if (metaTagMappingsResult != null && metaTagMappingsResult.Result != null && !metaTagMappingsResult.IsError)
                {
                    string oappPath = "";

                    if (!string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                        oappPath = Path.Combine(STAR.STARDNA.STARNETBasePath, STAR.STARDNA.DefaultOAPPsSourcePath);
                    else
                        oappPath = STAR.STARDNA.DefaultOAPPsSourcePath;

                    if (!CLIEngine.GetConfirmation($"Do you wish to create the OAPP in the default path defined in the STARDNA as 'DefaultOAPPsSourcePath'? The current path points to: {oappPath}"))
                        oappPath = CLIEngine.GetValidFolder("Where do you wish to create the OAPP?");

                    if (oappPath == "exit")
                    {
                        lightResult.Message = "User Exited";
                        return lightResult;
                    }

                    //string genesisNamespace = OAPPName.Replace(" ", "");
                    string genesisNamespace = OAPPName.ToPascalCase();

                    Console.WriteLine("");
                    if (!CLIEngine.GetConfirmation("Do you wish to use the OAPP Name for the Genesis Namespace (the OAPP namespace)? (Recommended)"))
                    {
                        Console.WriteLine();
                        genesisNamespace = CLIEngine.GetValidInput("What is the Genesis Namespace (the OAPP namespace)?");

                        if (genesisNamespace == "exit")
                        {
                            lightResult.Message = "User Exited";
                            return lightResult;
                        }
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

                            if (parentId == Guid.Empty)
                            {
                                lightResult.Message = "User Exited";
                                return lightResult;
                            }

                            CLIEngine.ShowWorkingMessage("Generating OAPP...");
                            lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, installedOAPPTemplate.STARNETDNA.Id, installedOAPPTemplate.STARNETDNA.VersionSequence, genesisType, dnaFolder, oappPath, genesisNamespace, metaHolonTagMappingsResult.Result, metaTagMappingsResult.Result, parentId, providerType);
                        }
                        else
                        {
                            Console.WriteLine("");
                            CLIEngine.ShowErrorMessage($"You are only level {STAR.BeamedInAvatarDetail.Level}. You need to be at least level 33 to be able to change the parent celestialbody. Using the default of Our World.");
                            Console.WriteLine("");
                            CLIEngine.ShowWorkingMessage("Generating OAPP...");
                            lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, installedOAPPTemplate.STARNETDNA.Id, installedOAPPTemplate.STARNETDNA.VersionSequence, genesisType, dnaFolder, oappPath, genesisNamespace, metaHolonTagMappingsResult.Result, metaTagMappingsResult.Result, providerType);
                        }
                    }
                    else
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage("Generating OAPP...");
                        lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Id : Guid.Empty, installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.VersionSequence : 0, genesisType, dnaFolder, oappPath, genesisNamespace, metaHolonTagMappingsResult.Result, metaTagMappingsResult.Result, providerType);
                    }

                    if (lightResult != null)
                    {
                        if (!lightResult.IsError && lightResult.Result != null)
                        {
                            oappPath = Path.Combine(oappPath, OAPPName);
                            OASISResult<OAPP> createOAPPResult = null;

                            try
                            {
                                if (celestialBodyMetaDataDNA != null)
                                {

                                }
                                else
                                {

                                }


                                STARNETDNA dna = new STARNETDNA()
                                {
                                    MetaData = new Dictionary<string, object>()
                                    {
                                        { "CelestialBodyId", lightResult.Result.CelestialBody.Id },
                                        { "CelestialBodyName", lightResult.Result.CelestialBody.Name },
                                        { "GenesisType", genesisType },
                                        { "OAPPTemplateId", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Id : null},
                                        { "OAPPTemplateName", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Name : null },
                                        { "OAPPTemplateDescription", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Description: null },
                                        { "OAPPTemplateType", OAPPTemplateType },
                                        { "OAPPTemplateVersion", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Version : null },
                                        { "OAPPTemplateVersionSequence", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.VersionSequence: null },
                                        { "OAPPTemplateInstalledPath", installedOAPPTemplate != null ? installedOAPPTemplate.InstalledPath : null},
                                        { "CelestialBodyMetaDataId", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Id : null },
                                        { "CelestialBodyMetaDataName", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Name : null },
                                        { "CelestialBodyMetaDataDescription", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Description : null },
                                        { "CelestialBodyMetaDataType", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.STARNETHolonType : null },
                                        { "CelestialBodyMetaDataVersionSequence", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.VersionSequence : null },
                                        { "CelestialBodyMetaDataVersion", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Version : null },
                                        { "CelestialBodyMetaDataInstalledPath", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.InstalledPath : null },
                                        { "CelestialBodyMetaDataGeneratedPath", cbMetaDataGeneratedPath },
                                        { "STARNETHolonType", OAPPType },
                                        { "OurWorldLat", ourWorldLat },
                                        { "OurWorldLong", ourWorldLong },
                                        { "OurWorld3dObject", ourWorld3dObject },
                                        { "OurWorld3dObjectURI", ourWorld3dObjectURI },
                                        { "OurWorld2dSprite", ourWorld2dSprite },
                                        { "OurWorld2dSpriteURI", ourWorld2dSpriteURI },
                                        { "OneWorldLat", oneWorldLat },
                                        { "OneWorldLong", oneWorldLong },
                                        { "OneWorld3dObject", oneWorld3dObject },
                                        { "OneWorld3dObjectURI", oneWorld3dObjectURI },
                                        { "OneWorld2dSprite", oneWorld2dSprite },
                                        { "OneWorld2dSpriteURI", oneWorld2dSpriteURI }
                                    }
                                };

                                STARNETHolon holon = new OAPP()
                                {
                                    Name = OAPPName,
                                    Description = OAPPDesc,
                                    GenesisType = genesisType,
                                    CelestialBodyId = lightResult.Result.CelestialBody.Id,
                                    CelestialBodyName = lightResult.Result.CelestialBody.Name,
                                    OAPPTemplateId = installedOAPPTemplate.STARNETDNA.Id,
                                    OAPPTemplateName = installedOAPPTemplate.STARNETDNA.Name,
                                    OAPPTemplateDescription = installedOAPPTemplate.STARNETDNA.Description,
                                    OAPPTemplateType = OAPPTemplateType,
                                    OAPPTemplateVersion = installedOAPPTemplate.STARNETDNA.Version,
                                    OAPPTemplateVersionSequence = installedOAPPTemplate.STARNETDNA.VersionSequence,
                                    CelestialBodyMetaDataId = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Id : Guid.Empty,
                                    CelestialBodyMetaDataName = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Name : null,
                                    CelestialBodyMetaDataDescription = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Description : null,
                                    CelestialBodyMetaDataType = celestialBodyMetaDataDNA != null ? (CelestialBodyType)Enum.Parse(typeof(CelestialBodyType), celestialBodyMetaDataDNA.STARNETDNA.STARNETHolonType.ToString()) : CelestialBodyType.Moon,
                                    CelestialBodyMetaDataVersion = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Version : null,
                                    CelestialBodyMetaDataVersionSequence = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.VersionSequence : 0,
                                    CelestialBodyMetaDataGeneratedPath = cbMetaDataGeneratedPath,
                                    //STARNETHolonType = OAPPType,
                                    OurWorldLat = ourWorldLat,
                                    OurWorldLong = ourWorldLong,
                                    OurWorld3dObject = ourWorld3dObject,
                                    OurWorld3dObjectURI = ourWorld3dObjectURI,
                                    OurWorld2dSprite = ourWorld2dSprite,
                                    OurWorld2dSpriteURI = ourWorld2dSpriteURI,
                                    OneWorldLat = oneWorldLat,
                                    OneWorldLong = oneWorldLong,
                                    OneWorld3dObject = oneWorld3dObject,
                                    OneWorld3dObjectURI = oneWorld3dObjectURI,
                                    OneWorld2dSprite = oneWorld2dSprite,
                                    OneWorld2dSpriteURI = oneWorld2dSpriteURI
                                };
                                    

                                    //Finally, save this to the STARNET App Store. This will be private on the store until the user publishes via the Star.Seed() command.
                                    createOAPPResult = await STAR.STARAPI.OAPPs.CreateAsync(STAR.BeamedInAvatar.Id, OAPPName, OAPPDesc, OAPPType, oappPath, new STARNETCreateOptions<OAPP, STARNETDNA>()
                                    {
                                        MetaTagMappings = new API.ONODE.Core.Objects.STARNET.MetaTagMappings()
                                        {
                                            MetaHolonTags = metaHolonTagMappingsResult.Result,
                                            MetaTags = metaTagMappingsResult.Result
                                        },
                                        STARNETDNA = new STARNETDNA()
                                        {
                                            MetaData = new Dictionary<string, object>()
                                    {
                                        { "CelestialBodyId", lightResult.Result.CelestialBody.Id },
                                        { "CelestialBodyName", lightResult.Result.CelestialBody.Name },
                                        { "GenesisType", genesisType },
                                        { "OAPPTemplateId", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Id : null},
                                        { "OAPPTemplateName", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Name : null },
                                        { "OAPPTemplateDescription", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Description: null },
                                        { "OAPPTemplateType", OAPPTemplateType },
                                        { "OAPPTemplateVersion", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Version : null },
                                        { "OAPPTemplateVersionSequence", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.VersionSequence: null },
                                        { "OAPPTemplateInstalledPath", installedOAPPTemplate != null ? installedOAPPTemplate.InstalledPath : null},
                                        { "CelestialBodyMetaDataId", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Id : null },
                                        { "CelestialBodyMetaDataName", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Name : null },
                                        { "CelestialBodyMetaDataDescription", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Description : null },
                                        { "CelestialBodyMetaDataType", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.STARNETHolonType : null },
                                        { "CelestialBodyMetaDataVersionSequence", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.VersionSequence : null },
                                        { "CelestialBodyMetaDataVersion", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Version : null },
                                        { "CelestialBodyMetaDataInstalledPath", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.InstalledPath : null },
                                        { "CelestialBodyMetaDataGeneratedPath", cbMetaDataGeneratedPath },
                                        { "STARNETHolonType", OAPPType },
                                        { "OurWorldLat", ourWorldLat },
                                        { "OurWorldLong", ourWorldLong },
                                        { "OurWorld3dObject", ourWorld3dObject },
                                        { "OurWorld3dObjectURI", ourWorld3dObjectURI },
                                        { "OurWorld2dSprite", ourWorld2dSprite },
                                        { "OurWorld2dSpriteURI", ourWorld2dSpriteURI },
                                        { "OneWorldLat", oneWorldLat },
                                        { "OneWorldLong", oneWorldLong },
                                        { "OneWorld3dObject", oneWorld3dObject },
                                        { "OneWorld3dObjectURI", oneWorld3dObjectURI },
                                        { "OneWorld2dSprite", oneWorld2dSprite },
                                        { "OneWorld2dSpriteURI", oneWorld2dSpriteURI }
                                    }
                                        },

                                        //TODO: For now we need to store the meta data here again otherwise when the holon is saved the blank props will override the metadata keyvalues above! Strongly typed overrides the keyvalue pairs but the metadata above is needed to store in the STARNETDNA, will try to remove this duplication later! ;-)
                                        STARNETHolon = new OAPP()
                                        {
                                            Name = OAPPName,
                                            Description = OAPPDesc,
                                            GenesisType = genesisType,
                                            CelestialBodyId = lightResult.Result.CelestialBody.Id,
                                            CelestialBodyName = lightResult.Result.CelestialBody.Name,
                                            OAPPTemplateId = installedOAPPTemplate.STARNETDNA.Id,
                                            OAPPTemplateName = installedOAPPTemplate.STARNETDNA.Name,
                                            OAPPTemplateDescription = installedOAPPTemplate.STARNETDNA.Description,
                                            OAPPTemplateType = OAPPTemplateType,
                                            OAPPTemplateVersion = installedOAPPTemplate.STARNETDNA.Version,
                                            OAPPTemplateVersionSequence = installedOAPPTemplate.STARNETDNA.VersionSequence,
                                            CelestialBodyMetaDataId = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Id : Guid.Empty,
                                            CelestialBodyMetaDataName = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Name : null,
                                            CelestialBodyMetaDataDescription = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Description : null,
                                            CelestialBodyMetaDataType = celestialBodyMetaDataDNA != null ? (CelestialBodyType)Enum.Parse(typeof(CelestialBodyType), celestialBodyMetaDataDNA.STARNETDNA.STARNETHolonType.ToString()) : CelestialBodyType.Moon,
                                            CelestialBodyMetaDataVersion = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Version : null,
                                            CelestialBodyMetaDataVersionSequence = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.VersionSequence : 0,
                                            CelestialBodyMetaDataGeneratedPath = cbMetaDataGeneratedPath,
                                            //STARNETHolonType = OAPPType,
                                            OurWorldLat = ourWorldLat,
                                            OurWorldLong = ourWorldLong,
                                            OurWorld3dObject = ourWorld3dObject,
                                            OurWorld3dObjectURI = ourWorld3dObjectURI,
                                            OurWorld2dSprite = ourWorld2dSprite,
                                            OurWorld2dSpriteURI = ourWorld2dSpriteURI,
                                            OneWorldLat = oneWorldLat,
                                            OneWorldLong = oneWorldLong,
                                            OneWorld3dObject = oneWorld3dObject,
                                            OneWorld3dObjectURI = oneWorld3dObjectURI,
                                            OneWorld2dSprite = oneWorld2dSprite,
                                            OneWorld2dSpriteURI = oneWorld2dSpriteURI
                                        }
                                    }, providerType);

                                //    
                                //    OASISResult<OAPP> createOAPPResult = await STAR.STARAPI.OAPPs.CreateAsync(STAR.BeamedInAvatar.Id, OAPPName, OAPPDesc, OAPPType, oappPath, metaHolonTagMappingsResult.Result, metaTagMappingsResult.Result, new Dictionary<string, object>()
                                //{
                                //    { "CelestialBodyId", lightResult.Result.CelestialBody.Id },
                                //    { "CelestialBodyName", lightResult.Result.CelestialBody.Name },
                                //    { "GenesisType", genesisType },
                                //    { "OAPPTemplateId", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Id : null},
                                //    { "OAPPTemplateName", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Name : null },
                                //    { "OAPPTemplateDescription", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Description: null },
                                //    { "OAPPTemplateType", OAPPTemplateType },
                                //    { "OAPPTemplateVersion", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.Version : null },
                                //    { "OAPPTemplateVersionSequence", installedOAPPTemplate != null ? installedOAPPTemplate.STARNETDNA.VersionSequence: null },
                                //    { "OAPPTemplateInstalledPath", installedOAPPTemplate != null ? installedOAPPTemplate.InstalledPath : null},
                                //    { "CelestialBodyMetaDataId", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Id : null },
                                //    { "CelestialBodyMetaDataName", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Name : null },
                                //    { "CelestialBodyMetaDataDescription", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Description : null },
                                //    { "CelestialBodyMetaDataType", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.STARNETHolonType : null },
                                //    { "CelestialBodyMetaDataVersionSequence", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.VersionSequence : null },
                                //    { "CelestialBodyMetaDataVersion", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Version : null },
                                //    { "CelestialBodyMetaDataInstalledPath", celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.InstalledPath : null },
                                //    { "CelestialBodyMetaDataGeneratedPath", cbMetaDataGeneratedPath },
                                //    { "STARNETHolonType", OAPPType },
                                //    { "OurWorldLat", ourWorldLat },
                                //    { "OurWorldLong", ourWorldLong },
                                //    { "OurWorld3dObject", ourWorld3dObject },
                                //    { "OurWorld3dObjectURI", ourWorld3dObjectURI },
                                //    { "OurWorld2dSprite", ourWorld2dSprite },
                                //    { "OurWorld2dSpriteURI", ourWorld2dSpriteURI },
                                //    { "OneWorldLat", oneWorldLat },
                                //    { "OneWorldLong", oneWorldLong },
                                //    { "OneWorld3dObject", oneWorld3dObject },
                                //    { "OneWorld3dObjectURI", oneWorld3dObjectURI },
                                //    { "OneWorld2dSprite", oneWorld2dSprite },
                                //    { "OneWorld2dSpriteURI", oneWorld2dSpriteURI } },
                                //        // { "Zomes", lightResult.Result.CelestialBody.CelestialBodyCore.Zomes } },
                                //        new OAPP() //TODO: For now we need to store the meta data here again otherwise when the holon is saved the blank props will override the metadata keyvalues above! Strongly typed overrides the keyvalue pairs but the metadata above is needed to store in the STARNETDNA, will try to remove this duplication later! ;-)
                                //        {
                                //            Name = OAPPName,
                                //            Description = OAPPDesc,
                                //            GenesisType = genesisType,
                                //            CelestialBodyId = lightResult.Result.CelestialBody.Id,
                                //            CelestialBodyName = lightResult.Result.CelestialBody.Name,
                                //            OAPPTemplateId = installedOAPPTemplate.STARNETDNA.Id,
                                //            OAPPTemplateName = installedOAPPTemplate.STARNETDNA.Name,
                                //            OAPPTemplateDescription = installedOAPPTemplate.STARNETDNA.Description,
                                //            OAPPTemplateType = OAPPTemplateType,
                                //            OAPPTemplateVersion = installedOAPPTemplate.STARNETDNA.Version,
                                //            OAPPTemplateVersionSequence = installedOAPPTemplate.STARNETDNA.VersionSequence,
                                //            CelestialBodyMetaDataId = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Id : Guid.Empty,
                                //            CelestialBodyMetaDataName = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Name : null,
                                //            CelestialBodyMetaDataDescription = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Description : null,
                                //            CelestialBodyMetaDataType = celestialBodyMetaDataDNA != null ? (CelestialBodyType)Enum.Parse(typeof(CelestialBodyType), celestialBodyMetaDataDNA.STARNETDNA.STARNETHolonType.ToString()) : CelestialBodyType.Moon,
                                //            CelestialBodyMetaDataVersion = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Version : null,
                                //            CelestialBodyMetaDataVersionSequence = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.VersionSequence : 0,
                                //            CelestialBodyMetaDataGeneratedPath = cbMetaDataGeneratedPath,
                                //            //STARNETHolonType = OAPPType,
                                //            OurWorldLat = ourWorldLat,
                                //            OurWorldLong = ourWorldLong,
                                //            OurWorld3dObject = ourWorld3dObject,
                                //            OurWorld3dObjectURI = ourWorld3dObjectURI,
                                //            OurWorld2dSprite = ourWorld2dSprite,
                                //            OurWorld2dSpriteURI = ourWorld2dSpriteURI,
                                //            OneWorldLat = oneWorldLat,
                                //            OneWorldLong = oneWorldLong,
                                //            OneWorld3dObject = oneWorld3dObject,
                                //            OneWorld3dObjectURI = oneWorld3dObjectURI,
                                //            OneWorld2dSprite = oneWorld2dSprite,
                                //            OneWorld2dSpriteURI = oneWorld2dSpriteURI
                                //        }, null, false, providerType);

                                //null, new OAPPDNA() //TODO: We can pass in custom OAPPDNA when figure out how to resole the cast issues in STARNETManagerBase! ;-) This code does allow custom data to be added to the root of the OAPPDNA.json file but tbh it looks better if its just stored in the MetaData above! ;-)
                                //{
                                //    CelestialBodyId = lightResult.Result.CelestialBody.Id,
                                //    CelestialBodyName = lightResult.Result.CelestialBody.Name,
                                //    GenesisType = genesisType,
                                //    OAPPTemplateId = installedOAPPTemplate.STARNETDNA.Id,
                                //    OAPPTemplateName = installedOAPPTemplate.STARNETDNA.Name,
                                //    OAPPTemplateDescription = installedOAPPTemplate.STARNETDNA.Description,
                                //    OAPPTemplateType = OAPPTemplateType,
                                //    OAPPTemplateVersion = installedOAPPTemplate.STARNETDNA.Version,
                                //    OAPPTemplateVersionSequence = installedOAPPTemplate.STARNETDNA.VersionSequence,
                                //    CelestialBodyMetaDataId = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Id : Guid.Empty,
                                //    CelestialBodyMetaDataName = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Name : null,
                                //    CelestialBodyMetaDataDescription = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Description : null,
                                //    CelestialBodyMetaDataType = celestialBodyMetaDataDNA != null ? (CelestialBodyType)Enum.Parse(typeof(CelestialBodyType), celestialBodyMetaDataDNA.STARNETDNA.STARNETHolonType.ToString()) : CelestialBodyType.Moon,
                                //    CelestialBodyMetaDataVersion = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.Version : null,
                                //    CelestialBodyMetaDataVersionSequence = celestialBodyMetaDataDNA != null ? celestialBodyMetaDataDNA.STARNETDNA.VersionSequence : 0,
                                //    CelestialBodyMetaDataGeneratedPath = celestialBodyMetaDataDNA != null ? cbMetaDataGeneratedPath : null,
                                //    STARNETHolonType = OAPPType,
                                //    OurWorldLat = ourWorldLat,
                                //    OurWorldLong = ourWorldLong,
                                //    OurWorld3dObject = ourWorld3dObject,
                                //    OurWorld3dObjectURI = ourWorld3dObjectURI,
                                //    OurWorld2dSprite = ourWorld2dSprite,
                                //    OurWorld2dSpriteURI = ourWorld2dSpriteURI,
                                //    OneWorldLat = oneWorlddLat,
                                //    OneWorldLong = oneWorldLong,
                                //    OneWorld3dObject = oneWorld3dObject,
                                //    OneWorld3dObjectURI = oneWorld3dObjectURI,
                                //    OneWorld2dSprite = oneWorld2dSprite,
                                //    OneWorld2dSpriteURI = oneWorld2dSpriteURI
                                //    //Zomes = lightResult.Result.CelestialBody.CelestialBodyCore.Zomes
                                //}, false, providerType);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleError(ref lightResult, $"Error Occured Creating The OAPP. Reason: {e.Message}");
                            }

                            if (createOAPPResult != null && createOAPPResult.Result != null && !createOAPPResult.IsError)
                            {
                                lightResult.Result.OAPP = createOAPPResult.Result;
                                OASISResult<bool> installRuntimesResult = null;

                                //Copy the template dependencies to the OAPP.
                                createOAPPResult.Result.STARNETDNA.Dependencies = installedOAPPTemplate.STARNETDNA.Dependencies;
                                OASISResult<OAPP> saveResult = await STARNETManager.UpdateAsync(STAR.BeamedInAvatar.Id, createOAPPResult.Result, true, providerType: providerType);

                                if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                {
                                    installRuntimesResult = await STARCLI.Runtimes.InstallOASISAndSTARRuntimesAsync(lightResult.Result.OAPP.STARNETDNA, oappPath, InstallRuntimesFor.OAPP, providerType);

                                    if (!(installRuntimesResult != null && installRuntimesResult.Result && !installRuntimesResult.IsError))
                                    {
                                        CLIEngine.ShowErrorMessage($"Error occured installing dependent runtimes for OAPP. Reason: {installRuntimesResult.Message}.\n\nPlease install these manually using the sub-command 'runtime install'");
                                        lightResult.IsError = true;
                                        lightResult.Message = installRuntimesResult.Message;
                                    }

                                    if (!string.IsNullOrEmpty(lightResult.Message) && !lightResult.IsError)
                                        CLIEngine.ShowSuccessMessage($"OAPP Successfully Generated. ({lightResult.Message})");
                                    else
                                        CLIEngine.ShowSuccessMessage($"OAPP Successfully Generated.");

                                    //await AddDependenciesAsync(createOAPPResult.Result.STARNETDNA, "OAPP", providerType);
                                    await AddDependenciesAsync(createOAPPResult.Result.STARNETDNA, providerType);

                                    OASISResult<STARNETDNA> dnaResult = await STARNETManager.ReadDNAFromSourceOrInstallFolderAsync<STARNETDNA>(lightResult.Result.OAPP.STARNETDNA.SourcePath);

                                    if (dnaResult != null && dnaResult.Result != null && !dnaResult.IsError)
                                        lightResult.Result.OAPP.STARNETDNA = dnaResult.Result;
                                    else
                                    {
                                        CLIEngine.ShowErrorMessage($"Error occured reading STARNETDNA. Reason: {dnaResult.Message}.");
                                        lightResult.IsError = true;
                                        lightResult.Message = installRuntimesResult.Message;
                                    }

                                    Console.WriteLine("");
                                    await ShowAsync(lightResult.Result.OAPP, customData: lightResult.Result.CelestialBody.CelestialBodyCore.Zomes);
                                    Console.WriteLine("");

                                    if (CLIEngine.GetConfirmation("Do you wish to open the OAPP now?"))
                                        Process.Start("explorer.exe", Path.Combine(oappPath, string.Concat(genesisNamespace, ".csproj")));

                                    Console.WriteLine("");

                                    if (CLIEngine.GetConfirmation("Do you wish to open the OAPP folder now?"))
                                        Process.Start("explorer.exe", oappPath);

                                    Console.WriteLine("");
                                    lightResult = await CreateOAPPComponentsOnSTARNETAsync(lightResult, oappPath, errorMessage, providerType);
                                }
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"Error Occured Saving The OAPP. Reason: {saveResult.Message}");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"Error Occured Creating The OAPP. Reason: {createOAPPResult?.Message}");
                        }
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error Occured: {lightResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Mapping MetaData To MetaTags: {metaTagMappingsResult.Result}");
            }

            return lightResult;
        }

    }
}
