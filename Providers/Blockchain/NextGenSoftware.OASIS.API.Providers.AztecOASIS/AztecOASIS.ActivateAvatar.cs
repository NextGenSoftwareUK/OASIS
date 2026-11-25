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




    }
}
