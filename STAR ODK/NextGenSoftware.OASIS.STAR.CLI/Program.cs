using System;
using System.Linq;
using System.Drawing;
using MongoDB.Driver;
using System.Diagnostics;
using System.Threading.Tasks;
using Console = System.Console;
using System.Collections.Generic;
using System.Security.Cryptography;
using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Enums;
using NextGenSoftware.OASIS.STAR.CLI.Lib;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.IO;

namespace NextGenSoftware.OASIS.STAR.CLI
{ //test
    class Program
    {
        //All params defined in STARDNA now.
        private static string DEFAULT_DNA_FOLDER;
        private static string DEFAULT_GENESIS_FOLDER;
        //private static string DEFAULT_GENESIS_NAMESPACE = STAR.STARDNA.GenesisNamespace;
        private const OAPPType DEFAULT_OAPP_TYPE = OAPPType.OAPPTemplate;
        private const OAPPTemplateType DEFAULT_OAPP_TEMPLATE_TYPE = OAPPTemplateType.Console;

        //private static string _privateKey = ""; //Set to privatekey when testing BUT remember to remove again before checking in code! Better to use avatar methods so private key is retreived from avatar and then no need to pass them in.
        private static string[] _args = null;
        private static bool _exiting = false;
        private static bool _inMainMenu = false;

        static async Task Main(string[] args)
        {
            try
            {
                //ConsoleHelper.SetCurrentFont("Consolas", 8);
                _args = args;
                ShowHeader();
                CLIEngine.ShowMessage("", false);
                Console.CancelKeyPress += Console_CancelKeyPress;

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
                STAR.OnDefaultCeletialBodyInit += STAR_OnDefaultCeletialBodyInit;

                //STAR.IsDetailedCOSMICOutputsEnabled = CLIEngine.GetConfirmation("Do you wish to enable detailed COSMIC outputs?");
                //Console.WriteLine("");
                //CLIEngine.ShowMessage("");

                //STAR.IsDetailedStatusUpdatesEnabled = CLIEngine.GetConfirmation("Do you wish to enable detailed STAR ODK Status outputs?");
                //Console.WriteLine("");
                
               // CLIEngine.ShowMessage("Uploading...");
               // Console.WriteLine("");
               // //CLIEngine.ShowProgressBar(0);
               //// Console.WriteLine("");
               // //CLIEngine.ShowWorkingMessage("Uploading... 0%");
               // //CLIEngine.ShowWorkingMessage("Uploading...");

               // for (int i =0; i<100; i++)
               // {
               //     //CLIEngine.UpdateWorkingMessageWithPercent(i);
               //    // CLIEngine.UpdateWorkingMessage($"Uploading... {i}%");
               //     //CLIEngine.ShowProgressBar(i, true);
               //     CLIEngine.ShowProgressBar((double)i/(double)100);
               //     Thread.Sleep(1000);
               // }
                
                //await ReadyPlayerOne(); //TODO: TEMP!  Remove after testing!

                OASISResult<IOmiverse> result = STAR.IgniteStar();

                if (result.IsError)
                    CLIEngine.ShowErrorMessage(string.Concat("Error Igniting STAR. Error Message: ", result.Message));
                else
                {
                    DEFAULT_DNA_FOLDER = STAR.STARDNA.OAPPMetaDataDNAFolder;
                    DEFAULT_GENESIS_FOLDER = STAR.STARDNA.DefaultOAPPsSourcePath;

                    await STARCLI.Avatars.BeamInAvatar();
                    await ReadyPlayerOne(); //TODO: May allow this to be called with a different provider in future.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                CLIEngine.ShowErrorMessage(string.Concat("An unknown error has occurred. Error Details: ", ex.ToString()));
                //AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //e.Cancel = !CLIEngine.GetConfirmation("STAR: Are you sure you wish to exit?");
            //_exiting = !e.Cancel;

             e.Cancel = true;

            //if (_inMainMenu)
            //    e.Cancel = !CLIEngine.GetConfirmation("STAR: Are you sure you wish to exit?");
            //else
            //    e.Cancel = true;

            ////Console.WriteLine("\nThe read operation has been interrupted.");
            ////Console.WriteLine($"  Key pressed: {e.SpecialKey}");
            ////Console.WriteLine($"  Cancel property: {e.Cancel}");

            //if (e.Cancel)
            //    ReadyPlayerOne();
        }

        private static void STAR_OnDefaultCeletialBodyInit(object sender, EventArgs.DefaultCelestialBodyInitEventArgs e)
        {
            if (STAR.IsDetailedCOSMICOutputsEnabled)
            {
                IHolon holon = Mapper<ICelestialBody, Holon>.MapBaseHolonProperties(e.Result.Result);
                STARCLI.Holons.ShowHolonProperties(holon);
            }
            //ShowHolonProperties((IHolon)e.Result);
        }

        private static async Task ReadyPlayerOne(ProviderType providerType = ProviderType.Default)
        {
            //ShowAvatarStats(); //TODO: Temp, put back in after testing! ;-)

            CLIEngine.ShowMessage("", false);
            CLIEngine.WriteAsciMessage(" READY PLAYER ONE?", Color.Green);
            //CLIEngine.ShowMessage("", false);

            //TODO: TEMP - REMOVE AFTER TESTING! :)
            //await Test(celestialBodyDNAFolder, geneisFolder);

            bool exit = false;
            do
            {
                try
                {

                    if (_exiting)
                        exit = true;

                    _inMainMenu = true;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("");
                    CLIEngine.ShowMessage("STAR: ", false, true);
                    string input = Console.ReadLine();

                    if (!string.IsNullOrEmpty(input))
                    {
                        string[] inputArgs = input.Split(" ");

                        if (inputArgs.Length > 0)
                        {
                            switch (inputArgs[0].ToLower())
                            {
                                case "ignite":
                                    {
                                        if (!STAR.IsStarIgnited)
                                            await STAR.IgniteStarAsync();
                                        else
                                            CLIEngine.ShowErrorMessage("STAR Is Already Ignited!");
                                    }
                                    break;

                                case "extinguish":
                                    {
                                        if (STAR.IsStarIgnited)
                                            await STAR.ExtinguishStarAsync();
                                        else
                                            CLIEngine.ShowErrorMessage("STAR Is Not Ignited!");
                                    }
                                    break;

                                case "help":
                                    {
                                        if (inputArgs.Length > 1 && inputArgs[1].ToLower() == "full")
                                            ShowCommands(true);
                                        else
                                            ShowCommands(false);
                                    }
                                    break;

                                case "version":
                                    {
                                        Console.WriteLine("");
                                        CLIEngine.ShowMessage($"OASIS RUNTIME VERSION:   v{OASISBootLoader.OASISBootLoader.OASISRuntimeVersion}.", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"OASIS API VERSION:       v{OASISBootLoader.OASISBootLoader.OASISAPIVersion}.", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"COSMIC ORM VERSION:      v{OASISBootLoader.OASISBootLoader.COSMICVersion}.", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"STAR RUNTIME VERSION:    v{OASISBootLoader.OASISBootLoader.STARRuntimeVersion}.", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"STAR ODK VERSION:        v{OASISBootLoader.OASISBootLoader.STARODKVersion}.", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"STARNET VERSION:         v{OASISBootLoader.OASISBootLoader.STARNETVersion}.", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"STAR API VERSION:        v{OASISBootLoader.OASISBootLoader.STARAPIVersion}.", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($".NET VERSION:            v{OASISBootLoader.OASISBootLoader.DotNetVersion}.", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"OASIS PROVIDER VERSIONS: Coming Soon...", ConsoleColor.Green, false); //TODO Implement ASAP.
                                    }
                                    break;

                                case "status":
                                    {
                                        Console.WriteLine("");
                                        CLIEngine.ShowMessage($"STAR ODK Status: {Enum.GetName(typeof(StarStatus), STAR.Status)}", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"COSMIC ORM Status: Online", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"OASIS Runtime Status: Online", ConsoleColor.Green, false);
                                        CLIEngine.ShowMessage($"OASIS Provider Status: Coming Soon...", ConsoleColor.Green, false); //TODO Implement ASAP.
                                    }
                                    break;

                                case "exit":
                                    exit = CLIEngine.GetConfirmation("STAR: Are you sure you wish to exit?");
                                    break;

                                case "light":
                                    {
                                        object oappTypeObj = null;
                                        object genesisTypeObj = null;
                                        OAPPTemplateType oappTemplateType = DEFAULT_OAPP_TEMPLATE_TYPE;
                                        OAPPType oappType = DEFAULT_OAPP_TYPE;
                                        Guid oappTemplateId = Guid.Empty;
                                        int oappTemplateVersion = 1;
                                        GenesisType genesisType = GenesisType.Planet;
                                        OASISResult<CoronalEjection> lightResult = null;
                                        _inMainMenu = false;

                                        //TODO: Need to re-write this so it uses named params that are parsed rather than relying on them being in the correct order!
                                        //Also this will then allow OAPPTemplate to be optional (3 params are optional).
                                        if (inputArgs.Length > 1)
                                        {
                                            if (inputArgs[1].ToLower() == "wiz")
                                                await STARCLI.OAPPs.LightWizardAsync(null);
                                            else
                                            {
                                                CLIEngine.ShowWorkingMessage("Generating OAPP...");

                                                if (inputArgs.Length > 2 && Enum.TryParse(typeof(OAPPType), inputArgs[3], true, out oappTypeObj))
                                                {
                                                    oappType = (OAPPType)oappTypeObj;

                                                    //if (inputArgs.Length > 3 && Enum.TryParse(typeof(OAPPTemplateType), inputArgs[4], true, out oappTypeObj))
                                                    //{
                                                    //    oappTemplateType = (OAPPTemplateType)oappTypeObj;

                                                        if (inputArgs.Length > 3 && Guid.TryParse(inputArgs[4], out oappTemplateId))
                                                        {
                                                            oappTemplateId = oappTemplateId;

                                                            if (inputArgs.Length > 4 && int.TryParse(inputArgs[5], out oappTemplateVersion))
                                                            {
                                                                oappTemplateVersion = oappTemplateVersion;

                                                                if (inputArgs.Length > 8)
                                                                {
                                                                    if (Enum.TryParse(typeof(GenesisType), inputArgs[9], true, out genesisTypeObj))
                                                                    {
                                                                        genesisType = (GenesisType)genesisTypeObj;

                                                                        if (inputArgs.Length > 9)
                                                                        {
                                                                            Guid parentId = Guid.Empty;

                                                                            if (Guid.TryParse(inputArgs[10], out parentId))
                                                                                lightResult = await STAR.LightAsync(inputArgs[1], inputArgs[2], oappType, oappTemplateId, oappTemplateVersion, genesisType, inputArgs[6], inputArgs[7], inputArgs[8], null, null, parentId);
                                                                            else
                                                                                CLIEngine.ShowErrorMessage($"The ParentCelestialBodyId Passed In ({inputArgs[6]}) Is Not Valid. Please Make Sure It Is One Of The Following: {EnumHelper.GetEnumValues(typeof(GenesisType), EnumHelperListType.ItemsSeperatedByComma)}.");
                                                                        }
                                                                        else
                                                                            lightResult = await STAR.LightAsync(inputArgs[1], inputArgs[2], oappType, oappTemplateId, oappTemplateVersion, genesisType, inputArgs[6], inputArgs[7], inputArgs[8], null, null, ProviderType.Default);
                                                                    }
                                                                    else
                                                                        CLIEngine.ShowErrorMessage($"The GenesisType Passed In ({inputArgs[7]}) Is Not Valid. Please Make Sure It Is One Of The Following: {EnumHelper.GetEnumValues(typeof(GenesisType), EnumHelperListType.ItemsSeperatedByComma)}.");
                                                                }
                                                                else
                                                                    lightResult = await STAR.LightAsync(inputArgs[1], inputArgs[2], oappType, oappTemplateId, oappTemplateVersion, inputArgs[6], inputArgs[7], inputArgs[8]);
                                                            }
                                                            else
                                                                CLIEngine.ShowErrorMessage($"The OAPPTemplateVersion Passed In ({inputArgs[6]}) Is Not Valid. .");
                                                        }
                                                        else
                                                            CLIEngine.ShowErrorMessage($"The OAPPTemplateId Passed In ({inputArgs[5]}) Is Not Valid. .");
                                                    //}
                                                    //else
                                                    //    CLIEngine.ShowErrorMessage($"The OAPPTemplateType Passed In ({inputArgs[4]}) Is Not Valid. Please Make Sure It Is One Of The Following: {EnumHelper.GetEnumValues(typeof(OAPPType), EnumHelperListType.ItemsSeperatedByComma)}.");
                                                }
                                                else
                                                    CLIEngine.ShowErrorMessage($"The OAPPType Passed In ({inputArgs[3]}) Is Not Valid. Please Make Sure It Is One Of The Following: {EnumHelper.GetEnumValues(typeof(OAPPType), EnumHelperListType.ItemsSeperatedByComma)}.");

                                                if (lightResult != null)
                                                {
                                                    if (!lightResult.IsError && lightResult.Result != null)
                                                        CLIEngine.ShowSuccessMessage($"OAPP Successfully Generated. ({lightResult.Message})");
                                                    else
                                                        CLIEngine.ShowErrorMessage($"Error Occurred: {lightResult.Message}");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("");
                                            CLIEngine.ShowMessage("LIGHT SUBCOMMAND:", ConsoleColor.Green);
                                            Console.WriteLine("");
                                            CLIEngine.ShowMessage("OAPPName               The name of the OAPP.", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage($"OAPPType               The type of the OAPP, which can be any of the following: {EnumHelper.GetEnumValues(typeof(OAPPType), EnumHelperListType.ItemsSeperatedByComma)}.", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage("DnaFolder              The path to the DNA Folder which will be used to generate the OAPP from.", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage("GenesisFolder          The path to the Genesis Folder where the OAPP will be created.", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage("GenesisNameSpace       The namespace of the OAPP to generate.", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage($"GenesisType            The Genesis Type can be any of the following: {EnumHelper.GetEnumValues(typeof(GenesisType), EnumHelperListType.ItemsSeperatedByComma)}.", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage("ParentCelestialBodyId  The ID (GUID) of the Parent CelestialBody the generated OAPP will belong to. (optional)", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage("NOTE: Use 'light wiz' to start the light wizard.", ConsoleColor.Green);

                                            if (CLIEngine.GetConfirmation("Do you wish to start the wizard?"))
                                            {
                                                Console.WriteLine("");
                                                await STARCLI.OAPPs.LightWizardAsync(null);
                                            }
                                            else
                                                Console.WriteLine("");

                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                        }
                                    }
                                    break;

                                case "bang":
                                    {
                                        _inMainMenu = false;
                                        object value = CLIEngine.GetValidInputForEnum("What type of metaverse do you wish to create?", typeof(MetaverseType));

                                        if (value != null)
                                        {
                                            MetaverseType metaverseType = (MetaverseType)value;
                                        }
                                    }
                                    break;

                                case "wiz":
                                    {
                                        _inMainMenu = false;
                                        OASISResult<CoronalEjection> lightResult = null;
                                        string OAPPName = CLIEngine.GetValidInput("What is the name of the OAPP?");
                                        object value = CLIEngine.GetValidInputForEnum("What type of OAPP do you wish to create?", typeof(OAPPType));

                                        if (value != null)
                                        {
                                            OAPPType OAPPType = (OAPPType)value;

                                            value = CLIEngine.GetValidInputForEnum("What type of GenesisType do you wish to create?", typeof(GenesisType));

                                            if (value != null)
                                            {
                                                GenesisType genesisType = (GenesisType)value;

                                                string genesisNamespace = CLIEngine.GetValidInput("What is the Genesis Namespace?");
                                                Guid parentId = Guid.Empty;

                                                if (!CLIEngine.GetConfirmation("Do you wish to add support for all OASIS Providers (recommended) or only specific ones?"))
                                                {
                                                    bool providersSelected = false;
                                                    List<ProviderType> providers = new List<ProviderType>();

                                                    while (!providersSelected)
                                                    {
                                                        object objProviderType = CLIEngine.GetValidInputForEnum("What provider do you wish to add?", typeof(ProviderType));
                                                        providers.Add((ProviderType)objProviderType);

                                                        if (!CLIEngine.GetConfirmation("Do you wish to add any other providers?"))
                                                            providersSelected = true;
                                                    }
                                                }

                                                string zomeName = CLIEngine.GetValidInput("What is the name of the Zome (collection of Holons)?");
                                                string holonName = CLIEngine.GetValidInput("What is the name of the Holon (OASIS Data Object)?");
                                                string propName = CLIEngine.GetValidInput("What is the name of the Field/Property?");
                                                object propType = CLIEngine.GetValidInputForEnum("What is the type of the Field/Property?", typeof(HolonPropType));

                                                //TODO: Come back to this... :)

                                                if (CLIEngine.GetConfirmation("Does this OAPP belong to another CelestialBody?"))
                                                    parentId = CLIEngine.GetValidInputForGuid("What is the Id (GUID) of the parent CelestialBody?");


                                                if (lightResult != null)
                                                {
                                                    if (!lightResult.IsError && lightResult.Result != null)
                                                        CLIEngine.ShowSuccessMessage($"OAPP Successfully Generated. ({lightResult.Message})");
                                                    else
                                                        CLIEngine.ShowErrorMessage($"Error Occurred: {lightResult.Message}");
                                                }
                                            }
                                        }
                                    }
                                    break;

                                case "flare":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "shine":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "dim":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "seed":
                                    await STARCLI.OAPPs.PublishAsync();
                                    break;

                                case "unseed":
                                    await STARCLI.OAPPs.UnpublishAsync();
                                    break;

                                case "twinkle":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "dust":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "radiate":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "emit":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "reflect":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "evolve":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "mutate":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "love":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "burst":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "super":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "net":
                                    {
                                        CLIEngine.ShowMessage("Coming soon...");
                                    }
                                    break;

                                case "gate":
                                    {
                                        Process.Start(new ProcessStartInfo
                                        {
                                            FileName = "https://oasisweb4.one/portal",
                                            UseShellExecute = true
                                        });
                                    }
                                    break;

                                case "api":
                                    {
                                        //string url = "https://oasisweb4.one/star"; //TODO: When the new STAR API is deployed use this URL instead.
                                        string url = "https://oasisweb4.one";
                                        if (inputArgs.Length > 1 && inputArgs[1] == "oasis")
                                            url = "https://oasisweb4.one";

                                            Process.Start(new ProcessStartInfo
                                            {
                                                FileName = url,
                                                UseShellExecute = true
                                            });
                                    }
                                    break;

                                case "oapp":
                                    {
                                        if (inputArgs.Length > 1)
                                        {
                                            switch (inputArgs[1].ToLower())
                                            {
                                                case "publish":
                                                    {
                                                        string oappPath = "";
                                                        bool dotNetPublish = false;

                                                        if (inputArgs.Length > 2)
                                                            oappPath = inputArgs[2];

                                                        if (inputArgs.Length > 3 && inputArgs[3].ToLower() == "dotnetpublish")
                                                            dotNetPublish = true;

                                                        await STARCLI.OAPPs.PublishAsync(oappPath, dotNetPublish);
                                                    }
                                                    break;

                                                case "template":
                                                    await ShowSubCommandAsync<OAPPTemplate>(inputArgs, "OAPP TEMPLATE", "", STARCLI.OAPPTemplates.CreateAsync, STARCLI.OAPPTemplates.UpdateAsync, STARCLI.OAPPTemplates.DeleteAsync, STARCLI.OAPPTemplates.DownloadAndInstallAsync, STARCLI.OAPPTemplates.UninstallAsync, STARCLI.OAPPTemplates.PublishAsync, STARCLI.OAPPTemplates.UnpublishAsync, STARCLI.OAPPTemplates.RepublishAsync, STARCLI.OAPPTemplates.ActivateAsync, STARCLI.OAPPTemplates.DeactivateAsync, STARCLI.OAPPTemplates.ShowAsync, STARCLI.OAPPTemplates.ListAllCreatedByBeamedInAvatarAsync, STARCLI.OAPPTemplates.ListAllAsync, STARCLI.OAPPTemplates.ListAllInstalledForBeamedInAvatarAsync, STARCLI.OAPPTemplates.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.OAPPTemplates.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.OAPPTemplates.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.OAPPTemplates.SearchAsync, STARCLI.OAPPTemplates.AddDependencyAsync, STARCLI.OAPPTemplates.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                                    break;

                                                default:
                                                    await ShowSubCommandAsync<OAPP>(inputArgs, "OAPP", "", STARCLI.OAPPs.CreateAsync, STARCLI.OAPPs.UpdateAsync, STARCLI.OAPPs.DeleteAsync, STARCLI.OAPPs.DownloadAndInstallAsync, STARCLI.OAPPs.UninstallAsync, STARCLI.OAPPs.PublishAsync, STARCLI.OAPPs.UnpublishAsync, STARCLI.OAPPs.RepublishAsync, STARCLI.OAPPs.ActivateAsync, STARCLI.OAPPs.DeactivateAsync, STARCLI.OAPPs.ShowAsync, STARCLI.OAPPs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.OAPPs.ListAllAsync, STARCLI.OAPPs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.OAPPs.SearchAsync, STARCLI.OAPPs.AddDependencyAsync, STARCLI.OAPPs.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                                    break;
                                            }
                                        }
                                        else
                                            await ShowSubCommandAsync<OAPP>(inputArgs, "OAPP", "", STARCLI.OAPPs.CreateAsync, STARCLI.OAPPs.UpdateAsync, STARCLI.OAPPs.DeleteAsync, STARCLI.OAPPs.DownloadAndInstallAsync, STARCLI.OAPPs.UninstallAsync, STARCLI.OAPPs.PublishAsync, STARCLI.OAPPs.UnpublishAsync, STARCLI.OAPPs.RepublishAsync, STARCLI.OAPPs.ActivateAsync, STARCLI.OAPPs.DeactivateAsync, STARCLI.OAPPs.ShowAsync, STARCLI.OAPPs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.OAPPs.ListAllAsync, STARCLI.OAPPs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.OAPPs.SearchAsync, STARCLI.OAPPs.AddDependencyAsync, STARCLI.OAPPs.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);

                                        break;
                                    }

                                case "happ":
                                    {
                                        if (inputArgs.Length > 1)
                                        {
                                            switch (inputArgs[1].ToLower())
                                            {
                                                case "publish":
                                                    {
                                                        string oappPath = "";
                                                        bool dotNetPublish = false;

                                                        if (inputArgs.Length > 2)
                                                            oappPath = inputArgs[2];

                                                        if (inputArgs.Length > 3 && inputArgs[3].ToLower() == "dotnetpublish")
                                                            dotNetPublish = true;

                                                        await STARCLI.OAPPs.PublishAsync(oappPath, dotNetPublish); //TODO: Implement PublishHappAsync ASAP!
                                                    }
                                                    break;
                                            }
                                        }

                                        //TODO: Make a hAPP STARManager ASAP! ;-) I think!
                                        await ShowSubCommandAsync<OAPP>(inputArgs, "hApp", "", STARCLI.OAPPs.CreateAsync, STARCLI.OAPPs.UpdateAsync, STARCLI.OAPPs.DeleteAsync, STARCLI.OAPPs.DownloadAndInstallAsync, STARCLI.OAPPs.UninstallAsync, STARCLI.OAPPs.PublishAsync, STARCLI.OAPPs.UnpublishAsync, STARCLI.OAPPs.RepublishAsync, STARCLI.OAPPs.ActivateAsync, STARCLI.OAPPs.DeactivateAsync, STARCLI.OAPPs.ShowAsync, STARCLI.OAPPs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.OAPPs.ListAllAsync, STARCLI.OAPPs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.OAPPs.SearchAsync, STARCLI.OAPPs.AddDependencyAsync, STARCLI.OAPPs.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                        break;
                                    }

                                case "runtime":
                                    await ShowSubCommandAsync<Runtime>(inputArgs, "runtime", "runtimes", STARCLI.Runtimes.CreateAsync, STARCLI.Runtimes.UpdateAsync, STARCLI.Runtimes.DeleteAsync, STARCLI.Runtimes.DownloadAndInstallAsync, STARCLI.Runtimes.UninstallAsync, STARCLI.Runtimes.PublishAsync, STARCLI.Runtimes.UnpublishAsync, STARCLI.Runtimes.RepublishAsync, STARCLI.Runtimes.ActivateAsync, STARCLI.Runtimes.DeactivateAsync, STARCLI.Runtimes.ShowAsync, STARCLI.Runtimes.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Runtimes.ListAllAsync, STARCLI.Runtimes.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Runtimes.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Runtimes.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Runtimes.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Runtimes.SearchAsync, STARCLI.Runtimes.AddDependencyAsync, STARCLI.Runtimes.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    break;

                                case "lib":
                                    await ShowSubCommandAsync<Library>(inputArgs, "library", "libs", STARCLI.Libs.CreateAsync, STARCLI.Libs.UpdateAsync, STARCLI.Libs.DeleteAsync, STARCLI.Libs.DownloadAndInstallAsync, STARCLI.Libs.UninstallAsync, STARCLI.Libs.PublishAsync, STARCLI.Libs.UnpublishAsync, STARCLI.Libs.RepublishAsync, STARCLI.Libs.ActivateAsync, STARCLI.Libs.DeactivateAsync, STARCLI.Libs.ShowAsync, STARCLI.Libs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Libs.ListAllAsync, STARCLI.Libs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Libs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Libs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Libs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Libs.SearchAsync, STARCLI.Libs.AddDependencyAsync, STARCLI.Libs.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    break;

                                case "celestialspace":
                                    await ShowSubCommandAsync<STARCelestialSpace>(inputArgs, "celestial space", "celestial spaces", STARCLI.CelestialSpaces.CreateAsync, STARCLI.CelestialSpaces.UpdateAsync, STARCLI.CelestialSpaces.DeleteAsync, STARCLI.CelestialSpaces.DownloadAndInstallAsync, STARCLI.CelestialSpaces.UninstallAsync, STARCLI.CelestialSpaces.PublishAsync, STARCLI.CelestialSpaces.UnpublishAsync, STARCLI.CelestialSpaces.RepublishAsync, STARCLI.CelestialSpaces.ActivateAsync, STARCLI.CelestialSpaces.DeactivateAsync, STARCLI.CelestialSpaces.ShowAsync, STARCLI.CelestialSpaces.ListAllCreatedByBeamedInAvatarAsync, STARCLI.CelestialSpaces.ListAllAsync, STARCLI.CelestialSpaces.ListAllInstalledForBeamedInAvatarAsync, STARCLI.CelestialSpaces.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.CelestialSpaces.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.CelestialSpaces.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.CelestialSpaces.SearchAsync, STARCLI.CelestialSpaces.AddDependencyAsync, STARCLI.CelestialSpaces.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    break;

                                case "celestialbody":
                                    {
                                        bool showSubCommand = false;

                                        if (inputArgs.Length > 1)
                                        {
                                            if (inputArgs[1].ToLower() == "metadata")
                                                showSubCommand = true;
                                        }

                                        if (showSubCommand)
                                            await ShowSubCommandAsync<CelestialBodyMetaDataDNA>(inputArgs, "celestial body metadata", "celestial body metadata", STARCLI.CelestialBodiesMetaDataDNA.CreateAsync, STARCLI.CelestialBodiesMetaDataDNA.UpdateAsync, STARCLI.CelestialBodiesMetaDataDNA.DeleteAsync, STARCLI.CelestialBodiesMetaDataDNA.DownloadAndInstallAsync, STARCLI.CelestialBodiesMetaDataDNA.UninstallAsync, STARCLI.CelestialBodiesMetaDataDNA.PublishAsync, STARCLI.CelestialBodiesMetaDataDNA.UnpublishAsync, STARCLI.CelestialBodiesMetaDataDNA.RepublishAsync, STARCLI.CelestialBodiesMetaDataDNA.ActivateAsync, STARCLI.CelestialBodiesMetaDataDNA.DeactivateAsync, STARCLI.CelestialBodiesMetaDataDNA.ShowAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllCreatedByBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllInstalledForBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.SearchAsync, STARCLI.CelestialBodiesMetaDataDNA.AddDependencyAsync, STARCLI.CelestialBodiesMetaDataDNA.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                        else
                                            await ShowSubCommandAsync<STARCelestialBody>(inputArgs, "celestial body", "celestial bodies", STARCLI.CelestialBodies.CreateAsync, STARCLI.CelestialBodies.UpdateAsync, STARCLI.CelestialBodies.DeleteAsync, STARCLI.CelestialBodies.DownloadAndInstallAsync, STARCLI.CelestialBodies.UninstallAsync, STARCLI.CelestialBodies.PublishAsync, STARCLI.CelestialBodies.UnpublishAsync, STARCLI.CelestialBodies.RepublishAsync, STARCLI.CelestialBodies.ActivateAsync, STARCLI.CelestialBodies.DeactivateAsync, STARCLI.CelestialBodies.ShowAsync, STARCLI.CelestialBodies.ListAllCreatedByBeamedInAvatarAsync, STARCLI.CelestialBodies.ListAllAsync, STARCLI.CelestialBodies.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Zomes.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Zomes.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Zomes.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Zomes.SearchAsync, STARCLI.Zomes.AddDependencyAsync, STARCLI.Zomes.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    }
                                    break;

                                case "zome":
                                    {
                                        bool showSubCommand = false;

                                        if (inputArgs.Length > 1)
                                        {
                                            if (inputArgs[1].ToLower() == "metadata")
                                                showSubCommand = true;
                                        }

                                        if (showSubCommand)
                                            await ShowSubCommandAsync<ZomeMetaDataDNA>(inputArgs, "zome metadata", "zome metadata", STARCLI.ZomesMetaDataDNA.CreateAsync, STARCLI.ZomesMetaDataDNA.UpdateAsync, STARCLI.ZomesMetaDataDNA.DeleteAsync, STARCLI.ZomesMetaDataDNA.DownloadAndInstallAsync, STARCLI.ZomesMetaDataDNA.UninstallAsync, STARCLI.ZomesMetaDataDNA.PublishAsync, STARCLI.ZomesMetaDataDNA.UnpublishAsync, STARCLI.ZomesMetaDataDNA.RepublishAsync, STARCLI.ZomesMetaDataDNA.ActivateAsync, STARCLI.ZomesMetaDataDNA.DeactivateAsync, STARCLI.ZomesMetaDataDNA.ShowAsync, STARCLI.ZomesMetaDataDNA.ListAllCreatedByBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.ListAllAsync, STARCLI.ZomesMetaDataDNA.ListAllInstalledForBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.SearchAsync, STARCLI.ZomesMetaDataDNA.AddDependencyAsync, STARCLI.ZomesMetaDataDNA.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                        else
                                            await ShowSubCommandAsync<STARZome>(inputArgs, "zome", "zomes", STARCLI.Zomes.CreateAsync, STARCLI.Zomes.UpdateAsync, STARCLI.Zomes.DeleteAsync, STARCLI.Zomes.DownloadAndInstallAsync, STARCLI.Zomes.UninstallAsync, STARCLI.Zomes.PublishAsync, STARCLI.Zomes.UnpublishAsync, STARCLI.Zomes.RepublishAsync, STARCLI.Zomes.ActivateAsync, STARCLI.Zomes.DeactivateAsync, STARCLI.Zomes.ShowAsync, STARCLI.Zomes.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Zomes.ListAllAsync, STARCLI.Zomes.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Zomes.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Zomes.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Zomes.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Zomes.SearchAsync, STARCLI.Zomes.AddDependencyAsync, STARCLI.Zomes.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    }
                                    break;

                                case "holon":
                                    {
                                        bool showSubCommand = false;

                                        if (inputArgs.Length > 1)
                                        {
                                            if (inputArgs[1].ToLower() == "metadata")
                                                showSubCommand = true;
                                        }

                                        if (showSubCommand)
                                            await ShowSubCommandAsync<HolonMetaDataDNA>(inputArgs, "holon metadata", "holon metadata", STARCLI.HolonsMetaDataDNA.CreateAsync, STARCLI.HolonsMetaDataDNA.UpdateAsync, STARCLI.HolonsMetaDataDNA.DeleteAsync, STARCLI.HolonsMetaDataDNA.DownloadAndInstallAsync, STARCLI.HolonsMetaDataDNA.UninstallAsync, STARCLI.HolonsMetaDataDNA.PublishAsync, STARCLI.HolonsMetaDataDNA.UnpublishAsync, STARCLI.HolonsMetaDataDNA.RepublishAsync, STARCLI.HolonsMetaDataDNA.ActivateAsync, STARCLI.HolonsMetaDataDNA.DeactivateAsync, STARCLI.HolonsMetaDataDNA.ShowAsync, STARCLI.HolonsMetaDataDNA.ListAllCreatedByBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.ListAllAsync, STARCLI.HolonsMetaDataDNA.ListAllInstalledForBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.SearchAsync, STARCLI.HolonsMetaDataDNA.AddDependencyAsync, STARCLI.HolonsMetaDataDNA.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                        else
                                            await ShowSubCommandAsync<STARHolon>(inputArgs, "holon", "holons", STARCLI.Holons.CreateAsync, STARCLI.Holons.UpdateAsync, STARCLI.Holons.DeleteAsync, STARCLI.Holons.DownloadAndInstallAsync, STARCLI.Holons.UninstallAsync, STARCLI.Holons.PublishAsync, STARCLI.Holons.UnpublishAsync, STARCLI.Holons.RepublishAsync, STARCLI.Holons.ActivateAsync, STARCLI.Holons.DeactivateAsync, STARCLI.Holons.ShowAsync, STARCLI.Holons.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Holons.ListAllAsync, STARCLI.Holons.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Holons.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Holons.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Holons.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Holons.SearchAsync, STARCLI.Holons.AddDependencyAsync, STARCLI.Holons.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    }
                                    break;

                                case "chapter":
                                    await ShowSubCommandAsync<Chapter>(inputArgs, "chapter", "chapters", STARCLI.Chapters.CreateAsync, STARCLI.Chapters.UpdateAsync, STARCLI.Chapters.DeleteAsync, STARCLI.Chapters.DownloadAndInstallAsync, STARCLI.Chapters.UninstallAsync, STARCLI.Chapters.PublishAsync, STARCLI.Chapters.UnpublishAsync, STARCLI.Chapters.RepublishAsync, STARCLI.Chapters.ActivateAsync, STARCLI.Chapters.DeactivateAsync, STARCLI.Chapters.ShowAsync, STARCLI.Chapters.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Chapters.ListAllAsync, STARCLI.Chapters.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Chapters.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Chapters.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Chapters.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Chapters.SearchAsync, STARCLI.Chapters.AddDependencyAsync, STARCLI.Chapters.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    break;

                                case "mission":
                                    await ShowSubCommandAsync<Mission>(inputArgs, "mission", "missions", STARCLI.Missions.CreateAsync, STARCLI.Missions.UpdateAsync, STARCLI.Missions.DeleteAsync, STARCLI.Missions.DownloadAndInstallAsync, STARCLI.Missions.UninstallAsync, STARCLI.Missions.PublishAsync, STARCLI.Missions.UnpublishAsync, STARCLI.Missions.RepublishAsync, STARCLI.Missions.ActivateAsync, STARCLI.Missions.DeactivateAsync, STARCLI.Missions.ShowAsync, STARCLI.Missions.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Missions.ListAllAsync, STARCLI.Missions.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Missions.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Missions.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Missions.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Missions.SearchAsync, STARCLI.Missions.AddDependencyAsync, STARCLI.Missions.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    break;

                                case "quest":
                                    await ShowSubCommandAsync<Quest>(inputArgs, "quest", "quests", STARCLI.Quests.CreateAsync, STARCLI.Quests.UpdateAsync, STARCLI.Quests.DeleteAsync, STARCLI.Quests.DownloadAndInstallAsync, STARCLI.Quests.UninstallAsync, STARCLI.Quests.PublishAsync, STARCLI.Quests.UnpublishAsync, STARCLI.Quests.RepublishAsync, STARCLI.Quests.ActivateAsync, STARCLI.Quests.DeactivateAsync, STARCLI.Quests.ShowAsync, STARCLI.Quests.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Quests.ListAllAsync, STARCLI.Quests.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Quests.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Quests.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Quests.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Quests.SearchAsync, STARCLI.Quests.AddDependencyAsync, STARCLI.Quests.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    break;

                                case "nft":
                                    {
                                       if (inputArgs.Length > 1 && inputArgs[1].ToLower() == "collection")
                                            await ShowSubCommandAsync<STARNFTCollection>(inputArgs, "nft collection", "nft collection's", STARCLI.NFTCollections.CreateAsync, STARCLI.NFTCollections.UpdateAsync, STARCLI.NFTCollections.DeleteAsync, STARCLI.NFTCollections.DownloadAndInstallAsync, STARCLI.NFTCollections.UninstallAsync, STARCLI.NFTCollections.PublishAsync, STARCLI.NFTCollections.UnpublishAsync, STARCLI.NFTCollections.RepublishAsync, STARCLI.NFTCollections.ActivateAsync, STARCLI.NFTCollections.DeactivateAsync, STARCLI.NFTCollections.ShowAsync, STARCLI.NFTCollections.ListAllCreatedByBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllAsync, STARCLI.NFTCollections.ListAllInstalledForBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.NFTCollections.SearchAsync, STARCLI.NFTCollections.AddDependencyAsync, STARCLI.NFTCollections.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, createWeb4Predicate: STARCLI.NFTCollections.CreateWeb4NFTCollectionAsync, updateWeb4Predicate: STARCLI.NFTCollections.UpdateWeb4NFTCollectionAsync, deleteWeb4Predicate: STARCLI.NFTCollections.DeleteWeb4NFTCollectionAsync, addWeb4NFTToCollectionPredicate: STARCLI.NFTCollections.AddWeb4NFTToCollectionAsync, removeWeb4NFTFromCollectionPredicate: STARCLI.NFTCollections.RemoveWeb4NFTFromCollectionAsync, listAllWeb4Predicate: STARCLI.NFTCollections.ListAllWeb4NFTCollections, listWeb4ForBeamedInAvatarPredicate: STARCLI.NFTCollections.ListWeb4NFTCollectionsForAvatar, showWeb4Predicate: STARCLI.NFTCollections.ShowWeb4NFTCollectionAsync, searchWeb4Predicate: STARCLI.NFTCollections.SearchWeb4NFTCollectionAsync, providerType: providerType);
                                       else
                                            await ShowSubCommandAsync<STARNFT>(inputArgs, "nft", "nft's", STARCLI.NFTs.CreateAsync, STARCLI.NFTs.UpdateAsync, STARCLI.NFTs.DeleteAsync, STARCLI.NFTs.DownloadAndInstallAsync, STARCLI.NFTs.UninstallAsync, STARCLI.NFTs.PublishAsync, STARCLI.NFTs.UnpublishAsync, STARCLI.NFTs.RepublishAsync, STARCLI.NFTs.ActivateAsync, STARCLI.NFTs.DeactivateAsync, STARCLI.NFTs.ShowAsync, STARCLI.NFTs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.NFTs.ListAllAsync, STARCLI.NFTs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.NFTs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.NFTs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.NFTs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.NFTs.SearchAsync, STARCLI.NFTs.AddDependencyAsync, STARCLI.NFTs.RemoveDependencyAsync, clonePredicate: STARCLI.NFTs.CloneAsync, mintPredicate: STARCLI.NFTs.MintNFTAsync, burnPredicate: STARCLI.NFTs.BurnNFTAsync, importPredicate: STARCLI.NFTs.ImportNFTAsync, exportPredicate: STARCLI.NFTs.ExportNFTAsync,  convertPredicate: STARCLI.NFTs.ConvertNFTAsync, updateWeb4Predicate: STARCLI.NFTs.UpdateWeb4NFTAsync, deleteWeb4Predicate: STARCLI.NFTs.DeleteWeb4NFTAsync, listAllWeb4Predicate: STARCLI.NFTs.ListAllWeb4NFTsAsync, listWeb4ForBeamedInAvatarPredicate: STARCLI.NFTs.ListAllWeb4NFTForAvatarsAsync, showWeb4Predicate: STARCLI.NFTs.ShowWeb4NFTAsync, searchWeb4Predicate: STARCLI.NFTs.SearchWeb4NFTAsync, providerType: providerType);
                                    }
                                    break;

                                case "geonft":
                                    {
                                        if (inputArgs.Length > 1 && inputArgs[1].ToLower() == "collection")
                                            await ShowSubCommandAsync<STARGeoNFTCollection>(inputArgs, "geo-nft collection", "geo-nft collection's", STARCLI.GeoNFTCollections.CreateAsync, STARCLI.GeoNFTCollections.UpdateAsync, STARCLI.GeoNFTCollections.DeleteAsync, STARCLI.GeoNFTCollections.DownloadAndInstallAsync, STARCLI.GeoNFTCollections.UninstallAsync, STARCLI.GeoNFTCollections.PublishAsync, STARCLI.GeoNFTCollections.UnpublishAsync, STARCLI.GeoNFTCollections.RepublishAsync, STARCLI.GeoNFTCollections.ActivateAsync, STARCLI.GeoNFTCollections.DeactivateAsync, STARCLI.GeoNFTCollections.ShowAsync, STARCLI.GeoNFTCollections.ListAllCreatedByBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllAsync, STARCLI.GeoNFTCollections.ListAllInstalledForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.SearchAsync, STARCLI.GeoNFTCollections.AddDependencyAsync, STARCLI.GeoNFTCollections.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, createWeb4Predicate: STARCLI.GeoNFTCollections.CreateWeb4GeoNFTCollectionAsync, updateWeb4Predicate: STARCLI.GeoNFTCollections.UpdateWeb4GeoNFTCollectionAsync, addWeb4NFTToCollectionPredicate: STARCLI.GeoNFTCollections.AddWeb4GeoNFTToCollectionAsync, removeWeb4NFTFromCollectionPredicate: STARCLI.GeoNFTCollections.RemoveWeb4GeoNFTFromCollectionAsync, deleteWeb4Predicate: STARCLI.GeoNFTCollections.DeleteWeb4GeoNFTCollectionAsync, listAllWeb4Predicate: STARCLI.GeoNFTCollections.ListAllWeb4GeoNFTCollections, listWeb4ForBeamedInAvatarPredicate: STARCLI.GeoNFTCollections.ListWeb4GeoNFTCollectionsForAvatar, showWeb4Predicate: STARCLI.GeoNFTCollections.ShowWeb4GeoNFTCollectionAsync, searchWeb4Predicate: STARCLI.GeoNFTCollections.SearchWeb4GeoNFTCollectionAsync, providerType: providerType);
                                        else
                                            await ShowSubCommandAsync<STARGeoNFT>(inputArgs, "geo-nft", "geo-nft's", STARCLI.GeoNFTs.CreateAsync, STARCLI.GeoNFTs.UpdateAsync, STARCLI.GeoNFTs.DeleteAsync, STARCLI.GeoNFTs.DownloadAndInstallAsync, STARCLI.GeoNFTs.UninstallAsync, STARCLI.GeoNFTs.PublishAsync, STARCLI.GeoNFTs.UnpublishAsync, STARCLI.GeoNFTs.RepublishAsync, STARCLI.GeoNFTs.ActivateAsync, STARCLI.GeoNFTs.DeactivateAsync, STARCLI.GeoNFTs.ShowAsync, STARCLI.GeoNFTs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllAsync, STARCLI.GeoNFTs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.GeoNFTs.SearchAsync, STARCLI.GeoNFTs.AddDependencyAsync, STARCLI.GeoNFTs.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, mintPredicate: STARCLI.GeoNFTs.MintGeoNFTAsync, burnPredicate: STARCLI.GeoNFTs.BurnGeoNFTAsync, importPredicate: STARCLI.GeoNFTs.ImportGeoNFTAsync, exportPredicate: STARCLI.GeoNFTs.ExportGeoNFTAsync, convertPredicate: STARCLI.GeoNFTs.ConvertGeoNFTAsync, updateWeb4Predicate: STARCLI.GeoNFTs.UpdateWeb4GeoNFTAsync, deleteWeb4Predicate: STARCLI.GeoNFTs.DeleteWeb4GeoNFTAsync, listAllWeb4Predicate: STARCLI.GeoNFTs.ListAllWeb4GeoNFTsAsync, listWeb4ForBeamedInAvatarPredicate: STARCLI.GeoNFTs.ListAllWeb4GeoNFTForAvatarsAsync, showWeb4Predicate: STARCLI.GeoNFTs.ShowWeb4GeoNFTAsync, searchWeb4Predicate: STARCLI.GeoNFTs.SearchWeb4GeoNFTAsync, providerType: providerType);
                                    }
                                    break;

                                case "geohotspot":
                                    await ShowSubCommandAsync<GeoHotSpot>(inputArgs, "geo-hotspot", "geo-hotspots", STARCLI.GeoHotSpots.CreateAsync, STARCLI.GeoHotSpots.UpdateAsync, STARCLI.GeoHotSpots.DeleteAsync, STARCLI.GeoHotSpots.DownloadAndInstallAsync, STARCLI.GeoHotSpots.UninstallAsync, STARCLI.GeoHotSpots.PublishAsync, STARCLI.GeoHotSpots.UnpublishAsync, STARCLI.GeoHotSpots.RepublishAsync, STARCLI.GeoHotSpots.ActivateAsync, STARCLI.GeoHotSpots.DeactivateAsync, STARCLI.GeoHotSpots.ShowAsync, STARCLI.GeoHotSpots.ListAllCreatedByBeamedInAvatarAsync, STARCLI.GeoHotSpots.ListAllAsync, STARCLI.GeoHotSpots.ListAllInstalledForBeamedInAvatarAsync, STARCLI.GeoHotSpots.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.GeoHotSpots.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.GeoHotSpots.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.GeoHotSpots.SearchAsync, STARCLI.GeoHotSpots.AddDependencyAsync, STARCLI.GeoHotSpots.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    break;

                                case "inventoryitem":
                                    await ShowSubCommandAsync<InventoryItem>(inputArgs, "inventoryitem", "inventoryitem", STARCLI.InventoryItems.CreateAsync, STARCLI.InventoryItems.UpdateAsync, STARCLI.InventoryItems.DeleteAsync, STARCLI.InventoryItems.DownloadAndInstallAsync, STARCLI.InventoryItems.UninstallAsync, STARCLI.InventoryItems.PublishAsync, STARCLI.InventoryItems.UnpublishAsync, STARCLI.InventoryItems.RepublishAsync, STARCLI.InventoryItems.ActivateAsync, STARCLI.InventoryItems.DeactivateAsync, STARCLI.InventoryItems.ShowAsync, STARCLI.InventoryItems.ListAllCreatedByBeamedInAvatarAsync, STARCLI.InventoryItems.ListAllAsync, STARCLI.InventoryItems.ListAllInstalledForBeamedInAvatarAsync, STARCLI.InventoryItems.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.InventoryItems.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.InventoryItems.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.InventoryItems.SearchAsync, STARCLI.InventoryItems.AddDependencyAsync, STARCLI.InventoryItems.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    break;

                                case "plugin":
                                    await ShowSubCommandAsync<Plugin>(inputArgs, "plugin", "plugin", STARCLI.Plugins.CreateAsync, STARCLI.Plugins.UpdateAsync, STARCLI.Plugins.DeleteAsync, STARCLI.Plugins.DownloadAndInstallAsync, STARCLI.Plugins.UninstallAsync, STARCLI.Plugins.PublishAsync, STARCLI.Plugins.UnpublishAsync, STARCLI.Plugins.RepublishAsync, STARCLI.Plugins.ActivateAsync, STARCLI.Plugins.DeactivateAsync, STARCLI.Plugins.ShowAsync, STARCLI.Plugins.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Plugins.ListAllAsync, STARCLI.Plugins.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Plugins.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Plugins.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Plugins.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Plugins.SearchAsync, STARCLI.Plugins.AddDependencyAsync, STARCLI.Plugins.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                    break;

                                case "avatar":
                                    await ShowAvatarSubCommandAsync(inputArgs);
                                    break;

                                case "karma":
                                    await ShowKarmaSubCommandAsync(inputArgs);
                                    break;

                                case "keys":
                                    await ShowKeysSubCommandAsync(inputArgs);
                                    break;

                                case "wallet":
                                    await ShowWalletSubCommandAsync(inputArgs);
                                    break;

                                case "map":
                                    await ShowMapSubCommandAsync(inputArgs);
                                    break;

                                case "seeds":
                                    await ShowSeedsSubCommandAsync(inputArgs);
                                    break;

                                case "data":
                                    await ShowDataSubCommandAsync(inputArgs);
                                    break;

                                case "oland":
                                    await ShowOlandSubCommandAsync(inputArgs);
                                    break;

                                case "search":
                                    CLIEngine.ShowMessage("Coming soon...");
                                    break;

                                case "onode":
                                    await ShowONODEConfigSubCommandAsync(inputArgs);
                                    break;

                                case "hypernet":
                                    await ShowHyperNETSubCommandAsync(inputArgs);
                                    break;

                                case "onet":
                                    await ShowONETSubCommandAsync(inputArgs);
                                    break;

                                case "config":
                                    await ShowConfigSubCommandAsync(inputArgs);
                                    break;

                                case "runcosmictests":
                                    {
                                        object oappTypeObj = null;
                                        OAPPType OAPPType = DEFAULT_OAPP_TYPE;
                                        //OAPPTemplateType OAPPTemplateType = DEFAULT_OAPP_TEMPLATE_TYPE;
                                        Guid OAPPTemplateId = Guid.NewGuid(); //TODO: Replace with an existing built-in OAPP Template Id (or allow user to specify one?).
                                        int OAPPTemplateVersion = 1;
                                        string dnaFolder = DEFAULT_DNA_FOLDER;
                                        string genesisFolder = DEFAULT_GENESIS_FOLDER;
                                        //string genesisNameSpace = DEFAULT_GENESIS_NAMESPACE;

                                        if (inputArgs.Length > 1)
                                        {
                                            if (Enum.TryParse(typeof(OAPPType), inputArgs[2], true, out oappTypeObj))
                                                OAPPType = (OAPPType)oappTypeObj;
                                        }

                                        if (inputArgs.Length > 2)
                                            dnaFolder = inputArgs[1];

                                        if (inputArgs.Length > 3)
                                            genesisFolder = inputArgs[2];

                                        if (OAPPType == DEFAULT_OAPP_TYPE)
                                            CLIEngine.ShowWorkingMessage($"OAPPType Not Specified, Using Default: {Enum.GetName(typeof(OAPPType), OAPPType)}");
                                        else
                                            CLIEngine.ShowWorkingMessage($"OAPPType Specified: {Enum.GetName(typeof(OAPPType), OAPPType)}");

                                        if (dnaFolder == DEFAULT_DNA_FOLDER)
                                            CLIEngine.ShowWorkingMessage($"DNAFolder Not Specified, Using Default: {dnaFolder}");
                                        else
                                            CLIEngine.ShowWorkingMessage($"DNAFolder Specified: {dnaFolder}");

                                        if (genesisFolder == DEFAULT_GENESIS_FOLDER)
                                            CLIEngine.ShowWorkingMessage($"GenesisFolder Not Specified, Using Default: {genesisFolder}");
                                        else
                                            CLIEngine.ShowWorkingMessage($"GenesisFolder Specified: {genesisFolder}");

                                        await STARCLI.STARTests.RunCOSMICTests(OAPPType, OAPPTemplateId, OAPPTemplateVersion, dnaFolder, genesisFolder);
                                    }
                                    break;

                                case "runoasisapitests":
                                    await STARCLI.STARTests.RunOASISAPTests();
                                    break;

                                default:
                                    CLIEngine.ShowErrorMessage("Command Unknown.");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        //ConsoleKeyInfo keyInfo = Console.ReadKey();

                        //if (keyInfo.KeyChar == 'c' && keyInfo.Modifiers == ConsoleModifiers.Control)
                        //    exit = CLIEngine.GetConfirmation("STAR: Are you sure you wish to exit?");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError($"An unknown error occurred in STARCLI.ReadyPlayerOne. Reason: {ex}", ex);
                }
            }
            while (!exit);

            CLIEngine.ShowMessage("Thank you for using STAR & The OASIS! We hope you enjoyed your stay, have a nice day! :)");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static async Task ShowSubCommandAsync<T>(string[] inputArgs, 
            string subCommand = "",
            string subCommandPlural = "",
            Func<ISTARNETCreateOptions<T, STARNETDNA>, object, bool, ProviderType, Task> createPredicate = null,  //WEB5 Commands
            Func<string, object, bool, ProviderType, Task> updatePredicate = null, 
            Func<string, bool, ProviderType, Task> deletePredicate = null,
            Func<string, InstallMode, ProviderType, Task> downloadAndInstallPredicate = null,
            Func<string, ProviderType, Task> uninstallPredicate = null,
            //Func<string, ProviderType, Task> reinstallPredicate = null,
            Func<string, bool, DefaultLaunchMode, bool, ProviderType, Task> publishPredicate = null,
            Func<string, ProviderType, Task> unpublishPredicate = null,
            Func<string, ProviderType, Task> republishPredicate = null,
            Func<string, ProviderType, Task> activatePredicate = null,
            Func<string, ProviderType, Task> deactivatePredicate = null,
            Func<string, bool, ProviderType, Task> showPredicate = null,
            Func<bool, bool, ProviderType, Task> listForBeamedInAvatarPredicate = null,
            Func<bool, bool, int, ProviderType, Task> listAllPredicate = null,
            Func<ProviderType, Task> listInstalledPredicate = null,
            Func<ProviderType, Task> listUninstalledPredicate = null,
            Func<ProviderType, Task> listUnpublishedPredicate = null,
            Func<ProviderType, Task> listDeactivatedPredicate = null,
            Func<string, bool, bool, ProviderType, Task> searchPredicate = null,
            Func<string, string, string, ISTARNETDNA, ProviderType, Task> addDependencyPredicate = null,
            Func<string, string, string, ProviderType, Task> removeDependencyPredicate = null,
            Func<object, Task> clonePredicate = null,
            Func<object, Task> mintPredicate = null, //WEB4 Commands
            Func<object, Task> burnPredicate = null,
            Func<object, Task> importPredicate = null,
            Func<object, Task> exportPredicate = null,
            //Func<object, Task> clonePredicate = null,
            Func<object, Task> convertPredicate = null,
            Func<object, ProviderType, Task> createWeb4Predicate = null,
            Func<string, ProviderType, Task> updateWeb4Predicate = null,
            Func<string, bool, ProviderType, Task> deleteWeb4Predicate = null,
            Func<string, ProviderType, Task> showWeb4Predicate = null,
            Func<string, bool, ProviderType, Task> searchWeb4Predicate = null,
            Func<ProviderType, Task> listAllWeb4Predicate = null,
            Func<ProviderType, Task> listWeb4ForBeamedInAvatarPredicate = null,
            Func<string, string, ProviderType, Task> addWeb4NFTToCollectionPredicate = null,
            Func<string, string, ProviderType, Task> removeWeb4NFTFromCollectionPredicate = null,
            bool showCreate = true,
            bool showUpdate = true,
            bool showDelete = true,
            ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        {
            string subCommandParam = "";
            string subCommandParam2 = "";
            string subCommandParam3 = "";
            string subCommandParam4 = "";
            bool showAllVersions = false;
            bool showForAllAvatars = false;
            bool showDetailed = false;
            bool web3 = false;
            bool web4 = false;
            string id = "";

            if (string.IsNullOrEmpty(subCommand))
                subCommand = inputArgs[0];

            //if ((inputArgs.Length > 1 && inputArgs[1] != "template" && inputArgs[1] != "metadata") || (inputArgs.Length > 2 && (inputArgs[1] == "template" || inputArgs[1] == "metadata")))
            if ((inputArgs.Length > 1 && inputArgs[1] != "template" && inputArgs[1] != "metadata" && inputArgs[1] != "collection") || (inputArgs.Length > 2 && (inputArgs[1] == "template" || inputArgs[1] == "metadata" || inputArgs[1] == "collection")))
            { 
                if (inputArgs[1] != "template" && inputArgs[1] != "metadata" && inputArgs[1] != "collection" && inputArgs.Length > 2)
                    id = inputArgs[2];

                if ((inputArgs[1] == "template" || inputArgs[1] == "metadata" || inputArgs[1] == "collection") && inputArgs.Length > 3)
                    id = inputArgs[3];


                if (inputArgs.Length > 1 && !string.IsNullOrEmpty(inputArgs[1]))
                    subCommandParam = inputArgs[1].ToLower();

                if (inputArgs.Length > 2 && !string.IsNullOrEmpty(inputArgs[2]))
                    subCommandParam2 = inputArgs[2].ToLower();

                if (inputArgs.Length > 3 && !string.IsNullOrEmpty(inputArgs[3]))
                    subCommandParam3 = inputArgs[3].ToLower();

                if (inputArgs.Length > 4 && !string.IsNullOrEmpty(inputArgs[4]))
                    subCommandParam4 = inputArgs[4].ToLower();

                if (inputArgs[1] == "template" || inputArgs[1] == "metadata" || inputArgs[1] == "collection")
                {
                    if (inputArgs.Length > 2 && !string.IsNullOrEmpty(inputArgs[2]))
                        subCommandParam = inputArgs[2].ToLower();

                    if (inputArgs.Length > 3 && !string.IsNullOrEmpty(inputArgs[3]))
                        subCommandParam2 = inputArgs[3].ToLower();

                    if (inputArgs.Length > 4 && !string.IsNullOrEmpty(inputArgs[4]))
                        subCommandParam3 = inputArgs[4].ToLower();

                    if (inputArgs.Length > 5 && !string.IsNullOrEmpty(inputArgs[5]))
                        subCommandParam4 = inputArgs[5].ToLower();
                }

                if (subCommandParam2.ToLower() == "allversions" || subCommandParam3.ToLower() == "allversions")
                    showAllVersions = true;

                if (subCommandParam2.ToLower() == "forallavatars" || subCommandParam3.ToLower() == "forallavatars")
                    showForAllAvatars = true;

                if (subCommandParam == "detailed" || subCommandParam2 == "detailed" || subCommandParam3 == "detailed")
                    showDetailed = true;

                web3 = subCommandParam == "web3" || subCommandParam2 == "web3" || subCommandParam3 == "web3" || subCommandParam4 == "web3" ? true : false;
                web4 = subCommandParam == "web4" || subCommandParam2 == "web4" || subCommandParam3 == "web4" || subCommandParam4 == "web4" ? true : false;

                switch (subCommandParam)
                {
                    case "create":
                        {
                            if (showCreate)
                            {
                                if (web4)
                                {
                                    if (createWeb4Predicate != null)
                                        await createWeb4Predicate(null, providerType); //TODO: Pass in params in a object or dynamic obj.
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else
                                {
                                    if (createPredicate != null)
                                        await createPredicate(null, null, true, providerType); //TODO: Pass in params in a object or dynamic obj.
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "mint":
                        {
                            if (mintPredicate != null)
                                await mintPredicate(null);
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "place":
                        {
                            if (subCommand.ToUpper() == "GEONFT")
                                await STARCLI.GeoNFTs.PublishAsync();
                            else
                                CLIEngine.ShowWarningMessage("This sub-command is only supported for the command 'geonft'.");
                        }
                        break;

                    case "burn":
                        {
                            if (burnPredicate != null)
                                await burnPredicate(null);
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "import":
                        {
                            if (importPredicate != null)
                                await importPredicate(web3);
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "export":
                        {
                            if (exportPredicate != null)
                                await exportPredicate(null);
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "clone":
                        {
                            if (clonePredicate != null)
                                await clonePredicate(null);
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "convert":
                        {
                            if (convertPredicate != null)
                                await convertPredicate(null);
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "update":
                        {
                            if (showUpdate)
                            {
                                if (web4)
                                {
                                    id = "";

                                    if (inputArgs.Length > 3)
                                        id = inputArgs[3];

                                    if (updateWeb4Predicate != null)
                                        await updateWeb4Predicate(id, providerType); //TODO: Pass in params in a object or dynamic obj.
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else
                                {
                                    if (updatePredicate != null)
                                        await updatePredicate(id, null, true, providerType);
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "delete":
                        {
                            if (showDelete)
                            {
                                bool softDelete = true;

                                if (inputArgs.Length > 3)
                                    bool.TryParse(inputArgs[3], out softDelete);

                                if (web4)
                                {
                                    id = "";

                                    if (inputArgs.Length > 3)
                                        id = inputArgs[3];

                                    if (inputArgs.Length > 4)
                                        bool.TryParse(inputArgs[4], out softDelete);

                                    if (deleteWeb4Predicate != null)
                                        await deleteWeb4Predicate(id, softDelete, providerType);
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else
                                {
                                    if (deletePredicate != null)
                                        await deletePredicate(id, softDelete, providerType);
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "download":
                        {
                            //if (isOAPPOrHappOrRuntime)
                            //{
                                if (downloadAndInstallPredicate != null)
                                    await downloadAndInstallPredicate(id, InstallMode.DownloadOnly, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            //}
                            //else
                            //    CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "install":
                        {
                            //if (isOAPPOrHappOrRuntime)
                            //{
                                if (downloadAndInstallPredicate != null)
                                    await downloadAndInstallPredicate(id, InstallMode.DownloadAndInstall, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            //}
                            //else
                            //    CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "uninstall":
                        {
                            //if (isOAPPOrHappOrRuntime)
                            //{
                                if (uninstallPredicate != null)
                                    await uninstallPredicate(id, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            //}
                            //else
                            //    CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    //case "reinstall":
                    //    {
                    //        if (isOAPPOrHappOrRuntime)
                    //        {
                    //            if (reinstallPredicate != null)
                    //                await reinstallPredicate(id, providerType);
                    //            else
                    //                CLIEngine.ShowMessage("Coming Soon...");
                    //        }
                    //        else
                    //            CLIEngine.ShowMessage("Command not supported.");
                    //    }
                    //    break;

                    case "publish":
                        {
                            if (publishPredicate != null)
                            {
                                if (subCommand.ToUpper() == "RUNTIME")
                                    await publishPredicate(id, false, DefaultLaunchMode.None, true, providerType);
                                else
                                    await publishPredicate(id, false, DefaultLaunchMode.Optional, true, providerType);
                            }
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "unpublish":
                        {
                            if (unpublishPredicate != null)
                                await unpublishPredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "republish":
                        {
                            if (republishPredicate != null)
                                await republishPredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "activate":
                        {
                            if (activatePredicate != null)
                                await activatePredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "deactivate":
                        {
                            if (deactivatePredicate != null)
                                await deactivatePredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "show":
                        {
                            if (id == "detailed")
                                id = inputArgs[3];

                            if (web4)
                            {
                                id = subCommandParam3;

                                if (showWeb4Predicate != null)
                                    await showWeb4Predicate(id, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else
                            {
                                if (showPredicate != null)
                                    await showPredicate(id, showDetailed, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                        }
                        break;

                    case "adddependency":
                        {
                            if (addDependencyPredicate != null)
                                await addDependencyPredicate(id, subCommandParam3, subCommandParam4, null, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "removedependency":
                        {
                            if (removeDependencyPredicate != null)
                                await removeDependencyPredicate(id, subCommandParam3, subCommandParam4, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "send":
                        {
                            if (subCommand.ToUpper() == "NFT")
                                await STARCLI.NFTs.SendNFTAsync();

                            else if (subCommand.ToUpper() == "GEONFT")
                                await STARCLI.GeoNFTs.SendGeoNFTAsync();
                            
                            else
                                CLIEngine.ShowWarningMessage("This sub-command is only supported for the command 'geonft' or 'nft'.");
                        }
                        break;

                    case "add":
                        {
                            if (addWeb4NFTToCollectionPredicate != null)
                                await addWeb4NFTToCollectionPredicate(id, subCommandParam3, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "remove":
                        {
                            if (removeWeb4NFTFromCollectionPredicate != null)
                                await removeWeb4NFTFromCollectionPredicate(id, subCommandParam3, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "list":
                        {
                            switch (subCommandParam2.ToLower())
                            {
                                case "installed":
                                    {
                                        if (listInstalledPredicate != null)
                                            await listInstalledPredicate(providerType);
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    break;

                                case "uninstalled":
                                    {
                                        if (listInstalledPredicate != null)
                                            await listUninstalledPredicate(providerType);
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    break;

                                case "unpublished":
                                    {
                                        if (listUnpublishedPredicate != null)
                                            await listUnpublishedPredicate(providerType);
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    break;

                                case "deactivated":
                                    {
                                        if (listDeactivatedPredicate != null)
                                            await listDeactivatedPredicate(providerType);
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    break;

                                default:
                                    {
                                        if (showForAllAvatars)
                                        {
                                            if (web4)
                                            {
                                                if (listAllWeb4Predicate != null)
                                                    await listAllWeb4Predicate(providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                            else
                                            {
                                                if (listAllPredicate != null)
                                                    await listAllPredicate(showAllVersions, showDetailed, 0, providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                        }
                                        else
                                        {
                                            if (web4)
                                            {
                                                if (listWeb4ForBeamedInAvatarPredicate != null)
                                                    await listWeb4ForBeamedInAvatarPredicate(providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                            else
                                            {
                                                if (listForBeamedInAvatarPredicate != null)
                                                    await listForBeamedInAvatarPredicate(showAllVersions, showDetailed, providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;

                    case "search":
                        {
                            if (web4)
                            {
                                if (showWeb4Predicate != null)
                                    await searchWeb4Predicate(subCommandParam3, showForAllAvatars, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else
                            {
                                if (searchPredicate != null)
                                    await searchPredicate(subCommandParam3, showAllVersions, showForAllAvatars, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(subCommandPlural))
                    subCommandPlural = $"{subCommand}'s";

                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Green;
                CLIEngine.ShowMessage($"{subCommand.ToUpper()} SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");

                int commandSpace = 22;
                int paramSpace = 22;
                string paramDivider = "  ";
                string web4Param = "";

                if (subCommand.ToUpper() == "NFT" || subCommand.ToUpper() == "GEO-NFT" || subCommand.ToUpper() == "NFT" || subCommand.ToUpper() == "GEO-NFT")
                    web4Param = "[web4]";

                if (showCreate)
                {
                    if (subCommand.ToUpper() == "GEONFT")
                    {
                        CLIEngine.ShowMessage(string.Concat("    mint".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Mints a WEB4 OASIS Geo-NFT and places in Our World for the currently beamed in avatar."), ConsoleColor.Green, false);
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR Geo-NFT by wrapping around a WEB4 OASIS Geo-NFT."), ConsoleColor.Green, false);
                    }

                    else if (subCommand.ToUpper() == "NFT")
                    {
                        CLIEngine.ShowMessage(string.Concat("    mint".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Mints a WEB4 OASIS NFT for the currently beamed in avatar."), ConsoleColor.Green, false);
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR NFT by wrapping around a WEB4 OASIS NFT."), ConsoleColor.Green, false);
                    }

                    else if (subCommand.ToUpper() == "NFT COLLECTION")
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name} [web4]".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR NFT by wrapping around a WEB4 OASIS NFT (see notes)."), ConsoleColor.Green, false);
                    //CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name} [web4]".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR NFT by wrapping around a WEB4 OASIS NFT. If [web4] is included it will create a WEB4 OASIS NFT"), ConsoleColor.Green, false);

                    else if (subCommand.ToUpper() == "GEO-NFT COLLECTION")
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name} [web4]".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR GEO-NFT by wrapping around a WEB4 OASIS GEO-NFT (see notes)."), ConsoleColor.Green, false);
                    //CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name} [web4]".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR GEO-NFT by wrapping around a WEB4 OASIS GEO-NFT. If [web4] is included it will create a WEB4 OASIS GEO-NFT"), ConsoleColor.Green, false);

                    else
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Create a ", subCommand, "."), ConsoleColor.Green, false);
                }

                if (showUpdate)
                    CLIEngine.ShowMessage(string.Concat("    update".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Update an existing ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);

                if (showDelete)
                    CLIEngine.ShowMessage(string.Concat("    delete".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Delete an existing ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "NFT" || subCommand.ToUpper() == "GEO-NFT")
                {
                    CLIEngine.ShowMessage(string.Concat("    burn".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Burn's a OASIS ", subCommand, " for the given {id} or {name}"), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    send".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Send a OASIS ", subCommand, " for the given {id} or {name} to another wallet cross-chain."), ConsoleColor.Green, false);

                    if (subCommand.ToUpper() == "NFT")
                        //CLIEngine.ShowMessage(string.Concat("    import".PadRight(commandSpace), "{id/name} [web3]".PadRight(paramSpace), paramDivider, "Imports a OASIS ", subCommand, " JSON file for the given {id} or {name}. If the [web3] parameter is included it will import an existing WEB3 NFT (JSON MetaData or NFT Token Address) and wrap it in a new WEB4 OASIS NFT."), ConsoleColor.Green, false);
                        CLIEngine.ShowMessage(string.Concat("    import".PadRight(commandSpace), "{id/name} [web3]".PadRight(paramSpace), paramDivider, "Imports a OASIS ", subCommand, " JSON file for the given {id} or {name}."), ConsoleColor.Green, false);
                    else
                        CLIEngine.ShowMessage(string.Concat("    import".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Imports a OASIS ", subCommand, " JSON file for the given {id} or {name}."), ConsoleColor.Green, false);

                    CLIEngine.ShowMessage(string.Concat("    export".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Exports a OASIS ", subCommand, " for the given {id} or {name} as a JSON file as well as a WEB3 JSON MetaData file."), ConsoleColor.Green, false);CLIEngine.ShowMessage(string.Concat("    burn".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Burn's a OASIS ", subCommand, " for the given {id} or {name}"), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    convert".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Allows the minting of different WEB3 NFT Standards for different chains from the same OASIS WEB4 Metadata."), ConsoleColor.Green, false);
                }

                if (subCommand.ToUpper() == "GEO-NFT")
                    CLIEngine.ShowMessage(string.Concat("    place".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Create a OASIS Geo-NFT from an existing OASIS NFT for the given {id} or {name} and place within Our World."), ConsoleColor.Green, false);

                CLIEngine.ShowMessage(string.Concat("    clone".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Clones a OASIS ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    adddependency".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Adds a runtime to the ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    removedependency".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Removes a runtime from the ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    download".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Download a ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    install".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Install/download a ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    uninstall".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Uninstall a ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                //CLIEngine.ShowMessage(string.Concat("    reinstall".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Reinstall a ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "OAPP" || subCommand.ToUpper() == "OAPPTEMPLATE" || subCommand.ToUpper() == "HAPP")
                {
                    if (subCommand.ToUpper() == "HAPP")
                        CLIEngine.ShowMessage(string.Concat("    publish".PadRight(commandSpace), ("{hAppPath} [publishDotNet]".PadRight(paramSpace)), paramDivider, "Publish a ", subCommand, " for the given {hAppPath}."), ConsoleColor.Green, false);
                    else
                        CLIEngine.ShowMessage(string.Concat("    publish".PadRight(commandSpace), "{oappPath} [publishDotNet]".PadRight(paramSpace), paramDivider, "Publish a ", subCommand, " for the given {oappPath}."), ConsoleColor.Green, false);
                }
                else
                    CLIEngine.ShowMessage(string.Concat("    publish".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Publish a ", subCommand, " to STARNET for the given {id} or {name}."), ConsoleColor.Green, false);

                CLIEngine.ShowMessage(string.Concat("    unpublish".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Unpublish a ", subCommand, " from STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    republish".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Republish a ", subCommand, " to STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    activate".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Activate (show) a ", subCommand, " on the STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    deactivate".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Deactivate (hide) a ", subCommand, " on the STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
                //CLIEngine.ShowMessage(string.Concat("    list".PadRight(commandSpace), string.Concat("[allVersions] [forAllAvatars] [detailed]", web4Param).PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " that have been created."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list".PadRight(commandSpace), string.Concat("", web4Param).PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " that have been created."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list installed".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " installed for the currently beamed in avatar."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list uninstalled".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " uninstalled for the currently beamed in avatar (allows reinstalling)."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list unpublished".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " unpublished for the currently beamed in avatar (allows republishing)."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list deactivated".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " deactivated for the currently beamed in avatar (allows reactivating)."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    show".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Shows the ", subCommandPlural, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    search".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Searches the ", subCommandPlural, " for the given search critera."), ConsoleColor.Green, false);
                
                if (subCommand.ToUpper() == "OAPP")
                    CLIEngine.ShowMessage(string.Concat("    template".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the OAPP Template Subcommand menu."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "CELESTIAL BODY")
                    CLIEngine.ShowMessage(string.Concat("    metadata".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the CelestialBody MetaData DNA Subcommand menu."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "ZOME")
                    CLIEngine.ShowMessage(string.Concat("    metadata".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the Zome MetaData DNA Subcommand menu."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "HOLON")
                    CLIEngine.ShowMessage(string.Concat("    metadata".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the Holon MetaData DNA Subcommand menu."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "NFT COLLECTION")
                {
                    CLIEngine.ShowMessage(string.Concat("    add".PadRight(commandSpace), "{id/name} {id/name}".PadRight(paramSpace), paramDivider, "Add's a WEB4 OASIS NFT to the collection."), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    remove".PadRight(commandSpace), "{id/name} {id/name}".PadRight(paramSpace), paramDivider, "Remove's a WEB4 OASIS NFT from the collection."), ConsoleColor.Green, false);
                }

                if (subCommand.ToUpper() == "GEONFT COLLECTION")
                {
                    CLIEngine.ShowMessage(string.Concat("    add".PadRight(commandSpace), "{id/name} {id/name}".PadRight(paramSpace), paramDivider, "Add's a WEB4 OASIS GEO-NFT to the collection."), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    remove".PadRight(commandSpace), "{id/name} {id/name}".PadRight(paramSpace), paramDivider, "Remove's a WEB4 OASIS GEO-NFT from the collection."), ConsoleColor.Green, false);
                }

                CLIEngine.ShowMessage($"NOTES:", ConsoleColor.Green);

                if (subCommand.ToUpper() == "OAPP")
                    CLIEngine.ShowMessage($"For the publish command, if the flag [publishDotNet] is specified it will first do a dotnet publish before publishing to STARNET.", ConsoleColor.Green);
                
                CLIEngine.ShowMessage($"For the list & search commands, if [allVersions] is omitted it will list the current version, otherwise it will list all versions. If [forAllAvatars] is omitted it will list only your {subCommandPlural}'s otherwise it will list all published {subCommandPlural}'s as well as yours.", ConsoleColor.Green);
                CLIEngine.ShowMessage($"For the list & show commands, if [detailed] is included it will list detailed stats also such as all dependenices installed.", ConsoleColor.Green);

                if (subCommand.ToUpper() == "GEO-NFT")
                    CLIEngine.ShowMessage($"For the update, delete, list, show or search command, if [web4] is included it will update/delete/list/show/search WEB4 OASIS Geo-NFT's, otherwise it will update/delete/list/show/search WEB5 STAR Geo-NFT's.", ConsoleColor.Green);

                if (subCommand.ToUpper() == "NFT")
                {
                    //If the[web3] parameter is included 
                    CLIEngine.ShowMessage($"For the import command if [web3] is included it will import an existing WEB3 NFT(JSON MetaData or NFT Token Address) and wrap it in a new WEB4 OASIS NFT.", ConsoleColor.Green);
                    CLIEngine.ShowMessage($"For the update, delete, list, show or search command, if [web4] is included it will update/delete/list/show/search WEB4 OASIS NFT's, otherwise it will update/delete/list/show/search WEB5 STAR NFT's.", ConsoleColor.Green);                    
                }

                if (subCommand.ToUpper() == "GEO-NFT COLLECTION")
                    CLIEngine.ShowMessage($"For the create, update, delete, list, show or search command, if [web4] is included it will create/update/delete/list/show/search WEB4 OASIS Geo-NFT Collection's, otherwise it will create/update/delete/list/show/search WEB5 STAR Geo-NFT Collection's.", ConsoleColor.Green);

                if (subCommand.ToUpper() == "NFT COLLECTION")
                    CLIEngine.ShowMessage($"For the create, update, delete, list, show or search command, if [web4] is included it will create/update/delete/list/show/search WEB4 OASIS NFT Collection's, otherwise it will create/update/delete/list/show/search WEB5 STAR NFT Collection's.", ConsoleColor.Green);


                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowAvatarSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                //Guid id = Guid.Empty;

                //if (inputArgs.Length > 2)
                //{
                //    if (!Guid.TryParse(inputArgs[2], out id))
                //        CLIEngine.ShowErrorMessage($"The id ({inputArgs[2]}) passed in is not a valid GUID!");
                //}

                switch (inputArgs[1].ToLower())
                {
                    case "beamin":
                        {
                            if (STAR.BeamedInAvatar == null)
                                await STARCLI.Avatars.BeamInAvatar();
                            else
                                CLIEngine.ShowErrorMessage($"Avatar {STAR.BeamedInAvatar.Username} Already Beamed In. Please Beam Out First!");
                        }
                        break;

                    case "beamout":
                        {
                            if (STAR.BeamedInAvatar != null)
                            {
                                OASISResult<IAvatar> avatarResult = await STAR.BeamedInAvatar.BeamOutAsync();

                                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                                {
                                    STAR.BeamedInAvatar = null;
                                    STAR.BeamedInAvatarDetail = null;
                                    CLIEngine.ShowSuccessMessage("Avatar Successfully Beamed Out! We Hope You Enjoyed Your Time In The OASIS! Please Come Again! :)");
                                }
                                else
                                    CLIEngine.ShowErrorMessage($"Error Beaming Out Avatar: {avatarResult.Message}");
                            }
                            else
                                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In!");
                        }
                        break;

                    case "whoisbeamedin":
                        {
                            if (STAR.BeamedInAvatar != null)
                                CLIEngine.ShowMessage($"Avatar {STAR.BeamedInAvatar.Username} Beamed In On {STAR.BeamedInAvatar.LastBeamedIn} And Last Beamed Out On {STAR.BeamedInAvatar.LastBeamedOut}. They Are Level {STAR.BeamedInAvatarDetail.Level} With {STAR.BeamedInAvatarDetail.Karma} Karma.", ConsoleColor.Green);
                            else
                                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In!");
                        }
                        break;

                    case "show":
                        {
                            if (inputArgs.Length > 2)
                            {
                                if (inputArgs[2] == "me")
                                    STARCLI.Avatars.ShowAvatar(STAR.BeamedInAvatar, STAR.BeamedInAvatarDetail);
                                else
                                    await STARCLI.Avatars.ShowAvatar(inputArgs[2]);
                            }
                            else
                                await STARCLI.Avatars.ShowAvatar();
                        }
                        break;


                    case "edit":
                        {
                            if (STAR.BeamedInAvatar != null)
                                CLIEngine.ShowMessage("Coming soon...");
                            else
                                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In!");
                        }
                        break;

                    case "list":
                        {
                            if (inputArgs.Length > 2 && inputArgs[2] == "detailed")
                                await STARCLI.Avatars.ListAvatarDetailsAsync();
                            else
                                await STARCLI.Avatars.ListAvatarsAsync();
                        }
                        break;

                    case "search":
                        {
                            await STARCLI.Avatars.SearchAvatarsAsync();
                        }
                        break;

                    case "forgotpassword":
                        {
                            await STARCLI.Avatars.ForgotPasswordAsync();
                        }
                        break;

                    case "resetpassword":
                        {
                            await STARCLI.Avatars.ResetPasswordAsync();
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"AVATAR SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    beamin                       Beam in (log in).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    beamout                      Beam out (log out).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    whoisbeamedin                Display who is currently beamed in (if any) and the last time they beamed in and out.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    show me                      Display the currently beamed in avatar details (if any).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    show          {id/username}  Shows the details for the avatar for the given {id} or {username}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    edit                         Edit the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    list          [detailed]     Lists all avatars. If [detailed] is included it will list detailed stats also.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    search                       Search avatars that match the given seach parameters.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    forgotpassword               Send a Forgot Password email to your email account containing a Reset Token.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    resetpassword                Allows you to reset your password using the Reset Token received in your email from the forgotpassword sub-command.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage($"NOTES:", ConsoleColor.Green);
                CLIEngine.ShowMessage($"For the search command, only public fields are returned such as level, karma, username & any fields the player has set to public.", ConsoleColor.Green);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowNftSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                //Guid id = Guid.Empty;

                //if (inputArgs.Length > 2)
                //{
                //    if (!Guid.TryParse(inputArgs[2], out id))
                //        CLIEngine.ShowErrorMessage($"The id ({inputArgs[2]}) passed in is not a valid GUID!");
                //}

                switch (inputArgs[1].ToLower())
                {
                    case "mint":
                    case "create":
                        await STARCLI.NFTs.CreateAsync(null);
                        break;

                    case "send":
                        await STARCLI.NFTs.SendNFTAsync();
                        break;

                    case "update":
                        {
                            await STARCLI.NFTs.UpdateAsync(inputArgs.Length > 2 ? inputArgs[2] : null);
                        }
                        break;

                    case "burn":
                        {
                            CLIEngine.ShowMessage("Coming soon...");
                        }
                        break;

                    case "publish":
                        {
                            await STARCLI.NFTs.PublishAsync();
                        }
                        break;

                    case "unpublish":
                        {
                            await STARCLI.NFTs.UnpublishAsync();
                        }
                        break;

                    case "show":
                        {
                            await STARCLI.NFTs.ListAllAsync();
                        }
                        break;

                    case "list":
                        {
                            if (inputArgs.Length > 2 && inputArgs[2] != null && inputArgs[2].ToLower() == "all")
                                await STARCLI.NFTs.ListAllAsync();
                            else
                                await STARCLI.NFTs.ListAllCreatedByBeamedInAvatarAsync();
                        }
                        break;

                    case "search":
                        {
                            CLIEngine.ShowMessage("Coming soon...");
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"NFT SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    mint/create           Mints a OASIS NFT for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    update     {id/name}  Updates a OASIS NFT for the given {id} or {name}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    burn                  Burn's a OASIS NFT for the given {id} or {name}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    send                  Send a OASIS NFT for the given {id} or {name} to another wallet cross-chain.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    publish    {id/name}  Publishes a OASIS NFT for the given {id} or {name} to the STARNET store so others can use in their own geo-nft's etc.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    unpublish  {id/name}  Unpublishes a OASIS NFT for the given {id} or {name} from the STARNET store.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    show       {id/name}  Shows the OASIS NFT for the given {id} or {name}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    list       [all]      List all OASIS NFT's that have been created. If the [all] flag is omitted it will list only your NFT's otherwise it will list all published NFT's as well as yours.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    search                Search for OASIS NFT's that match certain criteria and belong to the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowGeoNftSubCommandAsync(string[] inputArgs, ProviderType providerType)
        {
            string subCommand = "";
            string subCommandParam = "";
            string subCommandParam2 = "";
            string subCommandParam3 = "";
            string subCommandParam4 = "";
            bool showAllVersions = false;
            bool showForAllAvatars = false;
            bool showDetailed = false;

            if (inputArgs.Length > 1)
            {
                if (inputArgs.Length > 1 && !string.IsNullOrEmpty(inputArgs[1]))
                    subCommandParam = inputArgs[1].ToLower();

                if (inputArgs.Length > 2 && !string.IsNullOrEmpty(inputArgs[2]))
                    subCommandParam2 = inputArgs[2].ToLower();

                if (inputArgs.Length > 3 && !string.IsNullOrEmpty(inputArgs[3]))
                    subCommandParam3 = inputArgs[3].ToLower();

                if (inputArgs.Length > 4 && !string.IsNullOrEmpty(inputArgs[4]))
                    subCommandParam4 = inputArgs[4].ToLower();

                if (string.IsNullOrEmpty(subCommand))
                    subCommand = inputArgs[0];

                if (subCommandParam2.ToLower() == "allversions" || subCommandParam3.ToLower() == "allversions")
                    showAllVersions = true;

                if (subCommandParam2.ToLower() == "forallavatars" || subCommandParam3.ToLower() == "forallavatars")
                    showForAllAvatars = true;

                if (subCommandParam == "detailed" || subCommandParam2 == "detailed" || subCommandParam3 == "detailed")
                    showDetailed = true;

                switch (inputArgs[1].ToLower())
                {
                    case "mint":
                    case "create":
                        await STARCLI.GeoNFTs.CreateAsync(null);
                        break;

                    case "send":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "place":
                        await STARCLI.GeoNFTs.PublishAsync();
                        break;

                    case "update":
                        await STARCLI.GeoNFTs.UpdateAsync(providerType: providerType);
                        break;

                    case "burn":
                        {
                            CLIEngine.ShowMessage("Coming soon...");
                        }
                        break;

                    case "publish":
                        await STARCLI.GeoNFTs.PublishAsync(providerType: providerType);
                        break;

                    case "unpublish":
                        await STARCLI.GeoNFTs.UnpublishAsync(providerType: providerType);
                        break;

                    case "show":
                        await STARCLI.GeoNFTs.ShowAsync(providerType: providerType);
                        break;

                    case "list":
                        {
                            switch (subCommandParam2.ToLower())
                            {
                                case "installed":
                                    await STARCLI.GeoNFTs.ListAllInstalledForBeamedInAvatarAsync();
                                    break;

                                case "uninstalled":
                                    await STARCLI.GeoNFTs.ListAllUninstalledForBeamedInAvatarAsync();
                                    break;

                                case "unpublished":
                                    await STARCLI.GeoNFTs.ListAllUnpublishedForBeamedInAvatarAsync();
                                    break;

                                case "deactivated":
                                    await STARCLI.GeoNFTs.ListAllDeactivatedForBeamedInAvatarAsync();
                                    break;

                                default:
                                    {
                                        if (showForAllAvatars)
                                            await STARCLI.GeoNFTs.ListAllAsync(showAllVersions, showDetailed, 0, providerType);
                                        else
                                            await STARCLI.GeoNFTs.ListAllCreatedByBeamedInAvatarAsync(showAllVersions, showDetailed, providerType);
                                    }
                                    break;
                            }
                        }
                        break;

                    case "search":
                        await STARCLI.GeoNFTs.SearchAsync(providerType: providerType);
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"GEONFT SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    mint/create            Mints a OASIS Geo-NFT and places in Our World/AR World for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    update      {id/name}  Updates a OASIS Geo-NFT for the given {id} or {name}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    burn        {id/name}  Burn's a OASIS Geo-NFT for the given {id} or {name}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    send        {id/name}  Send a OASIS Geo-NFT for the given {id} or {name} to another wallet cross-chain.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    place       {id/name}  Create a OASIS Geo-NFT from an existing OASIS NFT for the given {id} or {name} and place within Our World.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    publish     {id/name}  Publishes a OASIS Geo-NFT for the given {id} or {name} to the STARNET store so others can use in their own geo-nft's etc.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    unpublish   {id/name}  Unpublishes a OASIS Geo-NFT for the given {id} or {name} from the STARNET store.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    show        {id/name}  Shows the OASIS Geo-NFT for the given {id} or {name}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    list        [all]      List all OASIS Geo-NFT's that have been created. If the [all] flag is omitted it will list only your Geo-NFT's otherwise it will list all published Geo-NFT's as well as yours.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    search                Search for OASIS Geo-NFT's that match certain criteria and belong to the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowKeysSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "link":
                        { 
                            STAR.OASISAPI.Keys.LinkProviderPublicKeyToAvatarById()
                        }
                        break;

                    case "list":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"KEYS SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    link                  Links a OASIS Provider Key to the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    list                  Shows the keys for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowKarmaSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "list":
                        STAR.OASISAPI.Avatars.ShowKarmaThresholds();
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"KARMA SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    list                  Display the karma thresholds.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowWalletSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "sendtoken":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "get":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "getDefault":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "setDefault":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "import":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "add":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "list":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "balance":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"WALLET SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    sendtoken          [walletAddress]  Sends a token to the given wallet address.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    get                [publickey]      Gets the wallet that the public key belongs to.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    getDefault                          Gets the default wallet for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    setDefault         [walletId]       Sets the default wallet for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import privateKey  [privatekey]     Imports a wallet using the privateKey.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import publicKey   [publickey]      Imports a wallet using the publicKey.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import secretPhase [secretPhase]    Imports a wallet using the secretPhase.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import json        [jsonFile]       Imports a wallet using the jsonFile.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    add                                 Adds a wallet for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    list                                Lists the wallets for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    balance                             Gets the total balance for all wallets for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    balance            [walletId]       Gets the balance for the given wallet for the currently beamed in avatar.", ConsoleColor.Green, false);

                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowMapSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "setprovider":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "draw3dobject":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "draw2dsprite":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "draw2dspriteonhud":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "placeHolon":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "placeBuilding":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "placeQuest":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "placeGeoNFT":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "placeGeoHotSpot":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "placeOAPP":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "pamLeft":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "pamRight":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "pamUp":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "pamDown":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "zoomOut":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "zoomIn":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "zoomToHolon":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "zoomToBuilding":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "zoomToQuest":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "zoomToGeoNFT":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "zoomToGeoHotSpot":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "zoomToOAPP":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "zoomToCoOrds":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "drawRouteOnMap":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "drawRouteOnMapBetweenHolons":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "drawRouteOnMapBetweenBuildings":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "drawRouteOnMapBetweenQuests":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "drawRouteOnMapBetweenGeoNFTs":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "drawRouteOnMapBetweenGeoHotSpots":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "drawRouteOnMapBetweenOAPPs":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"MAP SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    map setprovider                      {mapProviderType}                                 Sets the currently {mapProviderType}.");
                Console.WriteLine("    map draw3dobject                     {3dObjectPath} {x} {y}                            Draws a 3D object on the map at {x/y} co-ordinates for the given file {3dobjectPath}.");
                Console.WriteLine("    map draw2dsprite                     {2dSpritePath} {x} {y}                            Draws a 2d sprite on the map at {x/y} co-ordinates for the given file {2dSpritePath}.");
                Console.WriteLine("    map draw2dspriteonhud                {2dSpritePath}                                    Draws a 2d sprite on the HUD for the given file {2dSpritePath}.");
                Console.WriteLine("    map placeHolon                       {Holon id/name} {x} {y}                           Place the holon on the map.");
                Console.WriteLine("    map placeBuilding                    {Building id/name} {x} {y}                        Place the building on the map.");
                Console.WriteLine("    map placeQuest                       {Quest id/name} {x} {y}                           Place the Quest on the map.");
                Console.WriteLine("    map placeGeoNFT                      {GeoNFT id/name} {x} {y}                          Place the GeoNFT on the map.");
                Console.WriteLine("    map placeGeoHotSpot                  {GeoHotSpot id/name} {x} {y}                      Place the GeoHotSpot on the map.");
                Console.WriteLine("    map placeOAPP                        {OAPP id/name} {x} {y}                            Place the OAPP on the map.");
                Console.WriteLine("    map pamLeft                                                                            Pam the map left.");
                Console.WriteLine("    map pamRight                                                                           Pam the map right.");
                Console.WriteLine("    map pamUp                                                                              Pam the map left.");
                Console.WriteLine("    map pamDown                                                                            Pam the map down.");
                Console.WriteLine("    map zoomOut                                                                            Zoom the map out.");
                Console.WriteLine("    map zoomIn                                                                             Zoom the map in.");
                Console.WriteLine("    map zoomToHolon                       {GeoNFT id/name}                                 Zoom the map to the location of the given holon.");
                Console.WriteLine("    map zoomToBuilding                    {GeoNFT id/name}                                 Zoom the map to the location of the given building.");
                Console.WriteLine("    map zoomToQuest                       {GeoNFT id/name}                                 Zoom the map to the location of the given quest.");
                Console.WriteLine("    map zoomToGeoNFT                      {GeoNFT id/name}                                 Zoom the map to the location of the given GeoNFT.");
                Console.WriteLine("    map zoomToGeoHotSpot                  {GeoHotSpot id/name}                             Zoom the map to the location of the given GeoHotSpot.");
                Console.WriteLine("    map zoomToOAPP                        {OAPP id/name}                                   Zoom the map to the location of the given OAPP.");
                Console.WriteLine("    map zoomToCoOrds                      {x} {y}                                          Zoom the map to the location of the given {x} and {y} coordinates.");
                //Console.WriteLine("    map selectBuildingOnMap             {building id}                                    Selects the given building on the map.");
                //Console.WriteLine("    map highlightBuildingOnMap          {building id}                                    Highlight the given building on the map.");
                Console.WriteLine("    map drawRouteOnMap                    {startX} {startY} {endX} {endY}                  Draw a route on the map.");
                Console.WriteLine("    map drawRouteOnMapBetweenHolons       {fromHolon id/name} {toHolon id/name}            Draw a route on the map between the two holons.");
                Console.WriteLine("    map drawRouteOnMapBetweenBuildings    {fromBuilding id/name} {toBuilding id/name}      Draw a route on the map between the two buildings.");
                Console.WriteLine("    map drawRouteOnMapBetweenQuests       {fromQuest id/name} {toQuest id/name}            Draw a route on the map between the two quests.");
                Console.WriteLine("    map drawRouteOnMapBetweenGeoNFTs      {fromGeoNFT id/name} {ToGeoNFT id/name}          Draw a route on the map between the two GeoNFTs.");
                Console.WriteLine("    map drawRouteOnMapBetweenGeoHotSpots  {fromGeoHotSpot id/name} {ToGeoHotSpot id/name}  Draw a route on the map between the two GeoHotSpots.");
                Console.WriteLine("    map drawRouteOnMapBetweenOAPPs        {fromOAPP id/name} {ToOAPP id/name}              Draw a route on the map between the two OAPPs.");

                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowDataSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "save":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "load":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "delete":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "list":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"DATA SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    data save    {key} {value}  Saves data for the given {key} and {value} to the currently beamed in avatar.");
                Console.WriteLine("    data load    {key}          Loads data for the given {key} for the currently beamed in avatar.");
                Console.WriteLine("    data delete  {key}          Deletes data for the given {key} for the currently beamed in avatar.");
                Console.WriteLine("    data list                   Lists all data for the currently beamed in avatar.");
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowSeedsSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "balance":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "organisations":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "organisation":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "pay":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "donate":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "reward":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "invite":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "accept":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "qrcode":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"SEEDS SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    seeds balance        {telosAccountName/avatarId}  Get's the balance of your SEEDS account.");
                Console.WriteLine("    seeds organisations                               Get's a list of all the SEEDS organisations.");
                Console.WriteLine("    seeds organisation   {organisationName}           Get's a organisation for the given {organisationName}.");
                Console.WriteLine("    seeds pay            {telosAccountName/avatarId}  Pay using SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                Console.WriteLine("    seeds donate         {telosAccountName/avatarId}  Donate using SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                Console.WriteLine("    seeds reward         {telosAccountName/avatarId}  Reward using SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                Console.WriteLine("    seeds invite         {telosAccountName/avatarId}  Send invite to join SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                Console.WriteLine("    seeds accept         {telosAccountName/avatarId}  Accept the invite to join SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                Console.WriteLine("    seeds qrcode         {telosAccountName/avatarId}  Generate a sign-in QR code using either your {telosAccountName} or {avatarId}.");

                //CLIEngine.ShowMessage("    balance        {telosAccountName/avatarId}  Get's the balance of your SEEDS account.", ConsoleColor.Green, false);
                //CLIEngine.ShowMessage("    organisations                               Get's a list of all the SEEDS organisations.", ConsoleColor.Green, false);
                //CLIEngine.ShowMessage("    organisation   {organisationName}           Get's a list of all the SEEDS organisations.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowOlandSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "price":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "purchase":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "load":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "save":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "delete":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    case "list":
                        CLIEngine.ShowMessage("Coming soon...");
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"OLAND SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    price                  Get the currently OLAND price.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    purchase               Purchase OLAND for Our World/OASIS.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    load      {id}         Load a OLAND for the given {id}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    save      {id}         Save a OLAND for the given {id}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    delete    {id}         Delete a OLAND for the given {id}.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    list      {all}        If [all] is omitted it will list all OLAND for the given beamed in avatar, otherwise it will list all OLAND for all avatars.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowConfigSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "cosmicdetailedoutput":
                        { 
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "enabled":
                                        {
                                            STAR.IsDetailedCOSMICOutputsEnabled = true;
                                            CLIEngine.ShowMessage("Detailed COSMIC Output Enabled.");
                                        }
                                        break;

                                    case "disabled":
                                        {
                                            STAR.IsDetailedCOSMICOutputsEnabled = false;
                                            CLIEngine.ShowMessage("Detailed COSMIC Output Disabled.");
                                        }
                                        break;

                                    case "status":
                                        {
                                            if (STAR.IsDetailedCOSMICOutputsEnabled)
                                                CLIEngine.ShowSuccessMessage("COSMIC Detailed Output Status: Enabled.");
                                            else
                                                CLIEngine.ShowSuccessMessage("COSMIC Detailed Output Status: Disabled.");
                                        }
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown.");
                                        break;
                                }
                            }
                            else
                            {
                                if (STAR.IsDetailedCOSMICOutputsEnabled)
                                    CLIEngine.ShowSuccessMessage("COSMIC Detailed Output Status: Enabled.");
                                else
                                    CLIEngine.ShowSuccessMessage("COSMIC Detailed Output Status: Disabled.");
                            }
                        }
                        break;

                    case "starstatusdetailedoutput":
                        {
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "enabled":
                                        {
                                            STAR.IsDetailedCOSMICOutputsEnabled = true;
                                            CLIEngine.ShowSuccessMessage("STAR Detailed Status Enabled.");
                                        }
                                        break;

                                    case "disabled":
                                        {
                                            STAR.IsDetailedCOSMICOutputsEnabled = false;
                                            CLIEngine.ShowSuccessMessage("STAR Detailed Status Disabled.");
                                        }
                                        break;

                                    case "status":
                                        {
                                            if (STAR.IsDetailedCOSMICOutputsEnabled)
                                                CLIEngine.ShowMessage("STAR Detailed Status: Enabled.");
                                            else
                                                CLIEngine.ShowMessage("STAR Detailed Status: Disabled.");
                                        }
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown.");
                                        break;
                                }
                            }
                            else
                            {
                                if (STAR.IsDetailedCOSMICOutputsEnabled)
                                    CLIEngine.ShowMessage("STAR Detailed Status: Enabled.");
                                else
                                    CLIEngine.ShowMessage("STAR Detailed Status: Disabled.");
                            }
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"CONFIG SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    cosmicdetailedoutput     [enable/disable/status] Enables/disables COSMIC Detailed Output.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    starstatusdetailedoutput [enable/disable/status] Enables/disables STAR ODK Detailed Output.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowONODEConfigSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "start":
                        {
                            await StartONODEAsync();
                        }
                        break;

                    case "stop":
                        {
                            await StopONODEAsync();
                        }
                        break;

                    case "status":
                        {
                            await ShowONODEStatusAsync();
                        }
                        break;

                    case "config":
                        {
                            await OpenONODEConfigAsync();
                        }
                        break;

                    case "providers":
                        {
                            await ShowONODEProvidersAsync();
                        }
                        break;

                    case "startprovider":
                        {
                            if (inputArgs.Length > 2)
                                await StartONODEProviderAsync(inputArgs[2]);
                            else
                                CLIEngine.ShowErrorMessage("Please specify provider name: startprovider {ProviderName}");
                        }
                        break;

                    case "stopprovider":
                        {
                            if (inputArgs.Length > 2)
                                await StopONODEProviderAsync(inputArgs[2]);
                            else
                                CLIEngine.ShowErrorMessage("Please specify provider name: stopprovider {ProviderName}");
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"ONODE SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    start                          Starts a OASIS Node (ONODE) and registers it on the OASIS Network (ONET).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    stop                           Stops a OASIS Node (ONODE).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    status                         Shows stats for this ONODE.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    config                         Opens the ONODE's OASISDNA to allow changes to be made (you will need to stop and start the ONODE for changes to apply).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    providers                      Shows what OASIS Providers are running for this ONODE.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    startprovider  {ProviderName}  Starts a given provider.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    stopprovider   {ProviderName}  Stops a given provider.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowHyperNETSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "start":
                        {
                            CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "stop":
                        {
                            CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "status":
                        {
                            CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "config":
                        {
                            CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"HOLONET P2P HYPERNET SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    start   Starts the HoloNET P2P HyperNET Service.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    stop    Stops the HoloNET P2P HyperNET Service.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    status  Shows stats for the HoloNET P2P HyperNET Service.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    config  Opens the HyperNET's DNA to allow changes to be made (you will need to stop and start the HyperNET Service for changes to apply).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowONETSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "status":
                        {
                            await ShowONETStatusAsync();
                        }
                        break;

                    case "providers":
                        {
                            await ShowONETProvidersAsync();
                        }
                        break;

                    case "discover":
                        {
                            await DiscoverONETNodesAsync();
                        }
                        break;

                    case "connect":
                        {
                            if (inputArgs.Length > 2)
                                await ConnectToONETNodeAsync(inputArgs[2]);
                            else
                                CLIEngine.ShowErrorMessage("Please specify node address: connect {NodeAddress}");
                        }
                        break;

                    case "disconnect":
                        {
                            if (inputArgs.Length > 2)
                                await DisconnectFromONETNodeAsync(inputArgs[2]);
                            else
                                CLIEngine.ShowErrorMessage("Please specify node address: disconnect {NodeAddress}");
                        }
                        break;

                    case "topology":
                        {
                            await ShowONETTopologyAsync();
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"ONET SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    status      Shows stats for the OASIS Network (ONET).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    providers   Shows what OASIS Providers are running across the ONET and on what ONODE's.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    discover    Discovers available ONET nodes in the network.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    connect     Connects to a specific ONET node.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    disconnect  Disconnects from a specific ONET node.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    topology    Shows the ONET network topology and connections.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        //TODO: Not sure what this is used for?! :)
        private static void EnableOrDisableAutoProviderList(Func<bool, List<ProviderType>, bool> funct, bool isEnabled, List<ProviderType> providerTypes, string workingMessage, string successMessage, string errorMessage)
        {
            CLIEngine.ShowWorkingMessage(workingMessage);

            if (funct(isEnabled, providerTypes))
                CLIEngine.ShowSuccessMessage(successMessage);
            else
                CLIEngine.ShowErrorMessage(errorMessage);
        }

        private static void ShowHeader()
        {
            // Console.SetWindowSize(300, Console.WindowHeight);
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("*************************************************************************************************");
            Console.Write(" NextGen Software");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" STAR");
            Console.ForegroundColor = ConsoleColor.Green;
            //Console.Write($" (Synergiser Transformer Aggregator Resolver) HDK/ODK TEST HARNESS v{versionString} ");

            if (RandomNumberGenerator.GetInt32(1) == 0)
                Console.Write($" (Synergiser Transformer Aggregator Resolver) HDK/ODK {OASISBootLoader.OASISBootLoader.STARODKVersion} ");
            else
                Console.Write($" (Super Technogically Advanced Reality-Engine) HDK/ODK {OASISBootLoader.OASISBootLoader.STARODKVersion} ");

            Console.WriteLine("");
            Console.WriteLine("*************************************************************************************************");
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
            Console.Write(" ♥❤️❤ 💓");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" Of The OASIS)");
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private static void ShowCommands(bool showFullCommands = false)
        {
            //Table table = new Table("one", "two", "three");
            //int CommandColSize = 48;
            //int argsColSize = 42;
            //int indentSize = 4;

            


            ////var table = new ConsoleTable("one", "two", "three");
            //table.AddRow("ssdsd", "dfdfdf d fdfd d fd fd fd fd fd fd fd fd fdf d f", "dfdf df dfdfd fd dfd fdfdfdf dfd df df d dfd fdfd d fd fd fd fd fd fd fd fd fdf d 111122 2222 33333333333 44444 55555555 666666666666 677 ");
            //table.AddRow("2ssdsd", "2dfdfdf d fdfd d fd fd fd fd fd fd fd fd fdf d f", "2dfdf df dfdfd fd dfd fdfdfdf dfd df df d dfd fdfd d fd fd fd fd fd fd fd fd fdf d ");
            //table.AddRow("3ssdsd", "3dfdfdf d fdfd d fd fd fd fd fd fd fd fd fdf d f", "3dfdf df dfdfd fd dfd fdfdfdf dfd df df d dfd fdfd d fd fd fd fd fd fd fd fd fdf d ");
            //table.AddRow("4ssdsd", "4dfdfdf d fdfd d fd fd fd fd fd fd fd fd fdf d f", "4dfdf df dfdfd fd dfd fdfdfdf dfd df df d dfd fdfd d fd fd fd fd fd fd fd fd fdf d ");
            //table.AddRow("5sdsd", "5fdfdf d fdfd d fd fd fd fd fd fd fd fd fdf d f", "d5fdf df dfdfd fd dfd fdfdfdf dfd df df d dfd fdfd d fd fd fd fd fd fd fd fd fdf d ");
            //table.AddRow("6ssdsd", "6dfdfdf d fdfd d fd fd fd fd fd fd fd fd fdf d f", "6dfdf df dfdfd fd dfd fdfdfdf dfd df df d dfd fdfd d fd fd fd fd fd fd fd fd fdf d ");
            //table.AddRow("7sdsd", "7dfdfdf d fdfd d fd fd fd fd fd fd fd fd fdf d f", "7dfdf df dfdfd fd dfd fdfdfdf dfd df df d dfd fdfd d fd fd fd fd fd fd fd fd fdf d ");
            ////table.Write(Format.Default);
            ////table.Write(Format.Alternative);
            ////table.Write(Format.MarkDown);
            ////table.Write(Format.Minimal);

            //Console.Write(table.ToString());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n USAGE:");
            Console.WriteLine("    star {SUBCOMMAND}");
            Console.WriteLine("");
            Console.WriteLine(" FLAGS:");
            Console.WriteLine("    ignite           Ignite STAR & Boot The OASIS");
            Console.WriteLine("    extinguish       Extinguish STAR & Shutdown The OASIS");
            //Console.WriteLine("    beamin         Log in");
            //Console.WriteLine("    beamout        Log out");
            Console.WriteLine("    help [full]      Show this help page. If the [full] flag is omitted it will show only the top level sub-commands, if [full] is included it will show every option for each sub-command.");
            Console.WriteLine("    version          Show the versions of STAR ODK, COSMIC ORM, OASIS Runtime & the OASIS Providers..");
            Console.WriteLine("    status           Show the status of STAR ODK.");
            Console.WriteLine("    exit             Exit the STAR CLI.");
            Console.WriteLine("");
            Console.WriteLine(" SUBCOMMANDS:");

            
            if (showFullCommands)
            {
                DisplayCommand("light", "{OAPPName} {OAPPDesc} {OAPPType}", "Creates a new OAPP (Zomes/Holons/Star/Planet/Moon) at the given genesis folder location, from the given OAPP DNA.");
                DisplayCommand("", "{dnaFolder} {geneisFolder}", "");
                DisplayCommand("", "{genesisNameSpace} {genesisType}", "");
                DisplayCommand("", "{parentCelestialBodyId}", "");
                DisplayCommand("light", "", "Displays more detail on how to use this command and optionally launches the Light Wizard.");
                DisplayCommand("light wiz", "", "Start the Light Wizard.");
                DisplayCommand("light transmute", "{hAppDNA} {geneisFolder}", "Creates a new Planet (OApp) at the given folder genesis locations, from the given hApp DNA.");
                DisplayCommand("bang", "", "Generate a whole metaverse or part of one such as Multierveres, Universes, Dimensions, Galaxy Clusters, Galaxies, Solar Systems, Stars, Planets, Moons etc.");
                DisplayCommand("wiz", "", "Start the STAR ODK Wizard which will walk you through the steps for creating a OAPP tailored to your specefic needs (such as which OASIS Providers do you need and the specefic use case(s) you need etc).");
                DisplayCommand("flare", "", "Build a OAPP for the given {id} or {name}.");
                DisplayCommand("shine", "", "Launch & activate a OAPP for the given {id} or {name} by shining the 's light upon it...");
                DisplayCommand("twinkle", "", "Activate a published OAPP for the given {id} or {name} within the STARNET store.");
                DisplayCommand("dim", "", "Deactivate a published OAPP for the given {id} or {name} within the STARNET store.");
                DisplayCommand("seed", "", "Deploy/Publish a OAPP for the given {id} or {name} to the STARNET Store.");
                DisplayCommand("unseed", "", "Undeploy/Unpublish a OAPP for the given {id} or {name} from the STARNET Store.");
                DisplayCommand("reseed", "", "Redeploy/Republish a OAPP for the given {id} or {name} to the STARNET Store.");
                DisplayCommand("dust", "", "Delete a OAPP for the given {id} or {name} (this will also remove it from STARNET if it has already been published).");
                DisplayCommand("radiate", "", "Highlight the OAPP for the given {id} or {name} in the STARNET Store. *Admin/Wizards Only*");
                DisplayCommand("emit", "{id/name}", "Show how much light the OAPP is emitting into the solar system for the given {id} or {name} (this is determined by the collective karma score of all users of that OAPP).");
                DisplayCommand("reflect", "{id/name}", "Show stats of the OAPP for the given {id} or {name}.");
                DisplayCommand("evolve", "{id/name}", "Upgrade/update a OAPP for the given {id} or {name}.");
                DisplayCommand("mutate", "{id/name}", "Import/Export hApp, dApp & others for the given {id} or {name}.");
                DisplayCommand("love", "{id/username}", "Send/Receive Love for the given {id} or {username}.");
                DisplayCommand("burst", "", "View network stats/management/settings.");
                DisplayCommand("super", "", "Reserved For Future Use...");
                DisplayCommand("net", "", "Launch the STARNET Library/Store where you can list, search, update, publish, unpublish, install & uninstall OAPP's & more!");
                DisplayCommand("gate", "", "Opens the STARGATE to the OASIS Portal!");
                DisplayCommand("api", "[oasis]", "Opens the WEB5 STAR API (if oasis is included then it will open the WEB4 OASIS API instead).");
                DisplayCommand("avatar beamin", "", "Beam in (log in).");
                DisplayCommand("avatar beamout", "", "Beam out (Log out).");
                DisplayCommand("avatar whoisbeamedin", "", "Display who is currently beamed in (if any) and the last time they beamed in and out.");
                DisplayCommand("avatar show me", "", "Display the currently beamed in avatar details (if any).");
                DisplayCommand("avatar show", "{id/username}", "Shows the details for the avatar for the given {id} or {username}.");
                DisplayCommand("avatar edit", "", "Edit the currently beamed in avatar.");
                DisplayCommand("avatar list", "[detailed]", "Lists all avatars. If [detailed] is included it will list detailed stats also.");
                DisplayCommand("avatar search", "", "Search avatars that match the given search parameters (public fields only such as level, karma, username & any fields the player has set to public).");
                DisplayCommand("avatar forgotpassword", "", "Send a Forgot Password email to your email account containing a Reset Token.");
                DisplayCommand("avatar resetpassword", "", "Allows you to reset your password using the Reset Token received in your email from the forgotpassword sub-command.");
                DisplayCommand("karma list", "", "Display the karma thresholds.");
                DisplayCommand("keys link privateKey", "[walletId] [privateKey]", "Links a private key to the given wallet for the currently beamed in avatar.");
                DisplayCommand("keys link publicKey", "[walletId] [publicKey]", "Links a public key to the given wallet for the currently beamed in avatar.");
                DisplayCommand("keys link genKeyPair", "[walletId]", "Generates a unique keyvalue pair and then links them to to the given wallet for the currently beamed in avatar.");
                DisplayCommand("keys generateKeyPair", "", "Generates a unique keyvalue pair.");
                DisplayCommand("keys clearCache", "", "Clears the cache.");
                DisplayCommand("keys get provideruniquestoragekey", "{providerType}", "Gets the Provider Unique Storage Key for the given provider and the currently beamed in avatar.");
                DisplayCommand("keys get providerpublickeys", "{providerType}", "Gets the Provider Public Keys for the given provider and the currently beamed in avatar.");
                DisplayCommand("keys get avataridforprovideruniquestoragekey", "{avatarId}", "Gets the Provider Private Keys for the given provider and the currently beamed in avatar.");
                DisplayCommand("keys list", "", "Shows the keys for the currently beamed in avatar.");
                DisplayCommand("wallet sendtoken", "{walletAddress} {token} {amount}", "Sends a token to the given wallet address.");
                DisplayCommand("wallet transfer", "{from walletId/name} {amount} {to walletId/name}", "Transfers the given [amount] from one wallet to another for the currently beamed in avatar.");
                DisplayCommand("wallet get", "{publickey}", "Gets the wallet that the public key belongs to.");
                DisplayCommand("wallet getDefault", "", "Gets the default wallet for the currently beamed in avatar.");
                DisplayCommand("wallet setDefault", "{walletId}", "Sets the default wallet for the currently beamed in avatar.");
                DisplayCommand("wallet import privateKey", "{privateKey}", "Imports a wallet using the privateKey.");
                DisplayCommand("wallet import publicKey", "{publicKey}", "Imports a wallet using the publicKey.");
                DisplayCommand("wallet import secretPhase", "{secretPhase}", "Imports a wallet using the secretPhase.");
                DisplayCommand("wallet import json", "{jsonFile}", "Imports a wallet using the jsonFile.");
                DisplayCommand("wallet add", "", "Adds a wallet for the currently beamed in avatar.");
                DisplayCommand("wallet list", "", "Lists the wallets for the currently beamed in avatar.");
                DisplayCommand("wallet balance", "{walletId}", "Gets the balance for the given wallet for the currently beamed in avatar.");
                DisplayCommand("wallet balance", "", "Gets the total balance for all wallets for the currently beamed in avatar.");
                DisplayCommand("search", "", "Searches The OASIS for the given search parameters.");
                DisplaySTARNETHolonCommands("oapp", createDesc: "Shortcut to the light sub-command.", publishDesc: "Shortcut to the seed sub-command.", unpublishDesc: "Shortcut to the un-seed sub-command.", republishDesc: "Shortcut to the re-seed sub-command.");
                DisplaySTARNETHolonCommands("oapp template");
                DisplaySTARNETHolonCommands("runtime");
                DisplaySTARNETHolonCommands("lib");
                DisplaySTARNETHolonCommands("celestialspace");
                DisplaySTARNETHolonCommands("celestialbody");
                DisplaySTARNETHolonCommands("zome");
                DisplaySTARNETHolonCommands("holon");
                DisplaySTARNETHolonCommands("chapter");
                DisplaySTARNETHolonCommands("mission");
                DisplaySTARNETHolonCommands("quest");
                DisplayCommand("nft mint", "{id/name}", "Mints a WEB4 OASIS NFT for the currently beamed in avatar. Also allows minting more WEB3 NFT's from an existing WEB4 OASIS NFT.");
                DisplayCommand("nft burn", "{id/name}", "Burn's a nft for the given {id} or {name}.");
                DisplayCommand("nft send", "{id/name}", "Send a NFT for the given {id} or {name} to another wallet cross-chain.");
                DisplayCommand("nft import", "{id/name} [web3]", "Imports a WEB4 OASIS NFT JSON file. If [web3] param is given it will either import a WEB3 NFT JSON MetaData file and mint a WEB3 NFT and then wrap in a WEB4 OASIS NFT or use an existing WEB3 NFT Token Address to be wrapped in a WEB4 OASIS NFT.");
                DisplayCommand("nft export", "{id/name}", "Exports a WEB4 OASIS NFT as a JSON file as well as a WEB3 JSON MetaData file.");
                DisplayCommand("nft convert", "{id/name}", "Allows the minting of different WEB3 NFT Standards for different chains from the same OASIS WEB4 Metadata.");
                DisplaySTARNETHolonCommands("nft");
                DisplayCommand("nft collection add", "{colid/colname} {nftid/nftname}", "Adds a nft to the nft collection.");
                DisplayCommand("nft collection remove", "{colid/colname} {nftid/nftname}", "Remove's a nft from the nft collection.");
                //DisplaySTARNETHolonCommands("nft collection", showParams: "{id/name} [detailed] [web4]", showDesc: "Shows a nft collection for the given {id} or {name}. If [web4] is included it will show a WEB4 OASIS NFT Collection, otherwise it will show a WEB5 STAR NFT Collection.", listParams: "[allVersions] [forAllAvatars] [detailed] [web4]", listDesc: "List all that have been generated.", searchParams: "", searchDesc: "");
                //DisplaySTARNETHolonCommands("nft collection", showParams: "{id/name} [detailed] [web4]", listParams: "[allVersions] [forAllAvatars] [detailed] [web4]", searchParams: "[allVersions] [forAllAvatars] [web4]");
                DisplaySTARNETHolonCommands("nft collection");
                DisplayCommand("geonft mint", "{id/name}", "Mints a OASIS GeoNFT and places in Our World for the currently beamed in avatar. Also allows minting more WEB3 NFT's from an existing WEB4 OASIS GeoNFT.");
                DisplayCommand("geonft burn", "{id/name}", "Burn's a GeoNFT for the given {id} or {name}.");
                DisplayCommand("geonft place", "{id/name}", "Places an existing OASIS NFT for the given {id} or {name} in Our World for the currently beamed in avatar.");
                DisplayCommand("geonft send", "{id/name}", "Send a GeoNFT for the given {id} or {name} to another wallet cross-chain.");
                DisplayCommand("geonft import", "{id/name}", "Imports a WEB4 OASIS GeoNFT JSON file.");
                DisplayCommand("geonft export", "{id/name}", "Exports a WEB4 OASIS GeoNFT as a JSON file as well as a WEB3 JSON MetaData file.");
                DisplaySTARNETHolonCommands("geonft");
                DisplayCommand("geonft collection add", "{colid/colname} {nftid/nftname}", "Adds a geo-nft to the geo-nft collection.");
                DisplayCommand("geonft collection remove", "{colid/colname} {nftid/nftname}", "Remove's a geo-nft from the geo-nft collection.");
                //DisplaySTARNETHolonCommands("geonft collection", showParams: "{id/name} [detailed] [web4]", listParams: "[allVersions] [forAllAvatars] [detailed] [web4]", searchParams: "[allVersions] [forAllAvatars] [web4]");
                DisplaySTARNETHolonCommands("geonft collection");
                DisplaySTARNETHolonCommands("geohotspot");
                DisplaySTARNETHolonCommands("inventoryitem");
                DisplaySTARNETHolonCommands("plugin");

                //SEEDS Commands
                DisplayCommand("seeds balance", "{telosAccountName/avatarId}", "Get's the balance of your SEEDS account.");
                DisplayCommand("seeds organisations", "", "Get's a list of all the SEEDS organisations.");
                DisplayCommand("seeds organisation", "{organisationName}", "Get's a organisation for the given {organisationName}.");
                DisplayCommand("seeds pay", "{telosAccountName/avatarId}", "Pay using SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                DisplayCommand("seeds donate", "{telosAccountName/avatarId}", "Donate using SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                DisplayCommand("seeds reward", "{telosAccountName/avatarId}", "Reward using SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                DisplayCommand("seeds invite", "{telosAccountName/avatarId}", "Send invite to join SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                DisplayCommand("seeds accept", "{telosAccountName/avatarId}", "Accept the invite to join SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                DisplayCommand("seeds qrcode", "{telosAccountName/avatarId}", "Generate a sign-in QR code using either your {telosAccountName} or {avatarId}.");

                //Data Commands
                DisplayCommand("data save", "{key} {value}", "Saves data for the given {key} and {value} to the currently beamed in avatar.");
                DisplayCommand("data load", "{key}", "Loads data for the given {key} for the currently beamed in avatar.");
                DisplayCommand("data delete", "{key}", "Deletes data for the given {key} for the currently beamed in avatar.");
                DisplayCommand("data list", "", "Lists all data for the currently beamed in avatar.");

                //Map Commands
                DisplayCommand("map setprovider", "{mapProviderType}", "Sets the currently {mapProviderType}.");
                DisplayCommand("map draw3dobject", "{3dObjectPath} {x} {y}", "Draws a 3D object on the map at {x/y} co-ordinates for the given file {3dobjectPath}.");
                DisplayCommand("map draw2dsprite", "{2dSpritePath} {x} {y}", "Draws a 2d sprite on the map at {x/y} co-ordinates for the given file {2dSpritePath}.");
                DisplayCommand("map draw2dspriteonhud", "{2dSpritePath}", "Draws a 2d sprite on the HUD for the given file {2dSpritePath}.");
                DisplayCommand("map placeHolon", "{Holon id/name} {x} {y}", "Place the holon on the map.");
                DisplayCommand("map placeBuilding", "{Building id/name} {x} {y}", "Place the building on the map.");
                DisplayCommand("map placeQuest", "{Quest id/name} {x} {y}", "Place the Quest on the map.");
                DisplayCommand("map placeGeoNFT", "{GeoNFT id/name} {x} {y}", "Place the GeoNFT on the map.");
                DisplayCommand("map placeGeoHotSpot", "{GeoHotSpot id/name} {x} {y}", "Place the GeoHotSpot on the map.");
                DisplayCommand("map placeOAPP", "{OAPP id/name} {x} {y}", "Place the OAPP on the map.");
                DisplayCommand("map pamLeft", "", "Pam the map left.");
                DisplayCommand("map pamRight", "", "Pam the map right.");
                DisplayCommand("map pamUp", "", "Pam the map up.");
                DisplayCommand("map pamDown", "", "Pam the map down.");
                DisplayCommand("map zoomOut", "", "Zoom the map out.");
                DisplayCommand("map zoomIn", "", "Zoom the map in.");
                DisplayCommand("map zoomToHolon", "{GeoNFT id/name}", "Zoom the map to the location of the given holon.");
                DisplayCommand("map zoomToBuilding", "{GeoNFT id/name}", "Zoom the map to the location of the given building.");
                DisplayCommand("map zoomToQuest", "{GeoNFT id/name}", "Zoom the map to the location of the given quest.");
                DisplayCommand("map zoomToGeoNFT", "{GeoNFT id/name}", "Zoom the map to the location of the given GeoNFT.");
                DisplayCommand("map zoomToGeoHotSpot", "{GeoHotSpot id/name}", "Zoom the map to the location of the given GeoHotSpot.");
                DisplayCommand("map zoomToOAPP", "{OAPP id/name}", "Zoom the map to the location of the given OAPP.");
                DisplayCommand("map zoomToCoOrds", "{x} {y}", "Zoom the map to the location of the given {x} and {y} coordinates.");
                DisplayCommand("map drawRouteOnMap", "{startX} {startY} {endX} {endY}", "Draw a route on the map.");
                DisplayCommand("map drawRouteBetweenHolons", "{fromHolonId} {toHolonId}", "Draw a route on the map between the two holons.");
                DisplayCommand("map drawRouteBetweenBuildings", "{fromBuildingId} {toBuildingId}", "Draw a route on the map between the two buildings.");
                DisplayCommand("map drawRouteBetweenQuests", "{fromQuestId} {toQuestId}", "Draw a route on the map between the two quests.");
                DisplayCommand("map drawRouteBetweenGeoNFTs", "{fromGeoNFTId} {ToGeoNFTId}", "Draw a route on the map between the two GeoNFTs.");
                DisplayCommand("map drawRouteBetweenGeoHotSpots", "{fromGeoHotSpotId} {ToGeoHotSpotId}", "Draw a route on the map between the two GeoHotSpots.");
                DisplayCommand("map drawRouteBetweenOAPPs", "{fromOAPP id/name} {ToOAPP id/name}", "Draw a route on the map between the two OAPPs.");

                //OLAND Commands
                DisplayCommand("oland price", "", "Get the currently OLAND price.");
                DisplayCommand("oland purchase", "", "Purchase OLAND for Our World/OASIS.");
                DisplayCommand("oland load", "{id}", "Load a OLAND for the given {id}.");
                DisplayCommand("oland save", "", "Save a OLAND.");
                DisplayCommand("oland delete", "{id}", "Deletes a OLAND for the given {id}.");
                DisplayCommand("oland list", "[allVersions] [forAllAvatars]", "If [all] is omitted it will list all OLAND for the given beamed in avatar, otherwise it will list all OLAND for all avatars.");

                //ONET Commands
                DisplayCommand("onet status", "", "Shows stats for the OASIS Network (ONET).");
                DisplayCommand("onet providers", "", "Shows what OASIS Providers are running across the ONET and on what ONODE's.");
                DisplayCommand("onet discover", "", "Discovers available ONET nodes in the network.");
                DisplayCommand("onet connect", "{nodeAddress}", "Connects to a specific ONET node.");
                DisplayCommand("onet disconnect", "{nodeAddress}", "Disconnects from a specific ONET node.");
                DisplayCommand("onet topology", "", "Shows the ONET network topology and connections.");
               
                //ONODE Commands
                DisplayCommand("onode start", "", "Starts a OASIS Node (ONODE) and registers it on the OASIS Network (ONET).");
                DisplayCommand("onode stop", "", "Stops a OASIS Node (ONODE).");
                DisplayCommand("onode status", "", "Shows stats for this ONODE.");
                DisplayCommand("onode config", "", "Opens the ONODE's OASISDNA to allow changes to be made (you will need to stop and start the ONODE for changes to apply).");
                DisplayCommand("onode providers", "", "Shows what OASIS Providers are running for this ONODE.");
                DisplayCommand("onode startprovider", "{ProviderName}", "Starts a given provider.");
                DisplayCommand("onode stopprovider", "{ProviderName}", "Stops a given provider.");

                //HyperNET Commands
                DisplayCommand("hypernet start", "", "Starts the HoloNET P2P HyperNET Service.");
                DisplayCommand("hypernet stop", "", "Stops the HoloNET P2P HyperNET Service.");
                DisplayCommand("hypernet status", "", "Shows stats for the HoloNET P2P HyperNET Service.");
                DisplayCommand("hypernet config", "", "Opens the HyperNET's DNA to allow changes to be made (you will need to stop and start the HyperNET Service for changes to apply.");

                //ONET Commands
                DisplayCommand("onet status", "", "Shows stats for the OASIS Network (ONET).");
                DisplayCommand("onet providers", "", "Shows what OASIS Providers are running across the ONET and on what ONODE's.");

                //Config Commands
                DisplayCommand("config cosmicdetailedoutput", "{enable/disable/status}", "Enables/disables COSMIC Detailed Output.");
                DisplayCommand("config starstatusdetailedoutput", "{enable/disable/status}", "Enables/disables STAR ODK Detailed Output.");

                //Test Commands
                DisplayCommand("runcosmictests", "{OAPPType} {dnaFolder} {geneisFolder}", "Run the STAR ODK/COSMIC Tests... If OAPPType, DNAFolder or GenesisFolder are not specified it will use the defaults.");
                DisplayCommand("runoasisapitests", "", "Run the OASIS API Tests...");

                


                //DisplayCommand("oapp create", "", "Shortcut to the light sub-command.");
                //DisplayCommand("oapp update", "{id/name}", "Update an existing OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp adddependency", "{id/name}", "Adds a dependency to an existing OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp removedependency", "{id/name}", "Removes a dependency for an existing OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp delete", "{id/name}", "Delete an existing OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp publish", "{id/name}", "Shortcut to the seed sub-command.");
                //DisplayCommand("oapp unpublish", "{id/name}", "Shortcut to the un-seed sub-command.");
                //DisplayCommand("oapp republish", "{id/name}", "Shortcut to the re-seed sub-command.");
                //DisplayCommand("oapp activate", "{id/name}", "Activate a OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp deactivate", "{id/name}", "Deactivate a OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp download", "{id/name}", "Download a OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp install", "{id/name}", "Install/download a OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp uninstall", "{id/name}", "Uninstall a OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp show", "{id/name} [detailed]", "Shows a OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp list", "[allVersions] [forAllAvatars] [detailed]", "List all OAPPs (contains zomes and holons) that have been generated.");
                //DisplayCommand("oapp list installed", "", "List all OAPP's installed for the currently beamed in avatar.");
                //DisplayCommand("oapp list uninstalled", "", "List all OAPP's uninstalled for the currently beamed in avatar (and allow re-install).");
                //DisplayCommand("oapp list unpublished", "", "List all OAPP's unpublished for the currently beamed in avatar (and allow republish).");
                //DisplayCommand("oapp list deactivated", "", "List all OAPP's deactivated for the currently beamed in avatar (and allow reactivate).");
                //DisplayCommand("oapp search", "[allVersions] [forAllAvatars]", "Searches the OAPP's for the given search criteria.");
                //DisplayCommand("oapp template create", "", "Creates a OAPP template.");
                //DisplayCommand("oapp template update", "{id/name}", "Updates a OAPP template for the given {id} or {name}.");
                //DisplayCommand("oapp template adddependency", "{id/name}", "Adds a dependency to an existing OAPP Template for the given {id} or {name}.");
                //DisplayCommand("oapp template removedependency", "{id/name}", "Removes a dependency for an existing OAPP Template for the given {id} or {name}.");
                //DisplayCommand("oapp template delete", "{id/name}", "Deletes a OAPP template for the given {id} or {name}.");
                //DisplayCommand("oapp template publish", "{id/name}", "Publishes a OAPP template to the STARNET store for the given {id} or {name}.");
                //DisplayCommand("oapp template unpublish", "{id/name}", "Unpublishes a OAPP template from the STARNET store for the given {id} or {name}.");
                //DisplayCommand("oapp template republish", "{id/name}", "Republishes a OAPP template to the STARNET store for the given {id} or {name}.");
                //DisplayCommand("oapp template activate", "{id/name}", "Activate a OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp template deactivate", "{id/name}", "Deactivate a OAPP for the given {id} or {name}.");
                //DisplayCommand("oapp template download", "{id/name}", "Downloads a OAPP template for the given {id} or {name}.");
                //DisplayCommand("oapp template install", "{id/name}", "Installs/downloads a OAPP template for the given {id} or {name}.");
                //DisplayCommand("oapp template uninstall", "{id/name}", "Uninstalls a OAPP template for the given {id} or {name}.");
                //DisplayCommand("oapp template show", "{id/name} [detailed]", "Shows a OAPP template for the given {id} or {name}.");
                //DisplayCommand("oapp template list", "[allVersions] [forAllAvatars] [detailed]", "List all OAPP template's that have been created.");
                //DisplayCommand("oapp template list installed", "", "List all OAPP template's installed for the currently beamed in avatar.");
                //DisplayCommand("oapp template list uninstalled", "", "List all OAPP template's uninstalled for the currently beamed in avatar (and allow reinstalling).");
                //DisplayCommand("oapp template list unpublished", "", "List all OAPP template's unpublished for the currently beamed in avatar (and allow republishing).");
                //DisplayCommand("oapp template list deactivated", "", "List all OAPP template's deactivated for the currently beamed in avatar (and allow reactivating).");
                //DisplayCommand("oapp template search", "[allVersions] [forAllAvatars]", "Searches the OAPP template's for the given search criteria.");
                //DisplayCommand("happ create", "", "Shortcut to the light sub-command.");
                //DisplayCommand("happ update", "{id/name}", "Update an existing hApp for the given {id} or {name}.");
                //DisplayCommand("happ adddependency", "{id/name}", "Adds a dependency to an existing hApp for the given {id} or {name}.");
                //DisplayCommand("happ removedependency", "{id/name}", "Removes a dependency for an existing hApp for the given {id} or {name}.");
                //DisplayCommand("happ delete", "{id/name}", "Delete an existing hApp for the given {id} or {name}.");
                //DisplayCommand("happ publish", "{id/name}", "Publishes a hApp to the STARNET store for the given {id} or {name}.");
                //DisplayCommand("happ unpublish", "{id/name}", "Unpublishes a hApp from the STARNET store for the given {id} or {name}.");
                //DisplayCommand("happ republish", "{id/name}", "Republishes a hApp to the STARNET store for the given {id} or {name}.");
                //DisplayCommand("happ activate", "{id/name}", "Activates a hApp on the STARNET store for the given {id} or {name}.");
                //DisplayCommand("happ deactivate", "{id/name}", "Deactivates a hApp on the STARNET store for the given {id} or {name}.");
                //DisplayCommand("happ download", "{id/name}", "Downloads a hApp for the given {id} or {name}.");
                //DisplayCommand("happ install", "{id/name}", "Installs/downloads a hApp for the given {id} or {name}.");
                //DisplayCommand("happ uninstall", "{id/name}", "Uninstalls a hApp for the given {id} or {name}.");
                //DisplayCommand("happ show", "{id/name} [detailed]", "Shows a hApp for the given {id} or {name}.");
                //DisplayCommand("happ list", "[allVersions] [forAllAvatars] [detailed]", "List all hApp's (contains zomes) that have been generated.");
                //DisplayCommand("happ list installed", "", "List all hApp's installed for the currently beamed in avatar.");
                //DisplayCommand("happ list uninstalled", "", "List all hApp's uninstalled for the currently beamed in avatar (and allow reinstalling).");
                //DisplayCommand("happ list unpublished", "", "List all hApp's unpublished for the currently beamed in avatar (and allow republishing).");
                //DisplayCommand("happ list deactivated", "", "List all hApp's deactivated for the currently beamed in avatar (and allow reactivating).");
                //DisplayCommand("happ search", "[allVersions] [forAllAvatars]", "Searches the hApp's for the given search criteria.");
                //DisplayCommand("lib create", "", "Create a new library. Only admin's can create & publish OASIS/STAR runtime's.");
                //DisplayCommand("lib update", "{id/name}", "Update an existing library for the given {id} or {name}.");
                //DisplayCommand("lib adddependency", "{id/name}", "Adds a dependency to an existing library for the given {id} or {name}.");
                //DisplayCommand("lib removedependency", "{id/name}", "Removes a dependency for an existing library for the given {id} or {name}.");
                //DisplayCommand("lib delete", "{id/name}", "Delete an existing library for the given {id} or {name}.");
                //DisplayCommand("lib publish", "{id/name}", "Publish a library.");
                //DisplayCommand("lib unpublish", "{id/name}", "Unpublish a library.");
                //DisplayCommand("lib republish", "{id/name}", "Republish a library.");
                //DisplayCommand("lib activate", "{id/name}", "Activates a library on the STARNET store for the given {id} or {name}.");
                //DisplayCommand("lib deactivate", "{id/name}", "Deactivates a library on the STARNET store for the given {id} or {name}.");
                //DisplayCommand("lib download", "{id/name}", "Downloads a library for the given {id} or {name}.");
                //DisplayCommand("lib install", "{id/name}", "Installs/downloads a library for the given {id} or {name}.");
                //DisplayCommand("lib uninstall", "{id/name}", "Uninstalls a library for the given {id} or {name}.");
                //DisplayCommand("lib show", "{id/name} [detailed]", "Shows a library for the given {id} or {name}.");
                //DisplayCommand("lib list", "[allVersions] [forAllAvatars] [detailed]", "List all libraries that have been generated.");
                //DisplayCommand("lib list installed", "", "List all libraries installed for the currently beamed in avatar.");
                //DisplayCommand("lib list uninstalled", "", "List all libraries uninstalled for the currently beamed in avatar (and allow reinstalling).");
                //DisplayCommand("lib list unpublished", "", "List all libraries unpublished for the currently beamed in avatar (and allow republishing).");
                //DisplayCommand("lib list deactivated", "", "List all libraries deactivated for the currently beamed in avatar (and allow reactivating).");
                //DisplayCommand("lib search", "[allVersions] [forAllAvatars]", "Searches the libraries for the given search criteria.");



                //Console.WriteLine("    light                                         {OAPPName} {OAPPDesc} {OAPPType}          Creates a new OAPP (Zomes/Holons/Star/Planet/Moon) at the given genesis folder location, from the given OAPP DNA.");
                //Console.WriteLine("                                                  {dnaFolder} {geneisFolder}");
                //Console.WriteLine("                                                  {genesisNameSpace} {genesisType}");
                //Console.WriteLine("                                                  {parentCelestialBodyId}");
                //Console.WriteLine("    light                                                                                   Displays more detail on how to use this command and optionally launches the Light Wizard.");
                //Console.WriteLine("    light wiz                                                                               Start the Light Wizard.");
                //Console.WriteLine("    light transmute                               {hAppDNA} {geneisFolder}                  Creates a new Planet (OApp) at the given folder genesis locations, from the given hApp DNA.");
                //Console.WriteLine("    bang                                                                                    Generate a whole metaverse or part of one such as Multierveres, Universes, Dimensions, Galaxy Clusters, Galaxies, Solar Systems, Stars, Planets, Moons etc.");
                ////Console.WriteLine("    wiz                                                                                   Start the STAR ODK Wizard which will walk you through the steps for creating a OAPP tailored to your specefic needs");
                ////Console.WriteLine("                                                                                          (such as which OASIS Providers do you need and the specefic use case(s) you need etc).");
                //Console.WriteLine("    wiz                                                                                     Start the STAR ODK Wizard which will walk you through the steps for creating a OAPP tailored to your specefic needs (such as which OASIS Providers do you need and the specefic use case(s) you need etc).");
                //Console.WriteLine("    flare                                         {id/name}                                 Build a OAPP for the given {id} or {name}.");
                //Console.WriteLine("    shine                                         {id/name}                                 Launch & activate a OAPP for the given {id} or {name} by shining the 's light upon it..."); //TODO: Dev next.
                //Console.WriteLine("    twinkle                                       {id/name}                                 Activate a published OAPP for the given {id} or {name} within the STARNET store."); //TODO: Dev next.
                //Console.WriteLine("    dim                                           {id/name}                                 Deactivate a published OAPP for the given {id} or {name} within the STARNET store."); //TODO: Dev next.
                //Console.WriteLine("    seed                                          {id/name}                                 Deploy/Publish a OAPP for the given {id} or {name} to the STARNET Store.");
                //Console.WriteLine("    unseed                                        {id/name}                                 Undeploy/Unpublish a OAPP for the given {id} or {name} from the STARNET Store.");
                //Console.WriteLine("    reseed                                        {id/name}                                 Redeploy/Republish a OAPP for the given {id} or {name} to the STARNET Store.");
                //Console.WriteLine("    dust                                          {id/name}                                 Delete a OAPP for the given {id} or {name} (this will also remove it from STARNET if it has already been published)."); //TODO: Dev next.
                //Console.WriteLine("    radiate                                       {id/name}                                 Highlight the OAPP for the given {id} or {name} in the STARNET Store. *Admin/Wizards Only*");
                //Console.WriteLine("    emit                                          {id/name}                                 Show how much light the OAPP is emitting into the solar system for the given {id} or {name}");
                //Console.WriteLine("                                                                                            (this is determined by the collective karma score of all users of that OAPP).");
                //Console.WriteLine("    reflect                                       {id/name}                                 Show stats of the OAPP for the given {id} or {name}.");
                //Console.WriteLine("    evolve                                        {id/name}                                 Upgrade/update a OAPP) for the given {id} or {name}."); //TODO: Dev next.
                //Console.WriteLine("    mutate                                        {id/name}                                 Import/Export hApp, dApp & others for the given {id} or {name}.");
                //Console.WriteLine("    love                                          {id/username}                             Send/Receive Love for the given {id} or {username}.");
                //Console.WriteLine("    burst                                                                                   View network stats/management/settings.");
                //Console.WriteLine("    super                                                                                   Reserved For Future Use...");
                //Console.WriteLine("    net                                                                                     Launch the STARNET Library/Store where you can list, search, update, publish, unpublish, install & uninstall OAPP's & more!");
                //Console.WriteLine("    gate                                                                                    Opens the STARGATE to the OASIS Portal!");
                //Console.WriteLine("    api                                           [oasis]                                   Opens the WEB5 STAR API (if oasis is included then it will open the WEB4 OASIS API instead).");
                //Console.WriteLine("    avatar beamin                                                                           Beam in (log in).");
                //Console.WriteLine("    avatar beamout                                                                          Beam out (Log out).");
                //Console.WriteLine("    avatar whoisbeamedin                                                                    Display who is currently beamed in (if any) and the last time they beamed in and out.");
                //Console.WriteLine("    avatar show me                                                                          Display the currently beamed in avatar details (if any).");
                //Console.WriteLine("    avatar show                                   {id/username}                             Shows the details for the avatar for the given {id} or {username}.");
                //Console.WriteLine("    avatar edit                                                                             Edit the currently beamed in avatar.");
                //Console.WriteLine("    avatar list                                   [detailed]                                Lists all avatars. If [detailed] is included it will list detailed stats also.");
                //Console.WriteLine("    avatar search                                                                           Seach avatars that match the given seach parameters (public fields only such as level, karma, username & any fields the player has set to public).");
                //Console.WriteLine("    avatar forgotpassword                                                                   Send a Forgot Password email to your email account containing a Reset Token.");
                //Console.WriteLine("    avatar resetpassword                                                                    Allows you to reset your password using the Reset Token received in your email from the forgotpassword sub-command.");
                //Console.WriteLine("    karma list                                                                              Display the karma thresholds.");
                //Console.WriteLine("    keys link privateKey                          [walletId] [privateKey]                   Links a private key to the given wallet for the currently beamed in avatar.");
                //Console.WriteLine("    keys link publicKey                           [walletId] [publicKey]                    Links a public key to the given wallet for the currently beamed in avatar.");
                //Console.WriteLine("    keys link genKeyPair                          [walletId]                                Generates a unique keyvalue pair and then links them to to the given wallet for the currently beamed in avatar.");
                //Console.WriteLine("    keys generateKeyPair                                                                    Generates a unique keyvalue pair.");
                //Console.WriteLine("    keys clearCache                                                                         Clears the cache.");
                //Console.WriteLine("    keys get provideruniquestoragekey             {providerType}                            Gets the Provider Unique Storage Key for the given provider and the currently beamed in avatar.");
                //Console.WriteLine("    keys get providerpublickeys                   {providerType}                            Gets the Provider Public Keys for the given provider and the currently beamed in avatar.");
                //Console.WriteLine("    keys get avataridforprovideruniquestoragekey  {avatarId}                                Gets the Provider Private Keys for the given provider and the currently beamed in avatar.");
                //Console.WriteLine("    keys get avataridforprovideruniquestoragekey  {avatarId}                                Gets the Provider Private Keys for the given provider and the currently beamed in avatar.");
                //Console.WriteLine("    keys list                                                                               Shows the keys for the currently beamed in avatar.");
                //Console.WriteLine("    wallet sendtoken                              {walletAddress} {token} {amount}          Sends a token to the given wallet address.");
                //Console.WriteLine("    wallet transfer                               {from walletId/name} {amount}             Transfers the given [amount] from one wallet to another for the currently beamed in avatar.");
                //Console.WriteLine("                                                  {to walletId/name}");                           
                //Console.WriteLine("    wallet get                                    {publickey}                               Gets the wallet that the public key belongs to.");
                //Console.WriteLine("    wallet getDefault                                                                       Gets the default wallet for the currently beamed in avatar.");
                //Console.WriteLine("    wallet setDefault                             {walletId}                                Sets the default wallet for the currently beamed in avatar.");
                //Console.WriteLine("    wallet import privateKey                      {privateKey}                              Imports a wallet using the privateKey.");
                //Console.WriteLine("    wallet import publicKey                       {publicKey}                               Imports a wallet using the publicKey.");
                //Console.WriteLine("    wallet import secretPhase                     {secretPhase}                             Imports a wallet using the secretPhase.");
                //Console.WriteLine("    wallet import json                            {jsonFile}                                Imports a wallet using the jsonFile.");
                //Console.WriteLine("    wallet import json                            {jsonFile}                                Imports a wallet using the jsonFile.");
                //Console.WriteLine("    wallet add                                                                              Adds a wallet for the currently beamed in avatar.");
                //Console.WriteLine("    wallet list                                                                             Lists the wallets for the currently beamed in avatar.");
                //Console.WriteLine("    wallet balance                                {walletId}                                Gets the balance for the given wallet for the currently beamed in avatar.");
                //Console.WriteLine("    wallet balance                                                                          Gets the total balance for all wallets for the currently beamed in avatar.");
                //Console.WriteLine("    search                                                                                  Seaches The OASIS for the given seach parameters.");
                //Console.WriteLine("    oapp create                                                                             Shortcut to the light sub-command.");
                //Console.WriteLine("    oapp update                                    {id/name}                                 Update an existing OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp adddependency                             {id/name}                                 Adds a dependency to an existing OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp removedependency                          {id/name}                                 Removes a dependency for an existing OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp delete                                    {id/name}                                 Delete an existing OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp publish                                   {id/name}                                 Shortcut to the seed sub-command.");
                //Console.WriteLine("    oapp unpublish                                 {id/name}                                 Shortcut to the un-seed sub-command.");
                //Console.WriteLine("    oapp republish                                 {id/name}                                 Shortcut to the re-seed sub-command.");
                //Console.WriteLine("    oapp activate                                  {id/name}                                 Activate a OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp deactivate                                {id/name}                                 Deactivate a OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp download                                  {id/name}                                 Download a OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp install                                   {id/name}                                 Install/download a OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp uninstall                                 {id/name}                                 Uninstall a OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp show                                      {id/name} [detailed]                      Shows a OAPP for the given {id} or {name}.");
                //Console.WriteLine("    oapp list                                      [allVersions] [forAllAvatars] [detailed]  List all OAPPs (contains zomes and holons) that have been generated.");
                //Console.WriteLine("    oapp list installed                                                                      List all OAPP's installed for the currently beamed in avatar.");
                //Console.WriteLine("    oapp list uninstalled                                                                    List all OAPP's uninstalled for the currently beamed in avatar (and allow re-install).");
                //Console.WriteLine("    oapp list unpublished                                                                    List all OAPP's unpublished for the currently beamed in avatar (and allow republish).");
                //Console.WriteLine("    oapp list deactivated                                                                    List all OAPP's deactivated for the currently beamed in avatar (and allow reactivate).");
                //Console.WriteLine("    oapp search                                    [allVersions] [forAllAvatars]             Searches the OAPP's for the given search critera.");
                //Console.WriteLine("    runtime create                                                                          Create a new runtime. Only admin's can create & publish OASIS/STAR runtime's.");
                //Console.WriteLine("    runtime update                                {id/name}                                 Update an existing runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime adddependency                         {id/name}                                 Adds a dependency to an existing runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime removedependency                      {id/name}                                 Removes a dependency for an existing runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime addlib                                {id/name}                                 Adds a library to an existing runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime removelib                             {id/name}                                 Removes a library for an existing runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime addruntime                            {id/name}                                 Adds a sub-runtime to a runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime removeruntime                         {id/name}                                 Removes a sub-runtime from an existing runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime addtemplate                           {id/name}                                 Adds a sub-template to an existing runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime removetemplate                        {id/name}                                 Removes a sub-template for an existing runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime delete                                {id/name}                                 Delete an existing runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime publish                               {id/name}                                 Publish a runtime.");
                //Console.WriteLine("    runtime unpublish                             {id/name}                                 Unpublish a runtime.");
                //Console.WriteLine("    runtime republish                             {id/name}                                 Republish a runtime.");
                //Console.WriteLine("    runtime activate                              {id/name}                                 Activates a runtime on the STARNET store for the given {id} or {name}.");
                //Console.WriteLine("    runtime deactivate                            {id/name}                                 Decctivates a runtime on the STARNET store for the given {id} or {name}.");
                //Console.WriteLine("    runtime download                              {id/name}                                 Downloads a runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime install                               {id/name}                                 Installs/downloads a runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime uninstall                             {id/name}                                 Uninstalls a runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime reinstall                            {id/name}                                Reinstalls a runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime show                                  {id/name} [detailed]                      Shows a runtime for the given {id} or {name}.");
                //Console.WriteLine("    runtime list                                  [allVersions] [forAllAvatars] [detailed]  List all runtime's that have been generated.");
                //Console.WriteLine("    runtime list installed                                                                  List all runtime's installed for the currently beamed in avatar.");
                //Console.WriteLine("    runtime list uninstalled                                                                List all runtime's uninstalled for the currently beamed in avatar (and allow reinstalling).");
                //Console.WriteLine("    runtime list unpublished                                                                List all runtime's unpublished for the currently beamed in avatar (and allow republishing).");
                //Console.WriteLine("    runtime list deactivated                                                                List all runtime's deactivated for the currently beamed in avatar (and allow reactivating).");
                //Console.WriteLine("    runtime search                                [allVersions] [forAllAvatars]             Searches the runtime's for the given search critera.");
                //Console.WriteLine("    lib create                                                                              Create a new library. Only admin's can create & publish OASIS/STAR runtime's.");
                //Console.WriteLine("    lib update                                    {id/name}                                 Update an existing library for the given {id} or {name}.");
                //Console.WriteLine("    lib adddependency                             {id/name}                                 Adds a dependency to an existing library for the given {id} or {name}.");
                //Console.WriteLine("    lib removedependency                          {id/name}                                 Removes a dependency for an existing library for the given {id} or {name}.");
                //Console.WriteLine("    lib addlib                                    {id/name}                                 Adds a sub-library to an existing library for the given {id} or {name}.");
                //Console.WriteLine("    lib removelib                                 {id/name}                                 Removes a sub-library for an existing library for the given {id} or {name}.");
                //Console.WriteLine("    lib addruntime                                {id/name}                                 Adds a runtime to a library for the given {id} or {name}.");
                //Console.WriteLine("    lib removeruntime                             {id/name}                                 Removes a runtime from an existing library for the given {id} or {name}.");
                //Console.WriteLine("    lib addtemplate                               {id/name}                                 Adds a sub-template to an existing library for the given {id} or {name}.");
                //Console.WriteLine("    lib removetemplate                            {id/name}                                 Removes a sub-template for an existing library for the given {id} or {name}.");
                //Console.WriteLine("    lib delete                                    {id/name}                                 Delete an existing library for the given {id} or {name}.");
                //Console.WriteLine("    lib publish                                   {id/name}                                 Publish a library.");
                //Console.WriteLine("    lib unpublish                                 {id/name}                                 Unpublish a library.");
                //Console.WriteLine("    lib republish                                 {id/name}                                 Republish a library.");
                //Console.WriteLine("    lib activate                                  {id/name}                                 Activates a library on the STARNET store for the given {id} or {name}.");
                //Console.WriteLine("    lib deactivate                                {id/name}                                 Decctivates a library on the STARNET store for the given {id} or {name}.");
                //Console.WriteLine("    lib download                                  {id/name}                                 Downloads a library for the given {id} or {name}.");
                //Console.WriteLine("    lib install                                   {id/name}                                 Installs/downloads a library for the given {id} or {name}.");
                //Console.WriteLine("    lib uninstall                                 {id/name}                                 Uninstalls a library for the given {id} or {name}.");
                //Console.WriteLine("    lib reinstall                               {id/name}                                 Reinstalls a lib for the given {id} or {name}.");
                //Console.WriteLine("    lib show                                      {id/name} [detailed]                      Shows a library for the given {id} or {name}.");
                //Console.WriteLine("    lib list                                      [allVersions] [forAllAvatars] [detailed]  List all libraries that have been generated.");
                //Console.WriteLine("    lib list installed                                                                      List all libraries installed for the currently beamed in avatar.");
                //Console.WriteLine("    lib list uninstalled                                                                    List all libraries uninstalled for the currently beamed in avatar (and allow reinstalling).");
                //Console.WriteLine("    lib list unpublished                                                                    List all libraries unpublished for the currently beamed in avatar (and allow republishing).");
                //Console.WriteLine("    lib list deactivated                                                                    List all libraries deactivated for the currently beamed in avatar (and allow reactivating).");
                //Console.WriteLine("    lib search                                    [allVersions] [forAllAvatars]             Searches the libraries for the given search critera.");
                //Console.WriteLine("    celestialspace create                                                                   Creates a celestial space.");
                //Console.WriteLine("    celestialspace update                         {id/name}                                 Update an existing celestial space for the given {id} or {name}.");
                //Console.WriteLine("    celestialspace adddependency                  {id/name}                                 Adds a dependency to an existing celestial space for the given {id} or {name}.");
                //Console.WriteLine("    celestialspace removedependency               {id/name}                                 Removes a dependency for an existing celestial space for the given {id} or {name}.");
                //Console.WriteLine("    celestialspace delete                         {id/name}                                 Delete an existing celestial space for the given {id} or {name}.");
                //Console.WriteLine("    celestialspace publish                        {id/name}                                 Publishes a celestial space for the given {id} or {name} to the STARNET store so others can use in their own OAPP's etc.");
                //Console.WriteLine("    celestialspace unpublish                      {id/name}                                 Unpublishes a celestial space for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    celestialspace show                           {id/name} [detailed]                      Shows a celestial space for the given {id} or {name}.");
                //Console.WriteLine("    celestialspace list                           [allVersions] [forAllAvatars] [detailed]  List all celestial spaces that have been generated.");
                //Console.WriteLine("    celestialspace search                         [allVersions] [forAllAvatars]             Searches the celestial spaces for the given search critera.");
                //Console.WriteLine("    celestialbody create                                                                    Creates a celestial body.");
                //Console.WriteLine("    celestialbody update                          {id/name}                                 Update an existing celestial body for the given {id} or {name}.");
                //Console.WriteLine("    celestialbody adddependency                   {id/name}                                 Adds a dependency to an existing celestial body for the given {id} or {name}.");
                //Console.WriteLine("    celestialbody removedependency                {id/name}                                 Removes a dependency for an existing celestial body for the given {id} or {name}.");
                //Console.WriteLine("    celestialbody delete                          {id/name}                                 Delete an existing celestial body for the given {id} or {name}.");
                //Console.WriteLine("    celestialbody publish                         {id/name}                                 Publishes a celestial body for the given {id} or {name} to the STARNET store so others can use in their own OAPP's etc.");
                //Console.WriteLine("    celestialbody unpublish                       {id/name}                                 Unpublishes a celestial body for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    celestialbody show                            {id/name}                                 Shows a celestial body for the given {id} or {name}.");
                //Console.WriteLine("    celestialbody list                            [allVersions] [forAllAvatars] [detailed]  List all celestial bodies that have been generated.");
                //Console.WriteLine("    celestialbody search                          [allVersions] [forAllAvatars]             Searches the celestial bodies for the given search critera.");
                //Console.WriteLine("    celestialbody metadata create                                                           Creates celestial body metadata.");
                //Console.WriteLine("    celestialbody metadata update                 {id/name}                                 Update existing celestial body metadata for the given {id} or {name}.");
                //Console.WriteLine("    celestialbody metadata delete                 {id/name}                                 Delete existing celestial body metadata for the given {id} or {name}.");
                //Console.WriteLine("    celestialbody metadata publish                {id/name}                                 Publishes celestial body metadata for the given {id} or {name} to the STARNET store so others can use in their own OAPP's etc.");
                //Console.WriteLine("    celestialbody metadata unpublish              {id/name}                                 Unpublishes celestial body metadata for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    celestialbody metadata show                   {id/name}                                 Shows celestial body metadata for the given {id} or {name}.");
                //Console.WriteLine("    celestialbody metadata list                   [allVersions] [forAllAvatars] [detailed]  List all celestial body metadata that have been generated.");
                //Console.WriteLine("    celestialbody metadata search                 [allVersions] [forAllAvatars]             Searches the celestial body metadata for the given search critera.");
                //Console.WriteLine("    zome create                                                                             Create a zome (module).");
                //Console.WriteLine("    zome update                                   {id/name}                                 Update an existing zome for the given {id} or {name}");
                //Console.WriteLine("                                                                                            (can upload a zome.cs file containing custom code/logic/functions which is then shareable with other OAPP's).");
                //Console.WriteLine("    zome adddependency                            {id/name}                                 Adds a dependency to an existing zome for the given {id} or {name}.");
                //Console.WriteLine("    zome removedependency                         {id/name}                                 Removes a dependency for an existing zome for the given {id} or {name}.");
                //Console.WriteLine("    zome delete                                   {id/name}                                 Delete an existing zome for the given {id} or {name}.");
                //Console.WriteLine("    zome publish                                  {id/name}                                 Publishes a zome for the given {id} or {name} to the STARNET store so others can use in their own OAPP's/hApp's etc.");
                //Console.WriteLine("    zome unpublish                                {id/name}                                 Unpublishes a zome for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    zome show                                     {id/name}                                 Shows a zome for the given {id} or {name}.");
                //Console.WriteLine("    zome list                                     [allVersions] [forAllAvatars] [detailed]  List all zomes (modules that contain holons) that have been generated.");
                //Console.WriteLine("    zome search                                   [allVersions] [forAllAvatars]             Searches the zomes (modules) for the given search critera. If [all] is omitted it will search only your zomes otherwise it will search all public/shared zomes.");
                //Console.WriteLine("    zome metadata create                                                                    Create zome metadata.");
                //Console.WriteLine("    zome metadata update                          {id/name}                                 Update existing zome metadata for the given {id} or {name}");
                //Console.WriteLine("    zome metadata delete                          {id/name}                                 Delete existing zome metadata for the given {id} or {name}.");
                //Console.WriteLine("    zome metadata publish                         {id/name}                                 Publishes zome metadata for the given {id} or {name} to the STARNET store so others can use in their own OAPP's/hApp's etc.");
                //Console.WriteLine("    zome metadata unpublish                       {id/name}                                 Unpublishes zome metadata for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    zome metadata show                            {id/name}                                 Shows zome metadata for the given {id} or {name}.");
                //Console.WriteLine("    zome metadata list                            [allVersions] [forAllAvatars]             List all zome metadata that have been generated.");
                //Console.WriteLine("    zome metadata search                          [allVersions] [forAllAvatars]             Searches the zome metadata for the given search critera. If [all] is omitted it will search only your zomes otherwise it will search all public/shared zomes.");
                //Console.WriteLine("    holon create                                  json={holonJSONFile}                      Creates/Saves a holon from the given {holonJSONFile}.");
                //Console.WriteLine("    holon create wiz                                                                        Starts the Create Holon Wizard.");
                //Console.WriteLine("    holon update                                  {id/name}                                 Update an existing holon for the given {id} or {name}");
                //Console.WriteLine("    holon delete                                  {id/name}                                 Deletes a holon for the given {id} or {name}.");
                //Console.WriteLine("    holon publish                                 {id/name}                                 Publishes a holon for the given {id} or {name} to the STARNET store so others can use in their own OAPP's/hApp's etc.");
                //Console.WriteLine("    holon unpublish                               {id/name}                                 Unpublishes a holon for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    holon show                                    {id/name} [detailed]                      Shows a holon for the given {id} or {name}.");
                //Console.WriteLine("    holon list                                    [allVersions] [forAllAvatars]             List all holons (OASIS Data Objects) that have been generated.");
                //Console.WriteLine("    holon search                                  [allVersions] [forAllAvatars]             Searches the holons for the given search critera.");
                //Console.WriteLine("    holon metadata create                                                                   Creates/Saves holon metadata.");
                //Console.WriteLine("    holon metadata update                         {id/name}                                 Update an existing holon metadata for the given {id} or {name}");
                //Console.WriteLine("    holon metadata delete                         {id/name}                                 Deletes holon metadata for the given {id} or {name}.");
                //Console.WriteLine("    holon metadata publish                        {id/name}                                 Publishes holon metadata for the given {id} or {name} to the STARNET store so others can use in their own OAPP's/hApp's etc.");
                //Console.WriteLine("    holon metadata unpublish                      {id/name}                                 Unpublishes holon metadata for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    holon metadata show                           {id/name} [detailed]                      Shows holon metadata for the given {id} or {name}.");
                //Console.WriteLine("    holon metadata list                           [allVersions] [forAllAvatars] [detailed]  List all holon metadata that has been generated.");
                //Console.WriteLine("    holon metadata search                         [allVersions] [forAllAvatars]             Searches the holon metadata for the given search critera.");
                //Console.WriteLine("    chapter create                                                                          Creates a chapter that can be linked to a mission. Quests can be added to the chapter. Chapters are used to group quests together (optional).");
                //Console.WriteLine("    chapter update                                {id/name}                                 Updates a chapter for the given {id} or {name}.");
                //Console.WriteLine("    chapter delete                                {id/name}                                 Deletes a chapter for the given {id} or {name}.");
                //Console.WriteLine("    chapter publish                               {id/name}                                 Publishes a chapter to the STARNET store for the given {id} or {name} so others can use in their own missions.");
                //Console.WriteLine("    chapter unpublish                             {id/name}                                 Unpublishes a chapter from the STARNET store for the given {id} or {name}.");
                //Console.WriteLine("    chapter show                                  {id/name}                                 Shows the chapter for the given {id} or {name}.");
                //Console.WriteLine("    chapter list                                  [allVersions] [forAllAvatars]             List chapters that have been created.");
                //Console.WriteLine("    chapter search                                [allVersions] [forAllAvatars]             Search chapters that have been created.");
                //Console.WriteLine("    mission create                                                                          Creates a mission that chapters & quests can be added to.");
                //Console.WriteLine("    mission update                                {id/name}                                 Updates a mission for the given {id} or {name}.");
                //Console.WriteLine("    mission delete                                {id/name}                                 Deletes an mission for the given {id} or {name}.");
                //Console.WriteLine("    mission publish                               {id/name}                                 Publishes a mission  for the given {id} or {name} to the STARNET store so others can find and play in Our World/AR World, One World & any other OASIS OAPP.");
                //Console.WriteLine("    mission unpublish                             {id/name}                                 Unpublishes a mission from the STARNET store for the given {id} or {name}.");
                //Console.WriteLine("    mission show                                  {id/name} [detailed]                      Shows the mission for the given {id} or {name}.");
                //Console.WriteLine("    missions list                                 [allVersions] [forAllAvatars] [detailed]  List all mission's that have been created.");
                //Console.WriteLine("    missions search                               [allVersions] [forAllAvatars]             Search all mission's that have been created.");
                //Console.WriteLine("    quest create                                                                            Creates a quest that can be linked to a mission. Geo-nfts, geo-hotspots & rewards can be linked to the quest.");
                //Console.WriteLine("    quest update                                  {id/name}                                 Updates a quest for the given {id} or {name}.");
                //Console.WriteLine("    quest delete                                  {id/name}                                 Deletes a quest for the given {id} or {name}.");
                //Console.WriteLine("    quest publish                                 {id/name}                                 Publishes a quest to the STARNET store so others can use in their own quests as sub-quests or in missions/chapters.");
                //Console.WriteLine("    quest unpublish                               {id/name}                                 Unpublishes a quest from the STARNET store for the given {id} or {name}.");
                //Console.WriteLine("    quest show                                    {id/name} [detailed]                      Shows the quest for the given {id} or {name}.");
                //Console.WriteLine("    quest list                                    [allVersions] [forAllAvatars] [detailed]  List all quests that have been created.");
                //Console.WriteLine("    quest search                                  [allVersions] [forAllAvatars]             Search all quests that have been created.");
                //Console.WriteLine("    nft mint                                                                                Mints a WEB4 OASIS NFT for the currently beamed in avatar. Also allows minting more WEB3 NFT's from an existing WEB4 OASIS NFT.");
                //Console.WriteLine("    nft create                                                                              Creates a WEB5 STAR NFT by wrapping a WEB4 OASIS NFT.");
                //Console.WriteLine("    nft update                                    {id/name}                                 Updates a nft for the given {id} or {name}.");
                //Console.WriteLine("    nft burn                                      {id/name}                                 Burn's a nft for the given {id} or {name}.");
                //Console.WriteLine("    nft send                                      {id/name}                                 Send a NFT for the given {id} or {name} to another wallet cross-chain.");
                //Console.WriteLine("    nft import                                    [web3]                                    Imports a WEB4 OASIS NFT JSON file. If [web3] param is given it will import a WEB3 NFT JSON MetaData file of NFT Token Address to be wrapped in a WEB4 OASIS NFT.");
                //Console.WriteLine("    nft export                                                                              Exports a WEB4 OASIS NFT as a JSON file as well as a WEB3 JSON MetaData file.");
                //Console.WriteLine("    nft clone                                                                               Clones a WEB4 OASIS NFT.");
                //Console.WriteLine("    nft convert                                                                             Allows the minting of different WEB3 NFT Standards for different chains from the same OASIS WEB4 Metadata.");
                //Console.WriteLine("    nft publish                                   {id/name}                                 Publishes a OASIS NFT for the given {id} or {name} to the STARNET store so others can use in their own geo-nft's etc.");
                //Console.WriteLine("    nft unpublish                                 {id/name}                                 Unpublishes a OASIS NFT for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    nft show                                      {id/name} [detailed] [web4]               Shows the NFT for the given {id} or {name}.");
                //Console.WriteLine("    nft list                                      [allVersions] [forAllAvatars] [detailed] [web4]  Shows the NFT's that belong to the currently beamed in avatar.");
                //Console.WriteLine("    nft search                                    [allVersions] [forAllAvatars] [web4]      Search for NFT's that match certain criteria and belong to the currently beamed in avatar.");
                //Console.WriteLine("    nft collection create                                                                   Mints a WEB4 OASIS NFT for the currently beamed in avatar. Also allows minting more WEB3 NFT's from an existing WEB4 OASIS NFT.");
                //Console.WriteLine("    nft collection update                         {id/name}                                 Updates a nft for the given {id} or {name}.");
                //Console.WriteLine("    nft collection delete                         {id/name}                                 Updates a nft for the given {id} or {name}.");
                //Console.WriteLine("    nft collection publish                        {id/name}                                 Publishes a OASIS NFT for the given {id} or {name} to the STARNET store so others can use in their own geo-nft's etc.");
                //Console.WriteLine("    nft collection unpublish                      {id/name}                                 Unpublishes a OASIS NFT for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    nft collection show                           {id/name} [detailed] [web4]               Shows the NFT for the given {id} or {name}.");
                //Console.WriteLine("    nft collection list                           [allVersions] [forAllAvatars] [detailed] [web4] Shows the NFT's that belong to the currently beamed in avatar.");
                //Console.WriteLine("    nft collection search                         [allVersions] [forAllAvatars] [web4]     Search for NFT's that match certain criteria and belong to the currently beamed in avatar.");
                //Console.WriteLine("    geonft mint                                                                             Mints a OASIS Geo-NFT and places in Our World/AR World for the currently beamed in avatar. Also allows minting more WEB3 NFT's from an existing WEB4 OASIS Geo-NFT.");
                //Console.WriteLine("    geonft update                                 {id/name}                                 Updates a geo-nft for the given {id} or {name}.");
                //Console.WriteLine("    geonft burn                                   {id/name}                                 Burn's a geo-nft for the given {id} or {name}.");
                //Console.WriteLine("    geonft place                                  {id/name}                                 Places an existing OASIS NFT for the given {id} or {name} in Our World/AR World for the currently beamed in avatar.");
                //Console.WriteLine("    geonft send                                   {id/name}                                 Send a geo-nft for the given {id} or {name} to another wallet cross-chain.");
                //Console.WriteLine("    geonft import                                                                           Imports a WEB4 OASIS Geo-NFT JSON file.");
                //Console.WriteLine("    geonft export                                                                           Exports a WEB4 OASIS Geo-NFT as a JSON file as well as a WEB3 JSON MetaData file.");
                //Console.WriteLine("    geonft clone                                                                            Clones a WEB4 OASIS Geo-NFT.");
                //Console.WriteLine("    geonft convert                                                                          Allows the minting of different WEB3 NFT Standards for different chains from the same OASIS WEB4 Metadata.");
                //Console.WriteLine("    geonft publish                                {id/name}                                 Publishes a geo-nft for the given {id} or {name} to the STARNET store so others can use in their own quests etc.");
                //Console.WriteLine("    geonft unpublish                              {id/name}                                 Unpublishes a geo-nft for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    geonft show                                   {id/name} [detailed]                      Shows the Geo-NFT for the given {id} or {name}");
                //Console.WriteLine("    geonft list                                   [allVersions] [forAllAvatars] [detailed]  List all geo-nft's that have been created. If [all] is omitted it will list only your geo-nft's otherwise it will list all published geo-nft's as well as yours.");
                //Console.WriteLine("    geonft search                                 [allVersions] [forAllAvatars]             Search for Geo-NFT's that match certain criteria and belong to the currently beamed in avatar.");
                //Console.WriteLine("                                                                                            If [all] is used then it will also include any shared/public/published geo-nft's");
                //Console.WriteLine("    geohotspot create                                                                       Creates a geo-hotspot that chapters & quests can be added to.");
                //Console.WriteLine("    geohotspot update                             {id/name}                                 Updates a geo-hotspot for the given {id} or {name}.");
                //Console.WriteLine("    geohotspot delete                             {id/name}                                 Deletes an geo-hotspot for the given {id} or {name}.");
                //Console.WriteLine("    geohotspot publish                            {id/name}                                 Publishes a geo-hotspot for the given {id} or {name} to the STARNET store so others can use in their own quests.");
                //Console.WriteLine("    geohotspot unpublish                          {id/name}                                 Unpublishes a geo-hotspot from the STARNET store.");
                //Console.WriteLine("    geohotspot show                               {id/name} [detailed]                      Shows the geo-hotspot for the given {id} or {name}.");
                //Console.WriteLine("    geohotspots list                              [allVersions] [forAllAvatars] [detailed]  List all geo-hotspot's that have been created.");
                //Console.WriteLine("    geohotspots search                            [allVersions] [forAllAvatars]             Search all geo-hotspot's that have been created.");
                //Console.WriteLine("    inventoryitem create                                                                    Creates an inventory item that can be granted as a reward");
                //Console.WriteLine("                                                                                            (will be placed in the avatar's inventory) for completing quests, collecting geo-nft's, triggering geo-hotspots etc.");
                //Console.WriteLine("    inventoryitem update                          {id/name}                                 Updates a inventory item for the given {id} or {name}.");
                //Console.WriteLine("    inventoryitem delete                          {id/name}                                 Deletes a inventory item for the given {id} or {name}.");
                //Console.WriteLine("    inventoryitem publish                         {id/name}                                 Publishes an inventory item for the given {id} or {name} to the STARNET store so others can use in their own quests, geo-hotspots, geo-nfts, etc.");
                //Console.WriteLine("    inventoryitem unpublish                       {id/name}                                 Unpublishes an inventory item  for the given {id} or {name} from the STARNET store.");
                //Console.WriteLine("    inventoryitem show                            {id/name} [detailed]                      Shows the inventory item for the given {id} or {name}.");
                //Console.WriteLine("    inventoryitem list                            [allVersions] [forAllAvatars] [detailed]  List all inventory item's that have been created.");
                //Console.WriteLine("    inventoryitem search                          [allVersions] [forAllAvatars]             Search all inventory item's that have been created.");
                //Console.WriteLine("    plugin create                                                                           Creates a plugin.");
                //Console.WriteLine("    plugin update                                 {id/name}                                 Updates a plugin for the given {id} or {name}.");
                //Console.WriteLine("    plugin delete                                 {id/name}                                 Deletes a plugin for the given {id} or {name}.");
                //Console.WriteLine("    plugin publish                                {id/name}                                 Publishes a plugin for the given {id} or {name} to the STARNET store so others can use in their own quests.");
                //Console.WriteLine("    plugin unpublish                              {id/name}                                 Unpublishes a plugin from the STARNET store.");
                //Console.WriteLine("    plugin show                                   {id/name} [detailed]                      Shows the plugin for the given {id} or {name}.");
                //Console.WriteLine("    plugin list                                   [allVersions] [forAllAvatars]             List all plugin that have been created.");
                //Console.WriteLine("    plugin list installed                                                                   List all plugin's installed for the currently beamed in avatar.");
                //Console.WriteLine("    plugin list uninstalled                                                                 List all plugin's uninstalled for the currently beamed in avatar (and allow re-install).");
                //Console.WriteLine("    plugin list unpublished                                                                 List all plugin's unpublished for the currently beamed in avatar (and allow republish).");
                //Console.WriteLine("    plugin list deactivated                                                                 List all plugin's deactivated for the currently beamed in avatar (and allow reactivate).");
                //Console.WriteLine("    plugin search                                 [allVersions] [forAllAvatars]             Search all plugin's that have been created.");
                //Console.WriteLine("    plugin install                                {id/name}                                 Installs/downloads a plugin for the given {id} or {name}.");
                //Console.WriteLine("    plugin uninstall                              {id/name}                                 Uninstalls a plugin for the given {id} or {name}.");
                //Console.WriteLine("    seeds balance                                 {telosAccountName/avatarId}               Get's the balance of your SEEDS account.");
                //Console.WriteLine("    seeds organisations                                                                     Get's a list of all the SEEDS organisations.");
                //Console.WriteLine("    seeds organisation                            {organisationName}                        Get's a organisation for the given {organisationName}.");
                //Console.WriteLine("    seeds pay                                     {telosAccountName/avatarId}               Pay using SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                //Console.WriteLine("    seeds donate                                  {telosAccountName/avatarId}               Donate using SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                //Console.WriteLine("    seeds reward                                  {telosAccountName/avatarId}               Reward using SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                //Console.WriteLine("    seeds invite                                  {telosAccountName/avatarId}               Send invite to join SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                //Console.WriteLine("    seeds accept                                  {telosAccountName/avatarId}               Accept the invite to join SEEDS using either your {telosAccountName} or {avatarId} and earn karma.");
                //Console.WriteLine("    seeds qrcode                                  {telosAccountName/avatarId}               Generate a sign-in QR code using either your {telosAccountName} or {avatarId}.");
                //Console.WriteLine("    data save                                     {key} {value}                             Saves data for the given {key} and {value} to the currently beamed in avatar.");
                //Console.WriteLine("    data load                                     {key}                                     Loads data for the given {key} for the currently beamed in avatar.");
                //Console.WriteLine("    data delete                                   {key}                                     Deletes data for the given {key} for the currently beamed in avatar.");
                //Console.WriteLine("    data list                                                                               Lists all data for the currently beamed in avatar.");
                //Console.WriteLine("    map setprovider                               {mapProviderType}                         Sets the currently {mapProviderType}.");
                //Console.WriteLine("    map draw3dobject                              {3dObjectPath} {x} {y}                    Draws a 3D object on the map at {x/y} co-ordinates for the given file {3dobjectPath}.");
                //Console.WriteLine("    map draw2dsprite                              {2dSpritePath} {x} {y}                    Draws a 2d sprite on the map at {x/y} co-ordinates for the given file {2dSpritePath}.");
                //Console.WriteLine("    map draw2dspriteonhud                         {2dSpritePath}                            Draws a 2d sprite on the HUD for the given file {2dSpritePath}.");
                //Console.WriteLine("    map placeHolon                                {Holon id/name} {x} {y}                   Place the holon on the map.");
                //Console.WriteLine("    map placeBuilding                             {Building id/name} {x} {y}                Place the building on the map.");
                //Console.WriteLine("    map placeQuest                                {Quest id/name} {x} {y}                   Place the Quest on the map.");
                //Console.WriteLine("    map placeGeoNFT                               {GeoNFT id/name} {x} {y}                  Place the GeoNFT on the map.");
                //Console.WriteLine("    map placeGeoHotSpot                           {GeoHotSpot id/name} {x} {y}              Place the GeoHotSpot on the map.");
                //Console.WriteLine("    map placeOAPP                                 {OAPP id/name} {x} {y}                    Place the OAPP on the map.");
                //Console.WriteLine("    map pamLeft                                                                             Pam the map left.");
                //Console.WriteLine("    map pamRight                                                                            Pam the map right.");
                //Console.WriteLine("    map pamUp                                                                               Pam the map left.");
                //Console.WriteLine("    map pamDown                                                                             Pam the map down.");
                //Console.WriteLine("    map zoomOut                                                                             Zoom the map out.");
                //Console.WriteLine("    map zoomIn                                                                              Zoom the map in.");
                //Console.WriteLine("    map zoomToHolon                               {GeoNFT id/name}                          Zoom the map to the location of the given holon.");
                //Console.WriteLine("    map zoomToBuilding                            {GeoNFT id/name}                          Zoom the map to the location of the given building.");
                //Console.WriteLine("    map zoomToQuest                               {GeoNFT id/name}                          Zoom the map to the location of the given quest.");
                //Console.WriteLine("    map zoomToGeoNFT                              {GeoNFT id/name}                          Zoom the map to the location of the given GeoNFT.");
                //Console.WriteLine("    map zoomToGeoHotSpot                          {GeoHotSpot id/name}                      Zoom the map to the location of the given GeoHotSpot.");
                //Console.WriteLine("    map zoomToOAPP                                {OAPP id/name}                            Zoom the map to the location of the given OAPP.");
                //Console.WriteLine("    map zoomToCoOrds                              {x} {y}                                   Zoom the map to the location of the given {x} and {y} coordinates.");
                //Console.WriteLine("    map drawRouteOnMap                            {startX} {startY} {endX} {endY}           Draw a route on the map.");
                //Console.WriteLine("    map drawRouteBetweenHolons                    {fromHolonId} {toHolonId}                 Draw a route on the map between the two holons.");
                //Console.WriteLine("    map drawRouteBetweenBuildings                 {fromBuildingId} {toBuildingId}           Draw a route on the map between the two buildings.");
                //Console.WriteLine("    map drawRouteBetweenQuests                    {fromQuestId} {toQuestId}                 Draw a route on the map between the two quests.");
                //Console.WriteLine("    map drawRouteBetweenGeoNFTs                   {fromGeoNFTId} {ToGeoNFTId}               Draw a route on the map between the two GeoNFTs.");
                //Console.WriteLine("    map drawRouteBetweenGeoHotSpots               {fromGeoHotSpotId} {ToGeoHotSpotId}       Draw a route on the map between the two GeoHotSpots.");
                //Console.WriteLine("    map drawRouteBetweenOAPPs                     {fromOAPP id/name} {ToOAPP id/name}       Draw a route on the map between the two OAPPs.");
                //Console.WriteLine("    oland price                                                                             Get the currently OLAND price.");
                //Console.WriteLine("    oland purchase                                                                          Purchase OLAND for Our World/OASIS.");
                //Console.WriteLine("    oland load                                    {id}                                      Load a OLAND for the given {id}.");
                //Console.WriteLine("    oland save                                                                              Save a OLAND.");
                //Console.WriteLine("    oland delete                                  {id}                                      Deletes a OLAND for the given {id}.");
                //Console.WriteLine("    oland list                                    [allVersions] [forAllAvatars]             If [all] is omitted it will list all OLAND for the given beamed in avatar, otherwise it will list all OLAND for all avatars.");
                //Console.WriteLine("    onode start                                                                             Starts a OASIS Node (ONODE) and registers it on the OASIS Network (ONET).");
                //Console.WriteLine("    onode stop                                                                              Stops a OASIS Node (ONODE).");
                //Console.WriteLine("    onode status                                                                            Shows stats for this ONODE.");
                //Console.WriteLine("    onode config                                                                            Opens the ONODE's OASISDNA to allow changes to be made (you will need to stop and start the ONODE for changes to apply).");
                //Console.WriteLine("    onode providers                                                                         Shows what OASIS Providers are running for this ONODE.");
                //Console.WriteLine("    onode startprovider                           {ProviderName}                            Starts a given provider.");
                //Console.WriteLine("    onode stopprovider                            {ProviderName}                            Stops a given provider.");
                //Console.WriteLine("    hypernet start                                                                          Starts the HoloNET P2P HyperNET Service.");
                //Console.WriteLine("    hypernet stop                                                                           Stops the HoloNET P2P HyperNET Service.");
                //Console.WriteLine("    hypernet status                                                                         Shows stats for the HoloNET P2P HyperNET Service.");
                //Console.WriteLine("    hypernet config                                                                         Opens the HyperNET's DNA to allow changes to be made (you will need to stop and start the HyperNET Service for changes to apply.");
                //Console.WriteLine("    onet status                                                                             Shows stats for the OASIS Network (ONET).");
                //Console.WriteLine("    onet providers                                                                          Shows what OASIS Providers are running across the ONET and on what ONODE's.");
                //Console.WriteLine("    config cosmicdetailedoutput                   {enable/disable/status}                   Enables/disables COSMIC Detailed Output.");
                //Console.WriteLine("    config starstatusdetailedoutput               {enable/disable/status}                   Enables/disables STAR ODK Detailed Output.");
                //Console.WriteLine("    runcosmictests                                {OAPPType} {dnaFolder} {geneisFolder}     Run the STAR ODK/COSMIC Tests... If OAPPType, DNAFolder or GenesisFolder are not specified it will use the defaults.");
                //Console.WriteLine("    runoasisapitests                                                                        Run the OASIS API Tests...");

                Console.WriteLine("");
                //Console.WriteLine(" NOTES: -  is not needed if using the STAR CLI Console directly. Star is only needed if calling from the command line or another external script ( is simply the name of the exe).");
                Console.WriteLine(" NOTES:");
                Console.WriteLine("        When invoking any sub-commands that take a {id} or {name}, if neither is specified then a wizard will launch to help find the correct item.");
                Console.WriteLine("        In some cases, sub-commands may only list {id} as a param to save space but these also accept the {name}.");
                Console.WriteLine("        When invoking any sub-commands that have an optional [all] argument/flag, if it is omitted it will search only your items, otherwise it will search all published items as well as yours.");
                Console.WriteLine("        When invoking any sub-commands that have an optional [detailed] argument/flag, if it is included it will show detailed information for that item (such as show and list).");
                Console.WriteLine("        If you invoke the update, delete, list, show or search sub-command with [web4] param it will update/delete/list/show/search WEB4 OASIS Geo-NFT's/NFT's otherwise it will update/delete/list/show/search WEB5 STAR Geo-NFT's/NFT's.");
                Console.WriteLine("        If you invoke the create, update, delete, list, show or search sub-command with [web4] param it will create/update/delete/list/show/search WEB4 OASIS Geo-NFT/NFT Collection's otherwise it will create/update/delete/list/show/search WEB5 STAR Geo-NFT/NFT Collection's.");
                Console.WriteLine("        If you invoke a sub-command without any arguments it will show more detailed help on how to use that sub-command as well as the option to lanuch any wizards to help guide you.");
            }
            else
            {
                Console.WriteLine("    light                   Generate a OAPP.");
                Console.WriteLine("    bang                    Generate a whole metaverse or part of one such as Multierveres, Universes, Dimensions, Galaxy Clusters, Galaxies, Solar Systems, Stars, Planets, Moons etc.");
                Console.WriteLine("    wiz                     Start the STAR ODK Wizard which will walk you through the steps for creating a OAPP tailored to your specefic needs (such as which OASIS Providers do you need and the specefic use case(s) you need etc).");
                Console.WriteLine("    flare                   Build a OAPP.");
                Console.WriteLine("    shine                   Launch & activate a OAPP by shining the 's light upon it..."); //TODO: Dev next.
                Console.WriteLine("    twinkle                 Activate a published OAPP within the STARNET store."); //TODO: Dev next.
                Console.WriteLine("    dim                     Deactivate a published OAPP within the STARNET store."); //TODO: Dev next.
                Console.WriteLine("    seed                    Deploy/Publish a OAPP to the STARNET Store.");
                Console.WriteLine("    unseed                  Undeploy/Unpublish a OAPP from the STARNET Store.");
                Console.WriteLine("    dust                    Delete a OAPP (this will also remove it from STARNET if it has already been published)."); //TODO: Dev next.
                Console.WriteLine("    radiate                 Highlight the OAPP in the STARNET Store. *Admin/Wizards Only*");
                Console.WriteLine("    emit                    Show how much light the OAPP is emitting into the solar system (this is determined by the collective karma score of all users of that OAPP).");
                Console.WriteLine("    reflect                 Show stats of the OAPP.");
                Console.WriteLine("    evolve                  Upgrade/update a OAPP)."); //TODO: Dev next.
                Console.WriteLine("    mutate                  Import/Export hApp, dApp & others.");
                Console.WriteLine("    love                    Send/Receive Love.");
                Console.WriteLine("    burst                   View network stats/management/settings.");
                Console.WriteLine("    super                   Reserved For Future Use...");
                //Console.WriteLine("    net = Launch the STARNET Library/Store where you can list, search, update, publish, unpublish, install & uninstall OAPP's, zomes, holons, celestial spaces, celestial bodies, geo-nft's, geo-hotspots, missions, chapters, quests & inventory items.");
                Console.WriteLine("    net                     Launch the STARNET Library/Store where you can list, search, update, publish, unpublish, install & uninstall OAPP's & more!");
                Console.WriteLine("    gate                    Opens the STARGATE to the OASIS Portal!");
                Console.WriteLine("    api [oasis]             Opens the WEB5 STAR API (if oasis is included then it will open the WEB4 OASIS API instead).");
                Console.WriteLine("    avatar                  Manage avatars.");
                Console.WriteLine("    karma                   Manage karma.");
                Console.WriteLine("    keys                    Manage keys.");
                Console.WriteLine("    wallet                  Manage wallets.");
                Console.WriteLine("    search                  Search the OASIS.");

                DisplaySTARNETHolonCommandSummaries("oapp");
                DisplaySTARNETHolonCommandSummaries("oapp template");
                DisplaySTARNETHolonCommandSummaries("runtime");
                DisplaySTARNETHolonCommandSummaries("lib");
                DisplaySTARNETHolonCommandSummaries("celestialspace");
                DisplaySTARNETHolonCommandSummaries("celestialbody");
                DisplaySTARNETHolonCommandSummaries("zome");
                DisplaySTARNETHolonCommandSummaries("holon");
                DisplaySTARNETHolonCommandSummaries("chapter");
                DisplaySTARNETHolonCommandSummaries("mission");
                DisplaySTARNETHolonCommandSummaries("quest");
                DisplaySTARNETHolonCommandSummaries("nft");
                DisplaySTARNETHolonCommandSummaries("nft collection");
                DisplaySTARNETHolonCommandSummaries("geonft");
                DisplaySTARNETHolonCommandSummaries("geonft collection");
                DisplaySTARNETHolonCommandSummaries("geohotspot");
                DisplaySTARNETHolonCommandSummaries("inventoryitem");
                DisplaySTARNETHolonCommandSummaries("plugin");


                //Console.WriteLine("    oapp                    Create, edit, delete, publish, unpublish, install, uninstall, list & show OAPP's.");
                //Console.WriteLine("    oapp template           Create, edit, delete, publish, unpublish, install, uninstall, list & show OAPP Templates.");
                //Console.WriteLine("    happ                    Create, edit, delete, publish, unpublish, install, uninstall, list & show hApp's.");
                //Console.WriteLine("    runtime                 Create, edit, delete, publish, unpublish, install, uninstall, list & show runtime's.");
                //Console.WriteLine("    lib                     Create, edit, delete, publish, unpublish, install, uninstall, list & show libraries.");
                //Console.WriteLine("    celestialspace          Create, edit, delete, publish, unpublish, list & show celestial space's.");
                //Console.WriteLine("    celestialbody           Create, edit, delete, publish, unpublish, list & show celestial bodies's.");
                //Console.WriteLine("    celestialbody metadata  Create, edit, delete, publish, unpublish, list & show celestial body metadata.");
                //Console.WriteLine("    zome                    Create, edit, delete, publish, unpublish, list & show zome's.");
                //Console.WriteLine("    zome metadata           Create, edit, delete, publish, unpublish, list & show zome metadata.");
                //Console.WriteLine("    holon                   Create, edit, delete, publish, unpublish, list & show holon's.");
                //Console.WriteLine("    holon metadata          Create, edit, delete, publish, unpublish, list & show holon metadata.");
                //Console.WriteLine("    chapter                 Create, edit, delete, publish, unpublish, list & show chapter's.");
                //Console.WriteLine("    mission                 Create, edit, delete, publish, unpublish, list & show mission's.");
                //Console.WriteLine("    quest                   Create, edit, delete, publish, unpublish, list & show quest's.");
                //Console.WriteLine("    nft                     Mint, send, edit, burn, publish, unpublish, list & show nft's.");
                //Console.WriteLine("    nft collection          Work with nft collections.");
                //Console.WriteLine("    geonft                  Mint, place, edit, burn, publish, unpublish, list & show geo-nft's.");
                //Console.WriteLine("    geonft collection       Work with geo-nft collections.");
                //Console.WriteLine("    geohotspot              Create, edit, delete, publish, unpublish, list & show geo-hotspot's.");
                //Console.WriteLine("    inventoryitem           Create, edit, delete, publish, unpublish, list & show inventory item's.");
                //Console.WriteLine("    plugin                  Create, edit, delete, publish, unpublish, list & show plugin item's.");
                Console.WriteLine("    seeds                   Access the SEEDS API.");
                Console.WriteLine("    data                    Access the Data API.");
                Console.WriteLine("    map                     Access the Map API.");
                Console.WriteLine("    oland                   Access the OLAND (Virtual Land) API.");
                Console.WriteLine("    onode                   Manage this ONODE (OASIS Node) such as start, stop, view status, edit config, view providers, start & stop providers.");
                Console.WriteLine("    hypernet                Start, stop & view status for the HoloNET P2P HyperNET Service.");
                Console.WriteLine("    onet                    View the status for the ONET (OASIS Network).");
                Console.WriteLine("    config                  Enables/disables COSMIC detailed output & STAR ODK detailed output.");
                Console.WriteLine("    runcosmictests          Run the STAR ODK/COSMIC tests.");
                Console.WriteLine("    runoasisapitests        Run the OASIS API tests.");

                Console.WriteLine("");
                //Console.WriteLine(" NOTES: -  is not needed if using the STAR CLI Console directly. Star is only needed if calling from the command line or another external script ( is simply the name of the exe).");
                Console.WriteLine(" NOTES:");
                Console.WriteLine("        When invoking any sub-commands that take a {id} or {name}, if neither is specified then a wizard will launch to help find the correct item.");
                Console.WriteLine("        In some cases, sub-commands may only list {id} as a param to save space but these also accept the {name}.");
                Console.WriteLine("        When invoking any sub-commands that have an optional [all] argument/flag, if it is omitted it will search only your items, otherwise it will search all published items as well as yours.");
                Console.WriteLine("        When invoking any sub-commands that have an optional [detailed] argument/flag, if it is included it will show detailed information for that item (such as show and list).");
                Console.WriteLine("        If you invoke the list, show or search sub-command with [web4] param it will list/show/search WEB4 OASIS Geo-NFT's/NFT's otherwise it will list/show/search WEB5 STAR Geo-NFT's/NFT's..");
                Console.WriteLine("        If you invoke a sub-command without any arguments it will show more detailed help on how to use that sub-command as well as the option to lanuch any wizards to help guide you.");
            }

            Console.WriteLine("************************************************************************************************");
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private static void STAR_OnInitialized(object sender, System.EventArgs e)
        {
            CLIEngine.ShowSuccessMessage(" STAR Initialized.");
        }

        private static void STAR_OnOASISBootError(object sender, OASISBootErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage(e.ErrorReason);
        }

        private static void STAR_OnOASISBooted(object sender, EventArgs.OASISBootedEventArgs e)
        {
            // CLIEngine.ShowSuccessMessage(string.Concat("OASIS BOOTED.", e.Message));
        }

        private static void STAR_OnStarError(object sender, EventArgs.StarErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage(string.Concat("Error Igniting SuperStar. Reason: ", e.Reason));
        }

        private static void STAR_OnStarIgnited(object sender, System.EventArgs e)
        {
            //CLIEngine.ShowSuccessMessage("STAR IGNITED");
        }

        private static void STAR_OnStarStatusChanged(object sender, EventArgs.StarStatusChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                switch (e.MessageType)
                {
                    case Enums.StarStatusMessageType.Processing:
                        CLIEngine.ShowWorkingMessage(e.Message);
                        break;

                    case Enums.StarStatusMessageType.Success:
                        CLIEngine.ShowSuccessMessage(e.Message);
                        break;

                    case Enums.StarStatusMessageType.Error:
                        CLIEngine.ShowErrorMessage(e.Message);
                        break;
                }
            }
            else
            {
                switch (e.Status)
                {
                    case Enums.StarStatus.BootingOASIS:
                    //CLIEngine.ShowWorkingMessage("BOOTING OASIS...");
                    //break;

                    case Enums.StarStatus.OASISBooted:
                        //CLIEngine.ShowSuccessMessage("OASIS BOOTED"); //OASISBootLoader already shows this message so no need to show again! ;-)
                        break;

                    case Enums.StarStatus.Igniting:
                        CLIEngine.ShowWorkingMessage("IGNITING STAR...");
                        break;

                    case Enums.StarStatus.Ignited:
                        CLIEngine.ShowSuccessMessage("STAR IGNITED");
                        break;

                        //case Enums.SuperStarStatus.Error:
                        //  CLIEngine.ShowErrorMessage("SuperStar Error");
                }
            }
        }

        private static void STAR_OnCelestialSpacesLoaded(object sender, CelestialSpacesLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"CelesitalSpaces Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialSpacesSaved(object sender, CelestialSpacesSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"CelesitalSpaces Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialSpacesError(object sender, CelestialSpacesErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Error occurred loading/saving CelestialSpaces. Reason: {e.Reason}");
        }

        private static void STAR_OnCelestialSpaceLoaded(object sender, CelestialSpaceLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"CelesitalSpace Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialSpaceSaved(object sender, CelestialSpaceSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"CelesitalSpace Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialSpaceError(object sender, CelestialSpaceErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Error occurred loading/saving CelestialSpace. Reason: {e.Reason}");
        }

        private static void STAR_OnCelestialBodyLoaded(object sender, CelestialBodyLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"CelesitalBody Loaded Successfully. {detailedMessage}");
        }
        private static void STAR_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            //CLIEngine.ShowSuccessMessage($"CelesitalBody Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Error occurred loading/saving CelestialBody. Reason: {e.Reason}");
        }

        private static void STAR_OnCelestialBodiesLoaded(object sender, CelestialBodiesLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"CelesitalBodies Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialBodiesSaved(object sender, CelestialBodiesSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"CelesitalBodies Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnCelestialBodiesError(object sender, CelestialBodiesErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Error occurred loading/saving CelestialBodies. Reason: {e.Reason}");
        }

        private static void STAR_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"Zome Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"Zome Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Error occurred loading/saving Zome. Reason: {e.Reason}");
            //Console.WriteLine(string.Concat("Star Error Occurred. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNETErrorDetails.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNETErrorDetails.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
            //CLIEngine.ShowErrorMessage(string.Concat(" STAR Error Occurred. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void STAR_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"Zome Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"Zome Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Error occurred loading/saving Zomes. Reason: {e.Reason}");
        }

        private static void STAR_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            CLIEngine.ShowSuccessMessage(string.Concat(" STAR Holons Loaded. Holon Name: ", e.Result.Result.Name));
        }

        private static void STAR_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            if (e.Result.IsError)
                CLIEngine.ShowErrorMessage(e.Result.Message);
            else
                CLIEngine.ShowSuccessMessage(string.Concat("STAR Holons Saved. Holon Saved: ", e.Result.Result.Name));
        }

        private static void STAR_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Error occurred loading/saving Holon. Reason: {e.Reason}");
        }

        private static void STAR_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            CLIEngine.ShowSuccessMessage(string.Concat(" STAR Holons Loaded. Holons Loaded: ", e.Result.Result.Count()));
        }

        private static void STAR_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"Holons Saved Successfully. {detailedMessage}");
        }

        private static void STAR_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Error occurred loading/saving Holons. Reason: {e.Reason}");
        }

        private static void StarCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage($"Error occurred loading/saving Zome For StarCore. Reason: {e.Reason}");
            //Console.WriteLine(string.Concat("Star Core Error Occurred. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNETErrorDetails.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNETErrorDetails.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
            //CLIEngine.ShowErrorMessage(string.Concat(" Star Core Error Occurred. EndPoint: ", e.EndPoint, ". Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void DisplayCommand(string command, string args, string desc, int indent = 4, int commandColSize = 48, int argsColSize = 52)
        {
            Console.WriteLine(string.Concat("".PadRight(indent), command.PadRight(commandColSize), args.PadRight(argsColSize), desc));
        }

        private static void DisplaySTARNETHolonCommands(string holonType, string createParams = "", string createDesc = "", string updateParams = "", string updateDesc = "", string cloneParams = "", string cloneDesc = "", string addDependencyParams = "", string addDependencyDesc = "", string removeDependencyParams = "", string removeDependencyDesc = "", string deleteParams = "", string deleteDesc = "", string publishParams = "", string publishDesc = "", string unpublishParams = "", string unpublishDesc = "", string republishParams = "", string republishDesc = "", string activateParams = "", string activateDesc = "", string deactivateParams = "", string deactivateDesc = "", string downloadParams = "", string downloadDesc = "", string installParams = "", string installDesc = "", string uninstallParams = "", string uninstallDesc = "", string reinstallParams = "", string reinstallDesc = "", string showParams = "", string showDesc = "", string listParams = "", string listDesc = "", string listInstalledParams = "", string listInstalledDesc = "", string listUninstalledParams = "", string listUninstalledDesc = "", string listUnpublishedParams = "", string listUnpublishedDesc = "", string listDeactivatedParams = "", string listDeactivatedDesc = "", string searchParams = "", string searchDesc = "")
        {
            string web4Param = "";

            if (holonType == "nft collection" || holonType == "geonft collection")
                web4Param = " [web4]";

            DisplayCommand(string.Concat(holonType, " create"), !string.IsNullOrEmpty(createParams) ? createParams : web4Param, !string.IsNullOrEmpty(createDesc) ? createDesc : $"Create a new {holonType}.");
            DisplayCommand(string.Concat(holonType, " update"), !string.IsNullOrEmpty(updateParams) ? updateParams : string.Concat("{id/name}", web4Param), !string.IsNullOrEmpty(updateDesc) ? updateDesc : string.Concat("Updates an existing ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " clone"), !string.IsNullOrEmpty(cloneParams) ? cloneParams : "{id/name}", !string.IsNullOrEmpty(cloneDesc) ? cloneDesc : string.Concat("Clones an existing ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " adddependency"), !string.IsNullOrEmpty(addDependencyDesc) ? addDependencyDesc : "{id/name}", !string.IsNullOrEmpty(addDependencyDesc) ? addDependencyDesc : string.Concat("Adds a dependency to an existing ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " removedependency"), !string.IsNullOrEmpty(removeDependencyParams) ? removeDependencyParams : "{id/name}", !string.IsNullOrEmpty(removeDependencyDesc) ? removeDependencyDesc : string.Concat("Removes a dependency from an existing ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " delete"), !string.IsNullOrEmpty(deleteParams) ? deleteParams : string.Concat("{id/name}", web4Param), !string.IsNullOrEmpty(deleteDesc) ? deleteDesc : !string.IsNullOrEmpty(deleteDesc) ? deleteDesc : string.Concat("Deletes a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " publish"), !string.IsNullOrEmpty(publishParams) ? publishParams : "{id/name}", !string.IsNullOrEmpty(publishDesc) ? publishDesc : string.Concat("Publishes a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " unpublish"), !string.IsNullOrEmpty(unpublishParams) ? unpublishParams : "{id/name}", !string.IsNullOrEmpty(unpublishDesc) ? unpublishDesc : string.Concat("Unpublishes a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " republish"), !string.IsNullOrEmpty(republishParams) ? republishParams : "{id/name}", !string.IsNullOrEmpty(republishDesc) ? republishDesc : string.Concat("Republish a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " activate"), !string.IsNullOrEmpty(activateParams) ? activateParams : "{id/name}", !string.IsNullOrEmpty(activateDesc) ? activateDesc : string.Concat("Activate a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " deactivate"), !string.IsNullOrEmpty(deactivateParams) ? deactivateParams : "{id/name}", !string.IsNullOrEmpty(deactivateDesc) ? deactivateDesc : string.Concat("Deactivate a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " download"), !string.IsNullOrEmpty(downloadParams) ? downloadParams : "{id/name}", !string.IsNullOrEmpty(downloadDesc) ? downloadDesc : string.Concat("Download a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " install"), !string.IsNullOrEmpty(installParams) ? installParams : "{id/name}", !string.IsNullOrEmpty(installDesc) ? installDesc : string.Concat("Install/Download a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " uninstall"), !string.IsNullOrEmpty(uninstallParams) ? uninstallParams : "{id/name}", !string.IsNullOrEmpty(uninstallDesc) ? uninstallDesc : string.Concat("Uninstall a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " reinstall"), !string.IsNullOrEmpty(reinstallParams) ? reinstallParams : "{id/name}", !string.IsNullOrEmpty(reinstallDesc) ? reinstallDesc : string.Concat("Reinstall a ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " show"), !string.IsNullOrEmpty(showParams) ? showParams : string.Concat("{id/name} [detailed]", web4Param), !string.IsNullOrEmpty(showDesc) ? showDesc : string.Concat("Shows a  ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " list"), !string.IsNullOrEmpty(listParams) ? listParams : string.Concat("[allVersions] [forAllAvatars] [detailed]", web4Param), !string.IsNullOrEmpty(listDesc) ? listDesc : string.Concat("List all  ", holonType, " that have been generated."));
            DisplayCommand(string.Concat(holonType, " list installed"), !string.IsNullOrEmpty(listInstalledParams) ? listInstalledParams : "", !string.IsNullOrEmpty(listInstalledDesc) ? listInstalledDesc : string.Concat("List all ", holonType, "'s installed for the currently beamed in avatar."));
            DisplayCommand(string.Concat(holonType, " list uninstalled"), !string.IsNullOrEmpty(listUninstalledParams) ? listUninstalledParams : "", !string.IsNullOrEmpty(listUninstalledDesc) ? listUninstalledDesc : string.Concat("List all ", holonType, "'s uninstalled for the currently beamed in avatar (and allow re-install)."));
            DisplayCommand(string.Concat(holonType, " list unpublished"), !string.IsNullOrEmpty(listUnpublishedParams) ? listUnpublishedParams : "", !string.IsNullOrEmpty(listUnpublishedDesc) ? listUnpublishedDesc : string.Concat("List all ", holonType, "'s unpublished for the currently beamed in avatar (and allow republish)."));
            DisplayCommand(string.Concat(holonType, " list deactivated"), !string.IsNullOrEmpty(listDeactivatedParams) ? listDeactivatedParams : "", !string.IsNullOrEmpty(listDeactivatedDesc) ? listDeactivatedDesc : string.Concat("List all ", holonType, "'s deactivated for the currently beamed in avatar (and allow reactivate)."));
            DisplayCommand(string.Concat(holonType, " search"), !string.IsNullOrEmpty(searchParams) ? searchParams : string.Concat("[allVersions] [forAllAvatars]", web4Param), !string.IsNullOrEmpty(searchDesc) ? searchDesc : string.Concat("Searches the ", holonType, "'s for the given search criteria."));
        }

        private static void DisplaySTARNETHolonCommandSummaries(string holonType)
        {
            DisplayCommand(holonType, "", $" Create, edit, clone delete, publish, unpublish, install, uninstall, list & show {holonType}'s.");
        }

        #region ONET/ONODE CLI Implementation

        private static ONETManager? _onetManager;
        //private static ONETProtocol? _onetProtocol;
        private static ONETDiscovery? _onetDiscovery;

        private static async Task InitializeONETAsync()
        {
            try
            {
                if (_onetManager == null)
                {
                    CLIEngine.ShowWorkingMessage("Initializing ONET (OASIS Network)...");
                    _onetManager = new ONETManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    //_onetProtocol = new ONETProtocol(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    _onetDiscovery = new ONETDiscovery(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    await _onetDiscovery.InitializeAsync();
                    CLIEngine.ShowSuccessMessage("ONET initialized successfully");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error initializing ONET: {ex.Message}");
            }
        }

        #region ONODE Commands

        private static async Task StartONODEAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Starting ONODE...");
                
                var result = await _onetManager!.StartNetworkAsync();
                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to start ONODE: {result.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage($"ONODE started successfully. Node ID: {result.Result}");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error starting ONODE: {ex.Message}");
            }
        }

        private static async Task StopONODEAsync()
        {
            try
            {
                if (_onetManager == null)
                {
                    CLIEngine.ShowErrorMessage("ONODE is not running");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Stopping ONODE...");
                var result = await _onetManager.StopNetworkAsync();
                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to stop ONODE: {result.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("ONODE stopped successfully");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error stopping ONODE: {ex.Message}");
            }
        }

        private static async Task ShowONODEStatusAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Getting ONODE status...");

                var statusResult = await _onetManager!.GetNetworkStatusAsync();
                if (statusResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to get ONODE status: {statusResult.Message}");
                    return;
                }

            var status = statusResult.Result;
            Console.WriteLine();
            CLIEngine.ShowMessage("=== ONODE STATUS ===", ConsoleColor.Green);
            CLIEngine.ShowMessage($"Network ID: {status.NetworkId}", ConsoleColor.White);
            CLIEngine.ShowMessage($"Is Running: {status.IsRunning}", ConsoleColor.White);
            CLIEngine.ShowMessage($"Connected Nodes: {status.ConnectedNodes}", ConsoleColor.White);
            CLIEngine.ShowMessage($"Network Health: {status.NetworkHealth:P1}", ConsoleColor.White);
            CLIEngine.ShowMessage($"Last Activity: {status.LastActivity}", ConsoleColor.White);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONODE status: {ex.Message}");
            }
        }

        private static async Task OpenONODEConfigAsync()
        {
            try
            {
                CLIEngine.ShowWorkingMessage("Opening ONODE configuration...");
                
                var configPath = Path.Combine(Environment.CurrentDirectory, "OASISDNA.json");
                if (File.Exists(configPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = configPath,
                        UseShellExecute = true
                    });
                    CLIEngine.ShowSuccessMessage("ONODE configuration opened in default editor");
                }
                else
                {
                    CLIEngine.ShowErrorMessage("OASISDNA.json configuration file not found");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error opening ONODE configuration: {ex.Message}");
            }
        }

        private static async Task ShowONODEProvidersAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Getting ONODE providers...");

            // Get network stats instead of providers (providers method doesn't exist)
            var statsResult = await _onetManager!.GetNetworkStatsAsync();
            if (statsResult.IsError)
            {
                CLIEngine.ShowErrorMessage($"Failed to get ONODE stats: {statsResult.Message}");
                return;
            }

            var stats = statsResult.Result;
            Console.WriteLine();
            CLIEngine.ShowMessage("=== ONODE STATS ===", ConsoleColor.Green);
            
            foreach (var stat in stats)
            {
                CLIEngine.ShowMessage($"• {stat.Key}: {stat.Value}", ConsoleColor.White);
            }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONODE providers: {ex.Message}");
            }
        }

        private static async Task StartONODEProviderAsync(string providerName)
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage($"Starting provider: {providerName}...");

            // Provider management not implemented in ONETManager
            CLIEngine.ShowErrorMessage($"Provider management not implemented for {providerName}");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error starting provider {providerName}: {ex.Message}");
            }
        }

        private static async Task StopONODEProviderAsync(string providerName)
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage($"Stopping provider: {providerName}...");

            // Provider management not implemented in ONETManager
            CLIEngine.ShowErrorMessage($"Provider management not implemented for {providerName}");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error stopping provider {providerName}: {ex.Message}");
            }
        }

        #endregion

        #region ONET Commands

        private static async Task ShowONETStatusAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Getting ONET network status...");

                var statusResult = await _onetManager!.GetNetworkStatusAsync();
                if (statusResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to get ONET status: {statusResult.Message}");
                    return;
                }

                var status = statusResult.Result;
                Console.WriteLine();
                CLIEngine.ShowMessage("=== ONET NETWORK STATUS ===", ConsoleColor.Green);
                CLIEngine.ShowMessage($"Is Running: {status.IsRunning}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Connected Nodes: {status.ConnectedNodes}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Network Health: {status.NetworkHealth:P1}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Network ID: {status.NetworkId}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Last Activity: {status.LastActivity}", ConsoleColor.White);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONET status: {ex.Message}");
            }
        }

        private static async Task ShowONETProvidersAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Getting ONET network providers...");

                // Get network stats instead of providers (providers method doesn't exist)
                var statsResult = await _onetManager!.GetNetworkStatsAsync();
                if (statsResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to get ONET stats: {statsResult.Message}");
                    return;
                }

                var stats = statsResult.Result;
                Console.WriteLine();
                CLIEngine.ShowMessage("=== ONET NETWORK STATS ===", ConsoleColor.Green);
                
                foreach (var stat in stats)
                {
                    CLIEngine.ShowMessage($"• {stat.Key}: {stat.Value}", ConsoleColor.White);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONET providers: {ex.Message}");
            }
        }

        private static async Task DiscoverONETNodesAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Discovering ONET nodes...");

                var discoveryResult = await _onetDiscovery!.DiscoverAvailableNodesAsync();
                if (discoveryResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to discover nodes: {discoveryResult.Message}");
                    return;
                }

                var nodes = discoveryResult.Result;
                Console.WriteLine();
                CLIEngine.ShowMessage("=== DISCOVERED ONET NODES ===", ConsoleColor.Green);
                
                if (nodes.Any())
                {
                    foreach (var node in nodes)
                    {
                        CLIEngine.ShowMessage($"• {node.Id} - {node.Address}", ConsoleColor.White);
                        CLIEngine.ShowMessage($"  Status: {node.Status} | Latency: {node.Latency}ms | Reliability: {node.Reliability}%", ConsoleColor.Gray);
                        CLIEngine.ShowMessage($"  Capabilities: {string.Join(", ", node.Capabilities)}", ConsoleColor.Gray);
                    }
                }
                else
                {
                    CLIEngine.ShowMessage("No ONET nodes discovered", ConsoleColor.Yellow);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error discovering ONET nodes: {ex.Message}");
            }
        }

        private static async Task ConnectToONETNodeAsync(string nodeAddress)
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage($"Connecting to ONET node: {nodeAddress}...");

                var result = await _onetManager!.ConnectToNodeAsync(nodeAddress, nodeAddress);
                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to connect to node {nodeAddress}: {result.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage($"Successfully connected to ONET node: {nodeAddress}");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error connecting to ONET node {nodeAddress}: {ex.Message}");
            }
        }

        private static async Task DisconnectFromONETNodeAsync(string nodeAddress)
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage($"Disconnecting from ONET node: {nodeAddress}...");

                var result = await _onetManager!.DisconnectFromNodeAsync(nodeAddress);
                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to disconnect from node {nodeAddress}: {result.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage($"Successfully disconnected from ONET node: {nodeAddress}");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error disconnecting from ONET node {nodeAddress}: {ex.Message}");
            }
        }

        private static async Task ShowONETTopologyAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Getting ONET network topology...");

                var topologyResult = await _onetManager!.GetNetworkTopologyAsync();
                if (topologyResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to get network topology: {topologyResult.Message}");
                    return;
                }

                var topology = topologyResult.Result;
                Console.WriteLine();
                CLIEngine.ShowMessage("=== ONET NETWORK TOPOLOGY ===", ConsoleColor.Green);
                CLIEngine.ShowMessage($"Total Nodes: {topology.Nodes.Count}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Connections: {topology.Connections.Count}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Last Updated: {topology.LastUpdated}", ConsoleColor.White);
                
                if (topology.Nodes.Any())
                {
                    CLIEngine.ShowMessage("\nNodes:", ConsoleColor.Yellow);
                    foreach (var node in topology.Nodes)
                    {
                        CLIEngine.ShowMessage($"• {node.Id} - {node.Address} (Status: {node.Status})", ConsoleColor.Gray);
                    }
                }
                
                if (topology.Connections.Any())
                {
                    CLIEngine.ShowMessage("\nConnections:", ConsoleColor.Yellow);
                    foreach (var connection in topology.Connections)
                    {
                        CLIEngine.ShowMessage($"• {connection.FromNodeId} ↔ {connection.ToNodeId} (Latency: {connection.Latency}ms)", ConsoleColor.Gray);
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONET topology: {ex.Message}");
            }
        }

        #endregion

        #endregion
    }
}
