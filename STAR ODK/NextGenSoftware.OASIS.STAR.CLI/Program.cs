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
        private static Dictionary<string, Process> _webApiProcesses = new Dictionary<string, Process>();

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

        private static string ReadLineWithCommandHistory(List<string> commandHistory, ref int historyIndex, int startLeft, int startTop)
        {
            // Basic line reader with Up/Down arrow history. No left/right editing; typing/backspace always operate at the end.
            var buffer = new StringBuilder();
            int prevRenderLen = 0;
            int maxLen = Math.Max(0, Console.BufferWidth - startLeft);

            void Render()
            {
                Console.SetCursorPosition(startLeft, startTop);
                string text = buffer.ToString();
                Console.Write(text);
                if (prevRenderLen > text.Length)
                    Console.Write(new string(' ', prevRenderLen - text.Length));
                prevRenderLen = text.Length;
            }

            historyIndex = commandHistory.Count;
            Render();

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return buffer.ToString();
                }

                if (key.Key == ConsoleKey.UpArrow)
                {
                    if (commandHistory.Count > 0 && historyIndex > 0)
                    {
                        historyIndex--;
                        buffer.Clear();
                        buffer.Append(commandHistory[historyIndex]);
                        Render();
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.DownArrow)
                {
                    if (commandHistory.Count > 0)
                    {
                        if (historyIndex < commandHistory.Count - 1)
                        {
                            historyIndex++;
                            buffer.Clear();
                            buffer.Append(commandHistory[historyIndex]);
                        }
                        else
                        {
                            historyIndex = commandHistory.Count;
                            buffer.Clear();
                        }
                        Render();
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        Render();
                    }
                    continue;
                }

                char c = key.KeyChar;
                if (!char.IsControl(c) && buffer.Length < maxLen)
                {
                    buffer.Append(c);
                    Render();
                }
            }
        }

        /// <summary>When <see cref="CLIEngine.JsonOutput"/> is true, emit one JSON line for a holon operation result (non-interactive NFT/GeoNFT paths: mint, burn, import, export, remint, convert, place, send; STARNET holon <c>clone</c>; OAPP <c>light</c> JSON create).</summary>
        private static void EmitNiJsonForOasisResult<T>(OASISResult<T> r, string operationLabel, object successData = null)
        {
            if (!CLIEngine.JsonOutput)
                return;
            if (r == null)
            {
                StarCliShellOutput.WriteError(true, 1, $"{operationLabel}: null result", null);
                return;
            }

            if (r.IsError)
                StarCliShellOutput.WriteError(true, 1, r.Message ?? $"{operationLabel} failed", null);
            else
                StarCliShellOutput.WriteSuccess(true, string.IsNullOrEmpty(r.Message) ? $"{operationLabel} completed." : r.Message, successData);
        }

        private static async Task ShowSubCommandAsync<T>(string[] inputArgs, 
            string subCommand = "",
            string subCommandPlural = "",
            Func<ISTARNETCreateOptions<T, STARNETDNA>, object, bool, bool, ProviderType, Task> createPredicate = null,  //WEB5 Commands
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
            Func<string, Guid, bool, bool, ProviderType, int, Task> searchPredicate = null,
            Func<string, ISTARNETDNA, string, string, ProviderType, Task> addDependencyPredicate = null,
            Func<string, string, string, ProviderType, Task> removeDependencyPredicate = null,
            Func<object, Task<OASISResult<T>>> clonePredicate = null,
            Func<object, Task> mintPredicate = null, //WEB4 Commands
            Func<object, Task> burnPredicate = null,
            Func<object, Task> importPredicate = null,
            Func<object, Task> exportPredicate = null,
            //Func<object, Task> clonePredicate = null,
            Func<object, Task> convertPredicate = null,
            Func<object, ProviderType, Task> createWeb4Predicate = null,
            Func<string, ProviderType, Task> updateWeb4Predicate = null,
            //Func<string, bool, ProviderType, Task> deleteWeb4Predicate = null,
            Func<string, ProviderType, Task> showWeb4Predicate = null,
            Func<string, bool, ProviderType, Task> searchWeb4Predicate = null,
            Func<ProviderType, Task> listAllWeb4Predicate = null,
            Func<ProviderType, Task> listWeb4ForBeamedInAvatarPredicate = null,
            Func<string, string, ProviderType, Task> addWeb4NFTToCollectionPredicate = null,
            Func<string, string, ProviderType, Task> removeWeb4NFTFromCollectionPredicate = null,
            Func<string, ProviderType, Task> updateWeb3Predicate = null, //WEB3 Commands
            Func<string, bool?, bool?, ProviderType, Task<OASISResult<bool>>> deleteWeb3Predicate = null,
            Func<ProviderType, Task> listAllWeb3Predicate = null,
            Func<ProviderType, Task> listWeb3ForBeamedInAvatarPredicate = null,
            Func<string, ProviderType, Task> showWeb3Predicate = null,
            Func<string, bool, ProviderType, Task> searchWeb3Predicate = null,
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

                if (CLIEngine.NonInteractive && StarCliStarnetNonInteractiveGuard.IsWizardOnlySubcommand(subCommandParam))
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                        $"Subcommand '{subCommandParam}' is interactive-only (wizard). Omit --non-interactive for wizards.",
                        $"Entity: {subCommand}. Scriptable flows: list, show/update/delete/install/... with explicit id or GUID; oapp publish <path>; search <term>. See Docs/Devs/STAR_CLI_NonInteractive.md.");
                    return;
                }

                if (CLIEngine.NonInteractive &&
                    StarCliStarnetNonInteractiveGuard.WriteHolonSubCommandViolationIfNeeded(
                        CLIEngine.JsonOutput,
                        subCommand,
                        subCommandParam,
                        id,
                        inputArgs,
                        subCommandParam3,
                        subCommandParam4,
                        web3,
                        web4,
                        mintPredicate != null,
                        burnPredicate != null,
                        clonePredicate != null,
                        convertPredicate != null,
                        importPredicate != null,
                        exportPredicate != null,
                        addWeb4NFTToCollectionPredicate != null,
                        removeWeb4NFTFromCollectionPredicate != null,
                        addDependencyPredicate != null,
                        removeDependencyPredicate != null))
                    return;

                switch (subCommandParam)
                {
                    case "light":
                        {
                            if (!(string.Equals(subCommand, "OAPP", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(subCommand, "hApp", StringComparison.OrdinalIgnoreCase)))
                            {
                                CLIEngine.ShowErrorMessage("Command Unknown.");
                                break;
                            }

                            if (!showCreate)
                            {
                                CLIEngine.ShowErrorMessage("Command not supported.");
                                break;
                            }

                            if (!StarnetUiScriptedCreateCli.TryParseOappLightDirectArgv(inputArgs, out string oappLightOnlyJson, out string oappLightOnlyErr))
                            {
                                CLIEngine.ShowErrorMessage("Command Unknown.");
                                break;
                            }

                            if (!string.IsNullOrEmpty(oappLightOnlyErr))
                            {
                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                    oappLightOnlyErr,
                                    "Example: star --non-interactive --json oapp light ./LightRequest.json");
                                break;
                            }

                            if (!File.Exists(oappLightOnlyJson))
                            {
                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                    $"Light JSON file not found: {oappLightOnlyJson}",
                                    "See Docs/Devs/STAR_CLI_NonInteractive.md (Light JSON schema).");
                                break;
                            }

                            var oappLightOnlyOpts = new STARNETCreateOptions<OAPP, STARNETDNA>
                            {
                                STARNETHolon = new OAPP(),
                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildOappLightJsonCustomCreateParams(oappLightOnlyJson)
                            };
                            OASISResult<OAPP> lightOnlyRes = await STARCLI.OAPPs.CreateAsync(oappLightOnlyOpts, null, false, false, providerType);
                            if (CLIEngine.JsonOutput)
                                EmitNiJsonForOasisResult(lightOnlyRes, $"{subCommand} light",
                                    lightOnlyRes.Result != null ? new { id = lightOnlyRes.Result.STARNETDNA?.Id, name = lightOnlyRes.Result.STARNETDNA?.Name } : null);
                        }
                        break;

                    case "create":
                        {
                            if (showCreate)
                            {
                                if (web4)
                                {
                                    if (CLIEngine.NonInteractive)
                                    {
                                        if (createWeb4Predicate != null)
                                            await createWeb4Predicate(null, providerType);
                                        else if (createPredicate != null && !StarnetUiScriptedCreateCli.HolonLabelBypassesBaseScriptedCreate(subCommand))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParseCreateArgv(inputArgs, subCommand, out string w4Name, out string w4Desc, out string w4Cat, out string w4LibLang, out string w4Parent, out string w4Err))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    w4Err ?? "Invalid create arguments.",
                                                    "web4 flag with no web4-specific create: using same argv as web5 scripted create.");
                                                break;
                                            }

                                            var w4Opts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildScriptedCustomCreateParams(w4Name, w4Desc, w4Cat, w4Parent, w4LibLang)
                                            };
                                            await createPredicate(w4Opts, null, false, false, providerType);
                                        }
                                        else
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                "Non-interactive web4 create is not available for this entity.",
                                                "Omit web4 keyword or use a holon with scripted create. See Docs/Devs/STAR_CLI_NonInteractive.md.");
                                        }
                                    }
                                    else if (createWeb4Predicate != null)
                                        await createWeb4Predicate(null, providerType);
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else
                                {
                                    if (CLIEngine.NonInteractive)
                                    {
                                        if (StarnetUiScriptedCreateCli.HolonLabelBypassesBaseScriptedCreate(subCommand))
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                $"Non-interactive scripted create is not available for '{subCommand}' (this holon type does not delegate to STARNETUIBase scripted create).",
                                                "See StarnetUiScriptedCreateCli.HolonLabelBypassesBaseScriptedCreate in STAR.CLI.Lib and Docs/Devs/STAR_CLI_NonInteractive.md (Generic design).");
                                            break;
                                        }

                                        STARNETCreateOptions<T, STARNETDNA> scriptedOpts;
                                        if (string.Equals(subCommand, "geo-hotspot", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParseGeoHotSpotCreateArgv(inputArgs, out string ghName, out string ghDesc, out string ghType, out double ghLat, out double ghLon, out int ghRad, out string ghTrig, out int? ghTime, out string ghParent, out string ghErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    ghErr ?? "Invalid geo-hotspot create arguments.",
                                                    "Example: geo-hotspot create MyHS \"Desc\" Audio 51.5 -0.1 25 WhenArrivedAtGeoLocation [parentFolder] --audio-url https://example.com/a.mp3  |  --audio-file /path/to/local.mp3");
                                                break;
                                            }

                                            var ghParams = StarnetUiScriptedCreateCli.BuildGeoHotSpotScriptedCustomCreateParams(ghName, ghDesc, ghType, ghLat, ghLon, ghRad, ghTrig, ghTime, ghParent);
                                            StarnetUiScriptedCreateCli.ApplyGeoHotSpotMediaOptionalArgs(inputArgs, ghParams);
                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = ghParams
                                            };
                                        }
                                        else if (string.Equals(subCommand, "nft collection", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (StarnetUiScriptedCreateCli.TryParseNewWeb4NftCollectionCreateArgv(inputArgs, out string newCollName, out string newCollDesc, out string newCollErr))
                                            {
                                                if (!string.IsNullOrEmpty(newCollErr))
                                                {
                                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                        newCollErr,
                                                        "Example: nft collection create \"MyColl\" \"Description\"");
                                                    break;
                                                }

                                                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                                {
                                                    STARNETHolon = new T(),
                                                    CustomCreateParams = StarnetUiScriptedCreateCli.BuildMinimalWeb4NFTCollectionScriptedParams(newCollName, newCollDesc)
                                                };
                                            }
                                            else if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CollectionCreateArgv(inputArgs, out string wrapCollId, out string collErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    collErr ?? "Invalid nft collection create arguments.",
                                                    "Wrap: nft collection create <web4CollectionGuidOrName>  |  New: nft collection create <name> <description>");
                                                break;
                                            }
                                            else
                                            {
                                                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                                {
                                                    STARNETHolon = new T(),
                                                    CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4NFTCollectionScriptedParams(wrapCollId)
                                                };
                                            }
                                        }
                                        else if (string.Equals(subCommand, "geo-nft collection", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (StarnetUiScriptedCreateCli.TryParseNewWeb4GeoNftCollectionCreateArgv(inputArgs, out string newGeoCollName, out string newGeoCollDesc, out string newGeoCollErr))
                                            {
                                                if (!string.IsNullOrEmpty(newGeoCollErr))
                                                {
                                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                        newGeoCollErr,
                                                        "Example: geo-nft collection create \"MyColl\" \"Description\"");
                                                    break;
                                                }

                                                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                                {
                                                    STARNETHolon = new T(),
                                                    CustomCreateParams = StarnetUiScriptedCreateCli.BuildMinimalWeb4GeoNFTCollectionScriptedParams(newGeoCollName, newGeoCollDesc)
                                                };
                                            }
                                            else if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CollectionCreateArgv(inputArgs, out string wrapGeoCollId, out string gcollErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    gcollErr ?? "Invalid geo-nft collection create arguments.",
                                                    "Wrap: geo-nft collection create <web4CollectionGuidOrName>  |  New: geo-nft collection create <name> <description>");
                                                break;
                                            }
                                            else
                                            {
                                                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                                {
                                                    STARNETHolon = new T(),
                                                    CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4GeoNFTCollectionScriptedParams(wrapGeoCollId)
                                                };
                                            }
                                        }
                                        else if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CreateArgv(inputArgs, out string wrapNftId, out string wErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    wErr ?? "Invalid nft create arguments.",
                                                    "Example: nft create <web4NftGuid>");
                                                break;
                                            }

                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4NftScriptedParams(wrapNftId)
                                            };
                                        }
                                        else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CreateArgv(inputArgs, out string wrapGeoId, out string wgErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    wgErr ?? "Invalid geo-nft create arguments.",
                                                    "Example: geo-nft create <web4GeoNftGuid>");
                                                break;
                                            }

                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4GeoSpatialNftScriptedParams(wrapGeoId)
                                            };
                                        }
                                        else if (string.Equals(subCommand, "plugin", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParsePluginCreateArgv(inputArgs, out string plugName, out string plugDesc, out string plugParent, out string plugErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    plugErr ?? "Invalid plugin create arguments.",
                                                    "Example: plugin create \"MyPlugin\" \"Description\" [/optional/parent/dir]");
                                                break;
                                            }

                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildPluginScriptedCustomCreateParams(plugName, plugDesc, plugParent)
                                            };
                                        }
                                        else if ((string.Equals(subCommand, "OAPP", StringComparison.OrdinalIgnoreCase)
                                                 || string.Equals(subCommand, "hApp", StringComparison.OrdinalIgnoreCase))
                                                 && StarnetUiScriptedCreateCli.TryParseOappLightJsonCreateArgv(inputArgs, out string oappLightJson, out string oappLightErr))
                                        {
                                            if (!string.IsNullOrEmpty(oappLightErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    oappLightErr,
                                                    "Example: star --non-interactive --json oapp light ./LightRequest.json");
                                                break;
                                            }

                                            if (!System.IO.File.Exists(oappLightJson))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    $"Light JSON file not found: {oappLightJson}",
                                                    "See Docs/Devs/STAR_CLI_NonInteractive.md (Light JSON schema).");
                                                break;
                                            }

                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildOappLightJsonCustomCreateParams(oappLightJson)
                                            };
                                        }
                                        else if (!StarnetUiScriptedCreateCli.TryParseCreateArgv(inputArgs, subCommand, out string cName, out string cDesc, out string cCat, out string cLibLang, out string cParent, out string cErr))
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                cErr ?? "Invalid create arguments.",
                                                "Example: star --non-interactive oapp template create \"MyTpl\" \"Desc\" Console /optional/parent/dir");
                                            break;
                                        }
                                        else
                                        {
                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildScriptedCustomCreateParams(cName, cDesc, cCat, cParent, cLibLang)
                                            };
                                        }

                                        if (string.Equals(subCommand, "quest", StringComparison.OrdinalIgnoreCase))
                                        {
                                            scriptedOpts.CustomCreateParams ??= new Dictionary<string, object>();
                                            if (StarnetUiScriptedCreateCli.TryParseOptionalQuestObjectivesJsonPath(inputArgs, out string questObjJsonPath))
                                                scriptedOpts.CustomCreateParams[StarCliNonInteractiveCreateKeys.QuestObjectivesJsonPath] = questObjJsonPath;
                                            if (StarnetUiScriptedCreateCli.TryParseOptionalQuestLinkedHandoffArgv(inputArgs, out string qLinked, out string qHandoff))
                                            {
                                                if (!string.IsNullOrWhiteSpace(qLinked))
                                                    scriptedOpts.CustomCreateParams[StarCliNonInteractiveCreateKeys.QuestLinkedGeoHotSpotId] = qLinked.Trim();
                                                if (!string.IsNullOrWhiteSpace(qHandoff))
                                                    scriptedOpts.CustomCreateParams[StarCliNonInteractiveCreateKeys.QuestExternalHandoffUri] = qHandoff.Trim();
                                            }
                                        }

                                        if (createPredicate != null)
                                        {
                                            bool lightFromJson = scriptedOpts.CustomCreateParams != null
                                                && scriptedOpts.CustomCreateParams.ContainsKey(StarCliNonInteractiveCreateKeys.LightRequestJsonPath);
                                            if (lightFromJson && typeof(T) == typeof(OAPP))
                                            {
                                                var oappLightOpts = new STARNETCreateOptions<OAPP, STARNETDNA>
                                                {
                                                    STARNETHolon = new OAPP(),
                                                    CustomCreateParams = scriptedOpts.CustomCreateParams
                                                };
                                                if (scriptedOpts.STARNETDNA != null)
                                                    oappLightOpts.STARNETDNA = scriptedOpts.STARNETDNA;
                                                OASISResult<OAPP> lightCreateRes = await STARCLI.OAPPs.CreateAsync(oappLightOpts, null, false, false, providerType);
                                                if (CLIEngine.JsonOutput)
                                                    EmitNiJsonForOasisResult(lightCreateRes, $"{subCommand} light",
                                                        lightCreateRes.Result != null ? new { id = lightCreateRes.Result.STARNETDNA?.Id, name = lightCreateRes.Result.STARNETDNA?.Name } : null);
                                            }
                                            else
                                                await createPredicate(scriptedOpts, null, false, false, providerType);
                                        }
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    else if (createPredicate != null)
                                        await createPredicate(null, null, true, true, providerType);
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
                            {
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetMintRequestJsonPath(inputArgs, out string mintJson, out string mintErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            mintErr ?? "Invalid mint arguments.",
                                            "Example: nft mint /path/to/MintWeb4NFTRequest.json");
                                        break;
                                    }

                                    if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4NFT> mintRes = await STARCLI.NFTs.MintNFTAsync(mintJson);
                                        EmitNiJsonForOasisResult(mintRes, "nft mint",
                                            mintRes.Result != null ? new { web4NftId = mintRes.Result.Id.ToString() } : null);
                                    }
                                    else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4GeoSpatialNFT> mintGeo = await STARCLI.GeoNFTs.MintGeoNFTAsync(mintJson);
                                        EmitNiJsonForOasisResult(mintGeo, "geo-nft mint",
                                            mintGeo.Result != null ? new { web4GeoNftId = mintGeo.Result.Id.ToString() } : null);
                                    }
                                    else
                                        await mintPredicate(mintJson);
                                }
                                else
                                    await mintPredicate(null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "remint":
                        {
                            bool isNftEntity = string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase);
                            bool isGeoNftEntity = string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase);
                            string remintTarget = null;

                            if (CLIEngine.NonInteractive)
                            {
                                if (!StarCliNftStructuredArgv.TryGetRemintTargetId(inputArgs, out remintTarget, out string remintErr))
                                {
                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                        remintErr ?? "remint requires a target id.",
                                        "Example: nft remint <web4NftGuid>");
                                    break;
                                }
                            }

                            if (isNftEntity)
                            {
                                OASISResult<IWeb4NFT> remintRes = await STARCLI.NFTs.RemintNFTAsync(remintTarget);
                                EmitNiJsonForOasisResult(remintRes, "nft remint",
                                    remintRes.Result != null ? new { web4NftId = remintRes.Result.Id.ToString() } : null);
                            }
                            else if (isGeoNftEntity)
                            {
                                OASISResult<IWeb4GeoSpatialNFT> remintGeo = await STARCLI.GeoNFTs.RemintGeoNFTAsync(remintTarget);
                                EmitNiJsonForOasisResult(remintGeo, "geo-nft remint",
                                    remintGeo.Result != null ? new { web4GeoNftId = remintGeo.Result.Id.ToString() } : null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "place":
                        {
                            if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                            {
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetPlaceGeoJsonPath(inputArgs, out string placeJson, out string placeErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            placeErr ?? "Invalid place arguments.",
                                            "Example: geo-nft place /path/to/PlaceWeb4GeoSpatialNFTRequest.json");
                                        break;
                                    }

                                    OASISResult<IWeb4GeoSpatialNFT> placeRes = await STARCLI.GeoNFTs.PlaceGeoNFTFromJsonFileAsync(placeJson);
                                    EmitNiJsonForOasisResult(placeRes, "geo-nft place",
                                        placeRes.Result != null ? new { web4GeoNftId = placeRes.Result.Id.ToString() } : null);
                                }
                                else
                                    await STARCLI.GeoNFTs.PlaceGeoNFTAsync();
                            }
                            else
                                CLIEngine.ShowWarningMessage("place with JSON is supported for 'geo-nft' (WEB4).");
                        }
                        break;

                    case "burn":
                        {
                            if (burnPredicate != null)
                            {
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetBurnRequestJsonPath(inputArgs, out string burnJson, out string burnErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            burnErr ?? "Invalid burn arguments.",
                                            "Example: nft burn /path/to/BurnWeb3NFTRequest.json");
                                        break;
                                    }

                                    if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4NFT> burnRes = await STARCLI.NFTs.BurnNFTAsync(burnJson);
                                        EmitNiJsonForOasisResult(burnRes, "nft burn", null);
                                    }
                                    else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4GeoSpatialNFT> burnGeo = await STARCLI.GeoNFTs.BurnGeoNFTAsync(burnJson);
                                        EmitNiJsonForOasisResult(burnGeo, "geo-nft burn", null);
                                    }
                                    else
                                        await burnPredicate(burnJson);
                                }
                                else
                                    await burnPredicate(null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "import":
                        {
                            if (importPredicate != null)
                            {
                                bool niWeb4NftImport = CLIEngine.NonInteractive
                                    && (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase));

                                if (niWeb4NftImport)
                                {
                                    if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!StarCliNftStructuredArgv.TryResolveNftNonInteractiveImport(inputArgs, out string nftImpPath, out StarCliNftStructuredArgv.NftNonInteractiveImportKind nftImpKind, out string nftImpErr))
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                nftImpErr ?? "Invalid import arguments.",
                                                "Example: nft import /path/to/file.json  (JSON shape selects WEB3 mint vs token vs WEB4 file). Legacy: nft import web3-mint <file> | nft import web3-token <file>.");
                                            break;
                                        }

                                        switch (nftImpKind)
                                        {
                                            case StarCliNftStructuredArgv.NftNonInteractiveImportKind.Web3MintFromJson:
                                                {
                                                    OASISResult<IWeb4NFT> w3m = await STARCLI.NFTs.ImportNFTWeb3MintFromJsonFileAsync(nftImpPath);
                                                    EmitNiJsonForOasisResult(w3m, "nft import",
                                                        w3m.Result != null ? new { web4NftId = w3m.Result.Id.ToString() } : null);
                                                }
                                                break;
                                            case StarCliNftStructuredArgv.NftNonInteractiveImportKind.Web3TokenFromJson:
                                                {
                                                    OASISResult<IWeb4NFT> w3t = await STARCLI.NFTs.ImportNFTWeb3TokenFromJsonFileAsync(nftImpPath);
                                                    EmitNiJsonForOasisResult(w3t, "nft import",
                                                        w3t.Result != null ? new { web4NftId = w3t.Result.Id.ToString() } : null);
                                                }
                                                break;
                                            default:
                                                {
                                                    OASISResult<IWeb4NFT> impNft = await STARCLI.NFTs.ImportNFTAsync(nftImpPath);
                                                    EmitNiJsonForOasisResult(impNft, "nft import",
                                                        impNft.Result != null ? new { web4NftId = impNft.Result.Id.ToString() } : null);
                                                }
                                                break;
                                        }
                                    }
                                    else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!StarCliNftStructuredArgv.TryGetImportPath(inputArgs, out string importPath, out string importErr))
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                importErr ?? "Invalid import arguments.",
                                                "Example: geo-nft import /path/to/file");
                                            break;
                                        }

                                        OASISResult<IWeb4GeoSpatialNFT> impGeo = await STARCLI.GeoNFTs.ImportGeoNFTAsync(importPath);
                                        EmitNiJsonForOasisResult(impGeo, "geo-nft import",
                                            impGeo.Result != null ? new { web4GeoNftId = impGeo.Result.Id.ToString() } : null);
                                    }
                                }
                                else
                                    await importPredicate(web3);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "export":
                        {
                            if (exportPredicate != null)
                            {
                                bool niWeb4NftExport = CLIEngine.NonInteractive
                                    && (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase));

                                if (niWeb4NftExport)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetExportDest(inputArgs, out string exId, out string exPath, out string exErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            exErr ?? "Invalid export arguments.",
                                            "Example: nft export <idOrGuid> /dest/path  |  geo-nft export <idOrGuid> /dest/path");
                                        break;
                                    }

                                    if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4NFT> exNft = await STARCLI.NFTs.ExportNFTNonInteractiveAsync(exId, exPath, providerType);
                                        EmitNiJsonForOasisResult(exNft, "nft export",
                                            exNft.Result != null ? new { web4NftId = exNft.Result.Id.ToString(), destinationPath = exPath } : null);
                                    }
                                    else
                                    {
                                        OASISResult<IWeb4GeoSpatialNFT> exGeo = await STARCLI.GeoNFTs.ExportGeoNFTNonInteractiveAsync(exId, exPath, providerType);
                                        EmitNiJsonForOasisResult(exGeo, "geo-nft export",
                                            exGeo.Result != null ? new { web4GeoNftId = exGeo.Result.Id.ToString(), destinationPath = exPath } : null);
                                    }
                                }
                                else
                                    await exportPredicate(null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "clone":
                        {
                            if (clonePredicate != null)
                            {
                                object cloneArg = null;
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetFirstTokenAfterVerb(inputArgs, "clone", out string cloneSourceId, out string cloneErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            cloneErr ?? "clone requires a source id or name.",
                                            $"Example: {subCommand} clone <sourceIdOrName>");
                                        break;
                                    }

                                    cloneArg = cloneSourceId;
                                }

                                OASISResult<T> cloneRes = await clonePredicate(cloneArg);
                                if (CLIEngine.JsonOutput)
                                {
                                    object cloneSuccessData = null;
                                    if (cloneRes != null && !cloneRes.IsError && cloneRes.Result != null)
                                    {
                                        cloneSuccessData = new
                                        {
                                            id = cloneRes.Result.STARNETDNA?.Id,
                                            name = cloneRes.Result.STARNETDNA?.Name
                                        };
                                    }

                                    EmitNiJsonForOasisResult(cloneRes, $"{subCommand} clone", cloneSuccessData);
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "convert":
                        {
                            if (convertPredicate != null)
                            {
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetFirstTokenAfterVerb(inputArgs, "convert", out string convertSourceId, out string convertErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            convertErr ?? "convert requires a source id or name.",
                                            $"Example: {subCommand} convert <sourceIdOrName>");
                                        break;
                                    }

                                    bool isNftConvert = string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase);
                                    bool isGeoConvert = string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase);
                                    if (isNftConvert)
                                    {
                                        OASISResult<IWeb4NFT> cv = await STARCLI.NFTs.ConvertNFTAsync(convertSourceId);
                                        EmitNiJsonForOasisResult(cv, "nft convert", null);
                                    }
                                    else if (isGeoConvert)
                                    {
                                        OASISResult<IWeb4GeoSpatialNFT> cvGeo = await STARCLI.GeoNFTs.ConvertGeoNFTAsync(convertSourceId);
                                        EmitNiJsonForOasisResult(cvGeo, "geo-nft convert", null);
                                    }
                                    else
                                        await convertPredicate(convertSourceId);
                                }
                                else
                                    await convertPredicate(null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "send":
                        {
                            bool isNftSend = string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase);
                            bool isGeoNftSend = string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase);

                            if (CLIEngine.NonInteractive)
                            {
                                if (!StarCliNftStructuredArgv.TryGetSendArgs(inputArgs, out string sFrom, out string sTo, out string sTok, out string sMemo, out string sendErr))
                                {
                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                        sendErr ?? "Invalid send arguments.",
                                        "Example: nft send <fromWallet> <toWallet> <tokenAddress> <memo>");
                                    break;
                                }

                                if (isNftSend)
                                {
                                    OASISResult<ISendWeb4NFTResponse> sendNft = await STARCLI.NFTs.SendNFTAsync(sFrom, sTo, sTok, sMemo);
                                    EmitNiJsonForOasisResult(sendNft, "nft send",
                                        sendNft.Result != null
                                            ? new
                                            {
                                                bridgeOrderId = sendNft.Result.BridgeOrderId,
                                                sendTransactionResult = sendNft.Result.SendTransactionResult
                                            }
                                            : null);
                                }
                                else if (isGeoNftSend)
                                {
                                    OASISResult<ISendWeb4NFTResponse> sendGeo = await STARCLI.GeoNFTs.SendGeoNFTAsync(sFrom, sTo, sTok, sMemo);
                                    EmitNiJsonForOasisResult(sendGeo, "geo-nft send",
                                        sendGeo.Result != null
                                            ? new
                                            {
                                                bridgeOrderId = sendGeo.Result.BridgeOrderId,
                                                sendTransactionResult = sendGeo.Result.SendTransactionResult
                                            }
                                            : null);
                                }
                                else
                                    CLIEngine.ShowErrorMessage("Command not supported.");
                            }
                            else
                            {
                                if (isNftSend)
                                    await STARCLI.NFTs.SendNFTAsync();
                                else if (isGeoNftSend)
                                    await STARCLI.GeoNFTs.SendGeoNFTAsync();
                                else
                                    CLIEngine.ShowErrorMessage("Command not supported.");
                            }
                        }
                        break;

                    case "update":
                        {
                            if (showUpdate)
                            {
                                if (web3)
                                {
                                    id = "";

                                    if (inputArgs.Length > 3)
                                        id = inputArgs[3];

                                    if (updateWeb3Predicate != null)
                                        await updateWeb3Predicate(id, providerType);
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else if (web4)
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
                                    {
                                        object questEditParams = null;
                                        if (CLIEngine.NonInteractive && string.Equals(subCommand, "quest", StringComparison.OrdinalIgnoreCase)
                                            && StarnetUiScriptedCreateCli.TryParseQuestUpdateArgv(inputArgs, out QuestCliEditParams qEdit))
                                        {
                                            questEditParams = qEdit;
                                        }

                                        await updatePredicate(id, questEditParams, true, providerType);
                                    }
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
                                bool temp = false;
                                bool? softDelete = null;

                                if (inputArgs.Length > 3 && bool.TryParse(inputArgs[3], out temp))
                                    softDelete = temp;

                                if (web3)
                                {
                                    id = "";
                                    bool burnWeb3NFT = true;

                                    if (inputArgs.Length > 3)
                                        id = inputArgs[3];

                                    if (inputArgs.Length > 4 && bool.TryParse(inputArgs[4], out temp))
                                        softDelete = temp;

                                    if (inputArgs.Length > 5)
                                        bool.TryParse(inputArgs[5], out burnWeb3NFT);

                                    if (deleteWeb3Predicate != null)
                                    {
                                        var deleteResult = await deleteWeb3Predicate(id, softDelete, burnWeb3NFT, providerType);
                                        if (deleteResult != null && deleteResult.IsError)
                                            CLIEngine.ShowErrorMessage(deleteResult.Message);
                                    }
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else if (web4)
                                {
                                    id = "";
                                    bool? deleteChildWeb4NFTs = null;
                                    bool? deleteChildWeb3NFTs = null;
                                    bool? burnChildWeb3NFTs = null;

                                    if (inputArgs.Length > 3)
                                        id = inputArgs[3];

                                    if (inputArgs.Length > 4 && bool.TryParse(inputArgs[4], out temp))
                                        softDelete = temp;

                                    if (inputArgs.Length > 5 && bool.TryParse(inputArgs[5], out temp))
                                        deleteChildWeb4NFTs = temp;

                                    if (inputArgs.Length > 6 && bool.TryParse(inputArgs[6], out temp))
                                        deleteChildWeb3NFTs = temp;

                                    if (inputArgs.Length > 7 && bool.TryParse(inputArgs[7], out temp))
                                        burnChildWeb3NFTs = temp;

                                    switch (subCommand.ToUpper())
                                    {
                                        case "NFT":
                                            await STARCLI.NFTs.DeleteWeb4NFTAsync(id, softDelete, deleteChildWeb3NFTs, burnChildWeb3NFTs);
                                            break;

                                        case "GEONFT":
                                            await STARCLI.GeoNFTs.DeleteWeb4GeoNFTAsync(id, softDelete, deleteChildWeb3NFTs, burnChildWeb3NFTs);
                                            break;

                                        case "NFTCOLLECTION":
                                            await STARCLI.NFTCollections.DeleteWeb4NFTCollectionAsync(id, softDelete, deleteChildWeb4NFTs, deleteChildWeb3NFTs, burnChildWeb3NFTs);
                                            break;

                                        case "GEONFTCOLLECTION":
                                            await STARCLI.GeoNFTCollections.DeleteWeb4GeoNFTCollectionAsync(id, softDelete, deleteChildWeb4NFTs, deleteChildWeb3NFTs, burnChildWeb3NFTs);
                                            break;

                                        default:
                                            CLIEngine.ShowMessage("Coming Soon...");
                                            break;
                                    }

                                    //if (deleteWeb4Predicate != null)
                                    //    await deleteWeb4Predicate(id, softDelete, providerType);
                                    //else
                                    //    CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else
                                {
                                    //TODO: Temp, need to make so can pass nullable softDelete to Web5 delete functions.
                                    if (softDelete == null)
                                        softDelete = true;

                                    if (deletePredicate != null)
                                        await deletePredicate(id, softDelete.Value, providerType); //TODO: Fix later so we pass the ?bool softDelete value in like above for web4 and web3!
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
                            if (downloadAndInstallPredicate != null)
                                await downloadAndInstallPredicate(id, InstallMode.DownloadOnly, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "install":
                        {
                            if (downloadAndInstallPredicate != null)
                                await downloadAndInstallPredicate(id, InstallMode.DownloadAndInstall, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "uninstall":
                        {
                            if (uninstallPredicate != null)
                                await uninstallPredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
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

                            if (web3)
                            {
                                id = subCommandParam3;

                                if (showWeb3Predicate != null)
                                    await showWeb3Predicate(id, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else if (web4)
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
                                await addDependencyPredicate(id, null, subCommandParam3, subCommandParam4, providerType);
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
                                            if (web3)
                                            {
                                                if (listAllWeb3Predicate != null)
                                                    await listAllWeb3Predicate(providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                            else if (web4)
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
                                            if (web3)
                                            {
                                                if (listWeb3ForBeamedInAvatarPredicate != null)
                                                    await listWeb3ForBeamedInAvatarPredicate(providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                            else if (web4)
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
                            string searchCriteria;
                            int searchMax = 0;
                            if (StarCliStarnetSearchArgv.TryParse(inputArgs, out string parsedCriteria, out int parsedMax, out _))
                            {
                                searchCriteria = parsedCriteria;
                                searchMax = parsedMax;
                            }
                            else
                            {
                                searchCriteria = !string.IsNullOrWhiteSpace(subCommandParam3) ? subCommandParam3 : subCommandParam2;
                                if (CLIEngine.NonInteractive)
                                {
                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                        "search requires explicit criteria in non-interactive mode.",
                                        $"Example: {subCommand} search <criteria> [<maxResults>]  |  Global: --search-limit N");
                                    break;
                                }
                            }

                            if (CLIEngine.NonInteractive && string.IsNullOrWhiteSpace(searchCriteria))
                            {
                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                    "search requires a criteria token (name fragment or id). Example: oapp search MyOAPP 25",
                                    $"Entity: {subCommand}. Optional trailing integer limits rows (or use --search-limit N).");
                                break;
                            }

                            int effectiveSearchMax = searchMax > 0 ? searchMax : CLIEngine.MaxHolonSearchResults;

                            if (web3)
                            {
                                if (searchWeb3Predicate != null)
                                    await searchWeb3Predicate(searchCriteria, showForAllAvatars, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else if (web4)
                            {
                                if (searchWeb4Predicate != null)
                                    await searchWeb4Predicate(searchCriteria, showForAllAvatars, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else
                            {
                                if (searchPredicate != null)
                                    await searchPredicate(searchCriteria, default, showAllVersions, showForAllAvatars, providerType, effectiveSearchMax);
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
                if (CLIEngine.NonInteractive)
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                        $"Non-interactive mode requires an explicit subcommand and arguments for '{subCommand}'.",
                        "Examples: oapp list | runtime show <idOrName> | holon list. See Docs/Devs/STAR_CLI_NonInteractive.md.");
                    return;
                }

                if (string.IsNullOrEmpty(subCommandPlural))
                    subCommandPlural = $"{subCommand}'s";

                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Green;
                CLIEngine.ShowMessage($"{subCommand.ToUpper()} SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");

                int commandSpace = 22;
                int paramSpace = 23;
                string paramDivider = "  ";
                string web4Param = "";

                if (subCommand.ToUpper() == "NFT" || subCommand.ToUpper() == "GEO-NFT")
                    web4Param = "[web3] [web4]";

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

                    else if (subCommand.ToUpper() == "GEO-NFT COLLECTION")
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name} [web4]".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR GEO-NFT by wrapping around a WEB4 OASIS GEO-NFT (see notes)."), ConsoleColor.Green, false);

                    else
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Create a ", subCommand, "."), ConsoleColor.Green, false);
                }

                if (showUpdate)
                    CLIEngine.ShowMessage(string.Concat("    update".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Update an existing ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);

                if (showDelete)
                    CLIEngine.ShowMessage(string.Concat("    delete".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Delete an existing ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "NFT" || subCommand.ToUpper() == "GEO-NFT")
                {
                    CLIEngine.ShowMessage(string.Concat("    remint".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Remint an existing Web4 OASIS ", subCommand, " for the given {id} or {name} to create new Web3 Varients."), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    burn".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Burn's a OASIS ", subCommand, " for the given {id} or {name}"), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    send".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Send a OASIS ", subCommand, " for the given {id} or {name} to another wallet cross-chain."), ConsoleColor.Green, false);

                    if (subCommand.ToUpper() == "NFT")
                        CLIEngine.ShowMessage(string.Concat("    import".PadRight(commandSpace), "{id/name} [web3]".PadRight(paramSpace), paramDivider, "Imports a OASIS ", subCommand, " JSON file for the given {id} or {name}."), ConsoleColor.Green, false);
                    else
                        CLIEngine.ShowMessage(string.Concat("    import".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Imports a OASIS ", subCommand, " JSON file for the given {id} or {name}."), ConsoleColor.Green, false);

                    CLIEngine.ShowMessage(string.Concat("    export".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Exports a OASIS ", subCommand, " for the given {id} or {name} as a JSON file as well as a WEB3 JSON MetaData file."), ConsoleColor.Green, false);CLIEngine.ShowMessage(string.Concat("    burn".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Burn's a OASIS ", subCommand, " for the given {id} or {name}"), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    convert".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Allows the minting of different WEB3 NFT Standards for different chains from the same OASIS WEB4 Metadata."), ConsoleColor.Green, false);
                }

                if (subCommand.ToUpper() == "GEO-NFT")
                    CLIEngine.ShowMessage(string.Concat("    place".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Create a OASIS Geo-NFT from an existing OASIS NFT for the given {id} or {name} and place within Our World."), ConsoleColor.Green, false);

                CLIEngine.ShowMessage(string.Concat("    clone".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Clones a OASIS ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    adddependency".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Adds a dependency to the ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    removedependency".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Removes a dependency from the ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
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
                    CLIEngine.ShowMessage($"For the update, delete, list, show or search command, if [web3] is included it will update/delete/list/show/search WEB3 NFT's, if [web4] is included it will update/delete/list/show/search WEB4 OASIS NFT's, otherwise it will update/delete/list/show/search WEB5 STAR NFT's.", ConsoleColor.Green);                    
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
                            if (STAR.BeamedInAvatar != null)
                            {
                                CLIEngine.ShowErrorMessage($"Avatar {STAR.BeamedInAvatar.Username} Already Beamed In. Please Beam Out First!");
                                break;
                            }

                            if (CLIEngine.NonInteractive && inputArgs.Length >= 4)
                            {
                                string verify = Environment.GetEnvironmentVariable("STAR_CLI_EMAIL_VERIFY_TOKEN");
                                await STARCLI.Avatars.BeamInWithCredentialsAsync(inputArgs[2], inputArgs[3], verify);
                            }
                            else if (CLIEngine.NonInteractive)
                            {
                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                    "Non-interactive beam-in requires: avatar beamin <username> <password>",
                                    "Or set STAR_CLI_USERNAME / STAR_CLI_PASSWORD before boot (see STAR_CLI_NonInteractive.md).");
                            }
                            else
                                await STARCLI.Avatars.BeamInAvatar();
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

                    case "inventory":
                        {
                            bool detailed = inputArgs.Length > 2 && inputArgs[2].ToLower() == "detailed";
                            await STARCLI.Avatars.ShowAvatarInventoryAsync(detailed);
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
                CLIEngine.ShowMessage("    inventory [detailed]       List inventory items for the currently beamed-in avatar (WEB4 avatar API).", ConsoleColor.Green, false);
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

        private static async Task ShowKeysSubCommandAsync(string[] inputArgs, ProviderType providerType = ProviderType.Default)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "link":
                        {
                            if (inputArgs.Length > 2 && inputArgs[2].ToLower() == "private")
                                await STARCLI.Keys.LinkProviderPrivateKeyToBeamedInAvatarWalletAsync(providerType);

                            else if (inputArgs.Length > 2 && inputArgs[2].ToLower() == "public")
                                await STARCLI.Keys.LinkProviderPublicKeyToBeamedInAvatarWalletAsync(providerType);

                            else if (inputArgs.Length > 2 && inputArgs[2].ToLower() == "walletaddress")
                                await STARCLI.Keys.LinkProviderWalletAddressToBeamedInAvatarWalletAsync(providerType);

                            else if (inputArgs.Length > 2 && inputArgs[2].ToLower() == "generate")
                                STARCLI.Keys.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToBeamedInAvatarWallet(providerType);

                            else
                                await STARCLI.Keys.LinkProviderKeyToBeamedInAvatarWalletAsync(providerType);
                        }
                        break;

                    case "list":
                        {
                            if (inputArgs.Length > 2 && inputArgs[2].ToLower() == "private")
                                STARCLI.Keys.ListAllProviderPrivateKeysForBeamedInAvatar(providerType);

                            else if (inputArgs.Length > 2 && inputArgs[2].ToLower() == "public")
                                STARCLI.Keys.ListAllProviderPublicKeysForBeamedInAvatar(providerType);

                            else if (inputArgs.Length > 2 && inputArgs[2].ToLower() == "walletaddress")
                                STARCLI.Keys.ListAllProviderWalletAddressesForBeamedInAvatar(providerType);

                            else if (inputArgs.Length > 2 && inputArgs[2].ToLower() == "keypair")
                                STARCLI.Keys.ListAllProviderKeyPairsForBeamedInAvatar(providerType);

                            else if (inputArgs.Length > 2 && inputArgs[2].ToLower() == "storage")
                                STARCLI.Keys.ListAllProviderUniqueStorageKeysForBeamedInAvatar(providerType);

                            else
                                STARCLI.Keys.ListAllProviderKeysForBeamedInAvatar(providerType);
                        }
                        break;

                    case "generate":
                        STARCLI.Keys.GenerateKeyPairWithWallet(providerType);
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
                CLIEngine.ShowMessage("    link [private/public/walletaddress/generate]         Links a OASIS Provider Key (private, public or wallet address) to the beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    list [private/public/walletaddress/keypair/storage]  Shows the keys (private, public, wallet address, keypair or storage) for the beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    generate                                             Generates a unique keyvalue pair of private/public/wallet address keys.", ConsoleColor.Green, false);

                CLIEngine.ShowMessage("NOTES:", ConsoleColor.Green);
                CLIEngine.ShowMessage("For the link sub-command, if [generate] is included it will generate a keyvalue pair (and wallet address) and then link.", ConsoleColor.Green);
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

        private static bool IsReservedWalletImportKind(string token)
        {
            if (string.IsNullOrEmpty(token))
                return true;
            if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(token, "privatekey", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "publickey", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "secretphase", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "json", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private static async Task ShowWalletSubCommandAsync(string[] inputArgs, ProviderType providerType = ProviderType.Default)
        {
            bool? showOnlyDefault = null;
            bool? showPrivateKeys = null;
            bool? showSecretWords = null;
            string param = "";

            if (inputArgs.Contains("default"))
                showOnlyDefault = true;

            if (inputArgs.Contains("showprivatekeys"))
                showPrivateKeys = true;

            if (inputArgs.Contains("showsecretwords"))
                showSecretWords = true;

            if (inputArgs.Length > 3 && !string.IsNullOrEmpty(inputArgs[3]))
                param = inputArgs[3];

            //if (inputArgs.Length > 2 && inputArgs[2] == "default")
            //    showOnlyDefault = true;

            //if (inputArgs.Length > 3 && inputArgs[3] == "showprivatekeys")
            //    showPrivateKeys = true;

            //if (inputArgs.Length > 4 && inputArgs[4] == "showsecretwords")
            //    showSecretWords = true;

            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "create":
                        await STARCLI.Wallets.CreateWalletAsync();
                        break;

                    case "sendtoken":
                        await STARCLI.Wallets.SendToken(providerType);
                        break;

                    case "show":
                        {
                            string key = "";

                            if (inputArgs.Length > 2 && !string.IsNullOrEmpty(inputArgs[2]))
                                key = inputArgs[2];

                            STARCLI.Wallets.ShowWalletThatPublicKeyBelongsTo(key, showPrivateKeys, showSecretWords);
                        }
                        
                        break;

                    case "showdefault":
                        {
                            await STARCLI.Wallets.ShowDefaultWalletForBeamedInAvatarAsync(showPrivateKeys, showSecretWords);
                        }
                        break;

                    case "setdefault":
                        await STARCLI.Wallets.SetDefaultWalletAsync();
                        break;

                    case "import":
                        {
                            if (inputArgs.Length >= 3 && !string.IsNullOrWhiteSpace(inputArgs[2]))
                            {
                                string importTok = inputArgs[2].Trim();
                                if (string.Equals(importTok, "all", StringComparison.OrdinalIgnoreCase) && inputArgs.Length >= 4)
                                {
                                    string bulkJson = inputArgs[3]?.Trim();
                                    if (!string.IsNullOrEmpty(bulkJson) && File.Exists(bulkJson)
                                        && string.Equals(Path.GetExtension(bulkJson), ".json", StringComparison.OrdinalIgnoreCase))
                                    {
                                        await STARCLI.Wallets.ImportAllWalletsUsingJSONFileAsync(bulkJson, providerType);
                                        break;
                                    }
                                }
                                else if (!IsReservedWalletImportKind(importTok)
                                         && File.Exists(importTok)
                                         && string.Equals(Path.GetExtension(importTok), ".json", StringComparison.OrdinalIgnoreCase))
                                {
                                    await STARCLI.Wallets.ImportWalletUsingJSONFileAsync(importTok, providerType);
                                    break;
                                }
                            }

                            if (inputArgs.Length > 2 && !string.IsNullOrEmpty(inputArgs[2]))
                            {
                                switch (inputArgs[2])
                                {
                                    case "privateKey":
                                        STARCLI.Wallets.ImportWalletUsingPrivateKey(providerType);
                                        break;

                                    case "publicKey":
                                        STARCLI.Wallets.ImportWalletUsingPublicKey(providerType);
                                        break;

                                    case "secretPhase":
                                        await STARCLI.Wallets.ImportWalletUsingSecretRecoveryPhaseAsync(providerType);
                                        break;

                                    case "json":
                                        {
                                            if (inputArgs.Contains("all"))
                                            {
                                                param = "";
                                                if (inputArgs.Length > 5 && !string.IsNullOrEmpty(inputArgs[5]))
                                                    param = inputArgs[5];

                                                await STARCLI.Wallets.ImportAllWalletsUsingJSONFileAsync(param, providerType);
                                            }
                                            else
                                            {
                                                param = "";
                                                if (inputArgs.Length > 4 && !string.IsNullOrEmpty(inputArgs[4]))
                                                    param = inputArgs[4];

                                                await STARCLI.Wallets.ImportWalletUsingJSONFileAsync(param, providerType);
                                            }
                                        }
                                        break;

                                    default:
                                        CLIEngine.ShowWarningMessage("You need to enter privateKey, publicKey, secretPhase or json");
                                        break;
                                }
                            }
                            else
                                CLIEngine.ShowWarningMessage("You need to enter privateKey, publicKey, secretPhase or json");
                        }
                        break;

                    case "export":
                        {
                            if (inputArgs.Contains("all"))
                                await STARCLI.Wallets.ExportAllWalletsAsync(providerType);
                            else
                                await STARCLI.Wallets.ExportWalletAsync(param, providerType);
                        }
                        break;

                    case "update":
                        await STARCLI.Wallets.UpdateWallet(providerType);
                        break;

                    case "list":
                        {
                            await STARCLI.Wallets.ListProviderWalletsForBeamedInAvatarAsync(showOnlyDefault: showOnlyDefault.HasValue ? showOnlyDefault.Value : false, showPrivateKeys: showPrivateKeys.HasValue ? showPrivateKeys.Value : false, showSecretWords: showSecretWords.HasValue ? showSecretWords.Value : false, providerTypeToLoadFrom: providerType);
                        }
                        break;

                    case "balance":
                        {
                            if (inputArgs.Length > 2 && inputArgs[2] != null)
                                await STARCLI.Wallets.GetBalanceAsync(inputArgs[2]);
                            else
                                await STARCLI.Wallets.GetTotalBalance();
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
                CLIEngine.ShowMessage($"WALLET SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    create                                                              Creates a wallet for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    update                                                              Updates a wallet for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    show               [publickey] [showprivatekeys] [showsecretwords]  Shows the wallet that the public key belongs to.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    showdefault        [showprivatekeys] [showsecretwords]              Shows the default wallet for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    setdefault         [walletId]                                       Sets the default wallet for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    sendtoken          [walletAddress]                                  Sends a token to the given wallet address.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import privateKey  {privatekey}                                     Imports a wallet using the privateKey.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import publicKey   {publickey}                                      Imports a wallet using the publicKey.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import secretPhase {secretPhase}                                    Imports a wallet using the secretPhase.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import             {file.json}                                      Imports one wallet from export JSON (shorthand for import json).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import all         {jsonFile}                                       Imports all wallets from export-all JSON.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    import json        [all] {jsonFile}                                 Same as import / import all (legacy).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    export             [all] {walletId}                                 Exports all/a wallet(s) to a json file.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    list               [default] [showprivatekeys] [showsecretwords]    Lists the wallets for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    balance                                                             Gets the total balance for all wallets for the currently beamed in avatar.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    balance            {walletId} [providerType]                        Gets the balance for the given wallet for the currently beamed in avatar.", ConsoleColor.Green, false);

                CLIEngine.ShowMessage("NOTES:", ConsoleColor.Green);
                CLIEngine.ShowMessage("For the import sub-command, if [all] is included it will import a collection of wallets (from a previous 'export all' sub-command). If it is omitted it will import a singular wallet (from a previous 'export' sub-command).", ConsoleColor.Green);
                CLIEngine.ShowMessage("For the list sub-command, if [default] param is included it will only list the default wallets.", ConsoleColor.Green);
                CLIEngine.ShowMessage("For the list, show and showdefault sub-commands, if [showprivatekeys] param is included it will decrypt and show the private keys, likewise if [showsecretwords] is included it will decrypt and show the secret words.", ConsoleColor.Green);
                
                CLIEngine.ShowMessage("You can also create a wallet by linking a private key, public key or wallet address to your avatar using the keys sub-commands.", ConsoleColor.Green);
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

        private static async Task ShowCosmicSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "body":
                    case "celestialbody":
                        {
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "create":
                                    case "add":
                                        await STARCLI.COSMIC.CreateCelestialBodyWizardAsync();
                                        break;

                                    case "read":
                                    case "show":
                                    case "get":
                                        await STARCLI.COSMIC.ReadCelestialBodyWizardAsync();
                                        break;

                                    case "update":
                                    case "edit":
                                        await STARCLI.COSMIC.UpdateCelestialBodyWizardAsync();
                                        break;

                                    case "delete":
                                    case "remove":
                                        await STARCLI.COSMIC.DeleteCelestialBodyWizardAsync();
                                        break;

                                    case "list":
                                        await STARCLI.COSMIC.ListCelestialBodiesWizardAsync();
                                        break;

                                    case "search":
                                    case "find":
                                        await STARCLI.COSMIC.SearchCelestialBodiesWizardAsync();
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown. Available commands: create, read, update, delete, list, search, find");
                                        break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("");
                                CLIEngine.ShowMessage($"COSMIC CELESTIAL BODY SUBCOMMANDS:", ConsoleColor.Green);
                                Console.WriteLine("");
                                CLIEngine.ShowMessage("    create/add        Create a new celestial body using the wizard.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    read/show/get      Read/display a celestial body by ID or name.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    update/edit        Update an existing celestial body using the wizard.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    delete/remove      Delete a celestial body by ID or name.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    list               List all celestial bodies.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    search/find        Search/find celestial bodies by ID, name or description.", ConsoleColor.Green, false);
                            }
                        }
                        break;

                    case "space":
                    case "celestialspace":
                        {
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "create":
                                    case "add":
                                        await STARCLI.COSMIC.CreateCelestialSpaceWizardAsync();
                                        break;

                                    case "read":
                                    case "show":
                                    case "get":
                                        await STARCLI.COSMIC.ReadCelestialSpaceWizardAsync();
                                        break;

                                    case "update":
                                    case "edit":
                                        await STARCLI.COSMIC.UpdateCelestialSpaceWizardAsync();
                                        break;

                                    case "delete":
                                    case "remove":
                                        await STARCLI.COSMIC.DeleteCelestialSpaceWizardAsync();
                                        break;

                                    case "list":
                                        await STARCLI.COSMIC.ListCelestialSpacesWizardAsync();
                                        break;

                                    case "search":
                                    case "find":
                                        await STARCLI.COSMIC.SearchCelestialSpacesWizardAsync();
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown. Available commands: create, read, update, delete, list, search, find");
                                        break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("");
                                CLIEngine.ShowMessage($"COSMIC CELESTIAL SPACE SUBCOMMANDS:", ConsoleColor.Green);
                                Console.WriteLine("");
                                CLIEngine.ShowMessage("    create/add        Create a new celestial space using the wizard.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    read/show/get      Read/display a celestial space by ID or name.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    update/edit        Update an existing celestial space using the wizard.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    delete/remove      Delete a celestial space by ID or name.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    list               List all celestial spaces.", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    search/find        Search/find celestial spaces by ID, name or description.", ConsoleColor.Green, false);
                            }
                        }
                        break;

                    case "find":
                        {
                            if (inputArgs.Length > 2)
                            {
                                string idOrName = string.Join(" ", inputArgs.Skip(2));
                                var result = await STARCLI.COSMIC.FindAsync("find", idOrName);
                                if (!result.IsError && result.Result != null)
                                {
                                    CLIEngine.ShowSuccessMessage("Found:");
                                    STARCLI.Holons.ShowHolonProperties(result.Result);
                                }
                                else
                                {
                                    CLIEngine.ShowErrorMessage($"Error: {result.Message}");
                                }
                            }
                            else
                            {
                                var result = await STARCLI.COSMIC.FindAsync("find");
                                if (!result.IsError && result.Result != null)
                                {
                                    CLIEngine.ShowSuccessMessage("Found:");
                                    STARCLI.Holons.ShowHolonProperties(result.Result);
                                }
                                else
                                {
                                    CLIEngine.ShowErrorMessage($"Error: {result.Message}");
                                }
                            }
                        }
                        break;

                    case "scenarios":
                    case "scenario":
                    case "createscenario":
                    case "createusecase":
                    case "createcommonusecase":
                        {
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "universe":
                                    case "createuniverse":
                                        await STARCLI.COSMIC.CreateUniverseWithChildrenScenarioAsync();
                                        break;

                                    case "multiverse":
                                    case "createmultiverse":
                                        await STARCLI.COSMIC.CreateMultiverseWithChildrenScenarioAsync();
                                        break;

                                    case "galaxy":
                                    case "creategalaxy":
                                        await STARCLI.COSMIC.CreateGalaxyWithChildrenScenarioAsync();
                                        break;

                                    case "solarsystem":
                                    case "createsolarsystem":
                                        await STARCLI.COSMIC.CreateSolarSystemWithChildrenScenarioAsync();
                                        break;

                                    case "planet":
                                    case "createplanet":
                                        await STARCLI.COSMIC.CreatePlanetWithChildrenScenarioAsync();
                                        break;

                                    case "star":
                                    case "createstar":
                                        await STARCLI.COSMIC.CreateStarWithChildrenScenarioAsync();
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown. Available scenarios: universe, multiverse, galaxy, solarsystem, planet, star");
                                        break;
                                }
                            }
                            else
                            {
                                await STARCLI.COSMIC.ShowScenariosMenuAsync();
                            }
                        }
                        break;

                    case "simulation":
                        {
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "propose":
                                        await STARCLI.COSMIC.SimulationProposeWizardAsync();
                                        break;

                                    case "list":
                                        {
                                            if (inputArgs.Length > 3 && inputArgs[3].ToLower() == "proposals")
                                            {
                                                bool onlyMine = inputArgs.Length > 4 && inputArgs[4].ToLower() == "onlymine";
                                                await STARCLI.COSMIC.SimulationListProposalsWizardAsync(onlyMine);
                                            }
                                            else
                                            {
                                                await STARCLI.COSMIC.SimulationListWizardAsync();
                                            }
                                        }
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown. Available commands: propose, list, list proposals [onlymine]");
                                        break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("");
                                CLIEngine.ShowMessage($"COSMIC SIMULATION SUBCOMMANDS:", ConsoleColor.Green);
                                Console.WriteLine("");
                                CLIEngine.ShowMessage("    propose              Create a proposal for The Grand Simulation", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    list                  List content of The Grand Simulation", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    list proposals        List all simulation proposals", ConsoleColor.Green, false);
                                CLIEngine.ShowMessage("    list proposals onlymine  List only your proposals", ConsoleColor.Green, false);
                            }
                        }
                        break;

                    case "magicverse":
                    case "listmagicverse":
                        {
                            await STARCLI.COSMIC.ListMagicVerseWizardAsync();
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown. Available commands: body, space, find, scenarios, simulation, magicverse");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"COSMIC SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    body/celestialbody    Manage celestial bodies (stars, planets, moons, etc.)", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    space/celestialspace   Manage celestial spaces (omniverse, multiverse, universe, etc.)", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    find                   Find a celestial body/space by ID or name", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    scenarios              Common use case scenarios (create with full child hierarchy)", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    simulation             The Grand Simulation (proposals and content)", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    magicverse             List MagicVerse content (read-only)", ConsoleColor.Green, false);
                Console.WriteLine("");
                CLIEngine.ShowMessage("Examples:", ConsoleColor.Yellow);
                CLIEngine.ShowMessage("    cosmic body create              Create a new celestial body (asks for parent and type)", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmic body list                List celestial bodies (optionally for a parent)", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmic space create             Create a new celestial space (asks for parent and type)", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmic space list               List celestial spaces (optionally for a parent)", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmic find                     Find by ID or name", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmic scenarios                Show scenarios menu", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmic scenarios universe       Create universe with children", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmic simulation propose       Create a proposal for The Grand Simulation", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmic simulation list proposals  List all simulation proposals", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmic magicverse                List MagicVerse content", ConsoleColor.Green, false);
            }
        }

        private static async Task ShowConfigSubCommandAsync(string[] inputArgs)
        {
            Console.WriteLine("");
            if (inputArgs.Length > 1 && inputArgs[1].ToLower() == "dna")
            {
                ShowDNAPaths();
                Console.WriteLine("");
                return;
            }
            ShowDNAPaths();
            Console.WriteLine("");
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "dna":
                        // Handled above
                        break;

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

                    case "logproviderswitching":
                        {
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "enabled":
                                        {
                                            ProviderManager.Instance.OASISDNA.OASIS.StorageProviders.LogSwitchingProviders = true;
                                            CLIEngine.ShowSuccessMessage("OASIS Hyperdrive Provider Switching Logging: Enabled.");
                                        }
                                        break;

                                    case "disabled":
                                        {
                                            ProviderManager.Instance.OASISDNA.OASIS.StorageProviders.LogSwitchingProviders = false;
                                            CLIEngine.ShowSuccessMessage("OASIS Hyperdrive Provider Switching Logging: Disabled.");
                                        }
                                        break;

                                    case "status":
                                        {
                                            if (ProviderManager.Instance.OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
                                                CLIEngine.ShowMessage("OASIS Hyperdrive Provider Switching Logging: Enabled.");
                                            else
                                                CLIEngine.ShowMessage("OASIS Hyperdrive Provider Switching Logging: Disabled.");
                                        }
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown.");
                                        break;
                                }
                            }
                            else
                            {
                                if (ProviderManager.Instance.OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
                                    CLIEngine.ShowMessage("OASIS Hyperdrive Provider Switching Logging: Enabled.");
                                else
                                    CLIEngine.ShowMessage("OASIS Hyperdrive Provider Switching Logging: Disabled.");
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
                CLIEngine.ShowMessage("    dna                       Shows paths to DNATemplates, OASIS DNA and STAR DNA.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmicdetailedoutput     [enable/disable/status] Enables/disables COSMIC Detailed Output.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    starstatusdetailedoutput [enable/disable/status] Enables/disables STAR ODK Detailed Output.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    logproviderswitching     [enable/disable/status] Enables/disables OASIS Hyperdrive Provider Switching Logging.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        // ─── ONODE Commands ────────────────────────────────────────────────────────
        // All onode commands route through ONODEService supervisor API (127.0.0.1:8765).
        // Falls back to direct process spawn if supervisor is not installed/running.

        private static async Task ShowONODEMenuAsync(string[] inputArgs)
        {
            if (inputArgs.Length <= 1)
            {
                ShowONODEHelp();
                return;
            }

            // Parse --hidden / --visible / --minimised flags
            string? windowMode = null;
            if (inputArgs.Any(a => a.Equals("--hidden",    StringComparison.OrdinalIgnoreCase))) windowMode = "Hidden";
            if (inputArgs.Any(a => a.Equals("--visible",   StringComparison.OrdinalIgnoreCase))) windowMode = "Visible";
            if (inputArgs.Any(a => a.Equals("--minimised", StringComparison.OrdinalIgnoreCase))) windowMode = "Minimised";

            // Service/group target: first non-flag arg after the subcommand
            string target = inputArgs.Length > 2 ? inputArgs[2].ToLower() : "all";
            if (target.StartsWith("--")) target = "all";

            using var client = new NextGenSoftware.OASIS.ONODE.Client.SupervisorClient();

            switch (inputArgs[1].ToLower())
            {
                case "start":
                    await ONODEStartAsync(client, target, windowMode);
                    break;

                case "stop":
                    await ONODEStopAsync(client, target);
                    break;

                case "restart":
                    await ONODERestartAsync(client, target, windowMode);
                    break;

                case "status":
                    await ONODEStatusAsync(client);
                    break;

                case "logs":
                    string? logService = target == "all" ? null : target;
                    int lines = 100;
                    var linesArg = inputArgs.FirstOrDefault(a => a.StartsWith("--lines="));
                    if (linesArg != null && int.TryParse(linesArg.Split('=')[1], out var l)) lines = l;
                    bool follow = inputArgs.Any(a => a.Equals("--follow", StringComparison.OrdinalIgnoreCase));
                    await ONODELogsAsync(client, logService, lines, follow);
                    break;

                case "metrics":
                    await ONODEMetricsAsync(client);
                    break;

                case "config":
                    bool edit = inputArgs.Any(a => a.Equals("--edit", StringComparison.OrdinalIgnoreCase));
                    await ONODEConfigAsync(client, edit);
                    break;

                case "providers":
                    await ONODEProvidersAsync(client, inputArgs);
                    break;

                case "startprovider":
                    if (inputArgs.Length > 2) await StartONODEProviderAsync(inputArgs[2]);
                    else CLIEngine.ShowErrorMessage("Usage: onode startprovider {ProviderName}");
                    break;

                case "stopprovider":
                    if (inputArgs.Length > 2) await StopONODEProviderAsync(inputArgs[2]);
                    else CLIEngine.ShowErrorMessage("Usage: onode stopprovider {ProviderName}");
                    break;

                case "service":
                    await ONODEServiceCommandAsync(inputArgs);
                    break;

                default:
                    CLIEngine.ShowErrorMessage($"Unknown onode subcommand: {inputArgs[1]}");
                    ShowONODEHelp();
                    break;
            }
        }

        private static async Task ONODEStartAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string target, string? windowMode)
        {
            if (!client.IsAvailable)
            {
                CLIEngine.ShowWarningMessage("ONODEService not running — falling back to direct process spawn.");
                await ONODEStartDirectFallbackAsync(target, windowMode);
                return;
            }

            CLIEngine.ShowWorkingMessage($"Starting {target.ToUpper()}...");
            bool isGroup = new[] { "all","core","ai","extended" }.Contains(target) || target.Contains(",");
            bool isSingle = target.StartsWith("web");

            if (target.Contains(","))
            {
                var ids = target.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                await client.StartManyAsync(ids, windowMode);
            }
            else if (isSingle)
                await client.StartAsync(target, windowMode);
            else
                await client.StartGroupAsync(target, windowMode);

            CLIEngine.ShowSuccessMessage($"Start command sent for {target.ToUpper()}.");
        }

        private static async Task ONODEStopAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string target)
        {
            if (!client.IsAvailable)
            {
                CLIEngine.ShowWarningMessage("ONODEService not running — cannot stop via supervisor.");
                return;
            }

            CLIEngine.ShowWorkingMessage($"Stopping {target.ToUpper()}...");
            if (target.Contains(","))
            {
                var ids = target.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                await client.StopManyAsync(ids);
            }
            else if (target.StartsWith("web"))
                await client.StopAsync(target);
            else
                await client.StopGroupAsync(target);

            CLIEngine.ShowSuccessMessage($"Stop command sent for {target.ToUpper()}.");
        }

        private static async Task ONODERestartAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string target, string? windowMode)
        {
            if (!client.IsAvailable) { CLIEngine.ShowWarningMessage("ONODEService not running."); return; }
            CLIEngine.ShowWorkingMessage($"Restarting {target.ToUpper()}...");
            if (target.StartsWith("web")) await client.RestartAsync(target, windowMode);
            else await client.RestartGroupAsync(target, windowMode);
            CLIEngine.ShowSuccessMessage($"Restart command sent for {target.ToUpper()}.");
        }

        private static async Task ONODEStatusAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client)
        {
            if (!client.IsAvailable)
            {
                CLIEngine.ShowWarningMessage("ONODEService not running. Install it with: onode service install");
                return;
            }

            var status = await client.GetStatusAsync();
            if (status == null) { CLIEngine.ShowErrorMessage("Failed to retrieve status."); return; }

            Console.WriteLine("");
            CLIEngine.ShowMessage($"ONODE SUPERVISOR STATUS", ConsoleColor.Cyan);
            CLIEngine.ShowMessage($"  Node ID : {status.NodeId}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Version : {status.Version}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Uptime  : {FormatUptime((DateTime.UtcNow - status.StartedAt).TotalSeconds)}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Peers   : {status.Metrics.TotalPeers}", ConsoleColor.White, false);
            Console.WriteLine("");
            CLIEngine.ShowMessage($"  {"SERVICE",-10} {"STATUS",-12} {"PID",-8} {"PORT",-6} {"UPTIME",-12} {"RESTARTS"}", ConsoleColor.Green);
            foreach (var svc in status.Services)
            {
                var col = svc.Status == "Running" ? ConsoleColor.Green :
                          svc.Status == "Stopped" ? ConsoleColor.Gray :
                          svc.Status == "Degraded" || svc.Status == "Crashed" ? ConsoleColor.Red :
                          ConsoleColor.Yellow;
                var pid = svc.Pid.HasValue ? svc.Pid.ToString() : "-";
                CLIEngine.ShowMessage($"  {svc.Id.ToUpper(),-10} {svc.Status,-12} {pid,-8} {svc.Port,-6} {FormatUptime(svc.UptimeSeconds),-12} {svc.RestartCount}", col, false);
            }
            Console.WriteLine("");
        }

        private static async Task ONODELogsAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string? serviceId, int lines, bool follow)
        {
            if (!client.IsAvailable) { CLIEngine.ShowWarningMessage("ONODEService not running."); return; }

            do
            {
                var entries = await client.GetLogsAsync(serviceId, lines);
                if (entries != null)
                {
                    Console.Clear();
                    foreach (var e in entries)
                    {
                        var col = e.IsError ? ConsoleColor.Red : ConsoleColor.Gray;
                        CLIEngine.ShowMessage($"[{e.Timestamp:HH:mm:ss}] [{e.ServiceId.ToUpper()}] {e.Message}", col, false);
                    }
                }
                if (follow) await Task.Delay(2000);
            } while (follow);
        }

        private static async Task ONODEMetricsAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client)
        {
            if (!client.IsAvailable) { CLIEngine.ShowWarningMessage("ONODEService not running."); return; }

            var metrics = await client.GetMetricsAsync();
            if (metrics == null) { CLIEngine.ShowErrorMessage("Failed to retrieve metrics."); return; }

            Console.WriteLine("");
            CLIEngine.ShowMessage("ONODE AGGREGATE METRICS", ConsoleColor.Cyan);
            CLIEngine.ShowMessage($"  Total Peers       : {metrics.Aggregate.TotalPeers}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Bytes Read/s      : {FormatBytes(metrics.Aggregate.TotalBytesReadPerSec)}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Bytes Written/s   : {FormatBytes(metrics.Aggregate.TotalBytesWrittenPerSec)}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Requests/s        : {metrics.Aggregate.TotalRequestsPerSec:F1}", ConsoleColor.White, false);
            Console.WriteLine("");
            CLIEngine.ShowMessage($"  {"SERVICE",-10} {"PEERS",-8} {"READ/s",-12} {"WRITE/s",-12} {"REQ/s",-8} {"LATENCY ms"}", ConsoleColor.Green);
            foreach (var (id, m) in metrics.Services)
                CLIEngine.ShowMessage($"  {id.ToUpper(),-10} {m.PeersConnected,-8} {FormatBytes(m.BytesReadPerSec),-12} {FormatBytes(m.BytesWrittenPerSec),-12} {m.RequestsPerSec,-8:F1} {m.AvgLatencyMs:F1}", ConsoleColor.White, false);
            Console.WriteLine("");
        }

        private static async Task ONODEConfigAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, bool edit)
        {
            if (!client.IsAvailable) { CLIEngine.ShowWarningMessage("ONODEService not running."); return; }

            var config = await client.GetConfigAsync();
            if (config == null) { CLIEngine.ShowErrorMessage("Could not read OASISDNA.json."); return; }

            if (edit)
            {
                // Write to temp file and open in $EDITOR
                var tmp = Path.Combine(Path.GetTempPath(), "OASISDNA_edit.json");
                await File.WriteAllTextAsync(tmp, config);
                var editor = Environment.GetEnvironmentVariable("EDITOR") ?? (OperatingSystem.IsWindows() ? "notepad" : "nano");
                var psi = new ProcessStartInfo(editor, $"\"{tmp}\"") { UseShellExecute = true };
                var proc = Process.Start(psi);
                proc?.WaitForExit();
                var updated = await File.ReadAllTextAsync(tmp);
                await client.UpdateConfigAsync(updated);
                CLIEngine.ShowSuccessMessage("OASISDNA.json updated.");
            }
            else
            {
                Console.WriteLine(config);
            }
        }

        private static async Task ONODEServiceCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length < 3) { CLIEngine.ShowErrorMessage("Usage: onode service [install|uninstall|start|stop|restart]"); return; }
            switch (inputArgs[2].ToLower())
            {
                case "install":
                    CLIEngine.ShowMessage("To install ONODEService, run the service binary directly with --install:", ConsoleColor.Yellow, false);
                    CLIEngine.ShowMessage("  dotnet run --project <path-to-ONODEService> -- --install", ConsoleColor.White, false);
                    CLIEngine.ShowMessage("Or publish it and run: NextGenSoftware.OASIS.ONODE.Service install", ConsoleColor.White, false);
                    break;
                case "uninstall":
                    CLIEngine.ShowMessage("To uninstall ONODEService, run:", ConsoleColor.Yellow, false);
                    CLIEngine.ShowMessage("  NextGenSoftware.OASIS.ONODE.Service uninstall", ConsoleColor.White, false);
                    break;
                default:
                    CLIEngine.ShowErrorMessage($"Unknown service subcommand: {inputArgs[2]}");
                    break;
            }
            await Task.CompletedTask;
        }

        // Fallback: direct spawn when ONODEService is not running
        private static async Task ONODEStartDirectFallbackAsync(string target, string? windowMode)
        {
            var services = target switch
            {
                "all"      => new[] { "web4","web5","web6","web7","web8","web9","web10" },
                "core"     => new[] { "web4","web5" },
                "ai"       => new[] { "web6" },
                "extended" => new[] { "web7","web8","web9","web10" },
                _ when target.Contains(",") => target.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                _ => new[] { target }
            };

            string oasisRoot = @"C:\Source\OASIS2";
            if (!OperatingSystem.IsWindows())
                oasisRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source", "OASIS2");

            foreach (var svc in services)
            {
                var projectPath = svc switch
                {
                    "web4"  => Path.Combine(oasisRoot, "ONODE", "NextGenSoftware.OASIS.API.ONODE.WebAPI"),
                    "web5"  => Path.Combine(oasisRoot, "STAR ODK", "NextGenSoftware.OASIS.STAR.WebAPI"),
                    "web6"  => Path.Combine(oasisRoot, "WEB6",  "NextGenSoftware.OASIS.Web6.WebAPI"),
                    "web7"  => Path.Combine(oasisRoot, "WEB7",  "NextGenSoftware.OASIS.Web7.WebAPI"),
                    "web8"  => Path.Combine(oasisRoot, "WEB8",  "NextGenSoftware.OASIS.Web8.WebAPI"),
                    "web9"  => Path.Combine(oasisRoot, "WEB9",  "NextGenSoftware.OASIS.Web9.WebAPI"),
                    "web10" => Path.Combine(oasisRoot, "WEB10", "NextGenSoftware.OASIS.Web10.WebAPI"),
                    _ => ""
                };
                if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                {
                    CLIEngine.ShowWarningMessage($"{svc.ToUpper()} not found at {projectPath} — skipping.");
                    continue;
                }
                var hidden = windowMode?.Equals("Hidden", StringComparison.OrdinalIgnoreCase) == true;
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{projectPath}\"",
                    WorkingDirectory = projectPath,
                    UseShellExecute = !hidden,
                    CreateNoWindow = hidden,
                    WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
                };
                var process = Process.Start(psi);
                if (process != null)
                {
                    _webApiProcesses[svc] = process;
                    CLIEngine.ShowSuccessMessage($"{svc.ToUpper()} started (pid {process.Id}).");
                }
                else CLIEngine.ShowErrorMessage($"Failed to start {svc.ToUpper()}.");
            }
            await Task.CompletedTask;
        }

        private static void ShowONODEHelp()
        {
            Console.WriteLine("");
            CLIEngine.ShowMessage("ONODE SUBCOMMANDS:", ConsoleColor.Green);
            Console.WriteLine("");
            CLIEngine.ShowMessage("  start   [target] [--hidden|--visible|--minimised]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  stop    [target]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  restart [target] [--hidden|--visible|--minimised]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  status", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  logs    [target] [--lines=N] [--follow]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  metrics", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  config  [--edit]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  providers", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  startprovider {name}", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  stopprovider  {name}", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  service [install|uninstall]", ConsoleColor.Green, false);
            Console.WriteLine("");
            CLIEngine.ShowMessage("  [target] = web4|web5|web6|web7|web8|web9|web10|all|core|ai|extended|web4,web6,...", ConsoleColor.DarkGreen, false);
            Console.WriteLine("");
        }

        private static string FormatUptime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h{ts.Minutes:D2}m";
            if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m{ts.Seconds:D2}s";
            return $"{ts.Seconds}s";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1_000_000) return $"{bytes / 1_000_000.0:F1}MB";
            if (bytes >= 1_000) return $"{bytes / 1_000.0:F1}KB";
            return $"{bytes}B";
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
                CLIEngine.ShowWorkingMessage("Opening ONODE WEB4 OASIS DNA configuration...");
                
                var configPath = Path.Combine(Environment.CurrentDirectory, "DNA", "OASIS_DNA.json");
                if (File.Exists(configPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = configPath,
                        UseShellExecute = true
                    });
                    CLIEngine.ShowSuccessMessage("ONODE WEB4 OASIS DNA configuration opened in default editor");
                }
                else
                {
                    CLIEngine.ShowErrorMessage("OASISDNA.json configuration file not found");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error opening ONODE WEB4 OASIS DNA configuration: {ex.Message}");
            }
        }

        private static async Task OpenONODEWeb5ConfigAsync()
        {
            try
            {
                CLIEngine.ShowWorkingMessage("Opening ONODE WEB5 STAR DNA configuration...");

                var configPath = Path.Combine(Environment.CurrentDirectory, "DNA", "STAR_DNA.json");
                if (File.Exists(configPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = configPath,
                        UseShellExecute = true
                    });
                    CLIEngine.ShowSuccessMessage("ONODE WEB5 STAR DNA configuration opened in default editor");
                }
                else
                {
                    CLIEngine.ShowErrorMessage("STARDNA.json configuration file not found");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error opening ONODE WEB5 STAR DNA configuration: {ex.Message}");
            }
        }

        private static async Task ONODEProvidersAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string[] inputArgs)
        {
            // onode providers [list|enable|disable|priority] [providerType] [--service web4] [--priority N]
            string sub = inputArgs.Length > 2 ? inputArgs[2].ToLower() : "list";

            if (!client.IsAvailable)
            {
                CLIEngine.ShowWarningMessage("ONODEService not running. Provider management requires the supervisor.");
                return;
            }

            switch (sub)
            {
                case "list":
                {
                    CLIEngine.ShowWorkingMessage("Loading providers…");
                    var providers = await client.GetProvidersAsync();
                    if (providers == null || providers.Count == 0)
                    {
                        CLIEngine.ShowMessage("No providers configured in OASISDNA.json.", ConsoleColor.Yellow);
                        return;
                    }
                    Console.WriteLine();
                    CLIEngine.ShowMessage("OASIS Storage Providers:", ConsoleColor.Cyan);
                    CLIEngine.ShowMessage(new string('─', 50), ConsoleColor.DarkGray);
                    foreach (var p in providers.OrderBy(x => x.Priority))
                    {
                        var dot     = p.IsEnabled ? "●" : "○";
                        var colour  = p.IsEnabled ? ConsoleColor.Green : ConsoleColor.Gray;
                        var label   = p.IsEnabled ? "Enabled" : "Disabled";
                        CLIEngine.ShowMessage($"  {p.Priority,2}. {p.ProviderType,-24} {dot} {label}", colour, false);
                    }
                    break;
                }

                case "enable":
                {
                    var providerType = inputArgs.Length > 3 ? inputArgs[3] : null;
                    if (string.IsNullOrEmpty(providerType))
                    { CLIEngine.ShowErrorMessage("Usage: onode providers enable <ProviderType>"); return; }
                    CLIEngine.ShowWorkingMessage($"Enabling {providerType}…");
                    var result = await client.EnableProviderAsync(providerType);
                    if (result != null)
                    {
                        CLIEngine.ShowSuccessMessage(result.Message);
                        if (result.ReloadRequired)
                            CLIEngine.ShowMessage("⚠ Restart the affected service to apply: onode restart web4", ConsoleColor.Yellow);
                    }
                    else CLIEngine.ShowErrorMessage($"Failed to enable {providerType}. Check it exists in OASISDNA.json.");
                    break;
                }

                case "disable":
                {
                    var providerType = inputArgs.Length > 3 ? inputArgs[3] : null;
                    if (string.IsNullOrEmpty(providerType))
                    { CLIEngine.ShowErrorMessage("Usage: onode providers disable <ProviderType>"); return; }
                    CLIEngine.ShowWorkingMessage($"Disabling {providerType}…");
                    var result = await client.DisableProviderAsync(providerType);
                    if (result != null)
                    {
                        CLIEngine.ShowSuccessMessage(result.Message);
                        if (result.ReloadRequired)
                            CLIEngine.ShowMessage("⚠ Restart the affected service to apply: onode restart web4", ConsoleColor.Yellow);
                    }
                    else CLIEngine.ShowErrorMessage($"Failed to disable {providerType}.");
                    break;
                }

                case "priority":
                {
                    var providerType = inputArgs.Length > 3 ? inputArgs[3] : null;
                    var priorityStr  = inputArgs.Length > 4 ? inputArgs[4] : null;
                    if (string.IsNullOrEmpty(providerType) || !int.TryParse(priorityStr, out int priority))
                    { CLIEngine.ShowErrorMessage("Usage: onode providers priority <ProviderType> <N>"); return; }
                    CLIEngine.ShowWorkingMessage($"Setting {providerType} priority to {priority}…");
                    var result = await client.SetProviderPriorityAsync(providerType, priority);
                    if (result != null) CLIEngine.ShowSuccessMessage(result.Message);
                    else CLIEngine.ShowErrorMessage($"Failed to set priority for {providerType}.");
                    break;
                }

                default:
                    CLIEngine.ShowErrorMessage($"Unknown providers subcommand '{sub}'. Use: list | enable | disable | priority");
                    break;
            }
        }

        private static async Task ShowONODEProvidersAsync()
        {
            // Legacy stub — now routed to ONODEProvidersAsync
            CLIEngine.ShowMessage("Use: onode providers list|enable|disable|priority", ConsoleColor.Yellow);
            await Task.CompletedTask;
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
                    CLIEngine.ShowMessage($"â€¢ {stat.Key}: {stat.Value}", ConsoleColor.White);
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
                        CLIEngine.ShowMessage($"â€¢ {node.Id} - {node.Address}", ConsoleColor.White);
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
                        CLIEngine.ShowMessage($"â€¢ {node.Id} - {node.Address} (Status: {node.Status})", ConsoleColor.Gray);
                    }
                }
                
                if (topology.Connections.Any())
                {
                    CLIEngine.ShowMessage("\nConnections:", ConsoleColor.Yellow);
                    foreach (var connection in topology.Connections)
                    {
                        CLIEngine.ShowMessage($"â€¢ {connection.FromNodeId} â†” {connection.ToNodeId} (Latency: {connection.Latency}ms)", ConsoleColor.Gray);
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONET topology: {ex.Message}");
            }
        }

        #endregion

        #region Game Commands

        private static async Task ShowGameSessionCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                if (inputArgs.Length < 3)
                {
                    CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId>");
                    return;
                }

                if (!Guid.TryParse(inputArgs[2], out Guid gameId))
                {
                    CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                    return;
                }

                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                OASISResult<GameSession> result;

                switch (command.ToLower())
                {
                    case "start":
                        CLIEngine.ShowWorkingMessage($"Starting game session for game {gameId}...");
                        result = await gameManager.StartGameAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result != null)
                        {
                            CLIEngine.ShowSuccessMessage($"Game session started successfully. Session ID: {result.Result.Id}");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to start game session: {result.Message}");
                        }
                        break;

                    case "end":
                        CLIEngine.ShowWorkingMessage($"Ending game session for game {gameId}...");
                        var endResult = await gameManager.EndGameAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!endResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("Game session ended successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to end game session: {endResult.Message}");
                        }
                        break;

                    case "load":
                        CLIEngine.ShowWorkingMessage($"Loading game {gameId}...");
                        var loadResult = await gameManager.LoadGameAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!loadResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("Game loaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to load game: {loadResult.Message}");
                        }
                        break;

                    case "unload":
                        CLIEngine.ShowWorkingMessage($"Unloading game {gameId}...");
                        var unloadResult = await gameManager.UnloadGameAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!unloadResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("Game unloaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to unload game: {unloadResult.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game session command: {ex.Message}");
            }
        }

        private static async Task ShowGameLevelCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                if (inputArgs.Length < 4)
                {
                    CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId> <level> [x] [y] [z]");
                    return;
                }

                if (!Guid.TryParse(inputArgs[2], out Guid gameId))
                {
                    CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                    return;
                }

                string level = inputArgs[3];
                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                OASISResult<bool> result;

                switch (command.ToLower())
                {
                    case "loadlevel":
                        CLIEngine.ShowWorkingMessage($"Loading level '{level}' for game {gameId}...");
                        result = await gameManager.LoadLevelAsync(gameId, level, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Level '{level}' loaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to load level: {result.Message}");
                        }
                        break;

                    case "unloadlevel":
                        CLIEngine.ShowWorkingMessage($"Unloading level '{level}' for game {gameId}...");
                        var unloadLevelResult = await gameManager.UnloadLevelAsync(gameId, level);
                        if (!unloadLevelResult.IsError && unloadLevelResult.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Level '{level}' unloaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to unload level: {unloadLevelResult.Message}");
                        }
                        break;

                    case "jumptolevel":
                        CLIEngine.ShowWorkingMessage($"Jumping to level '{level}' for game {gameId}...");
                        result = await gameManager.JumpToLevelAsync(gameId, level, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Jumped to level '{level}' successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to jump to level: {result.Message}");
                        }
                        break;

                    case "jumptopoint":
                        if (inputArgs.Length < 7)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game jumptopoint <gameId> <level> <x> <y> <z>");
                            return;
                        }

                        if (!float.TryParse(inputArgs[4], out float x) || !float.TryParse(inputArgs[5], out float y) || !float.TryParse(inputArgs[6], out float z))
                        {
                            CLIEngine.ShowErrorMessage("Invalid coordinates. Please provide valid float values for x, y, and z.");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Jumping to point ({x}, {y}, {z}) in level '{level}' for game {gameId}...");
                        result = await gameManager.JumpToPointInLevelAsync(gameId, level, x, y, z, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Jumped to point ({x}, {y}, {z}) successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to jump to point: {result.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game level command: {ex.Message}");
            }
        }

        private static async Task ShowGameAreaCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                OASISResult<Guid> result;

                switch (command.ToLower())
                {
                    case "loadarea":
                        if (inputArgs.Length < 7)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game loadarea <gameId> <x> <y> <z> <radius>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out Guid gameId) || 
                            !float.TryParse(inputArgs[3], out float x) || 
                            !float.TryParse(inputArgs[4], out float y) || 
                            !float.TryParse(inputArgs[5], out float z) || 
                            !float.TryParse(inputArgs[6], out float radius))
                        {
                            CLIEngine.ShowErrorMessage("Invalid parameters. Please provide valid GUID and float values.");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Loading area at ({x}, {y}, {z}) with radius {radius} for game {gameId}...");
                        result = await gameManager.LoadAreaAsync(gameId, x, y, z, radius, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result != Guid.Empty)
                        {
                            CLIEngine.ShowSuccessMessage($"Area loaded successfully. Area ID: {result.Result}");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to load area: {result.Message}");
                        }
                        break;

                    case "unloadarea":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game unloadarea <gameId> <areaId>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out gameId) || !Guid.TryParse(inputArgs[3], out Guid areaId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID or area ID. Please provide valid GUIDs.");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Unloading area {areaId} for game {gameId}...");
                        var unloadResult = await gameManager.UnloadAreaAsync(gameId, areaId);
                        if (!unloadResult.IsError && unloadResult.Result)
                        {
                            CLIEngine.ShowSuccessMessage("Area unloaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to unload area: {unloadResult.Message}");
                        }
                        break;

                    case "jumptoarea":
                        if (inputArgs.Length < 6)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game jumptoarea <gameId> <x> <y> <z>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out gameId) || 
                            !float.TryParse(inputArgs[3], out x) || 
                            !float.TryParse(inputArgs[4], out y) || 
                            !float.TryParse(inputArgs[5], out z))
                        {
                            CLIEngine.ShowErrorMessage("Invalid parameters. Please provide valid GUID and float values.");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Jumping to area at ({x}, {y}, {z}) for game {gameId}...");
                        var jumpResult = await gameManager.JumpToAreaAsync(gameId, x, y, z, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!jumpResult.IsError && jumpResult.Result != Guid.Empty)
                        {
                            CLIEngine.ShowSuccessMessage($"Jumped to area successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to jump to area: {jumpResult.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game area command: {ex.Message}");
            }
        }

        private static async Task ShowGameUICommandAsync(string[] inputArgs, string command)
        {
            try
            {
                if (inputArgs.Length < 3)
                {
                    CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId>");
                    return;
                }

                if (!Guid.TryParse(inputArgs[2], out Guid gameId))
                {
                    CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                    return;
                }

                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                OASISResult<bool> result = default;

                switch (command.ToLower())
                {
                    case "showtitlescreen":
                        CLIEngine.ShowWorkingMessage($"Showing title screen for game {gameId}...");
                        result = await gameManager.ShowTitleScreenAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        break;

                    case "showmainmenu":
                        CLIEngine.ShowWorkingMessage($"Showing main menu for game {gameId}...");
                        result = await gameManager.ShowMainMenuAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        break;

                    case "showoptions":
                        CLIEngine.ShowWorkingMessage($"Showing options menu for game {gameId}...");
                        result = await gameManager.ShowOptionsAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        break;

                    case "showcredits":
                        CLIEngine.ShowWorkingMessage($"Showing credits for game {gameId}...");
                        result = await gameManager.ShowCreditsAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        break;
                }

                if (result != null)
                {
                    if (!result.IsError && result.Result)
                    {
                        CLIEngine.ShowSuccessMessage($"UI command executed successfully.");
                    }
                    else
                    {
                        CLIEngine.ShowErrorMessage($"Failed to execute UI command: {result.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game UI command: {ex.Message}");
            }
        }

        private static async Task ShowGameAudioCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

                switch (command.ToLower())
                {
                    case "setmastervolume":
                    case "setvoicevolume":
                    case "setsoundvolume":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId> <volume> (0.0 - 1.0)");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out Guid gameId) || !float.TryParse(inputArgs[3], out float volume))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID or volume. Please provide a valid GUID and volume (0.0 - 1.0).");
                            return;
                        }

                        if (volume < 0.0f || volume > 1.0f)
                        {
                            CLIEngine.ShowErrorMessage("Volume must be between 0.0 and 1.0.");
                            return;
                        }

                        OASISResult<bool> result;
                        if (command.ToLower() == "setmastervolume")
                        {
                            CLIEngine.ShowWorkingMessage($"Setting master volume to {volume} for game {gameId}...");
                            result = await gameManager.SetMasterVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty, volume);
                        }
                        else if (command.ToLower() == "setvoicevolume")
                        {
                            CLIEngine.ShowWorkingMessage($"Setting voice volume to {volume} for game {gameId}...");
                            result = await gameManager.SetVoiceVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty, volume);
                        }
                        else
                        {
                            CLIEngine.ShowWorkingMessage($"Setting sound volume to {volume} for game {gameId}...");
                            result = await gameManager.SetSoundVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty, volume);
                        }

                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage("Volume set successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to set volume: {result.Message}");
                        }
                        break;

                    case "getmastervolume":
                    case "getvoicevolume":
                    case "getsoundvolume":
                        if (inputArgs.Length < 3)
                        {
                            CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out gameId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                            return;
                        }

                        OASISResult<double> volumeResult;
                        if (command.ToLower() == "getmastervolume")
                        {
                            volumeResult = await gameManager.GetMasterVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        }
                        else if (command.ToLower() == "getvoicevolume")
                        {
                            volumeResult = await gameManager.GetVoiceVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        }
                        else
                        {
                            volumeResult = await gameManager.GetSoundVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        }

                        if (!volumeResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"Current volume: {volumeResult.Result}");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to get volume: {volumeResult.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game audio command: {ex.Message}");
            }
        }

        private static async Task ShowGameVideoCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

                switch (command.ToLower())
                {
                    case "setvideosetting":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game setvideosetting <gameId> <Low|Medium|High|Custom>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out Guid gameId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                            return;
                        }

                        if (!Enum.TryParse<VideoSetting>(inputArgs[3], true, out VideoSetting videoSetting))
                        {
                            CLIEngine.ShowErrorMessage("Invalid video setting. Please use: Low, Medium, High, or Custom");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Setting video setting to {videoSetting} for game {gameId}...");
                        var result = await gameManager.SetVideoSettingAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty, videoSetting);
                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Video setting set to {videoSetting} successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to set video setting: {result.Message}");
                        }
                        break;

                    case "getvideosetting":
                        if (inputArgs.Length < 3)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game getvideosetting <gameId>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out gameId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                            return;
                        }

                        var getResult = await gameManager.GetVideoSettingAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!getResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"Current video setting: {getResult.Result}");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to get video setting: {getResult.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game video command: {ex.Message}");
            }
        }

        private static async Task ShowGameInputCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                if (command.ToLower() == "bindkeys")
                {
                    CLIEngine.ShowMessage("Key binding functionality coming soon...");
                    CLIEngine.ShowMessage("This will allow you to configure key bindings for games.");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game input command: {ex.Message}");
            }
        }

        private static async Task ShowGameInventoryCommandAsync(string[] inputArgs)
        {
            try
            {
                if (inputArgs.Length < 3)
                {
                    CLIEngine.ShowMessage("GAME INVENTORY SUBCOMMANDS:", ConsoleColor.Green);
                    CLIEngine.ShowMessage("    inventory list              List all items in shared inventory", ConsoleColor.Green, false);
                    CLIEngine.ShowMessage("    inventory add <itemName>    Add item to shared inventory", ConsoleColor.Green, false);
                    CLIEngine.ShowMessage("    inventory remove <itemId>   Remove item from shared inventory", ConsoleColor.Green, false);
                    CLIEngine.ShowMessage("    inventory has <itemId>      Check if avatar has item by ID", ConsoleColor.Green, false);
                    CLIEngine.ShowMessage("    inventory hasbyname <name>  Check if avatar has item by name", ConsoleColor.Green, false);
                    return;
                }

                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                var avatarId = STAR.BeamedInAvatar?.Id ?? Guid.Empty;

                switch (inputArgs[2].ToLower())
                {
                    case "list":
                        CLIEngine.ShowWorkingMessage("Loading shared inventory...");
                        var listResult = await gameManager.GetSharedAssetsAsync(avatarId);
                        if (!listResult.IsError && listResult.Result != null)
                        {
                            CLIEngine.ShowSuccessMessage($"Found {listResult.Result.Count} item(s) in shared inventory:");
                            foreach (var item in listResult.Result)
                            {
                                CLIEngine.ShowMessage($"  â€¢ {item.Name} (ID: {item.Id})", ConsoleColor.White, false);
                            }
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to load inventory: {listResult.Message}");
                        }
                        break;

                    case "add":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game inventory add <itemName>");
                            return;
                        }
                        CLIEngine.ShowMessage("Adding items to inventory via CLI coming soon. Use the API directly for now.");
                        break;

                    case "remove":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game inventory remove <itemId>");
                            return;
                        }
                        if (!Guid.TryParse(inputArgs[3], out Guid itemId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid item ID. Please provide a valid GUID.");
                            return;
                        }
                        CLIEngine.ShowWorkingMessage($"Removing item {itemId} from inventory...");
                        var removeResult = await gameManager.RemoveItemFromInventoryAsync(avatarId, itemId);
                        if (!removeResult.IsError && removeResult.Result)
                        {
                            CLIEngine.ShowSuccessMessage("Item removed from inventory successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to remove item: {removeResult.Message}");
                        }
                        break;

                    case "has":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game inventory has <itemId>");
                            return;
                        }
                        if (!Guid.TryParse(inputArgs[3], out itemId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid item ID. Please provide a valid GUID.");
                            return;
                        }
                        var hasResult = await gameManager.HasItemAsync(avatarId, itemId);
                        if (!hasResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage(hasResult.Result ? "Avatar has this item." : "Avatar does not have this item.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to check item: {hasResult.Message}");
                        }
                        break;

                    case "hasbyname":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game inventory hasbyname <itemName>");
                            return;
                        }
                        var hasByNameResult = await gameManager.HasItemByNameAsync(avatarId, inputArgs[3]);
                        if (!hasByNameResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage(hasByNameResult.Result ? $"Avatar has item '{inputArgs[3]}'." : $"Avatar does not have item '{inputArgs[3]}'.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to check item: {hasByNameResult.Message}");
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Unknown inventory command.");
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game inventory command: {ex.Message}");
            }
        }

        #endregion

        #endregion
    }
}
