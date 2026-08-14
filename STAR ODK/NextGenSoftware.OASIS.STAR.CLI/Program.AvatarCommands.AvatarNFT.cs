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
    }
}
