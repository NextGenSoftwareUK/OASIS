using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using Console = System.Console;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.ONODE.Client;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Enums;
using NextGenSoftware.OASIS.STAR.CLI.Lib;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using System.IO;
using System.Reflection;

namespace NextGenSoftware.OASIS.STAR.CLI
{ //test
    partial class Program
    {
        private static async Task<bool> TryBootBeamInAsync(StarCliInvocation inv, string beamUser, string beamPass)
        {
            // Same skip list as STAR_CLI_NonInteractive.md â€” do not require avatar for these verbs (interactive or -n).
            bool skipBeamIn = _args.Length > 0 && StarCliInvocation.CommandSkipsAvatarBeamIn(_args[0]);
            if (skipBeamIn)
                return true;

            if (!inv.NonInteractive)
            {
                await STARCLI.Avatars.BeamInAvatar();
                return true;
            }

            if (string.IsNullOrWhiteSpace(beamUser) || string.IsNullOrWhiteSpace(beamPass))
            {
                StarCliShellOutput.WriteError(inv.JsonOutput, 2,
                    "Non-interactive mode requires credentials for this command: set STAR_CLI_USERNAME and STAR_CLI_PASSWORD, or use --username / --password, or prefix: avatar beamin <username> <password>",
                    null);
                return false;
            }

            string verifyToken = Environment.GetEnvironmentVariable("STAR_CLI_EMAIL_VERIFY_TOKEN");
            await STARCLI.Avatars.BeamInWithCredentialsAsync(beamUser, beamPass, verifyToken);
            return true;
        }

        static async Task Main(string[] args)
        {
            try
            {
                StarCliInvocation inv = StarCliInvocation.Parse(args);
                CLIEngine.NonInteractive = inv.NonInteractive;
                CLIEngine.JsonOutput = inv.JsonOutput;
                CLIEngine.Quiet = inv.Quiet;
                CLIEngine.AssumeYes = inv.AssumeYes;
                CLIEngine.MaxHolonSearchResults = inv.MaxHolonSearchResults;

                _args = inv.GetCommandArgsAfterOptionalAvatarBeamIn(out string beamUser, out string beamPass);

                if (inv.NonInteractive && _args.Length == 0 && (string.IsNullOrWhiteSpace(beamUser) || string.IsNullOrWhiteSpace(beamPass)))
                {
                    StarCliShellOutput.WriteError(inv.JsonOutput, 2,
                        "No command specified. Examples: star --non-interactive version | star --non-interactive --username USER --password PASS (beam-in only)",
                        null);
                    return;
                }

                //ConsoleHelper.SetCurrentFont("Consolas", 8);
                // DNA is published next to star; paths are relative to CWD. Launching from another folder
                // (e.g. ./Scripts/STAR\ CLI/RUN_STAR_CLI.sh from repo root) breaks File.Exists("DNA/OASIS_DNA.json").
                EnsureWorkingDirectoryNextToStarExecutableWhenDnaNotInCwd();
                ShowHeader();
                if (!CLIEngine.Quiet)
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
                {
                    if (CLIEngine.JsonOutput)
                        StarCliShellOutput.WriteError(true, 1, "Failed to ignite STAR.", result.Message);
                    else
                        CLIEngine.ShowErrorMessage(string.Concat("Error Igniting STAR. Error Message: ", result.Message));
                    return;
                }

                DEFAULT_DNA_FOLDER = STAR.STARDNA.OAPPMetaDataDNAFolder;
                DEFAULT_GENESIS_FOLDER = STAR.STARDNA.DefaultOAPPsSourcePath;

                if (!await TryBootBeamInAsync(inv, beamUser, beamPass))
                    return;

                // Scan and load installed plugins at boot time
                await ScanAndLoadPluginsAtBoot();

                if (inv.NonInteractive && _args.Length == 0)
                {
                    StarCliShellOutput.WriteSuccess(CLIEngine.JsonOutput, "Beam-in completed.",
                        STAR.BeamedInAvatar != null
                            ? new { username = STAR.BeamedInAvatar.Username }
                            : null);
                    return;
                }

                await ReadyPlayerOne(); //TODO: May allow this to be called with a different provider in future.
            }
            catch (CLIEngineNonInteractiveInputRequiredException niex)
            {
                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 3, niex.Message, null);
            }
            catch (Exception ex)
            {
                if (CLIEngine.JsonOutput)
                    StarCliShellOutput.WriteError(true, 1, ex.Message, ex.ToString());
                else
                {
                    Console.WriteLine("");
                    CLIEngine.ShowErrorMessage(string.Concat("An unknown error has occurred. Error Details: ", ex.ToString()));
                }
            }
        }

        /// <summary>
        /// If <c>DNA/OASIS_DNA.json</c> is not found from the current directory but exists beside the
        /// STAR CLI binary (publish or <c>dotnet run</c> output), set CWD to that directory so boot
        /// and file-manager-style paths behave consistently.
        /// </summary>
        private static void EnsureWorkingDirectoryNextToStarExecutableWhenDnaNotInCwd()
        {
            try
            {
                string oasisInCwd = Path.Combine(Environment.CurrentDirectory, "DNA", "OASIS_DNA.json");
                if (File.Exists(oasisInCwd))
                    return;

                // dotnet run: host is "dotnet"; DNA is next to star.dll under bin/Release/net8.0/
                try
                {
                    string loc = Assembly.GetExecutingAssembly().Location;
                    if (!string.IsNullOrEmpty(loc))
                    {
                        string dllDir = Path.GetDirectoryName(loc);
                        if (!string.IsNullOrEmpty(dllDir))
                        {
                            string oasisByDll = Path.Combine(dllDir, "DNA", "OASIS_DNA.json");
                            if (File.Exists(oasisByDll))
                            {
                                Environment.CurrentDirectory = dllDir;
                                return;
                            }
                        }
                    }
                }
                catch
                {
                    // non-fatal
                }

                // Single-file publish: BaseDirectory is the extract temp folder (no DNA). DNA/ is next to the real `star` binary.
                string proc = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(proc))
                {
                    string starDir = Path.GetDirectoryName(proc);
                    if (!string.IsNullOrEmpty(starDir))
                    {
                        string oasisNextToStar = Path.Combine(starDir, "DNA", "OASIS_DNA.json");
                        if (File.Exists(oasisNextToStar))
                        {
                            Environment.CurrentDirectory = starDir;
                            return;
                        }
                    }
                }

                string exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string oasisNextToExe = Path.Combine(exeDir, "DNA", "OASIS_DNA.json");
                if (File.Exists(oasisNextToExe))
                    Environment.CurrentDirectory = exeDir;
            }
            catch
            {
                // Non-fatal; IgniteStar will surface a clear DNA load error if paths are still wrong.
            }
        }

        private static async Task ScanAndLoadPluginsAtBoot()
        {
            try
            {
                var pluginLoader = new PluginLoader();
                var scanResult = await pluginLoader.ScanAndLoadPluginsAsync();
                
                if (!CLIEngine.Quiet && scanResult != null && !scanResult.IsError && scanResult.Result != null && scanResult.Result.Count > 0)
                {
                    CLIEngine.ShowMessage($"", false);
                    CLIEngine.ShowSuccessMessage($"Loaded {scanResult.Result.Count} installed plugin(s) at boot time.");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error scanning plugins at boot: {ex.Message}");
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // Allow default: Ctrl+C terminates the process. (e.Cancel = true would swallow SIGINT and trap the user.)
            e.Cancel = false;
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
            if (!CLIEngine.Quiet)
            {
                CLIEngine.WriteAsciMessage(" READY PLAYER ONE?", Color.Green);
                CLIEngine.ShowMessage("Please help support us by making a donation here: https://opencollective.com/oasis-web4 or consider buying some virtual land NFT's (OLAND) here: https://www.panxpan.com/projects/guardians-of-infinite-reality or buying one of our meta brick NFT's here: https://metabricks.xyz, thank you! :)");
            }
            
            //CLIEngine.ShowMessage("", false);

            //TODO: TEMP - REMOVE AFTER TESTING! :)
            //await Test(celestialBodyDNAFolder, geneisFolder);

            bool exit = false;
            bool shellMode = _args != null && _args.Length > 0;
            bool shellModeCommandConsumed = false;
            var commandHistory = new List<string>();
            do
            {
                try
                {

                    if (_exiting)
                        exit = true;

                    string[] inputArgs = null;
                    if (shellMode && !shellModeCommandConsumed)
                    {
                        // Non-interactive shell invocation: star <command> [subcommand] [params...]
                        inputArgs = _args;
                        shellModeCommandConsumed = true;
                    }
                    else
                    {
                        _inMainMenu = true;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("");
                        CLIEngine.ShowMessage("STAR: ", false, true);
                        int startLeft = Console.CursorLeft;
                        int startTop = Console.CursorTop;
                        int historyIndex = commandHistory.Count; // position after last item
                        string input = ReadLineWithCommandHistory(commandHistory, ref historyIndex, startLeft, startTop);

                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            string trimmed = input.Trim();
                            if (commandHistory.Count == 0 || !string.Equals(commandHistory[commandHistory.Count - 1], trimmed, StringComparison.Ordinal))
                                commandHistory.Add(trimmed);
                        }

                        if (!string.IsNullOrEmpty(input))
                            inputArgs = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (inputArgs != null && inputArgs.Length > 0)
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
                                        if (CLIEngine.JsonOutput)
                                        {
                                            StarCliShellOutput.WriteSuccess(true,
                                                "Human-readable command reference: run without --json or see Docs/Devs/STAR_CLI_NonInteractive.md",
                                                new { shellFlags = new[] { "--non-interactive (-n)", "--json", "--quiet (-q)", "--yes (-y)", "--username", "--password" } });
                                        }
                                        else if (inputArgs.Length > 1 && inputArgs[1].ToLower() == "full")
                                            ShowCommands(true);
                                        else
                                            ShowCommands(false);
                                    }
                                    break;

                                case "version":
                                    {
                                        if (CLIEngine.JsonOutput)
                                        {
                                            StarCliShellOutput.WriteSuccess(true, null, new
                                            {
                                                oasisRuntime = OASISBootLoader.OASISBootLoader.OASISRuntimeVersion,
                                                oasisApi = OASISBootLoader.OASISBootLoader.OASISAPIVersion,
                                                cosmicOrm = OASISBootLoader.OASISBootLoader.COSMICVersion,
                                                starRuntime = OASISBootLoader.OASISBootLoader.STARRuntimeVersion,
                                                starOdk = OASISBootLoader.OASISBootLoader.STARODKVersion,
                                                starnet = OASISBootLoader.OASISBootLoader.STARNETVersion,
                                                starApi = OASISBootLoader.OASISBootLoader.STARAPIVersion,
                                                dotNet = OASISBootLoader.OASISBootLoader.DotNetVersion,
                                                oasisProviderVersions = "Coming Soon"
                                            });
                                        }
                                        else
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
                                    }
                                    break;

                                case "status":
                                    {
                                        if (CLIEngine.JsonOutput)
                                        {
                                            StarCliShellOutput.WriteSuccess(true, null, new
                                            {
                                                starOdkStatus = Enum.GetName(typeof(StarStatus), STAR.Status),
                                                cosmicOrmStatus = "Online",
                                                oasisRuntimeStatus = "Online",
                                                oasisProviderStatus = "Coming Soon"
                                            });
                                        }
                                        else
                                        {
                                            Console.WriteLine("");
                                            CLIEngine.ShowMessage($"STAR ODK Status: {Enum.GetName(typeof(StarStatus), STAR.Status)}", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage($"COSMIC ORM Status: Online", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage($"OASIS Runtime Status: Online", ConsoleColor.Green, false);
                                            CLIEngine.ShowMessage($"OASIS Provider Status: Coming Soon...", ConsoleColor.Green, false); //TODO Implement ASAP.
                                            Console.WriteLine("");
                                            ShowDNAPaths();
                                        }
                                    }
                                    break;

                                case "dna":
                                    {
                                        Console.WriteLine("");
                                        ShowDNAPaths();
                                    }
                                    break;

                                case "exit":
                                    exit = CLIEngine.NonInteractive || CLIEngine.GetConfirmation("STAR: Are you sure you wish to exit?");
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

                                                // Primary: star light ./LightRequest.json (path must exist; .json extension)
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
                                                // Alias: star light json <file>
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
                                    break;

                                case "bang":
                                    {
                                        if (CLIEngine.NonInteractive)
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, "Command 'bang' is interactive-only. Omit --non-interactive or use a scripted workflow.", null);
                                            if (shellMode)
                                                Environment.ExitCode = 2;
                                            break;
                                        }
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
                                        if (CLIEngine.NonInteractive)
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, "Command 'wiz' is interactive-only. Use 'light <args>' with full parameters or interactive mode.", null);
                                            if (shellMode)
                                                Environment.ExitCode = 2;
                                            break;
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

                                                        if (CLIEngine.NonInteractive && string.IsNullOrWhiteSpace(oappPath))
                                                        {
                                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                                "Non-interactive oapp publish requires a source path.",
                                                                "Example: star --non-interactive oapp publish /path/to/oapp/source [dotnetpublish]");
                                                            break;
                                                        }

                                                        await STARCLI.OAPPs.PublishAsync(oappPath, dotNetPublish);
                                                    }
                                                    break;

                                                case "template":
                                                    await ShowSubCommandAsync<OAPPTemplate>(inputArgs, "OAPP TEMPLATE", "", STARCLI.OAPPTemplates.CreateAsync, STARCLI.OAPPTemplates.UpdateAsync, STARCLI.OAPPTemplates.DeleteAsync, STARCLI.OAPPTemplates.DownloadAndInstallAsync, STARCLI.OAPPTemplates.UninstallAsync, STARCLI.OAPPTemplates.PublishAsync, STARCLI.OAPPTemplates.UnpublishAsync, STARCLI.OAPPTemplates.RepublishAsync, STARCLI.OAPPTemplates.ActivateAsync, STARCLI.OAPPTemplates.DeactivateAsync, STARCLI.OAPPTemplates.ShowAsync, STARCLI.OAPPTemplates.ListAllCreatedByBeamedInAvatarAsync, STARCLI.OAPPTemplates.ListAllAsync, STARCLI.OAPPTemplates.ListAllInstalledForBeamedInAvatarAsync, STARCLI.OAPPTemplates.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.OAPPTemplates.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.OAPPTemplates.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.OAPPTemplates.SearchAsync, STARCLI.OAPPTemplates.AddDependencyAsync, STARCLI.OAPPTemplates.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPTemplates.CloneAsync, providerType: providerType);
                                                    break;

                                                default:
                                                    await ShowSubCommandAsync<OAPP>(inputArgs, "OAPP", "", STARCLI.OAPPs.CreateAsync, STARCLI.OAPPs.UpdateAsync, STARCLI.OAPPs.DeleteAsync, STARCLI.OAPPs.DownloadAndInstallAsync, STARCLI.OAPPs.UninstallAsync, STARCLI.OAPPs.PublishAsync, STARCLI.OAPPs.UnpublishAsync, STARCLI.OAPPs.RepublishAsync, STARCLI.OAPPs.ActivateAsync, STARCLI.OAPPs.DeactivateAsync, STARCLI.OAPPs.ShowAsync, STARCLI.OAPPs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.OAPPs.ListAllAsync, STARCLI.OAPPs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.OAPPs.SearchAsync, STARCLI.OAPPs.AddDependencyAsync, STARCLI.OAPPs.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPs.CloneAsync, providerType: providerType);
                                                    break;
                                            }
                                        }
                                        else
                                            await ShowSubCommandAsync<OAPP>(inputArgs, "OAPP", "", STARCLI.OAPPs.CreateAsync, STARCLI.OAPPs.UpdateAsync, STARCLI.OAPPs.DeleteAsync, STARCLI.OAPPs.DownloadAndInstallAsync, STARCLI.OAPPs.UninstallAsync, STARCLI.OAPPs.PublishAsync, STARCLI.OAPPs.UnpublishAsync, STARCLI.OAPPs.RepublishAsync, STARCLI.OAPPs.ActivateAsync, STARCLI.OAPPs.DeactivateAsync, STARCLI.OAPPs.ShowAsync, STARCLI.OAPPs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.OAPPs.ListAllAsync, STARCLI.OAPPs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.OAPPs.SearchAsync, STARCLI.OAPPs.AddDependencyAsync, STARCLI.OAPPs.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPs.CloneAsync, providerType: providerType);

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
                                        await ShowSubCommandAsync<OAPP>(inputArgs, "hApp", "", STARCLI.OAPPs.CreateAsync, STARCLI.OAPPs.UpdateAsync, STARCLI.OAPPs.DeleteAsync, STARCLI.OAPPs.DownloadAndInstallAsync, STARCLI.OAPPs.UninstallAsync, STARCLI.OAPPs.PublishAsync, STARCLI.OAPPs.UnpublishAsync, STARCLI.OAPPs.RepublishAsync, STARCLI.OAPPs.ActivateAsync, STARCLI.OAPPs.DeactivateAsync, STARCLI.OAPPs.ShowAsync, STARCLI.OAPPs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.OAPPs.ListAllAsync, STARCLI.OAPPs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.OAPPs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.OAPPs.SearchAsync, STARCLI.OAPPs.AddDependencyAsync, STARCLI.OAPPs.RemoveDependencyAsync, clonePredicate: STARCLI.OAPPs.CloneAsync, providerType: providerType);
                                        break;
                                    }

                                case "runtime":
                                    await ShowSubCommandAsync<Runtime>(inputArgs, "runtime", "runtimes", STARCLI.Runtimes.CreateAsync, STARCLI.Runtimes.UpdateAsync, STARCLI.Runtimes.DeleteAsync, STARCLI.Runtimes.DownloadAndInstallAsync, STARCLI.Runtimes.UninstallAsync, STARCLI.Runtimes.PublishAsync, STARCLI.Runtimes.UnpublishAsync, STARCLI.Runtimes.RepublishAsync, STARCLI.Runtimes.ActivateAsync, STARCLI.Runtimes.DeactivateAsync, STARCLI.Runtimes.ShowAsync, STARCLI.Runtimes.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Runtimes.ListAllAsync, STARCLI.Runtimes.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Runtimes.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Runtimes.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Runtimes.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Runtimes.SearchAsync, STARCLI.Runtimes.AddDependencyAsync, STARCLI.Runtimes.RemoveDependencyAsync, clonePredicate: STARCLI.Runtimes.CloneAsync, providerType: providerType);
                                    break;

                                case "lib":
                                    await ShowSubCommandAsync<Library>(inputArgs, "library", "libs", STARCLI.Libs.CreateAsync, STARCLI.Libs.UpdateAsync, STARCLI.Libs.DeleteAsync, STARCLI.Libs.DownloadAndInstallAsync, STARCLI.Libs.UninstallAsync, STARCLI.Libs.PublishAsync, STARCLI.Libs.UnpublishAsync, STARCLI.Libs.RepublishAsync, STARCLI.Libs.ActivateAsync, STARCLI.Libs.DeactivateAsync, STARCLI.Libs.ShowAsync, STARCLI.Libs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Libs.ListAllAsync, STARCLI.Libs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Libs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Libs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Libs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Libs.SearchAsync, STARCLI.Libs.AddDependencyAsync, STARCLI.Libs.RemoveDependencyAsync, clonePredicate: STARCLI.Libs.CloneAsync, providerType: providerType);
                                    break;

                                case "celestialspace":
                                    await ShowSubCommandAsync<STARCelestialSpace>(inputArgs, "celestial space", "celestial spaces", STARCLI.CelestialSpaces.CreateAsync, STARCLI.CelestialSpaces.UpdateAsync, STARCLI.CelestialSpaces.DeleteAsync, STARCLI.CelestialSpaces.DownloadAndInstallAsync, STARCLI.CelestialSpaces.UninstallAsync, STARCLI.CelestialSpaces.PublishAsync, STARCLI.CelestialSpaces.UnpublishAsync, STARCLI.CelestialSpaces.RepublishAsync, STARCLI.CelestialSpaces.ActivateAsync, STARCLI.CelestialSpaces.DeactivateAsync, STARCLI.CelestialSpaces.ShowAsync, STARCLI.CelestialSpaces.ListAllCreatedByBeamedInAvatarAsync, STARCLI.CelestialSpaces.ListAllAsync, STARCLI.CelestialSpaces.ListAllInstalledForBeamedInAvatarAsync, STARCLI.CelestialSpaces.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.CelestialSpaces.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.CelestialSpaces.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.CelestialSpaces.SearchAsync, STARCLI.CelestialSpaces.AddDependencyAsync, STARCLI.CelestialSpaces.RemoveDependencyAsync, clonePredicate: STARCLI.CelestialSpaces.CloneAsync, providerType: providerType);
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
                                            await ShowSubCommandAsync<CelestialBodyMetaDataDNA>(inputArgs, "celestial body metadata", "celestial body metadata", STARCLI.CelestialBodiesMetaDataDNA.CreateAsync, STARCLI.CelestialBodiesMetaDataDNA.UpdateAsync, STARCLI.CelestialBodiesMetaDataDNA.DeleteAsync, STARCLI.CelestialBodiesMetaDataDNA.DownloadAndInstallAsync, STARCLI.CelestialBodiesMetaDataDNA.UninstallAsync, STARCLI.CelestialBodiesMetaDataDNA.PublishAsync, STARCLI.CelestialBodiesMetaDataDNA.UnpublishAsync, STARCLI.CelestialBodiesMetaDataDNA.RepublishAsync, STARCLI.CelestialBodiesMetaDataDNA.ActivateAsync, STARCLI.CelestialBodiesMetaDataDNA.DeactivateAsync, STARCLI.CelestialBodiesMetaDataDNA.ShowAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllCreatedByBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllInstalledForBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.CelestialBodiesMetaDataDNA.SearchAsync, STARCLI.CelestialBodiesMetaDataDNA.AddDependencyAsync, STARCLI.CelestialBodiesMetaDataDNA.RemoveDependencyAsync, clonePredicate: STARCLI.CelestialBodiesMetaDataDNA.CloneAsync, providerType: providerType);
                                        else
                                            await ShowSubCommandAsync<STARCelestialBody>(inputArgs, "celestial body", "celestial bodies", STARCLI.CelestialBodies.CreateAsync, STARCLI.CelestialBodies.UpdateAsync, STARCLI.CelestialBodies.DeleteAsync, STARCLI.CelestialBodies.DownloadAndInstallAsync, STARCLI.CelestialBodies.UninstallAsync, STARCLI.CelestialBodies.PublishAsync, STARCLI.CelestialBodies.UnpublishAsync, STARCLI.CelestialBodies.RepublishAsync, STARCLI.CelestialBodies.ActivateAsync, STARCLI.CelestialBodies.DeactivateAsync, STARCLI.CelestialBodies.ShowAsync, STARCLI.CelestialBodies.ListAllCreatedByBeamedInAvatarAsync, STARCLI.CelestialBodies.ListAllAsync, STARCLI.CelestialBodies.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Zomes.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Zomes.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Zomes.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Zomes.SearchAsync, STARCLI.Zomes.AddDependencyAsync, STARCLI.Zomes.RemoveDependencyAsync, clonePredicate: STARCLI.CelestialBodies.CloneAsync, providerType: providerType);
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
                                            await ShowSubCommandAsync<ZomeMetaDataDNA>(inputArgs, "zome metadata", "zome metadata", STARCLI.ZomesMetaDataDNA.CreateAsync, STARCLI.ZomesMetaDataDNA.UpdateAsync, STARCLI.ZomesMetaDataDNA.DeleteAsync, STARCLI.ZomesMetaDataDNA.DownloadAndInstallAsync, STARCLI.ZomesMetaDataDNA.UninstallAsync, STARCLI.ZomesMetaDataDNA.PublishAsync, STARCLI.ZomesMetaDataDNA.UnpublishAsync, STARCLI.ZomesMetaDataDNA.RepublishAsync, STARCLI.ZomesMetaDataDNA.ActivateAsync, STARCLI.ZomesMetaDataDNA.DeactivateAsync, STARCLI.ZomesMetaDataDNA.ShowAsync, STARCLI.ZomesMetaDataDNA.ListAllCreatedByBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.ListAllAsync, STARCLI.ZomesMetaDataDNA.ListAllInstalledForBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.ZomesMetaDataDNA.SearchAsync, STARCLI.ZomesMetaDataDNA.AddDependencyAsync, STARCLI.ZomesMetaDataDNA.RemoveDependencyAsync, clonePredicate: STARCLI.ZomesMetaDataDNA.CloneAsync, providerType: providerType);
                                        else
                                            await ShowSubCommandAsync<STARZome>(inputArgs, "zome", "zomes", STARCLI.Zomes.CreateAsync, STARCLI.Zomes.UpdateAsync, STARCLI.Zomes.DeleteAsync, STARCLI.Zomes.DownloadAndInstallAsync, STARCLI.Zomes.UninstallAsync, STARCLI.Zomes.PublishAsync, STARCLI.Zomes.UnpublishAsync, STARCLI.Zomes.RepublishAsync, STARCLI.Zomes.ActivateAsync, STARCLI.Zomes.DeactivateAsync, STARCLI.Zomes.ShowAsync, STARCLI.Zomes.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Zomes.ListAllAsync, STARCLI.Zomes.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Zomes.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Zomes.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Zomes.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Zomes.SearchAsync, STARCLI.Zomes.AddDependencyAsync, STARCLI.Zomes.RemoveDependencyAsync, clonePredicate: STARCLI.Zomes.CloneAsync, providerType: providerType);
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
                                            await ShowSubCommandAsync<HolonMetaDataDNA>(inputArgs, "holon metadata", "holon metadata", STARCLI.HolonsMetaDataDNA.CreateAsync, STARCLI.HolonsMetaDataDNA.UpdateAsync, STARCLI.HolonsMetaDataDNA.DeleteAsync, STARCLI.HolonsMetaDataDNA.DownloadAndInstallAsync, STARCLI.HolonsMetaDataDNA.UninstallAsync, STARCLI.HolonsMetaDataDNA.PublishAsync, STARCLI.HolonsMetaDataDNA.UnpublishAsync, STARCLI.HolonsMetaDataDNA.RepublishAsync, STARCLI.HolonsMetaDataDNA.ActivateAsync, STARCLI.HolonsMetaDataDNA.DeactivateAsync, STARCLI.HolonsMetaDataDNA.ShowAsync, STARCLI.HolonsMetaDataDNA.ListAllCreatedByBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.ListAllAsync, STARCLI.HolonsMetaDataDNA.ListAllInstalledForBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.HolonsMetaDataDNA.SearchAsync, STARCLI.HolonsMetaDataDNA.AddDependencyAsync, STARCLI.HolonsMetaDataDNA.RemoveDependencyAsync, clonePredicate: STARCLI.HolonsMetaDataDNA.CloneAsync, providerType: providerType);
                                        else
                                            await ShowSubCommandAsync<STARHolon>(inputArgs, "holon", "holons", STARCLI.Holons.CreateAsync, STARCLI.Holons.UpdateAsync, STARCLI.Holons.DeleteAsync, STARCLI.Holons.DownloadAndInstallAsync, STARCLI.Holons.UninstallAsync, STARCLI.Holons.PublishAsync, STARCLI.Holons.UnpublishAsync, STARCLI.Holons.RepublishAsync, STARCLI.Holons.ActivateAsync, STARCLI.Holons.DeactivateAsync, STARCLI.Holons.ShowAsync, STARCLI.Holons.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Holons.ListAllAsync, STARCLI.Holons.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Holons.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Holons.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Holons.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Holons.SearchAsync, STARCLI.Holons.AddDependencyAsync, STARCLI.Holons.RemoveDependencyAsync, clonePredicate: STARCLI.Holons.CloneAsync, providerType: providerType);
                                    }
                                    break;

                                case "chapter":
                                    await ShowSubCommandAsync<Chapter>(inputArgs, "chapter", "chapters", STARCLI.Chapters.CreateAsync, STARCLI.Chapters.UpdateAsync, STARCLI.Chapters.DeleteAsync, STARCLI.Chapters.DownloadAndInstallAsync, STARCLI.Chapters.UninstallAsync, STARCLI.Chapters.PublishAsync, STARCLI.Chapters.UnpublishAsync, STARCLI.Chapters.RepublishAsync, STARCLI.Chapters.ActivateAsync, STARCLI.Chapters.DeactivateAsync, STARCLI.Chapters.ShowAsync, STARCLI.Chapters.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Chapters.ListAllAsync, STARCLI.Chapters.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Chapters.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Chapters.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Chapters.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Chapters.SearchAsync, STARCLI.Chapters.AddDependencyAsync, STARCLI.Chapters.RemoveDependencyAsync, clonePredicate: STARCLI.Chapters.CloneAsync, providerType: providerType);
                                    break;

                                case "mission":
                                    await ShowSubCommandAsync<Mission>(inputArgs, "mission", "missions", STARCLI.Missions.CreateAsync, STARCLI.Missions.UpdateAsync, STARCLI.Missions.DeleteAsync, STARCLI.Missions.DownloadAndInstallAsync, STARCLI.Missions.UninstallAsync, STARCLI.Missions.PublishAsync, STARCLI.Missions.UnpublishAsync, STARCLI.Missions.RepublishAsync, STARCLI.Missions.ActivateAsync, STARCLI.Missions.DeactivateAsync, STARCLI.Missions.ShowAsync, STARCLI.Missions.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Missions.ListAllAsync, STARCLI.Missions.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Missions.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Missions.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Missions.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Missions.SearchAsync, STARCLI.Missions.AddDependencyAsync, STARCLI.Missions.RemoveDependencyAsync, clonePredicate: STARCLI.Missions.CloneAsync, providerType: providerType);
                                    break;

                                case "quest":
                                    await ShowSubCommandAsync<Quest>(inputArgs, "quest", "quests", STARCLI.Quests.CreateAsync, STARCLI.Quests.UpdateAsync, STARCLI.Quests.DeleteAsync, STARCLI.Quests.DownloadAndInstallAsync, STARCLI.Quests.UninstallAsync, STARCLI.Quests.PublishAsync, STARCLI.Quests.UnpublishAsync, STARCLI.Quests.RepublishAsync, STARCLI.Quests.ActivateAsync, STARCLI.Quests.DeactivateAsync, STARCLI.Quests.ShowAsync, STARCLI.Quests.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Quests.ListAllAsync, STARCLI.Quests.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Quests.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Quests.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Quests.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Quests.SearchAsync, STARCLI.Quests.AddDependencyAsync, STARCLI.Quests.RemoveDependencyAsync, clonePredicate: STARCLI.Quests.CloneAsync, providerType: providerType);
                                    break;

                                case "game":
                                    {
                                        if (inputArgs.Length > 1)
                                        {
                                            string subCommand = inputArgs[1].ToLower();
                                            
                                            // Game session management commands
                                            if (subCommand == "start")
                                            {
                                                await ShowGameSessionCommandAsync(inputArgs, "start");
                                            }
                                            else if (subCommand == "end")
                                            {
                                                await ShowGameSessionCommandAsync(inputArgs, "end");
                                            }
                                            else if (subCommand == "load")
                                            {
                                                await ShowGameSessionCommandAsync(inputArgs, "load");
                                            }
                                            else if (subCommand == "unload")
                                            {
                                                await ShowGameSessionCommandAsync(inputArgs, "unload");
                                            }
                                            // Level management commands
                                            else if (subCommand == "loadlevel")
                                            {
                                                await ShowGameLevelCommandAsync(inputArgs, "loadlevel");
                                            }
                                            else if (subCommand == "unloadlevel")
                                            {
                                                await ShowGameLevelCommandAsync(inputArgs, "unloadlevel");
                                            }
                                            else if (subCommand == "jumptolevel")
                                            {
                                                await ShowGameLevelCommandAsync(inputArgs, "jumptolevel");
                                            }
                                            else if (subCommand == "jumptopoint")
                                            {
                                                await ShowGameLevelCommandAsync(inputArgs, "jumptopoint");
                                            }
                                            // Area management commands
                                            else if (subCommand == "loadarea")
                                            {
                                                await ShowGameAreaCommandAsync(inputArgs, "loadarea");
                                            }
                                            else if (subCommand == "unloadarea")
                                            {
                                                await ShowGameAreaCommandAsync(inputArgs, "unloadarea");
                                            }
                                            else if (subCommand == "jumptoarea")
                                            {
                                                await ShowGameAreaCommandAsync(inputArgs, "jumptoarea");
                                            }
                                            // UI commands
                                            else if (subCommand == "showtitlescreen")
                                            {
                                                await ShowGameUICommandAsync(inputArgs, "showtitlescreen");
                                            }
                                            else if (subCommand == "showmainmenu")
                                            {
                                                await ShowGameUICommandAsync(inputArgs, "showmainmenu");
                                            }
                                            else if (subCommand == "showoptions")
                                            {
                                                await ShowGameUICommandAsync(inputArgs, "showoptions");
                                            }
                                            else if (subCommand == "showcredits")
                                            {
                                                await ShowGameUICommandAsync(inputArgs, "showcredits");
                                            }
                                            // Audio commands
                                            else if (subCommand == "setmastervolume")
                                            {
                                                await ShowGameAudioCommandAsync(inputArgs, "setmastervolume");
                                            }
                                            else if (subCommand == "setvoicevolume")
                                            {
                                                await ShowGameAudioCommandAsync(inputArgs, "setvoicevolume");
                                            }
                                            else if (subCommand == "setsoundvolume")
                                            {
                                                await ShowGameAudioCommandAsync(inputArgs, "setsoundvolume");
                                            }
                                            else if (subCommand == "getmastervolume")
                                            {
                                                await ShowGameAudioCommandAsync(inputArgs, "getmastervolume");
                                            }
                                            else if (subCommand == "getvoicevolume")
                                            {
                                                await ShowGameAudioCommandAsync(inputArgs, "getvoicevolume");
                                            }
                                            else if (subCommand == "getsoundvolume")
                                            {
                                                await ShowGameAudioCommandAsync(inputArgs, "getsoundvolume");
                                            }
                                            // Video commands
                                            else if (subCommand == "setvideosetting")
                                            {
                                                await ShowGameVideoCommandAsync(inputArgs, "setvideosetting");
                                            }
                                            else if (subCommand == "getvideosetting")
                                            {
                                                await ShowGameVideoCommandAsync(inputArgs, "getvideosetting");
                                            }
                                            // Input commands
                                            else if (subCommand == "bindkeys")
                                            {
                                                await ShowGameInputCommandAsync(inputArgs, "bindkeys");
                                            }
                                            // Inventory commands
                                            else if (subCommand == "inventory")
                                            {
                                                await ShowGameInventoryCommandAsync(inputArgs);
                                            }
                                            // Standard STARNET commands (create, update, delete, publish, etc.)
                                            else
                                            {
                                                await ShowSubCommandAsync<Game>(inputArgs, "game", "games", STARCLI.Games.CreateAsync, STARCLI.Games.UpdateAsync, STARCLI.Games.DeleteAsync, STARCLI.Games.DownloadAndInstallAsync, STARCLI.Games.UninstallAsync, STARCLI.Games.PublishAsync, STARCLI.Games.UnpublishAsync, STARCLI.Games.RepublishAsync, STARCLI.Games.ActivateAsync, STARCLI.Games.DeactivateAsync, STARCLI.Games.ShowAsync, STARCLI.Games.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Games.ListAllAsync, STARCLI.Games.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Games.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Games.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Games.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Games.SearchAsync, STARCLI.Games.AddDependencyAsync, STARCLI.Games.RemoveDependencyAsync, clonePredicate: STARCLI.Games.CloneAsync, providerType: providerType);
                                            }
                                        }
                                        else
                                        {
                                            await ShowSubCommandAsync<Game>(inputArgs, "game", "games", STARCLI.Games.CreateAsync, STARCLI.Games.UpdateAsync, STARCLI.Games.DeleteAsync, STARCLI.Games.DownloadAndInstallAsync, STARCLI.Games.UninstallAsync, STARCLI.Games.PublishAsync, STARCLI.Games.UnpublishAsync, STARCLI.Games.RepublishAsync, STARCLI.Games.ActivateAsync, STARCLI.Games.DeactivateAsync, STARCLI.Games.ShowAsync, STARCLI.Games.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Games.ListAllAsync, STARCLI.Games.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Games.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Games.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Games.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Games.SearchAsync, STARCLI.Games.AddDependencyAsync, STARCLI.Games.RemoveDependencyAsync, clonePredicate: STARCLI.Games.CloneAsync, providerType: providerType);
                                        }
                                    }
                                    break;

                                case "nft":
                                    {
                                       if (inputArgs.Length > 1 && inputArgs[1].ToLower() == "collection")
                                            //await ShowSubCommandAsync<STARNFTCollection>(inputArgs, "nft collection", "nft collection's", STARCLI.NFTCollections.CreateAsync, STARCLI.NFTCollections.UpdateAsync, STARCLI.NFTCollections.DeleteAsync, STARCLI.NFTCollections.DownloadAndInstallAsync, STARCLI.NFTCollections.UninstallAsync, STARCLI.NFTCollections.PublishAsync, STARCLI.NFTCollections.UnpublishAsync, STARCLI.NFTCollections.RepublishAsync, STARCLI.NFTCollections.ActivateAsync, STARCLI.NFTCollections.DeactivateAsync, STARCLI.NFTCollections.ShowAsync, STARCLI.NFTCollections.ListAllCreatedByBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllAsync, STARCLI.NFTCollections.ListAllInstalledForBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.NFTCollections.SearchAsync, STARCLI.NFTCollections.AddDependencyAsync, STARCLI.NFTCollections.RemoveDependencyAsync, clonePredicate: STARCLI.NFTCollections.CloneAsync, createWeb4Predicate: STARCLI.NFTCollections.CreateWeb4NFTCollectionAsync, updateWeb4Predicate: STARCLI.NFTCollections.UpdateWeb4NFTCollectionAsync, deleteWeb4Predicate: STARCLI.NFTCollections.DeleteWeb4NFTCollectionAsync, addWeb4NFTToCollectionPredicate: STARCLI.NFTCollections.AddWeb4NFTToCollectionAsync, removeWeb4NFTFromCollectionPredicate: STARCLI.NFTCollections.RemoveWeb4NFTFromCollectionAsync, listAllWeb4Predicate: STARCLI.NFTCollections.ListAllWeb4NFTCollections, listWeb4ForBeamedInAvatarPredicate: STARCLI.NFTCollections.ListWeb4NFTCollectionsForAvatar, showWeb4Predicate: STARCLI.NFTCollections.ShowWeb4NFTCollectionAsync, searchWeb4Predicate: STARCLI.NFTCollections.SearchWeb4NFTCollectionAsync, providerType: providerType);
                                            await ShowSubCommandAsync<STARNFTCollection>(inputArgs, "nft collection", "nft collection's", STARCLI.NFTCollections.CreateAsync, STARCLI.NFTCollections.UpdateAsync, STARCLI.NFTCollections.DeleteAsync, STARCLI.NFTCollections.DownloadAndInstallAsync, STARCLI.NFTCollections.UninstallAsync, STARCLI.NFTCollections.PublishAsync, STARCLI.NFTCollections.UnpublishAsync, STARCLI.NFTCollections.RepublishAsync, STARCLI.NFTCollections.ActivateAsync, STARCLI.NFTCollections.DeactivateAsync, STARCLI.NFTCollections.ShowAsync, STARCLI.NFTCollections.ListAllCreatedByBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllAsync, STARCLI.NFTCollections.ListAllInstalledForBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.NFTCollections.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.NFTCollections.SearchAsync, STARCLI.NFTCollections.AddDependencyAsync, STARCLI.NFTCollections.RemoveDependencyAsync, clonePredicate: STARCLI.NFTCollections.CloneAsync, createWeb4Predicate: STARCLI.NFTCollections.CreateWeb4NFTCollectionAsync, updateWeb4Predicate: STARCLI.NFTCollections.UpdateWeb4NFTCollectionAsync, addWeb4NFTToCollectionPredicate: STARCLI.NFTCollections.AddWeb4NFTToCollectionAsync, removeWeb4NFTFromCollectionPredicate: STARCLI.NFTCollections.RemoveWeb4NFTFromCollectionAsync, listAllWeb4Predicate: STARCLI.NFTCollections.ListAllWeb4NFTCollections, listWeb4ForBeamedInAvatarPredicate: STARCLI.NFTCollections.ListWeb4NFTCollectionsForAvatar, showWeb4Predicate: STARCLI.NFTCollections.ShowWeb4NFTCollectionAsync, searchWeb4Predicate: STARCLI.NFTCollections.SearchWeb4NFTCollectionAsync, providerType: providerType);
                                        else
                                            //await ShowSubCommandAsync<STARNFT>(inputArgs, "nft", "nft's", STARCLI.NFTs.CreateAsync, STARCLI.NFTs.UpdateAsync, STARCLI.NFTs.DeleteAsync, STARCLI.NFTs.DownloadAndInstallAsync, STARCLI.NFTs.UninstallAsync, STARCLI.NFTs.PublishAsync, STARCLI.NFTs.UnpublishAsync, STARCLI.NFTs.RepublishAsync, STARCLI.NFTs.ActivateAsync, STARCLI.NFTs.DeactivateAsync, STARCLI.NFTs.ShowAsync, STARCLI.NFTs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.NFTs.ListAllAsync, STARCLI.NFTs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.NFTs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.NFTs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.NFTs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.NFTs.SearchAsync, STARCLI.NFTs.AddDependencyAsync, STARCLI.NFTs.RemoveDependencyAsync, clonePredicate: STARCLI.NFTs.CloneAsync, mintPredicate: STARCLI.NFTs.MintNFTAsync, burnPredicate: STARCLI.NFTs.BurnNFTAsync, importPredicate: STARCLI.NFTs.ImportNFTAsync, exportPredicate: STARCLI.NFTs.ExportNFTAsync,  convertPredicate: STARCLI.NFTs.ConvertNFTAsync, updateWeb4Predicate: STARCLI.NFTs.UpdateWeb4NFTAsync, deleteWeb4Predicate: STARCLI.NFTs.DeleteWeb4NFTAsync, listAllWeb4Predicate: STARCLI.NFTs.ListAllWeb4NFTsAsync, listWeb4ForBeamedInAvatarPredicate: STARCLI.NFTs.ListAllWeb4NFTForAvatarsAsync, showWeb4Predicate: STARCLI.NFTs.ShowWeb4NFTAsync, searchWeb4Predicate: STARCLI.NFTs.SearchWeb4NFTAsync, showWeb3Predicate: STARCLI.NFTs.ShowWeb3NFTAsync, searchWeb3Predicate: STARCLI.NFTs.SearchWeb3NFTAsync, listAllWeb3Predicate: STARCLI.NFTs.ListAllWeb3NFTsAsync, listWeb3ForBeamedInAvatarPredicate: STARCLI.NFTs.ListAllWeb3NFTForAvatarsAsync, updateWeb3Predicate: STARCLI.NFTs.UpdateWeb3NFTAsync, deleteWeb3Predicate: STARCLI.NFTs.DeleteWeb3NFTAsync, providerType: providerType);
                                            await ShowSubCommandAsync<STARNFT>(inputArgs, "nft", "nft's", STARCLI.NFTs.CreateAsync, STARCLI.NFTs.UpdateAsync, STARCLI.NFTs.DeleteAsync, STARCLI.NFTs.DownloadAndInstallAsync, STARCLI.NFTs.UninstallAsync, STARCLI.NFTs.PublishAsync, STARCLI.NFTs.UnpublishAsync, STARCLI.NFTs.RepublishAsync, STARCLI.NFTs.ActivateAsync, STARCLI.NFTs.DeactivateAsync, STARCLI.NFTs.ShowAsync, STARCLI.NFTs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.NFTs.ListAllAsync, STARCLI.NFTs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.NFTs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.NFTs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.NFTs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.NFTs.SearchAsync, STARCLI.NFTs.AddDependencyAsync, STARCLI.NFTs.RemoveDependencyAsync, clonePredicate: STARCLI.NFTs.CloneAsync, mintPredicate: STARCLI.NFTs.MintNFTAsync, burnPredicate: STARCLI.NFTs.BurnNFTAsync, importPredicate: STARCLI.NFTs.ImportNFTAsync, exportPredicate: STARCLI.NFTs.ExportNFTAsync, convertPredicate: STARCLI.NFTs.ConvertNFTAsync, updateWeb4Predicate: STARCLI.NFTs.UpdateWeb4NFTAsync, listAllWeb4Predicate: STARCLI.NFTs.ListAllWeb4NFTsAsync, listWeb4ForBeamedInAvatarPredicate: STARCLI.NFTs.ListAllWeb4NFTForAvatarsAsync, showWeb4Predicate: STARCLI.NFTs.ShowWeb4NFTAsync, searchWeb4Predicate: STARCLI.NFTs.SearchWeb4NFTAsync, updateWeb3Predicate: STARCLI.NFTs.UpdateWeb3NFTAsync, deleteWeb3Predicate: STARCLI.NFTs.DeleteWeb3NFTAsync, listAllWeb3Predicate: STARCLI.NFTs.ListAllWeb3NFTsAsync, listWeb3ForBeamedInAvatarPredicate: STARCLI.NFTs.ListAllWeb3NFTForAvatarsAsync, showWeb3Predicate: STARCLI.NFTs.ShowWeb3NFTAsync, searchWeb3Predicate: STARCLI.NFTs.SearchWeb3NFTAsync, providerType: providerType);
                                    }
                                    break;

                                case "geonft":
                                    {
                                        if (inputArgs.Length > 1 && inputArgs[1].ToLower() == "collection")
                                            //await ShowSubCommandAsync<STARGeoNFTCollection>(inputArgs, "geo-nft collection", "geo-nft collection's", STARCLI.GeoNFTCollections.CreateAsync, STARCLI.GeoNFTCollections.UpdateAsync, STARCLI.GeoNFTCollections.DeleteAsync, STARCLI.GeoNFTCollections.DownloadAndInstallAsync, STARCLI.GeoNFTCollections.UninstallAsync, STARCLI.GeoNFTCollections.PublishAsync, STARCLI.GeoNFTCollections.UnpublishAsync, STARCLI.GeoNFTCollections.RepublishAsync, STARCLI.GeoNFTCollections.ActivateAsync, STARCLI.GeoNFTCollections.DeactivateAsync, STARCLI.GeoNFTCollections.ShowAsync, STARCLI.GeoNFTCollections.ListAllCreatedByBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllAsync, STARCLI.GeoNFTCollections.ListAllInstalledForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.SearchAsync, STARCLI.GeoNFTCollections.AddDependencyAsync, STARCLI.GeoNFTCollections.RemoveDependencyAsync, clonePredicate: STARCLI.GeoNFTCollections.CloneAsync, createWeb4Predicate: STARCLI.GeoNFTCollections.CreateWeb4GeoNFTCollectionAsync, updateWeb4Predicate: STARCLI.GeoNFTCollections.UpdateWeb4GeoNFTCollectionAsync, addWeb4NFTToCollectionPredicate: STARCLI.GeoNFTCollections.AddWeb4GeoNFTToCollectionAsync, removeWeb4NFTFromCollectionPredicate: STARCLI.GeoNFTCollections.RemoveWeb4GeoNFTFromCollectionAsync, deleteWeb4Predicate: STARCLI.GeoNFTCollections.DeleteWeb4GeoNFTCollectionAsync, listAllWeb4Predicate: STARCLI.GeoNFTCollections.ListAllWeb4GeoNFTCollections, listWeb4ForBeamedInAvatarPredicate: STARCLI.GeoNFTCollections.ListWeb4GeoNFTCollectionsForAvatar, showWeb4Predicate: STARCLI.GeoNFTCollections.ShowWeb4GeoNFTCollectionAsync, searchWeb4Predicate: STARCLI.GeoNFTCollections.SearchWeb4GeoNFTCollectionAsync, providerType: providerType);
                                            await ShowSubCommandAsync<STARGeoNFTCollection>(inputArgs, "geo-nft collection", "geo-nft collection's", STARCLI.GeoNFTCollections.CreateAsync, STARCLI.GeoNFTCollections.UpdateAsync, STARCLI.GeoNFTCollections.DeleteAsync, STARCLI.GeoNFTCollections.DownloadAndInstallAsync, STARCLI.GeoNFTCollections.UninstallAsync, STARCLI.GeoNFTCollections.PublishAsync, STARCLI.GeoNFTCollections.UnpublishAsync, STARCLI.GeoNFTCollections.RepublishAsync, STARCLI.GeoNFTCollections.ActivateAsync, STARCLI.GeoNFTCollections.DeactivateAsync, STARCLI.GeoNFTCollections.ShowAsync, STARCLI.GeoNFTCollections.ListAllCreatedByBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllAsync, STARCLI.GeoNFTCollections.ListAllInstalledForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.GeoNFTCollections.SearchAsync, STARCLI.GeoNFTCollections.AddDependencyAsync, STARCLI.GeoNFTCollections.RemoveDependencyAsync, clonePredicate: STARCLI.GeoNFTCollections.CloneAsync, createWeb4Predicate: STARCLI.GeoNFTCollections.CreateWeb4GeoNFTCollectionAsync, updateWeb4Predicate: STARCLI.GeoNFTCollections.UpdateWeb4GeoNFTCollectionAsync, addWeb4NFTToCollectionPredicate: STARCLI.GeoNFTCollections.AddWeb4GeoNFTToCollectionAsync, removeWeb4NFTFromCollectionPredicate: STARCLI.GeoNFTCollections.RemoveWeb4GeoNFTFromCollectionAsync, listAllWeb4Predicate: STARCLI.GeoNFTCollections.ListAllWeb4GeoNFTCollections, listWeb4ForBeamedInAvatarPredicate: STARCLI.GeoNFTCollections.ListWeb4GeoNFTCollectionsForAvatar, showWeb4Predicate: STARCLI.GeoNFTCollections.ShowWeb4GeoNFTCollectionAsync, searchWeb4Predicate: STARCLI.GeoNFTCollections.SearchWeb4GeoNFTCollectionAsync, providerType: providerType);
                                        else
                                            await ShowSubCommandAsync<STARGeoNFT>(inputArgs, "geo-nft", "geo-nft's", STARCLI.GeoNFTs.CreateAsync, STARCLI.GeoNFTs.UpdateAsync, STARCLI.GeoNFTs.DeleteAsync, STARCLI.GeoNFTs.DownloadAndInstallAsync, STARCLI.GeoNFTs.UninstallAsync, STARCLI.GeoNFTs.PublishAsync, STARCLI.GeoNFTs.UnpublishAsync, STARCLI.GeoNFTs.RepublishAsync, STARCLI.GeoNFTs.ActivateAsync, STARCLI.GeoNFTs.DeactivateAsync, STARCLI.GeoNFTs.ShowAsync, STARCLI.GeoNFTs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllAsync, STARCLI.GeoNFTs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.GeoNFTs.SearchAsync, STARCLI.GeoNFTs.AddDependencyAsync, STARCLI.GeoNFTs.RemoveDependencyAsync, clonePredicate: STARCLI.GeoNFTs.CloneAsync, mintPredicate: STARCLI.GeoNFTs.MintGeoNFTAsync, burnPredicate: STARCLI.GeoNFTs.BurnGeoNFTAsync, importPredicate: STARCLI.GeoNFTs.ImportGeoNFTAsync, exportPredicate: STARCLI.GeoNFTs.ExportGeoNFTAsync, convertPredicate: STARCLI.GeoNFTs.ConvertGeoNFTAsync, updateWeb4Predicate: STARCLI.GeoNFTs.UpdateWeb4GeoNFTAsync, listAllWeb4Predicate: STARCLI.GeoNFTs.ListAllWeb4GeoNFTsAsync, listWeb4ForBeamedInAvatarPredicate: STARCLI.GeoNFTs.ListAllWeb4GeoNFTForAvatarsAsync, showWeb4Predicate: STARCLI.GeoNFTs.ShowWeb4GeoNFTAsync, searchWeb4Predicate: STARCLI.GeoNFTs.SearchWeb4GeoNFTAsync, providerType: providerType);
                                        //await ShowSubCommandAsync<STARGeoNFT>(inputArgs, "geo-nft", "geo-nft's", STARCLI.GeoNFTs.CreateAsync, STARCLI.GeoNFTs.UpdateAsync, STARCLI.GeoNFTs.DeleteAsync, STARCLI.GeoNFTs.DownloadAndInstallAsync, STARCLI.GeoNFTs.UninstallAsync, STARCLI.GeoNFTs.PublishAsync, STARCLI.GeoNFTs.UnpublishAsync, STARCLI.GeoNFTs.RepublishAsync, STARCLI.GeoNFTs.ActivateAsync, STARCLI.GeoNFTs.DeactivateAsync, STARCLI.GeoNFTs.ShowAsync, STARCLI.GeoNFTs.ListAllCreatedByBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllAsync, STARCLI.GeoNFTs.ListAllInstalledForBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.GeoNFTs.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.GeoNFTs.SearchAsync, STARCLI.GeoNFTs.AddDependencyAsync, STARCLI.GeoNFTs.RemoveDependencyAsync, clonePredicate: STARCLI.GeoNFTs.CloneAsync, mintPredicate: STARCLI.GeoNFTs.MintGeoNFTAsync, burnPredicate: STARCLI.GeoNFTs.BurnGeoNFTAsync, importPredicate: STARCLI.GeoNFTs.ImportGeoNFTAsync, exportPredicate: STARCLI.GeoNFTs.ExportGeoNFTAsync, convertPredicate: STARCLI.GeoNFTs.ConvertGeoNFTAsync, updateWeb4Predicate: STARCLI.GeoNFTs.UpdateWeb4GeoNFTAsync, deleteWeb4Predicate: STARCLI.GeoNFTs.DeleteWeb4GeoNFTAsync, listAllWeb4Predicate: STARCLI.GeoNFTs.ListAllWeb4GeoNFTsAsync, listWeb4ForBeamedInAvatarPredicate: STARCLI.GeoNFTs.ListAllWeb4GeoNFTForAvatarsAsync, showWeb4Predicate: STARCLI.GeoNFTs.ShowWeb4GeoNFTAsync, searchWeb4Predicate: STARCLI.GeoNFTs.SearchWeb4GeoNFTAsync, providerType: providerType);
                                    }
                                    break;

                                case "geohotspot":
                                    await ShowSubCommandAsync<GeoHotSpot>(inputArgs, "geo-hotspot", "geo-hotspots", STARCLI.GeoHotSpots.CreateAsync, STARCLI.GeoHotSpots.UpdateAsync, STARCLI.GeoHotSpots.DeleteAsync, STARCLI.GeoHotSpots.DownloadAndInstallAsync, STARCLI.GeoHotSpots.UninstallAsync, STARCLI.GeoHotSpots.PublishAsync, STARCLI.GeoHotSpots.UnpublishAsync, STARCLI.GeoHotSpots.RepublishAsync, STARCLI.GeoHotSpots.ActivateAsync, STARCLI.GeoHotSpots.DeactivateAsync, STARCLI.GeoHotSpots.ShowAsync, STARCLI.GeoHotSpots.ListAllCreatedByBeamedInAvatarAsync, STARCLI.GeoHotSpots.ListAllAsync, STARCLI.GeoHotSpots.ListAllInstalledForBeamedInAvatarAsync, STARCLI.GeoHotSpots.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.GeoHotSpots.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.GeoHotSpots.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.GeoHotSpots.SearchAsync, STARCLI.GeoHotSpots.AddDependencyAsync, STARCLI.GeoHotSpots.RemoveDependencyAsync, clonePredicate: STARCLI.GeoHotSpots.CloneAsync, providerType: providerType);
                                    break;

                                case "inventoryitem":
                                    await ShowSubCommandAsync<InventoryItem>(inputArgs, "inventoryitem", "inventoryitem", STARCLI.InventoryItems.CreateAsync, STARCLI.InventoryItems.UpdateAsync, STARCLI.InventoryItems.DeleteAsync, STARCLI.InventoryItems.DownloadAndInstallAsync, STARCLI.InventoryItems.UninstallAsync, STARCLI.InventoryItems.PublishAsync, STARCLI.InventoryItems.UnpublishAsync, STARCLI.InventoryItems.RepublishAsync, STARCLI.InventoryItems.ActivateAsync, STARCLI.InventoryItems.DeactivateAsync, STARCLI.InventoryItems.ShowAsync, STARCLI.InventoryItems.ListAllCreatedByBeamedInAvatarAsync, STARCLI.InventoryItems.ListAllAsync, STARCLI.InventoryItems.ListAllInstalledForBeamedInAvatarAsync, STARCLI.InventoryItems.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.InventoryItems.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.InventoryItems.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.InventoryItems.SearchAsync, STARCLI.InventoryItems.AddDependencyAsync, STARCLI.InventoryItems.RemoveDependencyAsync, clonePredicate: STARCLI.InventoryItems.CloneAsync, providerType: providerType);
                                    break;

                                case "plugin":
                                    await ShowSubCommandAsync<Plugin>(inputArgs, "plugin", "plugin", STARCLI.Plugins.CreateAsync, STARCLI.Plugins.UpdateAsync, STARCLI.Plugins.DeleteAsync, STARCLI.Plugins.DownloadAndInstallAsync, STARCLI.Plugins.UninstallAsync, STARCLI.Plugins.PublishAsync, STARCLI.Plugins.UnpublishAsync, STARCLI.Plugins.RepublishAsync, STARCLI.Plugins.ActivateAsync, STARCLI.Plugins.DeactivateAsync, STARCLI.Plugins.ShowAsync, STARCLI.Plugins.ListAllCreatedByBeamedInAvatarAsync, STARCLI.Plugins.ListAllAsync, STARCLI.Plugins.ListAllInstalledForBeamedInAvatarAsync, STARCLI.Plugins.ListAllUninstalledForBeamedInAvatarAsync, STARCLI.Plugins.ListAllUnpublishedForBeamedInAvatarAsync, STARCLI.Plugins.ListAllDeactivatedForBeamedInAvatarAsync, STARCLI.Plugins.SearchAsync, STARCLI.Plugins.AddDependencyAsync, STARCLI.Plugins.RemoveDependencyAsync, clonePredicate: STARCLI.Plugins.CloneAsync, providerType: providerType);
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
                                    await ShowONODEMenuAsync(inputArgs);
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

                                case "cosmic":
                                    await ShowCosmicSubCommandAsync(inputArgs);
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
                                    if (CLIEngine.JsonOutput)
                                        StarCliShellOutput.WriteError(true, 1, "Command unknown.", inputArgs[0]);
                                    else
                                        CLIEngine.ShowErrorMessage("Command Unknown.");
                                    if (shellMode)
                                        Environment.ExitCode = 1;
                                    break;
                            }

                        // In shell mode, execute a single command and then exit.
                        if (shellMode)
                            exit = true;
                    }
                    else
                    {
                        //ConsoleKeyInfo keyInfo = Console.ReadKey();

                        //if (keyInfo.KeyChar == 'c' && keyInfo.Modifiers == ConsoleModifiers.Control)
                        //    exit = CLIEngine.GetConfirmation("STAR: Are you sure you wish to exit?");
                    }
                }
                catch (CLIEngineNonInteractiveInputRequiredException niex)
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 3, niex.Message, null);
                    if (shellMode)
                    {
                        Environment.ExitCode = 3;
                        exit = true;
                    }
                    else
                        OASISErrorHandling.HandleError($"STAR CLI: {niex.Message}", niex);
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError($"An unknown error occurred in STARCLI.ReadyPlayerOne. Reason: {ex}", ex);
                }
            }
            while (!exit);

            if (!CLIEngine.Quiet)
                CLIEngine.ShowMessage("Thank you for using STAR & The OASIS! We hope you enjoyed your stay, have a nice day! :)");
            Console.ForegroundColor = ConsoleColor.White;
        }

    }
}
