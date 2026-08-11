using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Console = System.Console;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.STAR.Enums;
using NextGenSoftware.OASIS.STAR.CLI.Lib;
using NextGenSoftware.OASIS.STAR.EventArgs;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.API.Core.Events;
using OASISBootLoader = NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    partial class Program
    {
        private static void ShowHeader()
        {
            if (CLIEngine.Quiet)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"STAR ODK {OASISBootLoader.OASISBootLoader.STARODKVersion} (non-interactive)");
                Console.ResetColor();
                return;
            }

            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("*************************************************************************************************");
            Console.Write(" NextGen Software");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" STAR");
            Console.ForegroundColor = ConsoleColor.Green;

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

            Colorful.Console.WriteAscii(" STAR", System.Drawing.Color.Yellow);

            ShowCommands();

            Console.WriteLine("");
            Console.Write(" Welcome to");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" STAR");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" (The");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" ❤️ ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" Of The OASIS)");
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private static void ShowCommands(bool showFullCommands = false)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n USAGE:");
            Console.WriteLine("    star {SUBCOMMAND}");
            Console.WriteLine("    star [--non-interactive|-n] [--json] [--quiet|-q] [--yes|-y] [--username U] [--password P] {SUBCOMMAND} ...");
            Console.WriteLine("         (automation flags may appear anywhere; see Docs/Devs/STAR_CLI_NonInteractive.md)");
            Console.WriteLine("");
            Console.WriteLine(" FLAGS:");
            DisplaySummary("--non-interactive (-n)", "Script/CI: no stdin prompts; omit for interactive wizards.");
            DisplaySummary("--json", "Machine-readable JSON on stdout where supported; quieter startup.");
            DisplaySummary("ignite", "Ignite STAR & Boot The OASIS");
            DisplaySummary("extinguish", "Extinguish STAR & Shutdown The OASIS");
            DisplaySummary("help [full]", "Show this help page. If the [full] flag is omitted it will show only the top level sub-commands, if [full] is included it will show every option for each sub-command.");
            DisplaySummary("version", "Show the versions of STAR ODK, COSMIC ORM, OASIS Runtime & the OASIS Providers...");
            DisplaySummary("status", "Show the status of STAR ODK.");
            DisplaySummary("dna", "Show paths to DNATemplates, OASIS DNA and STAR DNA.");
            DisplaySummary("exit", "Exit the STAR CLI.");

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
                DisplayCommand("light", "{LightRequest.json}", "Non-interactive / scripted: full Light from StarCliLightRequest JSON. Same as: oapp light <file> / oapp create light <file>. Alias: light json <file>.");
                DisplayCommand("light transmute", "{hAppDNA} {geneisFolder}", "Creates a new Planet (OApp) at the given folder genesis locations, from the given hApp DNA.");
                DisplayCommand("bang", "", "Generate a whole metaverse or part of one such as Multierveres, Universes, Dimensions, Galaxy Clusters, Galaxies, Solar Systems, Stars, Planets, Moons etc.");
                DisplayCommand("wiz", "", "Start the STAR ODK Wizard which will walk you through the steps for creating a OAPP tailored to your specefic needs.");
                DisplayCommand("flare", "", "Build a OAPP for the given {id} or {name}.");
                DisplayCommand("shine", "", "Launch & activate a OAPP for the given {id} or {name}.");
                DisplayCommand("twinkle", "", "Activate a published OAPP for the given {id} or {name} within the STARNET store.");
                DisplayCommand("dim", "", "Deactivate a published OAPP for the given {id} or {name} within the STARNET store.");
                DisplayCommand("seed", "", "Deploy/Publish a OAPP for the given {id} or {name} to the STARNET Store.");
                DisplayCommand("unseed", "", "Undeploy/Unpublish a OAPP for the given {id} or {name} from the STARNET Store.");
                DisplayCommand("reseed", "", "Redeploy/Republish a OAPP for the given {id} or {name} to the STARNET Store.");
                DisplayCommand("dust", "", "Delete a OAPP for the given {id} or {name}.");
                DisplayCommand("radiate", "", "Highlight the OAPP for the given {id} or {name} in the STARNET Store. *Admin/Wizards Only*");
                DisplayCommand("emit", "{id/name}", "Show how much light the OAPP is emitting.");
                DisplayCommand("reflect", "{id/name}", "Show stats of the OAPP for the given {id} or {name}.");
                DisplayCommand("evolve", "{id/name}", "Upgrade/update a OAPP for the given {id} or {name}.");
                DisplayCommand("mutate", "{id/name}", "Import/Export hApp, dApp & others for the given {id} or {name}.");
                DisplayCommand("love", "{id/username}", "Send/Receive Love for the given {id} or {username}.");
                DisplayCommand("burst", "", "View network stats/management/settings.");
                DisplayCommand("super", "", "Reserved For Future Use...");
                DisplayCommand("net", "", "Launch the STARNET Library/Store.");
                DisplayCommand("gate", "", "Opens the STARGATE to the OASIS Portal!");
                DisplayCommand("api", "[oasis]", "Opens the WEB5 STAR API.");
                DisplayCommand("avatar beamin", "", "Beam in (log in).");
                DisplayCommand("avatar beamout", "", "Beam out (Log out).");
                DisplayCommand("avatar whoisbeamedin", "", "Display who is currently beamed in.");
                DisplayCommand("avatar show me", "", "Display the currently beamed in avatar details.");
                DisplayCommand("avatar show", "{id/username}", "Shows the details for the avatar for the given {id} or {username}.");
                DisplayCommand("avatar edit", "", "Edit the currently beamed in avatar.");
                DisplayCommand("avatar list", "[detailed]", "Lists all avatars.");
                DisplayCommand("avatar search", "", "Search avatars.");
                DisplayCommand("avatar inventory", "[detailed]", "List inventory items for the currently beamed-in avatar.");
                DisplayCommand("avatar forgotpassword", "", "Send a Forgot Password email.");
                DisplayCommand("avatar resetpassword", "", "Reset your password using the Reset Token.");
                DisplayCommand("karma list", "", "Display the karma thresholds.");
                DisplaySTARNETHolonCommands("oapp", createDesc: "Shortcut to the light sub-command.", publishDesc: "Shortcut to the seed sub-command.", unpublishDesc: "Shortcut to the un-seed sub-command.", republishDesc: "Shortcut to the re-seed sub-command.");
                DisplayCommand("oapp light", "{LightRequest.json}", "Non-interactive / scripted: full Light from StarCliLightRequest.");
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
                DisplayCommand("nft mint", "{id/name}", "Mints a WEB4 OASIS NFT.");
                DisplayCommand("nft burn", "{id/name}", "Burns a nft for the given {id} or {name}.");
                DisplayCommand("nft send", "{id/name}", "Send a NFT for the given {id} or {name} to another wallet cross-chain.");
                DisplayCommand("nft import", "{id/name} [web3]", "Imports a WEB4 OASIS NFT JSON file.");
                DisplayCommand("nft export", "{id/name}", "Exports a WEB4 OASIS NFT as a JSON file.");
                DisplayCommand("nft convert", "{id/name}", "Allows the minting of different WEB3 NFT Standards.");
                DisplaySTARNETHolonCommands("nft");
                DisplayCommand("nft collection add", "{colid/colname} {nftid/nftname}", "Adds a nft to the nft collection.");
                DisplayCommand("nft collection remove", "{colid/colname} {nftid/nftname}", "Remove a nft from the nft collection.");
                DisplaySTARNETHolonCommands("nft collection");
                DisplayCommand("geonft mint", "{id/name}", "Mints a OASIS GeoNFT.");
                DisplayCommand("geonft burn", "{id/name}", "Burns a GeoNFT for the given {id} or {name}.");
                DisplayCommand("geonft place", "{id/name}", "Places an existing OASIS NFT in Our World.");
                DisplayCommand("geonft send", "{id/name}", "Send a GeoNFT to another wallet cross-chain.");
                DisplayCommand("geonft import", "{id/name}", "Imports a WEB4 OASIS GeoNFT JSON file.");
                DisplayCommand("geonft export", "{id/name}", "Exports a WEB4 OASIS GeoNFT as a JSON file.");
                DisplaySTARNETHolonCommands("geonft");
                DisplaySTARNETHolonCommands("geohotspot");
                DisplaySTARNETHolonCommands("inventoryitem");
                DisplaySTARNETHolonCommands("plugin");
                DisplayCommand("seeds balance", "{telosAccountName/avatarId}", "Get the balance of your SEEDS account.");
                DisplayCommand("seeds organisations", "", "Get a list of all the SEEDS organisations.");
                DisplayCommand("seeds organisation", "{organisationName}", "Get a organisation for the given {organisationName}.");
                DisplayCommand("seeds pay", "{telosAccountName/avatarId}", "Pay using SEEDS.");
                DisplayCommand("seeds donate", "{telosAccountName/avatarId}", "Donate using SEEDS.");
                DisplayCommand("seeds reward", "{telosAccountName/avatarId}", "Reward using SEEDS.");
                DisplayCommand("seeds invite", "{telosAccountName/avatarId}", "Send invite to join SEEDS.");
                DisplayCommand("seeds accept", "{telosAccountName/avatarId}", "Accept the invite to join SEEDS.");
                DisplayCommand("seeds qrcode", "{telosAccountName/avatarId}", "Generate a sign-in QR code.");
                DisplayCommand("data save", "{key} {value}", "Saves data for the given {key} and {value}.");
                DisplayCommand("data load", "{key}", "Loads data for the given {key}.");
                DisplayCommand("data delete", "{key}", "Deletes data for the given {key}.");
                DisplayCommand("data list", "", "Lists all data for the currently beamed in avatar.");
            }
            else
            {
                Console.WriteLine("    ignite                  Ignite STAR & Boot The OASIS");
                Console.WriteLine("    extinguish              Extinguish STAR & Shutdown The OASIS");
                Console.WriteLine("    help [full]             Show this help page.");
                Console.WriteLine("    version                 Show the versions of STAR ODK, COSMIC ORM, OASIS Runtime & the OASIS Providers.");
                Console.WriteLine("    status                  Show the status of STAR ODK.");
                Console.WriteLine("    dna                     Show paths to DNATemplates, OASIS DNA and STAR DNA.");
                Console.WriteLine("    exit                    Exit the STAR CLI.");
                Console.WriteLine("    light                   Create a new OAPP.");
                Console.WriteLine("    bang                    Generate a whole metaverse.");
                Console.WriteLine("    wiz                     Start the STAR ODK Wizard.");
                Console.WriteLine("    net                     Launch the STARNET Library/Store.");
                Console.WriteLine("    gate                    Opens the STARGATE to the OASIS Portal!");
                Console.WriteLine("    api [oasis]             Opens the WEB5 STAR API.");
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
                Console.WriteLine("    seeds                   Access the SEEDS API.");
                Console.WriteLine("    data                    Access the Data API.");
                Console.WriteLine("    map                     Access the Map API.");
                Console.WriteLine("    oland                   Access the OLAND (Virtual Land) API.");
                Console.WriteLine("    onode                   Manage this ONODE.");
                Console.WriteLine("    hypernet                Start, stop & view status for the HoloNET P2P HyperNET Service.");
                Console.WriteLine("    onet                    View the status for the ONET (OASIS Network).");
                Console.WriteLine("    config                  Enables/disables COSMIC detailed output & STAR ODK detailed output.");
                Console.WriteLine("    runcosmictests          Run the STAR ODK/COSMIC tests.");
                Console.WriteLine("    runoasisapitests        Run the OASIS API tests.");

                Console.WriteLine("");
                Console.WriteLine(" NOTES:");
                Console.WriteLine("        When invoking any sub-commands that take a {id} or {name}, if neither is specified then a wizard will launch to help find the correct item.");
                Console.WriteLine("        In some cases, sub-commands may only list {id} as a param to save space but these also accept the {name}.");
            }

            Console.WriteLine("************************************************************************************************");
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private static string ResolveFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path ?? "";
            return Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path.Replace('\\', Path.DirectorySeparatorChar)));
        }

        private static void ShowDNAPaths()
        {
            string oasisPath = ResolveFullPath(STAR.OASISDNAPath);
            string starPath = ResolveFullPath(STAR.STARDNAPath);
            CLIEngine.ShowMessage("DNA paths in use:", ConsoleColor.Cyan, false);
            CLIEngine.ShowMessage($"  OASIS DNA:   {oasisPath}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  STAR DNA:    {starPath}", ConsoleColor.White, false);
            if (STAR.IsStarIgnited && STAR.STARDNA != null)
            {
                string dnatemplatesPath = string.IsNullOrEmpty(STAR.STARDNA.STARBasePath)
                    ? "(N/A)"
                    : Path.GetFullPath(Path.Combine(STAR.STARDNA.STARBasePath, "DNATemplates"));
                CLIEngine.ShowMessage($"  DNATemplates: {dnatemplatesPath}", ConsoleColor.White, false);
            }
            else
                CLIEngine.ShowMessage("  DNATemplates: (N/A — STAR not ignited)", ConsoleColor.Gray, false);
        }

        private static void DisplayCommand(string command, string args, string desc, int indent = 4, int commandColSize = 48, int argsColSize = 52)
        {
            Console.WriteLine(string.Concat("".PadRight(indent), command.PadRight(commandColSize), args.PadRight(argsColSize), desc));
        }

        private static void DisplaySTARNETHolonCommands(string holonType, string createParams = "", string createDesc = "", string updateParams = "", string updateDesc = "", string cloneParams = "", string cloneDesc = "", string addDependencyParams = "", string addDependencyDesc = "", string removeDependencyParams = "", string removeDependencyDesc = "", string deleteParams = "", string deleteDesc = "", string publishParams = "", string publishDesc = "", string unpublishParams = "", string unpublishDesc = "", string republishParams = "", string republishDesc = "", string activateParams = "", string activateDesc = "", string deactivateParams = "", string deactivateDesc = "", string downloadParams = "", string downloadDesc = "", string installParams = "", string installDesc = "", string uninstallParams = "", string uninstallDesc = "", string reinstallParams = "", string reinstallDesc = "", string showParams = "", string showDesc = "", string listParams = "", string listDesc = "", string listInstalledParams = "", string listInstalledDesc = "", string listUninstalledParams = "", string listUninstalledDesc = "", string listUnpublishedParams = "", string listUnpublishedDesc = "", string listDeactivatedParams = "", string listDeactivatedDesc = "", string searchParams = "", string searchDesc = "")
        {
            string web4Param = "";

            if (holonType == "nft collection" || holonType == "geonft collection" || holonType == "nft" || holonType == "geonft")
                web4Param = " [web3] [web4]";

            DisplayCommand(string.Concat(holonType, " create"), !string.IsNullOrEmpty(createParams) ? createParams : web4Param, !string.IsNullOrEmpty(createDesc) ? createDesc : $"Create a new {holonType}.");
            DisplayCommand(string.Concat(holonType, " update"), !string.IsNullOrEmpty(updateParams) ? updateParams : string.Concat("{id/name}", web4Param), !string.IsNullOrEmpty(updateDesc) ? updateDesc : string.Concat("Updates an existing ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " clone"), !string.IsNullOrEmpty(cloneParams) ? cloneParams : "{id/name}", !string.IsNullOrEmpty(cloneDesc) ? cloneDesc : string.Concat("Clones an existing ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " adddependency"), !string.IsNullOrEmpty(addDependencyDesc) ? addDependencyDesc : "{id/name}", !string.IsNullOrEmpty(addDependencyDesc) ? addDependencyDesc : string.Concat("Adds a dependency to an existing ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " removedependency"), !string.IsNullOrEmpty(removeDependencyParams) ? removeDependencyParams : "{id/name}", !string.IsNullOrEmpty(removeDependencyDesc) ? removeDependencyDesc : string.Concat("Removes a dependency from an existing ", holonType, " for the given {id} or {name}."));
            DisplayCommand(string.Concat(holonType, " delete"), !string.IsNullOrEmpty(deleteParams) ? deleteParams : string.Concat("{id/name}", web4Param), !string.IsNullOrEmpty(deleteDesc) ? deleteDesc : string.Concat("Deletes a ", holonType, " for the given {id} or {name}."));
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
            DisplayCommand(string.Concat(holonType, " list uninstalled"), !string.IsNullOrEmpty(listUninstalledParams) ? listUninstalledParams : "", !string.IsNullOrEmpty(listUninstalledDesc) ? listUninstalledDesc : string.Concat("List all ", holonType, "'s uninstalled for the currently beamed in avatar."));
            DisplayCommand(string.Concat(holonType, " list unpublished"), !string.IsNullOrEmpty(listUnpublishedParams) ? listUnpublishedParams : "", !string.IsNullOrEmpty(listUnpublishedDesc) ? listUnpublishedDesc : string.Concat("List all ", holonType, "'s unpublished for the currently beamed in avatar."));
            DisplayCommand(string.Concat(holonType, " list deactivated"), !string.IsNullOrEmpty(listDeactivatedParams) ? listDeactivatedParams : "", !string.IsNullOrEmpty(listDeactivatedDesc) ? listDeactivatedDesc : string.Concat("List all ", holonType, "'s deactivated for the currently beamed in avatar."));
            DisplayCommand(string.Concat(holonType, " search"), !string.IsNullOrEmpty(searchParams) ? searchParams : string.Concat("[allVersions] [forAllAvatars]", web4Param), !string.IsNullOrEmpty(searchDesc) ? searchDesc : string.Concat("Searches the ", holonType, "'s for the given search criteria."));
        }

        private static void DisplaySTARNETHolonCommandSummaries(string holonType)
        {
            DisplaySummary(holonType, $"Create, edit, clone, delete, publish, unpublish, install, uninstall, list & show {holonType}'s.");
        }

        private static void DisplaySummary(string command, string desc)
        {
            DisplayCommand(command, desc, "", commandColSize: 24);
        }

        private static void STAR_OnInitialized(object sender, System.EventArgs e)
        {
            CLIEngine.ShowSuccessMessage(" STAR Initialized.");
        }

        private static void STAR_OnOASISBootError(object sender, OASISBootErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage(e.ErrorReason);
        }

        private static void STAR_OnOASISBooted(object sender, OASISBootedEventArgs e)
        {
            // CLIEngine.ShowSuccessMessage(string.Concat("OASIS BOOTED.", e.Message));
        }

        private static void STAR_OnStarError(object sender, StarErrorEventArgs e)
        {
            CLIEngine.ShowErrorMessage(string.Concat("Error Igniting SuperStar. Reason: ", e.Reason));
        }

        private static void STAR_OnStarIgnited(object sender, System.EventArgs e)
        {
            Console.WriteLine("");
            ShowDNAPaths();
        }

        private static void STAR_OnStarStatusChanged(object sender, StarStatusChangedEventArgs e)
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
                    case Enums.StarStatus.OASISBooted:
                        break;

                    case Enums.StarStatus.Igniting:
                        CLIEngine.ShowWorkingMessage("IGNITING STAR...");
                        break;

                    case Enums.StarStatus.Ignited:
                        CLIEngine.ShowSuccessMessage("STAR IGNITED");
                        break;
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
        }

        private static void STAR_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"Zomes Loaded Successfully. {detailedMessage}");
        }

        private static void STAR_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            string detailedMessage = string.IsNullOrEmpty(e.Result.Message) ? e.Result.Message : "";
            CLIEngine.ShowSuccessMessage($"Zomes Saved Successfully. {detailedMessage}");
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
            CLIEngine.ShowSuccessMessage(string.Concat(" STAR Holons Loaded. Holons Loaded: ", e.Result.Result?.Count() ?? 0));
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
        }
    }
}
