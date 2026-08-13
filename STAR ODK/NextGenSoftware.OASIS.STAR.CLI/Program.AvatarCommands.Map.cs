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

    }
}
