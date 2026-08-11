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
    }
}
