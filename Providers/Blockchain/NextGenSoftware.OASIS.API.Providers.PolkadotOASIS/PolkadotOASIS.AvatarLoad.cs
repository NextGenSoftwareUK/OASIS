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
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    public partial class PolkadotOASIS
    {
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            await _lifecycleGate.WaitAsync();

            try
            {
                if(_isDisposed)throw new ObjectDisposedException(nameof(PolkadotOASIS));
                if (_isActivated)
                {
                    response.Result = true;
                    response.Message = "Polkadot provider is already activated";
                    return response;
                }

                var chain=(await QueryRpcAsync("system_chain",Array.Empty<object>())).GetString();
                if(string.IsNullOrWhiteSpace(chain))throw new System.IO.InvalidDataException("Polkadot chain name is missing.");
                if(!_chainId.StartsWith("0x",StringComparison.Ordinal) && !string.Equals(chain,_chainId,StringComparison.OrdinalIgnoreCase))
                    throw new System.IO.InvalidDataException("Polkadot RPC chain name differs from configured network.");
                var genesis=(await QueryRpcAsync("chain_getBlockHash",new object[]{0})).GetString();
                if(!IsGenesisHash(genesis) || _chainId.StartsWith("0x",StringComparison.Ordinal) && !string.Equals(genesis,_chainId,StringComparison.OrdinalIgnoreCase))
                    throw new System.IO.InvalidDataException("Polkadot RPC genesis differs from configured network or is invalid.");
                ReportedChainName=chain;ReportedGenesisHash=genesis;
                _isActivated=true;IsProviderActivated=true;response.Result=true;
                response.Message="Polkadot RPC network verified; storage and transaction capabilities require separate verification.";
            }
            catch (Exception ex)
            {
                _isActivated=false;IsProviderActivated=false;ReportedChainName=null;ReportedGenesisHash=null;
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Polkadot provider: {ex.Message}");
            }
            finally{_lifecycleGate.Release();}

            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            await _lifecycleGate.WaitAsync();

            try
            {
                _isActivated = false;
                IsProviderActivated=false;ReportedChainName=null;ReportedGenesisHash=null;
                response.Result = true;
                response.Message = "Polkadot provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Polkadot provider: {ex.Message}");
            }
            finally{_lifecycleGate.Release();}

            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

    }
}
