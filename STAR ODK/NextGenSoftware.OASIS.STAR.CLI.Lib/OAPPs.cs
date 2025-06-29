using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using Nethereum.Contracts.Standards.ENS.ETHRegistrarController.ContractDefinition;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class OAPPs : STARNETUIBase<OAPP, DownloadedOAPP, InstalledOAPP>
    {
        public OAPPs(Guid avatarId) : base(new API.ONODE.Core.Managers.OAPPManager(avatarId),
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
        { }

        public override async Task<OASISResult<OAPP>> CreateAsync(object createParams, OAPP newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default)
        {
            //return base.CreateAsync(createParams, newHolon, providerType);
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
            object enumValue = null;
            OAPPType OAPPType = OAPPType.OAPPTemplate;
            OAPPTemplateType OAPPTemplateType = OAPPTemplateType.Console;
            IOAPPTemplate OAPPTemplate = null;
            ICelestialBody
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

            //CLIEngine.ShowDivider();
            //CLIEngine.ShowMessage("Welcome to the OASIS Omniverse/MagicVerse Light Wizard!");
            //CLIEngine.ShowDivider();
            //Console.WriteLine();
            //CLIEngine.ShowMessage("This wizard will allow you create an OAPP (Moon, Planet, Star & More) which will appear in the MagicVerse within the OASIS Omniverse.", false);
            //CLIEngine.ShowMessage("The OAPP will also optionally appear within the AR geo-location Our World/AR World platform/game in your desired geo-location.");
            //CLIEngine.ShowMessage("The OAPP will also optionally appear within the One World (Open World MMORPG) game/platform. VR support is also provided.");
            //CLIEngine.ShowMessage("The OAPP can have as many interfaces/lenses (way to visualize/represent the data of your OAPP) as you like, for example you can also have a 2D web view as well as a 3D view, Metaverse/Omniverse view, etc.");
            //CLIEngine.ShowMessage("Each OAPP is composed of zomes (re-usable/composable modules containing collections of holons) & holons (generic/composable re-usable OASIS Data Objects). This means the zomes and holons can be shared and re-used with other OAPPs within the STARNET Library. Different zomes and holons can be plugged together to form unique combinations for new OAPPs saving lots of time!");
            //CLIEngine.ShowMessage("Each OAPP is built/generated on top of a powerful easy to use ORM called (WEB5) COSMIC (The Worlds ORM because it aggregrates all of the worlds data into a simple to use ORM) which allows very easy data management across all of web2 and web3 making data interoperability and interchange very simple and makes silos a thing of the past!");
            //CLIEngine.ShowMessage("COSMIC is built on top of the powerful WEB4 OASIS API so each OAPP also has easy to use API's for manging keys, wallets, data, nfts, geo-nfts, providers, avatars, karma & much more!");
            //CLIEngine.ShowMessage("A OAPP can be anything you want such as a website, game, app, service, api, protocol or anything else that a template exists for!");
            //CLIEngine.ShowMessage("Data can be shared between OAPP's but you are always in full control of your data, you own your data and you can choose exactly who and how that data is shared. You have full data sovereignty.");
            //CLIEngine.ShowMessage("Due to your OAPP being built on the OASIS API you also benefit from many other advanced features such as auto-replication, auto-failover and auto-load balancing so if one node goes down in your local area it will automatically find the next fastest one in your area irrespective of network.");
            //CLIEngine.ShowMessage("The more users your OAPP has the larger that celestial body (moon, planet or star) will appear within The MagicVerse. The higher the karma score of the owner (can be a individual or company/organisation) of the OAPP becomes the closer that celestial bodies orbit will be to it's parent so if it's a moon it will get closer and closer to the planet and if it's a planet it will get closer and closer to it's star.");
            //CLIEngine.ShowDivider();

            ShowHeader();

            string OAPPName = CLIEngine.GetValidInput("What is the name of the OAPP?");

            if (OAPPName == "exit")
            {
                lightResult.Message = "User Exited";
                return lightResult;
            }

            string OAPPDesc = CLIEngine.GetValidInput("What is the description of the OAPP?");

            if (OAPPDesc == "exit")
            {
                lightResult.Message = "User Exited";
                return lightResult;
            }

            if (CLIEngine.GetConfirmation("Do you want to create the OAPP from an OAPP Template or do you want to generate the code only? Select 'Y' for OAPPTemplate or 'N' for Generated Code Only."))
            {
                Console.WriteLine("");
                enumValue = CLIEngine.GetValidInputForEnum("What type of OAPP Template do you wish to use?", typeof(OAPPTemplateType));

                if (enumValue != null)
                {
                    if (enumValue.ToString() == "exit")
                    {
                        lightResult.Message = "User Exited";
                        return lightResult;
                    }
                    else
                    {
                        OAPPTemplateType = (OAPPTemplateType)enumValue;
                        bool templateInstalled = false;

                        do
                        {
                            //if (CLIEngine.GetConfirmation("Do you know the GUID/ID of the OAPP Template?"))
                            //{
                            //    Console.WriteLine("");
                            //    OAPPTemplateId = CLIEngine.GetValidInputForGuid("What is the GUID/ID?");
                            //}
                            //else
                            //{
                            //    Console.WriteLine("");

                            //    if (CLIEngine.GetConfirmation("Do you know the name of the OAPP Template?"))
                            //    {
                            //        Console.WriteLine("");
                            //        string OAPPTemplateName = CLIEngine.GetValidInput("What is the name?");

                            //        if (OAPPTemplateName == "exit")
                            //        {
                            //            lightResult.Message = "User Exited";
                            //            return lightResult;
                            //        }

                            //        CLIEngine.ShowWorkingMessage("Searching STARNET...");
                            //        OAPPTemplateId = ProcessOAPPTemplateResults(await STAR.STARAPI.OAPPTemplates.SearchAsync(STAR.BeamedInAvatar.Id, OAPPTemplateName, false, false, 0, providerType), OAPPTemplateName);
                            //    }
                            //    else
                            //    {
                            //        Console.WriteLine("");
                            //        CLIEngine.ShowWorkingMessage("Searching STARNET...");
                            //        OAPPTemplateId = ProcessOAPPTemplateResults(await STAR.STARAPI.OAPPTemplates.LoadAllAsync(STAR.BeamedInAvatar.Id, OAPPTemplateType), string.Concat("type ", Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType)));
                            //    }
                            //}

                            //OASISResult<OAPPTemplate> templateResult = await FindAsync<OAPPTemplate>("use", STARNETHolonUIName: "OAPP Template", providerType: providerType);
                            OASISResult<OAPPTemplate> templateResult = await STARCLI.OAPPTemplates.FindAsync<OAPPTemplate>("use", providerType: providerType);

                            if (templateResult != null && templateResult.Result != null && !templateResult.IsError)
                            {
                                OASISResult<bool> oappTemplateInstalledResult = await STAR.STARAPI.OAPPTemplates.IsInstalledAsync(STAR.BeamedInAvatar.Id, templateResult.Result.STARNETDNA.Id, templateResult.Result.STARNETDNA.VersionSequence, providerType);

                                if (oappTemplateInstalledResult != null && !oappTemplateInstalledResult.IsError)
                                {
                                    if (!oappTemplateInstalledResult.Result)
                                    {
                                        if (CLIEngine.GetConfirmation($"The selected OAPP Template is not currently installed. Do you wish to install it now?"))
                                        {
                                            OASISResult<InstalledOAPPTemplate> installResult = await STARCLI.OAPPTemplates.DownloadAndInstallAsync(templateResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                                            if (installResult.Result != null && !installResult.IsError)
                                            {
                                                templateInstalled = true;
                                                OAPPTemplate = installResult.Result;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        templateInstalled = true;
                                        OAPPTemplate = templateResult.Result;
                                    }
                                }
                                else
                                    CLIEngine.ShowErrorMessage($"Error occured checking if OAPP Template is installed. Reason: {oappTemplateInstalledResult.Message}");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"Error occured finding OAPP Template. Reason: {templateResult.Message}");
                        }
                        while (!templateInstalled);

                        //do
                        //{
                        //    if (CLIEngine.GetConfirmation("Do you know the GUID/ID of the OAPP Template?"))
                        //    {
                        //        Console.WriteLine("");
                        //        OAPPTemplateId = CLIEngine.GetValidInputForGuid("What is the GUID/ID?");
                        //    }
                        //    else
                        //    {
                        //        Console.WriteLine("");

                        //        if (CLIEngine.GetConfirmation("Do you know the name of the OAPP Template?"))
                        //        {
                        //            Console.WriteLine("");
                        //            string OAPPTemplateName = CLIEngine.GetValidInput("What is the name?");

                        //            if (OAPPTemplateName == "exit")
                        //            {
                        //                lightResult.Message = "User Exited";
                        //                return lightResult;
                        //            }

                        //            CLIEngine.ShowWorkingMessage("Searching STARNET...");
                        //            OAPPTemplateId = ProcessOAPPTemplateResults(await STAR.STARAPI.OAPPTemplates.SearchAsync(STAR.BeamedInAvatar.Id, OAPPTemplateName, false, false, 0, providerType), OAPPTemplateName);
                        //        }
                        //        else
                        //        {
                        //            Console.WriteLine("");
                        //            CLIEngine.ShowWorkingMessage("Searching STARNET...");
                        //            OAPPTemplateId = ProcessOAPPTemplateResults(await STAR.STARAPI.OAPPTemplates.LoadAllAsync(STAR.BeamedInAvatar.Id, OAPPTemplateType), string.Concat("type ", Enum.GetName(typeof(OAPPTemplateType), OAPPTemplateType)));
                        //        }
                        //    }

                        //    if (OAPPTemplateId != Guid.Empty)
                        //    {
                        //        OASISResult<bool> oappTemplateInstalledResult = STAR.STARAPI.OAPPTemplates.IsInstalled(STAR.BeamedInAvatar.Id, OAPPTemplateId, 0, providerType);

                        //        if (oappTemplateInstalledResult != null && !oappTemplateInstalledResult.IsError)
                        //        {
                        //            if (!oappTemplateInstalledResult.Result)
                        //            {
                        //                if (CLIEngine.GetConfirmation($"The selected OAPP Template is not currently installed. Do you wish to install it now?"))
                        //                {
                        //                    //STAR.OASISAPI.OAPPTemplates.InstallOAPPTemplateAsync(STAR.BeamedInAvatar.Id, OAPPTemplateId, providerType);
                        //                    OASISResult<InstalledOAPPTemplate> installResult = await STARCLI.OAPPTemplates.DownloadAndInstallAsync(OAPPTemplateId.ToString(), InstallMode.DownloadAndInstall, providerType);

                        //                    if (installResult.Result != null && !installResult.IsError)
                        //                        templateInstalled = true;
                        //                }
                        //            }
                        //        }
                        //        else
                        //            CLIEngine.ShowErrorMessage($"Error occured checking if OAPP Template is installed. Reason: {oappTemplateInstalledResult.Message}");
                        //    }
                        //}
                        //while (!templateInstalled);
                    }
                }
            }
            else
                OAPPType = OAPPType.GeneratedCodeOnly;

            //TODO: I think star bang was going to be used to create non OAPP Celestial bodies or spaces outside of the magic verse.
            //if (CLIEngine.GetConfirmation("Do you wish the OAPP to be part of the MagicVerse within the OASIS Omniverse (will optionally appear in Our World/AR World)? If you say yes then new avatars will only be able to create moons that orbit Our World until you reach karma level 33 where you will then be able to create planets, when you reach level 77 you can create stars. If you select no then you can create whatever you like outside of the MagicVerse but it will still be within the OASIS Omniverse."))
            //{

            //}

            if (CLIEngine.GetConfirmation("Do you wish for your OAPP to appear in the AR geo-location Our World/AR World game/platform? (recommeneded)"))
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

            if (CLIEngine.GetConfirmation("Do you wish for your OAPP to appear in the Open World MMORPG One World game/platform? (recommeneded)"))
            {
                Console.WriteLine("");
                oneWorlddLat = CLIEngine.GetValidInputForLong("What is the lat geo-location you wish for your OAPP to appear in One World?");

                if (oneWorlddLat == -1)
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

            enumValue = CLIEngine.GetValidInputForEnum("What type of GenesisType do you wish to create? (New avatars will only be able to create moons that orbit Our World until you reach karma level 33 where you will then be able to create planets, when you reach level 77 you can create stars & beyond 77 you can create Galaxies and even entire Universes in your jounrey to become fully God realised!.)", typeof(GenesisType));

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
                bool validFolder = false;

                do
                {
                    if (CLIEngine.GetConfirmation("Do you wish to create the CelestialBody/Zomes/Holons DNA now? (Enter 'n' if you already have a folder containing the DNA)."))
                    {
                        do
                        {
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

                                    addMoreProps = CLIEngine.GetConfirmation("Do you wish to add more fields/properties to the Holon?");
                                
                                } while (addMoreProps);

                                zome.Children.Add(holon);
                                addMoreHolons = CLIEngine.GetConfirmation("Do you wish to add more Holon's to the Zome?");

                            } while (addMoreHolons);

                            zomes.Add(zome);
                            addMoreZomes = CLIEngine.GetConfirmation("Do you wish to add more Zome's to the Celestial Body/OAPP?");

                        } while (addMoreZomes);

                        //TODO: Now generate the CelestialBody/Zomes/Holons DNA and save it to a folder.
                        CLIEngine.ShowWorkingMessage("Generating CelestialBody/Zomes/Holons DNA...");

                    }
                    else
                    {
                        Console.WriteLine("");
                        dnaFolder = CLIEngine.GetValidFolder("What is the path to the CelestialBody/Zomes/Holons MetaData DNA?", false);
                    }

                    if (dnaFolder == "exit")
                    {
                        lightResult.Message = "User Exited";
                        return lightResult;
                    }

                    if (Directory.Exists(dnaFolder) && Directory.GetFiles(dnaFolder).Length > 0)
                        validFolder = true;
                    else
                        CLIEngine.ShowErrorMessage($"The DnaFolder {dnaFolder} is not valid, it does not contain any files! Please try again!");

                } while (!validFolder);

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

                //oappPath = Path.Combine(oappPath, OAPPTemplateName);

                //string genesisFolder = CLIEngine.GetValidFolder("What is the path to the GenesisFolder (where the OAPP will be generated)?");


                string genesisNamespace = OAPPName;

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
                        lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, OAPPTemplateType, OAPPTemplate.STARNETDNA.Id, OAPPTemplate.STARNETDNA.VersionSequence, genesisType, dnaFolder, oappPath, genesisNamespace, parentId, providerType);
                    }
                    else
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowErrorMessage($"You are only level {STAR.BeamedInAvatarDetail.Level}. You need to be at least level 33 to be able to change the parent celestialbody. Using the default of Our World.");
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage("Generating OAPP...");
                        lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, OAPPTemplateType, OAPPTemplate.STARNETDNA.Id, OAPPTemplate.STARNETDNA.VersionSequence, genesisType, dnaFolder, oappPath, genesisNamespace, providerType);
                    }
                }
                else
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage("Generating OAPP...");
                    lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, OAPPTemplateType, OAPPTemplate.STARNETDNA.Id, OAPPTemplate.STARNETDNA.VersionSequence, genesisType, dnaFolder, oappPath, genesisNamespace, providerType);
                }

                if (lightResult != null)
                {
                    if (!lightResult.IsError && lightResult.Result != null)
                    {
                        //Finally, save this to the STARNET App Store. This will be private on the store until the user publishes via the Star.Seed() command.
                        oappPath = Path.Combine(oappPath, OAPPName);

                        OASISResult<OAPP> createOAPPResult = await STAR.STARAPI.OAPPs.CreateAsync(STAR.BeamedInAvatar.Id, OAPPName, OAPPDesc, OAPPType, oappPath, null, null, new OAPPDNA()
                        {
                            CelestialBodyId = lightResult.Result.CelestialBody.Id,
                            CelestialBodyName = lightResult.Result.CelestialBody.Name,
                            GenesisType = genesisType,
                            OAPPTemplateId = OAPPTemplate.STARNETDNA.Id,
                            OAPPTemplateVersionSequence = OAPPTemplate.STARNETDNA.VersionSequence,
                            STARNETHolonType = OAPPType,
                            OurWorldLat = ourWorldLat,
                            OurWorldLong = ourWorldLong,
                            OurWorld3dObject = ourWorld3dObject,
                            OurWorld3dObjectURI = ourWorld3dObjectURI,
                            OurWorld2dSprite = ourWorld2dSprite,
                            OurWorld2dSpriteURI = ourWorld2dSpriteURI,
                            OneWorldLat = oneWorlddLat,
                            OneWorldLong = oneWorldLong,
                            OneWorld3dObject = oneWorld3dObject,
                            OneWorld3dObjectURI = oneWorld3dObjectURI,
                            OneWorld2dSprite = oneWorld2dSprite,
                            OneWorld2dSpriteURI = oneWorld2dSpriteURI,
                            Zomes = lightResult.Result.CelestialBody.CelestialBodyCore.Zomes
                        }, false, providerType);

                        if (createOAPPResult != null && createOAPPResult.Result != null && !createOAPPResult.IsError)
                        {
                            lightResult.Result.OAPP = createOAPPResult.Result;

                            CLIEngine.ShowSuccessMessage($"OAPP Successfully Generated. ({lightResult.Message})");
                            ShowOAPP((IOAPPDNA)lightResult.Result.OAPP.STARNETDNA, lightResult.Result.CelestialBody.CelestialBodyCore.Zomes);
                            Console.WriteLine("");

                            if (CLIEngine.GetConfirmation("Do you wish to open the OAPP now?"))
                                Process.Start("explorer.exe", Path.Combine(oappPath, string.Concat(OAPPName, " OAPP"), string.Concat(genesisNamespace, ".csproj")));

                            Console.WriteLine("");

                            if (CLIEngine.GetConfirmation("Do you wish to open the OAPP folder now?"))
                                Process.Start("explorer.exe", Path.Combine(oappPath, string.Concat(OAPPName, " OAPP")));

                            Console.WriteLine("");
                        }
                        else
                            CLIEngine.ShowErrorMessage($"Error Occured Creating The OAPP. Reason: {createOAPPResult.Message}");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error Occured: {lightResult.Message}");
                }
            }

            return lightResult;
        }

        public override Task PublishAsync(string sourcePath = "", bool edit = false, ProviderType providerType = ProviderType.Default)
        {
            return PublishAsync(sourcePath, edit, false, providerType);
        }

        public async Task<OASISResult<IOAPP>> PublishAsync(string sourcePath = "", bool edit = false, bool dotNetPublish = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
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
            //string launchTarget = "";
            string publishPath = "";
            bool registerOnSTARNET = false;
            string STARNETInfo = "If you select 'Y' to this question then your OAPP will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .oapp install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.";
            //string launchTargetQuestion = "";
            // bool publishDotNot = false;

            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage("Welcome to the OAPP Publish Wizard!");
            CLIEngine.ShowDivider();
            Console.WriteLine();
            CLIEngine.ShowMessage("This wizard will publish your OAPP to STARNET. There are 4 ways of doing this:");
            CLIEngine.ShowMessage("1. Publish the standard OAPP (.oapp) file with no runtimes bundled with it. (Default & recommended). The target machine will need to have the .NET, OASIS & STAR runtimes installed.");
            CLIEngine.ShowMessage("2. Publish the standard OAPP (.oappselfcontained) file bundled with the OASIS & STAR runtimes (approx 250MB). The target machine will need to have the .NET runtime installed.");
            CLIEngine.ShowMessage("3. Publish the standard OAPP (.oappselfcontainedfull) file bundled with the OASIS, STAR runtimes & .NET runtimes (approx 500MB). No dependencies needed, fully self-contained.");
            CLIEngine.ShowMessage("4. Publish the OAPP source (.oappsource) file which only contains the source. People can then download the source and build the OAPP on their machine (if they are missing any of the dependencies such as the runtimes there will be automatically restored using nuget). NOTE: This means your source would NEED to be made public (not a problem for Open Source etc).");
            CLIEngine.ShowMessage("Each approach has pros and cons with 4 being the smallest and then 2,3 and 4. Smaller means quicker upload and download and less storage space required (lower hosting costs) but also comes with the risk there may be problems building (4 only) or running the OAPP on the target machine if they are missing any of the dependencies such ast he runtimes etc.");

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

            OASISResult<BeginPublishResult> beginPublishResult = await BeginPublishingAsync(sourcePath, providerType);

            if (beginPublishResult != null && !beginPublishResult.IsError && beginPublishResult.Result != null)
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate the standard .oapp file? (Recommended). This file contains only the built & published OAPP source code. NOTE: You will need to make sure the target machine that runs this OAPP has both the appropriate OASIS & STAR ODK Runtimes installed along with the appropriate .NET Runtime."))
                {
                    generateOAPP = true;

                    if (CLIEngine.GetConfirmation($"Do you wish to upload/publish the .oapp file to STARNET? {STARNETInfo}"))
                    {
                        if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                            uploadOAPPToCloud = true;

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

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained .oapp file? This file contains the built & published OAPP source code along with the OASIS & STAR ODK Runtimes. NOTE: You will need to make sure the target machine that runs this OAPP has the appropriate .NET runtime installed. The file will be a minimum of 250 MB."))
                {
                    generateOAPPSelfContained = true;

                    if (CLIEngine.GetConfirmation($"Do you wish to upload/publish the self-contained .oapp file to STARNET?"))
                    {
                        if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                            uploadOAPPSelfContainedToCloud = true;

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

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained (full) .oapp file? This file contains the built & published OAPP source code along with the OASIS, STAR ODK & .NET Runtimes. NOTE: The file will be a minimum of 500 MB."))
                {
                    generateOAPPSelfContained = true;

                    if (CLIEngine.GetConfirmation($"Do you wish to upload/publish the self-contained (full) .oapp file to STARNET?"))
                    {
                        if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                            uploadOAPPToCloud = true;

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

                if (!uploadOAPPToCloud && OAPPBinaryProviderType == ProviderType.None &&
                    !uploadOAPPSelfContainedToCloud && OAPPSelfContainedBinaryProviderType == ProviderType.None &&
                    !uploadOAPPSelfContainedFullToCloud && OAPPSelfContainedFullBinaryProviderType == ProviderType.None)
                    CLIEngine.ShowMessage("Since you did not select to upload to the cloud or OASIS storage the oapp will not be published to STARNET.");
                else
                    registerOnSTARNET = true;

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
                await PreFininaliazePublishingAsync(beginPublishResult.Result.SimpleWizard, sourcePath, publishPath, beginPublishResult.Result.LaunchTarget, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
                result = await STAR.STARAPI.OAPPs.PublishOAPPAsync(STAR.BeamedInAvatar.Id, sourcePath, beginPublishResult.Result.LaunchTarget, publishPath, edit, registerOnSTARNET, dotNetPublish, generateOAPPSource, uploadOAPPSource, makeOAPPSourcePublic, generateOAPP, generateOAPPSelfContained, generateOAPPSelfContainedFull, uploadOAPPToCloud, uploadOAPPSelfContainedToCloud, uploadOAPPSelfContainedFullToCloud, providerType, OAPPBinaryProviderType, OAPPSelfContainedBinaryProviderType, OAPPSelfContainedFullBinaryProviderType);
                OASISResult<OAPP> publishResult = new OASISResult<OAPP>((OAPP)result.Result);
                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(result, publishResult);
                await PostFininaliazePublishingAsync(publishResult, sourcePath, providerType);
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured: {beginPublishResult.Message}");

            return result;
        }

        //public override void Show<T>(T starHolon, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false)
        //{
        //    base.Show(starHolon, showHeader, showFooter, showNumbers, number, showDetailedInfo);
        //}

        public override void Show<OAPP>(OAPP oapp, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false)
        {
            IOAPPDNA OAPPDNA = (IOAPPDNA)oapp.STARNETDNA;

            CLIEngine.ShowMessage(string.Concat($"Id:                                                   ", OAPPDNA.Id != Guid.Empty ? OAPPDNA.Id : "None"));
            CLIEngine.ShowMessage(string.Concat($"Name:                                                 ", !string.IsNullOrEmpty(OAPPDNA.Name) ? OAPPDNA.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description:                                          ", !string.IsNullOrEmpty(OAPPDNA.Description) ? OAPPDNA.Description : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Type:                                            ", Enum.GetName(typeof(OAPPType), OAPPDNA.OAPPType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Type:                                   ", Enum.GetName(typeof(OAPPTemplateType), OAPPDNA.OAPPTemplateType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Id:                                     ", OAPPDNA.OAPPTemplateId));
            CLIEngine.ShowMessage(string.Concat($"Genesis Type:                                         ", Enum.GetName(typeof(GenesisType), OAPPDNA.GenesisType)));
            CLIEngine.ShowMessage(string.Concat($"Celestial Body Id:                                    ", OAPPDNA.CelestialBodyId != Guid.Empty ? OAPPDNA.CelestialBodyId : "None"));
            CLIEngine.ShowMessage(string.Concat($"Celestial Body Name:                                  ", !string.IsNullOrEmpty(OAPPDNA.CelestialBodyName) ? OAPPDNA.CelestialBodyName : "None"));
            CLIEngine.ShowMessage(string.Concat($"Celestial Body Type:                                  ", Enum.GetName(typeof(HolonType), OAPPDNA.CelestialBodyType)));
            CLIEngine.ShowMessage(string.Concat($"Created On:                                           ", OAPPDNA.CreatedOn != DateTime.MinValue ? OAPPDNA.CreatedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created By:                                           ", OAPPDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(OAPPDNA.CreatedByAvatarUsername, " (", OAPPDNA.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published On:                                         ", OAPPDNA.PublishedOn != DateTime.MinValue ? OAPPDNA.PublishedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published By:                                         ", OAPPDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(OAPPDNA.PublishedByAvatarUsername, " (", OAPPDNA.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published Path:                                  ", !string.IsNullOrEmpty(OAPPDNA.PublishedPath) ? OAPPDNA.PublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Filesize:                                        ", OAPPDNA.FileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published Path:                   ", !string.IsNullOrEmpty(OAPPDNA.SelfContainedPublishedPath) ? OAPPDNA.PublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Filesize:                         ", OAPPDNA.SelfContainedFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published Path:              ", !string.IsNullOrEmpty(OAPPDNA.SelfContainedFullPublishedPath) ? OAPPDNA.PublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Filesize:                    ", OAPPDNA.SelfContainedFullFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published On STARNET:                            ", OAPPDNA.PublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published To Cloud:                              ", OAPPDNA.PublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published To OASIS Provider:                     ", Enum.GetName(typeof(ProviderType), OAPPDNA.PublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To Cloud:               ", OAPPDNA.SelfContainedPublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To OASIS Provider:      ", Enum.GetName(typeof(ProviderType), OAPPDNA.SelfContainedPublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To Cloud:          ", OAPPDNA.SelfContainedFullPublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), OAPPDNA.SelfContainedFullPublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Published Path:                           ", !string.IsNullOrEmpty(OAPPDNA.SourcePublishedPath) ? OAPPDNA.SourcePublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Filesize:                                 ", OAPPDNA.SourceFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Published On STARNET:                     ", OAPPDNA.SourcePublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Public On STARNET:                        ", OAPPDNA.SourcePublicOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"Launch Target:                                        ", !string.IsNullOrEmpty(OAPPDNA.LaunchTarget) ? OAPPDNA.LaunchTarget : "None"));
            CLIEngine.ShowMessage(string.Concat($"Version:                                              ", OAPPDNA.Version));
            CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:                                     ", OAPPDNA.STARODKVersion));
            CLIEngine.ShowMessage(string.Concat($"OASIS Version:                                        ", OAPPDNA.OASISVersion));
            CLIEngine.ShowMessage(string.Concat($"COSMIC Version:                                       ", OAPPDNA.COSMICVersion));
            CLIEngine.ShowMessage(string.Concat($".NET Version:                                         ", OAPPDNA.DotNetVersion));


            //TODO: Fix later!
            //if (zomes != null && zomes.Count > 0)
            //{
            //    Console.WriteLine("");
            //    STARCLI.Zomes.ShowZomesAndHolons(zomes);
            //}

            //TODO: Come back to this.
            //if (oapp.CelestialBody != null && oapp.CelestialBody.CelestialBodyCore != null && oapp.CelestialBody.CelestialBodyCore.Zomes != null)
            //    ShowZomesAndHolons(oapp.CelestialBody.CelestialBodyCore.Zomes);

            //else if (oapp.Zomes != null)
            //    ShowZomesAndHolons(oapp.Zomes);

            CLIEngine.ShowDivider();
        }

        public void ShowOAPP(IOAPPDNA oapp, List<IZome> zomes = null)
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

            CLIEngine.ShowMessage(string.Concat($"Id:                                                   ", oapp.Id != Guid.Empty ? oapp.Id : "None"));
            CLIEngine.ShowMessage(string.Concat($"Name:                                                 ", !string.IsNullOrEmpty(oapp.Name) ? oapp.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description:                                          ", !string.IsNullOrEmpty(oapp.Description) ? oapp.Description : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Type:                                            ", Enum.GetName(typeof(OAPPType), oapp.OAPPType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Type:                                   ", Enum.GetName(typeof(OAPPTemplateType), oapp.OAPPTemplateType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Id:                                     ", oapp.OAPPTemplateId));
            CLIEngine.ShowMessage(string.Concat($"Genesis Type:                                         ", Enum.GetName(typeof(GenesisType), oapp.GenesisType)));
            CLIEngine.ShowMessage(string.Concat($"Celestial Body Id:                                    ", oapp.CelestialBodyId != Guid.Empty ? oapp.CelestialBodyId : "None"));
            CLIEngine.ShowMessage(string.Concat($"Celestial Body Name:                                  ", !string.IsNullOrEmpty(oapp.CelestialBodyName) ? oapp.CelestialBodyName : "None"));
            CLIEngine.ShowMessage(string.Concat($"Celestial Body Type:                                  ", Enum.GetName(typeof(HolonType), oapp.CelestialBodyType)));
            CLIEngine.ShowMessage(string.Concat($"Created On:                                           ", oapp.CreatedOn != DateTime.MinValue ? oapp.CreatedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created By:                                           ", oapp.CreatedByAvatarId != Guid.Empty ? string.Concat(oapp.CreatedByAvatarUsername, " (", oapp.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published On:                                         ", oapp.PublishedOn != DateTime.MinValue ? oapp.PublishedOn.ToString() : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published By:                                         ", oapp.PublishedByAvatarId != Guid.Empty ? string.Concat(oapp.PublishedByAvatarUsername, " (", oapp.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published Path:                                  ", !string.IsNullOrEmpty(oapp.PublishedPath) ? oapp.PublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Filesize:                                        ", oapp.FileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published Path:                   ", !string.IsNullOrEmpty(oapp.SelfContainedPublishedPath) ? oapp.PublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Filesize:                         ", oapp.SelfContainedFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published Path:              ", !string.IsNullOrEmpty(oapp.SelfContainedFullPublishedPath) ? oapp.PublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Filesize:                    ", oapp.SelfContainedFullFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published On STARNET:                            ", oapp.PublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published To Cloud:                              ", oapp.PublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Published To OASIS Provider:                     ", Enum.GetName(typeof(ProviderType), oapp.PublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To Cloud:               ", oapp.SelfContainedPublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To OASIS Provider:      ", Enum.GetName(typeof(ProviderType), oapp.SelfContainedPublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To Cloud:          ", oapp.SelfContainedFullPublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oapp.SelfContainedFullPublishedProviderType)));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Published Path:                           ", !string.IsNullOrEmpty(oapp.SourcePublishedPath) ? oapp.SourcePublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Filesize:                                 ", oapp.SourceFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Published On STARNET:                     ", oapp.SourcePublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Source Public On STARNET:                        ", oapp.SourcePublicOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"Launch Target:                                        ", !string.IsNullOrEmpty(oapp.LaunchTarget) ? oapp.LaunchTarget : "None"));
            CLIEngine.ShowMessage(string.Concat($"Version:                                              ", oapp.Version));
            CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:                                     ", oapp.STARODKVersion));
            CLIEngine.ShowMessage(string.Concat($"OASIS Version:                                        ", oapp.OASISVersion));
            CLIEngine.ShowMessage(string.Concat($"COSMIC Version:                                       ", oapp.COSMICVersion));
            CLIEngine.ShowMessage(string.Concat($".NET Version:                                         ", oapp.DotNetVersion));


            if (zomes != null && zomes.Count > 0)
            {
                Console.WriteLine("");
                STARCLI.Zomes.ShowZomesAndHolons(zomes);
            }

            //TODO: Come back to this.
            //if (oapp.CelestialBody != null && oapp.CelestialBody.CelestialBodyCore != null && oapp.CelestialBody.CelestialBodyCore.Zomes != null)
            //    ShowZomesAndHolons(oapp.CelestialBody.CelestialBodyCore.Zomes);

            //else if (oapp.Zomes != null)
            //    ShowZomesAndHolons(oapp.Zomes);

            CLIEngine.ShowDivider();
        }

        //private static IOAPPTemplate ProcessOAPPTemplateResults(OASISResult<IEnumerable<OAPPTemplate>> oappTemplateResults, string searchTerm)
        //{
        //    IOAPPTemplate OAPPTemplate = null;

        //    if (oappTemplateResults != null && oappTemplateResults.Result != null && !oappTemplateResults.IsError)
        //    {
        //        if (oappTemplateResults.Result.Count() > 1)
        //        {
        //            CLIEngine.ShowMessage($"The following OAPP Template's were found for '{searchTerm}':");

        //            foreach (OAPPTemplate oappTemplate in oappTemplateResults.Result)
        //                STARCLI.OAPPTemplates.Show(oappTemplate);

        //            if (CLIEngine.GetConfirmation("Do you wish to use any of these OAPP Templates?"))
        //                OAPPTemplate = CLIEngine.GetValidInputForGuid($"Which OAPP Template do you wish to use? Please enter the VersionSequence of the OAPP Template you wish to use.");
        //        }
        //        else if (oappTemplateResults.Result.Count() == 1)
        //        {
        //            CLIEngine.ShowMessage($"The following OAPP Template was found for '{searchTerm}':");
        //            STARCLI.OAPPTemplates.Show(oappTemplateResults.Result.FirstOrDefault());

        //            if (CLIEngine.GetConfirmation("Do you wish to use this OAPP Template?"))
        //                OAPPTemplate = oappTemplateResults.Result.FirstOrDefault();
        //        }
        //        else
        //            CLIEngine.ShowMessage($"No results were found for '{searchTerm}'.");
        //    }
        //    else
        //        CLIEngine.ShowErrorMessage($"Error occured searching for OAPP Templates: Reason: {oappTemplateResults.Message}");

        //    return OAPPTemplate;
        //}
    }
}