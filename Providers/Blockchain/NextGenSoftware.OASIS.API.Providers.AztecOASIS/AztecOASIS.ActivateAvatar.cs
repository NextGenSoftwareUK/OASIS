using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS
{
    public partial class AztecOASIS
    {
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                _aztecService = new AztecService(_apiClient, _stablecoinContractAddress);
                _bridgeService = new AztecBridgeService(_apiClient, _bridgeContractAddress, _operatorAccountAlias);
                _aztecRepository = new AztecRepository();

                IsProviderActivated = true;
                result.Result = true;
                result.Message = "Aztec provider activated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to activate Aztec provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _aztecService = null;
            _bridgeService = null;
            _aztecRepository = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }


        public async Task<OASISResult<PrivateNote>> CreatePrivateNoteAsync(decimal value, string ownerPublicKey, string metadata = null)
        {
            var result = new OASISResult<PrivateNote>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                result.Result = await _aztecService.CreatePrivateNoteAsync(value, ownerPublicKey, metadata);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }

            return result;
        }

        public async Task<OASISResult<AztecProof>> GenerateProofAsync(string proofType, object payload)
        {
            var result = new OASISResult<AztecProof>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                result.Result = await _aztecService.GenerateProofAsync(proofType, payload);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<AztecTransaction>> SubmitProofAsync(AztecProof proof)
        {
            var result = new OASISResult<AztecTransaction>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                result.Result = await _aztecService.SubmitProofAsync(proof);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<AztecTransaction>> DepositFromZcashAsync(decimal amount, string zcashTxId, PrivateNote aztecNote)
        {
            var result = new OASISResult<AztecTransaction>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                result.Result = await _bridgeService.DepositFromZcashAsync(amount, zcashTxId, aztecNote);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<AztecTransaction>> WithdrawToZcashAsync(PrivateNote note, AztecProof proof, string destinationAddress)
        {
            var result = new OASISResult<AztecTransaction>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                result.Result = await _bridgeService.WithdrawToZcashAsync(note, proof, destinationAddress);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        // Stablecoin operations
        public async Task<OASISResult<string>> MintStablecoinAsync(string aztecAddress, decimal amount, string zcashTxHash, string viewingKey)
        {
            var result = new OASISResult<string>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var mintResult = await _aztecService.MintStablecoinAsync(aztecAddress, amount, zcashTxHash, viewingKey);
                result.Result = mintResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> BurnStablecoinAsync(string aztecAddress, decimal amount, string positionId)
        {
            var result = new OASISResult<string>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var burnResult = await _aztecService.BurnStablecoinAsync(aztecAddress, amount, positionId);
                result.Result = burnResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> DeployToYieldStrategyAsync(string aztecAddress, decimal amount, string strategy)
        {
            var result = new OASISResult<string>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var deployResult = await _aztecService.DeployToYieldStrategyAsync(aztecAddress, amount, strategy);
                result.Result = deployResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> SeizeCollateralAsync(string aztecAddress, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                var seizeResult = await _aztecService.SeizeCollateralAsync(aztecAddress, amount);
                result.Result = seizeResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }



        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>
            {
                Result = new List<IAvatar>(),
                IsError = false
            };
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar from Aztec (stored as holon)
                var holon = await LoadHolonAsync(Id);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Convert holon to avatar
                if (holon.Result is IAvatar avatar)
                {
                    result.Result = avatar;
                }
                else
                {
                    result.Result = ConvertHolonToAvatar(holon.Result);
                }
                result.IsError = false;
                result.Message = "Avatar loaded successfully from Aztec";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0) => LoadAvatarAsync(Id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar by Aztec address (provider key)
                var holon = await LoadHolonAsync(providerKey);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Convert holon to avatar
                if (holon.Result is IAvatar avatar)
                {
                    result.Result = avatar;
                }
                else
                {
                    result.Result = ConvertHolonToAvatar(holon.Result);
                }
                result.IsError = false;
                result.Message = "Avatar loaded successfully from Aztec by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar by searching for holon with username in metadata
                var holonsResult = await LoadHolonsByMetaDataAsync("Username", avatarUsername, HolonType.Avatar);
                if (holonsResult.IsError || holonsResult.Result == null || !holonsResult.Result.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                    return result;
                }

                var holon = holonsResult.Result.First();
                if (holon is IAvatar avatar)
                {
                    result.Result = avatar;
                }
                else
                {
                    result.Result = ConvertHolonToAvatar(holon);
                }
                result.IsError = false;
                result.Message = "Avatar loaded successfully from Aztec by username";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar by searching for holon with email in metadata
                var holonsResult = await LoadHolonsByMetaDataAsync("Email", avatarEmail, HolonType.Avatar);
                if (holonsResult.IsError || holonsResult.Result == null || !holonsResult.Result.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                    return result;
                }

                var holon = holonsResult.Result.First();
                if (holon is IAvatar avatar)
                {
                    result.Result = avatar;
                }
                else
                {
                    result.Result = ConvertHolonToAvatar(holon);
                }
                result.IsError = false;
                result.Message = "Avatar loaded successfully from Aztec by email";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar detail as separate object (holon with HolonType.AvatarDetail)
                var holonResult = await LoadHolonAsync($"avatar-detail:{id}");
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found");
                    return result;
                }

                var avatarDetail = ConvertHolonToAvatarDetail(holonResult.Result);
                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully from Aztec";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar detail as separate object (HolonType.AvatarDetail)
                var holonsResult = await LoadHolonsByMetaDataAsync("Email", avatarEmail, HolonType.AvatarDetail);
                if (holonsResult.IsError || holonsResult.Result == null || !holonsResult.Result.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found by email");
                    return result;
                }

                var avatarDetail = ConvertHolonToAvatarDetail(holonsResult.Result.First());
                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully from Aztec by email";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

    }
}
