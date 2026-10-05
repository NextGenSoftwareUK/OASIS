using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS
{
    public class ZcashOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISNETProvider
    {
        private IZcashRepository _zcashRepository;
        private IZcashService _zcashService;
        private IZcashBridgeService _zcashBridgeService;
        private readonly ZcashRPCClient _rpcClient;
        private readonly string _rpcUrl;
        private readonly string _rpcUser;
        private readonly string _rpcPassword;
        private readonly string _network; // "mainnet" or "testnet"

        public ZcashOASIS(string rpcUrl = null, string rpcUser = null, string rpcPassword = null, string network = "testnet")
        {
            this.ProviderName = nameof(ZcashOASIS);
            this.ProviderDescription = "Zcash Blockchain Provider with Shielded Transaction Support";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ZcashOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _rpcUrl = rpcUrl ?? Environment.GetEnvironmentVariable("ZCASH_RPC_URL") ?? "http://localhost:8232";
            _rpcUser = rpcUser ?? Environment.GetEnvironmentVariable("ZCASH_RPC_USER") ?? "user";
            _rpcPassword = rpcPassword ?? Environment.GetEnvironmentVariable("ZCASH_RPC_PASSWORD") ?? "password";
            _network = network;

            _rpcClient = new ZcashRPCClient(_rpcUrl, _rpcUser, _rpcPassword);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new();

            try
            {
                // Test connection to Zcash node
                var connectionTest = await _rpcClient.TestConnectionAsync();
                if (connectionTest.IsError)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"Failed to connect to Zcash node: {connectionTest.Message}");
                    return result;
                }

                _zcashRepository = new ZcashRepository(_rpcClient);
                _zcashService = new ZcashService(_rpcClient);
                _zcashBridgeService = new ZcashBridgeService(_rpcClient);

                result.Result = true;
                IsProviderActivated = true;
                result.Message = "Zcash provider activated successfully";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Unknown Error Occurred In ZcashOASIS Provider in ActivateProviderAsync. Reason: {e}");
            }

            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _zcashRepository = null;
            _zcashService = null;
            _zcashBridgeService = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        // Zcash-specific methods for shielded transactions
        public async Task<OASISResult<ShieldedTransaction>> CreateShieldedTransactionAsync(
            string fromAddress,
            string toAddress,
            decimal amount,
            string memo = null)
        {
            var result = new OASISResult<ShieldedTransaction>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var tx = await _zcashService.CreateShieldedTransactionAsync(fromAddress, toAddress, amount, memo);
                result.Result = tx;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<ViewingKey>> GenerateViewingKeyAsync(string address)
        {
            var result = new OASISResult<ViewingKey>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var viewingKey = await _zcashService.GenerateViewingKeyAsync(address);
                result.Result = viewingKey;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<PartialNote>> CreatePartialNoteAsync(decimal amount, int numberOfParts)
        {
            var result = new OASISResult<PartialNote>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var partialNote = await _zcashService.CreatePartialNoteAsync(amount, numberOfParts);
                result.Result = partialNote;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        // Bridge operations
        public async Task<OASISResult<string>> LockZECForBridgeAsync(
            decimal amount,
            string destinationChain,
            string destinationAddress,
            string viewingKey = null)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var txId = await _zcashBridgeService.LockZECForBridgeAsync(amount, destinationChain, destinationAddress, viewingKey);
                result.Result = txId;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> ReleaseZECAsync(string lockTxHash, decimal amount, string destinationAddress)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var releaseResult = await _zcashBridgeService.ReleaseZECAsync(lockTxHash, amount, destinationAddress);
                if (releaseResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, releaseResult.Message);
                    return result;
                }

                result.Result = releaseResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        // Required abstract method implementations (simplified for MVP)
        // These will be implemented with full functionality as needed

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // For Zcash, avatars would be stored as holons with shielded addresses
                // This is a simplified implementation
                result.Result = new List<IAvatar>();
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load avatar from Zcash (stored as holon)
                var holon = await LoadHolonAsync(Id);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Convert holon to avatar (simplified)
                result.Result = null; // Would convert holon to avatar
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return LoadAvatarAsync(Id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load avatar by Zcash address (provider key)
                var holon = await LoadHolonAsync(providerKey);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                result.Result = null; // Would convert holon to avatar
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Get avatar ID from index
                var avatarId = await _zcashRepository.GetAvatarIdByUsernameAsync(avatarUsername);
                if (!avatarId.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Load avatar by ID
                var avatarResult = await LoadAvatarAsync(avatarId.Value, version);
                result.Result = avatarResult.Result;
                result.IsError = avatarResult.IsError;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Get avatar ID from index
                var avatarId = await _zcashRepository.GetAvatarIdByEmailAsync(avatarEmail);
                if (!avatarId.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Load avatar by ID
                var avatarResult = await LoadAvatarAsync(avatarId.Value, version);
                result.Result = avatarResult.Result;
                result.IsError = avatarResult.IsError;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load holon by ID
                var holon = await LoadHolonAsync(id);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found");
                    return result;
                }

                // Convert holon to avatar detail (simplified - would need proper mapping)
                // For now, return null as IAvatarDetail conversion requires more context
                result.Result = null;
                result.IsError = false;
                result.Message = "Avatar detail loaded but conversion to IAvatarDetail not fully implemented";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Get avatar ID from index
                var avatarId = await _zcashRepository.GetAvatarIdByEmailAsync(avatarEmail);
                if (!avatarId.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found");
                    return result;
                }

                // Load avatar detail by ID
                var avatarDetailResult = await LoadAvatarDetailAsync(avatarId.Value, version);
                result.Result = avatarDetailResult.Result;
                result.IsError = avatarDetailResult.IsError;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Get avatar ID from index
                var avatarId = await _zcashRepository.GetAvatarIdByUsernameAsync(avatarUsername);
                if (!avatarId.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found");
                    return result;
                }

                // Load avatar detail by ID
                var avatarDetailResult = await LoadAvatarDetailAsync(avatarId.Value, version);
                result.Result = avatarDetailResult.Result;
                result.IsError = avatarDetailResult.IsError;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load all holons of type AvatarDetail
                var holonsResult = await LoadAllHolonsAsync(HolonType.AvatarDetail, false, false, 0, 0, true, false, version);
                
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                // Convert holons to avatar details (simplified - would need proper conversion)
                var avatarDetails = new List<IAvatarDetail>();
                // Note: Full conversion would require proper mapping from Holon to IAvatarDetail
                // For now, return empty list with message
                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = "Avatar details loaded but conversion to IAvatarDetail not fully implemented";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Convert avatar to holon for storage
                var holon = Avatar as IHolon ?? new Holon
                {
                    Id = Avatar.Id,
                    Name = Avatar.Username,
                    Description = Avatar.Email,
                    HolonType = HolonType.Avatar,
                    MetaData = new Dictionary<string, object>
                    {
                        { "avatar_username", Avatar.Username },
                        { "avatar_email", Avatar.Email },
                        { "avatar_type", "Avatar" }
                    }
                };

                var saveResult = await SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                // Save index holons for username and email lookup
                if (!string.IsNullOrEmpty(Avatar.Username))
                {
                    await _zcashRepository.SaveAvatarIndexAsync("username", Avatar.Username, Avatar.Id);
                }
                if (!string.IsNullOrEmpty(Avatar.Email))
                {
                    await _zcashRepository.SaveAvatarIndexAsync("email", Avatar.Email, Avatar.Id);
                }

                result.Result = Avatar;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Convert avatar detail to holon for storage
                var holon = Avatar as IHolon ?? new Holon
                {
                    Id = Avatar.Id,
                    Name = Avatar.Username,
                    Description = Avatar.Email,
                    HolonType = HolonType.AvatarDetail,
                    MetaData = new Dictionary<string, object>
                    {
                        { "avatar_username", Avatar.Username },
                        { "avatar_email", Avatar.Email },
                        { "avatar_type", "AvatarDetail" }
                    }
                };

                var saveResult = await SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                // Save index holons for username and email lookup
                if (!string.IsNullOrEmpty(Avatar.Username))
                {
                    await _zcashRepository.SaveAvatarIndexAsync("username", Avatar.Username, Avatar.Id);
                }
                if (!string.IsNullOrEmpty(Avatar.Email))
                {
                    await _zcashRepository.SaveAvatarIndexAsync("email", Avatar.Email, Avatar.Id);
                }

                result.Result = Avatar;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Delete avatar holon
                var deleteResult = await DeleteHolonAsync(id);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, deleteResult.Message);
                    return result;
                }

                result.Result = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load avatar by provider key first
                var avatar = await LoadAvatarByProviderKeyAsync(providerKey);
                if (avatar.IsError || avatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Delete using GUID
                var deleteResult = await DeleteAvatarAsync(avatar.Result.Id, softDelete);
                result.Result = deleteResult.Result;
                result.IsError = deleteResult.IsError;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Get avatar ID from index
                var avatarId = await _zcashRepository.GetAvatarIdByEmailAsync(avatarEmail);
                if (!avatarId.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Delete using GUID
                var deleteResult = await DeleteAvatarAsync(avatarId.Value, softDelete);
                result.Result = deleteResult.Result;
                result.IsError = deleteResult.IsError;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Get avatar ID from index
                var avatarId = await _zcashRepository.GetAvatarIdByUsernameAsync(avatarUsername);
                if (!avatarId.HasValue)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Delete using GUID
                var deleteResult = await DeleteAvatarAsync(avatarId.Value, softDelete);
                result.Result = deleteResult.Result;
                result.IsError = deleteResult.IsError;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();

                // Load all holons and search through them
                // TODO: Fix ISearchParams.HolonType - commented out due to build errors
                var allHolonsResult = await LoadAllHolonsAsync(
                    HolonType.All, // searchParams?.HolonType ?? HolonType.All,
                    loadChildren,
                    recursive,
                    maxChildDepth,
                    0,
                    continueOnError,
                    false,
                    version);

                if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                {
                    foreach (var holon in allHolonsResult.Result)
                    {
                        bool matches = false;

                        // TODO: Fix ISearchParams properties - commented out due to build errors
                        /*
                        // Search by name
                        if (!string.IsNullOrEmpty(searchParams?.SearchString) &&
                            !string.IsNullOrEmpty(holon.Name) &&
                            holon.Name.Contains(searchParams.SearchString, StringComparison.OrdinalIgnoreCase))
                        {
                            matches = true;
                        }

                        // Search by description
                        if (!matches && !string.IsNullOrEmpty(searchParams?.SearchString) &&
                            !string.IsNullOrEmpty(holon.Description) &&
                            holon.Description.Contains(searchParams.SearchString, StringComparison.OrdinalIgnoreCase))
                        {
                            matches = true;
                        }

                        // Search by metadata
                        if (!matches && searchParams?.MetaData != null && holon.MetaData != null)
                        {
                            foreach (var kvp in searchParams.MetaData)
                            {
                                if (holon.MetaData.ContainsKey(kvp.Key) &&
                                    holon.MetaData[kvp.Key]?.ToString() == kvp.Value)
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                        */
                        // Temporary: Skip search filtering until ISearchParams is fixed
                        matches = true;

                        if (matches)
                        {
                            matchingHolons.Add(holon);
                        }
                    }
                }

                // TODO: Fix SearchResults.Results property - commented out due to build errors
                // searchResults.Results = matchingHolons;
                // Temporary: Return empty results until SearchResults is fixed
                result.Message = "Search functionality temporarily disabled - needs ISearchParams and SearchResults fixes";
                result.Result = searchResults;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var holon = await _zcashRepository.LoadHolonAsync(id);
                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var holon = await _zcashRepository.LoadHolonByProviderKeyAsync(providerKey);
                result.Result = holon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var holons = await _zcashRepository.LoadHolonsForParentAsync(id);
                
                // Filter by type if specified
                if (type != HolonType.All)
                {
                    holons = holons.Where(h => h.HolonType == type);
                }

                result.Result = holons;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load parent holon first to get its ID
                var parentHolon = await LoadHolonAsync(providerKey);
                if (parentHolon.IsError || parentHolon.Result == null)
                {
                    result.Result = new List<IHolon>();
                    result.IsError = false;
                    return result;
                }

                // Load children using parent ID
                var childrenResult = await LoadHolonsForParentAsync(parentHolon.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                result.Result = childrenResult.Result;
                result.IsError = childrenResult.IsError;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var holons = await _zcashRepository.LoadHolonsByMetaDataAsync(metaKey, metaValue);
                
                // Filter by type if specified
                if (type != HolonType.All)
                {
                    holons = holons.Where(h => h.HolonType == type);
                }

                result.Result = holons;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var allMatchingHolons = new List<IHolon>();
                
                // For each key-value pair, load matching holons
                foreach (var kvp in metaKeyValuePairs)
                {
                    var holons = await _zcashRepository.LoadHolonsByMetaDataAsync(kvp.Key, kvp.Value);
                    
                    // TODO: Fix MetaKeyValuePairMatchMode.And - commented out due to build errors
                    // if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.And)
                    if (false) // Temporary: Always use OR mode until MetaKeyValuePairMatchMode is fixed
                    {
                        // AND mode: holon must match all key-value pairs
                        if (allMatchingHolons.Count == 0)
                        {
                            allMatchingHolons = holons.ToList();
                        }
                        else
                        {
                            // Intersect with existing results
                            allMatchingHolons = allMatchingHolons.Where(h => holons.Any(h2 => h2.Id == h.Id)).ToList();
                        }
                    }
                    else
                    {
                        // OR mode: holon can match any key-value pair
                        allMatchingHolons.AddRange(holons.Where(h => !allMatchingHolons.Any(h2 => h2.Id == h.Id)));
                    }
                }
                
                // Filter by type if specified
                if (type != HolonType.All)
                {
                    allMatchingHolons = allMatchingHolons.Where(h => h.HolonType == type).ToList();
                }

                result.Result = allMatchingHolons;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load all transactions and extract holons
                var txListResult = await _rpcClient.ListTransactionsAsync(10000); // Large limit to get all
                var holons = new List<IHolon>();

                if (!txListResult.IsError && txListResult.Result != null)
                {
                    foreach (var tx in txListResult.Result)
                    {
                        if (tx.ContainsKey("memo") && tx["memo"] != null)
                        {
                            try
                            {
                                var memo = tx["memo"].ToString();
                                var holon = await _zcashRepository.LoadHolonByProviderKeyAsync(
                                    tx.ContainsKey("txid") ? tx["txid"]?.ToString() ?? "" : "");
                                
                                if (holon != null)
                                {
                                    // Filter by type if specified
                                    if (type == HolonType.All || holon.HolonType == type)
                                    {
                                        holons.Add(holon);
                                    }
                                }
                            }
                            catch
                            {
                                if (!continueOnError)
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                var savedHolon = await _zcashRepository.SaveHolonAsync(holon);
                result.Result = savedHolon;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var savedHolons = new List<IHolon>();
            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (saveResult.IsError && !continueOnError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    savedHolons.Add(saveResult.Result);
                }
            }
            result.Result = savedHolons;
            result.IsError = false;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load holon first
                var holon = await LoadHolonAsync(id);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found");
                    return result;
                }

                // Delete holon (soft delete by default)
                // Use repository DeleteHolonAsync method
                var deleted = await _zcashRepository.DeleteHolonAsync(id, softDelete: true);
                if (deleted)
                {
                    result.Result = holon.Result;
                    result.IsError = false;
                    result.Message = "Holon deleted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to delete holon");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return result;
                }

                // Load holon by provider key first
                var holon = await LoadHolonAsync(providerKey);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found");
                    return result;
                }

                // Delete using GUID
                var deleteResult = await DeleteHolonAsync(holon.Result.Id);
                result.Result = deleteResult.Result;
                result.IsError = deleteResult.IsError;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "Import not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarById not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByUsername not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAllDataForAvatarByEmail not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "ExportAll not yet implemented for Zcash provider");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            OASISErrorHandling.HandleError(ref result, "SendToken not yet fully implemented for Zcash provider");
            return await Task.FromResult(result);
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            OASISErrorHandling.HandleError(ref result, "MintToken not yet fully implemented for Zcash provider");
            return await Task.FromResult(result);
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            OASISErrorHandling.HandleError(ref result, "BurnToken not yet fully implemented for Zcash provider");
            return await Task.FromResult(result);
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            OASISErrorHandling.HandleError(ref result, "LockToken not yet fully implemented for Zcash provider");
            return await Task.FromResult(result);
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return await Task.FromResult(result);
            }
            OASISErrorHandling.HandleError(ref result, "UnlockToken not yet fully implemented for Zcash provider");
            return await Task.FromResult(result);
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return await Task.FromResult(result);
                }

                // Get balance for the specified address or wallet
                string address = request?.WalletAddress;
                var balanceResult = await _rpcClient.GetBalanceAsync(address);
                
                if (balanceResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, balanceResult.Message);
                    return await Task.FromResult(result);
                }

                result.Result = (double)balanceResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return await Task.FromResult(result);
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return await Task.FromResult(result);
                }

                // Get transaction list
                // TODO: Fix IGetWeb3TransactionsRequest.Limit property - commented out due to build errors
                int count = 100; // request?.Limit ?? 100;
                var txListResult = await _rpcClient.ListTransactionsAsync(count);
                
                if (txListResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, txListResult.Message);
                    return await Task.FromResult(result);
                }

                // Convert to IWalletTransaction list
                var transactions = new List<IWalletTransaction>();
                if (txListResult.Result != null)
                {
                    foreach (var tx in txListResult.Result)
                    {
                        // Create wallet transaction from Zcash transaction data
                        // Note: This is a simplified conversion - would need proper mapping
                        // TODO: Fix WalletTransaction properties - commented out due to build errors
                        /*
                        var walletTx = new WalletTransaction
                        {
                            TransactionHash = tx.ContainsKey("txid") ? tx["txid"]?.ToString() : "",
                            FromAddress = tx.ContainsKey("address") ? tx["address"]?.ToString() : "",
                            ToAddress = tx.ContainsKey("address") ? tx["address"]?.ToString() : "",
                            Amount = tx.ContainsKey("amount") ? Convert.ToDouble(tx["amount"]) : 0.0,
                            // Additional fields would be mapped here
                        };
                        transactions.Add(walletTx);
                        */
                        // Temporary: Skip transaction conversion until WalletTransaction is fixed
                    }
                }

                result.Result = transactions;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return await Task.FromResult(result);
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
        {
            return GenerateKeyPairAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                    return await Task.FromResult(result);
                }

                // Determine address type (z-address for shielded, t-address for transparent)
                string addressType = request?.WalletAddress?.StartsWith("z") == true ? "sapling" : null;
                
                OASISResult<string> addressResult;
                if (addressType == "sapling")
                {
                    addressResult = await _rpcClient.GetNewZAddressAsync();
                }
                else
                {
                    addressResult = await _rpcClient.GetNewTAddressAsync();
                }

                if (addressResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, addressResult.Message);
                    return await Task.FromResult(result);
                }

                // Create key pair result
                // TODO: Fix KeyPairAndWallet type - commented out due to build errors
                /*
                var keyPair = new KeyPairAndWallet
                {
                    PublicKey = addressResult.Result,
                    WalletAddress = addressResult.Result,
                    // Note: Private keys are not exposed by Zcash RPC for security
                    // In production, would need to handle key generation differently
                };

                result.Result = keyPair;
                */
                // Temporary: Return address directly until KeyPairAndWallet is fixed
                OASISErrorHandling.HandleError(ref result, "KeyPairAndWallet type not available - needs to be implemented");
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return await Task.FromResult(result);
        }

        #endregion

        #region IOASISNETProvider - GetAvatarsNearMe and GetHolonsNearMe

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return result;
            }

            OASISErrorHandling.HandleError(ref result, "GetAvatarsNearMe not yet implemented for Zcash provider");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Zcash provider is not activated");
                return result;
            }

            OASISErrorHandling.HandleError(ref result, "GetHolonsNearMe not yet implemented for Zcash provider");
            return result;
        }

        #endregion
    }
}

