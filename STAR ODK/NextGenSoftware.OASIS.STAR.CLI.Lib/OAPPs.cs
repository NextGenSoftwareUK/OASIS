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

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class OAPPs : STARNETUIBase<OAPP, DownloadedOAPP, InstalledOAPP, OAPPDNA>
    public class OAPPs : STARNETUIBase<OAPP, DownloadedOAPP, InstalledOAPP, STARNETDNA>
    {
        public OAPPs(Guid avatarId, STARDNA STARDNA) : base(new OAPPManager(avatarId, STARDNA),
            "Welcome to the OASIS Omniverse/MagicVerse Light Wizard!", new List<string>
            {
                "This wizard will allow you create an OAPP (Moon, Planet, Star & More) which will appear in the MagicVerse within the OASIS Omniverse.",
                "The OAPP will also optionally appear within the AR geo-location Our World/AR World platform/game in your desired geo-location.",
                "The OAPP will also optionally appear within the One World (Open World MMORPG) game/platform. VR support is also provided.",
                "The OAPP can have as many interfaces/lenses (way to visualize/represent the data of your OAPP) as you like, for example you can also have a 2D web view as well as a 3D view, Metaverse/Omniverse view, etc.",
                "Each OAPP is composed of zomes (re-usable/composable modules containing collections of holons) & holons (generic/composable re-usable OASIS Data Objects). This means the zomes and holons can be shared and re-used with other OAPPs within the STARNET Library. Different zomes and holons can be plugged together to form unique combinations for new OAPPs saving lots of time!",
                "Each OAPP is built/generated on top of a powerful easy to use ORM called (WEB5) COSMIC (The Worlds ORM because it aggregrates all of the worlds data into a simple to use ORM) which allows very easy data management across all of web2 and web3 making data interoperability and interchange very simple and makes silos a thing of the past!",
                "COSMIC is built on top of the powerful WEB4 OASIS API so each OAPP also has easy to use API's for manging keys, wallets, data, nfts, geo-nfts, providers, avatars, karma & much more!",
                "The OAPP can be created from anything you like such as a website, javascript template, game, app, service, etc in any language, platform or OS.",
                "Data can be shared between OAPP's but you are always in full control of your data, you own your data and you can choose exactly who and how that data is shared. You have full data sovereignty.",
                "Due to your OAPP being built on the OASIS API you also benefit from many other advanced features such as auto-replication, auto-failover and auto-load balancing so if one node goes down in your local area it will automatically find the next fastest one in your area irrespective of network.",
                "The more users your OAPP has the larger that celestial body (moon, planet or star) will appear within The MagicVerse. The higher the karma score of the owner (can be a individual or company/organisation) of the OAPP becomes the more it will glow and get closer that celestial bodies orbit will be to it's parent so if it's a moon it will get closer and closer to the planet and if it's a planet it will get closer and closer to it's star.",
                "The wizard will create a folder with a OAPPDNA.json file in it containing the files/folders generated from the OAPP Template you select. You can edit any of these files as well as place any additional files/folders you need into this folder.",
                "Finally you run the sub-command 'oapp publish' to convert the folder containing the OAPP (can contain any number of files and sub-folders) into a OAPP file (.oapp) as well as optionally upload to STARNET.",
                "You can then share the .oapp file with others across any platform or OS, who can then install the OAPP from the file using the sub-command 'oapp install'. You can also optionally choose to upload the .oapp file to STARNET so others can search, download and install the OAPP.",
                "You can also optionally choose to upload the .oapp file to the STARNET store so others can search, download and install the quest."
            },
            STAR.STARDNA.DefaultOAPPsSourcePath, "DefaultOAPPsSourcePath",
            STAR.STARDNA.DefaultOAPPsPublishedPath, "DefaultOAPPsPublishedPath",
            STAR.STARDNA.DefaultOAPPsDownloadedPath, "DefaultOAPPsDownloadedPath",
            STAR.STARDNA.DefaultOAPPsInstalledPath, "DefaultOAPPsInstalledPath")
        {
            //((OAPPManager)this.STARNETManager).OnOAPPDownloadStatusChanged += OnDownloadStatusChanged;
            //((OAPPManager)this.STARNETManager).OnOAPPInstallStatusChanged += OnInstallStatusChanged;
            //((OAPPManager)this.STARNETManager).OnOAPPPublishStatusChanged += OnPublishStatusChanged;
            //((OAPPManager)this.STARNETManager).OnOAPPUploadStatusChanged += OnUploadStatusChanged;
        }

        //public override void Dispose()
        //{
        //    ((OAPPManager)this.STARNETManager).OnOAPPDownloadStatusChanged -= OnDownloadStatusChanged;
        //    ((OAPPManager)this.STARNETManager).OnOAPPInstallStatusChanged -= OnInstallStatusChanged;
        //    ((OAPPManager)this.STARNETManager).OnOAPPPublishStatusChanged -= OnPublishStatusChanged;
        //    ((OAPPManager)this.STARNETManager).OnOAPPUploadStatusChanged -= OnUploadStatusChanged;

        //    base.Dispose();
        //}

        public override async Task<OASISResult<OAPP>> CreateAsync(object createParams, OAPP newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<CoronalEjection> result = await LightWizardAsync(createParams, providerType);

            return new OASISResult<OAPP>
            {
                IsError = result.IsError,
                Message = result.Message,
                Result = result.Result != null && result.Result.OAPP != null ? (OAPP)result.Result.OAPP : null
            };
        }

        public async Task<OASISResult<CoronalEjection>> LightWizardAsync(object createParams, ProviderType providerType = ProviderType.Default)
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

                        if (ourWorld3dObjectURI == null)
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
                                    object propType = CLIEngine.GetValidInputForEnum("What is the type of the Field/Property?", typeof(NodeType)); //typeof(HolonPropType));

                                    if (propType != null)
                                    {
                                        if (propType.ToString() == "exit")
                                        {
                                            lightResult.Message = "User Exited";
                                            return lightResult;
                                        }
                                        NodeType holonPropType = (NodeType)propType;

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
                            OAPPMetaDataDNAFolder = Path.Combine(STAR.STARDNA.BaseSTARPath, STAR.STARDNA.OAPPMetaDataDNAFolder);

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

                    if (!string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                        oappPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, STAR.STARDNA.DefaultOAPPsSourcePath);
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

                            //Finally, save this to the STARNET App Store. This will be private on the store until the user publishes via the Star.Seed() command.
                            OASISResult<OAPP> createOAPPResult = await STAR.STARAPI.OAPPs.CreateAsync(STAR.BeamedInAvatar.Id, OAPPName, OAPPDesc, OAPPType, oappPath, metaHolonTagMappingsResult.Result, metaTagMappingsResult.Result, new Dictionary<string, object>()
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
                            { "OneWorld2dSpriteURI", oneWorld2dSpriteURI } },
                                // { "Zomes", lightResult.Result.CelestialBody.CelestialBodyCore.Zomes } },
                                new OAPP() //TODO: For now we need to store the meta data here again otherwise when the holon is saved the blank props will override the metadata keyvalues above! Strongly typed overrides the keyvalue pairs but the metadata above is needed to store in the STARNETDNA, will try to remove this duplication later! ;-)
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
                                }, null, false, providerType);

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
                                    Show(lightResult.Result.OAPP, customData: lightResult.Result.CelestialBody.CelestialBodyCore.Zomes);
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
                                CLIEngine.ShowErrorMessage($"Error Occured Creating The OAPP. Reason: {createOAPPResult.Message}");
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



        public override async Task<OASISResult<OAPP>> PublishAsync(string sourcePath = "", bool edit = false, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.Optional, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        {
            return await PublishAsync(sourcePath, edit, defaultLaunchMode, askToInstallAtEnd, providerType);
        }

        public async Task<OASISResult<IOAPP>> PublishAsync(string sourcePath = "", bool edit = false, bool dotNetPublish = false, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            bool generateOAPPSource = false;
            bool uploadOAPPSource = false;
            bool generateOAPP = true;
            bool uploadOAPPToCloud = false;
            bool generateOAPPSelfContained = false;
            bool uploadOAPPSelfContainedToCloud = false;
            bool generateOAPPSelfContainedFull = false;
            bool uploadOAPPSelfContainedFullToCloud = false;
            bool makeOAPPSourcePublic = false;
            ProviderType OAPPBinaryProviderType = ProviderType.None; //ProviderType.IPFSOASIS;
            ProviderType OAPPSelfContainedBinaryProviderType = ProviderType.None; //ProviderType.IPFSOASIS;
            ProviderType OAPPSelfContainedFullBinaryProviderType = ProviderType.None; //ProviderType.IPFSOASIS;
            bool registerOnSTARNET = false;
            string STARNETInfo = "If you select 'Y' to this question then your OAPP will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .oapp install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.";

            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage("Welcome to the OAPP Publish Wizard!");
            CLIEngine.ShowDivider();
            Console.WriteLine();
            CLIEngine.ShowMessage("This wizard will publish your OAPP to STARNET. There are 4 ways of doing this:");
            CLIEngine.ShowMessage("1. Publish the standard OAPP (.oapp) file with no runtimes bundled with it. (Default & recommended). The target machine will need to have the .NET, OASIS & STAR runtimes installed.");
            CLIEngine.ShowMessage("2. Publish the standard OAPP (.oappselfcontained) file bundled with the OASIS & STAR runtimes (approx 210MB). The target machine will need to have the .NET runtime installed.");
            CLIEngine.ShowMessage("3. Publish the standard OAPP (.oappselfcontainedfull) file bundled with the OASIS, STAR runtimes & .NET runtimes (approx 500MB). No dependencies needed, fully self-contained.");
            CLIEngine.ShowMessage("4. Publish the OAPP source (.oappsource) file which only contains the source. People can then download the source and build the OAPP on their machine (if they are missing any of the dependencies such as the runtimes these will be automatically restored). NOTE: This means your source would NEED to be made public (not a problem for Open Source etc).");
            CLIEngine.ShowMessage("Each approach has pros and cons with 4 being the smallest and then 2,3 and 4. Smaller means quicker upload and download and less storage space required (lower hosting costs) but also comes with the risk there may be problems building (4 only) or running the OAPP on the target machine if they are missing any of the dependencies such as the runtimes etc. Another advantage of 1,2 & 3 is the launch target is verified in the pre-built OAPP.");
            CLIEngine.ShowMessage("If you choose the Simple Wizard yhen option 1 will be chosen by default, if you wish to choose another option or a combination of options you must choose the Advanced Wizard.");

            CLIEngine.ShowDivider();

            if (string.IsNullOrEmpty(sourcePath))
            {
                string OAPPPathQuestion = "What is the full path to the (dotnet) published output for the OAPP you wish to publish?";
                //launchTargetQuestion = "What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the OAPP? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";

                if (!CLIEngine.GetConfirmation("Have you already published the OAPP within Visual Studio (VS), Visual Studio Code (VSCode) or using the dotnet command? (If your OAPP is using a non dotnet template you can answer 'N')."))
                {
                    OAPPPathQuestion = "What is the full path to the OAPP you wish to publish?";
                    dotNetPublish = true;
                    Console.WriteLine();
                    CLIEngine.ShowMessage("No worries, we will do that for you (if it's a dotnet OAPP)! ;-)");
                }
                else
                    Console.WriteLine();

                sourcePath = CLIEngine.GetValidFolder(OAPPPathQuestion, false);
            }

            //OASISResult<IOAPPDNA> OAPPDNAResult = await STAR.STARAPI.OAPPs.ReadDNAFromSourceOrInstallFolderAsync<IOAPPDNA>(sourcePath);

            //if (OAPPDNAResult != null && OAPPDNAResult.Result != null && !OAPPDNAResult.IsError)
            //{
            //    switch (OAPPDNAResult.Result.OAPPTemplateType)
            //    {
            //        case OAPPTemplateType.Console:
            //        case OAPPTemplateType.WPF:
            //        case OAPPTemplateType.WinForms:
            //            launchTarget = $"{OAPPDNAResult.Result.Name}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
            //                                                                   //launchTarget = $"bin\\Release\\net8.0\\{OAPPDNAResult.Result.OAPPName}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
            //            break;

            //        case OAPPTemplateType.Blazor:
            //        case OAPPTemplateType.MAUI:
            //        case OAPPTemplateType.WebMVC:
            //            //launchTarget = $"bin\\Release\\net8.0\\index.html"; 
            //            launchTarget = $"index.html";
            //            break;
            //    }

            //    if (!string.IsNullOrEmpty(launchTarget))
            //    {
            //        if (!CLIEngine.GetConfirmation($"{launchTargetQuestion} Do you wish to use the following default launch target: {launchTarget}?"))
            //            launchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", sourcePath);
            //        else
            //            launchTarget = Path.Combine(sourcePath, launchTarget);
            //    }
            //    else
            //        launchTarget = CLIEngine.GetValidFile(launchTargetQuestion, sourcePath);


            //((OAPPManager)this.STARNETManager).OnOAPPDownloadStatusChanged += OnDownloadStatusChanged;
            //((OAPPManager)this.STARNETManager).OnOAPPInstallStatusChanged += OnInstallStatusChanged;
            //((OAPPManager)this.STARNETManager).OnOAPPPublishStatusChanged += OnPublishStatusChanged;
            //((OAPPManager)this.STARNETManager).OnOAPPUploadStatusChanged += OnUploadStatusChanged;


            OASISResult<BeginPublishResult> beginPublishResult = await BeginPublishingAsync(sourcePath, DefaultLaunchMode.Mandatory, providerType);

            if (beginPublishResult != null && !beginPublishResult.IsError && beginPublishResult.Result != null)
            {
                if (beginPublishResult.Result.SimpleWizard)
                {
                    registerOnSTARNET = true;
                    uploadOAPPToCloud = true;
                }
                else
                {
                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to generate the standard .oapp file? (Recommended). This file contains only the built & published OAPP source code. NOTE: You will need to make sure the target machine that runs this OAPP has both the appropriate OASIS & STAR ODK Runtimes installed along with the appropriate .NET Runtime."))
                    {
                        generateOAPP = true;
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to upload/publish the .oapp file to STARNET? {STARNETInfo}"))
                        {
                            Console.WriteLine("");
                            if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                                uploadOAPPToCloud = true;

                            Console.WriteLine("");
                            if (!beginPublishResult.Result.SimpleWizard)
                            {
                                object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                                if (OAPPBinaryProviderTypeObject != null)
                                {
                                    if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                    {
                                        result.Message = "User Exited";
                                        return result;
                                    }
                                    else
                                        OAPPBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                                }
                            }
                        }
                    }

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained .oapp file? This file contains the built & published OAPP source code along with the OASIS & STAR ODK Runtimes. NOTE: You will need to make sure the target machine that runs this OAPP has the appropriate .NET runtime installed. The file will be a minimum of 210 MB."))
                    {
                        generateOAPPSelfContained = true;
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to upload/publish the self-contained .oapp file to STARNET?"))
                        {
                            Console.WriteLine("");
                            if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                                uploadOAPPSelfContainedToCloud = true;

                            Console.WriteLine("");
                            object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                            if (OAPPBinaryProviderTypeObject != null)
                            {
                                if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                {
                                    result.Message = "User Exited";
                                    return result;
                                }
                                else
                                    OAPPSelfContainedBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                            }
                        }
                    }

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained (full) .oapp file? This file contains the built & published OAPP source code along with the OASIS, STAR ODK & .NET Runtimes. NOTE: The file will be a minimum of 500 MB."))
                    {
                        generateOAPPSelfContainedFull = true;
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to upload/publish the self-contained (full) .oapp file to STARNET?"))
                        {
                            Console.WriteLine("");
                            if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                                uploadOAPPSelfContainedFullToCloud = true;

                            Console.WriteLine("");
                            object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                            if (OAPPBinaryProviderTypeObject != null)
                            {
                                if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                {
                                    result.Message = "User Exited";
                                    return result;
                                }
                                else
                                    OAPPSelfContainedFullBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                            }
                        }
                    }

                    if (!uploadOAPPToCloud && OAPPBinaryProviderType == ProviderType.None &&
                        !uploadOAPPSelfContainedToCloud && OAPPSelfContainedBinaryProviderType == ProviderType.None &&
                        !uploadOAPPSelfContainedFullToCloud && OAPPSelfContainedFullBinaryProviderType == ProviderType.None)
                        CLIEngine.ShowMessage("Since you did not select to upload to the cloud or OASIS storage the oapp will not be published to STARNET.");
                    else
                        registerOnSTARNET = true;

                    Console.WriteLine("");
                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to generate a .oappsource file?"))
                    {
                        generateOAPPSource = true;
                        Console.WriteLine("");
                        if (CLIEngine.GetConfirmation("Do you wish to upload the .oappsource file to STARNET? The next question will ask if you wish to make this public. You may choose to upload and keep private as an extra backup for your code for example."))
                        {
                            uploadOAPPSource = true;
                            Console.WriteLine("");

                            if (CLIEngine.GetConfirmation("Do you wish to make the .oappsource public? People will be able to view your code so only do this if you are happy with this. NOTE: If you select 'N' to this question then people will not be able to download, build, publish and install your OAPP from your .oappsource file. You will need to upload the full pre-built & published .oapp file using one of the other options above if you want people to be able to download and install your OAPP from STARNET. If you wish people to be able to download and install from your .oappsource file then select 'Y'."))
                                makeOAPPSourcePublic = true;
                        }
                    }
                }

                //Console.WriteLine("");
                Console.WriteLine("");
                //OASISResult<string> pubPathResult = await GetPublishPathAsync(beginPublishResult.Result, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
                OASISResult<string> pubPathResult = await GetPublishPathAsync(beginPublishResult.Result.SourcePath, beginPublishResult.Result.SimpleWizard, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);

                if (pubPathResult != null && !string.IsNullOrEmpty(pubPathResult.Result) && !pubPathResult.IsError)
                {
                    result = await ((OAPPManager)STARNETManager).PublishOAPPAsync(STAR.BeamedInAvatar.Id, sourcePath, beginPublishResult.Result.LaunchTarget, pubPathResult.Result, edit, registerOnSTARNET, dotNetPublish, generateOAPPSource, uploadOAPPSource, makeOAPPSourcePublic, generateOAPP, generateOAPPSelfContained, generateOAPPSelfContainedFull, uploadOAPPToCloud, uploadOAPPSelfContainedToCloud, uploadOAPPSelfContainedFullToCloud, providerType, OAPPBinaryProviderType, OAPPSelfContainedBinaryProviderType, OAPPSelfContainedFullBinaryProviderType, beginPublishResult.Result.EmbedRuntimes, beginPublishResult.Result.EmbedLibs, beginPublishResult.Result.EmbedTemplates);
                    OASISResult<OAPP> publishResult = new OASISResult<OAPP>((OAPP)result.Result);
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(result, publishResult);
                    await PostFininaliazePublishingAsync(publishResult, sourcePath, askToInstallAtEnd, providerType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETUIBase.FininaliazePublishingAsync calling PreFininaliazePublishingAsync. Reason: {pubPathResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured: {beginPublishResult.Message}");

            return result;
        }

        public override async Task<OASISResult<InstalledOAPP>> DownloadAndInstallAsync(string idOrName = "", InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<InstalledOAPP> installResult = await base.DownloadAndInstallAsync(idOrName, installMode, providerType);

            if (installResult != null && installResult.Result != null && !installResult.IsError)
            {
                //Install any dependencies that are required for the OAPP to run (such as runtimes etc).
                OASISResult<bool> installRuntimesResult = await STARCLI.Runtimes.InstallOASISAndSTARRuntimesAsync(installResult.Result.STARNETDNA, installResult.Result.InstalledPath, InstallRuntimesFor.OAPP, providerType);

                if (!(installRuntimesResult != null && installRuntimesResult.Result && !installRuntimesResult.IsError))
                {
                    CLIEngine.ShowErrorMessage($"Error occured installing dependent runtimes for OAPP. Reason: {installRuntimesResult.Message}. Please install these manually using the sub-command 'runtime install'");
                    installResult.IsError = true;
                    installResult.Message = installRuntimesResult.Message;
                }
            }

            return installResult;
        }

        public override void Show<OAPP>(OAPP oapp, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH, object customData = null)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                DisplayProperty("Number", number.ToString(), displayFieldLength);

            CLIEngine.ShowMessage(string.Concat($"Id:".PadRight(displayFieldLength), oapp.STARNETDNA.Id != Guid.Empty ? oapp.STARNETDNA.Id : "None"), false);
            DisplayProperty("Name", !string.IsNullOrEmpty(oapp.STARNETDNA.Name) ? oapp.STARNETDNA.Name : "None", displayFieldLength);
            DisplayProperty("Description", !string.IsNullOrEmpty(oapp.STARNETDNA.Description) ? oapp.STARNETDNA.Description : "None", displayFieldLength);
            DisplayProperty("Type", oapp.STARNETDNA.STARNETHolonType, displayFieldLength);
            DisplayProperty("Category", oapp.STARNETDNA.STARNETCategory.ToString(), displayFieldLength);
            DisplayProperty("Genesis Type", ParseMetaDataForEnum(oapp.MetaData, "GenesisType", typeof(GenesisType)), displayFieldLength);
            DisplayProperty("Celestial Body Id", ParseMetaData(oapp.MetaData, "CelestialBodyId"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("OAPP TEMPLATE", "", displayFieldLength, false);
            DisplayProperty("Id", ParseMetaData(oapp.MetaData, "OAPPTemplateId"), displayFieldLength);
            DisplayProperty("Name", ParseMetaData(oapp.MetaData, "OAPPTemplateName"), displayFieldLength);
            DisplayProperty("Description", ParseMetaData(oapp.MetaData, "OAPPTemplateDescription"), displayFieldLength);
            DisplayProperty("Category", ParseMetaDataForEnum(oapp.MetaData, "OAPPTemplateType", typeof(OAPPTemplateType)), displayFieldLength);
            DisplayProperty("Version", ParseMetaData(oapp.MetaData, "OAPPTemplateVersion"), displayFieldLength);
            DisplayProperty("Version Sequence", ParseMetaData(oapp.MetaData, "OAPPTemplateVersionSequence"), displayFieldLength);
            DisplayProperty("Installed Path", ParseMetaData(oapp.MetaData, "OAPPTemplateInstalledPath"), displayFieldLength);
            ShowHolonMetaTagMappings(oapp.STARNETDNA.MetaTagMappings.MetaHolonTags, showDetailedInfo, displayFieldLength);
            ShowMetaTagMappings(oapp.STARNETDNA.MetaTagMappings.MetaTags, showDetailedInfo, displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("CELESTIAL BODY META DATA DNA", "", displayFieldLength, false);
            DisplayProperty("Id", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataId"), displayFieldLength);
            DisplayProperty("Name", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataName"), displayFieldLength);
            DisplayProperty("Description", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataDescription"), displayFieldLength);
            DisplayProperty("Type", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataType"), displayFieldLength);
            DisplayProperty("Version", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataVersion"), displayFieldLength);
            DisplayProperty("Version Sequence", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataVersionSequence"), displayFieldLength);
            DisplayProperty("Installed Path", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataInstalledPath"), displayFieldLength);
            DisplayProperty("Generated Path", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataGeneratedPath"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("Our World Lat/Long", ParseMetaDataForLatLong(oapp.MetaData, "OurWorldLat", "OurWorldLong"), displayFieldLength);
            DisplayProperty("Our World 3D Object", ParseMetaDataForBinaryUploadAndURI(oapp.MetaData, "OurWorld3dObject", "OurWorld3dObjectURI"), displayFieldLength);
            DisplayProperty("Our World 2D Sprite", ParseMetaDataForBinaryUploadAndURI(oapp.MetaData, "OurWorld2dSprite", "OurWorld2dSpriteURI"), displayFieldLength);
            DisplayProperty("One World Lat/Long", ParseMetaDataForLatLong(oapp.MetaData, "OneWorldLat", "OneWorldLong"), displayFieldLength);
            DisplayProperty("One World 3D Object", ParseMetaDataForBinaryUploadAndURI(oapp.MetaData, "OneWorld3dObject", "OneWorld3dObjectURI"), displayFieldLength);
            DisplayProperty("One World 2D Sprite", ParseMetaDataForBinaryUploadAndURI(oapp.MetaData, "OneWorld2dSprite", "OneWorld2dSpriteURI"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("Source Path", !string.IsNullOrEmpty(oapp.STARNETDNA.SourcePath) ? oapp.STARNETDNA.SourcePath : "None", displayFieldLength);
            DisplayProperty("Published On", oapp.STARNETDNA.PublishedOn != DateTime.MinValue ? oapp.STARNETDNA.PublishedOn.ToString() : "None", displayFieldLength);
            DisplayProperty("Published By", oapp.STARNETDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(oapp.STARNETDNA.PublishedByAvatarUsername, " (", oapp.STARNETDNA.PublishedByAvatarId.ToString(), ")") : "None", displayFieldLength);
            DisplayProperty("Published Path", !string.IsNullOrEmpty(oapp.STARNETDNA.PublishedPath) ? oapp.STARNETDNA.PublishedPath : "None", displayFieldLength);
            DisplayProperty("Filesize", oapp.STARNETDNA.FileSize > 0 ? oapp.STARNETDNA.FileSize.ToString() : "None", displayFieldLength);
            DisplayProperty("Published On STARNET", oapp.STARNETDNA.PublishedOnSTARNET ? "True" : "False", displayFieldLength);
            DisplayProperty("Published To Cloud", oapp.STARNETDNA.PublishedToCloud ? "True" : "False", displayFieldLength);
            DisplayProperty("Published To OASIS Provider", oapp.STARNETDNA.PublishedProviderType, displayFieldLength);
            DisplayProperty("Launch Target", !string.IsNullOrEmpty(oapp.STARNETDNA.LaunchTarget) ? oapp.STARNETDNA.LaunchTarget : "None", displayFieldLength);
            DisplayProperty($"{STARNETManager.STARNETHolonUIName} Version", oapp.STARNETDNA.Version, displayFieldLength);
            DisplayProperty("Version Sequence", oapp.STARNETDNA.VersionSequence.ToString(), displayFieldLength);
            DisplayProperty("Number Of Versions", oapp.STARNETDNA.NumberOfVersions.ToString(), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("SELF CONTAINED", "", displayFieldLength, false);
            DisplayProperty("Published Path", ParseMetaData(oapp.MetaData, "SelfContainedPublishedPath"), displayFieldLength);
            DisplayProperty("Filesize", ParseMetaData(oapp.MetaData, "SelfContainedFileSize"), displayFieldLength);
            DisplayProperty("Published To Cloud", ParseMetaData(oapp.MetaData, "SelfContainedPublishedToCloud", "False"), displayFieldLength);
            DisplayProperty("Published To OASIS Provider", ParseMetaData(oapp.MetaData, "SelfContainedPublishedProviderType"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("SELF CONTAINED FULL", "", displayFieldLength, false);
            DisplayProperty("Published Path", ParseMetaData(oapp.MetaData, "SelfContainedFullPublishedPath"), displayFieldLength);
            DisplayProperty("Filesize", ParseMetaData(oapp.MetaData, "SelfContainedFullFileSize"), displayFieldLength);
            DisplayProperty("Published To Cloud", ParseMetaData(oapp.MetaData, "SelfContainedFullPublishedToCloud", "False"), displayFieldLength);
            DisplayProperty("Published To OASIS Provider", ParseMetaData(oapp.MetaData, "SelfContainedFullPublishedProviderType"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("SOURCE CODE ONLY", "", displayFieldLength, false);
            DisplayProperty("Published Path", ParseMetaData(oapp.MetaData, "SourcePublishedPath"), displayFieldLength);
            DisplayProperty("Filesize", ParseMetaData(oapp.MetaData, "SourceFileSize"), displayFieldLength);
            DisplayProperty("Published On STARNET", ParseMetaData(oapp.MetaData, "SourcePublishedOnSTARNET", "False"), displayFieldLength);
            DisplayProperty("Public On STARNET", ParseMetaData(oapp.MetaData, "SourcePublicOnSTARNET", "False"), displayFieldLength);


            //DisplayProperty("OASIS Holon Version:                        ", oapp.Version), displayFieldLength);
            //DisplayProperty("OASIS Holon VersionId:                      ", oapp.VersionId), displayFieldLength);
            //DisplayProperty("OASIS Holon PreviousVersionId:              ", oapp.PreviousVersionId), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("Downloads", oapp.STARNETDNA.Downloads.ToString(), displayFieldLength);
            DisplayProperty("Installs", oapp.STARNETDNA.Installs.ToString(), displayFieldLength);
            DisplayProperty("Total Downloads", oapp.STARNETDNA.TotalDownloads.ToString(), displayFieldLength);
            DisplayProperty("Total Installs", oapp.STARNETDNA.TotalInstalls.ToString(), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("OASIS Runtime Version", oapp.STARNETDNA.OASISRuntimeVersion.ToString(), displayFieldLength);
            DisplayProperty("OASIS API Version", oapp.STARNETDNA.OASISAPIVersion.ToString(), displayFieldLength);
            DisplayProperty("COSMIC Version", oapp.STARNETDNA.COSMICVersion.ToString(), displayFieldLength);
            DisplayProperty("STAR Runtime Version", oapp.STARNETDNA.STARRuntimeVersion.ToString(), displayFieldLength);
            DisplayProperty("STAR ODK Version", oapp.STARNETDNA.STARODKVersion.ToString(), displayFieldLength);
            DisplayProperty("STARNET Version", oapp.STARNETDNA.STARNETVersion.ToString(), displayFieldLength);
            DisplayProperty("STAR API Version", oapp.STARNETDNA.STARAPIVersion.ToString(), displayFieldLength);
            DisplayProperty(".NET Version", oapp.STARNETDNA.DotNetVersion.ToString(), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("Created On", oapp.STARNETDNA.CreatedOn != DateTime.MinValue ? oapp.STARNETDNA.CreatedOn.ToString() : "None", displayFieldLength);
            DisplayProperty("Created By", oapp.STARNETDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(oapp.STARNETDNA.CreatedByAvatarUsername, " (", oapp.STARNETDNA.CreatedByAvatarId.ToString(), ")") : "None", displayFieldLength);
            DisplayProperty("Modified On", oapp.STARNETDNA.ModifiedOn != DateTime.MinValue ? oapp.STARNETDNA.CreatedOn.ToString() : "None", displayFieldLength);
            DisplayProperty("Modified By", oapp.STARNETDNA.ModifiedByAvatarId != Guid.Empty ? string.Concat(oapp.STARNETDNA.ModifiedByAvatarUsername, " (", oapp.STARNETDNA.ModifiedByAvatarId.ToString(), ")") : "None", displayFieldLength);
            DisplayProperty("Active", oapp.MetaData != null && oapp.MetaData.ContainsKey("Active") && oapp.MetaData["Active"] != null && oapp.MetaData["Active"].ToString() == "1" ? "True" : "False", displayFieldLength);

            ShowAllDependencies(oapp, showDetailedInfo, displayFieldLength);

            if (customData != null)
            {
                List<IZome> zomes = customData as List<IZome>;

                if (zomes != null && zomes.Count > 0)
                {
                    Console.WriteLine("");
                    STARCLI.Zomes.ShowZomesAndHolons(zomes);
                }
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }


        private void DisplaySTARNETHolonMetaData(ISTARNETDependency metaData, int displayFieldLength)
        {
            DisplayProperty("Id", metaData.STARNETHolonId.ToString(), displayFieldLength);
            DisplayProperty("Name", metaData.Name, displayFieldLength);
            DisplayProperty("Description", metaData.Description, displayFieldLength);
            DisplayProperty("Version", metaData.Version, displayFieldLength);
            DisplayProperty("Version Sequence", metaData.VersionSequence.ToString(), displayFieldLength);
            DisplayProperty("Installed From", metaData.InstalledFrom, displayFieldLength);
            DisplayProperty("Installed To", metaData.InstalledTo, displayFieldLength);
            Console.WriteLine("");
        }

        private async Task<(OASISResult<CoronalEjection>, CelestialBodyMetaDataDNA)> CreateMetaDataOnSTARNETAsync(OASISResult<CoronalEjection> lightResult, IGenerateMetaDataDNAResult generateResult, GenesisType genesisType, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            CelestialBodyMetaDataDNA celestialBodyMetaDataDNA = null;

            if (CLIEngine.GetConfirmation("Would you like to upload the generated metadata DNA to STARNET so you or others (if you choose to make it public) can re-use for other OAPP's?"))
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the CelestialBody generated metadata DNA to STARNET?"))
                {
                    Console.WriteLine("");
                    CelestialBodyType celestialBodyType = CelestialBodyType.Moon;

                    switch (genesisType)
                    {
                        case GenesisType.Moon:
                            celestialBodyType = CelestialBodyType.Moon;
                            break;

                        case GenesisType.Planet:
                            celestialBodyType = CelestialBodyType.Planet;
                            break;

                        case GenesisType.Star:
                            celestialBodyType = CelestialBodyType.Star;
                            break;

                        case GenesisType.SuperStar:
                            celestialBodyType = CelestialBodyType.SuperStar;
                            break;

                        case GenesisType.GrandSuperStar:
                            celestialBodyType = CelestialBodyType.GrandSuperStar;
                            break;
                    }

                    OASISResult<CelestialBodyMetaDataDNA> createResult = await STARCLI.CelestialBodiesMetaDataDNA.CreateAsync(null, null, true, false, celestialBodyType, providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        celestialBodyMetaDataDNA = createResult.Result;

                        try
                        {
                            DirectoryHelper.CopyFilesRecursively(generateResult.CelestialBodyMetaDataDNAPath, createResult.Result.STARNETDNA.SourcePath);
                            CLIEngine.ShowSuccessMessage("CelestialBody MetaData DNA successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<CelestialBodyMetaDataDNA> publishResult = await STARCLI.CelestialBodiesMetaDataDNA.PublishAsync(createResult.Result.STARNETDNA.SourcePath, false, DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("CelestialBody MetaData DNA successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the CelestialBody MetaData DNA in STAR.CLI.Lib.CelestialBodiesMetaDataDNA.PublishAsync. Reason: {publishResult.Message}");
                            }
                            else
                                Console.WriteLine("");
                        }
                        catch (Exception e)
                        {
                            OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured attempting to copy the CelestialBodyMetaDataDNA from {generateResult.CelestialBodyMetaDataDNAPath} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured in STAR.CLI.Lib.CelestialBodiesMetaDataDNA.CreateAsync. Reason: {createResult.Message}");
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the Zome generated metadata DNA to STARNET?"))
                {
                    Console.WriteLine("");
                    OASISResult<ZomeMetaDataDNA> createResult = await STARCLI.ZomesMetaDataDNA.CreateAsync(null, null, true, false, providerType: providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        try
                        {
                            DirectoryHelper.CopyFilesRecursively(generateResult.ZomeMetaDataDNAPath, createResult.Result.STARNETDNA.SourcePath);
                            CLIEngine.ShowSuccessMessage("Zome MetaData DNA successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it/them now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<ZomeMetaDataDNA> publishResult = await STARCLI.ZomesMetaDataDNA.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("Zome MetaData DNA successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Zome MetaData DNA in STAR.CLI.Lib.ZomesMetaDataDNA.PublishAsync. Reason: {publishResult.Message}");
                            }
                            else
                                Console.WriteLine("");
                        }
                        catch (Exception e)
                        {
                            OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured attempting to copy the ZomeMetaDataDNA from {generateResult.CelestialBodyMetaDataDNAPath} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured in STAR.CLI.Lib.ZomesMetaDataDNA.CreateAsync. Reason: {createResult.Message}");
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the Holon generated metadata DNA to STARNET?"))
                {
                    Console.WriteLine("");
                    OASISResult<HolonMetaDataDNA> createResult = await STARCLI.HolonsMetaDataDNA.CreateAsync(null, null, true, false, providerType: providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        try
                        {
                            DirectoryHelper.CopyFilesRecursively(generateResult.HolonMetaDataDNAPath, createResult.Result.STARNETDNA.SourcePath);
                            CLIEngine.ShowSuccessMessage("Holon MetaData DNA successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it/them now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<HolonMetaDataDNA> publishResult = await STARCLI.HolonsMetaDataDNA.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("Holon MetaData DNA successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Holon MetaData DNA in STAR.CLI.Lib.HolonsMetaDataDNA.PublishAsync. Reason: {publishResult.Message}");
                            }
                            else
                                Console.WriteLine("");
                        }
                        catch (Exception e)
                        {
                            OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured attempting to copy the ZomeMetaDataDNA from {generateResult.HolonMetaDataDNAPath} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured in STAR.CLI.Lib.HolonsMetaDataDNA.CreateAsync. Reason: {createResult.Message}");
                }
                else
                    Console.WriteLine("");
            }
            else
                Console.WriteLine("");

            return (lightResult, celestialBodyMetaDataDNA);
        }

        private OASISResult<CoronalEjection> CopyGeneratedCodeToSTARNET<T>(OASISResult<CoronalEjection> result, OASISResult<T> createResult, string holonDisplayName, string sourcePath, string generatedCodeSubFolder, string errorMessage, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon
        {
            string path = Path.Combine(sourcePath, STAR.STARDNA.OAPPGeneratedCodeFolder, "CSharp", generatedCodeSubFolder);

            try
            {
                DirectoryHelper.CopyFilesRecursively(path, createResult.Result.STARNETDNA.SourcePath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to copy the {holonDisplayName} from {path} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
            }

            path = Path.Combine(sourcePath, STAR.STARDNA.OAPPGeneratedCodeFolder, "CSharp", "Interfaces", generatedCodeSubFolder);

            try
            {
                DirectoryHelper.CopyFilesRecursively(path, createResult.Result.STARNETDNA.SourcePath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to copy the {holonDisplayName} from {path} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
            }

            return result;
        }

        private async Task<OASISResult<CoronalEjection>> CreateOAPPComponentsOnSTARNETAsync(OASISResult<CoronalEjection> lightResult, string OAPPSourcePath, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            if (CLIEngine.GetConfirmation("Would you like to upload the generated OAPP Components (CelestialBody, Zomes & Holons) DNA to STARNET so you or others (if you choose to make it public) can re-use for other OAPP's?"))
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the CelestialBody generated to STARNET?"))
                {
                    Console.WriteLine("");
                    OASISResult<STARCelestialBody> createResult = await STARCLI.CelestialBodies.CreateAsync(null, null, true, false, providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        lightResult = CopyGeneratedCodeToSTARNET(lightResult, createResult, "CelestialBody", OAPPSourcePath, "CelestialBodies", errorMessage, providerType);

                        if (lightResult != null && lightResult.Result != null && !lightResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("CelestialBody successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<STARCelestialBody> publishResult = await STARCLI.CelestialBodies.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("CelestialBody successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Zome(s) in STAR.CLI.Lib.CelestialBodies.PublishAsync. Reason: {publishResult.Message}");
                            }
                            else
                                Console.WriteLine("");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured creating the STARNET CelestialBody in STAR.CLI.Lib.CelestialBodies.CreateAsync. Reason: {createResult.Message}");
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the Zome(s) generated to STARNET?"))
                {
                    OASISResult<STARZome> createResult = await STARCLI.Zomes.CreateAsync(null, null, true, false, providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        lightResult = CopyGeneratedCodeToSTARNET(lightResult, createResult, "Zomes", OAPPSourcePath, "Zomes", errorMessage, providerType);
                        CLIEngine.ShowSuccessMessage("Zome(s) successfully created on STARNET!");

                        if (CLIEngine.GetConfirmation("Would you like to publish it/them now?"))
                        {
                            Console.WriteLine("");
                            OASISResult<STARZome> publishResult = await STARCLI.Zomes.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                            if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                CLIEngine.ShowSuccessMessage("Zome(s) successfully uploaded to STARNET!");
                            else
                                OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Zome(s) in STAR.CLI.Lib.Zomes.PublishAsync. Reason: {publishResult.Message}");
                        }
                        else
                            Console.WriteLine("");
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured creating the Zome in STAR.CLI.Lib.Zomes.CreateAsync. Reason: {createResult.Message}");
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the Holon(s) generated to STARNET?"))
                {
                    OASISResult<STARHolon> createResult = await STARCLI.Holons.CreateAsync(null, null, true, false, providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        lightResult = CopyGeneratedCodeToSTARNET(lightResult, createResult, "Holons", OAPPSourcePath, "Holons", errorMessage, providerType);

                        if (lightResult != null && lightResult.Result != null && !lightResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("Holon(s) successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it/them now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<STARHolon> publishResult = await STARCLI.Holons.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("Holon(s) successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Holon(s) in STAR.CLI.Lib.Holons.PublishAsync. Reason: {publishResult.Message}");

                                //    createResult.Result.STARNETDNA.IsPublic = CLIEngine.GetConfirmation("Would you like to make the Holon(s) public on STARNET?");
                                //else
                                //    createResult.Result.STARNETDNA.IsPublic = false;
                            }
                            else
                                Console.WriteLine("");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured creating the Holon(s) in STAR.CLI.Lib.Holons.CreateAsync. Reason: {createResult.Message}");
                }
                else
                    Console.WriteLine("");
            }
            else
                Console.WriteLine("");

            return lightResult;
        }

        //private OASISResult<Dictionary<string, (string, string)>> MapCustomHolonMetaTagsToTemplate(IInstalledOAPPTemplate oappTemplate, Dictionary<string, IList<INode>> nodes)
        private OASISResult<List<MetaHolonTag>> MapCustomHolonMetaTagsToTemplate(IInstalledOAPPTemplate oappTemplate, Dictionary<string, IList<INode>> nodes)
        {
            //OASISResult<Dictionary<string, (string, string)>> result = new OASISResult<Dictionary<string, (string, string)>>(new Dictionary<string, (string, string)>());
            OASISResult<List<MetaHolonTag>> result = new OASISResult<List<MetaHolonTag>>(new List<MetaHolonTag>());

            try
            {
                int nodesTotal = CountNodes(nodes);
                OASISResult<List<string>> getCustomTagsResult = GetCustomTagsFromTemplate(oappTemplate.InstalledPath, new List<string>(), "{{", "}}");

                if (getCustomTagsResult != null && getCustomTagsResult.Result != null && !getCustomTagsResult.IsError)
                {
                    CLIEngine.ShowMessage(string.Concat($"Found {getCustomTagsResult.Result.Count} custom holon tag(s) in the OAPP Template '{oappTemplate.STARNETDNA.Name}'", getCustomTagsResult.Result.Count > 0 ? ":" : "."));

                    if (getCustomTagsResult.Result.Count > 0)
                    {
                        Console.WriteLine("");

                        foreach (string tag in getCustomTagsResult.Result)
                            CLIEngine.ShowMessage(tag, false);
                    }

                    CLIEngine.ShowMessage(string.Concat($"Found {nodesTotal} holon meta data node(s)", nodesTotal > 0 ? ":" : "."));
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("HOLON".PadRight(20), "NODE".PadRight(20), "TYPE".PadRight(20)), false);
                    Console.WriteLine("");

                    foreach (string holonName in nodes.Keys)
                    {
                        foreach (INode node in nodes[holonName])
                            CLIEngine.ShowMessage(string.Concat(holonName.PadRight(20), node.NodeName.PadRight(20), Enum.GetName(typeof(NodeType), node.NodeType).PadRight(20)), false);
                    }

                    if (getCustomTagsResult.Result.Count > 0 && nodesTotal > 0 && CLIEngine.GetConfirmation("Would you like to map any of these tags to your holon meta data?"))
                    {
                        bool mapTags = true;
                        Console.WriteLine("");

                        do
                        {
                            string tag = "";
                            string metaField = "";
                            INode selectedNode = null;

                            do
                            {
                                tag = CLIEngine.GetValidInput("Please enter the tag you wish to map (case sensitive). Enter 'exit' to cancel:");

                                if (tag == "exit")
                                    break;

                                if (string.IsNullOrEmpty(tag) || !getCustomTagsResult.Result.Contains(tag))
                                    CLIEngine.ShowErrorMessage($"The tag '{tag}' was not found. Please try again.");

                                else if (result.Result.Any(x => x.MetaTag == tag))
                                {
                                    MetaHolonTag matchedTag = result.Result.FirstOrDefault(x => x.MetaTag == metaField);

                                    if (matchedTag != null)
                                        CLIEngine.ShowErrorMessage($"The tag '{tag}' has already been mapped to '{matchedTag.NodeName}'. Please try again.");
                                    else
                                        CLIEngine.ShowErrorMessage($"The tag '{tag}' has already been mapped. Please try again.");

                                    tag = "";
                                }

                            } while (!getCustomTagsResult.Result.Contains(tag));

                            if (tag != "exit")
                            {
                                do
                                {
                                    metaField = CLIEngine.GetValidInput("Please enter the holon meta data node (field) you wish to map to this tag (case sensitive). Enter 'exit' to cancel:");

                                    if (metaField == "exit")
                                        break;

                                    selectedNode = GetNode(metaField, nodes);

                                    //if (string.IsNullOrEmpty(metaField) || !IsNodeFound(metaField, nodes))
                                    if (string.IsNullOrEmpty(metaField) || selectedNode == null)
                                        CLIEngine.ShowErrorMessage($"The holon meta data node '{metaField}' was not found. Please try again.");
                                    
                                    else if (result.Result.Any(x => x.MetaTag == metaField))
                                    {
                                        MetaHolonTag matchedTag = result.Result.FirstOrDefault(x => x.MetaTag == metaField);
                                        
                                        if (matchedTag != null)
                                            CLIEngine.ShowErrorMessage($"The holon meta data node '{metaField}' has already been mapped to '{matchedTag.NodeName}'. Please try again.");
                                        else
                                            CLIEngine.ShowErrorMessage($"The holon meta data node'{metaField}' has already been mapped. Please try again.");

                                        metaField = "";
                                    }
                                } while (!IsNodeFound(metaField, nodes));
                            }

                            if (tag != "exit" && metaField != "exit" && CLIEngine.GetConfirmation($"Please confirm you wish to map the tag '{tag}' to the holon meta data node '{metaField}'?"))
                            {
                                //result.Result[tag] = (GetHolonThatNodeBelongsTo(metaField, nodes), metaField);
                                //result.Result.Add(new MetaHolonTag() { HolonName = GetHolonThatNodeBelongsTo(metaField, nodes), NodeName = metaField, NodeType = new EnumValue<NodeType>(selectedNode.NodeType), MetaTag = tag });
                                result.Result.Add(new MetaHolonTag() { HolonName = GetHolonThatNodeBelongsTo(metaField, nodes), NodeName = metaField, NodeType = Enum.GetName(typeof(NodeType), selectedNode.NodeType), MetaTag = tag });
                                Console.WriteLine("");
                                CLIEngine.ShowSuccessMessage($"Meta tag '{tag}' mapped to holon meta data node '{metaField}'");
                                Console.WriteLine("");
                                ShowHolonMetaTagMappings(result.Result, true);
                            }
                            else
                                Console.WriteLine("");

                            if (tag == "exit" || metaField == "exit")
                                break;

                            mapTags = CLIEngine.GetConfirmation("Would you like to map more tags?");
                            Console.WriteLine("");

                        } while (mapTags);
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPP.MapCustomMetaTagsToTemplate. Reason: {e.Message}");
            }

            return result;
        }

        private OASISResult<Dictionary<string, string>> MapCustomMetaTagsToTemplate(IInstalledOAPPTemplate oappTemplate)
        {
            OASISResult<Dictionary<string, string>> result = new OASISResult<Dictionary<string, string>>(new Dictionary<string, string>());

            try
            {
                //OASISResult<List<string>> getCustomTagsResult = GetCustomTagsFromTemplate(oappTemplate.InstalledPath, new List<string>(), "{{{", "}}}");
                OASISResult<List<string>> getCustomTagsResult = GetCustomTagsFromTemplate(oappTemplate.InstalledPath, new List<string>(), "[[", "]]");

                if (getCustomTagsResult != null && getCustomTagsResult.Result != null && !getCustomTagsResult.IsError)
                {
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat($"Found {getCustomTagsResult.Result.Count} custom tag(s) in the OAPP Template '{oappTemplate.STARNETDNA.Name}'", getCustomTagsResult.Result.Count > 0 ? ":" : "."));

                    if (getCustomTagsResult.Result.Count > 0)
                    {
                        Console.WriteLine("");

                        foreach (string tag in getCustomTagsResult.Result)
                            CLIEngine.ShowMessage(tag, false);
                    }

                    if (getCustomTagsResult.Result.Count > 0 && CLIEngine.GetConfirmation("Would you like to map any of these tags to your meta data?"))
                    {
                        bool mapTags = true;
                        Console.WriteLine("");

                        do
                        {
                            string tag = "";
                            string metaField = "";

                            do
                            {
                                tag = CLIEngine.GetValidInput("Please enter the tag you wish to map (case sensitive). Enter 'exit' to cancel:");

                                if (tag == "exit")
                                    break;

                                if (string.IsNullOrEmpty(tag) || !getCustomTagsResult.Result.Contains(tag))
                                    CLIEngine.ShowErrorMessage($"The tag '{tag}' was not found. Please try again.");

                                else if (result.Result.Keys.Contains(tag))
                                {
                                    CLIEngine.ShowErrorMessage($"The tag '{tag}' has already been mapped to '{result.Result[tag]}'. Please try again.");
                                    tag = "";
                                }

                            } while (!getCustomTagsResult.Result.Contains(tag));

                            if (tag != "exit")
                            {
                                metaField = CLIEngine.GetValidInput("Please enter the meta data you wish to map to this tag. Enter 'exit' to cancel:");

                                if (tag != "exit" && metaField != "exit" && CLIEngine.GetConfirmation($"Please confirm you wish to map the tag '{tag}' to the meta data '{metaField}'?"))
                                {
                                    result.Result[tag] = metaField;
                                    Console.WriteLine("");
                                    CLIEngine.ShowSuccessMessage($"Meta tag '{tag}' mapped to meta data '{metaField}'");
                                    Console.WriteLine("");
                                    ShowMetaTagMappings(result.Result, true);
                                }
                                else
                                    Console.WriteLine("");

                                mapTags = CLIEngine.GetConfirmation("Would you like to map more tags?");
                                Console.WriteLine("");
                            }
                            else
                                break;
                        } while (mapTags);
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPP.MapCustomMetaTagsToTemplate. Reason: {e.Message}");
            }

            return result;
        }

        private bool IsNodeFound(string nodeName, Dictionary<string, IList<INode>> nodes)
        {
            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                {
                    if (nodes[key].Any(x => x.NodeName == nodeName))
                        return true;
                }
            }

            return false;
        }

        private INode GetNode(string nodeName, Dictionary<string, IList<INode>> nodes)
        {
            INode node = null;

            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                {
                    node = nodes[key].FirstOrDefault(x => x.NodeName == nodeName);

                    if (node != null)
                        break;
                }
            }

            return node;
        }

        private int CountNodes(Dictionary<string, IList<INode>> nodes)
        {
            int total = 0;

            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                    total += nodes[key].Count;
            }

            return total;
        }

        private string GetHolonThatNodeBelongsTo(string nodeName, Dictionary<string, IList<INode>> nodes)
        {
            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                {
                    if (nodes[key].Any(x => x.NodeName == nodeName))
                        return key;
                }
            }

            return "";
        }

        private bool IsMetaMatchFound(string nodeName, Dictionary<string, (string, string)> nodes)
        {
            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                {
                    if (nodes[key].Item2 == nodeName)
                        return true;
                }
            }

            return false;
        }

        //private void ShowMetaTagMappings(Dictionary<string, string> metaTagMappings)
        //{
        //    CLIEngine.ShowMessage(string.Concat("Tag".PadRight(22), "Meta Data".PadRight(22)), false);

        //    foreach (string key in metaTagMappings.Keys)
        //        CLIEngine.ShowMessage(string.Concat(key.PadRight(22), metaTagMappings[key].PadRight(22)), false);
        //}

        private OASISResult<List<string>> GetCustomTagsFromTemplate(string pathToTemplate, List<string> tags, string startTag, string endTag)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();

            try
            {
                foreach (DirectoryInfo dir in new DirectoryInfo(pathToTemplate).GetDirectories())
                {
                    if (dir.Name != "bin" && dir.Name != "obj")
                    {
                        OASISResult<List<string>> getTagsResult = GetCustomTagsFromTemplate(dir.FullName, tags, startTag, endTag);

                        if (getTagsResult != null && getTagsResult.Result != null && !getTagsResult.IsError)
                        {
                            if (getTagsResult.Result.Count > 0)
                                tags.AddRange(getTagsResult.Result);
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured in OAPP.GetCustomTagsFromTemplate. Reason: {getTagsResult.Message}");
                    }
                }

                foreach (FileInfo file in new DirectoryInfo(pathToTemplate).GetFiles("*.cs"))
                {
                    using (TextReader tr = File.OpenText(file.FullName))
                    {
                        string line;
                        while ((line = tr.ReadLine()) != null)
                        {
                            if (line.Contains(startTag))
                            {
                                int start = line.IndexOf(startTag);
                                start += 2;
                                int end = line.IndexOf(endTag, start);
                                string tag = line.Substring(start, end - start);

                                if (tag.Substring(0, 1) == "{")
                                    tag = tag.Substring(1);

                                if (!tags.Contains(tag))
                                    tags.Add(tag);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPP.GetCustomTagsFromTemplate. Reason: {e.Message}");
            }

            result.Result = tags;
            return result;
        }    
        
        private OASISResult<Dictionary<string, IList<INode>>> ValidateCelestialBodyDataDNA(string dnaFolder)
        {
            OASISResult<Dictionary<string, IList<INode>>> result = new OASISResult<Dictionary<string, IList<INode>>>();
            OASISResult<Dictionary<string, IList<INode>>> nodesResult = STAR.ExtractNodesFromCelestialBodyMetaDataDNA(dnaFolder);

            if (nodesResult != null && nodesResult.Result != null && !nodesResult.IsError)
            {
                if (nodesResult != null && nodesResult.Result != null && !nodesResult.IsError)
                {
                    result.Result = nodesResult.Result;

                    if (result.Result.Count == 0)
                        CLIEngine.ShowWarningMessage($"The CelestialBody MetaData DNA does not contain any valid data! Please try again!");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured extracting nodes from CelestialBody MetaData DNA. Reason: {nodesResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured extracting nodes from CelestialBody MetaData DNA. Reason: {nodesResult.Message}");

            return result;
        }
    }
}