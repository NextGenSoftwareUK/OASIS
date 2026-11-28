using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using LockWeb3TokenRequest = NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.LockWeb3TokenRequest;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Models;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS
{
    public partial class MidenOASIS
    {

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate key pair: use KeyHelper (IKeyPairAndWallet from Utilities via KeyManager/Core); API could be used with a matching DTO if needed
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    result.Result = keyPair;
                    result.IsError = false;
                    result.Message = "Key pair generated successfully for Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to generate key pair from Miden");
                }
            }
            catch (Exception ex)
            {
                // Fallback: Use KeyHelper if API call fails
                try
                {
                    var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                    if (keyPair != null)
                    {
                        result.Result = keyPair;
                        result.IsError = false;
                        result.Message = "Key pair generated successfully using KeyHelper (fallback)";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
                    }
                }
                catch
                {
                    OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
                }
            }
            return result;
        }

    }
}
