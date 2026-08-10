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

    }
}
