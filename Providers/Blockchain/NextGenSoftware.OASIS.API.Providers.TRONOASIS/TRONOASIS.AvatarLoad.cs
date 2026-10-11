using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS
    {
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            await _runtimeLifecycle.WaitAsync();
            try
            {
                if (IsProviderActivated)
                {
                    response.Result = true;
                    return response;
                }
                if (_nativeTransfers == null)
                {
                    _ownedRuntime = new TronSdkRuntime(new Uri(_fullNodeGrpcEndpoint), new Uri(_solidityNodeGrpcEndpoint),
                        _genesisBlockId, Environment.GetEnvironmentVariable(_apiKeyEnvironmentVariable), _contractFeeLimitSun);
                    _nativeTransfers = _ownedRuntime.NativeTransfers;
                    _nativeSigningKeyResolver = address => Task.FromResult(Environment.GetEnvironmentVariable(_signingKeyEnvironmentVariable));
                }
                await _nativeTransfers.VerifyNetworkAsync();
                await ((TronProviderStorageBackend)Backend).VerifyForActivationAsync();
                IsProviderActivated = true;
                response.Result = true;
                response.Message = "TRON provider activated successfully";
            }
            catch (Exception ex)
            {
                IsProviderActivated = false;
                ((TronProviderStorageBackend)Backend).ResetOwnedStorage();
                if (_ownedRuntime != null)
                {
                    var failedRuntime = _ownedRuntime;
                    _ownedRuntime = null;
                    _nativeTransfers = null;
                    _nativeSigningKeyResolver = null;
                    try { await failedRuntime.DisposeAsync(); }
                    catch (Exception cleanupError) { ex = new AggregateException(ex, cleanupError); }
                }
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating TRON provider: {ex.Message}");
            }
            finally { _runtimeLifecycle.Release(); }
            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            await _runtimeLifecycle.WaitAsync();
            try
            {
                IsProviderActivated = false;
                ((TronProviderStorageBackend)Backend).ResetOwnedStorage();
                if (_ownedRuntime != null)
                {
                    var runtime = _ownedRuntime;
                    _ownedRuntime = null;
                    _nativeTransfers = null;
                    _nativeSigningKeyResolver = null;
                    await runtime.DisposeAsync();
                }
                IsProviderActivated = false;
                response.Result = true;
                response.Message = "TRON provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating TRON provider: {ex.Message}");
            }
            finally { _runtimeLifecycle.Release(); }
            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

    }
}
