using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProviderAsync method in EOSIOOASIS Provider. Reason:";

            try
            {
                _eosClient = new EosClient(new Uri(HostURI));
                // Get server state. Just need to receive correct response, otherwise exception would be thrown.
                var nodeInfo = await _eosClient.GetNodeInfo();

                // Response was received, but payload was incorrect.
                if (nodeInfo == null || !nodeInfo.IsNodeInfoCorrect())
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} NodeInfo Received Incorrect.");
                else if (!string.IsNullOrWhiteSpace(EOSChainId) && !string.Equals(nodeInfo.ChainId, EOSChainId, StringComparison.OrdinalIgnoreCase))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The endpoint chain ID does not match the configured EOSIO chain ID.");
                else
                {
                    _holonRepository = new HolonEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                    _avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                    _avatarRepository = new AvatarEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                    _transferRepository = new EosTransferRepository(EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (!result.IsError)
            {
                IsProviderActivated = true;
                result.Result = true;
            }

            //if (result.IsError)
            //    return result;

            //return await base.ActivateProviderAsync();
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProvider method in EOSIOOASIS Provider. Reason:";

            try
            {
                _eosClient = new EosClient(new Uri(HostURI));
                // Get server state. Just need to receive correct response, otherwise exception would be thrown.
                var nodeInfo = _eosClient.GetNodeInfo().Result;

                // Response was received, but payload was incorrect.
                if (nodeInfo == null || !nodeInfo.IsNodeInfoCorrect())
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} NodeInfo Received Incorrect.");
                else if (!string.IsNullOrWhiteSpace(EOSChainId) && !string.Equals(nodeInfo.ChainId, EOSChainId, StringComparison.OrdinalIgnoreCase))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The endpoint chain ID does not match the configured EOSIO chain ID.");
                else
                {
                    _holonRepository = new HolonEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                    _avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                    _avatarRepository = new AvatarEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                    _transferRepository = new EosTransferRepository(EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (!result.IsError)
            {
                IsProviderActivated = true;
                result.Result = true;
            }

            return result;

            //if (result.IsError)
            //    return result;

            //return base.ActivateProvider();
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _eosClient?.Dispose();
            _eosClient = null;
            _holonRepository = null;
            _avatarDetailRepository = null;
            _avatarRepository = null;
            _transferRepository = null;

            _avatarManager = null;
            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            //return await base.DeActivateProviderAsync();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _eosClient?.Dispose();
            _eosClient = null;
            _holonRepository = null;
            _avatarDetailRepository = null;
            _avatarRepository = null;
            _transferRepository = null;

            _avatarManager = null;
            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            //return base.DeActivateProvider();
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var allAvatarsDTOs = await _avatarRepository.ReadAll();
                if (allAvatarsDTOs.IsEmpty)
                    return result;

                result.Result =
                    allAvatarsDTOs
                        .Select(avatarDto => avatarDto.GetBaseAvatar())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var allAvatarsDTOs = _avatarRepository.ReadAll().Result;
                if (allAvatarsDTOs.IsEmpty)
                    return result;

                result.Result =
                    allAvatarsDTOs
                        .Select(avatarDto => avatarDto.GetBaseAvatar())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            if (Id == null)
                throw new ArgumentNullException(nameof(Id));

            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarDto = await _avatarRepository.Read(Id);
                var avatarEntity = avatarDto?.GetBaseAvatar();
                if (avatarEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar with such ID, not found!";
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }

                result.Result = avatarEntity;
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }

            return result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => FindStoredIdentityAsync<IAvatar>(async () => (await _avatarRepository.ReadAll().ConfigureAwait(false))
                .Select(row => row.GetBaseAvatar()), avatar => avatar.Email, avatarEmail);

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => FindStoredIdentityAsync<IAvatar>(async () => (await _avatarRepository.ReadAll().ConfigureAwait(false))
                .Select(row => row.GetBaseAvatar()), avatar => avatar.Username, avatarUsername);

        private static async Task<OASISResult<T>> FindStoredIdentityAsync<T>(
            Func<Task<IEnumerable<T>>> read, Func<T, string> selector, string value) where T : class
        {
            var result = new OASISResult<T>();
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                var rows = await read().ConfigureAwait(false);
                var matches = rows.Where(row => string.Equals(selector(row), value, StringComparison.OrdinalIgnoreCase))
                    .Take(2).ToArray();
                if (matches.Length == 0)
                {
                    result.IsError = true;
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    result.Message = "EOSIO storage identity was not found.";
                    return result;
                }

                if (matches.Length != 1)
                    throw new StorageIdentityAmbiguousException();

                result.Result = matches[0];
                result.IsLoaded = true;
                result.LoadedCount = 1;
            }
            catch (Exception error)
            {
                OASISErrorHandling.HandleError(ref result, "EOSIO storage identity lookup failed.", error);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
            => LoadAvatarAsync(Id, version).GetAwaiter().GetResult();

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey,
            int version = 0)
        {
            var result = new OASISResult<IAvatar>();
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

                // Load avatar by provider key from EOSIO blockchain
                var avatarData = await _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = providerKey
                });
                if (avatarData != null)
                {
                    // Convert EOSIO account data to OASIS Avatar
                    var avatar = new Avatar
                    {
                        Id = CreateDeterministicGuid($"{Core.Enums.ProviderType.EOSIOOASIS}:{avatarData.AccountName ?? "eosio_user"}"),
                        Username = avatarData.AccountName ?? "",
                        Email = "", // EOSIO doesn't store email directly
                        CreatedDate = DateTime.TryParse(avatarData.Created, out var createdDate) ? createdDate : DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                        MetaData = new Dictionary<string, object>
                        {
                            ["EOSIOAccountName"] = avatarData.AccountName,
                            ["EOSIOHeadBlockNum"] = avatarData.HeadBlockNum,
                            ["EOSIOHeadBlockTime"] = avatarData.HeadBlockTime,
                            ["EOSIOCoreLiquidBalance"] = avatarData.CoreLiquidBalance,
                            ["EOSIORamUsage"] = avatarData.RamUsage,
                            ["EOSIOPrivileged"] = avatarData.Privileged,
                            ["Provider"] = "EOSIOOASIS"
                        }
                    };
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by provider key from EOSIO";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key on EOSIO blockchain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

    }
}
