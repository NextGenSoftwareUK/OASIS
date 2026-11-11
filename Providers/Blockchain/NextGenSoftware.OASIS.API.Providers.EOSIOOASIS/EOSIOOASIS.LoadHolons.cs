using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using EOSNewYork.EOSCore;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public partial class EOSIOOASIS
    {
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by email first, then delete
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }
                    result.Result = deleteResult.Result;
                    result.IsDeleted = deleteResult.IsDeleted;
                    result.DeletedCount = deleteResult.DeletedCount;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully by email from EOSIO";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email in EOSIO");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername,
            bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by username first, then delete
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }
                    result.Result = deleteResult.Result;
                    result.IsDeleted = deleteResult.IsDeleted;
                    result.DeletedCount = deleteResult.DeletedCount;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully by username from EOSIO";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username in EOSIO");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by provider key first, then delete
                var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }
                    result.Result = deleteResult.Result;
                    result.IsDeleted = deleteResult.IsDeleted;
                    result.DeletedCount = deleteResult.DeletedCount;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully by provider key from EOSIO";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key in EOSIO");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("A nonempty holon ID is required.", nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonDto = await _holonRepository.Read(id).ConfigureAwait(false);
                var holonEntity = holonDto?.GetBaseHolon();
                if (holonEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    result.Message = "Holon with such ID, not found!";
                    return result;
                }

                result.Result = holonEntity;
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query EOSIO smart contract for holon by provider key
                var holonData = await _eosClient.GetHolonByProviderKeyAsync(providerKey);
                if (holonData != null)
                {
                    var holon = ParseEOSIOToHolon(holonData);
                    if (holon != null)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully by provider key from EOSIO";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse holon data from EOSIO");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found by provider key in EOSIO");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var rows = (await _holonRepository.ReadAll().ConfigureAwait(false))
                    .Where(row => !row.IsDeleted).Select(row => row.GetBaseHolon()).ToArray();
                var project = HolonGraphProjection.Create(rows, loadChildren, recursive, maxChildDepth,
                    curentChildDepth, loadChildrenFromProvider,
                    row => (IHolon)OasisJson.Deserialize(OasisJson.Serialize<object>(row), typeof(Holon)),
                    rootsAreChildren: true);
                var matches = rows.Where(row => row.ParentHolonId == id &&
                    (type == HolonType.All || row.HolonType == type)).Select(project).ToList();
                result.Result = matches;
                result.IsLoaded = true;
                result.LoadedCount = matches.Count;
            }
            catch (Exception error)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(error), error);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate EOSIO provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query EOSIO smart contract for holons for parent by provider key
                var holonsData = await _eosClient.GetHolonsForParentByProviderKeyAsync(providerKey);
                if (holonsData != null)
                {
                    var holons = new List<IHolon>();
                    foreach (var holonData in holonsData)
                    {
                        var holon = ParseEOSIOToHolon(holonData);
                        if (holon != null)
                        {
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from EOSIO";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allHolonDTOs = _holonRepository.ReadAll().Result;
                if (allHolonDTOs.IsEmpty)
                    return result;

                result.Result =
                    allHolonDTOs
                        .Select(holonDto => holonDto.GetBaseHolon())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allHolonDTOs = await _holonRepository.ReadAll();
                if (allHolonDTOs.IsEmpty)
                    return result;

                result.Result =
                    allHolonDTOs
                        .Select(holonDto => holonDto.GetBaseHolon())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, GetEOSExceptionDetails(ex));
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider)
                .GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            ArgumentNullException.ThrowIfNull(holon);
            var batch = await SaveHolonBatchAsync(new[] { holon }, saveChildren, recursive, maxChildDepth, 0)
                .ConfigureAwait(false);
            var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>, IHolon>(batch);
            if (batch.IsSaved)
                result.Result = holon;
            return result;
        }


    }
}
