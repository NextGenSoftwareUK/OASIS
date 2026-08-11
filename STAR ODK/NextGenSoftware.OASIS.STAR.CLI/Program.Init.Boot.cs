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
    }
}
