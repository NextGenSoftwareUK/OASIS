using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    public partial class BitcoinOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            var savedHolons = new List<IHolon>();
            var failures = new List<string>();
            var receipts = new List<string>();
            response.Result = savedHolons;

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                ArgumentNullException.ThrowIfNull(holons);
                var position = 0;
                foreach (var holon in holons)
                {
                    position++;
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (saveResult.Result != null)
                        savedHolons.Add(saveResult.Result);
                    if (saveResult.MetaData.TryGetValue("BitcoinAcknowledgedTransactionIds", out var graphReceipts))
                    {
                        receipts.AddRange(graphReceipts.Split(','));
                        response.SavedCount += saveResult.SavedCount;
                    }
                    else if (!saveResult.IsError)
                    {
                        receipts.Add(saveResult.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS]);
                        response.SavedCount++;
                    }

                    if (!saveResult.IsError)
                        continue;

                    failures.Add($"Holon at position {position}: {saveResult.Message}");
                    if (saveResult.ErrorCode == BitcoinBroadcastOutcomeUnknown)
                    {
                        response.ErrorCode = saveResult.ErrorCode;
                        response.MetaData = saveResult.MetaData;
                        break;
                    }
                    if (!continueOnError)
                        break;
                }

                if (failures.Count > 0)
                {
                    OASISErrorHandling.HandleError(ref response,
                        $"Bitcoin acknowledged {response.SavedCount} holon publications. {failures.Count} attempts lack acknowledgement.");
                    response.ErrorCount = failures.Count;
                    response.InnerMessages.AddRange(failures);
                }
                else
                    response.Message = $"Bitcoin acknowledged {response.SavedCount} holon publications.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error saving holons to Bitcoin: {ex.Message}", ex);
            }

            if (receipts.Count > 0)
                response.MetaData["BitcoinAcknowledgedTransactionIds"] = string.Join(",", receipts);
            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
            => DeleteWalletHolonAsync(() => LoadHolonAsync(id, loadChildren: false));

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
            => DeleteWalletHolonAsync(() => ResolveWalletHolonForDeleteAsync(providerKey));

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchWalletRecordsAsync(searchParams, loadChildren, recursive, maxChildDepth, version);

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Import holons to Bitcoin blockchain
                var importResult = await SaveHolonsAsync(holons);
                response.SavedCount = importResult.SavedCount;
                response.ErrorCode = importResult.ErrorCode;
                response.MetaData = importResult.MetaData;
                response.InnerMessages = importResult.InnerMessages;
                if (importResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to import holons to Bitcoin: {importResult.Message}");
                    response.ErrorCount = importResult.ErrorCount;
                    return response;
                }

                response.Result = true;
                response.IsError = false;
                response.Message = $"Bitcoin acknowledged {response.SavedCount} imported holons.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error importing holons to Bitcoin: {ex.Message}", ex);
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => LoadAllHolonsAsync(loadChildren: false, version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
            => ExportWalletHolonsForOwnerAsync(avatarId, version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => ExportWalletHolonsForAvatarAsync(LoadAvatarByUsernameAsync(avatarUsername), version);

    }
}
