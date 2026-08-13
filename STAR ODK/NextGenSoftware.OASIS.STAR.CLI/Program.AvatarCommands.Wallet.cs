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
    }
}
