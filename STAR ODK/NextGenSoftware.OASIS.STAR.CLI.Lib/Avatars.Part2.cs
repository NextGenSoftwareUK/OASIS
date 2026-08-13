using System.Globalization;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class Avatars
    {

        //public void EnableOrDisableAutoProviderList(Func<bool, List<ProviderType>, bool> funct, bool isEnabled, List<ProviderType> providerTypes, string workingMessage, string successMessage, string errorMessage)
        //{
        //    CLIEngine.ShowWorkingMessage(workingMessage);

        //    if (funct(isEnabled, providerTypes))
        //        CLIEngine.ShowSuccessMessage(successMessage);
        //    else
        //        CLIEngine.ShowErrorMessage(errorMessage);
        //}

        public void ShowAvatar(IAvatar avatar, IAvatarDetail avatarDetail, bool listMode = false)
        {
            if (avatar != null)
            {
                //CLIEngine.ShowSuccessMessage("Avatar Loaded Successfully");
                CLIEngine.ShowMessage($"Avatar ID:                   {avatar.Id}");
                CLIEngine.ShowMessage($"Avatar Name:                 {avatar.FullName}");
                CLIEngine.ShowMessage($"Avatar Username:             {avatar.Username}");
                CLIEngine.ShowMessage($"Avatar Type:                 {avatar.AvatarType.Name}");
                CLIEngine.ShowMessage($"Avatar Created Date:         {avatar.CreatedDate}");
                CLIEngine.ShowMessage($"Avatar Modified Date:         {avatar.ModifiedDate}");
                CLIEngine.ShowMessage($"Avatar Last Beamed In Date:  {avatar.LastBeamedIn}");
                CLIEngine.ShowMessage($"Avatar Last Beamed Out Date: {avatar.LastBeamedOut}");
                CLIEngine.ShowMessage(String.Concat("Avatar Is Active:            ", avatar.IsActive ? "True" : "False"));
                CLIEngine.ShowMessage(String.Concat("Avatar Is Beamed In:         ", avatar.IsBeamedIn ? "True" : "False"));
                CLIEngine.ShowMessage(String.Concat("Avatar Is Verified:          ", avatar.IsVerified ? "True" : "False"));
                //CLIEngine.ShowMessage($"Avatar Version: {avatar.Version}");

                if (!listMode && avatarDetail != null && CLIEngine.GetConfirmation($"Do you wish to view more detailed information?"))
                    ShowAvatarStats(avatar, avatarDetail);

                if (listMode && avatarDetail != null)
                    ShowAvatarStats(avatar, avatarDetail);
            }
            else
                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In!");
        }

        public async Task ShowAvatar(Guid id = new Guid())
        {
            if (id == Guid.Empty)
                id = CLIEngine.GetValidInputForGuid("What is the ID/GUID for the avatar you wish to view?");

            OASISResult<IAvatar> avatarResult = await STAR.OASISAPI.Avatars.LoadAvatarAsync(id);

            if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
            {
                OASISResult<IAvatarDetail> avatarDetailResult = await STAR.OASISAPI.Avatars.LoadAvatarDetailAsync(id);

                if (avatarDetailResult != null && !avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    ShowAvatar(avatarResult.Result, avatarDetailResult.Result);
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar Detail: {avatarDetailResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar: {avatarResult.Message}");
        }

        public async Task ShowAvatar(string idOrUsername)
        {
            Guid id = Guid.Empty;

            if (string.IsNullOrEmpty(idOrUsername))
                idOrUsername = CLIEngine.GetValidInput("What is the username or ID/GUID for the avatar you wish to view?");

            if (Guid.TryParse(idOrUsername, out id))
                await ShowAvatar(id);
            else
            {
                OASISResult<IAvatar> avatarResult = await STAR.OASISAPI.Avatars.LoadAvatarAsync(idOrUsername);

                if (avatarResult != null && !avatarResult.IsError && avatarResult.Result != null)
                {
                    OASISResult<IAvatarDetail> avatarDetailResult = await STAR.OASISAPI.Avatars.LoadAvatarDetailByUsernameAsync(idOrUsername);

                    if (avatarDetailResult != null && !avatarDetailResult.IsError && avatarDetailResult.Result != null)
                        ShowAvatar(avatarResult.Result, avatarDetailResult.Result);
                    else
                        CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar Detail: {avatarDetailResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Loading Avatar: {avatarResult.Message}");
            }
        }

        public async Task ShowAvatar()
        {
            await ShowAvatar("");
        }


        public async Task<OASISResult<bool>> ForgotPasswordAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string email = GetValidExistingEmail("Enter your email: ", providerType);
            ErrorHandling.HandleResponseWithDefaultErrorMessage(result, await STAR.OASISAPI.Avatars.ForgotPasswordAsync(email, providerType), "Error occured sending Forgot Password email. Reason: ", "Successfully Sent Forgot Password Email, Please Check Your Email.");

            if (result != null && result.Result != null && !result.IsError && CLIEngine.GetConfirmation("Would you like to enter the token you received in the email to reset your password now?"))
            {
                Console.WriteLine("");
                result = await ResetPasswordAsync(providerType);
            }

            return result;
        }

        public async Task<OASISResult<bool>> ResetPasswordAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string token = CLIEngine.GetValidInput("What is the token you received in the Forgotten Password email you received?");
            string oldPassword = CLIEngine.ReadPassword("Enter your old password: ");
            string newPassword = CLIEngine.GetValidPassword("Enter your new password: ");
            ErrorHandling.HandleResponseWithDefaultErrorMessage(result, await STAR.OASISAPI.Avatars.ResetPasswordAsync(token, oldPassword, newPassword, providerType), "ResetPasswordAsync", "Successfully Reset Password");

            if (result != null && result.Result != null && !result.IsError)
            {
                OASISResult<IAvatar> avatarResult = await STAR.OASISAPI.Avatars.LoadAvatarAsync(STAR.BeamedInAvatar.Id);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                    STAR.BeamedInAvatar = avatarResult.Result;
            }

            return result;
        }
        public async Task SearchAvatarsAsync(string searchTerm = "", ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                Console.WriteLine("");
                searchTerm = CLIEngine.GetValidInput("What do you want to search for (enter username, name, email etc)?");
            }
            else
                Console.WriteLine("");

            CLIEngine.ShowWorkingMessage("Searching Avatars...");
            ListAvatars(await STAR.OASISAPI.Avatars.SearchAvatarsAsync(searchTerm, providerType));
        }

        public void SearchAvatars(string searchTerm = "", ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                Console.WriteLine("");
                searchTerm = CLIEngine.GetValidInput("What is the name of the Avatar you wish to search for?");
            }

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Searching Avatars...");
            ListAvatars(STAR.OASISAPI.Avatars.SearchAvatars(searchTerm, providerType));
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> ListAvatarsAsync(ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Listing Avatars...");
            return await ListAvatars(await STAR.OASISAPI.Avatars.LoadAllAvatarsAsync(providerType: providerType));
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> ListAvatarDetailsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            string errorMessage = "Error occured in ListAvatarDetailsAsync. Reason:";

            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Listing Avatar Details...");
            OASISResult<IEnumerable<IAvatar>> avatarResults = await STAR.OASISAPI.Avatars.LoadAllAvatarsAsync(providerType: providerType);

            if (avatarResults != null && avatarResults.Result != null && !avatarResults.IsError)
            {
                OASISResult<IEnumerable<IAvatarDetail>> avatarDetailResults = await STAR.OASISAPI.Avatars.LoadAllAvatarDetailsAsync(providerType: providerType);

                if (avatarDetailResults != null && avatarDetailResults.Result != null && !avatarDetailResults.IsError)
                {
                    result = avatarDetailResults;
                    await ListAvatars(avatarResults, avatarDetailResults);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAllAvatarDetailsAsync. Reason: {avatarDetailResults.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadAllAvatarsAsync. Reason: {avatarResults.Message}");

            return result;
        }

        private async Task<OASISResult<IEnumerable<IAvatar>>> ListAvatars(OASISResult<IEnumerable<IAvatar>> avatarsResult, OASISResult<IEnumerable<IAvatarDetail>> avatarDetailsResult = null)
        {
            if (avatarsResult != null)
            {
                if (!avatarsResult.IsError)
                {
                    if (avatarsResult.Result != null && avatarsResult.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (avatarsResult.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{avatarsResult.Result.Count()} Avatar Found:");
                        else
                            CLIEngine.ShowMessage($"{avatarsResult.Result.Count()} Avatars Found:");

                        CLIEngine.ShowDivider();

                        Dictionary<Guid, IAvatarDetail> avatarDetails = new Dictionary<Guid, IAvatarDetail>();
                        if (avatarDetailsResult != null && avatarDetailsResult.Result != null && !avatarDetailsResult.IsError)
                        {
                            foreach (IAvatarDetail avatarDetail in avatarDetailsResult.Result)
                            {
                                if (!avatarDetails.ContainsKey(avatarDetail.Id))
                                    avatarDetails.Add(avatarDetail.Id, avatarDetail);
                            }
                        }

                        foreach (IAvatar avatar in avatarsResult.Result)
                        {
                            ShowAvatar(avatar, avatarDetails.TryGetValue(avatar.Id, out IAvatarDetail? value) ? value : null, true);
                            CLIEngine.ShowDivider();
                        }
                    }
                    else
                        CLIEngine.ShowWarningMessage("No Avatar's Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading Avatar's. Reason: {avatarsResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading Avatar's.");

            return avatarsResult;
        }

        /// <summary>
        /// Lists inventory items for the beamed-in avatar via <c>STAR.OASISAPI.Avatars.GetAvatarInventoryAsync</c> (same data as WEB4 GET /api/avatar/inventory).
        /// </summary>
        public async Task ShowAvatarInventoryAsync(bool showDetailedInfo = false, ProviderType providerType = ProviderType.Default)
        {
            if (STAR.BeamedInAvatar == null)
            {
                if (CLIEngine.JsonOutput)
                {
                    Environment.ExitCode = 2;
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { success = false, exitCode = 2, error = "No Avatar Is Beamed In!", detail = (string?)null }, AvatarInventoryJsonOptions));
                    return;
                }

                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In!");
                return;
            }

            if (!CLIEngine.JsonOutput)
                CLIEngine.ShowWorkingMessage("Loading avatar inventory...");

            CLIEngine.SupressConsoleLogging = true;
            OASISResult<IEnumerable<IInventoryItem>> inventoryResult =
                await STAR.OASISAPI.Avatars.GetAvatarInventoryAsync(STAR.BeamedInAvatar.Id, providerType);
            CLIEngine.SupressConsoleLogging = false;

            if (inventoryResult == null || inventoryResult.IsError)
            {
                string msg = inventoryResult?.Message ?? "Failed to load avatar inventory.";
                if (CLIEngine.JsonOutput)
                {
                    Environment.ExitCode = 1;
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { success = false, exitCode = 1, error = msg, detail = (string?)null }, AvatarInventoryJsonOptions));
                    return;
                }

                CLIEngine.ShowErrorMessage($"Error loading avatar inventory: {msg}");
                return;
            }

            List<IInventoryItem> items = inventoryResult.Result?.ToList() ?? new List<IInventoryItem>();

            if (CLIEngine.JsonOutput)
            {
                object BuildInventoryItemJson(IInventoryItem i)
                {
                    if (!showDetailedInfo)
                    {
                        return new
                        {
                            id = i.Id,
                            name = i.Name,
                            description = i.Description,
                            quantity = i.Quantity,
                            gameSource = i.GameSource,
                            itemType = i.ItemType,
                            nftId = i.NftId
                        };
                    }

                    var dna = i.STARNETDNA;
                    return new
                    {
                        id = i.Id,
                        name = i.Name,
                        description = i.Description,
                        quantity = i.Quantity,
                        gameSource = i.GameSource,
                        itemType = i.ItemType,
                        nftId = i.NftId,
                        starnetDNA = dna == null ? null : new
                        {
                            id = dna.Id,
                            name = dna.Name,
                            description = dna.Description,
                            starnetHolonType = dna.STARNETHolonType,
                            starnetCategory = FormatStarnetDnaJsonValue(dna.STARNETCategory),
                            starnetSubCategory = FormatStarnetDnaJsonValue(dna.STARNETSubCategory),
                            version = dna.Version,
                            versionSequence = dna.VersionSequence,
                            createdOn = dna.CreatedOn,
                            createdByAvatarUsername = dna.CreatedByAvatarUsername,
                            publishedOn = dna.PublishedOn,
                            publishedProviderType = dna.PublishedProviderType,
                            launchTarget = dna.LaunchTarget,
                        }
                    };
                }

                var jsonGroupsOrdered = new[] { "Keys", "Weapons", "Ammo", "Armor", "Items", "Monsters", "Other" };
                var jsonGrouped = items
                    .GroupBy(i => GetInventoryTabLabel(i.ItemType.ToString()))
                    .ToDictionary(g => g.Key, g => g.ToList());

                var groupsPayload = new List<object>();
                foreach (string tab in jsonGroupsOrdered)
                {
                    if (!jsonGrouped.TryGetValue(tab, out List<IInventoryItem>? groupItems) || groupItems == null || groupItems.Count == 0)
                        continue;

                    groupsPayload.Add(new
                    {
                        type = tab,
                        count = groupItems.Count,
                        items = groupItems.Select(BuildInventoryItemJson).ToList()
                    });
                }

                foreach (var extraTab in jsonGrouped.Keys.Except(jsonGroupsOrdered).OrderBy(x => x))
                {
                    List<IInventoryItem>? groupItems = jsonGrouped[extraTab];
                    if (groupItems == null || groupItems.Count == 0) continue;

                    groupsPayload.Add(new
                    {
                        type = extraTab,
                        count = groupItems.Count,
                        items = groupItems.Select(BuildInventoryItemJson).ToList()
                    });
                }

                var payload = items.Select(BuildInventoryItemJson).ToList();

                Console.Out.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    message = inventoryResult.Message,
                    data = new { count = items.Count, items = payload, groups = groupsPayload }
                }, AvatarInventoryJsonOptions));
                return;
            }

            Console.WriteLine("");
            CLIEngine.ShowMessage(
                $"{items.Count} inventory item(s) for {STAR.BeamedInAvatar.Username}:",
                ConsoleColor.Green);

            if (items.Count == 0)
            {
                CLIEngine.ShowWarningMessage("Inventory is empty.");
                return;
            }

            var groupsOrdered = new[] { "Keys", "Weapons", "Ammo", "Armor", "Items", "Monsters", "Other" };
            var grouped = items
                .GroupBy(i => GetInventoryTabLabel(i.ItemType.ToString()))
                .ToDictionary(g => g.Key, g => g.ToList());

            CLIEngine.ShowDivider();
            foreach (string tab in groupsOrdered)
            {
                if (!grouped.TryGetValue(tab, out List<IInventoryItem>? groupItems) || groupItems == null || groupItems.Count == 0)
                    continue;

                CLIEngine.ShowMessage($"  {tab}:", ConsoleColor.Yellow, false);
                Console.WriteLine("");

                foreach (IInventoryItem item in groupItems.OrderBy(i => i.Name).ThenBy(i => i.Id))
                {
                    int qty = item.Quantity > 0 ? item.Quantity : 1;
                    string nft = item.NftId == Guid.Empty ? "" : $"  [NFT: {item.NftId}]";
                    CLIEngine.ShowMessage(
                        $"    {qty} x {item.Name ?? "(unnamed)"}  (Id: {item.Id}){nft}",
                        ConsoleColor.Green,
                        false);

                    if (!string.IsNullOrWhiteSpace(item.Description))
                        CLIEngine.ShowMessage($"      {item.Description}", ConsoleColor.Green, false);

                    if (!string.IsNullOrWhiteSpace(item.GameSource) || item.ItemType != default)
                        CLIEngine.ShowMessage(
                            $"      source: {item.GameSource ?? "-"}  type: {item.ItemType}",
                            ConsoleColor.Green,
                            false);

                    if (showDetailedInfo)
                    {
                        await STARCLI.InventoryItems.ShowAsync(
                            item,
                            showHeader: false,
                            showFooter: false,
                            showNumbers: false,
                            number: 0,
                            showDetailedInfo: true);
                        CLIEngine.ShowDivider();
                    }

                    // Spacing between entries (matches the overall UX of other detailed lists).
                    Console.WriteLine("");
                }
            }

            foreach (var extraTab in grouped.Keys.Except(groupsOrdered).OrderBy(x => x))
            {
                List<IInventoryItem>? groupItems = grouped[extraTab];
                if (groupItems == null || groupItems.Count == 0) continue;

                CLIEngine.ShowMessage($"  {extraTab}:", ConsoleColor.Yellow, false);
                Console.WriteLine("");

                foreach (IInventoryItem item in groupItems.OrderBy(i => i.Name).ThenBy(i => i.Id))
                {
                    int qty = item.Quantity > 0 ? item.Quantity : 1;
                    string nft = item.NftId == Guid.Empty ? "" : $"  [NFT: {item.NftId}]";
                    CLIEngine.ShowMessage(
                        $"    {qty} x {item.Name ?? "(unnamed)"}  (Id: {item.Id}){nft}",
                        ConsoleColor.Green,
                        false);

                    if (!string.IsNullOrWhiteSpace(item.Description))
                        CLIEngine.ShowMessage($"      {item.Description}", ConsoleColor.Green, false);

                    if (!string.IsNullOrWhiteSpace(item.GameSource) || item.ItemType != default)
                        CLIEngine.ShowMessage(
                            $"      source: {item.GameSource ?? "-"}  type: {item.ItemType}",
                            ConsoleColor.Green,
                            false);

                    if (showDetailedInfo)
                    {
                        await STARCLI.InventoryItems.ShowAsync(
                            item,
                            showHeader: false,
                            showFooter: false,
                            showNumbers: false,
                            number: 0,
                            showDetailedInfo: true);
                        CLIEngine.ShowDivider();
                    }

                    // Spacing between entries (matches the overall UX of other detailed lists).
                    Console.WriteLine("");
                }
            }

            CLIEngine.ShowDivider();
        }
    }
}
