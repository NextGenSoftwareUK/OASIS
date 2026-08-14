using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.CLI.Lib;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    partial class Program
    {
        private static async Task HandleLightCommandAsync(string[] inputArgs, ProviderType providerType, bool shellMode)
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

            if (inputArgs.Length > 1)
            {
                if (inputArgs[1].ToLower() == "wiz")
                {
                    if (CLIEngine.NonInteractive)
                    {
                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                            "Command 'light wiz' is interactive-only. Use `light ./LightRequest.json`, `light json <file>`, or full positional `light` arguments.",
                            "Example: star --non-interactive --json light ./LightRequest.json");
                        if (shellMode)
                            Environment.ExitCode = 2;
                    }
                    else
                        await STARCLI.OAPPs.LightWizardAsync(null);
                }
                else
                {
                    string lightJsonPath = null;
                    bool skipPositionalLight = false;

                    if (inputArgs.Length == 2
                        && string.Equals(Path.GetExtension(inputArgs[1]), ".json", StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(inputArgs[1]))
                            lightJsonPath = inputArgs[1];
                        else
                        {
                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                $"Light JSON file not found: {inputArgs[1]}",
                                "Example: star --non-interactive --json light ./LightRequest.json");
                            if (shellMode)
                                Environment.ExitCode = 2;
                            skipPositionalLight = true;
                        }
                    }
                    else if (string.Equals(inputArgs[1], "json", StringComparison.OrdinalIgnoreCase))
                    {
                        if (inputArgs.Length < 3)
                        {
                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                "light json requires a path to LightRequest JSON.",
                                "Prefer: star --non-interactive --json light ./LightRequest.json");
                            if (shellMode)
                                Environment.ExitCode = 2;
                            skipPositionalLight = true;
                        }
                        else if (!File.Exists(inputArgs[2]))
                        {
                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                $"Light JSON file not found: {inputArgs[2]}",
                                "See Docs/Devs/STAR_CLI_NonInteractive.md.");
                            if (shellMode)
                                Environment.ExitCode = 2;
                            skipPositionalLight = true;
                        }
                        else
                            lightJsonPath = inputArgs[2];
                    }

                    if (lightJsonPath != null)
                    {
                        lightResult = await STARCLI.OAPPs.LightFromJsonFileAsync(lightJsonPath, providerType);
                        if (CLIEngine.JsonOutput)
                        {
                            object lightData = null;
                            if (lightResult != null && !lightResult.IsError && lightResult.Result != null)
                            {
                                lightData = new
                                {
                                    celestialBodyId = lightResult.Result.CelestialBody?.Id,
                                    celestialBodyName = lightResult.Result.CelestialBody?.Name,
                                    oappId = lightResult.Result.OAPP?.STARNETDNA?.Id,
                                    oappName = lightResult.Result.OAPP?.STARNETDNA?.Name
                                };
                            }

                            EmitNiJsonForOasisResult(lightResult, "light", lightData);
                        }
                        else if (lightResult != null)
                        {
                            if (!lightResult.IsError && lightResult.Result != null)
                                CLIEngine.ShowSuccessMessage($"OAPP Successfully Generated. ({lightResult.Message})");
                            else
                                CLIEngine.ShowErrorMessage($"Error Occurred: {lightResult.Message}");
                        }
                    }
                    else if (!skipPositionalLight)
                    {
                        CLIEngine.ShowWorkingMessage("Generating OAPP...");

                        if (inputArgs.Length > 2 && Enum.TryParse(typeof(OAPPType), inputArgs[3], true, out oappTypeObj))
                        {
                            oappType = (OAPPType)oappTypeObj;

                            if (inputArgs.Length > 3 && Guid.TryParse(inputArgs[4], out oappTemplateId))
                            {
                                if (inputArgs.Length > 4 && int.TryParse(inputArgs[5], out oappTemplateVersion))
                                {
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
            }
            else
            {
                if (CLIEngine.NonInteractive)
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                        "Non-interactive mode requires full 'light' arguments, `light ./LightRequest.json`, or `light json <file>`.",
                        "See Docs/Devs/STAR_CLI_NonInteractive.md and existing 'light' positional parameter help in ShowCommands.");
                    if (shellMode)
                        Environment.ExitCode = 2;
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
        }

        private static async Task HandleWizCommandAsync(string[] inputArgs, ProviderType providerType, bool shellMode)
        {
            if (CLIEngine.NonInteractive)
            {
                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, "Command 'wiz' is interactive-only. Use 'light <args>' with full parameters or interactive mode.", null);
                if (shellMode)
                    Environment.ExitCode = 2;
                return;
            }

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

        private static async Task HandleGameCommandAsync(string[] inputArgs, ProviderType providerType)
        {
            if (inputArgs.Length > 1)
            {
                string subCommand = inputArgs[1].ToLower();

                if (subCommand == "start")
                    await ShowGameSessionCommandAsync(inputArgs, "start");
                else if (subCommand == "end")
                    await ShowGameSessionCommandAsync(inputArgs, "end");
                else if (subCommand == "load")
                    await ShowGameSessionCommandAsync(inputArgs, "load");
                else if (subCommand == "unload")
                    await ShowGameSessionCommandAsync(inputArgs, "unload");
                else if (subCommand == "loadlevel")
                    await ShowGameLevelCommandAsync(inputArgs, "loadlevel");
                else if (subCommand == "unloadlevel")
                    await ShowGameLevelCommandAsync(inputArgs, "unloadlevel");
                else if (subCommand == "jumptolevel")
                    await ShowGameLevelCommandAsync(inputArgs, "jumptolevel");
                else if (subCommand == "jumptopoint")
                    await ShowGameLevelCommandAsync(inputArgs, "jumptopoint");
                else if (subCommand == "loadarea")
                    await ShowGameAreaCommandAsync(inputArgs, "loadarea");
                else if (subCommand == "unloadarea")
                    await ShowGameAreaCommandAsync(inputArgs, "unloadarea");
                else if (subCommand == "jumptoarea")
                    await ShowGameAreaCommandAsync(inputArgs, "jumptoarea");
                else if (subCommand == "showtitlescreen")
                    await ShowGameUICommandAsync(inputArgs, "showtitlescreen");
                else if (subCommand == "showmainmenu")
                    await ShowGameUICommandAsync(inputArgs, "showmainmenu");
                else if (subCommand == "showoptions")
                    await ShowGameUICommandAsync(inputArgs, "showoptions");
                else if (subCommand == "showcredits")
                    await ShowGameUICommandAsync(inputArgs, "showcredits");
                else if (subCommand == "setmastervolume")
                    await ShowGameAudioCommandAsync(inputArgs, "setmastervolume");
                else if (subCommand == "setvoicevolume")
                    await ShowGameAudioCommandAsync(inputArgs, "setvoicevolume");
                else if (subCommand == "setsoundvolume")
                    await ShowGameAudioCommandAsync(inputArgs, "setsoundvolume");
                else if (subCommand == "getmastervolume")
                    await ShowGameAudioCommandAsync(inputArgs, "getmastervolume");
                else if (subCommand == "getvoicevolume")
                    await ShowGameAudioCommandAsync(inputArgs, "getvoicevolume");
                else if (subCommand == "getsoundvolume")
                    await ShowGameAudioCommandAsync(inputArgs, "getsoundvolume");
                else if (subCommand == "setvideosetting")
                    await ShowGameVideoCommandAsync(inputArgs, "setvideosetting");
                else if (subCommand == "getvideosetting")
                    await ShowGameVideoCommandAsync(inputArgs, "getvideosetting");
                else if (subCommand == "bindkeys")
                    await ShowGameInputCommandAsync(inputArgs, "bindkeys");
                else if (subCommand == "inventory")
                    await ShowGameInventoryCommandAsync(inputArgs);
                else
                    await ShowSubCommandAsync<Game>(inputArgs, "game", "games", STARCLI.Games.CreateAsync, STARCLI.Games.UpdateAsync, STARCLI.Games.DeleteAsync, STARCLI.Games.DownloadAndInstallAsync, STARCLI.Games.UninstallAsync, STARCLI.Games.PublishAsync, STARCLI.Games.UnpublishAsync, STARCLI.Games.RepublishAsync, STARCLI.Games.ActivateAsync, STARCLI.Games.DeactivateAsync, STARCLI.Games.ShowAsync, STARCLI.Games.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Games.ListAllAsync, STARCLI.Games.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Games.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Games.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Games.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Games.SearchAsync, STARCLI.Games.AddDependencyAsync, STARCLI.Games.RemoveDependencyAsync, clonePredicate: STARCLI.Games.CloneAsync, providerType: providerType);
            }
            else
            {
                await ShowSubCommandAsync<Game>(inputArgs, "game", "games", STARCLI.Games.CreateAsync, STARCLI.Games.UpdateAsync, STARCLI.Games.DeleteAsync, STARCLI.Games.DownloadAndInstallAsync, STARCLI.Games.UninstallAsync, STARCLI.Games.PublishAsync, STARCLI.Games.UnpublishAsync, STARCLI.Games.RepublishAsync, STARCLI.Games.ActivateAsync, STARCLI.Games.DeactivateAsync, STARCLI.Games.ShowAsync, STARCLI.Games.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Games.ListAllAsync, STARCLI.Games.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Games.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Games.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Games.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Games.SearchAsync, STARCLI.Games.AddDependencyAsync, STARCLI.Games.RemoveDependencyAsync, clonePredicate: STARCLI.Games.CloneAsync, providerType: providerType);
            }
        }
    }
}
